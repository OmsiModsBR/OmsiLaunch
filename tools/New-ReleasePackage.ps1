[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $OutputDirectory = '',
    # Overwriting an already published versioned archive in artifacts\release
    # is refused unless this switch is given.
    [switch] $AllowOverwritePublished
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
[xml] $identity = Get-Content -LiteralPath (Join-Path $root 'OmsiLaunch.Version.props') -Raw
$identityProperties = $identity.Project.PropertyGroup | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $root 'artifacts\release' }
# Resolve a relative -OutputDirectory against the PowerShell location, not the
# process working directory: manifest paths are computed from absolute file
# names, and the published-location guard compares absolute paths.
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$stage = Join-Path $OutputDirectory 'OmsiLaunch-current'
$zip = Join-Path $OutputDirectory 'OmsiLaunch-current.zip'
$publicZip = Join-Path $OutputDirectory ('OmsiLaunch-' + $identityProperties.OmsiLaunchProductVersion + '.zip')
$publicChecksum = $publicZip + '.sha256'

$publishedDirectory = [IO.Path]::GetFullPath((Join-Path $root 'artifacts\release')).TrimEnd('\')
$isPublishedLocation = [string]::Equals([IO.Path]::GetFullPath($OutputDirectory).TrimEnd('\'), $publishedDirectory, [StringComparison]::OrdinalIgnoreCase)
if ($isPublishedLocation -and (Test-Path -LiteralPath $publicZip) -and -not $AllowOverwritePublished) {
    throw "Refusing to overwrite the existing published archive $publicZip. Use -OutputDirectory for a candidate package or -AllowOverwritePublished deliberately."
}
# The package is assembled in a brand-new, uniquely named staging directory:
# nothing left from an earlier run can enter the closure. The previous
# OmsiLaunch-current folder and archives are replaced only after the new
# closure, its manifest and its archive have been validated.
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$finalStage = $stage
$stage = Join-Path $OutputDirectory ('.staging-' + [Guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $stage) { throw "Staging directory unexpectedly exists: $stage" }
New-Item -ItemType Directory -Path $stage | Out-Null
if (@(Get-ChildItem -LiteralPath $stage -Force).Count -ne 0) { throw 'Staging directory is not empty.' }
New-Item -ItemType Directory -Path (Join-Path $stage 'plugins'), (Join-Path $stage '.omsilaunch\assets\splash'), (Join-Path $stage '.omsilaunch\docs'), (Join-Path $stage '.omsilaunch\examples') -Force | Out-Null
$verifyDirectory = $null
try {

$cli = Join-Path $root "artifacts\bin\OmsiLaunch.Cli\$Configuration\net6.0-windows"
$bootstrapper = Join-Path $root "artifacts\bin\OmsiLaunch.Bootstrapper\$Configuration\OmsiLaunch.exe"
$windowsHost = Join-Path $root "artifacts\bin\OmsiLaunch.WindowsHost\$Configuration\OmsiLaunchW.exe"
$netHost = Join-Path $root "artifacts\bin\OmsiLaunch.Bootstrapper\$Configuration\nethost.dll"
$plugin = Join-Path $root "artifacts\bin\OmsiLaunch.Plugin\x86\$Configuration\net6.0-windows"
$native = Join-Path $root "artifacts\x86\$Configuration\OmsiLaunch.Native.x86.dll"

# Stale-input guard. Native.x86, the bootstrapper and the Windows host are not
# built by the managed solution; an older binary left in artifacts\ would be
# packaged and hashed as if it were current. Every packaged build output must
# be at least as new as the sources it is built from.
function Assert-Fresh([string] $Artifact, [string[]] $SourceRoots, [string[]] $Patterns) {
    if (-not (Test-Path -LiteralPath $Artifact)) { throw "Required build artifact missing: $Artifact" }
    $built = (Get-Item -LiteralPath $Artifact).LastWriteTimeUtc
    $newest = foreach ($sourceRoot in $SourceRoots) {
        $full = Join-Path $root $sourceRoot
        if (Test-Path -LiteralPath $full -PathType Leaf) { Get-Item -LiteralPath $full }
        elseif (Test-Path -LiteralPath $full) { Get-ChildItem -LiteralPath $full -Recurse -File -Include $Patterns | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } }
    }
    $latest = $newest | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($latest -and $latest.LastWriteTimeUtc -gt $built) { throw "Stale build artifact: $Artifact is older than $($latest.FullName). Rebuild before packaging (tools\Invoke-OfflineValidation.ps1 builds every project)." }
}
$versionProps = 'OmsiLaunch.Version.props'
# Native.x86 is verified deterministically, not by timestamp: the build writes
# a receipt with the SHA-256 of the DLL it produced and of every source it was
# built from. The packaged DLL must be that exact output, and every source must
# still have the recorded content; every current native source must be listed.
function Assert-NativeReceipt([string] $Dll) {
    $receipt = Join-Path (Split-Path -Parent $Dll) 'OmsiLaunch.Native.x86.build-receipt.txt'
    if (-not (Test-Path -LiteralPath $Dll)) { throw "Required native artifact missing: $Dll" }
    if (-not (Test-Path -LiteralPath $receipt)) { throw "Native build receipt missing: $receipt. Build src\OmsiLaunch.Native.x86 (tools\Invoke-OfflineValidation.ps1 does) before packaging." }
    $lines = Get-Content -LiteralPath $receipt | Where-Object { $_ }
    $output = ($lines | Where-Object { $_ -like 'output=*' } | Select-Object -First 1)
    if (-not $output) { throw 'Native build receipt has no output hash.' }
    if ((Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash -ne $output.Substring(7).ToUpperInvariant()) { throw "Native.x86 does not match its build receipt: $Dll is not the output of the recorded build (stale or foreign binary)." }
    $recorded = @{}
    foreach ($line in $lines | Where-Object { $_ -like 'source=*' }) {
        $parts = $line.Substring(7).Split('|', 2)
        $recorded[[IO.Path]::GetFullPath($parts[1])] = $parts[0].ToUpperInvariant()
        if (-not (Test-Path -LiteralPath $parts[1])) { throw "Native source listed in the build receipt no longer exists: $($parts[1])" }
        if ((Get-FileHash -LiteralPath $parts[1] -Algorithm SHA256).Hash -ne $parts[0].ToUpperInvariant()) { throw "Native source changed after the recorded build: $($parts[1]). Rebuild src\OmsiLaunch.Native.x86." }
    }
    $current = @(Get-ChildItem -LiteralPath (Join-Path $root 'src\OmsiLaunch.Native.x86') -File -Recurse -Include '*.cpp', '*.h', '*.rc', '*.vcxproj' | ForEach-Object { $_.FullName }) + (Join-Path $root $versionProps)
    foreach ($source in $current) { if (-not $recorded.ContainsKey([IO.Path]::GetFullPath($source))) { throw "Native source not covered by the build receipt: $source. Rebuild src\OmsiLaunch.Native.x86." } }
}
Assert-NativeReceipt $native
Assert-Fresh $bootstrapper @('tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp', 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.rc', 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.vcxproj', $versionProps) @('*')
Assert-Fresh $windowsHost @('tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp', 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.rc', 'tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.vcxproj', $versionProps) @('*')
# Managed assemblies are checked against their own project only: MSBuild does
# not recompile a dependent assembly when a referenced project's public
# surface is unchanged, so a solution-wide comparison would be a false alarm.
$managed = @{
    'OmsiLaunch.Controller.dll' = 'tools\OmsiLaunch.Cli'; 'OmsiLaunch.Api.dll' = 'src\OmsiLaunch.Api'; 'OmsiLaunch.Core.dll' = 'src\OmsiLaunch.Core'
    'OmsiLaunch.Configuration.dll' = 'src\OmsiLaunch.Configuration'; 'OmsiLaunch.Content.dll' = 'src\OmsiLaunch.Content'; 'OmsiLaunch.Process.dll' = 'src\OmsiLaunch.Process'
    'OmsiLaunch.Builds.Omsi23004.dll' = 'src\OmsiLaunch.Builds.Omsi23004'; 'OmsiLaunch.NativeResolution.dll' = 'src\OmsiLaunch.NativeResolution'
    'OmsiLaunch.Runtime.Modules.dll' = 'src\OmsiLaunch.Runtime.Modules'
}
foreach ($entry in $managed.GetEnumerator()) { Assert-Fresh (Join-Path $cli $entry.Key) @($entry.Value) @('*.cs', '*.csproj') }
$pluginManaged = @{ 'OmsiLaunch.Plugin.dll' = 'src\OmsiLaunch.Plugin'; 'OmsiLaunch.Interop.dll' = 'src\OmsiLaunch.Interop'; 'OmsiLaunch.Api.dll' = 'src\OmsiLaunch.Api'; 'OmsiLaunch.Builds.Omsi23004.dll' = 'src\OmsiLaunch.Builds.Omsi23004'; 'OmsiLaunch.NativeResolution.dll' = 'src\OmsiLaunch.NativeResolution'; 'OmsiLaunch.Runtime.Modules.dll' = 'src\OmsiLaunch.Runtime.Modules' }
foreach ($entry in $pluginManaged.GetEnumerator()) { Assert-Fresh (Join-Path $plugin $entry.Key) @($entry.Value) @('*.cs', '*.csproj') }
Assert-Fresh (Join-Path $plugin 'OmsiLaunch.PluginNE.dll') @('src\OmsiLaunch.Plugin\OmsiLaunch.PluginNE.rc', $versionProps) @('*')

$cliFiles = @(
    'OmsiLaunch.Controller.dll', 'OmsiLaunch.Controller.deps.json', 'OmsiLaunch.Controller.runtimeconfig.json',
    'OmsiLaunch.Api.dll', 'OmsiLaunch.Configuration.dll', 'OmsiLaunch.Content.dll',
    'OmsiLaunch.Core.dll', 'OmsiLaunch.Process.dll', 'OmsiLaunch.Builds.Omsi23004.dll', 'OmsiLaunch.NativeResolution.dll', 'OmsiLaunch.Runtime.Modules.dll', 'YamlDotNet.dll'
)

if (-not (Test-Path -LiteralPath $bootstrapper)) { throw "Required controller bootstrapper missing: $bootstrapper" }
Copy-Item -LiteralPath $bootstrapper -Destination (Join-Path $stage 'OmsiLaunch.exe')
if (-not (Test-Path -LiteralPath $windowsHost)) { throw "Required Windows host missing: $windowsHost" }
Copy-Item -LiteralPath $windowsHost -Destination (Join-Path $stage 'OmsiLaunchW.exe')
if (-not (Test-Path -LiteralPath $netHost)) { throw "Required controller nethost missing: $netHost" }
Copy-Item -LiteralPath $netHost -Destination (Join-Path $stage 'nethost.dll')
$pluginFiles = @(
    'OmsiLaunch.Plugin.opl', 'OmsiLaunch.PluginNE.dll', 'OmsiLaunch.Plugin.dll',
    'OmsiLaunch.Plugin.deps.json', 'OmsiLaunch.Plugin.runtimeconfig.json',
    'OmsiLaunch.Api.dll', 'OmsiLaunch.Builds.Omsi23004.dll', 'OmsiLaunch.NativeResolution.dll', 'OmsiLaunch.Runtime.Modules.dll', 'OmsiLaunch.Interop.dll'
)

foreach ($file in $cliFiles) {
    $source = Join-Path $cli $file
    if (-not (Test-Path -LiteralPath $source)) { throw "Required CLI artifact missing: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $stage $file)
}
foreach ($file in $pluginFiles) {
    $source = Join-Path $plugin $file
    if (-not (Test-Path -LiteralPath $source)) { throw "Required plugin artifact missing: $source" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $stage "plugins\$file")
}
if (-not (Test-Path -LiteralPath $native)) { throw "Required native artifact missing: $native" }
Copy-Item -LiteralPath $native -Destination (Join-Path $stage 'plugins\OmsiLaunch.Native.x86.dll')
Copy-Item -Path (Join-Path $cli 'assets\splash\*.bmp') -Destination (Join-Path $stage '.omsilaunch\assets\splash')
Copy-Item -LiteralPath (Join-Path $root 'examples\release-session.example.json') -Destination (Join-Path $stage '.omsilaunch\examples\release-session.example.json')
New-Item -ItemType Directory -Force -Path (Join-Path $stage '.omsilaunch\examples\session-profiles\rmg-leste') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs\examples\session-profiles\rmg-leste\profile.yaml') -Destination (Join-Path $stage '.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml')
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $stage 'LICENSE')
# The LGPL-3.0 supplements the GPL-3.0: both texts go with the program (LGPL-3.0 section 4).
Copy-Item -LiteralPath (Join-Path $root 'COPYING') -Destination (Join-Path $stage 'COPYING')
Copy-Item -LiteralPath (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination (Join-Path $stage 'THIRD-PARTY-NOTICES.md')

# The English documentation ships as the same tree as docs\ so its relative
# links resolve offline and the CLI usage text's reference path
# (.omsilaunch\docs\reference\cli.md) exists (documentation audit BUG-08).
# docs\localized is copied below.
$docsSource = Join-Path $root 'docs'
Get-ChildItem -LiteralPath $docsSource -File -Recurse | Where-Object { $_.FullName.Substring($docsSource.Length + 1) -notlike 'localized\*' } | ForEach-Object {
    $destination = Join-Path $stage ('.omsilaunch\docs\' + $_.FullName.Substring($docsSource.Length + 1))
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $destination
}

# Translations: every locale listed in tools\Localization\locales.json ships
# complete, under the same structure; a missing page stops the packaging so a
# partial locale never ships as a Beta 3 translation.
$localization = Get-Content -LiteralPath (Join-Path $root 'tools\Localization\locales.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$localizedRoot = Join-Path $root 'docs\localized'
New-Item -ItemType Directory -Path (Join-Path $stage '.omsilaunch\docs\localized') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $localizedRoot 'LOCALIZATION-MANIFEST.md') -Destination (Join-Path $stage '.omsilaunch\docs\localized\LOCALIZATION-MANIFEST.md')
foreach ($locale in @($localization.locales | ForEach-Object { $_.id })) {
    foreach ($page in @($localization.pages)) {
        $source = Join-Path $localizedRoot ($locale + '\' + $page.Replace('/', '\'))
        if (-not (Test-Path -LiteralPath $source)) { throw "Localized page missing: $locale/$page" }
        $destination = Join-Path $stage ('.omsilaunch\docs\localized\' + $locale + '\' + $page.Replace('/', '\'))
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $destination
    }
}

$files = Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object {
    [ordered]@{
        path = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
        bytes = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}
$manifest = [ordered]@{
    product = $identityProperties.OmsiLaunchProductName
    product_version = $identityProperties.OmsiLaunchProductVersion
    package_alias = $identityProperties.OmsiLaunchPackageAlias
    control_protocol = '0.1'
    # Compatibility is "OMSI 2 installed"; capabilities are resolved per
    # target in process. Known layouts are only a fast path.
    requirement = 'OMSI 2'
    known_layouts = @(
        [ordered]@{ build_id = 'Omsi23004'; code_sha256 = '13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028' }
    )
    configuration = $Configuration
    generated_utc = [DateTimeOffset]::UtcNow.ToString('O')
    files = $files
}
# UTF-8 without BOM: the manifest is read at runtime by System.Text.Json and
# must not depend on which PowerShell edition produced it.
[IO.File]::WriteAllText((Join-Path $stage 'release-manifest.json'), ($manifest | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))

# Self-check before archiving: the staged closure must equal the manifest
# exactly (same file set, same hashes) and the plugin closure must equal the
# build output it was copied from.
# The checker's findings are printed when it fails, so a failure says why.
$integrity = & (Join-Path $PSScriptRoot 'Test-ReleasePackageIntegrity.ps1') -PackagePath $stage 2>&1
if ($LASTEXITCODE -ne 0) { $integrity | ForEach-Object { Write-Host $_ }; throw 'The staged package failed its integrity self-check.' }
foreach ($file in $pluginFiles) {
    if ((Get-FileHash -LiteralPath (Join-Path $plugin $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $stage "plugins\$file")).Hash) { throw "Staged plugin file differs from its build output: $file" }
}
if ((Get-FileHash -LiteralPath $native).Hash -ne (Get-FileHash -LiteralPath (Join-Path $stage 'plugins\OmsiLaunch.Native.x86.dll')).Hash) { throw 'Staged Native.x86 differs from its build output.' }
$candidateZip = $stage + '.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $candidateZip -CompressionLevel Optimal

# The archive is validated as shipped: extracted into a fresh directory and
# checked against the same manifest bytes that were validated in staging.
$verifyDirectory = Join-Path ([IO.Path]::GetTempPath()) ('omsilaunch-verify-' + [Guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $candidateZip -DestinationPath $verifyDirectory
if ((Get-FileHash -LiteralPath (Join-Path $verifyDirectory 'release-manifest.json')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $stage 'release-manifest.json')).Hash) { throw 'The archived manifest differs from the validated staging manifest.' }
$integrity = & (Join-Path $PSScriptRoot 'Test-ReleasePackageIntegrity.ps1') -PackagePath $verifyDirectory 2>&1
if ($LASTEXITCODE -ne 0) { $integrity | ForEach-Object { Write-Host $_ }; throw 'The extracted archive failed validation against its manifest.' }

Remove-Item -LiteralPath $finalStage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zip, $publicZip, $publicChecksum -Force -ErrorAction SilentlyContinue
Move-Item -LiteralPath $stage -Destination $finalStage
Move-Item -LiteralPath $candidateZip -Destination $zip
Copy-Item -LiteralPath $zip -Destination $publicZip
$hash = (Get-FileHash -LiteralPath $publicZip -Algorithm SHA256).Hash
Set-Content -LiteralPath $publicChecksum -Value ($hash + '  ' + [IO.Path]::GetFileName($publicZip)) -Encoding ascii
Write-Output $publicZip
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path -LiteralPath ($stage + '.zip')) { Remove-Item -LiteralPath ($stage + '.zip') -Force -ErrorAction SilentlyContinue }
    if ($verifyDirectory -and (Test-Path -LiteralPath $verifyDirectory)) { Remove-Item -LiteralPath $verifyDirectory -Recurse -Force -ErrorAction SilentlyContinue }
}
