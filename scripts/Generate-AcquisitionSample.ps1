[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\CrystariumBoutique.AcquisitionGenerator.csproj'
$sampleRoot = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\sample-data'
$outputRoot = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\sample-output'
$supplement = Join-Path $repositoryRoot 'src\CrystariumBoutique\acquisition-data\item-acquisition-supplement.json'

dotnet run --project $project --configuration Release -- generate `
    --tracky (Join-Path $sampleRoot 'tracky-chest-drops-v2.sample.json') `
    --source (Join-Path $sampleRoot 'tracky-source.sample.json') `
    --duty-metadata (Join-Path $sampleRoot 'duty-metadata.sample.json') `
    --game-path $GamePath `
    --output $supplement `
    --manifest (Join-Path $outputRoot 'generation-manifest.json') `
    --review (Join-Path $outputRoot 'generation-review.json')

if ($LASTEXITCODE -ne 0) {
    throw "The acquisition sample generator exited with code $LASTEXITCODE."
}
