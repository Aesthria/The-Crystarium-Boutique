[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RepoIndexPath,

    [Parameter(Mandatory)]
    [string]$PackagePath,

    [Parameter(Mandatory)]
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$resolvedIndexPath = (Resolve-Path -LiteralPath $RepoIndexPath).Path
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$rawIndex = Get-Content -Raw -LiteralPath $resolvedIndexPath
if (-not $rawIndex.TrimStart().StartsWith('[', [StringComparison]::Ordinal) -or
    -not $rawIndex.TrimEnd().EndsWith(']', [StringComparison]::Ordinal)) {
    throw 'Repository index must be a top-level JSON array.'
}
$entries = @($rawIndex | ConvertFrom-Json)
if ($entries.Count -ne 1) {
    throw "Repository index must contain exactly one entry; found $($entries.Count)."
}
$entry = $entries[0]

$requiredText = @(
    'Author',
    'Name',
    'Punchline',
    'Description',
    'InternalName',
    'AssemblyVersion',
    'RepoUrl',
    'ApplicableVersion',
    'DownloadLinkInstall',
    'DownloadLinkUpdate',
    'IconUrl',
    'Changelog'
)
foreach ($property in $requiredText) {
    if ([string]::IsNullOrWhiteSpace([string]$entry.$property)) {
        throw "Repository entry property '$property' is required."
    }
}
if ($entry.Author -ne 'Aesthria' -or
    $entry.Name -ne 'The Crystarium Boutique' -or
    $entry.InternalName -ne 'CrystariumBoutique' -or
    $entry.ApplicableVersion -ne 'any' -or
    $entry.DalamudApiLevel -ne 15 -or
    $entry.LoadPriority -ne -100 -or
    $entry.IsHide -ne $false -or
    $entry.IsTestingExclusive -ne $false) {
    throw 'Repository entry identity/channel/API values are invalid.'
}
if (@($entry.Tags).Count -ne 6 -or @($entry.Tags | Select-Object -Unique).Count -ne 6) {
    throw 'Repository entry must contain the six unique approved tags.'
}
if ($entry.PSObject.Properties.Name -contains 'TestingAssemblyVersion' -or
    $entry.PSObject.Properties.Name -contains 'DownloadLinkTesting' -or
    $entry.PSObject.Properties.Name -contains 'TestingDalamudApiLevel') {
    throw 'Stable repository entry must not contain testing-channel fields.'
}

$tag = "v$ExpectedVersion"
$assetName = "CrystariumBoutique-$ExpectedVersion.zip"
$expectedDownload = "https://github.com/Aesthria/The-Crystarium-Boutique/releases/download/$tag/$assetName"
if ($entry.DownloadLinkInstall -ne $expectedDownload -or
    $entry.DownloadLinkUpdate -ne $expectedDownload) {
    throw 'Install/update URLs do not match the deterministic versioned GitHub Release asset URL.'
}
if ($entry.RepoUrl -ne 'https://github.com/Aesthria/The-Crystarium-Boutique' -or
    $entry.IconUrl -ne 'https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/src/CrystariumBoutique/images/icon.png') {
    throw 'Repository or icon URL is inconsistent with the future public repository layout.'
}

$forbiddenText = @(
    'CHANGEME',
    'example.com',
    'localhost',
    '127.0.0.1',
    'C:\Dev',
    'The-Crystarium-Boutique-private-archive',
    'PRIVATE-BETA-TESTING'
)
foreach ($text in $forbiddenText) {
    if ($rawIndex.Contains($text, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Repository index contains forbidden placeholder/private text '$text'."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $manifestEntry = $archive.GetEntry('PACKAGE-MANIFEST.json')
    if ($null -eq $manifestEntry) {
        throw 'Release ZIP does not contain PACKAGE-MANIFEST.json.'
    }
    $stream = $manifestEntry.Open()
    try {
        $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false), $true)
        try {
            $packageManifest = $reader.ReadToEnd() | ConvertFrom-Json
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}
finally {
    $archive.Dispose()
}

if ($packageManifest.packageVersion -ne $ExpectedVersion -or
    [string]$packageManifest.assemblyVersion -ne [string]$entry.AssemblyVersion -or
    $packageManifest.dalamudApiLevel -ne $entry.DalamudApiLevel -or
    [IO.Path]::GetFileName($resolvedPackagePath) -ne $assetName) {
    throw 'Repository index and release package versions/API/filename are inconsistent.'
}

Write-Output "Repository index validation passed: 1 entry, version $ExpectedVersion, API $($entry.DalamudApiLevel)."
