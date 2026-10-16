[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [switch] $SkipNative,
    [switch] $SkipDocs,
    # The WindowsUiTests need an interactive desktop (tray menu foreground).
    [switch] $SkipUi,
    # Overrides the C++ platform toolset of the native projects (the projects
    # name v145; a build machine with an older Visual Studio passes e.g. v143).
    [string] $PlatformToolset = ''
)

# Builds every OmsiLaunch project (managed solution, the projects outside the
# solution, the native projects and the native test module) and runs every
# offline test suite.
# It never launches OMSI and never touches an OMSI installation.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failures = New-Object System.Collections.Generic.List[string]
$toolset = if ($PlatformToolset) { @("-p:PlatformToolset=$PlatformToolset") } else { @() }

function Invoke-Step([string] $Name, [scriptblock] $Action) {
    Write-Host "== $Name"
    try { & $Action; if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) { throw "exit code $LASTEXITCODE" } }
    catch { $failures.Add("$Name : $_"); Write-Host "FAILED $Name : $_" -ForegroundColor Red }
}

Invoke-Step 'dotnet build OmsiLaunch.sln' { & dotnet build (Join-Path $root 'OmsiLaunch.sln') -c $Configuration --nologo -v q -p:TreatWarningsAsErrors=true | Where-Object { $_ -notmatch 'NETSDK1138' } }
# The x86 projects (plugin, plugin integration tests) are built explicitly:
# the solution configuration does not refresh their x86 outputs.
Invoke-Step 'dotnet build IntegrationTests (x86)' { & dotnet build (Join-Path $root 'tests\OmsiLaunch.IntegrationTests\OmsiLaunch.IntegrationTests.csproj') -c $Configuration --nologo -v q -p:TreatWarningsAsErrors=true | Where-Object { $_ -notmatch 'NETSDK1138' } }

if (-not $SkipNative) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) { $failures.Add('MSBuild (VC++ toolset) was not found; native projects were not built.') }
    else {
        Invoke-Step 'Native.x86 (Win32)' { & $msbuild (Join-Path $root 'src\OmsiLaunch.Native.x86\OmsiLaunch.Native.x86.vcxproj') -p:Configuration=$Configuration -p:Platform=Win32 -nologo -v:q @toolset }
        Invoke-Step 'Bootstrapper (x64)' { & $msbuild (Join-Path $root 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.vcxproj') -p:Configuration=$Configuration -p:Platform=x64 -nologo -v:q @toolset }
        Invoke-Step 'WindowsHost (x64)' { & $msbuild (Join-Path $root 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.vcxproj') -p:Configuration=$Configuration -p:Platform=x64 -nologo -v:q @toolset }
        # Native module component used by the plugin integration tests (never packaged).
        Invoke-Step 'Module.NativeTest (Win32)' { & $msbuild (Join-Path $root 'tests\OmsiLaunch.Module.NativeTest\OmsiLaunch.Module.NativeTest.vcxproj') -p:Configuration=Release -p:Platform=Win32 -nologo -v:q @toolset }
    }
}

function Invoke-Suite([string] $Project) {
    $bin = Join-Path $root "artifacts\bin\$Project"
    $exe = Get-ChildItem -LiteralPath $bin -Recurse -Filter "$Project.exe" | Where-Object { $_.FullName -match "\\$Configuration\\" } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $exe) { throw "no $Configuration build of $Project" }
    $output = & $exe.FullName 2>&1
    $output | ForEach-Object { if ("$_" -match '^(PASS|FAIL)') { Write-Host "  $_" } }
    if ($LASTEXITCODE -ne 0) { throw "$Project reported failures" }
}

$suites = @('OmsiLaunch.TestHost', 'OmsiLaunch.UnitTests', 'OmsiLaunch.IntegrationTests', 'OmsiLaunch.ProfileTests', 'OmsiLaunch.ModuleTests')
if (-not $SkipUi) { $suites += 'OmsiLaunch.WindowsUiTests' }
foreach ($suite in $suites) {
    Invoke-Step $suite { Invoke-Suite $suite }
}
if (-not $SkipDocs) { Invoke-Step 'OmsiLaunch.DocumentationTests' { Invoke-Suite 'OmsiLaunch.DocumentationTests' } }
# Packaging regression: candidate package under artifacts\candidate (never
# artifacts\release), integrity self-check and negative cases. Requires the
# native builds above.
if (-not $SkipNative) { Invoke-Step 'Packaging pipeline' { & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-PackagingPipeline.ps1') } }

if ($failures.Count -ne 0) { Write-Host ''; Write-Host 'OFFLINE VALIDATION FAILED' -ForegroundColor Red; $failures | ForEach-Object { Write-Host " - $_" }; exit 1 }
Write-Host ''; Write-Host 'OFFLINE VALIDATION PASSED' -ForegroundColor Green
