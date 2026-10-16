namespace OmsiLaunch.Runtime.Modules;

// The managed component of a module implements this type (named by the
// manifest's components[].entry, with a public parameterless constructor).
// Every callback runs on OMSI's UI thread, like the product's own runtime
// operations; a callback that overruns its manifest budget fails its phase.
public interface IRuntimeModule
{
    // Lifecycle step 4: the session's world is about to start. Acquire
    // resources through the context so the bridge can release them.
    void Arm(IRuntimeModuleContext context);
    // Lifecycle step 5: gameplay was entered.
    void GameplayReady();
    // Lifecycle step 6: one declared operation (without the module.<id>. prefix).
    // Required arguments were checked. Throw ModuleOperationException for a
    // typed failure.
    IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments);
    // Lifecycle step 7: the session ends or the module is disabled. The bridge
    // releases every tracked resource afterwards, whatever Stop did.
    void Stop();
}

// What the bridge offers a module. Nothing here exposes addresses, the
// executable or its identity.
public interface IRuntimeModuleContext
{
    Guid SessionId { get; }
    string ModuleId { get; }
    // The module's own directory (read its assets from here).
    string ModuleDirectory { get; }
    // The effective value of every setting the module declares (bridge
    // protocol 2), already validated and normalized; "auto" is passed as is.
    IReadOnlyDictionary<string, string> Settings { get; }
    // The module's writable data directory for this installation
    // (.omsilaunch\module-data\<id>), created on first use. It is not part of
    // the session transaction: what the module writes there stays.
    string DataDirectory { get; }
    // The OmsiLaunch product version (for example 0.1.0-beta4).
    string ProductVersion { get; }
    // The pipe the module's host program serves (\\.\pipe\OmsiLaunch.Module.
    // <session>.<id>); null when the module declares no host component.
    string? SessionChannelName { get; }
    // Cancelled when the module stops or is disabled.
    CancellationToken Stopping { get; }
    bool IsCapabilityAvailable(string capability);
    // Emits the runtime event module.<id>.<name>. The event "notice" (data
    // "title" and "text") is shown to the user by the OmsiLaunch tray.
    void Emit(string name, IReadOnlyDictionary<string, string>? data = null);
    // Registers a resource the bridge releases (in reverse order) at cleanup.
    void Track(string kind, IDisposable resource);
    // Runs work on a tracked background thread. The work must return soon
    // after Stopping is cancelled; a thread still alive after cleanup is
    // reported as OL_E_MODULE_CLEANUP_INCOMPLETE.
    void RunBackground(string name, Action<CancellationToken> work);
    // Starts one of the module's auxiliary components (pinned .exe). The
    // process is bound to the session and terminated at cleanup.
    IModuleAuxiliaryProcess StartAuxiliaryProcess(string componentPath, string arguments);
}

// A loaded native component (omsilaunch_module_native_v1). Disposing it calls
// its stop export and removes every hook it owns (Dispose throws when one
// cannot be removed). The DLL stays loaded, inert, until OMSI exits.
public interface IModuleNativeComponent : IDisposable
{
    void GameplayReady();
}

public interface IModuleAuxiliaryProcess : IDisposable
{
    int ProcessId { get; }
    bool HasExited { get; }
}

public sealed class ModuleOperationException : Exception
{
    public string Code { get; }
    public ModuleOperationException(string code, string message) : base(message) => Code = code;
}
