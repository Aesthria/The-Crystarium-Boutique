[CmdletBinding()]
param(
    [string]$Version,

    [switch]$NoRestore,

    [switch]$SkipBuildAndTests,

    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repositoryRoot 'src\CrystariumBoutique\CrystariumBoutique.csproj'
$packageDefinitionPath = Join-Path $PSScriptRoot 'RuntimePackageFiles.psd1'
$packageDefinition = Import-PowerShellDataFile -LiteralPath $packageDefinitionPath

[xml]$project = Get-Content -Raw -LiteralPath $projectPath
$projectVersion = [string]$project.Project.PropertyGroup.Version
$assemblyVersion = [string]$project.Project.PropertyGroup.AssemblyVersion
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $projectVersion
}

if ($Version -ne $projectVersion) {
    throw "Requested package version '$Version' does not match project version '$projectVersion'."
}

$versionMatch = [regex]::Match(
    $Version,
    '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)-beta\.(?<beta>[1-9]\d*)$')
if (-not $versionMatch.Success) {
    throw "Private beta versions must use major.minor.patch-beta.N: '$Version'."
}

$expectedAssemblyVersion = '{0}.{1}.{2}.{3}' -f `
    $versionMatch.Groups['major'].Value,
    $versionMatch.Groups['minor'].Value,
    $versionMatch.Groups['patch'].Value,
    $versionMatch.Groups['beta'].Value
if ($assemblyVersion -ne $expectedAssemblyVersion) {
    throw "AssemblyVersion '$assemblyVersion' must match private-beta mapping '$expectedAssemblyVersion'."
}

$statusLines = @(git -c "safe.directory=$repositoryRoot" -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the Git working tree.'
}

$sourceDirty = $statusLines.Count -gt 0
if ($sourceDirty -and -not $AllowDirty) {
    throw 'The working tree is dirty. Commit reviewed files first, or use -AllowDirty only for local verification.'
}

if (-not $SkipBuildAndTests) {
    $buildArguments = @{
        Configuration = 'Release'
        SkipStage = $true
    }
    if ($NoRestore) {
        $buildArguments.NoRestore = $true
    }

    & (Join-Path $PSScriptRoot 'Build.ps1') @buildArguments
}

$releaseDirectory = Join-Path $repositoryRoot 'src\CrystariumBoutique\bin\x64\Release'
if (-not (Test-Path -LiteralPath $releaseDirectory -PathType Container)) {
    throw "Release build output does not exist: $releaseDirectory"
}

$pluginManifestPath = Join-Path $releaseDirectory 'CrystariumBoutique.json'
$pluginAssemblyPath = Join-Path $releaseDirectory 'CrystariumBoutique.dll'
$pluginManifest = Get-Content -Raw -LiteralPath $pluginManifestPath | ConvertFrom-Json
if ([string]$pluginManifest.AssemblyVersion -ne $assemblyVersion) {
    throw "Generated Dalamud manifest version '$($pluginManifest.AssemblyVersion)' does not match '$assemblyVersion'."
}

$builtAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginAssemblyPath).Version.ToString()
if ($builtAssemblyVersion -ne $assemblyVersion) {
    throw "Built assembly version '$builtAssemblyVersion' does not match '$assemblyVersion'."
}

$versionInfo = (Get-Item -LiteralPath $pluginAssemblyPath).VersionInfo
if ($versionInfo.FileVersion -ne $assemblyVersion) {
    throw "Built file version '$($versionInfo.FileVersion)' does not match '$assemblyVersion'."
}

if (-not $versionInfo.ProductVersion.StartsWith($Version, [StringComparison]::Ordinal)) {
    throw "Built product version '$($versionInfo.ProductVersion)' does not begin with '$Version'."
}

$artifactRoot = Join-Path $repositoryRoot 'artifacts\private-beta'
$workingRoot = Join-Path $artifactRoot 'work'
$versionWorkingRoot = Join-Path $workingRoot $Version
$packageRoot = Join-Path $versionWorkingRoot 'CrystariumBoutique'
$zipPath = Join-Path $artifactRoot "CrystariumBoutique-$Version.zip"

$resolvedArtifactRoot = [IO.Path]::GetFullPath($artifactRoot)
$resolvedVersionWorkingRoot = [IO.Path]::GetFullPath($versionWorkingRoot)
if (-not $resolvedVersionWorkingRoot.StartsWith(
        $resolvedArtifactRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to package outside the repository artifact directory: $resolvedVersionWorkingRoot"
}

if (Test-Path -LiteralPath $versionWorkingRoot) {
    Remove-Item -LiteralPath $versionWorkingRoot -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

function Copy-RequiredFile {
    param(
        [Parameter(Mandatory)]
        [string]$Source,

        [Parameter(Mandatory)]
        [string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required package file is missing: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not [string]::IsNullOrWhiteSpace($destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

foreach ($fileName in $packageDefinition.RootFiles) {
    Copy-RequiredFile `
        -Source (Join-Path $releaseDirectory $fileName) `
        -Destination (Join-Path $packageRoot $fileName)
}

foreach ($fileName in $packageDefinition.ImageFiles) {
    Copy-RequiredFile `
        -Source (Join-Path $releaseDirectory "images\$fileName") `
        -Destination (Join-Path $packageRoot "images\$fileName")
}

foreach ($fileName in $packageDefinition.DataFiles) {
    Copy-RequiredFile `
        -Source (Join-Path $releaseDirectory "data\$fileName") `
        -Destination (Join-Path $packageRoot "data\$fileName")
}

foreach ($distributionFile in $packageDefinition.DistributionFiles) {
    Copy-RequiredFile `
        -Source (Join-Path $repositoryRoot $distributionFile.Source) `
        -Destination (Join-Path $packageRoot $distributionFile.Destination)
}

$sourceCommit = (git -c "safe.directory=$repositoryRoot" -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the source Git commit.'
}

if (-not $versionInfo.ProductVersion.Contains($sourceCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Built product version '$($versionInfo.ProductVersion)' does not identify source commit '$sourceCommit'. Rebuild the checked-out commit before packaging."
}

$sourceCommitTimestamp = (git -c "safe.directory=$repositoryRoot" -C $repositoryRoot show -s --format=%cI HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the source Git commit timestamp.'
}

$sourceRepository = 'https://github.com/Aesthria/The-Crystarium-Boutique'
$buildIdentity = "$Version@$sourceCommit"
$dotnetSdkVersion = (dotnet --version).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($dotnetSdkVersion)) {
    throw 'Unable to resolve the .NET SDK build version.'
}

$pluginAssemblySha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $pluginAssemblyPath).Hash.ToLowerInvariant()
$dalamudReference = [Reflection.Assembly]::LoadFile($pluginAssemblyPath).GetReferencedAssemblies() |
    Where-Object Name -EQ 'Dalamud' |
    Select-Object -First 1
if ($null -eq $dalamudReference) {
    throw 'Built plugin assembly does not reference Dalamud.'
}

$manifestFiles = @(
    Get-ChildItem -LiteralPath $packageRoot -File -Recurse |
        ForEach-Object {
            $relativePath = [IO.Path]::GetRelativePath($packageRoot, $_.FullName).Replace('\', '/')
            [pscustomobject][ordered]@{
                path = $relativePath
                sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant()
                length = $_.Length
            }
        } |
        Sort-Object path
)

$packageManifest = [pscustomobject][ordered]@{
    schemaVersion = 2
    packageVersion = $Version
    assemblyVersion = $assemblyVersion
    buildIdentity = $buildIdentity
    buildConfiguration = 'Release'
    buildPlatform = 'x64'
    dotnetSdkVersion = $dotnetSdkVersion
    dalamudAssemblyVersion = $dalamudReference.Version.ToString()
    pluginAssemblySha256 = $pluginAssemblySha256
    pluginProductVersion = $versionInfo.ProductVersion
    sourceRepository = $sourceRepository
    sourceCommit = $sourceCommit
    sourceCommitTimestampUtc = ([DateTimeOffset]::Parse($sourceCommitTimestamp)).ToUniversalTime().ToString('O')
    sourceDirty = $sourceDirty
    archiveTimestampUtc = '2000-01-01T00:00:00.0000000+00:00'
    files = $manifestFiles
}

$manifestPath = Join-Path $packageRoot 'PACKAGE-MANIFEST.json'
$packageManifest |
    ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM

Add-Type -AssemblyName System.IO.Compression
$zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $archive = [IO.Compression.ZipArchive]::new(
        $zipStream,
        [IO.Compression.ZipArchiveMode]::Create,
        $false)
    try {
        $archiveTimestamp = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
        $archiveFiles = @(
            Get-ChildItem -LiteralPath $versionWorkingRoot -File -Recurse |
                Sort-Object { [IO.Path]::GetRelativePath($versionWorkingRoot, $_.FullName) }
        )
        foreach ($file in $archiveFiles) {
            $entryName = [IO.Path]::GetRelativePath($versionWorkingRoot, $file.FullName).Replace('\', '/')
            $entry = $archive.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = $archiveTimestamp
            $inputStream = [IO.File]::OpenRead($file.FullName)
            try {
                $entryStream = $entry.Open()
                try {
                    $inputStream.CopyTo($entryStream)
                }
                finally {
                    $entryStream.Dispose()
                }
            }
            finally {
                $inputStream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $zipStream.Dispose()
}

& (Join-Path $PSScriptRoot 'Test-PrivateBetaPackage.ps1') `
    -PackagePath $zipPath `
    -ExpectedVersion $Version

Remove-Item -LiteralPath $versionWorkingRoot -Recurse -Force

if ($env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "package_path=$zipPath"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "package_name=CrystariumBoutique-$Version"
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "version=$Version"
}

Write-Output "Private beta package created: $zipPath"
