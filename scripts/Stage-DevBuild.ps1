[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourceDirectory = Join-Path $repositoryRoot "src\CrystariumBoutique\bin\x64\$Configuration"
$artifactRoot = Join-Path $repositoryRoot 'artifacts'
$destinationDirectory = Join-Path $artifactRoot 'dev\CrystariumBoutique'
$packageDefinition = Import-PowerShellDataFile `
    -LiteralPath (Join-Path $PSScriptRoot 'RuntimePackageFiles.psd1')

if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
    throw "Build output does not exist: $sourceDirectory"
}

$resolvedArtifactRoot = [IO.Path]::GetFullPath($artifactRoot)
$resolvedDestination = [IO.Path]::GetFullPath($destinationDirectory)
if (-not $resolvedDestination.StartsWith(
        $resolvedArtifactRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to stage outside the repository artifact directory: $resolvedDestination"
}

if (Test-Path -LiteralPath $destinationDirectory) {
    Remove-Item -LiteralPath $destinationDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null

foreach ($artifactName in $packageDefinition.RootFiles) {
    $sourcePath = Join-Path $sourceDirectory $artifactName
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Required development artifact is missing: $sourcePath"
    }

    Copy-Item -LiteralPath $sourcePath -Destination $destinationDirectory -Force
}

$imageDestinationDirectory = Join-Path $destinationDirectory 'images'
New-Item -ItemType Directory -Path $imageDestinationDirectory -Force | Out-Null
foreach ($imageName in $packageDefinition.ImageFiles) {
    $imageSourcePath = Join-Path $sourceDirectory "images\$imageName"
    if (-not (Test-Path -LiteralPath $imageSourcePath -PathType Leaf)) {
        throw "Required development image is missing: $imageSourcePath"
    }

    Copy-Item -LiteralPath $imageSourcePath -Destination $imageDestinationDirectory -Force
}

$dataDestinationDirectory = Join-Path $destinationDirectory 'data'
New-Item -ItemType Directory -Path $dataDestinationDirectory -Force | Out-Null
foreach ($dataName in $packageDefinition.DataFiles) {
    $dataSourcePath = Join-Path $sourceDirectory "data\$dataName"
    if (-not (Test-Path -LiteralPath $dataSourcePath -PathType Leaf)) {
        throw "Required development data file is missing: $dataSourcePath"
    }

    Copy-Item -LiteralPath $dataSourcePath -Destination $dataDestinationDirectory -Force
}

Write-Output "Development build staged at $destinationDirectory"
Write-Output "Dalamud Dev Plugin Location DLL: $(Join-Path $destinationDirectory 'CrystariumBoutique.dll')"
