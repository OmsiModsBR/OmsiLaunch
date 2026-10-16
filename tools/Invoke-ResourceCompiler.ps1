[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SourcePath,
    [Parameter(Mandatory)] [string] $OutputPath,
    [Parameter(Mandatory)] [string] $IncludeDirectory
)

# Runs the Windows SDK resource compiler (x86) on a .rc file. The plugin's
# native export wrapper links the resulting .res. The newest installed Windows
# SDK is used, so the build does not depend on one machine's SDK version.
$ErrorActionPreference = 'Stop'
$bin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$rc = Get-ChildItem -LiteralPath $bin -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version] $_.Name } -Descending |
    ForEach-Object { Join-Path $_.FullName 'x86\rc.exe' } |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $rc) { Write-Error "rc.exe (Windows SDK, x86) was not found under '$bin'. Install the Windows SDK."; exit 2 }

& $rc /nologo /I $IncludeDirectory /fo $OutputPath $SourcePath
exit $LASTEXITCODE
