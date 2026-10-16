[CmdletBinding()]
param(
    # Directory in which the candidate package is produced. It must be below
    # artifacts\ and must not be artifacts\release (published archives).
    [string] $OutputDirectory = ''
)

# Offline packaging regression (correction pass after Round A, RA-007).
#  1. Produces a candidate package from clean staging and requires its staged
#     closure and its re-extracted archive to validate against the manifest.
#  2. Proves the integrity gate blocks a tampered DLL, an old Native.x86 copy,
#     a stale plugin copy, a deleted listed file, an unexpected file, a manifest
#     hash change, duplicate entries (exact, by case, by separator) and invalid
#     paths.
#  3. Proves the packager refuses a Native.x86 that is not the recorded build
#     output (old copy) and sources changed after the recorded build.
#  4. Proves the published archive is never overwritten implicitly.
# It never launches OMSI and never touches an OMSI installation.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $root 'artifacts\candidate\post-round-a' }
$artifacts = (Join-Path $root 'artifacts') + '\'
$full = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $full.StartsWith($artifacts, [StringComparison]::OrdinalIgnoreCase) -or $full.StartsWith((Join-Path $root 'artifacts\release'), [StringComparison]::OrdinalIgnoreCase)) {
    throw "The candidate output must be below artifacts\ and outside artifacts\release: $full"
}
$failures = New-Object System.Collections.Generic.List[string]
$integrity = Join-Path $PSScriptRoot 'Test-ReleasePackageIntegrity.ps1'
$packager = Join-Path $PSScriptRoot 'New-ReleasePackage.ps1'
# Runs a child PowerShell and captures its output and exit code. In Windows
# PowerShell 5.1, redirected stderr of a native command becomes a terminating
# error under ErrorActionPreference=Stop, so expected refusals are captured
# with Continue.
function Invoke-Child([string[]] $Arguments) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $text = & powershell -NoProfile -ExecutionPolicy Bypass @Arguments 2>&1 | Out-String; return @{ ExitCode = $LASTEXITCODE; Output = $text } }
    finally { $ErrorActionPreference = $previous }
}
function Invoke-Integrity([string] $Path) { return (Invoke-Child @('-File', $integrity, '-PackagePath', $Path)).ExitCode }
function Pass([string] $Name) { Write-Host "PASS packaging.$Name" }

# 1. Candidate package from clean staging.
$result = Invoke-Child @('-File', $packager, '-Configuration', 'Release', '-OutputDirectory', $full)
if ($result.ExitCode -ne 0) { throw "New-ReleasePackage.ps1 failed for the candidate package:`n$($result.Output)" }
$stage = Join-Path $full 'OmsiLaunch-current'
if ((Invoke-Integrity $stage) -ne 0) { $failures.Add('candidate staged closure failed integrity') }
if ((Invoke-Integrity (Join-Path $full 'OmsiLaunch-current.zip')) -ne 0) { $failures.Add('candidate archive (re-extracted) failed integrity') }
if ([IO.File]::ReadAllBytes((Join-Path $stage 'release-manifest.json'))[0] -eq 0xEF) { $failures.Add('candidate manifest was written with a BOM') }
if (@(Get-ChildItem -LiteralPath $full -Force | Where-Object { $_.Name -like '.staging-*' }).Count -ne 0) { $failures.Add('staging directory was left behind') }
$receipt = Join-Path $root 'artifacts\x86\Release\OmsiLaunch.Native.x86.build-receipt.txt'
$receiptHash = ((Get-Content -LiteralPath $receipt | Where-Object { $_ -like 'output=*' } | Select-Object -First 1).Substring(7)).ToUpperInvariant()
if ((Get-FileHash -LiteralPath (Join-Path $stage 'plugins\OmsiLaunch.Native.x86.dll')).Hash -ne $receiptHash) { $failures.Add('packaged Native.x86 is not the recorded build output') }
if ($failures.Count -eq 0) { Pass 'candidate-coherent-from-clean-staging' }

# 2. Integrity gate negative cases, each on a fresh copy of the staged closure.
# An older Native.x86 build: the one of a previous release when this machine has one,
# otherwise a stand-in (the candidate's own native with one byte changed), so the test
# also runs on a clean clone and in CI.
$oldNative = Join-Path $root 'artifacts\release\OmsiLaunch-current\plugins\OmsiLaunch.Native.x86.dll'
if (-not (Test-Path -LiteralPath $oldNative)) {
    $oldNative = Join-Path ([IO.Path]::GetTempPath()) ('omsilaunch-old-native-' + [Guid]::NewGuid().ToString('N') + '.dll')
    $standIn = [IO.File]::ReadAllBytes((Join-Path $stage 'plugins\OmsiLaunch.Native.x86.dll')); $standIn[200] = $standIn[200] -bxor 1
    [IO.File]::WriteAllBytes($oldNative, $standIn)
}
function Edit-Manifest([string] $Path, [scriptblock] $Change) {
    $manifestPath = Join-Path $Path 'release-manifest.json'
    $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    & $Change $manifest
    [IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
}
function Copy-Entry($Manifest, [string] $Path, [string] $NewPath) { $entry = $Manifest.files | Where-Object { $_.path -eq $Path } | Select-Object -First 1; $Manifest.files += [pscustomobject]@{ path = $NewPath; bytes = $entry.bytes; sha256 = $entry.sha256 } }
$cases = @(
    @{ Name = 'tampered-dll'; Mutate = { param($p) $b = [IO.File]::ReadAllBytes("$p\OmsiLaunch.Core.dll"); $b[$b.Length - 1] = $b[$b.Length - 1] -bxor 1; [IO.File]::WriteAllBytes("$p\OmsiLaunch.Core.dll", $b) } },
    @{ Name = 'tampered-native'; Mutate = { param($p) $b = [IO.File]::ReadAllBytes("$p\plugins\OmsiLaunch.Native.x86.dll"); $b[100] = $b[100] -bxor 1; [IO.File]::WriteAllBytes("$p\plugins\OmsiLaunch.Native.x86.dll", $b) } },
    @{ Name = 'old-native-copy'; Mutate = { param($p) Copy-Item -LiteralPath $oldNative -Destination "$p\plugins\OmsiLaunch.Native.x86.dll" -Force } },
    @{ Name = 'stale-plugin-copy'; Mutate = { param($p) Copy-Item -LiteralPath "$p\plugins\OmsiLaunch.Api.dll" -Destination "$p\plugins\OmsiLaunch.Interop.dll" -Force } },
    @{ Name = 'deleted-listed-file'; Mutate = { param($p) Remove-Item -LiteralPath "$p\OmsiLaunchW.exe" } },
    @{ Name = 'unexpected-file'; Mutate = { param($p) Set-Content -LiteralPath "$p\plugins\OmsiLaunch.Stale.dll" -Value 'stale' } },
    @{ Name = 'manifest-hash-changed'; Mutate = { param($p) Edit-Manifest $p { param($m) ($m.files | Where-Object { $_.path -eq 'plugins/OmsiLaunch.Plugin.dll' }).sha256 = ('0' * 64) } } },
    @{ Name = 'manifest-hash-malformed'; Mutate = { param($p) Edit-Manifest $p { param($m) ($m.files | Where-Object { $_.path -eq 'plugins/OmsiLaunch.Plugin.dll' }).sha256 = 'ABC' } } },
    @{ Name = 'manifest-duplicate-entry'; Mutate = { param($p) Edit-Manifest $p { param($m) Copy-Entry $m 'plugins/OmsiLaunch.Plugin.dll' 'plugins/OmsiLaunch.Plugin.dll' } } },
    @{ Name = 'manifest-duplicate-by-case'; Mutate = { param($p) Edit-Manifest $p { param($m) Copy-Entry $m 'plugins/OmsiLaunch.Plugin.dll' 'PLUGINS/omsilaunch.plugin.DLL' } } },
    @{ Name = 'manifest-duplicate-by-separator'; Mutate = { param($p) Edit-Manifest $p { param($m) Copy-Entry $m 'plugins/OmsiLaunch.Plugin.dll' 'plugins\OmsiLaunch.Plugin.dll' } } },
    @{ Name = 'manifest-parent-path'; Mutate = { param($p) Edit-Manifest $p { param($m) ($m.files | Where-Object { $_.path -eq 'LICENSE' }).path = '../LICENSE' } } },
    @{ Name = 'manifest-absolute-path'; Mutate = { param($p) Edit-Manifest $p { param($m) ($m.files | Where-Object { $_.path -eq 'LICENSE' }).path = 'C:/LICENSE' } } },
    @{ Name = 'manifest-invalid-json'; Mutate = { param($p) Set-Content -LiteralPath "$p\release-manifest.json" -Value '{ "files": [' } }
)
foreach ($case in $cases) {
    $copy = Join-Path ([IO.Path]::GetTempPath()) ('omsilaunch-negative-' + [Guid]::NewGuid().ToString('N'))
    try {
        Copy-Item -LiteralPath $stage -Destination $copy -Recurse
        & $case.Mutate $copy
        if ((Invoke-Integrity $copy) -eq 0) { $failures.Add("integrity accepted $($case.Name)") } else { Pass "rejects-$($case.Name)" }
    }
    finally { if (Test-Path -LiteralPath $copy) { Remove-Item -LiteralPath $copy -Recurse -Force } }
}

# 3. The packager refuses a Native.x86 that is not the recorded build output,
#    and a receipt whose sources no longer match. Build outputs are restored
#    byte for byte afterwards.
$native = Join-Path $root 'artifacts\x86\Release\OmsiLaunch.Native.x86.dll'
$nativeBackup = [IO.File]::ReadAllBytes($native); $nativeTime = (Get-Item -LiteralPath $native).LastWriteTimeUtc
$receiptBackup = [IO.File]::ReadAllBytes($receipt)
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('omsilaunch-stale-' + [Guid]::NewGuid().ToString('N'))
try {
    Copy-Item -LiteralPath $oldNative -Destination $native -Force
    # A fresh timestamp must not help: the check is by content, not by time.
    (Get-Item -LiteralPath $native).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(5)
    $result = Invoke-Child @('-File', $packager, '-Configuration', 'Release', '-OutputDirectory', $scratch)
    if ($result.ExitCode -eq 0 -or -not ($result.Output -match 'does not match its build receipt')) { $failures.Add('packager accepted an old Native.x86 copy') } else { Pass 'rejects-old-native-in-build-output' }
    [IO.File]::WriteAllBytes($native, $nativeBackup); (Get-Item -LiteralPath $native).LastWriteTimeUtc = $nativeTime

    $lines = [IO.File]::ReadAllLines($receipt)
    $lines = $lines | ForEach-Object { if ($_ -like 'source=*NativeBoundary.cpp') { 'source=' + ('0' * 64) + '|' + $_.Split('|', 2)[1] } else { $_ } }
    [IO.File]::WriteAllLines($receipt, $lines)
    $result = Invoke-Child @('-File', $packager, '-Configuration', 'Release', '-OutputDirectory', $scratch)
    if ($result.ExitCode -eq 0 -or -not ($result.Output -match 'Native source changed after the recorded build')) { $failures.Add('packager accepted a Native.x86 whose sources changed after the build') } else { Pass 'rejects-native-sources-changed' }
}
finally {
    [IO.File]::WriteAllBytes($native, $nativeBackup); (Get-Item -LiteralPath $native).LastWriteTimeUtc = $nativeTime
    [IO.File]::WriteAllBytes($receipt, $receiptBackup)
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
}
if ((Get-FileHash -LiteralPath $native).Hash -ne $receiptHash) { $failures.Add('Native.x86 build output was not restored') }

# 4. The published archive in artifacts\release is never overwritten implicitly.
[xml] $identity = Get-Content -LiteralPath (Join-Path $root 'OmsiLaunch.Version.props') -Raw
$version = ($identity.Project.PropertyGroup | Select-Object -First 1).OmsiLaunchProductVersion
$published = Get-Item -LiteralPath (Join-Path $root "artifacts\release\OmsiLaunch-$version.zip") -ErrorAction SilentlyContinue
if ($published) {
    $before = (Get-FileHash -LiteralPath $published.FullName).Hash
    $result = Invoke-Child @('-File', $packager, '-Configuration', 'Release')
    $refused = $result.ExitCode -ne 0 -and $result.Output -match 'Refusing to overwrite'
    if (-not $refused -or (Get-FileHash -LiteralPath $published.FullName).Hash -ne $before) { $failures.Add('packager overwrote a published archive without -AllowOverwritePublished') } else { Pass 'published-archive-protected' }
}

if ($failures.Count -ne 0) { $failures | ForEach-Object { Write-Host "FAIL $_" }; exit 1 }
Pass 'all'
