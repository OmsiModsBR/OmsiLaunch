[CmdletBinding()]
param(
    # A package directory (staged closure) or a package .zip.
    [Parameter(Mandatory)]
    [string] $PackagePath,
    # Optional, read-only: compare an installation's product-owned files with
    # the package manifest. Nothing in the installation is modified.
    [string] $InstallationRoot = ''
)

# Verifies that a package is one coherent closure:
#  - release-manifest.json lists exactly the files present (no extra, none missing),
#  - every listed hash and size matches the packaged bytes,
#  - the required product closure (executables, controller, permanent plugin
#    closure including Native.x86) is present,
#  - the manifest is readable by the runtime (UTF-8, BOM tolerated) and declares
#    configuration Release.
# Exit code 0 = coherent; 1 = any mismatch. It never launches OMSI.
$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]
$temporary = $null
try {
    $package = (Resolve-Path -LiteralPath $PackagePath).Path
    if ((Get-Item -LiteralPath $package) -isnot [IO.DirectoryInfo]) {
        $temporary = Join-Path ([IO.Path]::GetTempPath()) ('omsilaunch-package-' + [Guid]::NewGuid().ToString('N'))
        Expand-Archive -LiteralPath $package -DestinationPath $temporary
        $package = $temporary
    }
    $manifestPath = Join-Path $package 'release-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath)) { throw 'release-manifest.json is missing from the package.' }
    $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    if ($manifest.configuration -ne 'Release') { $failures.Add("manifest configuration is '$($manifest.configuration)', expected Release") }

    $listed = @{}
    foreach ($entry in $manifest.files) {
        if ([string]::IsNullOrWhiteSpace($entry.path)) { $failures.Add('manifest entry without path'); continue }
        $relative = $entry.path.Replace('/', '\')
        $segments = $relative.Split('\')
        if ([IO.Path]::IsPathRooted($relative) -or $relative.Contains(':') -or @($segments | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' }).Count -ne 0) { $failures.Add("invalid manifest path: $($entry.path)"); continue }
        if (-not ($entry.sha256 -match '^[0-9A-Fa-f]{64}$')) { $failures.Add("invalid manifest hash for $relative"); continue }
        # PowerShell hashtables are case-insensitive: two spellings of one path
        # are a duplicate, exactly as Windows would resolve them.
        if ($listed.ContainsKey($relative)) { $failures.Add("duplicate manifest entry: $relative"); continue }
        $listed[$relative] = $entry
        $file = Join-Path $package $relative
        if (-not (Test-Path -LiteralPath $file)) { $failures.Add("listed but missing: $relative"); continue }
        $item = Get-Item -LiteralPath $file
        if ($item.Length -ne [long]$entry.bytes) { $failures.Add("size mismatch: $relative ($($item.Length) vs $($entry.bytes))") }
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.sha256) { $failures.Add("hash mismatch: $relative") }
    }
    Get-ChildItem -LiteralPath $package -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($package.Length + 1)
        if ($relative -ne 'release-manifest.json' -and -not $listed.ContainsKey($relative)) { $failures.Add("present but not listed: $relative") }
    }

    $required = @(
        'OmsiLaunch.exe', 'OmsiLaunchW.exe', 'nethost.dll', 'OmsiLaunch.Controller.dll', 'OmsiLaunch.Controller.deps.json', 'OmsiLaunch.Controller.runtimeconfig.json',
        'OmsiLaunch.Api.dll', 'OmsiLaunch.Configuration.dll', 'OmsiLaunch.Content.dll', 'OmsiLaunch.Core.dll', 'OmsiLaunch.Process.dll', 'OmsiLaunch.Builds.Omsi23004.dll', 'OmsiLaunch.NativeResolution.dll', 'OmsiLaunch.Runtime.Modules.dll', 'YamlDotNet.dll',
        'plugins\OmsiLaunch.Plugin.opl', 'plugins\OmsiLaunch.PluginNE.dll', 'plugins\OmsiLaunch.Plugin.dll', 'plugins\OmsiLaunch.Plugin.deps.json', 'plugins\OmsiLaunch.Plugin.runtimeconfig.json',
        'plugins\OmsiLaunch.Api.dll', 'plugins\OmsiLaunch.Builds.Omsi23004.dll', 'plugins\OmsiLaunch.NativeResolution.dll', 'plugins\OmsiLaunch.Runtime.Modules.dll', 'plugins\OmsiLaunch.Interop.dll', 'plugins\OmsiLaunch.Native.x86.dll'
    )
    foreach ($relative in $required) { if (-not $listed.ContainsKey($relative)) { $failures.Add("required closure file not in manifest: $relative") } }
    # Runtime modules live in plugins\OmsiLaunch.Modules\<id>\ and are not part of the closure.
    $pluginEntries = @($listed.Keys | Where-Object { $_ -like 'plugins\*' -and $_ -notlike 'plugins\*\*' })
    if ($pluginEntries.Count -ne 11) { $failures.Add("permanent plugin closure has $($pluginEntries.Count) files, expected 11") }

    $installationReport = $null
    if (-not [string]::IsNullOrWhiteSpace($InstallationRoot)) {
        $installation = (Resolve-Path -LiteralPath $InstallationRoot).Path
        $differences = @()
        foreach ($relative in @($listed.Keys | Where-Object { $_ -notlike '.omsilaunch\*' }) + 'release-manifest.json') {
            $installed = Join-Path $installation $relative
            $packaged = Join-Path $package $relative
            if (-not (Test-Path -LiteralPath $installed)) { $differences += "missing in installation: $relative"; continue }
            if ((Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath $packaged).Hash) { $differences += "differs from package: $relative" }
        }
        $installationReport = [ordered]@{ installation = $installation; coherent_with_package = ($differences.Count -eq 0); differences = $differences }
    }

    [ordered]@{
        package = $PackagePath
        product_version = $manifest.product_version
        configuration = $manifest.configuration
        manifest_entries = $listed.Count
        manifest_bom = ([IO.File]::ReadAllBytes($manifestPath)[0] -eq 0xEF)
        coherent = ($failures.Count -eq 0)
        failures = $failures
        installation_comparison = $installationReport
    } | ConvertTo-Json -Depth 5
    if ($failures.Count -ne 0) { exit 1 }
    exit 0
}
finally { if ($temporary -and (Test-Path -LiteralPath $temporary)) { Remove-Item -LiteralPath $temporary -Recurse -Force } }
