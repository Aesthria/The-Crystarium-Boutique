[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "CrystariumBoutique-layout-test-$([Guid]::NewGuid().ToString('N'))"
$flatRoot = Join-Path $testRoot 'flat'
$nestedRoot = Join-Path $testRoot 'nested'
$flatZip = Join-Path $testRoot 'flat.zip'
$nestedZip = Join-Path $testRoot 'nested.zip'

try {
    New-Item -ItemType Directory -Path $flatRoot -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $nestedRoot 'CrystariumBoutique') -Force | Out-Null

    foreach ($root in @($flatRoot, (Join-Path $nestedRoot 'CrystariumBoutique'))) {
        [IO.File]::WriteAllText(
            (Join-Path $root 'CrystariumBoutique.dll'),
            'layout-test',
            [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllText(
            (Join-Path $root 'CrystariumBoutique.json'),
            '{}',
            [Text.UTF8Encoding]::new($false))
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($flatRoot, $flatZip)
    [IO.Compression.ZipFile]::CreateFromDirectory($nestedRoot, $nestedZip)

    & (Join-Path $PSScriptRoot 'Assert-ReleasePackageLayout.ps1') -PackagePath $flatZip

    $nestedRejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Assert-ReleasePackageLayout.ps1') -PackagePath $nestedZip
    }
    catch {
        if ($_.Exception.Message -notlike "*extra top-level 'CrystariumBoutique/' directory*") {
            throw
        }
        $nestedRejected = $true
    }

    if (-not $nestedRejected) {
        throw 'Regression: a release package with an extra top-level directory was accepted.'
    }
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

Write-Output 'Release package layout regression passed: flat root accepted; extra top-level directory rejected.'
