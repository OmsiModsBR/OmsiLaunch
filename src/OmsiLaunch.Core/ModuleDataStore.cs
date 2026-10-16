using OmsiLaunch.Api;
using OmsiLaunch.Process;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Core;

// A module's writable data (.omsilaunch\module-data\<id>) is the module's, not
// part of any session transaction. Clearing it is an explicit user request.
public static class ModuleDataStore
{
    // Deletes .omsilaunch\module-data\<id> and nothing else. Refused while a
    // session owns the installation (its modules may be writing there).
    // False when there was nothing to delete.
    public static bool Clear(string installationRoot, string moduleId)
    {
        if (!RuntimeModuleProtocol.IsModuleId(moduleId)) throw new ArgumentException(PublicErrorCodes.MODULE_NOT_FOUND + ": '" + moduleId + "' is not a module id.");
        var root = Path.GetFullPath(installationRoot);
        InstallationLease lease;
        try { lease = InstallationLease.Acquire(root); }
        catch (InvalidOperationException) { throw new InvalidOperationException("OL_E_INSTALLATION_BUSY: a session owns this installation; end it before clearing module data."); }
        using (lease)
        {
            var data = Path.Combine(root, RuntimeModuleProtocol.ModuleDataDirectory);
            var target = Path.Combine(data, moduleId);
            if (!Directory.Exists(target)) return false;
            // Nothing outside the installation is ever deleted: a link is
            // removed as a link, and a linked module-data folder is refused.
            if (ModuleCatalog.IsReparsePoint(Path.Combine(root, ".omsilaunch")) || ModuleCatalog.IsReparsePoint(data))
                throw new IOException(PublicErrorCodes.MODULE_INTEGRITY + ": " + RuntimeModuleProtocol.ModuleDataDirectory + " is reached through a link; nothing was deleted.");
            DeleteTree(target);
            return true;
        }
    }

    private static void DeleteTree(string directory)
    {
        if (ModuleCatalog.IsReparsePoint(directory)) { Directory.Delete(directory); return; }
        foreach (var child in Directory.EnumerateDirectories(directory)) DeleteTree(child);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }
        Directory.Delete(directory);
    }
}
