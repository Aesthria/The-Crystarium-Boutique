[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$RepoOutputPath,

    [string]$Changelog,

    [switch]$NoRestore,

    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($RepoOutputPath)) {
    $RepoOutputPath = Join-Path $repositoryRoot 'repo.json'
}

$packageArguments = @{
    Version = $Version
    SourceRoot = $repositoryRoot
}
if ($NoRestore) {
    $packageArguments.NoRestore = $true
}
if ($AllowDirty) {
    $packageArguments.AllowDirty = $true
}

& (Join-Path $PSScriptRoot 'Package-Release.ps1') @packageArguments
$artifactKind = if ($Version.Contains('-rc.', [StringComparison]::Ordinal)) {
    'release-dry-run'
}
else {
    'release'
}
$packagePath = Join-Path $repositoryRoot "artifacts\$artifactKind\CrystariumBoutique-$Version.zip"
$firstHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagePath).Hash

$repeatArguments = @{
    Version = $Version
    SourceRoot = $repositoryRoot
    SkipBuildAndTests = $true
}
if ($AllowDirty) {
    $repeatArguments.AllowDirty = $true
}
& (Join-Path $PSScriptRoot 'Package-Release.ps1') @repeatArguments
$secondHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagePath).Hash
if ($firstHash -ne $secondHash) {
    throw "Release package is not deterministic: '$firstHash' != '$secondHash'."
}

$repoArguments = @{
    Version = $Version
    PackagePath = $packagePath
    OutputPath = $RepoOutputPath
}
if (-not [string]::IsNullOrWhiteSpace($Changelog)) {
    $repoArguments.Changelog = $Changelog
}
& (Join-Path $PSScriptRoot 'Generate-RepoIndex.ps1') @repoArguments

& (Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1') `
    -PackagePath $packagePath `
    -ExpectedVersion $Version
& (Join-Path $PSScriptRoot 'Test-RepoIndex.ps1') `
    -RepoIndexPath $RepoOutputPath `
    -PackagePath $packagePath `
    -ExpectedVersion $Version

Write-Output "Release dry run passed for $Version."
Write-Output "Package: $packagePath"
Write-Output "Package SHA-256: $secondHash"
Write-Output "Repository index: $([IO.Path]::GetFullPath($RepoOutputPath))"
