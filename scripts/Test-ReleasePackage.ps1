[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$definition = Import-PowerShellDataFile `
    -LiteralPath (Join-Path $PSScriptRoot 'ReleasePackageFiles.psd1')
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$expectedFileName = "CrystariumBoutique-$ExpectedVersion.zip"
if ([IO.Path]::GetFileName($resolvedPackagePath) -ne $expectedFileName) {
    throw "Release ZIP '$resolvedPackagePath' must be named '$expectedFileName'."
}

& (Join-Path $PSScriptRoot 'Assert-ReleasePackageLayout.ps1') `
    -PackagePath $resolvedPackagePath

Add-Type -AssemblyName System.IO.Compression
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $entries = @($archive.Entries | Where-Object { -not [string]::IsNullOrEmpty($_.Name) })
    $entryMap = @{}
    foreach ($entry in $entries) {
        if ($entryMap.ContainsKey($entry.FullName)) {
            throw "Release package contains duplicate entry '$($entry.FullName)'."
        }
        $entryMap[$entry.FullName] = $entry
    }

    $expectedEntries = @(
        $definition.RootFiles
        $definition.ImageFiles | ForEach-Object { "images/$_" }
        $definition.DataFiles | ForEach-Object { "data/$_" }
        $definition.DistributionFiles | ForEach-Object {
            $_.Destination.Replace('\', '/')
        }
        'PACKAGE-MANIFEST.json'
    )
    $missing = @($expectedEntries | Where-Object { -not $entryMap.ContainsKey($_) })
    $unexpected = @($entryMap.Keys | Where-Object { $_ -notin $expectedEntries } | Sort-Object)
    if ($missing.Count -gt 0) {
        throw "Release package is missing allowlisted entries: $($missing -join ', ')."
    }
    if ($unexpected.Count -gt 0) {
        throw "Release package contains unexpected entries: $($unexpected -join ', ')."
    }

    $forbiddenNames = @(
        'Glamourer.Api.dll',
        'Penumbra.Api.dll',
        'Glamourer.dll',
        'Penumbra.dll',
        'Luna.dll'
    )
    $forbiddenPattern = '(^|/)(src|tests|tools|bin|obj|\.git|\.vs|artifacts)(/|$)|\.(cs|csproj|pdb|user|suo|log|dmp|key|pfx|pem)$|generation-review\.json$|Foundation\.docx?$|PRIVATE-BETA-TESTING\.md$|crystarium-item-frame\.png$'
    $forbidden = @(
        $entryMap.Keys |
            Where-Object {
                [IO.Path]::GetFileName($_) -in $forbiddenNames -or $_ -match $forbiddenPattern
            } |
            Sort-Object
    )
    if ($forbidden.Count -gt 0) {
        throw "Release package contains forbidden content: $($forbidden -join ', ')."
    }

    # ZIP stores calendar fields but not the original timezone offset. Compare the
    # persisted components rather than a DateTimeOffset instant.
    $badTimestamps = @(
        $entries |
            Where-Object {
                $_.LastWriteTime.Year -ne 2000 -or
                $_.LastWriteTime.Month -ne 1 -or
                $_.LastWriteTime.Day -ne 1 -or
                $_.LastWriteTime.Hour -ne 0 -or
                $_.LastWriteTime.Minute -ne 0 -or
                $_.LastWriteTime.Second -ne 0
            }
    )
    if ($badTimestamps.Count -gt 0) {
        throw "Release package contains nondeterministic timestamps: $($badTimestamps.FullName -join ', ')."
    }

    function Read-EntryBytes {
        param([Parameter(Mandatory)]$Entry)

        $stream = $Entry.Open()
        try {
            $memory = [IO.MemoryStream]::new()
            try {
                $stream.CopyTo($memory)
                return $memory.ToArray()
            }
            finally {
                $memory.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }

    function Read-EntryText {
        param([Parameter(Mandatory)]$Entry)
        return [Text.Encoding]::UTF8.GetString((Read-EntryBytes -Entry $Entry))
    }

    $packageManifest = Read-EntryText `
        -Entry $entryMap['PACKAGE-MANIFEST.json'] |
        ConvertFrom-Json
    if ($packageManifest.schemaVersion -ne 1 -or
        $packageManifest.packageVersion -ne $ExpectedVersion) {
        throw "Release package manifest version/schema is inconsistent."
    }
    if ($packageManifest.buildConfiguration -ne 'Release' -or
        $packageManifest.buildPlatform -ne 'x64' -or
        $packageManifest.dalamudApiLevel -ne 15) {
        throw 'Release package was not produced for x64 Release / Dalamud API 15.'
    }
    if ([string]$packageManifest.sourceCommit -notmatch '^[0-9a-f]{40}$' -or
        $packageManifest.sourceRepository -ne 'https://github.com/Aesthria/The-Crystarium-Boutique') {
        throw 'Release package source identity is invalid.'
    }
    if ($packageManifest.buildIdentity -ne "$ExpectedVersion@$($packageManifest.sourceCommit)") {
        throw "Release package build identity '$($packageManifest.buildIdentity)' is invalid."
    }

    $manifestMap = @{}
    foreach ($file in $packageManifest.files) {
        if ($manifestMap.ContainsKey($file.path)) {
            throw "Package manifest contains duplicate path '$($file.path)'."
        }
        $manifestMap[$file.path] = $file
    }
    $expectedManifestPaths = @(
        $expectedEntries |
            Where-Object { $_ -ne 'PACKAGE-MANIFEST.json' }
    )
    $manifestMissing = @($expectedManifestPaths | Where-Object { -not $manifestMap.ContainsKey($_) })
    $manifestUnexpected = @($manifestMap.Keys | Where-Object { $_ -notin $expectedManifestPaths })
    if ($manifestMissing.Count -gt 0 -or $manifestUnexpected.Count -gt 0) {
        throw 'Package manifest does not match the public release allowlist.'
    }

    foreach ($relativePath in $expectedManifestPaths) {
        $entry = $entryMap[$relativePath]
        $bytes = Read-EntryBytes -Entry $entry
        $actualHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
        $record = $manifestMap[$relativePath]
        if ($record.length -ne $bytes.Length -or $record.sha256 -ne $actualHash) {
            throw "Package manifest hash/length mismatch for '$relativePath'."
        }
    }

    $pluginManifest = Read-EntryText -Entry $entryMap['CrystariumBoutique.json'] |
        ConvertFrom-Json
    if ($pluginManifest.InternalName -ne 'CrystariumBoutique' -or
        [string]$pluginManifest.AssemblyVersion -ne [string]$packageManifest.assemblyVersion -or
        $pluginManifest.DalamudApiLevel -ne 15) {
        throw 'Packaged Dalamud manifest identity/version/API is inconsistent.'
    }

    $pluginBytes = Read-EntryBytes -Entry $entryMap['CrystariumBoutique.dll']
    $pluginHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($pluginBytes)).ToLowerInvariant()
    if ($pluginHash -ne $packageManifest.pluginAssemblySha256) {
        throw 'Packaged plugin DLL hash does not match PACKAGE-MANIFEST.json.'
    }

    $depsText = Read-EntryText -Entry $entryMap['CrystariumBoutique.deps.json']
    foreach ($forbiddenName in $forbiddenNames) {
        if ($depsText.Contains($forbiddenName, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Dependency manifest mentions forbidden dependency '$forbiddenName'."
        }
    }

    $extractionRoot = Join-Path ([IO.Path]::GetTempPath()) "CrystariumBoutique-release-$([Guid]::NewGuid().ToString('N'))"
    try {
        [IO.Compression.ZipFile]::ExtractToDirectory($resolvedPackagePath, $extractionRoot)
        $extractedAssemblyPath = Join-Path $extractionRoot 'CrystariumBoutique.dll'
        $extractedManifestPath = Join-Path $extractionRoot 'CrystariumBoutique.json'
        if (-not (Test-Path -LiteralPath $extractedAssemblyPath -PathType Leaf) -or
            -not (Test-Path -LiteralPath $extractedManifestPath -PathType Leaf)) {
            throw 'Dalamud installer compatibility failed: main DLL and manifest must exist at extraction root.'
        }

        $extractedManifest = Get-Content -Raw -LiteralPath $extractedManifestPath | ConvertFrom-Json
        if ($extractedManifest.InternalName -ne 'CrystariumBoutique') {
            throw "Extracted manifest InternalName '$($extractedManifest.InternalName)' is invalid."
        }
        if ([string]$extractedManifest.AssemblyVersion -ne [string]$packageManifest.assemblyVersion) {
            throw 'Extracted manifest assembly version does not match PACKAGE-MANIFEST.json.'
        }

        $extractedAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($extractedAssemblyPath).Version.ToString()
        if ($extractedAssemblyVersion -ne [string]$packageManifest.assemblyVersion) {
            throw "Extracted DLL assembly version '$extractedAssemblyVersion' does not match '$($packageManifest.assemblyVersion)'."
        }
    }
    finally {
        if (Test-Path -LiteralPath $extractionRoot) {
            Remove-Item -LiteralPath $extractionRoot -Recurse -Force
        }
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Release package validation passed: $($entries.Count) files, version $ExpectedVersion."
