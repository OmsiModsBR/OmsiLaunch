using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Interop;

namespace OmsiLaunch.Plugin;

public interface IPluginRuntimeControl
{
    RuntimeCommandResult Execute(RuntimeCommand command);
    IReadOnlyList<PluginRuntimeEvent> PollLifecycle() => Array.Empty<PluginRuntimeEvent>();
    // Starts observing the Direct3D device (acquires it, queues d3d.ready);
    // used before a module that hooks the device hears gameplay-ready.
    void ActivateDirect3D() { }
    void Shutdown() { }
}

public sealed record PluginRuntimeEvent(string Name, IReadOnlyDictionary<string, string> Data);

// Current adapter over profile-owned, in-process memory. It intentionally
// exposes semantic operations only; addresses remain inside the build profile.
internal sealed class CurrentRuntimeControl : IPluginRuntimeControl
{
    private readonly InProcessOmsiMemory memory = new();
    private readonly OmsiTimeAdapter time;
    private readonly OmsiRuntimeReaders readers;
    private readonly OmsiWeatherWriter weather;
    private readonly OmsiCameraWriter camera;
    private readonly OmsiCameraLockWriter cameraLock;
    private readonly string sessionTag;
    private CameraLockPolicy? activeCameraLock;
    private long nextCameraLockPoll;
    private string? lastCameraLockError;
    private long nextD3DPoll;
    private bool d3DActivated;
    private PluginRuntimeEvent? pendingD3DEvent;

    public CurrentRuntimeControl()
    {
        sessionTag = Guid.TryParse(Environment.GetEnvironmentVariable("OMSILAUNCH_SESSION_ID"), out var sessionId)
            ? sessionId.ToString("N") : "unbound";
        // Addresses come from the in-process resolution of the build catalog,
        // never from fixed profile constants. A global or layout that is not
        // verified is absent; Execute rejects every operation whose native
        // capability is unavailable before any reader or writer is reached.
        var globals = PluginNativeTargets.Globals();
        var layouts = PluginNativeTargets.Layouts();
        uint G(string name) => globals.TryGetValue(name, out var address) ? address : 0;
        time = new OmsiTimeAdapter(memory, new OmsiTimeLayout(G("TimeHour"), G("TimeMinute"), G("TimeSecond"), G("TimeDay"), G("TimeMonth"), G("TimeYear")));
        readers = new OmsiRuntimeReaders(memory, globals, layouts);
        weather = layouts.ContainsKey("Weather") ? new OmsiWeatherWriter(memory, globals, layouts) : null!;
        camera = layouts.ContainsKey("Camera") ? new OmsiCameraWriter(memory, globals, layouts) : null!;
        cameraLock = new OmsiCameraLockWriter(memory, globals, layouts);
    }

    public RuntimeCommandResult Execute(RuntimeCommand command)
    {
        if (!PluginDiagnostics.PluginTrace) return ExecuteOperation(command);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var result = ExecuteOperation(command);
        var elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1_000_000 / System.Diagnostics.Stopwatch.Frequency;
        PluginDiagnostics.Operation(command.Operation, result.Succeeded, result.ErrorCode, elapsed);
        return result;
    }

    // OMSI's own exit routine runs from a native one-shot timer after this
    // answer is sent (it unloads the plugin on the way out).
    private static IReadOnlyDictionary<string, string> RequestCooperativeExit()
    {
        if (!PluginNativeTargets.IsAvailable("session.cooperative-exit")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        if (NativeRequestCooperativeExit() != 1) throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE");
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["scheduled"] = "true" };
    }

    [System.Runtime.InteropServices.DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private static extern int NativeRequestCooperativeExit();

    private RuntimeCommandResult ExecuteOperation(RuntimeCommand command)
    {
        // Per-capability gate: an operation whose native targets were not
        // resolved and verified on this build never touches OMSI memory.
        if (PublicCapabilityRegistry.NativeCapabilityOf(command.Operation) is { } capability && !PluginNativeTargets.IsAvailable(capability))
            return new RuntimeCommandResult(command.SessionId, command.RequestId, false, PublicErrorCodes.CAPABILITY_UNRESOLVED,
                new Dictionary<string, string> { ["detail"] = "The native capability '" + capability + "' is not resolved on this OMSI build.", ["capability"] = capability });
        try
        {
            IReadOnlyDictionary<string, string> values = command.Operation switch
            {
                // Internal (not a public operation): the host's cooperative exit.
                "session.close" => RequestCooperativeExit(),
                "time.read" => ReadTime(),
                "time.set" => SetTime(command.Arguments),
                "map.read" => readers.ReadMapAsync().AsTask().GetAwaiter().GetResult(),
                "map.tiles.list" => readers.ListMapTilesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "weather.read" => readers.ReadWeatherAsync().AsTask().GetAwaiter().GetResult(),
                "weather.set" => SetWeather(command.Arguments),
                "weather.metar.set" => SetMetarWeather(command.Arguments),
                "weather.actual.read" => readers.ReadActualWeatherAsync().AsTask().GetAwaiter().GetResult(),
                "camera.read" => readers.ReadCameraAsync().AsTask().GetAwaiter().GetResult(),
                "camera.set" => SetCamera(command.Arguments),
                "camera.lock" => LockCamera(command.Arguments),
                "camera.unlock" => UnlockCamera(),
                "d3d.status" => ReadD3DStatus(),
                "d3d.texture.create" => CreateD3DTexture(command.Arguments),
                "d3d.texture.describe" => DescribeD3DTexture(command.Arguments),
                "d3d.texture.update" => UpdateD3DTexture(command.Arguments),
                "d3d.texture.release" => ReleaseD3DTexture(command.Arguments),
                "road-vehicles.read" => readers.ReadRoadVehiclesAsync().AsTask().GetAwaiter().GetResult(),
                "road-vehicles.list" => readers.ListRoadVehiclesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                // The internal route retains its native diagnostic address for
                // research. The public spawn route projects only an opaque,
                // session-scoped RoadVehicle handle.
                "internal.road-vehicles.make-basic" => MakeBasicRoadVehicle(command.Arguments, includeNativeAddress: true),
                "road-vehicles.spawn" => SpawnRoadVehicle(command.Arguments),
                "road-vehicles.place-random" => PlaceRandomBus(command.Arguments),
                "road-vehicle.read" => ReadRoadVehicle(command.Arguments),
                "vehicle.variable.get" => ReadRoadVehicleVariable(command.Arguments),
                "vehicle.variable.set" => WriteRoadVehicleVariable(command.Arguments),
                "vehicle.variables.list" => ListRoadVehicleVariables(command.Arguments, false),
                "vehicle.string-variable.get" => ReadRoadVehicleStringVariable(command.Arguments),
                "vehicle.string-variable.set" => WriteRoadVehicleStringVariable(command.Arguments),
                "vehicle.trigger" => TriggerRoadVehicle(command.Arguments),
                "vehicle.sound-trigger" => SoundTriggerRoadVehicle(command.Arguments),
                "vehicle.string-variables.list" => ListRoadVehicleVariables(command.Arguments, true),
                "vehicle.constant.get" => ReadRoadVehicleConstant(command.Arguments),
                "vehicle.constants.list" => ListRoadVehicleConstants(command.Arguments, false),
                "vehicle.curve.evaluate" => EvaluateRoadVehicleCurve(command.Arguments),
                "vehicle.curves.list" => ListRoadVehicleConstants(command.Arguments, true),
                "vehicle.hofs.read" => ReadRoadVehicleHofs(command.Arguments),
                "player-vehicle.read" => readers.ReadPlayerVehicleAsync().AsTask().GetAwaiter().GetResult(),
                "humans.read" => readers.ReadHumansAsync().AsTask().GetAwaiter().GetResult(),
                "humans.list" => readers.ListHumansAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "human.read" => ReadHuman(command.Arguments),
                "timetable.read" => readers.ReadTimeTableAsync().AsTask().GetAwaiter().GetResult(),
                "timetable.no-rv-numbers.list" => readers.ListNoRvNumbersAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.tracks.list" => readers.ListTimeTableTracksAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.trips.list" => readers.ListTimeTableTripsAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.lines.list" => readers.ListTimeTableLinesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.rv-files.list" => readers.ListTimeTableRvFilesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.track-entries.list" => readers.ListTimeTableTrackEntriesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.bus-stops.list" => readers.ListTimeTableBusStopsAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.station-links.list" => readers.ListTimeTableStationLinksAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.tours.list" => readers.ListTimeTableToursAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.profiles.list" => readers.ListTimeTableProfilesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.tour-entries.list" => readers.ListTimeTableTourEntriesAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "drivers.read" => readers.ReadDriversAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "tickets.read" => readers.ReadTicketPackAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                "timetable.logs.read" => readers.ReadTimeTableLogsAsync(ListPage.From(command.Arguments)).AsTask().GetAwaiter().GetResult(),
                _ => throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE")
            };
            return new RuntimeCommandResult(command.SessionId, command.RequestId, true, Values: values);
        }
        catch (RuntimeOperationException exception)
        {
            return new RuntimeCommandResult(command.SessionId, command.RequestId, false, exception.Code,
                new Dictionary<string, string> { ["detail"] = exception.Message });
        }
        catch (D3DOperationException exception)
        {
            return new RuntimeCommandResult(command.SessionId, command.RequestId, false, exception.Code,
                new Dictionary<string, string> { ["detail"] = exception.Message, ["native_status"] = exception.NativeStatus.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }
        catch (Exception exception)
        {
            // A typed public code carried by any exception survives to the
            // caller (R4); only an untyped failure becomes OPERATION_FAILED.
            var code = PublicErrorCodes.FromMessage(exception.Message) ?? "OL_E_RUNTIME_OPERATION_FAILED";
            return new RuntimeCommandResult(command.SessionId, command.RequestId, false, code, new Dictionary<string, string> { ["detail"] = exception.Message, ["exception"] = exception.GetType().Name });
        }
    }

    public IReadOnlyList<PluginRuntimeEvent> PollLifecycle()
    {
        var events = new List<PluginRuntimeEvent>();
        var now = Environment.TickCount64;
        if (activeCameraLock is { } policy && now >= Interlocked.Read(ref nextCameraLockPoll))
        {
            Interlocked.Exchange(ref nextCameraLockPoll, now + 100);
            try
            {
                cameraLock.ApplyAsync(policy.Family, policy.Preset).AsTask().GetAwaiter().GetResult();
                lastCameraLockError = null;
            }
            catch (Exception exception)
            {
                if (!string.Equals(lastCameraLockError, exception.Message, StringComparison.Ordinal))
                {
                    lastCameraLockError = exception.Message;
                    events.Add(new PluginRuntimeEvent("camera.lock.degraded", new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["code"] = exception.Message
                    }));
                }
            }
        }
        if (!d3DActivated) return events;
        if (pendingD3DEvent is { } pending)
        {
            pendingD3DEvent = null;
            events.Add(pending);
            return events;
        }
        if (now < Interlocked.Read(ref nextD3DPoll)) return events;
        // The native device probe runs at most every 250 ms; events reach
        // the host through the telemetry ring whatever their spacing.
        Interlocked.Exchange(ref nextD3DPoll, now + 250);
        NativeD3DProbe(out var status);
        var name = status.LifecycleTransition switch
        {
            1 => "d3d.ready",
            2 => "d3d.lost",
            3 => "d3d.resetting",
            4 => "d3d.restored",
            5 => "d3d.stopped",
            _ => null
        };
        if (name is not null)
            events.Add(new PluginRuntimeEvent(name, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["state"] = D3DStateName(status.LifecycleState),
                ["generation"] = status.DeviceGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["execution_thread_id"] = status.ExecutionThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["live_textures"] = status.LiveTextureCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));
        return events;
    }

    public void ActivateDirect3D() => ActivateD3D();
    public void Shutdown() => NativeD3DShutdown();
    private IReadOnlyDictionary<string, string> ReadTime()
    {
        var snapshot = time.ReadAsync().AsTask().GetAwaiter().GetResult();
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["hour"] = snapshot.Hour.ToString(System.Globalization.CultureInfo.InvariantCulture), ["minute"] = snapshot.Minute.ToString(System.Globalization.CultureInfo.InvariantCulture), ["second"] = snapshot.Second.ToString(System.Globalization.CultureInfo.InvariantCulture), ["day"] = snapshot.Day.ToString(System.Globalization.CultureInfo.InvariantCulture), ["month"] = snapshot.Month.ToString(System.Globalization.CultureInfo.InvariantCulture), ["year"] = snapshot.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    private IReadOnlyDictionary<string, string> SetTime(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        byte? hour = ParseByte(arguments, "hour", 0, 23);
        byte? minute = ParseByte(arguments, "minute", 0, 59);
        float? second = ParseFloat(arguments, "second", 0, 59.999f);
        if (hour is null && minute is null && second is null) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        time.WriteClockAsync(hour, minute, second).AsTask().GetAwaiter().GetResult();
        if (NativeRuntimeApplyTime() == 0) throw new InvalidOperationException("OL_E_TIME_APPLY_FAILED");
        return ReadTime();
    }

    // Manual weather: every value is validated before anything is written;
    // live METAR updates stop first, otherwise OMSI replaces the record on its
    // next download. OMSI derives the per-tick values (fog, light, actual
    // wind, humidity) from the record on its next unpaused weather tick.
    private IReadOnlyDictionary<string, string> SetWeather(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || arguments.Count == 0) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (weather is null || !PluginNativeTargets.IsAvailable("weather.set")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        arguments.TryGetValue("cloud_type", out var cloudType);
        var values = weather.Prepare(arguments.Where(pair => pair.Key != "cloud_type").ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        if (cloudType is not null && (cloudType.Length is 0 or > 64 || cloudType.Any(char.IsControl))) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", "cloud_type");
        weather.WriteAsync(values).AsTask().GetAwaiter().GetResult();
        var result = new Dictionary<string, string>(readers.ReadWeatherAsync().AsTask().GetAwaiter().GetResult(), StringComparer.Ordinal);
        if (cloudType is not null)
        {
            var status = NativeSetWeatherCloudType(cloudType);
            if (status == 3) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", "cloud_type");
            if (status == 0) throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE");
            result["cloud_type_name"] = cloudType;
            // "-1" is OMSI's own "no clouds"; any other unknown name is drawn as none.
            result["cloud_type_resolved"] = status == 1 ? "true" : "false";
        }
        return result;
    }

    // Live METAR weather for an ICAO station, as OMSI's weather dialog sets it:
    // OMSI downloads and applies it on its next weather update.
    private IReadOnlyDictionary<string, string> SetMetarWeather(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("icao", out var icao) || arguments.Count != 1) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (weather is null || !PluginNativeTargets.IsAvailable("weather.metar.set")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        icao = icao.ToUpperInvariant();
        if (icao.Length != 4 || !icao.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9')) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", "icao");
        var status = NativeSetMetarIcao(icao);
        if (status == 3) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", "icao");
        if (status != 1) throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE");
        weather.RequestMetarAsync().AsTask().GetAwaiter().GetResult();
        return readers.ReadActualWeatherAsync().AsTask().GetAwaiter().GetResult();
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeSetWeatherCloudType(string name);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeSetMetarIcao(string icao);

    private IReadOnlyDictionary<string, string> SetCamera(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        camera.WriteAsync(arguments).AsTask().GetAwaiter().GetResult();
        return readers.ReadCameraAsync().AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> LockCamera(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("family", out var rawFamily) ||
            !int.TryParse(rawFamily, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var family))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        int? preset = null;
        if (arguments.TryGetValue("preset", out var rawPreset))
        {
            if (!int.TryParse(rawPreset, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsedPreset))
                throw new ArgumentOutOfRangeException("preset", "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
            preset = parsedPreset;
        }
        cameraLock.ApplyAsync(family, preset).AsTask().GetAwaiter().GetResult();
        activeCameraLock = new CameraLockPolicy(family, preset);
        lastCameraLockError = null;
        Interlocked.Exchange(ref nextCameraLockPoll, Environment.TickCount64 + 100);
        return OmsiCameraLockWriter.Describe(family, preset);
    }

    private IReadOnlyDictionary<string, string> UnlockCamera()
    {
        activeCameraLock = null;
        lastCameraLockError = null;
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["locked"] = "false" };
    }

    private IReadOnlyDictionary<string, string> ReadD3DStatus()
    {
        var nativeStatus = NativeD3DProbe(out var status);
        CaptureD3DTransition(status);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["available"] = (nativeStatus == 0).ToString().ToLowerInvariant(),
            ["native_status"] = nativeStatus.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["query_interface_hresult"] = $"0x{unchecked((uint)status.QueryInterfaceResult):X8}",
            ["cooperative_level_hresult"] = $"0x{unchecked((uint)status.CooperativeLevelResult):X8}",
            ["execution_thread_id"] = status.ExecutionThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["owned_device_references"] = status.OwnedDeviceReferences.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["state"] = D3DStateName(status.LifecycleState),
            ["transition"] = D3DTransitionName(status.LifecycleTransition),
            ["generation"] = status.DeviceGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["live_textures"] = status.LiveTextureCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["reset_hook_installed"] = (status.ResetHookInstalled != 0).ToString().ToLowerInvariant(),
            ["last_reset_thread_id"] = status.LastResetThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["binding"] = "Omsi23004_692EBFBF:D3DDevice"
        };
    }

    private IReadOnlyDictionary<string, string> CreateD3DTexture(IReadOnlyDictionary<string, string>? arguments)
    {
        ActivateD3D();
        var width = ParseUInt(arguments, "width", 1, 4096);
        var height = ParseUInt(arguments, "height", 1, 4096);
        var levels = ParseUInt(arguments, "levels", 0, 16, 1);
        var formatName = ReadRequired(arguments, "format").ToUpperInvariant();
        var format = formatName switch
        {
            "A8R8G8B8" => 21u, "X8R8G8B8" => 22u, "R5G6B5" => 23u,
            "X1R5G5B5" => 24u, "A1R5G5B5" => 25u, "A4R4G4B4" => 26u,
            "A8" => 28u, "A8L8" => 51u, "L8" => 50u,
            _ => throw new D3DOperationException("OL_E_D3D_INVALID_TEXTURE_FORMAT", 6, formatName)
        };
        var status = NativeD3DTextureCreate(width, height, format, levels, out var result);
        ThrowD3D(status, result, "CreateTexture");
        return D3DTextureValues(result, formatName);
    }

    private IReadOnlyDictionary<string, string> DescribeD3DTexture(IReadOnlyDictionary<string, string>? arguments)
    {
        ActivateD3D();
        var handle = ParseHandle(arguments);
        var level = ParseUInt(arguments, "level", 0, 15, 0);
        var status = NativeD3DTextureDescribe(handle, level, out var result);
        ThrowD3D(status, result, "GetLevelDesc");
        return D3DTextureValues(result, D3DFormatName(result.Format));
    }

    private IReadOnlyDictionary<string, string> UpdateD3DTexture(IReadOnlyDictionary<string, string>? arguments)
    {
        ActivateD3D();
        var handle = ParseHandle(arguments);
        var level = ParseUInt(arguments, "level", 0, 15, 0);
        var x = ParseUInt(arguments, "x", 0, 4095, 0);
        var y = ParseUInt(arguments, "y", 0, 4095, 0);
        var width = ParseUInt(arguments, "width", 1, 4096);
        var height = ParseUInt(arguments, "height", 1, 4096);
        byte[] pixels;
        try { pixels = Convert.FromBase64String(ReadRequired(arguments, "pixels_base64")); }
        catch (FormatException exception) { throw new D3DOperationException("OL_E_D3D_INVALID_PIXEL_BUFFER", 14, exception.Message); }
        if (pixels.Length > 48 * 1024) throw new D3DOperationException("OL_E_D3D_INVALID_PIXEL_BUFFER", 14, "Runtime mailbox pixel payload exceeds 48 KiB.");
        var status = NativeD3DTextureUpdate(handle, level, x, y, width, height, pixels, checked((uint)pixels.Length), out var result);
        ThrowD3D(status, result, "LockRect/UnlockRect");
        return D3DTextureValues(result, D3DFormatName(result.Format));
    }

    private IReadOnlyDictionary<string, string> ReleaseD3DTexture(IReadOnlyDictionary<string, string>? arguments)
    {
        ActivateD3D();
        var handle = ParseHandle(arguments);
        var status = NativeD3DTextureRelease(handle, out var result);
        ThrowD3D(status, result, "Release");
        return D3DTextureValues(result, D3DFormatName(result.Format));
    }

    private IReadOnlyDictionary<string, string> D3DTextureValues(NativeD3DTextureResult result, string format) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["handle"] = $"d3dtex-{sessionTag}-{result.Handle:X16}",
            ["state"] = result.TextureState switch { 1 => "LIVE", 2 => "RELEASED", 3 => "STALE", _ => "UNKNOWN" },
            ["device_state"] = D3DStateName(result.LifecycleState),
            ["generation"] = result.DeviceGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["width"] = result.Width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["height"] = result.Height.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["format"] = format,
            ["levels"] = result.Levels.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["level"] = result.Level.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["level_width"] = result.LevelWidth.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["level_height"] = result.LevelHeight.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["hresult"] = $"0x{unchecked((uint)result.OperationResult):X8}",
            ["execution_thread_id"] = result.ExecutionThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

    private static void ThrowD3D(int status, NativeD3DTextureResult result, string operation)
    {
        if (status == 0) return;
        var code = status switch
        {
            6 => "OL_E_D3D_INVALID_TEXTURE_FORMAT",
            7 => "OL_E_D3D_NOT_READY",
            8 => "OL_E_D3D_DEVICE_LOST",
            9 => "OL_E_D3D_RESET_IN_PROGRESS",
            12 => "OL_E_D3D_STALE_RESOURCE_HANDLE",
            13 => "OL_E_D3D_RESOURCE_RELEASED",
            14 => "OL_E_D3D_INVALID_ARGUMENT",
            _ => "OL_E_D3D_NATIVE_CALL_FAILED"
        };
        throw new D3DOperationException(code, status, $"{operation} failed with HRESULT 0x{unchecked((uint)result.OperationResult):X8}.");
    }

    private static string ReadRequired(IReadOnlyDictionary<string, string>? arguments, string name) =>
        arguments is not null && arguments.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new D3DOperationException("OL_E_D3D_INVALID_ARGUMENT", 14, $"Missing {name}.");

    private static uint ParseUInt(IReadOnlyDictionary<string, string>? arguments, string name, uint minimum, uint maximum, uint? fallback = null)
    {
        if (arguments is null || !arguments.TryGetValue(name, out var raw))
        {
            if (fallback.HasValue) return fallback.Value;
            throw new D3DOperationException("OL_E_D3D_INVALID_ARGUMENT", 14, $"Missing {name}.");
        }
        if (!uint.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
            throw new D3DOperationException("OL_E_D3D_INVALID_ARGUMENT", 14, $"Invalid {name}.");
        return value;
    }

    private ulong ParseHandle(IReadOnlyDictionary<string, string>? arguments)
    {
        var value = ReadRequired(arguments, "handle");
        var prefix = $"d3dtex-{sessionTag}-";
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || !ulong.TryParse(value.AsSpan(prefix.Length), System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out var handle) || handle == 0)
            throw new D3DOperationException("OL_E_D3D_STALE_RESOURCE_HANDLE", 12, "Invalid texture handle.");
        return handle;
    }

    private static string D3DStateName(uint state) => state switch { 0 => "NOT_READY", 1 => "READY", 2 => "LOST", 3 => "RESETTING", 4 => "STOPPING", 5 => "STOPPED", _ => "UNKNOWN" };
    private static string D3DTransitionName(uint transition) => transition switch { 0 => "NONE", 1 => "READY", 2 => "LOST", 3 => "RESETTING", 4 => "RESTORED", 5 => "STOPPED", _ => "UNKNOWN" };
    private static string D3DFormatName(uint format) => format switch { 21 => "A8R8G8B8", 22 => "X8R8G8B8", 23 => "R5G6B5", 24 => "X1R5G5B5", 25 => "A1R5G5B5", 26 => "A4R4G4B4", 28 => "A8", 50 => "L8", 51 => "A8L8", _ => $"D3DFMT_{format}" };

    private void ActivateD3D()
    {
        if (d3DActivated) return;
        NativeD3DProbe(out var status);
        CaptureD3DTransition(status);
    }

    private void CaptureD3DTransition(NativeD3DStatus status)
    {
        d3DActivated = true;
        var name = status.LifecycleTransition switch { 1 => "d3d.ready", 2 => "d3d.lost", 3 => "d3d.resetting", 4 => "d3d.restored", 5 => "d3d.stopped", _ => null };
        if (name is null) return;
        pendingD3DEvent = new PluginRuntimeEvent(name, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["state"] = D3DStateName(status.LifecycleState),
            ["generation"] = status.DeviceGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["execution_thread_id"] = status.ExecutionThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["live_textures"] = status.LiveTextureCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    private IReadOnlyDictionary<string, string> ReadRoadVehicle(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || string.IsNullOrWhiteSpace(handle))
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED");
        return readers.ReadRoadVehicleAsync(handle).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> SpawnRoadVehicle(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("model", out var model)) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return MakeBasicRoadVehicle(new Dictionary<string, string>(StringComparer.Ordinal) { ["bus"] = model }, includeNativeAddress: false);
    }

    private IReadOnlyDictionary<string, string> MakeBasicRoadVehicle(IReadOnlyDictionary<string, string>? arguments, bool includeNativeAddress)
    {
        if (arguments is null || !arguments.TryGetValue("bus", out var bus)) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        ValidateBasicBusIdentity(bus);
        // OMSI's native MakeVehicle accepts a missing path and can choose a
        // fallback vehicle. Reject it before the call so a requested identity
        // can never be reported as a false successful creation.
        if (!InstallationPaths.TryGetContainedRelativePath(Environment.CurrentDirectory, bus, out var contained) || !File.Exists(Path.Combine(InstallationPaths.NormalizeRoot(Environment.CurrentDirectory), contained)))
            throw new InvalidOperationException("OL_E_MAKEVEHICLE_BUS_NOT_FOUND");
        var status = NativeMakeVehicleBasic(bus, out var native);
        if (status != 0)
        {
            var error = native.Status switch
            {
                9 => "OL_E_MAKEVEHICLE_DELTA_ZERO",
                10 => "OL_E_MAKEVEHICLE_DELTA_MULTIPLE",
                _ => "OL_E_MAKEVEHICLE_NATIVE_FAILED"
            };
            throw new InvalidOperationException($"{error}:native_status={native.Status};raw_return={native.RawReturn};before={native.BeforeCount};after={native.AfterCount};delta={native.DeltaCount}");
        }
        var handle = readers.RegisterRoadVehicleHandleAsync(native.CreatedVehicle).AsTask().GetAwaiter().GetResult();
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["bus"] = bus,
            ["created_handle"] = handle,
            ["before_count"] = native.BeforeCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["after_count"] = native.AfterCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["delta_count"] = native.DeltaCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["raw_native_return"] = native.RawReturn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["identity_validation"] = "collection-delta-and-profiled-vmt"
        };
        if (includeNativeAddress) result["internal_created_native_address"] = $"0x{native.CreatedVehicle:X8}";
        return result;
    }

    private static void ValidateBasicBusIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 240 || value.IndexOf('\0') >= 0 || value.Contains("..", StringComparison.Ordinal) ||
            !value.Replace('/', '\\').StartsWith("Vehicles\\", StringComparison.OrdinalIgnoreCase) || !value.EndsWith(".bus", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("OL_E_RUNTIME_BUS_IDENTITY_INVALID", nameof(value));
    }

    private IReadOnlyDictionary<string, string> PlaceRandomBus(IReadOnlyDictionary<string, string>? arguments)
    {
        static int Read(IReadOnlyDictionary<string, string>? values, string name, int fallback, int minimum, int maximum)
        {
            if (values is null || !values.TryGetValue(name, out var raw)) return fallback;
            if (!int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
            return value;
        }
        var aiType = Read(arguments, "ai_type", 0, 0, 255); var group = Read(arguments, "group", 1, 0, 65_535);
        var type = Read(arguments, "type", -1, -1, 65_535); var scheduled = Read(arguments, "scheduled", 0, 0, 1);
        var tour = Read(arguments, "tour", 0, 0, 65_535); var line = Read(arguments, "line", 0, 0, 65_535);
        if (NativePlaceRandomBus(aiType, group, type, scheduled, tour, line, out var rawReturn, out var before, out var after) != 0)
            throw new InvalidOperationException("OL_E_PLACE_RANDOM_BUS_FAILED");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["raw_return"] = rawReturn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["before_count"] = before.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["after_count"] = after.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["delta_count"] = (after - before).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["identity_validation"] = "native-placement-return-is-diagnostic-only"
        };
    }

    private IReadOnlyDictionary<string, string> ReadRoadVehicleVariable(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return readers.ReadRoadVehicleVariableAsync(handle, name).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> WriteRoadVehicleVariable(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name) || !arguments.TryGetValue("value", out var rawValue))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (!float.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
            throw new ArgumentOutOfRangeException("value", "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        return readers.WriteRoadVehicleVariableAsync(handle, name, value).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> ReadRoadVehicleStringVariable(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return readers.ReadRoadVehicleStringVariableAsync(handle, name).AsTask().GetAwaiter().GetResult();
    }

    // OMSI's own SetStringVariable (it assigns a copy; the vehicle script reads
    // the new value on its next frame).
    private IReadOnlyDictionary<string, string> WriteRoadVehicleStringVariable(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name) || !arguments.TryGetValue("value", out var value))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (!PluginNativeTargets.IsAvailable("scripts.string.write")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        if (name.Length is 0 or > 128 || value.Length > 1024 || value.Contains('\0')) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", nameof(value));
        var address = readers.ResolveScriptTargetAsync(handle, stringVariable: name).AsTask().GetAwaiter().GetResult();
        NativeScriptStatus(NativeSetScriptStringVariable((nint)address, name, value));
        return readers.ReadRoadVehicleStringVariableAsync(handle, name).AsTask().GetAwaiter().GetResult();
    }

    // A named script trigger, as OMSI's input handlers raise it: value 1
    // presses (runs "name"), 0 releases ("name_off"), 2 drags ("name_drag").
    // A press is normally followed by a release. Unknown trigger names do
    // nothing in OMSI.
    private IReadOnlyDictionary<string, string> TriggerRoadVehicle(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name) || !arguments.TryGetValue("value", out var rawValue))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (!PluginNativeTargets.IsAvailable("scripts.trigger")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        if (!int.TryParse(rawValue, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) || value is < 0 or > 2)
            throw new ArgumentOutOfRangeException("value", "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        if (name.Length is 0 or > 128) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", nameof(name));
        var address = readers.ResolveScriptTargetAsync(handle).AsTask().GetAwaiter().GetResult();
        NativeScriptStatus(NativeScriptTrigger((nint)address, name, value));
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["name"] = name, ["value"] = rawValue, ["dispatched"] = "true" };
    }

    private IReadOnlyDictionary<string, string> SoundTriggerRoadVehicle(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("trigger", out var trigger))
            throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        if (!PluginNativeTargets.IsAvailable("scripts.trigger")) throw new InvalidOperationException(PublicErrorCodes.CAPABILITY_UNRESOLVED);
        // OMSI's own callers pass an empty file name for "the trigger's sounds".
        arguments.TryGetValue("file", out var file);
        file ??= "";
        if (trigger.Length is 0 or > 128 || file.Length > 260) throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", nameof(trigger));
        var address = readers.ResolveScriptTargetAsync(handle, requireSounds: true).AsTask().GetAwaiter().GetResult();
        NativeScriptStatus(NativeScriptSoundTrigger((nint)address, trigger, file));
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["trigger"] = trigger, ["file"] = file, ["dispatched"] = "true" };
    }

    private static void NativeScriptStatus(int status)
    {
        switch (status)
        {
            case 1: return;
            case 3: throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID: not representable in OMSI's string encoding");
            case 4: throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE: the vehicle's script method is not the profiled one");
            default: throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE");
        }
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeSetScriptStringVariable(nint instance, string name, string value);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeScriptTrigger(nint instance, string name, int value);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeScriptSoundTrigger(nint instance, string trigger, string filename);

    private IReadOnlyDictionary<string, string> ReadRoadVehicleConstant(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name)) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return readers.ReadRoadVehicleConstantAsync(handle, name).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> ListRoadVehicleConstants(IReadOnlyDictionary<string, string>? arguments, bool curves)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle)) throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED");
        return readers.ListRoadVehicleConstantsAsync(handle, curves, ListPage.From(arguments)).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> EvaluateRoadVehicleCurve(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || !arguments.TryGetValue("name", out var name) || !arguments.TryGetValue("x", out var rawX) ||
            !float.TryParse(rawX, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return readers.EvaluateRoadVehicleCurveAsync(handle, name, x).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> ReadRoadVehicleHofs(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle)) throw new InvalidOperationException("OL_E_RUNTIME_ARGUMENT_REQUIRED");
        return readers.ReadRoadVehicleHofsAsync(handle, ListPage.From(arguments)).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> ListRoadVehicleVariables(IReadOnlyDictionary<string, string>? arguments, bool stringVariables)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle)) throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED");
        return readers.ListRoadVehicleVariablesAsync(handle, stringVariables, ListPage.From(arguments)).AsTask().GetAwaiter().GetResult();
    }

    private IReadOnlyDictionary<string, string> ReadHuman(IReadOnlyDictionary<string, string>? arguments)
    {
        if (arguments is null || !arguments.TryGetValue("handle", out var handle) || string.IsNullOrWhiteSpace(handle))
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED");
        return readers.ReadHumanAsync(handle).AsTask().GetAwaiter().GetResult();
    }

    private static byte? ParseByte(IReadOnlyDictionary<string, string> arguments, string name, byte min, byte max)
    {
        if (!arguments.TryGetValue(name, out var value)) return null;
        if (!byte.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed < min || parsed > max)
            throw new ArgumentOutOfRangeException(name, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        return parsed;
    }

    private static float? ParseFloat(IReadOnlyDictionary<string, string> arguments, string name, float min, float max)
    {
        if (!arguments.TryGetValue(name, out var value)) return null;
        if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) || float.IsNaN(parsed) || float.IsInfinity(parsed) || parsed < min || parsed > max)
            throw new ArgumentOutOfRangeException(name, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        return parsed;
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeRuntimeApplyTime();

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int NativeMakeVehicleBasic(string busIdentity, out NativeMakeVehicleDiagnostics diagnostics);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativePlaceRandomBus(int aiType, int group, int type, int scheduled, int tour, int line, out int rawReturn, out int beforeCount, out int afterCount);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeD3DProbe(out NativeD3DStatus status);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeD3DTextureCreate(uint width, uint height, uint format, uint levels, out NativeD3DTextureResult result);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeD3DTextureDescribe(ulong handle, uint level, out NativeD3DTextureResult result);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeD3DTextureUpdate(ulong handle, uint level, uint x, uint y, uint width, uint height, byte[] pixels, uint pixelBytes, out NativeD3DTextureResult result);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeD3DTextureRelease(ulong handle, out NativeD3DTextureResult result);

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void NativeD3DShutdown();

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeD3DStatus
    {
        public int Status;
        public nuint Slot;
        public nuint Candidate;
        public int QueryInterfaceResult;
        public int CooperativeLevelResult;
        public uint ExecutionThreadId;
        public uint OwnedDeviceReferences;
        public uint LifecycleState;
        public uint LifecycleTransition;
        public uint DeviceGeneration;
        public uint LiveTextureCount;
        public uint ResetHookInstalled;
        public uint LastResetThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeD3DTextureResult
    {
        public int Status;
        public int OperationResult;
        public ulong Handle;
        public uint LifecycleState;
        public uint DeviceGeneration;
        public uint TextureState;
        public uint Width;
        public uint Height;
        public uint Format;
        public uint Levels;
        public uint Level;
        public uint LevelWidth;
        public uint LevelHeight;
        public uint ExecutionThreadId;
    }

    private sealed class D3DOperationException : Exception
    {
        public D3DOperationException(string code, int nativeStatus, string message) : base(message) { Code = code; NativeStatus = nativeStatus; }
        public string Code { get; }
        public int NativeStatus { get; }
    }

    private sealed class RuntimeOperationException : Exception
    {
        public RuntimeOperationException(string code, string message) : base(message) => Code = code;
        public string Code { get; }
    }

    private sealed record CameraLockPolicy(int Family, int? Preset);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMakeVehicleDiagnostics
    {
        public int Status;
        public int RawReturn;
        public int BeforeCount;
        public int AfterCount;
        public int DeltaCount;
        public int CopyReturn;
        public uint ProgMan;
        public uint RoadVehicles;
        public uint RoadVehicleTypes;
        public uint CriticalSection;
        public uint TemporaryList;
        public uint CreatedVehicle;
        public uint CreatedVehicleVmt;
    }
}

internal sealed class CurrentRuntimeCommandMailbox
{
    private const int Capacity = 65_536;
    private const int StateOffset = 0;
    private const int LengthOffset = 4;
    private const int DataOffset = 8;
    private readonly string name;
    private readonly Guid sessionId;
    public CurrentRuntimeCommandMailbox(string name, Guid sessionId) { this.name = name; this.sessionId = sessionId; }

    public bool TryDispatch(IPluginRuntimeControl control)
    {
        try
        {
            using var mapping = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWrite);
            using var view = mapping.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
            if (view.ReadInt32(StateOffset) != 1) return false;
            var length = view.ReadInt32(LengthOffset);
            if (length <= 0 || length > Capacity - DataOffset) { view.Write(StateOffset, 0); view.Flush(); return false; }
            var input = new byte[length]; view.ReadArray(DataOffset, input, 0, length);
            if (!RuntimeCommandWire.TryDeserializeRequest(input, out var command) || command is null)
            {
                // Undecodable: there is no request identity to answer. The slot
                // is released; the host's own cleanup covers the timeout.
                view.Write(StateOffset, 0); view.Write(LengthOffset, 0); view.Flush(); return false;
            }
            byte[] output;
            if (command.SessionId != sessionId) output = Error(command, "OL_E_RUNTIME_SESSION_MISMATCH");
            else
            {
                // Nothing an operation does may escape into OMSI's UI timer or
                // leave the request unanswered: failures become a typed result.
                RuntimeCommandResult? result = null;
                try { result = control.Execute(command); output = RuntimeCommandWire.SerializeResponse(result); }
                catch (Exception exception) when (exception is not OutOfMemoryException) { output = Error(command, "OL_E_RUNTIME_OPERATION_FAILED"); }
                // A bounded list keeps its contract when the row bound alone does
                // not fit the slot (large maps): trailing rows are dropped and
                // the result says so (documentation audit BUG-05).
                if (output.Length > Capacity - DataOffset && result is not null && FitBoundedList(result, Capacity - DataOffset) is { } fitted) output = fitted;
                // Any other oversized result is reported as a typed error rather
                // than a silent reset that the host would misread as a timeout.
                // The error envelope itself carries no values and is always small.
                if (output.Length > Capacity - DataOffset) output = Error(command, "OL_E_RUNTIME_RESPONSE_TOO_LARGE");
            }
            // The host abandons a request on timeout by resetting the slot. A
            // response for an abandoned or superseded request must never be
            // published: it would wedge the channel as BUSY for the session.
            if (view.ReadInt32(StateOffset) != 1 || !RuntimeCommandWire.TryReadRequestId(ReadSlot(view), out var pendingId) || pendingId != command.RequestId) return false;
            view.Write(LengthOffset, output.Length); view.WriteArray(DataOffset, output, 0, output.Length); view.Write(StateOffset, 2); view.Flush(); return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static byte[] Error(RuntimeCommand command, string code) => RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(command.SessionId, command.RequestId, false, code));

    // An index-paged list result (it carries returned_count and truncated) with
    // rows keyed "<row>.<n>.<field>" by absolute record index is shrunk by
    // dropping its highest-numbered rows until the serialized response fits;
    // next_cursor then starts at the first dropped row, so no record is lost.
    // Returns null for any other result (handle lists page by handle order and
    // are bounded to fit).
    internal static byte[]? FitBoundedList(RuntimeCommandResult result, int capacity)
    {
        if (!result.Succeeded || result.Values is not { } values || !values.ContainsKey("returned_count") || !values.ContainsKey("truncated")) return null;
        if (values.Keys.Any(key => key.EndsWith(".handle", StringComparison.Ordinal) && key.Count(c => c == '.') == 2)) return null;
        static int? RowOf(string key)
        {
            var first = key.IndexOf('.'); if (first < 0) return null;
            var second = key.IndexOf('.', first + 1); var digits = second < 0 ? key[(first + 1)..] : key[(first + 1)..second];
            return int.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var row) ? row : null;
        }
        var rows = values.Keys.Select(RowOf).Where(row => row is not null).Select(row => row!.Value).Distinct().OrderBy(row => row).ToList();
        while (rows.Count > 0)
        {
            var dropped = rows.Count - Math.Max(1, rows.Count / 8);
            var firstDropped = rows[dropped];
            rows.RemoveRange(dropped, rows.Count - dropped);
            var keep = rows.ToHashSet();
            var trimmed = values.Where(pair => RowOf(pair.Key) is not { } row || keep.Contains(row)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            trimmed["returned_count"] = keep.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            trimmed["truncated"] = "true";
            trimmed["next_cursor"] = "i:" + firstDropped.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var bytes = RuntimeCommandWire.SerializeResponse(result with { Values = trimmed });
            if (bytes.Length <= capacity) return bytes;
        }
        return null;
    }

    private static byte[] ReadSlot(MemoryMappedViewAccessor view)
    {
        var length = view.ReadInt32(LengthOffset);
        if (length <= 0 || length > Capacity - DataOffset) return Array.Empty<byte>();
        var bytes = new byte[length]; view.ReadArray(DataOffset, bytes, 0, length); return bytes;
    }
}
