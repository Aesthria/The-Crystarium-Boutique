[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\CrystariumBoutique.AcquisitionGenerator.csproj'
$dataRoot = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\supplemental-data\LuminaSupplemental-5.1.4'
$outputRoot = Join-Path $repositoryRoot 'tools\CrystariumBoutique.AcquisitionGenerator\supplemental-output'
$supplement = Join-Path $repositoryRoot 'src\CrystariumBoutique\acquisition-data\item-acquisition-supplement.json'

dotnet run --project $project --configuration Release --no-build -- generate-lumina-supplemental `
    --supplemental-data $dataRoot `
    --source (Join-Path $dataRoot 'source.json') `
    --game-path $GamePath `
    --output $supplement `
    --manifest (Join-Path $outputRoot 'generation-manifest.json') `
    --review (Join-Path $outputRoot 'generation-review.json')

if ($LASTEXITCODE -ne 0) {
    throw "The acquisition supplement generator exited with code $LASTEXITCODE."
}
