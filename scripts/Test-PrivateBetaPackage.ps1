[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$packageDefinition = Import-PowerShellDataFile `
    -LiteralPath (Join-Path $PSScriptRoot 'RuntimePackageFiles.psd1')
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path

Add-Type -AssemblyName System.IO.Compression
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $fileEntries = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    $entryMap = @{}
    foreach ($entry in $fileEntries) {
        if ($entryMap.ContainsKey($entry.FullName)) {
            throw "Package contains a duplicate entry: $($entry.FullName)"
        }

        $entryMap[$entry.FullName] = $entry
    }

    $forbiddenRuntimeDependencies = @(
        'Glamourer.Api.dll'
        'Penumbra.Api.dll'
        'Luna.dll'
        'Microsoft.Extensions.DependencyInjection.Abstractions.dll'
        'Microsoft.Extensions.DependencyInjection.dll'
        'Microsoft.Extensions.Logging.Abstractions.dll'
    )
    $forbiddenDependencyEntries = @(
        $entryMap.Keys |
            Where-Object { [IO.Path]::GetFileName($_) -in $forbiddenRuntimeDependencies } |
            Sort-Object
    )
    if ($forbiddenDependencyEntries.Count -gt 0) {
        throw "Package contains forbidden runtime dependencies: $($forbiddenDependencyEntries -join ', ')"
    }

    $expectedEntries = @(
        $packageDefinition.RootFiles | ForEach-Object { "CrystariumBoutique/$_" }
        $packageDefinition.ImageFiles | ForEach-Object { "CrystariumBoutique/images/$_" }
        $packageDefinition.DataFiles | ForEach-Object { "CrystariumBoutique/data/$_" }
        $packageDefinition.DistributionFiles | ForEach-Object {
            "CrystariumBoutique/$($_.Destination.Replace('\', '/'))"
        }
        'CrystariumBoutique/PACKAGE-MANIFEST.json'
    )

    $missingEntries = @($expectedEntries | Where-Object { -not $entryMap.ContainsKey($_) })
    $unexpectedEntries = @($entryMap.Keys | Where-Object { $_ -notin $expectedEntries } | Sort-Object)
    if ($missingEntries.Count -gt 0) {
        throw "Package is missing expected entries: $($missingEntries -join ', ')"
    }

    if ($unexpectedEntries.Count -gt 0) {
        throw "Package contains unexpected entries: $($unexpectedEntries -join ', ')"
    }

    $forbiddenPattern = '(^|/)(src|tests|tools|bin|obj|\.git|\.vs|artifacts)(/|$)|\.(cs|csproj|pdb|user|suo|log|dmp|key|pfx|pem)$'
    $forbiddenEntries = @($entryMap.Keys | Where-Object { $_ -match $forbiddenPattern })
    if ($forbiddenEntries.Count -gt 0) {
        throw "Package contains forbidden content: $($forbiddenEntries -join ', ')"
    }

    function Read-ZipEntryText {
        param([Parameter(Mandatory)]$Entry)

        $stream = $Entry.Open()
        try {
            $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false), $true)
            try {
                return $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }

    $packageManifest = Read-ZipEntryText `
        -Entry $entryMap['CrystariumBoutique/PACKAGE-MANIFEST.json'] |
        ConvertFrom-Json
    if ($packageManifest.schemaVersion -ne 2) {
        throw "Unsupported package manifest schema: $($packageManifest.schemaVersion)"
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion) -and
        $packageManifest.packageVersion -ne $ExpectedVersion) {
        throw "Package version '$($packageManifest.packageVersion)' does not match '$ExpectedVersion'."
    }

    if ($packageManifest.sourceRepository -ne 'https://github.com/Aesthria/The-Crystarium-Boutique') {
        throw "Unexpected source repository '$($packageManifest.sourceRepository)'."
    }

    if ([string]$packageManifest.sourceCommit -notmatch '^[0-9a-f]{40}$') {
        throw "Invalid source commit '$($packageManifest.sourceCommit)'."
    }

    $expectedBuildIdentity = "$($packageManifest.packageVersion)@$($packageManifest.sourceCommit)"
    if ($packageManifest.buildIdentity -ne $expectedBuildIdentity) {
        throw "Build identity '$($packageManifest.buildIdentity)' does not match '$expectedBuildIdentity'."
    }

    if ($packageManifest.buildConfiguration -ne 'Release' -or
        $packageManifest.buildPlatform -ne 'x64') {
        throw "Package was not produced as an x64 Release build."
    }

    $globalJson = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'global.json') |
        ConvertFrom-Json
    $minimumSdkVersion = [Version][string]$globalJson.sdk.version
    $actualSdkVersion = [Version][string]$packageManifest.dotnetSdkVersion
    $usesPinnedFeatureBand =
        $actualSdkVersion.Major -eq $minimumSdkVersion.Major -and
        $actualSdkVersion.Minor -eq $minimumSdkVersion.Minor -and
        [Math]::Floor($actualSdkVersion.Build / 100) -eq
            [Math]::Floor($minimumSdkVersion.Build / 100)
    $sdkVersionAccepted = if ([string]$globalJson.sdk.rollForward -eq 'latestPatch') {
        $usesPinnedFeatureBand -and $actualSdkVersion -ge $minimumSdkVersion
    }
    else {
        $actualSdkVersion -eq $minimumSdkVersion
    }

    if (-not $sdkVersionAccepted) {
        throw "Package used .NET SDK '$($packageManifest.dotnetSdkVersion)', which is outside the global.json policy rooted at '$($globalJson.sdk.version)' with rollForward '$($globalJson.sdk.rollForward)'."
    }

    if ([string]$packageManifest.dalamudAssemblyVersion -notmatch '^15\.') {
        throw "Package used unexpected Dalamud assembly '$($packageManifest.dalamudAssemblyVersion)'."
    }

    if (-not [string]$packageManifest.pluginProductVersion -or
        -not ([string]$packageManifest.pluginProductVersion).Contains(
            [string]$packageManifest.sourceCommit,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "Plugin product version '$($packageManifest.pluginProductVersion)' does not identify the packaged source commit."
    }

    $manifestFileMap = @{}
    foreach ($file in $packageManifest.files) {
        if ($manifestFileMap.ContainsKey($file.path)) {
            throw "Package manifest contains a duplicate path: $($file.path)"
        }

        $manifestFileMap[$file.path] = $file
    }

    $expectedManifestPaths = @(
        $expectedEntries |
            Where-Object { $_ -ne 'CrystariumBoutique/PACKAGE-MANIFEST.json' } |
            ForEach-Object { $_.Substring('CrystariumBoutique/'.Length) }
    )
    $missingManifestPaths = @($expectedManifestPaths | Where-Object { -not $manifestFileMap.ContainsKey($_) })
    $unexpectedManifestPaths = @($manifestFileMap.Keys | Where-Object { $_ -notin $expectedManifestPaths })
    if ($missingManifestPaths.Count -gt 0 -or $unexpectedManifestPaths.Count -gt 0) {
        throw "Package manifest file list differs from the package allowlist. Missing: $($missingManifestPaths -join ', '); unexpected: $($unexpectedManifestPaths -join ', ')."
    }

    $pluginManifestEntry = $manifestFileMap['CrystariumBoutique.dll']
    if ($null -eq $pluginManifestEntry -or
        $packageManifest.pluginAssemblySha256 -ne $pluginManifestEntry.sha256) {
        throw 'Package-level plugin assembly hash does not match the file manifest.'
    }

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        foreach ($relativePath in $expectedManifestPaths) {
            $entry = $entryMap["CrystariumBoutique/$relativePath"]
            $stream = $entry.Open()
            try {
                $actualHash = [Convert]::ToHexString($sha256.ComputeHash($stream)).ToLowerInvariant()
            }
            finally {
                $stream.Dispose()
            }

            $manifestFile = $manifestFileMap[$relativePath]
            if ($actualHash -ne $manifestFile.sha256) {
                throw "SHA-256 mismatch for '$relativePath'."
            }

            if ($entry.Length -ne $manifestFile.length) {
                throw "Length mismatch for '$relativePath'."
            }
        }
    }
    finally {
        $sha256.Dispose()
    }

    $pluginManifest = Read-ZipEntryText `
        -Entry $entryMap['CrystariumBoutique/CrystariumBoutique.json'] |
        ConvertFrom-Json
    if ([string]$pluginManifest.AssemblyVersion -ne [string]$packageManifest.assemblyVersion) {
        throw "Dalamud manifest AssemblyVersion '$($pluginManifest.AssemblyVersion)' does not match package manifest '$($packageManifest.assemblyVersion)'."
    }
    $expectedIconUrl = 'https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/src/CrystariumBoutique/images/icon.png'
    if ([string]::IsNullOrWhiteSpace([string]$pluginManifest.IconUrl) -or
        [string]$pluginManifest.IconUrl -ne $expectedIconUrl) {
        throw 'Packaged Dalamud manifest IconUrl is missing or inconsistent with repo.json.'
    }

    $dependencyManifestText = Read-ZipEntryText `
        -Entry $entryMap['CrystariumBoutique/CrystariumBoutique.deps.json']
    $forbiddenDependencyMetadata = @(
        $forbiddenRuntimeDependencies |
            ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) } |
            Where-Object { $dependencyManifestText -match [regex]::Escape($_) }
    )
    if ($forbiddenDependencyMetadata.Count -gt 0) {
        throw "Package dependency manifest references forbidden runtime dependencies: $($forbiddenDependencyMetadata -join ', ')"
    }

    Write-Output "Private beta package validation passed: $($fileEntries.Count) files, version $($packageManifest.packageVersion)."
}
finally {
    $archive.Dispose()
}
