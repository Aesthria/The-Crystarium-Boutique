[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [switch]$NoRestore,

    [switch]$SkipStage,

    [string]$Version,

    [string]$AssemblyVersion
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$solutionPath = Join-Path $repositoryRoot 'CrystariumBoutique.sln'

if ([string]::IsNullOrWhiteSpace($env:DOTNET_CLI_HOME)) {
    $env:DOTNET_CLI_HOME = Join-Path $repositoryRoot '.dotnet-cli-home'
}

if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    $env:NUGET_PACKAGES = Join-Path $repositoryRoot '.packages'
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location $repositoryRoot
try {
    if (-not $NoRestore) {
        dotnet restore $solutionPath --locked-mode
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }
    }

    $buildArguments = @(
        'build',
        $solutionPath,
        '--configuration',
        $Configuration,
        '--property:Platform=x64',
        '--no-restore'
    )
    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        $buildArguments += "--property:Version=$Version"
        $buildArguments += "--property:InformationalVersion=$Version"
    }

    if (-not [string]::IsNullOrWhiteSpace($AssemblyVersion)) {
        $buildArguments += "--property:AssemblyVersion=$AssemblyVersion"
        $buildArguments += "--property:FileVersion=$AssemblyVersion"
    }

    dotnet @buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }

    $pluginOutputDirectory = Join-Path $repositoryRoot "src\CrystariumBoutique\bin\x64\$Configuration"
    & (Join-Path $PSScriptRoot 'Test-RuntimeDependencies.ps1') `
        -AssemblyPath (Join-Path $pluginOutputDirectory 'CrystariumBoutique.dll') `
        -DependencyManifestPath (Join-Path $pluginOutputDirectory 'CrystariumBoutique.deps.json')

    foreach ($testProject in @(
        @{
            Path = 'tests\CrystariumBoutique.Core.Tests\CrystariumBoutique.Core.Tests.csproj'
            Platform = $null
        },
        @{
            Path = 'tests\CrystariumBoutique.Plugin.Tests\CrystariumBoutique.Plugin.Tests.csproj'
            Platform = 'x64'
        }
    )) {
        $testArguments = @(
            'test',
            (Join-Path $repositoryRoot $testProject.Path),
            '--configuration',
            $Configuration,
            '--no-build',
            '--no-restore'
        )
        if ($testProject.Platform) {
            $testArguments += "--property:Platform=$($testProject.Platform)"
        }

        dotnet @testArguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed for '$($testProject.Path)' with exit code $LASTEXITCODE."
        }
    }

    if (-not $SkipStage) {
        & (Join-Path $PSScriptRoot 'Stage-DevBuild.ps1') -Configuration $Configuration
    }
}
finally {
    Pop-Location
}
