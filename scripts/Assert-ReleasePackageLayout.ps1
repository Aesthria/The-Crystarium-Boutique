[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [string]$InternalName = 'CrystariumBoutique'
)

$ErrorActionPreference = 'Stop'
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).Path

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $rootAssembly = "$InternalName.dll"
    $rootManifest = "$InternalName.json"
    $nestedAssembly = "$InternalName/$rootAssembly"
    $nestedManifest = "$InternalName/$rootManifest"

    if ($null -ne $archive.GetEntry($nestedAssembly) -or
        $null -ne $archive.GetEntry($nestedManifest)) {
        throw "Release package has an extra top-level '$InternalName/' directory."
    }

    $assemblyEntry = $archive.GetEntry($rootAssembly)
    $manifestEntry = $archive.GetEntry($rootManifest)
    if ($null -eq $assemblyEntry -or $null -eq $manifestEntry) {
        throw "Release package must contain '$rootAssembly' and '$rootManifest' at ZIP root."
    }

    if (-not [string]::IsNullOrEmpty([IO.Path]::GetDirectoryName($assemblyEntry.FullName)) -or
        -not [string]::IsNullOrEmpty([IO.Path]::GetDirectoryName($manifestEntry.FullName))) {
        throw 'Main plugin DLL and manifest must be siblings at ZIP root.'
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Release package root layout passed for '$InternalName'."
