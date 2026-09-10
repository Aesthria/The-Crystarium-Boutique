[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$AssemblyPath,

    [Parameter(Mandatory)]
    [string]$DependencyManifestPath
)

$ErrorActionPreference = 'Stop'
$forbiddenDependencies = @(
    'Glamourer.Api'
    'Penumbra.Api'
    'Luna'
    'Microsoft.Extensions.DependencyInjection.Abstractions'
    'Microsoft.Extensions.DependencyInjection'
    'Microsoft.Extensions.Logging.Abstractions'
)

$resolvedAssemblyPath = (Resolve-Path -LiteralPath $AssemblyPath).Path
$resolvedDependencyManifestPath = (Resolve-Path -LiteralPath $DependencyManifestPath).Path
$assemblyReferences = @(
    [Reflection.Assembly]::LoadFile($resolvedAssemblyPath).GetReferencedAssemblies() |
        ForEach-Object Name
)
$forbiddenAssemblyReferences = @(
    $forbiddenDependencies | Where-Object { $_ -in $assemblyReferences }
)
if ($forbiddenAssemblyReferences.Count -gt 0) {
    throw "Boutique assembly references forbidden runtime dependencies: $($forbiddenAssemblyReferences -join ', ')"
}

$dependencyManifest = Get-Content -Raw -LiteralPath $resolvedDependencyManifestPath | ConvertFrom-Json
$dependencyLibraries = @($dependencyManifest.libraries.psobject.Properties.Name)
$forbiddenManifestLibraries = @(
    $forbiddenDependencies |
        Where-Object {
            $dependencyName = $_
            $dependencyLibraries | Where-Object {
                $_ -eq $dependencyName -or $_.StartsWith("$dependencyName/", [StringComparison]::OrdinalIgnoreCase)
            }
        }
)
if ($forbiddenManifestLibraries.Count -gt 0) {
    throw "Boutique dependency manifest contains forbidden runtime dependencies: $($forbiddenManifestLibraries -join ', ')"
}

Write-Output "Runtime dependency validation passed: $($assemblyReferences.Count) assembly references and $($dependencyLibraries.Count) dependency-manifest libraries inspected."
