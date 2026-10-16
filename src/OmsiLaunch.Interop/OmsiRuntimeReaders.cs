namespace OmsiLaunch.Interop;

// Read-only object layouts are supplied by an exact OmsiBuildProfile. This
// adapter never publishes its object addresses: callers receive semantic data.
public sealed class OmsiRuntimeReaders
{
    private readonly IOmsiMemory memory;
    private readonly IReadOnlyDictionary<string, uint> globals;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts;
    // Handles are deliberately session-local aliases. Native addresses never
    // cross the PluginRuntime boundary and every use is revalidated against
    // the live OMSI collection. Each handle also pins an identity fingerprint
    // (VMT + type/definition identity) captured at allocation: a freed object
    // whose address is reused between two list reads is invisible to the
    // collection check alone, so resolution re-reads the fingerprint and
    // rejects the alias when the object at that address is no longer the one
    // the handle was issued for.
    private readonly Dictionary<uint, RoadVehicleHandleEntry> roadVehicleHandles = new();
    private readonly Dictionary<string, RoadVehicleHandleEntry> roadVehicleHandlesByToken = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, int> roadVehicleAddressGenerations = new();
    private HashSet<uint> activeRoadVehicleAddresses = new();
    private int nextRoadVehicleHandle;
    private readonly Dictionary<uint, HumanHandleEntry> humanHandles = new();
    private readonly Dictionary<string, HumanHandleEntry> humanHandlesByToken = new(StringComparer.Ordinal);
    private int nextHumanHandle;

    public OmsiRuntimeReaders(IOmsiMemory memory, IReadOnlyDictionary<string, uint> globals, IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts)
    {
        this.memory = memory; this.globals = globals; this.layouts = layouts;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadMapAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("Map", cancellationToken).ConfigureAwait(false); var layout = Layout("Map");
        // Map metadata strings are individually optional in OMSI. A malformed
        // optional display field must not hide the authoritative loaded state.
        var name = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Name"], cancellationToken)).ConfigureAwait(false);
        var filename = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Filename"], cancellationToken)).ConfigureAwait(false);
        var friendlyName = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["FriendlyName"], cancellationToken)).ConfigureAwait(false);
        var description = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Description"], cancellationToken)).ConfigureAwait(false);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["loaded"] = (await memory.ReadValueAsync<bool>(address + layout["Loaded"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["loaded_tiles"] = (await memory.ReadValueAsync<int>(address + layout["LoadedTiles"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["left_hand_traffic"] = (await memory.ReadValueAsync<bool>(address + layout["LeftHandTraffic"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["name"] = name,
            ["filename"] = filename,
            ["friendly_name"] = friendlyName,
            ["description"] = description,
            ["max_speed"] = (await memory.ReadValueAsync<float>(address + layout["MaxSpeed"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["year_start"] = (await memory.ReadValueAsync<int>(address + layout["YearStart"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["year_end"] = (await memory.ReadValueAsync<int>(address + layout["YearEnd"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListMapTilesAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var map = await ObjectAddressAsync("Map", cancellationToken).ConfigureAwait(false);
        var mapLayout = Layout("Map"); var tileLayout = Layout("MapTile");
        var tiles = await memory.ReadDelphiPointerArrayAsync(await memory.ReadPointer32Async(map + mapLayout["Tiles"], cancellationToken).ConfigureAwait(false), 4_096, cancellationToken).ConfigureAwait(false);
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        var end = (int)Math.Min(tiles.Length, (long)start + page.Take(256));
        ListPage.CompleteIndex(output, tiles.Length, Math.Min(start, tiles.Length), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
        {
            var address = tiles[index]; if (address == 0) continue;
            var prefix = $"tile.{index}.";
            try
            {
                output[prefix + "filename"] = await SafeStringAsync(() => ReadAnsiAsync(address + tileLayout["Filename"], cancellationToken)).ConfigureAwait(false);
                // The tile's grid position and its own index in the map's tile list
                // (research report beta4-static runtime-map-data-shutdown.md section 2).
                output[prefix + "x"] = await IntAsync(address, tileLayout, "X", cancellationToken).ConfigureAwait(false);
                output[prefix + "y"] = await IntAsync(address, tileLayout, "Y", cancellationToken).ConfigureAwait(false);
                output[prefix + "tile_index"] = await IntAsync(address, tileLayout, "Index", cancellationToken).ConfigureAwait(false);
                output[prefix + "version"] = await IntAsync(address, tileLayout, "Version", cancellationToken).ConfigureAwait(false);
                output[prefix + "unsaved"] = (await memory.ReadValueAsync<bool>(address + tileLayout["Unsaved"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "loaded"] = (await memory.ReadValueAsync<bool>(address + tileLayout["Loaded"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "load_requested"] = (await memory.ReadValueAsync<bool>(address + tileLayout["LoadRequest"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "failed"] = (await memory.ReadValueAsync<bool>(address + tileLayout["Failed"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "thread_loading"] = (await memory.ReadValueAsync<bool>(address + tileLayout["ThreadLoading"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "prepared_for_ode"] = (await memory.ReadValueAsync<bool>(address + tileLayout["PreparedForOde"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
                output[prefix + "width_south"] = await FloatAsync(address, tileLayout, "WidthSouth", cancellationToken).ConfigureAwait(false);
                output[prefix + "width_north"] = await FloatAsync(address, tileLayout, "WidthNorth", cancellationToken).ConfigureAwait(false);
                output[prefix + "objects"] = await OptionalArrayCountAsync(address + tileLayout["Objects"], cancellationToken).ConfigureAwait(false);
                output[prefix + "path_groups"] = await OptionalArrayCountAsync(address + tileLayout["PathGroups"], cancellationToken).ConfigureAwait(false);
                output[prefix + "path_segments"] = await OptionalArrayCountAsync(address + tileLayout["PathSegments"], cancellationToken).ConfigureAwait(false);
                output[prefix + "surface_objects"] = await OptionalArrayCountAsync(address + tileLayout["SurfaceObjects"], cancellationToken).ConfigureAwait(false);
                output[prefix + "spline_segments"] = await OptionalArrayCountAsync(address + tileLayout["SplineSegments"], cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is ArgumentOutOfRangeException or InvalidDataException or EndOfStreamException)
            {
                output[prefix + "state"] = "unavailable";
                output[prefix + "error"] = exception.GetType().Name;
            }
        }
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadWeatherAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("Weather", cancellationToken).ConfigureAwait(false); var layout = Layout("Weather");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["fog_density"] = await FloatAsync(address, layout, "FogDensity", cancellationToken).ConfigureAwait(false),
            ["lightness"] = await FloatAsync(address, layout, "Lightness", cancellationToken).ConfigureAwait(false),
            ["primary_light_factor"] = await FloatAsync(address, layout, "PrimaryLightFactor", cancellationToken).ConfigureAwait(false),
            ["secondary_light_factor"] = await FloatAsync(address, layout, "SecondaryLightFactor", cancellationToken).ConfigureAwait(false),
            ["ambient_light_factor"] = await FloatAsync(address, layout, "AmbientLightFactor", cancellationToken).ConfigureAwait(false),
            ["cloud_type"] = (await memory.ReadValueAsync<int>(address + layout["CloudType"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["cloud_transparency"] = await FloatAsync(address, layout, "CloudTransparency", cancellationToken).ConfigureAwait(false),
            ["precipitation_set"] = (await memory.ReadValueAsync<bool>(address + layout["PrecipitationSet"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["wet_ground"] = (await memory.ReadValueAsync<bool>(address + layout["WetGround"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["wind_speed"] = await FloatAsync(address, layout, "WindSpeed", cancellationToken).ConfigureAwait(false),
            ["wind_direction"] = await FloatAsync(address, layout, "WindDirection", cancellationToken).ConfigureAwait(false),
            ["relative_humidity"] = await FloatAsync(address, layout, "RelativeHumidity", cancellationToken).ConfigureAwait(false),
            ["absolute_humidity"] = await FloatAsync(address, layout, "AbsoluteHumidity", cancellationToken).ConfigureAwait(false)
            , ["temperature"] = await FloatAsync(address, layout, "ActiveTemperature", cancellationToken).ConfigureAwait(false)
            , ["dew_point"] = await FloatAsync(address, layout, "ActiveDewPoint", cancellationToken).ConfigureAwait(false)
            , ["pressure"] = await FloatAsync(address, layout, "ActivePressure", cancellationToken).ConfigureAwait(false)
            , ["precipitation"] = (await memory.ReadValueAsync<byte>(address + layout["ActivePrecipitation"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
            , ["precipitation_rate"] = (await memory.ReadValueAsync<byte>(address + layout["ActivePrecipitationRate"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
            // Source values (what weather.set writes); fog_density and
            // lightness above are their per-tick projections.
            , ["set.fog_density"] = await FloatAsync(address, layout, "ActiveFogDensity", cancellationToken).ConfigureAwait(false)
            , ["set.lightness"] = await FloatAsync(address, layout, "ActiveLightness", cancellationToken).ConfigureAwait(false)
            , ["cloud_type_name"] = await ReadAnsiAsync(address + layout["ActiveCloudType"], cancellationToken).ConfigureAwait(false) ?? string.Empty
            , ["cloud_height"] = await FloatAsync(address, layout, "ActiveCloudHeight", cancellationToken).ConfigureAwait(false)
            , ["ground_moisture"] = await FloatAsync(address, layout, "ActiveGroundMoisture", cancellationToken).ConfigureAwait(false)
            , ["ground_moisture_cover"] = await FloatAsync(address, layout, "ActiveGroundMoistureCover", cancellationToken).ConfigureAwait(false)
            , ["ground_wetness"] = await FloatAsync(address, layout, "ActiveGroundWetness", cancellationToken).ConfigureAwait(false)
            , ["snow"] = (await memory.ReadValueAsync<bool>(address + layout["ActiveSnow"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant()
            , ["snow_on_roads"] = (await memory.ReadValueAsync<bool>(address + layout["ActiveSnowOnRoads"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant()
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadActualWeatherAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("ActualWeather", cancellationToken).ConfigureAwait(false); var layout = Layout("ActualWeather");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["active"] = (await memory.ReadValueAsync<bool>(address + layout["Active"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["icao"] = await ReadAnsiAsync(address + layout["Icao"], cancellationToken).ConfigureAwait(false) ?? string.Empty,
            ["last_downloaded"] = (await memory.ReadValueAsync<double>(address + layout["LastDownloaded"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["invalid_icao"] = (await memory.ReadValueAsync<bool>(address + layout["InvalidIcao"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["counter"] = (await memory.ReadValueAsync<uint>(address + layout["Counter"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["process"] = (await memory.ReadValueAsync<byte>(address + layout["Process"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadCameraAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("Camera", cancellationToken).ConfigureAwait(false); var layout = Layout("Camera");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["family"] = (await memory.ReadValueAsync<int>(globals["CameraFamily"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["name"] = await ReadUnicodeAsync(address + layout["Name"], cancellationToken).ConfigureAwait(false) ?? string.Empty,
            ["field_of_view"] = await FloatAsync(address, layout, "FieldOfView", cancellationToken).ConfigureAwait(false),
            ["normal_field_of_view"] = await FloatAsync(address, layout, "NormalFieldOfView", cancellationToken).ConfigureAwait(false),
            ["distance"] = await FloatAsync(address, layout, "Distance", cancellationToken).ConfigureAwait(false)
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("RoadVehicles", cancellationToken).ConfigureAwait(false); var layout = Layout("RoadVehicleList");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["count"] = (await memory.ReadValueAsync<int>(address + layout["Count"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ai_collection"] = (await memory.ReadValueAsync<bool>(address + layout["Ai"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["player_index"] = (await memory.ReadValueAsync<int>(globals["PlayerVehicleIndex"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    // Handle lists page in handle order: a vehicle that joins between two
    // pages gets a higher handle and appears on a later page; none is repeated.
    public const int HandlePageLimit = 1000;

    public async ValueTask<IReadOnlyDictionary<string, string>> ListRoadVehiclesAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var after = page.AfterHandle();
        var addresses = await RoadVehicleAddressesAsync(cancellationToken).ConfigureAwait(false);
        var entries = new List<(int Position, string Handle, long Number)>();
        for (var index = 0; index < addresses.Length; index++)
        {
            if (addresses[index] == 0) continue;
            var handle = await GetRoadVehicleHandleAsync(addresses[index], cancellationToken).ConfigureAwait(false);
            entries.Add((index, handle, ListPage.HandleNumber(handle)));
        }
        return HandlePage("vehicle", addresses.Length, entries, after, page.Take(HandlePageLimit));
    }

    private static Dictionary<string, string> HandlePage(string row, int count, List<(int Position, string Handle, long Number)> entries, long after, int take)
    {
        var ordered = entries.Where(x => x.Number > after).OrderBy(x => x.Number).ToList();
        var returned = Math.Min(take, ordered.Count);
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in ordered.Take(returned)) output[row + "." + entry.Position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".handle"] = entry.Handle;
        ListPage.CompleteHandles(output, count, returned, returned > 0 ? ordered[returned - 1].Number : null, ordered.Count > returned);
        return output;
    }

    // Internal bridge for native operations that have already resolved an
    // object through a profiled collection delta. The returned token remains
    // session-scoped and is the only identity allowed above Interop.
    // The containing assembly is an implementation boundary, not the public
    // OmsiLaunch API. PluginRuntime is the only production caller.
    public async ValueTask<string> RegisterRoadVehicleHandleAsync(uint address, CancellationToken cancellationToken = default)
    {
        if (address == 0 || Array.IndexOf(await RoadVehicleAddressesAsync(cancellationToken).ConfigureAwait(false), address) < 0)
            throw new InvalidOperationException("OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION");
        var vmt = await memory.ReadPointer32Async(address, cancellationToken).ConfigureAwait(false);
        if (vmt < 0x00400000 || vmt >= 0x00C2B000)
            throw new InvalidOperationException("OL_E_RUNTIME_CREATED_OBJECT_INVALID");
        return await GetRoadVehicleHandleAsync(address, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("A runtime vehicle handle is required.", nameof(handle));
        var addresses = await RoadVehicleAddressesAsync(cancellationToken).ConfigureAwait(false);
        var address = await ResolveRoadVehicleHandleAsync(handle, addresses, cancellationToken).ConfigureAwait(false);

        return await ReadRoadVehicleAtAsync(handle, address, cancellationToken).ConfigureAwait(false);
    }

    // Player resolution and vehicle projection must use the same collection snapshot.
    // A second list read can observe a changed OMSI list and bind the public handle to another object.
    private async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleAtAsync(string handle, uint address, CancellationToken cancellationToken)
    {

        var layout = Layout("RoadVehicle");
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["handle"] = handle,
            ["runtime_index"] = await IntAsync(address, layout, "RuntimeIndex", cancellationToken).ConfigureAwait(false),
            ["tile"] = await IntAsync(address, layout, "Tile", cancellationToken).ConfigureAwait(false),
            ["marked_for_killing"] = (await memory.ReadValueAsync<bool>(address + layout["MarkedForKilling"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["traffic_type"] = (await memory.ReadValueAsync<byte>(address + layout["TrafficType"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["position_x"] = await FloatAtAsync(address + layout["Position"], cancellationToken).ConfigureAwait(false),
            ["position_y"] = await FloatAtAsync(address + layout["Position"] + 4, cancellationToken).ConfigureAwait(false),
            ["position_z"] = await FloatAtAsync(address + layout["Position"] + 8, cancellationToken).ConfigureAwait(false),
            ["rotation_x"] = await FloatAtAsync(address + layout["Rotation"], cancellationToken).ConfigureAwait(false),
            ["rotation_y"] = await FloatAtAsync(address + layout["Rotation"] + 4, cancellationToken).ConfigureAwait(false),
            ["rotation_z"] = await FloatAtAsync(address + layout["Rotation"] + 8, cancellationToken).ConfigureAwait(false),
            ["rotation_w"] = await FloatAtAsync(address + layout["Rotation"] + 12, cancellationToken).ConfigureAwait(false),
            ["steering"] = await FloatAsync(address, layout, "Steering", cancellationToken).ConfigureAwait(false),
            ["tacho"] = await FloatAsync(address, layout, "Tacho", cancellationToken).ConfigureAwait(false),
            ["ground_speed"] = await FloatAsync(address, layout, "GroundSpeed", cancellationToken).ConfigureAwait(false),
            ["kilometres"] = (await memory.ReadValueAsync<double>(address + layout["Kilometres"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["throttle"] = await FloatAsync(address, layout, "Throttle", cancellationToken).ConfigureAwait(false),
            ["brake"] = await FloatAsync(address, layout, "Brake", cancellationToken).ConfigureAwait(false),
            ["clutch"] = await FloatAsync(address, layout, "Clutch", cancellationToken).ConfigureAwait(false),
            ["ai_enabled"] = (await memory.ReadValueAsync<bool>(address + layout["AiEnabled"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["ai_mode"] = (await memory.ReadValueAsync<byte>(address + layout["AiMode"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ai_light"] = await FloatAsync(address, layout, "AiLight", cancellationToken).ConfigureAwait(false),
            ["ai_interior_light"] = await FloatAsync(address, layout, "AiInteriorLight", cancellationToken).ConfigureAwait(false),
            ["ai_indicator_left"] = await FloatAsync(address, layout, "AiIndicatorLeft", cancellationToken).ConfigureAwait(false),
            ["ai_indicator_right"] = await FloatAsync(address, layout, "AiIndicatorRight", cancellationToken).ConfigureAwait(false),
            ["ai_brake_light"] = await FloatAsync(address, layout, "AiBrakeLight", cancellationToken).ConfigureAwait(false)
        };
        return values;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadPlayerVehicleAsync(CancellationToken cancellationToken = default)
    {
        var playerIndex = await memory.ReadValueAsync<int>(globals["PlayerVehicleIndex"], cancellationToken).ConfigureAwait(false);
        var addresses = await RoadVehicleAddressesAsync(cancellationToken).ConfigureAwait(false);
        if (playerIndex < 0 || playerIndex >= addresses.Length || addresses[playerIndex] == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal) { ["present"] = "false" };
        var address = addresses[playerIndex];
        var handle = await GetRoadVehicleHandleAsync(address, cancellationToken).ConfigureAwait(false);
        var snapshot = new Dictionary<string, string>(await ReadRoadVehicleAtAsync(handle, address, cancellationToken).ConfigureAwait(false), StringComparer.Ordinal)
        {
            ["present"] = "true",
            ["player_index"] = playerIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return snapshot;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleVariableAsync(string handle, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A script variable name is required.", nameof(name));
        var address = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptVariables");
        var definition = await memory.ReadPointer32Async(address + layout["ComplMapObject"], cancellationToken).ConfigureAwait(false);
        var state = await memory.ReadPointer32Async(address + layout["ComplObjectInstance"], cancellationToken).ConfigureAwait(false);
        if (definition == 0 || state == 0) throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");
        var names = await memory.ReadStringArrayAsync(definition + layout["VariableNames"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumVariables"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var index = Array.FindIndex(names, candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_NOT_FOUND");
        var slot = await memory.ReadPointer32Async(address + layout["PublicValues"], cancellationToken).ConfigureAwait(false);
        var values = await memory.ReadPointer32Async(slot, cancellationToken).ConfigureAwait(false);
        var length = await ReadArrayLengthAsync(values, cancellationToken).ConfigureAwait(false);
        if (index >= length) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_NOT_FOUND");
        var valueAddress = await memory.ReadPointer32Async(values + checked((uint)index * 4), cancellationToken).ConfigureAwait(false);
        if (valueAddress == 0) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_UNAVAILABLE");
        var value = await memory.ReadValueAsync<float>(valueAddress, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["name"] = name, ["value"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    // OmsiHook's SetVariable writes through the resolved public-variable slot.
    // Keep that pointer traversal profile-bound and revalidate the opaque handle first.
    public async ValueTask<IReadOnlyDictionary<string, string>> WriteRoadVehicleVariableAsync(string handle, string name, float requestedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A script variable name is required.", nameof(name));
        if (!float.IsFinite(requestedValue)) throw new ArgumentOutOfRangeException(nameof(requestedValue), "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");

        var address = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptVariables");
        var definition = await memory.ReadPointer32Async(address + layout["ComplMapObject"], cancellationToken).ConfigureAwait(false);
        if (definition == 0) throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");

        var names = await memory.ReadStringArrayAsync(definition + layout["VariableNames"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumVariables"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var index = Array.FindIndex(names, candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_NOT_FOUND");

        var slot = await memory.ReadPointer32Async(address + layout["PublicValues"], cancellationToken).ConfigureAwait(false);
        if (slot == 0) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_UNAVAILABLE");
        var values = await memory.ReadPointer32Async(slot, cancellationToken).ConfigureAwait(false);
        var length = await ReadArrayLengthAsync(values, cancellationToken).ConfigureAwait(false);
        if (index >= length) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_NOT_FOUND");
        var valueAddress = await memory.ReadPointer32Async(values + checked((uint)index * 4), cancellationToken).ConfigureAwait(false);
        if (valueAddress == 0) throw new InvalidOperationException("OL_E_RUNTIME_VARIABLE_UNAVAILABLE");

        await memory.WriteValueAsync(valueAddress, requestedValue, cancellationToken).ConfigureAwait(false);
        var observedValue = await memory.ReadValueAsync<float>(valueAddress, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["handle"] = handle,
            ["name"] = name,
            ["requested_value"] = requestedValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["value"] = observedValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleStringVariableAsync(string handle, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A string variable name is required.", nameof(name));
        var address = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptVariables");
        var definition = await memory.ReadPointer32Async(address + layout["ComplMapObject"], cancellationToken).ConfigureAwait(false);
        var state = await memory.ReadPointer32Async(address + layout["ComplObjectInstance"], cancellationToken).ConfigureAwait(false);
        if (definition == 0 || state == 0) throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");
        var names = await memory.ReadStringArrayAsync(definition + layout["StringVariableNames"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumVariables"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var index = Array.FindIndex(names, candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND");
        var data = await memory.ReadPointer32Async(state + layout["StringValues"], cancellationToken).ConfigureAwait(false);
        var length = await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false);
        if (index >= length) throw new InvalidOperationException("OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND");
        var value = await memory.ReadStringAsync(data + checked((uint)index * 4), OmsiStringEncoding.Unicode, cancellationToken: cancellationToken).ConfigureAwait(false) ?? string.Empty;
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["name"] = name, ["value"] = value };
    }

    // The live address of a road vehicle for a native script call. With a
    // string-variable name, its script must declare it (OMSI's
    // SetStringVariable silently ignores unknown names). With requireSounds,
    // the vehicle must have a sound pack.
    public async ValueTask<uint> ResolveScriptTargetAsync(string handle, string? stringVariable = null, bool requireSounds = false, CancellationToken cancellationToken = default)
    {
        if (stringVariable is not null) await ReadRoadVehicleStringVariableAsync(handle, stringVariable, cancellationToken).ConfigureAwait(false);
        var address = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptVariables");
        if (await memory.ReadPointer32Async(address + layout["ComplMapObject"], cancellationToken).ConfigureAwait(false) == 0 ||
            await memory.ReadPointer32Async(address + layout["ComplObjectInstance"], cancellationToken).ConfigureAwait(false) == 0)
            throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");
        if (requireSounds && await memory.ReadPointer32Async(address + layout["SoundPack"], cancellationToken).ConfigureAwait(false) == 0)
            throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE: the vehicle has no sound pack");
        return address;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleConstantAsync(string handle, string name, CancellationToken cancellationToken = default)
    {
        var (constants, _) = await ResolveScriptConstantsAsync(handle, cancellationToken).ConfigureAwait(false);
        var names = await memory.ReadStringArrayAsync(constants + Layout("ScriptConstants")["Names"], OmsiStringEncoding.Ansi, maximumElements: checked((int)Layout("ScriptConstants")["MaximumElements"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var index = Array.FindIndex(names, candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("OL_E_RUNTIME_CONSTANT_NOT_FOUND");
        var values = await memory.ReadStructArrayAsync<float>(constants + Layout("ScriptConstants")["Values"], maximumElements: checked((int)Layout("ScriptConstants")["MaximumElements"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (index >= values.Length) throw new InvalidOperationException("OL_E_RUNTIME_CONSTANT_NOT_FOUND");
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["name"] = name, ["value"] = values[index].ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListRoadVehicleConstantsAsync(string handle, bool curves, ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var (constants, _) = await ResolveScriptConstantsAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptConstants");
        var names = await memory.ReadStringArrayAsync(constants + layout[curves ? "FunctionNames" : "Names"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumElements"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var output = new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle };
        var end = (int)Math.Min(names.Length, (long)start + page.Take(512));
        for (var index = start; index < end; index++) output[$"name.{index}"] = names[index] ?? string.Empty;
        ListPage.CompleteIndex(output, names.Length, Math.Min(start, names.Length), Math.Max(0, end - start));
        return output;
    }

    // Matches OmsiHook.OmsiComplMapObjInst.GetCurve: linearly interpolate the
    // profiled native point array and clamp outside its end points.
    public async ValueTask<IReadOnlyDictionary<string, string>> EvaluateRoadVehicleCurveAsync(string handle, string name, float x, CancellationToken cancellationToken = default)
    {
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x), "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        var (constants, _) = await ResolveScriptConstantsAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptConstants");
        var names = await memory.ReadStringArrayAsync(constants + layout["FunctionNames"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumElements"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        var index = Array.FindIndex(names, candidate => string.Equals(candidate, name, StringComparison.Ordinal));
        if (index < 0) throw new InvalidOperationException("OL_E_RUNTIME_CURVE_NOT_FOUND");
        var functions = await memory.ReadDelphiPointerArrayAsync(await memory.ReadPointer32Async(constants + layout["Functions"], cancellationToken).ConfigureAwait(false), checked((int)layout["MaximumElements"]), cancellationToken).ConfigureAwait(false);
        if (index >= functions.Length || functions[index] == 0) throw new InvalidOperationException("OL_E_RUNTIME_CURVE_NOT_FOUND");
        var points = await memory.ReadStructArrayAsync<CurvePoint>(functions[index] + layout["FunctionPoints"], maximumElements: checked((int)layout["MaximumElements"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (points.Length == 0) throw new InvalidOperationException("OL_E_RUNTIME_CURVE_EMPTY");
        float result = points[0].Y;
        if (x >= points[^1].X) result = points[^1].Y;
        else if (x > points[0].X)
        {
            var found = false;
            for (var point = 0; point < points.Length - 1; point++)
            {
                var left = points[point]; var right = points[point + 1];
                if (x < left.X || x > right.X) continue;
                if (right.X == left.X) throw new InvalidOperationException("OL_E_RUNTIME_CURVE_DEGENERATE");
                result = left.Y + (x - left.X) * (right.Y - left.Y) / (right.X - left.X);
                found = true; break;
            }
            if (!found) throw new InvalidOperationException("OL_E_RUNTIME_CURVE_INVALID");
        }
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle, ["name"] = name, ["x"] = x.ToString(System.Globalization.CultureInfo.InvariantCulture), ["value"] = result.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadRoadVehicleHofsAsync(string handle, ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var vehicle = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("RoadVehicleDefinition");
        var definition = await memory.ReadPointer32Async(vehicle + layout["Definition"], cancellationToken).ConfigureAwait(false);
        if (definition == 0) throw new InvalidOperationException("OL_E_RUNTIME_HOF_UNAVAILABLE");
        var hofs = await memory.ReadDelphiPointerArrayAsync(await memory.ReadPointer32Async(definition + layout["Hofs"], cancellationToken).ConfigureAwait(false), 1_024, cancellationToken).ConfigureAwait(false);
        var output = new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle };
        var end = (int)Math.Min(hofs.Length, (long)start + page.Take(256));
        for (var index = start; index < end; index++)
        {
            if (hofs[index] == 0) continue;
            output[$"hof.{index}.name"] = await SafeStringAsync(() => ReadUnicodeAsync(hofs[index] + layout["HofName"], cancellationToken)).ConfigureAwait(false);
            output[$"hof.{index}.service_trip"] = await SafeStringAsync(() => ReadAnsiAsync(hofs[index] + layout["HofServiceTrip"], cancellationToken)).ConfigureAwait(false);
        }
        ListPage.CompleteIndex(output, hofs.Length, Math.Min(start, hofs.Length), Math.Max(0, end - start));
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadDriversAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var layout = Layout("Driver");
        var data = await memory.ReadPointer32Async(globals["Drivers"], cancellationToken).ConfigureAwait(false);
        var count = await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false);
        if (count > 4_096) throw new InvalidDataException("Invalid driver array length.");
        var output = new Dictionary<string, string>(StringComparer.Ordinal) { ["selected_index"] = (await memory.ReadValueAsync<int>(globals["SelectedDriver"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture) };
        var end = (int)Math.Min(count, (long)start + page.Take(128));
        ListPage.CompleteIndex(output, count, Math.Min(start, count), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
        {
            var address = data + checked((uint)index * layout["Size"]); var prefix = $"driver.{index}.";
            output[prefix + "filename"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Filename"], cancellationToken)).ConfigureAwait(false);
            output[prefix + "name"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Name"], cancellationToken)).ConfigureAwait(false);
            output[prefix + "gender"] = (await memory.ReadValueAsync<bool>(address + layout["Gender"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
            output[prefix + "bus_stops"] = (await memory.ReadValueAsync<uint>(address + layout["BusStops"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "crashes"] = (await memory.ReadValueAsync<uint>(address + layout["Crashes"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "passengers"] = (await memory.ReadValueAsync<uint>(address + layout["Passengers"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "tickets"] = (await memory.ReadValueAsync<uint>(address + layout["Tickets"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "cash"] = await FloatAtAsync(address + layout["Cash"], cancellationToken).ConfigureAwait(false);
        }
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadTicketPackAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var address = globals["TicketPack"]; var layout = Layout("TicketPack"); var ticket = Layout("Ticket");
        var output = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["filename"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Filename"], cancellationToken)).ConfigureAwait(false),
            ["voice_path"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["VoicePath"], cancellationToken)).ConfigureAwait(false),
            ["stamper_factor"] = await FloatAtAsync(address + layout["Stamper"], cancellationToken).ConfigureAwait(false),
            ["buy_factor"] = await FloatAtAsync(address + layout["Buy"], cancellationToken).ConfigureAwait(false),
            ["chattiness"] = await FloatAtAsync(address + layout["Chattiness"], cancellationToken).ConfigureAwait(false),
            ["whinge_factor"] = await FloatAtAsync(address + layout["Whinge"], cancellationToken).ConfigureAwait(false)
        };
        var data = await memory.ReadPointer32Async(address + layout["Tickets"], cancellationToken).ConfigureAwait(false); var count = await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false);
        if (count > 4_096) throw new InvalidDataException("Invalid ticket array length.");
        var end = (int)Math.Min(count, (long)start + page.Take(128));
        ListPage.CompleteIndex(output, count, Math.Min(start, count), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
        {
            var item = data + checked((uint)index * layout["TicketSize"]); var prefix = $"ticket.{index}.";
            output[prefix + "name"] = await SafeStringAsync(() => ReadAnsiAsync(item + ticket["Name"], cancellationToken)).ConfigureAwait(false);
            output[prefix + "display_name"] = await SafeStringAsync(() => ReadAnsiAsync(item + ticket["DisplayName"], cancellationToken)).ConfigureAwait(false);
            output[prefix + "value"] = await FloatAtAsync(item + ticket["Value"], cancellationToken).ConfigureAwait(false);
            output[prefix + "maximum_stations"] = (await memory.ReadValueAsync<int>(item + ticket["MaximumStations"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "day_ticket"] = (await memory.ReadValueAsync<bool>(item + ticket["DayTicket"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant();
        }
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadTimeTableLogsAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var layout = Layout("TimeTableLog"); var data = await memory.ReadPointer32Async(globals["TimeTableLogs"], cancellationToken).ConfigureAwait(false); var count = await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false);
        if (count > 16_384) throw new InvalidDataException("Invalid timetable log array length.");
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        var end = (int)Math.Min(count, (long)start + page.Take(256));
        ListPage.CompleteIndex(output, count, Math.Min(start, count), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
        {
            var item = data + checked((uint)index * layout["Size"]); var prefix = $"log.{index}.";
            output[prefix + "bus_stop"] = await SafeStringAsync(() => ReadAnsiAsync(item + layout["BusStopName"], cancellationToken)).ConfigureAwait(false);
            output[prefix + "estimated_arrival"] = (await memory.ReadValueAsync<int>(item + layout["EstimatedArrival"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "estimated_departure"] = (await memory.ReadValueAsync<int>(item + layout["EstimatedDeparture"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "actual_arrival"] = (await memory.ReadValueAsync<int>(item + layout["ActualArrival"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "actual_departure"] = (await memory.ReadValueAsync<int>(item + layout["ActualDeparture"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "arrival_ok"] = (await memory.ReadValueAsync<byte>(item + layout["ArrivalOk"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
            output[prefix + "departure_ok"] = (await memory.ReadValueAsync<byte>(item + layout["DepartureOk"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListRoadVehicleVariablesAsync(string handle, bool stringVariables, ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var address = await ResolveRoadVehicleHandleAsync(handle, cancellationToken).ConfigureAwait(false);
        var layout = Layout("ScriptVariables");
        var definition = await memory.ReadPointer32Async(address + layout["ComplMapObject"], cancellationToken).ConfigureAwait(false);
        if (definition == 0) throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");
        var names = await memory.ReadStringArrayAsync(definition + layout[stringVariables ? "StringVariableNames" : "VariableNames"], OmsiStringEncoding.Ansi, maximumElements: checked((int)layout["MaximumVariables"]), cancellationToken: cancellationToken).ConfigureAwait(false);
        // The session mailbox has a fixed payload cap: at most 512 names per page.
        var output = new Dictionary<string, string>(StringComparer.Ordinal) { ["handle"] = handle };
        var end = (int)Math.Min(names.Length, (long)start + page.Take(512));
        for (var index = start; index < end; index++) if (!string.IsNullOrWhiteSpace(names[index])) output[$"name.{index}"] = names[index]!;
        ListPage.CompleteIndex(output, names.Length, Math.Min(start, names.Length), Math.Max(0, end - start));
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadHumansAsync(CancellationToken cancellationToken = default)
    {
        if (!globals.TryGetValue("Humans", out var location)) throw new InvalidOperationException("Missing profiled Humans global.");
        var data = await memory.ReadPointer32Async(location, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, string>(StringComparer.Ordinal) { ["count"] = (await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture) };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListHumansAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var after = page.AfterHandle();
        var addresses = await HumanAddressesAsync(cancellationToken).ConfigureAwait(false);
        var entries = new List<(int Position, string Handle, long Number)>();
        for (var index = 0; index < addresses.Length; index++)
        {
            if (addresses[index] == 0) continue;
            var handle = await GetHumanHandleAsync(addresses[index], cancellationToken).ConfigureAwait(false);
            entries.Add((index, handle, ListPage.HandleNumber(handle)));
        }
        return HandlePage("human", addresses.Length, entries, after, page.Take(HandlePageLimit));
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadHumanAsync(string handle, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("A runtime human handle is required.", nameof(handle));
        var addresses = await HumanAddressesAsync(cancellationToken).ConfigureAwait(false);
        var address = await ResolveHumanHandleAsync(handle, addresses, cancellationToken).ConfigureAwait(false);
        var layout = Layout("Human");
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["handle"] = handle,
            ["runtime_index"] = await IntAsync(address, layout, "RuntimeIndex", cancellationToken).ConfigureAwait(false),
            ["human_index"] = await IntAsync(address, layout, "HumanIndex", cancellationToken).ConfigureAwait(false),
            ["tile"] = await IntAsync(address, layout, "Tile", cancellationToken).ConfigureAwait(false),
            ["marked_for_killing"] = (await memory.ReadValueAsync<bool>(address + layout["MarkedForKilling"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["collision_type"] = await IntAsync(address, layout, "CollisionType", cancellationToken).ConfigureAwait(false),
            ["position_x"] = await FloatAtAsync(address + layout["Position"], cancellationToken).ConfigureAwait(false),
            ["position_y"] = await FloatAtAsync(address + layout["Position"] + 4, cancellationToken).ConfigureAwait(false),
            ["position_z"] = await FloatAtAsync(address + layout["Position"] + 8, cancellationToken).ConfigureAwait(false),
            ["target_x"] = await FloatAtAsync(address + layout["Target"], cancellationToken).ConfigureAwait(false),
            ["target_y"] = await FloatAtAsync(address + layout["Target"] + 4, cancellationToken).ConfigureAwait(false),
            ["target_z"] = await FloatAtAsync(address + layout["Target"] + 8, cancellationToken).ConfigureAwait(false),
            ["target_station"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["TargetStation"], cancellationToken)).ConfigureAwait(false),
            ["pre_target_station"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["PreTargetStation"], cancellationToken)).ConfigureAwait(false),
            ["departure"] = await FloatAsync(address, layout, "Departure", cancellationToken).ConfigureAwait(false),
            ["enter_bus_at"] = await FloatAsync(address, layout, "EnterBusAt", cancellationToken).ConfigureAwait(false),
            ["seat_bus"] = await IntAsync(address, layout, "SeatBus", cancellationToken).ConfigureAwait(false),
            ["seat_station"] = await IntAsync(address, layout, "SeatStation", cancellationToken).ConfigureAwait(false),
            ["ticket_type"] = (await memory.ReadValueAsync<byte>(address + layout["TicketType"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ticket_index"] = (await memory.ReadValueAsync<byte>(address + layout["TicketIndex"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ticket_ready"] = (await memory.ReadValueAsync<bool>(address + layout["TicketReady"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant(),
            ["state"] = await FloatAsync(address, layout, "State", cancellationToken).ConfigureAwait(false),
            ["speed"] = await FloatAsync(address, layout, "Speed", cancellationToken).ConfigureAwait(false),
            ["bus_index"] = await IntAsync(address, layout, "BusIndex", cancellationToken).ConfigureAwait(false),
            ["station"] = await IntAsync(address, layout, "Station", cancellationToken).ConfigureAwait(false),
            ["ai_mode"] = (await memory.ReadValueAsync<byte>(address + layout["AiMode"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ai_mode_ex"] = (await memory.ReadValueAsync<byte>(address + layout["AiModeEx"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ai_sub_mode"] = (await memory.ReadValueAsync<byte>(address + layout["AiSubMode"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["collision_state"] = (await memory.ReadValueAsync<byte>(address + layout["CollisionState"], cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ReadTimeTableAsync(CancellationToken cancellationToken = default)
    {
        var address = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false); var layout = Layout("TimeTableManager");
        var output = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["invalid"] = (await memory.ReadValueAsync<bool>(address + layout["Invalid"], cancellationToken).ConfigureAwait(false)).ToString().ToLowerInvariant()
        };
        foreach (var item in new[] { ("tracks", "Tracks"), ("trips", "Trips"), ("bus_stops", "BusStops"), ("station_links", "StationLinks"), ("lines", "Lines"), ("rv_files", "RvFiles") })
        {
            var data = await memory.ReadPointer32Async(address + layout[item.Item2], cancellationToken).ConfigureAwait(false);
            output[item.Item1] = (await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return output;
    }

    // Vehicle numbers OMSI keeps out of free AI use (reserved by timetable
    // tours or fixed AI lists): "group" and "number" per entry.
    public async ValueTask<IReadOnlyDictionary<string, string>> ListNoRvNumbersAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex();
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var list = await memory.ReadPointer32Async(manager + Layout("TimeTableManager")["NoRvList"], cancellationToken).ConfigureAwait(false);
        var layout = Layout("StringList");
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        if (list == 0 || await memory.ReadValueAsync<uint>(list, cancellationToken).ConfigureAwait(false) != layout["Vmt"]) throw new InvalidOperationException("OL_E_RUNTIME_VALUE_INVALID: the reserved-number list is not the expected string list");
        var count = await memory.ReadValueAsync<int>(list + layout["Count"], cancellationToken).ConfigureAwait(false);
        var items = await memory.ReadPointer32Async(list + layout["Items"], cancellationToken).ConfigureAwait(false);
        if (count < 0 || count > 65536 || (count > 0 && items == 0)) throw new InvalidOperationException("OL_E_RUNTIME_VALUE_INVALID: the reserved-number list has an implausible size");
        var end = (int)Math.Min(count, (long)start + page.Take(256));
        ListPage.CompleteIndex(output, count, Math.Min(start, count), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
        {
            var text = await SafeStringAsync(async () => await ReadUnicodeAsync(await memory.ReadPointer32Async(items + (uint)index * layout["ItemStride"], cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
            var tab = text.IndexOf('\t');
            output[$"no_rv.{index}.group"] = tab < 0 ? text : text[..tab];
            output[$"no_rv.{index}.number"] = tab < 0 ? "" : text[(tab + 1)..];
        }
        return output;
    }

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableTracksAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("Tracks", "TimeTableTrack", (output, index, address, layout, token) => AddTrackAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableTripsAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("Trips", "TimeTableTrip", (output, index, address, layout, token) => AddTripAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableLinesAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("Lines", "TimeTableLine", (output, index, address, layout, token) => AddLineAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableRvFilesAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("RvFiles", "TimeTableRvFile", (output, index, address, layout, token) => AddRvFileAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public async ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableTrackEntriesAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex(); var take = page.Take(512);
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var managerLayout = Layout("TimeTableManager"); var trackLayout = Layout("TimeTableTrack"); var entryLayout = Layout("TimeTableTrackEntry");
        var tracks = await memory.ReadPointer32Async(manager + managerLayout["Tracks"], cancellationToken).ConfigureAwait(false);
        var trackCount = await ReadArrayLengthAsync(tracks, cancellationToken).ConfigureAwait(false);
        var allCount = 0; var returned = 0; var output = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var trackIndex = 0; trackIndex < trackCount; trackIndex++)
        {
            var track = checked(tracks + (uint)trackIndex * trackLayout["Size"]);
            var entries = await memory.ReadPointer32Async(track + trackLayout["Entries"], cancellationToken).ConfigureAwait(false);
            var entryCount = await ReadArrayLengthAsync(entries, cancellationToken).ConfigureAwait(false);
            for (var entryIndex = 0; entryIndex < entryCount; entryIndex++, allCount++)
                if (allCount >= start && returned < take) { await AddTrackEntryAsync(output, allCount, trackIndex, checked(entries + (uint)entryIndex * entryLayout["Size"]), entryLayout, cancellationToken).ConfigureAwait(false); returned++; }
        }
        ListPage.CompleteIndex(output, allCount, Math.Min(start, allCount), returned);
        return output;
    }

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableBusStopsAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("BusStops", "TimeTableBusStop", (output, index, address, layout, token) => AddBusStopAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableStationLinksAsync(ListPage? page = null, CancellationToken cancellationToken = default) =>
        ListTimeTableRecordsAsync("StationLinks", "TimeTableStationLink", (output, index, address, layout, token) => AddStationLinkAsync(output, index, address, layout, token), page ?? ListPage.First, cancellationToken);

    public async ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableToursAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex(); var take = page.Take(128);
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var managerLayout = Layout("TimeTableManager"); var lineLayout = Layout("TimeTableLine"); var tourLayout = Layout("TimeTableTour");
        var lineData = await memory.ReadPointer32Async(manager + managerLayout["Lines"], cancellationToken).ConfigureAwait(false);
        var lineCount = await ReadArrayLengthAsync(lineData, cancellationToken).ConfigureAwait(false);
        var allCount = 0; var returned = 0;
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            var line = checked(lineData + (uint)lineIndex * lineLayout["Size"]);
            var tours = await memory.ReadPointer32Async(line + lineLayout["Tours"], cancellationToken).ConfigureAwait(false);
            var tourCount = await ReadArrayLengthAsync(tours, cancellationToken).ConfigureAwait(false);
            for (var tourIndex = 0; tourIndex < tourCount; tourIndex++, allCount++)
                if (allCount >= start && returned < take) { await AddTourAsync(output, allCount, lineIndex, checked(tours + (uint)tourIndex * tourLayout["Size"]), tourLayout, cancellationToken).ConfigureAwait(false); returned++; }
        }
        ListPage.CompleteIndex(output, allCount, Math.Min(start, allCount), returned);
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableProfilesAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex(); var take = page.Take(128);
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var managerLayout = Layout("TimeTableManager"); var tripLayout = Layout("TimeTableTrip"); var profileLayout = Layout("TimeTableProfile");
        var tripData = await memory.ReadPointer32Async(manager + managerLayout["Trips"], cancellationToken).ConfigureAwait(false);
        var tripCount = await ReadArrayLengthAsync(tripData, cancellationToken).ConfigureAwait(false);
        var allCount = 0; var returned = 0;
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var tripIndex = 0; tripIndex < tripCount; tripIndex++)
        {
            var trip = checked(tripData + (uint)tripIndex * tripLayout["Size"]);
            var profiles = await memory.ReadPointer32Async(trip + tripLayout["Profiles"], cancellationToken).ConfigureAwait(false);
            var profileCount = await ReadArrayLengthAsync(profiles, cancellationToken).ConfigureAwait(false);
            for (var profileIndex = 0; profileIndex < profileCount; profileIndex++, allCount++)
                if (allCount >= start && returned < take) { await AddProfileAsync(output, allCount, tripIndex, checked(profiles + (uint)profileIndex * profileLayout["Size"]), profileLayout, cancellationToken).ConfigureAwait(false); returned++; }
        }
        ListPage.CompleteIndex(output, allCount, Math.Min(start, allCount), returned);
        return output;
    }

    public async ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableTourEntriesAsync(ListPage? page = null, CancellationToken cancellationToken = default)
    {
        page ??= ListPage.First;
        var start = page.StartIndex(); var take = page.Take(256);
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var managerLayout = Layout("TimeTableManager"); var lineLayout = Layout("TimeTableLine"); var tourLayout = Layout("TimeTableTour"); var entryLayout = Layout("TimeTableTourEntry");
        var lineData = await memory.ReadPointer32Async(manager + managerLayout["Lines"], cancellationToken).ConfigureAwait(false);
        var lineCount = await ReadArrayLengthAsync(lineData, cancellationToken).ConfigureAwait(false);
        var allCount = 0; var returned = 0; var output = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            var line = checked(lineData + (uint)lineIndex * lineLayout["Size"]);
            var tours = await memory.ReadPointer32Async(line + lineLayout["Tours"], cancellationToken).ConfigureAwait(false);
            var tourCount = await ReadArrayLengthAsync(tours, cancellationToken).ConfigureAwait(false);
            for (var tourIndex = 0; tourIndex < tourCount; tourIndex++)
            {
                var tour = checked(tours + (uint)tourIndex * tourLayout["Size"]);
                var entries = await memory.ReadPointer32Async(tour + tourLayout["Entries"], cancellationToken).ConfigureAwait(false);
                var entryCount = await ReadArrayLengthAsync(entries, cancellationToken).ConfigureAwait(false);
                for (var entryIndex = 0; entryIndex < entryCount; entryIndex++, allCount++)
                    if (allCount >= start && returned < take) { await AddTourEntryAsync(output, allCount, lineIndex, tourIndex, checked(entries + (uint)entryIndex * entryLayout["Size"]), entryLayout, cancellationToken).ConfigureAwait(false); returned++; }
            }
        }
        ListPage.CompleteIndex(output, allCount, Math.Min(start, allCount), returned);
        return output;
    }

    private async ValueTask<IReadOnlyDictionary<string, string>> ListTimeTableRecordsAsync(string managerField, string recordLayoutName, Func<Dictionary<string, string>, int, uint, IReadOnlyDictionary<string, uint>, CancellationToken, ValueTask> append, ListPage page, CancellationToken cancellationToken)
    {
        var start = page.StartIndex();
        var manager = await ObjectAddressAsync("TimeTableManager", cancellationToken).ConfigureAwait(false);
        var managerLayout = Layout("TimeTableManager"); var layout = Layout(recordLayoutName);
        var data = await memory.ReadPointer32Async(manager + managerLayout[managerField], cancellationToken).ConfigureAwait(false);
        var count = await ReadArrayLengthAsync(data, cancellationToken).ConfigureAwait(false);
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        var end = (int)Math.Min(count, (long)start + page.Take(128));
        ListPage.CompleteIndex(output, count, Math.Min(start, count), Math.Max(0, end - start));
        for (var index = start; index < end; index++)
            await append(output, index, checked(data + (uint)index * layout["Size"]), layout, cancellationToken).ConfigureAwait(false);
        return output;
    }

    private async ValueTask AddTrackAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"track.{index}.";
        output[prefix + "filename"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Filename"], token)).ConfigureAwait(false);
        output[prefix + "path"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["FilePath"], token)).ConfigureAwait(false);
        output[prefix + "entries"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["Entries"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "length"] = await FloatAtAsync(address + layout["Length"], token).ConfigureAwait(false);
    }

    private async ValueTask AddTripAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"trip.{index}.";
        output[prefix + "filename"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Filename"], token)).ConfigureAwait(false);
        output[prefix + "chrono_origin"] = await IntAsync(address, layout, "ChronoOrigin", token).ConfigureAwait(false);
        output[prefix + "target"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Target"], token)).ConfigureAwait(false);
        output[prefix + "line"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Line"], token)).ConfigureAwait(false);
        output[prefix + "train_reverse"] = (await memory.ReadValueAsync<byte>(address + layout["TrainReverse"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "invalid"] = ((await memory.ReadValueAsync<byte>(address + layout["Invalid"], token).ConfigureAwait(false)) != 0).ToString().ToLowerInvariant();
        output[prefix + "profiles"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["Profiles"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "bus_stops"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["BusStops"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "track_name"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["TrackName"], token)).ConfigureAwait(false);
        output[prefix + "track_index"] = await IntAsync(address, layout, "TrackIndex", token).ConfigureAwait(false);
        // Current-build representation is not reconciled. It may be an
        // internal reference rather than a public station-link identifier.
        // Do not serialize a pointer-shaped implementation value.
    }

    private async ValueTask AddLineAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"line.{index}.";
        output[prefix + "name"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Name"], token)).ConfigureAwait(false);
        output[prefix + "user_allowed"] = ((await memory.ReadValueAsync<byte>(address + layout["UserAllowed"], token).ConfigureAwait(false)) != 0).ToString().ToLowerInvariant();
        output[prefix + "priority"] = (await memory.ReadValueAsync<byte>(address + layout["Priority"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "tours"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["Tours"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "chrono_origin"] = await IntAsync(address, layout, "ChronoOrigin", token).ConfigureAwait(false);
    }

    private async ValueTask AddRvFileAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"rv_file.{index}.";
        output[prefix + "start_date_rel_2000"] = await IntAsync(address, layout, "StartDateRel2000", token).ConfigureAwait(false);
        output[prefix + "end_date_rel_2000"] = await IntAsync(address, layout, "EndDateRel2000", token).ConfigureAwait(false);
        output[prefix + "line"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Line"], token)).ConfigureAwait(false);
        output[prefix + "number_tours"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["NumberTours"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "type_tours"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["TypeTours"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "type_line_files"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["TypeLineFiles"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "type_line_probability"] = await FloatAtAsync(address + layout["TypeLineProbability"], token).ConfigureAwait(false);
    }

    private async ValueTask AddTrackEntryAsync(Dictionary<string, string> output, int index, int trackIndex, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"track_entry.{index}.";
        output[prefix + "track_index"] = trackIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "id"] = (await memory.ReadValueAsync<uint>(address + layout["IdCode"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "path_index_on_object"] = await IntAsync(address, layout, "PathIndexOnObject", token).ConfigureAwait(false);
        output[prefix + "path_tile"] = await IntAsync(address, layout, "PathTile", token).ConfigureAwait(false);
        output[prefix + "path_index"] = await IntAsync(address, layout, "PathIndex", token).ConfigureAwait(false);
        output[prefix + "relative_distance"] = await FloatAtAsync(address + layout["RelativeDistance"], token).ConfigureAwait(false);
        output[prefix + "distance"] = await FloatAtAsync(address + layout["Distance"], token).ConfigureAwait(false);
        output[prefix + "valid"] = (await memory.ReadValueAsync<bool>(address + layout["Valid"], token).ConfigureAwait(false)).ToString().ToLowerInvariant();
        output[prefix + "path_order_check"] = (await memory.ReadValueAsync<byte>(address + layout["PathOrderCheck"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "allowed_fstrn"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["AllowedFstrn"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "chrono_origin"] = await IntAsync(address, layout, "ChronoOrigin", token).ConfigureAwait(false);
        output[prefix + "bad_chronos"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["BadChronos"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async ValueTask AddBusStopAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"bus_stop.{index}.";
        output[prefix + "name"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Name"], token)).ConfigureAwait(false);
        output[prefix + "supplement"] = await SafeStringAsync(() => ReadUnicodeAsync(address + layout["Supplement"], token)).ConfigureAwait(false);
        output[prefix + "tile"] = await IntAsync(address, layout, "Tile", token).ConfigureAwait(false);
        output[prefix + "id"] = (await memory.ReadValueAsync<uint>(address + layout["IdCode"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "parent_id"] = (await memory.ReadValueAsync<uint>(address + layout["ParentIdCode"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "preset_alighting"] = await FloatAtAsync(address + layout["PresetAlighting"], token).ConfigureAwait(false);
        output[prefix + "index"] = await IntAsync(address, layout, "Index", token).ConfigureAwait(false);
        output[prefix + "starting_links"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["StartingLinks"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "ending_links"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["EndingLinks"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "chrono_origin"] = await IntAsync(address, layout, "ChronoOrigin", token).ConfigureAwait(false);
    }

    private async ValueTask AddStationLinkAsync(Dictionary<string, string> output, int index, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"station_link.{index}.";
        output[prefix + "length"] = await FloatAtAsync(address + layout["Length"], token).ConfigureAwait(false);
        output[prefix + "start_bus_stop_id"] = (await memory.ReadValueAsync<uint>(address + layout["StartBusStopId"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "end_bus_stop_id"] = (await memory.ReadValueAsync<uint>(address + layout["EndBusStopId"], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "start_bus_stop"] = await IntAsync(address, layout, "StartBusStop", token).ConfigureAwait(false);
        output[prefix + "end_bus_stop"] = await IntAsync(address, layout, "EndBusStop", token).ConfigureAwait(false);
        output[prefix + "chrono_origin"] = await IntAsync(address, layout, "ChronoOrigin", token).ConfigureAwait(false);
        output[prefix + "valid"] = ((await memory.ReadValueAsync<byte>(address + layout["Valid"], token).ConfigureAwait(false)) != 0).ToString().ToLowerInvariant();
        output[prefix + "visible"] = (await memory.ReadValueAsync<bool>(address + layout["Visible"], token).ConfigureAwait(false)).ToString().ToLowerInvariant();
        output[prefix + "track_entries"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["TrackEntries"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "start_track_entry"] = await IntAsync(address, layout, "StartTrackEntry", token).ConfigureAwait(false);
        output[prefix + "end_track_entry"] = await IntAsync(address, layout, "EndTrackEntry", token).ConfigureAwait(false);
    }

    private async ValueTask AddTourAsync(Dictionary<string, string> output, int index, int lineIndex, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"tour.{index}.";
        output[prefix + "line_index"] = lineIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "name"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Name"], token)).ConfigureAwait(false);
        output[prefix + "ai_group"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["AiGroup"], token)).ConfigureAwait(false);
        output[prefix + "ai_type"] = await IntAsync(address, layout, "AiType", token).ConfigureAwait(false);
        output[prefix + "ai_group_index"] = await IntAsync(address, layout, "AiGroupIndex", token).ConfigureAwait(false);
        output[prefix + "vehicle_reservations"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["VehicleReservations"], token)).ConfigureAwait(false);
        output[prefix + "vehicle_indices"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["VehicleIndices"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "has_normal_vehicle"] = ((await memory.ReadValueAsync<byte>(address + layout["HasNormalVehicle"], token).ConfigureAwait(false)) != 0).ToString().ToLowerInvariant();
        output[prefix + "completed_day"] = await IntAsync(address, layout, "CompletedDay", token).ConfigureAwait(false);
        output[prefix + "invalid"] = ((await memory.ReadValueAsync<byte>(address + layout["Invalid"], token).ConfigureAwait(false)) != 0).ToString().ToLowerInvariant();
        output[prefix + "entries"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["Entries"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private async ValueTask AddProfileAsync(Dictionary<string, string> output, int index, int tripIndex, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"profile.{index}.";
        output[prefix + "trip_index"] = tripIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "name"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Name"], token)).ConfigureAwait(false);
        output[prefix + "total_time"] = await FloatAtAsync(address + layout["TotalTime"], token).ConfigureAwait(false);
        output[prefix + "stop_times"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["StopTimes"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "track_entry_times"] = (await ReadArrayLengthAsync(await memory.ReadPointer32Async(address + layout["TrackEntryTimes"], token).ConfigureAwait(false), token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "service_trip"] = (await memory.ReadValueAsync<bool>(address + layout["ServiceTrip"], token).ConfigureAwait(false)).ToString().ToLowerInvariant();
    }

    private async ValueTask AddTourEntryAsync(Dictionary<string, string> output, int index, int lineIndex, int tourIndex, uint address, IReadOnlyDictionary<string, uint> layout, CancellationToken token)
    {
        var prefix = $"tour_entry.{index}.";
        output[prefix + "line_index"] = lineIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "tour_index"] = tourIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        output[prefix + "trip"] = await SafeStringAsync(() => ReadAnsiAsync(address + layout["Trip"], token)).ConfigureAwait(false);
        output[prefix + "trip_index"] = await IntAsync(address, layout, "TripIndex", token).ConfigureAwait(false);
        output[prefix + "profile_index"] = await IntAsync(address, layout, "Profile", token).ConfigureAwait(false);
        output[prefix + "start_time"] = await FloatAtAsync(address + layout["StartTime"], token).ConfigureAwait(false);
        output[prefix + "end_time"] = await FloatAtAsync(address + layout["EndTime"], token).ConfigureAwait(false);
        output[prefix + "smooth_transition"] = (await memory.ReadValueAsync<bool>(address + layout["SmoothTransition"], token).ConfigureAwait(false)).ToString().ToLowerInvariant();
    }

    private async ValueTask<uint> ObjectAddressAsync(string global, CancellationToken cancellationToken)
    {
        if (!globals.TryGetValue(global, out var location)) throw new InvalidOperationException("Missing profiled global: " + global);
        var value = await memory.ReadPointer32Async(location, cancellationToken).ConfigureAwait(false);
        if (value == 0) throw new InvalidOperationException("OMSI runtime object is unavailable: " + global);
        return value;
    }
    private IReadOnlyDictionary<string, uint> Layout(string name) => layouts.TryGetValue(name, out var value) ? value : throw new InvalidOperationException("Missing profiled layout: " + name);
    private async ValueTask<string> FloatAsync(uint address, IReadOnlyDictionary<string, uint> layout, string name, CancellationToken token) => (await memory.ReadValueAsync<float>(address + layout[name], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    private async ValueTask<string> FloatAtAsync(uint address, CancellationToken token) => (await memory.ReadValueAsync<float>(address, token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    private async ValueTask<string> IntAsync(uint address, IReadOnlyDictionary<string, uint> layout, string name, CancellationToken token) => (await memory.ReadValueAsync<int>(address + layout[name], token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    private ValueTask<string?> ReadAnsiAsync(uint address, CancellationToken token) => memory.ReadStringAsync(address, OmsiStringEncoding.Ansi, cancellationToken: token);
    private ValueTask<string?> ReadUnicodeAsync(uint address, CancellationToken token) => memory.ReadStringAsync(address, OmsiStringEncoding.Unicode, cancellationToken: token);
    private async ValueTask<int> ReadArrayLengthAsync(uint data, CancellationToken token)
    {
        if (data == 0) return 0;
        var length = await memory.ReadDelphiLengthAsync(data, token).ConfigureAwait(false);
        if (length < 0 || length > 100_000) throw new InvalidDataException("Invalid OMSI runtime array length.");
        return length;
    }
    private async ValueTask<string> OptionalArrayCountAsync(uint pointerField, CancellationToken token)
    {
        try
        {
            var data = await memory.ReadPointer32Async(pointerField, token).ConfigureAwait(false);
            return (await ReadArrayLengthAsync(data, token).ConfigureAwait(false)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (ArgumentOutOfRangeException) { return "unavailable"; }
        catch (InvalidDataException) { return "unavailable"; }
        catch (EndOfStreamException) { return "unavailable"; }
    }
    private static async ValueTask<string> SafeStringAsync(Func<ValueTask<string?>> reader)
    {
        try { return await reader().ConfigureAwait(false) ?? string.Empty; }
        catch (InvalidDataException) { return string.Empty; }
        catch (EndOfStreamException) { return string.Empty; }
        // Optional legacy fields occasionally carry a non-null sentinel rather
        // than a Delphi long-string pointer. Preserve the tile snapshot and
        // report that string as unavailable instead of dereferencing it.
        catch (ArgumentOutOfRangeException) { return string.Empty; }
    }

    private async ValueTask<uint[]> RoadVehicleAddressesAsync(CancellationToken token)
    {
        var address = await ObjectAddressAsync("RoadVehicles", token).ConfigureAwait(false);
        var list = Layout("RoadVehicleList");
        var wrapper = await memory.ReadPointer32Async(address + list["Items"], token).ConfigureAwait(false);
        if (wrapper == 0) return Array.Empty<uint>();
        var count = await memory.ReadValueAsync<int>(wrapper + 8, token).ConfigureAwait(false);
        var data = await memory.ReadPointer32Async(wrapper + 4, token).ConfigureAwait(false);
        if (count < 0 || count > 10_000 || (count > 0 && data == 0)) throw new InvalidDataException("Invalid OMSI RoadVehicles list.");
        var bytes = new byte[checked(count * sizeof(uint))];
        if (bytes.Length != 0) await memory.ReadExactAsync(data, bytes, token).ConfigureAwait(false);
        var result = new uint[count];
        for (var index = 0; index < count; index++) result[index] = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint)));
        ReconcileRoadVehicleHandles(result);
        return result;
    }

    private async ValueTask<uint> ResolveRoadVehicleHandleAsync(string handle, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("A runtime vehicle handle is required.", nameof(handle));
        return await ResolveRoadVehicleHandleAsync(handle, await RoadVehicleAddressesAsync(token).ConfigureAwait(false), token).ConfigureAwait(false);
    }

    private async ValueTask<(uint Constants, uint Vehicle)> ResolveScriptConstantsAsync(string handle, CancellationToken token)
    {
        var vehicle = await ResolveRoadVehicleHandleAsync(handle, token).ConfigureAwait(false);
        var script = Layout("ScriptVariables"); var constants = Layout("ScriptConstants");
        var definition = await memory.ReadPointer32Async(vehicle + script["ComplMapObject"], token).ConfigureAwait(false);
        if (definition == 0) throw new InvalidOperationException("OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE");
        var block = await memory.ReadPointer32Async(definition + constants["ConstBlock"], token).ConfigureAwait(false);
        if (block == 0) throw new InvalidOperationException("OL_E_RUNTIME_CONSTANTS_UNAVAILABLE");
        return (block, vehicle);
    }

    private readonly record struct CurvePoint(float X, float Y);

    // Identity pinned to a handle at allocation. Both words are stable for the
    // lifetime of one native instance and change when the allocator hands the
    // same address to a different object: the Delphi VMT identifies the class
    // and the second word is a per-object type identity (vehicle definition
    // pointer, human model index). Two reads on resolve; no address escapes.
    // The third word is OMSI's IDCode: every map-object instance gets a new,
    // never reused number at construction, so a freed and reallocated address
    // is told apart exactly. For a human (a pooled slot that lives for the
    // whole map session) the fourth word identifies the current spawn: its
    // target station and departure change when the slot is spawned again.
    private readonly record struct ObjectFingerprint(uint Vmt, uint Identity, uint IdCode = 0, ulong Spawn = 0);

    private async ValueTask<ObjectFingerprint> RoadVehicleFingerprintAsync(uint address, CancellationToken token) =>
        new(await memory.ReadPointer32Async(address, token).ConfigureAwait(false),
            await memory.ReadPointer32Async(address + Layout("RoadVehicleDefinition")["Definition"], token).ConfigureAwait(false),
            await memory.ReadValueAsync<uint>(address + Layout("RoadVehicle")["IdCode"], token).ConfigureAwait(false));

    private async ValueTask<uint> ResolveRoadVehicleHandleAsync(string handle, uint[] addresses, CancellationToken token)
    {
        if (!roadVehicleHandlesByToken.TryGetValue(handle, out var entry) ||
            !roadVehicleAddressGenerations.TryGetValue(entry.Address, out var generation) ||
            generation != entry.Generation || Array.IndexOf(addresses, entry.Address) < 0)
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_STALE");
        // The generation only advances when a list read observed the address
        // missing. Destruction and reallocation between two reads keeps the
        // address in the collection, so the pinned identity is the tie-breaker.
        if (await RoadVehicleFingerprintAsync(entry.Address, token).ConfigureAwait(false) != entry.Fingerprint)
        {
            DropRoadVehicleHandle(entry);
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_STALE");
        }
        return entry.Address;
    }

    private void ReconcileRoadVehicleHandles(IEnumerable<uint> addresses)
    {
        var current = addresses.Where(address => address != 0).ToHashSet();
        foreach (var removed in activeRoadVehicleAddresses.Except(current).ToArray())
        {
            roadVehicleHandles.Remove(removed);
            roadVehicleAddressGenerations[removed] = roadVehicleAddressGenerations.TryGetValue(removed, out var generation) ? generation + 1 : 1;
        }
        activeRoadVehicleAddresses = current;
    }

    private void DropRoadVehicleHandle(RoadVehicleHandleEntry entry)
    {
        roadVehicleHandles.Remove(entry.Address);
        roadVehicleHandlesByToken.Remove(entry.Token);
        roadVehicleAddressGenerations[entry.Address] = (roadVehicleAddressGenerations.TryGetValue(entry.Address, out var generation) ? generation : entry.Generation) + 1;
    }

    private readonly record struct RoadVehicleHandleEntry(uint Address, int Generation, string Token, ObjectFingerprint Fingerprint);

    private async ValueTask<string> GetRoadVehicleHandleAsync(uint address, CancellationToken token)
    {
        if (!activeRoadVehicleAddresses.Contains(address)) throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_STALE");
        var fingerprint = await RoadVehicleFingerprintAsync(address, token).ConfigureAwait(false);
        if (roadVehicleHandles.TryGetValue(address, out var entry))
        {
            if (entry.Fingerprint == fingerprint) return entry.Token;
            // Same address, different object: retire the alias so the caller
            // never sees the old token bound to the new instance.
            DropRoadVehicleHandle(entry);
        }
        var generation = roadVehicleAddressGenerations.TryGetValue(address, out var value) ? value : 0;
        roadVehicleAddressGenerations[address] = generation;
        entry = new RoadVehicleHandleEntry(address, generation, "rv-" + (++nextRoadVehicleHandle).ToString("D6", System.Globalization.CultureInfo.InvariantCulture), fingerprint);
        roadVehicleHandles.Add(address, entry);
        roadVehicleHandlesByToken.Add(entry.Token, entry);
        return entry.Token;
    }

    private async ValueTask<uint[]> HumanAddressesAsync(CancellationToken token)
    {
        if (!globals.TryGetValue("Humans", out var location)) throw new InvalidOperationException("Missing profiled Humans global.");
        var result = await memory.ReadDelphiPointerArrayAsync(await memory.ReadPointer32Async(location, token).ConfigureAwait(false), 100_000, token).ConfigureAwait(false);
        ReconcileHumanHandles(result);
        return result;
    }

    private async ValueTask<ObjectFingerprint> HumanFingerprintAsync(uint address, CancellationToken token)
    {
        var layout = Layout("Human");
        var station = await memory.ReadPointer32Async(address + layout["TargetStation"], token).ConfigureAwait(false);
        var departure = await memory.ReadValueAsync<uint>(address + layout["Departure"], token).ConfigureAwait(false);
        return new(await memory.ReadPointer32Async(address, token).ConfigureAwait(false),
            await memory.ReadValueAsync<uint>(address + layout["HumanIndex"], token).ConfigureAwait(false),
            await memory.ReadValueAsync<uint>(address + layout["IdCode"], token).ConfigureAwait(false),
            ((ulong)station << 32) | departure);
    }

    // A despawned human (RenderMe 0) keeps its slot but is no longer in the world.
    private async ValueTask<bool> HumanActiveAsync(uint address, CancellationToken token) =>
        await memory.ReadValueAsync<byte>(address + Layout("Human")["RenderMe"], token).ConfigureAwait(false) != 0;

    private readonly record struct HumanHandleEntry(uint Address, string Token, ObjectFingerprint Fingerprint);

    private void ReconcileHumanHandles(uint[] addresses)
    {
        if (humanHandles.Count == 0) return;
        var current = addresses.Where(address => address != 0).ToHashSet();
        foreach (var entry in humanHandles.Values.Where(candidate => !current.Contains(candidate.Address)).ToArray()) DropHumanHandle(entry);
    }

    private void DropHumanHandle(HumanHandleEntry entry)
    {
        humanHandles.Remove(entry.Address);
        humanHandlesByToken.Remove(entry.Token);
    }

    private async ValueTask<string> GetHumanHandleAsync(uint address, CancellationToken token)
    {
        var fingerprint = await HumanFingerprintAsync(address, token).ConfigureAwait(false);
        if (humanHandles.TryGetValue(address, out var entry))
        {
            if (entry.Fingerprint == fingerprint) return entry.Token;
            DropHumanHandle(entry);
        }
        entry = new HumanHandleEntry(address, "hb-" + (++nextHumanHandle).ToString("D6", System.Globalization.CultureInfo.InvariantCulture), fingerprint);
        humanHandles.Add(address, entry);
        humanHandlesByToken.Add(entry.Token, entry);
        return entry.Token;
    }

    private async ValueTask<uint> ResolveHumanHandleAsync(string handle, uint[] addresses, CancellationToken token)
    {
        if (!humanHandlesByToken.TryGetValue(handle, out var entry) || Array.IndexOf(addresses, entry.Address) < 0)
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_STALE");
        if (!await HumanActiveAsync(entry.Address, token).ConfigureAwait(false) || await HumanFingerprintAsync(entry.Address, token).ConfigureAwait(false) != entry.Fingerprint)
        {
            DropHumanHandle(entry);
            throw new InvalidOperationException("OL_E_RUNTIME_OBJECT_HANDLE_STALE");
        }
        return entry.Address;
    }
}
