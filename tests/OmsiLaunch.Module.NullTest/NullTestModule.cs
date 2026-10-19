using System.Globalization;
using OmsiLaunch.Api;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Module.NullTest;

// Does nothing to OMSI. It exercises the lifecycle: tracks a resource, emits
// events, answers operations, fails on request, and stops.
public sealed class NullTestModule : IRuntimeModule, ITickingModule
{
    private int ticks;
    // Bridge protocol 3: counts ticks and reports the third one.
    public void Tick(TimeSpan elapsed)
    {
        if (++ticks == 3) context?.Emit("ticked", new Dictionary<string, string> { ["elapsed_ms"] = ((long)elapsed.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) });
    }

    private IRuntimeModuleContext? context;
    private int calls;

    public void Arm(IRuntimeModuleContext context)
    {
        this.context = context;
        context.Track("marker", new Marker(context));
        context.Emit("armed", new Dictionary<string, string> { ["session_id"] = context.SessionId.ToString("D") });
    }

    public void GameplayReady() => context?.Emit("gameplay-ready");

    public IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)
    {
        calls++;
        return operation switch
        {
            "ping" => new Dictionary<string, string> { ["echo"] = arguments.TryGetValue("text", out var text) ? text : "", ["calls"] = calls.ToString(CultureInfo.InvariantCulture) },
            "fail" => throw new ModuleOperationException(PublicErrorCodes.RUNTIME_VALUE_OUT_OF_RANGE, "requested failure"),
            // Bridge protocol 3: a catalog operation in process, the OMSI version, and the native half.
            "runtime-time" => RuntimeTime(),
            "native-echo" => new Dictionary<string, string> { ["answer"] = context?.NativeComponent?.Execute("echo", "{\"a\":\"b\"}") ?? "none" },
            _ => throw new ModuleOperationException(PublicErrorCodes.MODULE_OPERATION_UNKNOWN, operation)
        };
    }

    private IReadOnlyDictionary<string, string> RuntimeTime()
    {
        var result = context!.Runtime.Execute("time.read");
        var refused = context.Runtime.Execute("weather.read");
        return new Dictionary<string, string> { ["succeeded"] = result.Succeeded ? "true" : "false", ["undeclared_code"] = refused.ErrorCode ?? "", ["omsi_version"] = context.OmsiVersion, ["hour"] = result.Values.TryGetValue("hour", out var hour) ? hour : "" };
    }

    public void Stop() => context?.Emit("stopping");

    private sealed class Marker : IDisposable
    {
        private readonly IRuntimeModuleContext context;
        public Marker(IRuntimeModuleContext context) => this.context = context;
        public void Dispose() => context.Emit("released");
    }
}
