[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$OutputPath,

    [string]$Changelog
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot 'repo.json'
}
else {
    $OutputPath = [IO.Path]::GetFullPath($OutputPath)
}

$versionMatch = [regex]::Match(
    $Version,
    '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-rc\.(?<revision>0|[1-9]\d*))?$')
if (-not $versionMatch.Success) {
    throw "Repository versions must use major.minor.patch or major.minor.patch-rc.N: '$Version'."
}
if (-not $versionMatch.Groups['revision'].Success -and [string]::IsNullOrWhiteSpace($Changelog)) {
    throw 'A non-empty -Changelog is required for a final release repository entry.'
}
if ([string]::IsNullOrWhiteSpace($Changelog)) {
    $Changelog = 'Local release-infrastructure dry run; not published.'
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

if ($packageManifest.packageVersion -ne $Version) {
    throw "Package version '$($packageManifest.packageVersion)' does not match '$Version'."
}
if ($packageManifest.dalamudApiLevel -ne 15) {
    throw "Unexpected Dalamud API level '$($packageManifest.dalamudApiLevel)'."
}

$tag = "v$Version"
$assetName = "CrystariumBoutique-$Version.zip"
$releaseUrl = "https://github.com/Aesthria/The-Crystarium-Boutique/releases/download/$tag/$assetName"
$entry = [pscustomobject][ordered]@{
    Author = 'Aesthria'
    Name = 'The Crystarium Boutique'
    Punchline = "Browse FFXIV's wardrobe like a boutique."
    Description = 'Browse FFXIV equipment like a boutique, preview items and dyes instantly, organize Favorites and Designs, use the Crystal Wardrobe, and view offline acquisition information.'
    InternalName = 'CrystariumBoutique'
    AssemblyVersion = [string]$packageManifest.assemblyVersion
    RepoUrl = 'https://github.com/Aesthria/The-Crystarium-Boutique'
    ApplicableVersion = 'any'
    DalamudApiLevel = [int]$packageManifest.dalamudApiLevel
    LoadPriority = -100
    DownloadLinkInstall = $releaseUrl
    DownloadLinkUpdate = $releaseUrl
    IconUrl = 'https://raw.githubusercontent.com/Aesthria/The-Crystarium-Boutique/main/src/CrystariumBoutique/images/icon.png'
    Tags = @(
        'appearance'
        'glamour'
        'wardrobe'
        'fashion'
        'equipment'
        'design'
    )
    Changelog = $Changelog
    IsHide = $false
    IsTestingExclusive = $false
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
$indexJson = ConvertTo-Json -InputObject @($entry) -Depth 6
$indexJson = $indexJson.Replace("`r`n", "`n") + "`n"
[IO.File]::WriteAllText($OutputPath, $indexJson, [Text.UTF8Encoding]::new($false))

& (Join-Path $PSScriptRoot 'Test-RepoIndex.ps1') `
    -RepoIndexPath $OutputPath `
    -PackagePath $resolvedPackagePath `
    -ExpectedVersion $Version

Write-Output "Repository index created: $OutputPath"
Write-Output "Repository index SHA-256: $((Get-FileHash -Algorithm SHA256 -LiteralPath $OutputPath).Hash)"
