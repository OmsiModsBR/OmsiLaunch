using System.Runtime.InteropServices;

// Native imports resolve from the plugin's own directory (plugins\) or the
// Windows system directory only; the OMSI root and PATH are never probed.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]

// The plugin integration tests load a real module through the bridge environment.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OmsiLaunch.IntegrationTests")]
