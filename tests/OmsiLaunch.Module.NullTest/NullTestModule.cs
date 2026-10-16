using System.Globalization;
using OmsiLaunch.Api;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Module.NullTest;

// Does nothing to OMSI. It exercises the lifecycle: tracks a resource, emits
// events, answers operations, fails on request, and stops.
public sealed class NullTestModule : IRuntimeModule
{
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
            _ => throw new ModuleOperationException(PublicErrorCodes.MODULE_OPERATION_UNKNOWN, operation)
        };
    }

    public void Stop() => context?.Emit("stopping");

    private sealed class Marker : IDisposable
    {
        private readonly IRuntimeModuleContext context;
        public Marker(IRuntimeModuleContext context) => this.context = context;
        public void Dispose() => context.Emit("released");
    }
}
