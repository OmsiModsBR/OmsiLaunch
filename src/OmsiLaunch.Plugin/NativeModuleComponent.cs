using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OmsiLaunch.Api;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Plugin;

// A runtime module's native component (omsilaunch_module_native_v1.h). The
// component asks for hooks by symbolic target; the bridge resolves, verifies
// and installs them through the native hook registry under the owner
// "module:<id>", and removes them all after the component's stop export
// returns. The DLL is then left loaded and inert until OMSI exits: a thread
// may still be returning through one of its detours (for example from a
// blocking ReadFile), and unloading it under that thread would crash OMSI.
internal sealed unsafe class NativeModuleComponent : IModuleNativeComponent
{
    // Mirrors OlHookResult (NativeHooks.h); 12 is get_value's "buffer too small".
    internal const int HookOk = 0, HookArgument = 1, HookNotFound = 2, HookForeign = 4, HookNotAllowed = 11, BufferTooSmall = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct Bridge
    {
        public uint Size;
        public uint Version;
        public nint Context;
        public delegate* unmanaged[Cdecl]<nint, byte*, nint, nint*, int> RequestHook;
        public delegate* unmanaged[Cdecl]<nint, byte*, byte*, int> Emit;
        // Bridge protocol 2 (the table grew; a component checks size first).
        public delegate* unmanaged[Cdecl]<nint, byte*, byte*, uint, uint*, int> GetValue;
        // Bridge protocol 3: run_operation(context, operation, arguments_json, out, capacity, length).
        public delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*, uint, uint*, int> RunOperation;
        // Bridge protocol 3: d3d_is_game_device(context, device).
        public delegate* unmanaged[Cdecl]<nint, nint, int> D3DIsGameDevice;
    }

    private readonly ModuleManifest manifest;
    private readonly Action<string, IReadOnlyDictionary<string, string>> emit;
    private readonly Func<string, string?> values;
    private readonly Func<string, IReadOnlyDictionary<string, string>?, ModuleCallResult> runtime;
    private readonly string owner;
    private nint library;
    private Bridge* bridge;
    private GCHandle self;
    private delegate* unmanaged[Cdecl]<Bridge*, int> gameplayReady;
    private delegate* unmanaged[Cdecl]<void> stop;
    // Bridge protocol 3 exports (both optional).
    private delegate* unmanaged[Cdecl]<Bridge*, uint, int> tick;
    private delegate* unmanaged[Cdecl]<Bridge*, byte*, byte*, byte*, uint, uint*, int> execute;
    private bool hooksAllowed;
    private bool disposed;
    // Set once stop was called: late bridge calls are refused or ignored.
    private volatile bool released;
    // Hooks this component obtained (registry id, symbolic target), for the
    // cleanup report.
    private readonly List<(int Id, string Target)> installed = new();

    private NativeModuleComponent(ModuleManifest manifest, Action<string, IReadOnlyDictionary<string, string>> emit, Func<string, string?> values, Func<string, IReadOnlyDictionary<string, string>?, ModuleCallResult> runtime)
    {
        this.manifest = manifest; this.emit = emit; this.values = values; this.runtime = runtime; owner = "module:" + manifest.Id;
    }

    public static NativeModuleComponent Start(ModuleManifest manifest, string path, Action<string, IReadOnlyDictionary<string, string>> emit, Func<string, string?>? values = null, Func<string, IReadOnlyDictionary<string, string>?, ModuleCallResult>? runtime = null)
    {
        var component = new NativeModuleComponent(manifest, emit, values ?? (_ => null), runtime ?? ((_, _) => new ModuleCallResult(false, PublicErrorCodes.RUNTIME_OPERATION_UNKNOWN, new Dictionary<string, string>(StringComparer.Ordinal))));
        try
        {
            component.library = NativeLibrary.Load(path);
            var start = (delegate* unmanaged[Cdecl]<Bridge*, int>)NativeLibrary.GetExport(component.library, "omsilaunch_module_native_v1_start");
            component.stop = (delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(component.library, "omsilaunch_module_native_v1_stop");
            component.gameplayReady = NativeLibrary.TryGetExport(component.library, "omsilaunch_module_native_v1_gameplay_ready", out var ready) ? (delegate* unmanaged[Cdecl]<Bridge*, int>)ready : null;
            component.tick = NativeLibrary.TryGetExport(component.library, "omsilaunch_module_native_v1_tick", out var tickExport) ? (delegate* unmanaged[Cdecl]<Bridge*, uint, int>)tickExport : null;
            component.execute = NativeLibrary.TryGetExport(component.library, "omsilaunch_module_native_v1_execute", out var executeExport) ? (delegate* unmanaged[Cdecl]<Bridge*, byte*, byte*, byte*, uint, uint*, int>)executeExport : null;
            component.self = GCHandle.Alloc(component);
            component.bridge = (Bridge*)NativeMemory.AllocZeroed((nuint)sizeof(Bridge));
            component.bridge->Size = (uint)sizeof(Bridge);
            component.bridge->Version = 1;
            component.bridge->Context = GCHandle.ToIntPtr(component.self);
            component.bridge->RequestHook = &RequestHook;
            component.bridge->Emit = &EmitEvent;
            component.bridge->GetValue = &GetValue;
            component.bridge->RunOperation = &RunOperation;
            component.bridge->D3DIsGameDevice = &D3DIsGameDevice;
            component.hooksAllowed = true;
            int result;
            try { result = start(component.bridge); }
            finally { component.hooksAllowed = false; }
            if (result != 0) throw new ModuleOperationException(PublicErrorCodes.MODULE_PHASE_FAILED, "the native component's start returned " + result.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return component;
        }
        catch
        {
            // A failed start: stop it, remove anything it hooked, and unload
            // it only when it never ran (no detour can be executing).
            if (component.bridge != null && component.stop != null) component.Release(callStop: true);
            else component.Release(callStop: false);
            throw;
        }
    }

    public void GameplayReady()
    {
        if (gameplayReady == null || disposed) return;
        hooksAllowed = true;
        int result;
        try { result = gameplayReady(bridge); }
        finally { hooksAllowed = false; }
        if (result != 0) throw new ModuleOperationException(PublicErrorCodes.MODULE_PHASE_FAILED, "the native component's gameplay_ready returned " + result.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public bool HasTick => tick != null && !disposed;
    public bool HasExecute => execute != null && !disposed;

    // A non-zero result fails the tick like any other phase of the module.
    public void Tick(uint elapsedMilliseconds)
    {
        if (tick == null || disposed) return;
        var result = tick(bridge, elapsedMilliseconds);
        if (result != 0) throw new ModuleOperationException(PublicErrorCodes.MODULE_PHASE_FAILED, "the native component's tick returned " + result.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // execute(bridge, operation, arguments_json, out, capacity, length): the
    // component answers JSON; 12 means the buffer was too small (length says
    // how much), so the call is repeated once with a buffer that fits.
    public string? Execute(string operation, string argumentsJson)
    {
        if (execute == null || disposed) return null;
        var operationBytes = Encoding.UTF8.GetBytes(operation + "\0");
        var argumentBytes = Encoding.UTF8.GetBytes(argumentsJson + "\0");
        var capacity = 4096u;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var output = new byte[capacity];
            uint length = 0;
            int result;
            fixed (byte* operationPointer = operationBytes) fixed (byte* argumentPointer = argumentBytes) fixed (byte* outputPointer = output)
                result = execute(bridge, operationPointer, argumentPointer, outputPointer, capacity, &length);
            if (result == 0) return Encoding.UTF8.GetString(output, 0, (int)Math.Min(length, capacity));
            if (result == BufferTooSmall && attempt == 0 && length < RuntimeModuleProtocol.MaxRuntimeResultBytes) { capacity = length + 1; continue; }
            throw new ModuleOperationException(PublicErrorCodes.MODULE_OPERATION_FAILED, "the native component's execute returned " + result.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        return null;
    }

    public void Dispose() => Release(callStop: true);

    // Stop, then remove every hook of the component. The library, the bridge
    // table and the handle stay allocated (see the class comment); only a
    // component that never started is unloaded.
    private void Release(bool callStop)
    {
        if (disposed) return;
        disposed = true;
        if (callStop && stop != null) stop();
        released = true;
        NativeHookRemoveOwner(owner, out var remaining);
        if (remaining != 0)
        {
            // Which hooks stayed and why: "installed" (another hook sits on
            // top of it) or "site-changed" (someone outside the registry
            // rewrote the site, so it is not written back).
            var retained = string.Join(",", installed.Select(x => (x.Id, x.Target, State: NativeHookVerify(x.Id))).Where(x => x.State != HookNotFound)
                .Select(x => x.Target + "=" + (x.State == HookOk ? "installed" : x.State == HookForeign ? "site-changed(" + Owner(name => NativeHookSiteOwner(x.Id, name, name.Capacity)) + ")" : x.State.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            emit("module.hooks.retained", new Dictionary<string, string>(StringComparer.Ordinal) { ["module"] = manifest.Id, ["hooks"] = remaining.ToString(System.Globalization.CultureInfo.InvariantCulture), ["retained"] = retained });
            throw new InvalidOperationException(remaining.ToString(System.Globalization.CultureInfo.InvariantCulture) + " hook(s) of " + owner + " could not be removed: " + retained);
        }
        if (!callStop && library != 0)
        {
            NativeLibrary.Free(library); library = 0;
            if (bridge != null) { NativeMemory.Free(bridge); bridge = null; }
            if (self.IsAllocated) self.Free();
        }
    }

    // "iat:<dll>!<function>" or "d3d9:device.<method>".
    private int Request(string target, nint detour, nint* original)
    {
        if (!hooksAllowed || released) return HookNotAllowed;
        if (detour == 0 || original == null) return HookArgument;
        int result; int id = 0; nint previous = 0;
        if (target.StartsWith("iat:", StringComparison.Ordinal) && target.IndexOf('!') is var bang and > 4 && bang < target.Length - 1)
        {
            if (!Declares("bridge.hooks.iat")) return HookNotAllowed;
            result = NativeHookInstallIat(owner, null, target[4..bang], target[(bang + 1)..], detour, out previous, out id);
        }
        else if (target.StartsWith("d3d9:device.", StringComparison.Ordinal))
        {
            if (!Declares("bridge.hooks.d3d9")) return HookNotAllowed;
            var slot = D3DSlot(target);
            if (slot < 0) return HookNotFound;
            result = NativeHookInstallD3DDevice(owner, slot, detour, out previous, out id);
        }
        else return HookNotFound;
        if (result == HookOk) { *original = previous; installed.Add((id, target)); }
        var data = new Dictionary<string, string>(StringComparer.Ordinal) { ["module"] = manifest.Id, ["target"] = target, ["result"] = result.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        // An import can have several entries (Omsi.exe imports kernel32's file
        // functions twice); the hook covers all of them.
        if (result == HookOk && target.StartsWith("iat:", StringComparison.Ordinal) && target.IndexOf('!') is var separator and > 4)
            data["slots"] = NativeHookImportSlots(null, target[4..separator], target[(separator + 1)..]).ToString(System.Globalization.CultureInfo.InvariantCulture);
        // A refused device method: name the module that owns it now.
        if (result == HookForeign && target.StartsWith("d3d9:device.", StringComparison.Ordinal) && D3DSlot(target) is var index and >= 0)
            data["owner"] = Owner(name => NativeHookD3DSlotOwner(index, name, name.Capacity));
        emit("module.hook", data);
        return result;
    }

    private static int D3DSlot(string target) => target["d3d9:device.".Length..] switch { "Reset" => 16, "Present" => 17, "BeginScene" => 41, "EndScene" => 42, _ => -1 };

    private static string Owner(Func<StringBuilder, int> query)
    {
        var name = new StringBuilder(260);
        return query(name) == HookOk ? name.ToString() : "unknown";
    }

    private bool Declares(string capability) => manifest.RequiredCapabilities.Contains(capability) || manifest.OptionalCapabilities.Contains(capability);

    private static NativeModuleComponent? From(nint context) => context == 0 ? null : GCHandle.FromIntPtr(context).Target as NativeModuleComponent;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int RequestHook(nint context, byte* target, nint detour, nint* original)
    {
        try { return From(context) is { } component && target != null ? component.Request(Marshal.PtrToStringUTF8((nint)target) ?? "", detour, original) : HookArgument; }
        catch (Exception) { return HookArgument; }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int EmitEvent(nint context, byte* name, byte* json)
    {
        try
        {
            if (From(context) is not { } component || name == null) return HookArgument;
            if (component.released) return HookNotAllowed;
            var eventName = Marshal.PtrToStringUTF8((nint)name) ?? "";
            if (!RuntimeModuleProtocol.IsOperationId(eventName)) return HookArgument;
            var data = new Dictionary<string, string>(StringComparer.Ordinal);
            if (json != null)
            {
                using var document = JsonDocument.Parse(Encoding.UTF8.GetBytes(Marshal.PtrToStringUTF8((nint)json) ?? "{}"));
                if (document.RootElement.ValueKind != JsonValueKind.Object) return HookArgument;
                foreach (var property in document.RootElement.EnumerateObject()) data[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : property.Value.GetRawText();
            }
            component.emit("module." + component.manifest.Id + "." + eventName, eventName == ModuleNotices.EventName ? ModuleNotices.Sanitize(data) : data);
            return HookOk;
        }
        catch (Exception) { return HookArgument; }
    }

    // get_value(context, name, buffer, capacity, length): copies the value as
    // UTF-8 with a terminating zero; *length receives the bytes needed
    // without the zero. 2 for an unknown name, 12 when capacity is too small.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int GetValue(nint context, byte* name, byte* buffer, uint capacity, uint* length)
    {
        try
        {
            if (From(context) is not { } component || name == null || length == null) return HookArgument;
            if (component.released) return HookNotAllowed;
            var value = component.values(Marshal.PtrToStringUTF8((nint)name) ?? "");
            if (value is null) return HookNotFound;
            var bytes = Encoding.UTF8.GetBytes(value);
            *length = (uint)bytes.Length;
            if (buffer == null || capacity < (uint)bytes.Length + 1) return BufferTooSmall;
            bytes.CopyTo(new Span<byte>(buffer, (int)capacity));
            buffer[bytes.Length] = 0;
            return HookOk;
        }
        catch (Exception) { return HookArgument; }
    }

    // d3d_is_game_device(context, device): 1 the game's own device, 0 another
    // device, 10 no device observed yet, 11 capability not declared or after stop.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int D3DIsGameDevice(nint context, nint device)
    {
        try
        {
            if (From(context) is not { } component) return HookArgument;
            if (component.released || !component.Declares("bridge.hooks.d3d9")) return HookNotAllowed;
            return NativeIsGameDevice(device);
        }
        catch (Exception) { return HookArgument; }
    }

    // run_operation(context, operation, arguments_json, out, capacity, length):
    // runs a catalog operation the manifest declares and writes the result as
    // JSON {"succeeded":bool,"error_code":string|null,"values":{...}} (UTF-8,
    // zero terminated). 0 = answered (the operation's own failure is inside the
    // JSON), 1 argument, 11 after stop, 12 buffer too small (*length = needed).
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int RunOperation(nint context, byte* operation, byte* argumentsJson, byte* buffer, uint capacity, uint* length)
    {
        try
        {
            if (From(context) is not { } component || operation == null || length == null) return HookArgument;
            if (component.released) return HookNotAllowed;
            var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
            if (argumentsJson != null)
            {
                using var document = JsonDocument.Parse(Encoding.UTF8.GetBytes(Marshal.PtrToStringUTF8((nint)argumentsJson) ?? "{}"));
                if (document.RootElement.ValueKind != JsonValueKind.Object) return HookArgument;
                foreach (var property in document.RootElement.EnumerateObject()) arguments[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()! : property.Value.GetRawText();
            }
            var result = component.runtime(Marshal.PtrToStringUTF8((nint)operation) ?? "", arguments);
            var json = JsonSerializer.Serialize(new Dictionary<string, object?> { ["succeeded"] = result.Succeeded, ["error_code"] = result.ErrorCode, ["values"] = result.Values });
            var bytes = Encoding.UTF8.GetBytes(json);
            *length = (uint)bytes.Length;
            if (buffer == null || capacity < (uint)bytes.Length + 1) return BufferTooSmall;
            bytes.CopyTo(new Span<byte>(buffer, (int)capacity));
            buffer[bytes.Length] = 0;
            return HookOk;
        }
        catch (Exception) { return HookArgument; }
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int NativeHookInstallIat(string owner, [MarshalAs(UnmanagedType.LPWStr)] string? hostModule, string importModule, string function, nint detour, out nint original, out int id);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int NativeHookInstallD3DDevice(string owner, int slotIndex, nint detour, out nint original, out int id);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int NativeHookRemoveOwner(string owner, out int remaining);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int NativeHookImportSlots([MarshalAs(UnmanagedType.LPWStr)] string? hostModule, string importModule, string function);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeIsGameDevice(nint device);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeHookVerify(int id);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeHookSiteOwner(int id, StringBuilder name, int capacity);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeHookD3DSlotOwner(int slotIndex, StringBuilder name, int capacity);
}
