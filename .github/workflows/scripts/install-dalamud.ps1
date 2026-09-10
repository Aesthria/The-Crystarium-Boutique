$ErrorActionPreference = 'Stop'

$archivePath = Join-Path $env:RUNNER_TEMP 'dalamud-api15.zip'
$dalamudHome = Join-Path $env:RUNNER_TEMP 'dalamud-api15'
Invoke-WebRequest -UseBasicParsing -Uri $env:DALAMUD_BUNDLE_URL -OutFile $archivePath

$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
if ($actualHash -ne $env:DALAMUD_BUNDLE_SHA256) {
    throw "Dalamud bundle SHA-256 '$actualHash' does not match the reviewed hash '$env:DALAMUD_BUNDLE_SHA256'."
}

Expand-Archive -LiteralPath $archivePath -DestinationPath $dalamudHome
$requiredFiles = @(
    'Dalamud.dll',
    'Dalamud.Bindings.ImGui.dll',
    'Lumina.dll',
    'Lumina.Excel.dll',
    'Serilog.dll',
    'BCnEncoder.dll',
    'CommunityToolkit.HighPerformance.dll',
    'Microsoft.Extensions.ObjectPool.dll'
)
$missingFiles = @(
    $requiredFiles |
        Where-Object { -not (Test-Path -LiteralPath (Join-Path $dalamudHome $_) -PathType Leaf) }
)
if ($missingFiles.Count -gt 0) {
    throw "The pinned Dalamud bundle is missing required files: $($missingFiles -join ', ')."
}

$actualDalamudVersion = [Reflection.AssemblyName]::GetAssemblyName(
    (Join-Path $dalamudHome 'Dalamud.dll')).Version.ToString()
if ($actualDalamudVersion -ne $env:DALAMUD_ASSEMBLY_VERSION) {
    throw "Dalamud assembly version '$actualDalamudVersion' does not match '$env:DALAMUD_ASSEMBLY_VERSION'."
}

"DALAMUD_HOME=$dalamudHome" | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
