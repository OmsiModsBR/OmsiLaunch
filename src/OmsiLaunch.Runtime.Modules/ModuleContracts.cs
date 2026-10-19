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
    // The OMSI product version the running game reports about itself (for
    // example 2.3.004), never the executable's PE file version (2.2.032 on the
    // reference build); "unknown" when it cannot be determined with certainty.
    string OmsiVersion { get; }
    // The operations of the catalog the module declared in runtime_operations,
    // executed in process (bridge protocol 3, capability bridge.runtime).
    IModuleRuntime Runtime { get; }
    // The module's loaded native component, null when it has none. A managed
    // component reaches the native half through it (Execute, bridge protocol 3).
    IModuleNativeComponent? NativeComponent { get; }
    // Community content the manifest declared in content_inputs (bridge protocol 3).
    IModuleContent Content { get; }
    // Keyboard and mouse capture (bridge protocol 3, capability bridge.input).
    IModuleInput Input { get; }
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

// A module that wants a periodic callback (bridge protocol 3, capability
// bridge.tick, manifest tick_ms). Tick runs on OMSI's UI thread, at most once
// per tick_ms; it is not a per-frame callback (draw from the Direct3D Present
// hook instead). A tick that overruns tick_ms is reported and the ticks it
// missed are skipped, never queued.
public interface ITickingModule
{
    void Tick(TimeSpan elapsed);
}

// The catalog operations a module may run in process: the same registry, the
// same typed OL_E_* results and the same mutation policies as the runtime
// control channel, without the channel. Only the operations the manifest lists
// in runtime_operations are allowed (OL_E_MODULE_OPERATION_NOT_DECLARED); the
// list is transparency and planning information, not a security boundary,
// because the module already runs inside OMSI.
public interface IModuleRuntime
{
    // True when the manifest declares the operation.
    bool IsDeclared(string operation);
    // Never throws for a failed operation: the result carries the typed code.
    ModuleCallResult Execute(string operation, IReadOnlyDictionary<string, string>? arguments = null);
}

// What the bridge captured from the game window (bridge protocol 3, capability
// bridge.input). Plain data: no window handle, no message structure. Keyboard
// events carry the virtual key; Char carries the UTF-16 code unit of a typed
// character; mouse coordinates are client coordinates of the game's view
// window, the one Direct3D draws into; Delta is the wheel's notch count times
// 120. Modifiers: 1 Shift, 2 Ctrl, 4 Alt (Shift/Ctrl from the message flags).
public enum ModuleInputKind : byte { KeyDown = 1, KeyUp = 2, Char = 3, MouseMove = 4, ButtonDown = 5, ButtonUp = 6, Wheel = 7, DoubleClick = 8 }
public enum ModuleMouseButton : byte { None = 0, Left = 1, Right = 2, Middle = 3 }
public readonly record struct ModuleInputEvent(ModuleInputKind Kind, int Key, int X, int Y, ModuleMouseButton Button, int Delta, int Modifiers);

// Capture of the game's keyboard and mouse messages for the module's own menu
// (the pause menu replacement). While a module captures, the messages that
// belong to the game's window tree are handed to it and removed, so the game
// and its native forms do not also act on them; native modal dialogs (save,
// options, new game) need real input, so the module releases the capture
// before it asks for one. One module captures at a time; the capture is
// released when the module stops or fails, whatever Stop did.
public interface IModuleInput
{
    // True when the build resolved what capturing needs (native capability ui.input.capture).
    bool IsAvailable { get; }
    // True while this module holds the capture.
    bool IsCapturing { get; }
    // Starts or ends the capture. False when it cannot start: not available,
    // not on the UI thread, or another module holds it.
    bool Capture(bool enabled);
    // Takes the events captured since the last call (at most 256 are kept;
    // the oldest are dropped first). Empty when this module does not capture.
    IReadOnlyList<ModuleInputEvent> Drain();
}

// A loaded native component (omsilaunch_module_native_v1). Disposing it calls
// its stop export and removes every hook it owns (Dispose throws when one
// cannot be removed). The DLL stays loaded, inert, until OMSI exits.
public interface IModuleNativeComponent : IDisposable
{
    void GameplayReady();
    // Optional native exports (false when the component does not export them).
    bool HasTick { get; }
    void Tick(uint elapsedMilliseconds);
    bool HasExecute { get; }
    // Calls the component's execute export: operation and JSON arguments in,
    // JSON out. Null when the component has no execute export.
    string? Execute(string operation, string argumentsJson);
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
