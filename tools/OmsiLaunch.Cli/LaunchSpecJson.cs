using System.Collections;
using System.Reflection;
using System.Text.Json;
using OmsiLaunch.Api;

// Strict loader for /spec files. System.Text.Json silently ignores unknown
// members; a typo in a LaunchSpec would then be accepted as "applied". Every
// object member is checked against the public record shape before binding.
internal static class LaunchSpecJson
{
    public const long MaxBytes = 1024 * 1024;
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static async Task<LaunchSpec> LoadAsync(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("OL_E_SPEC_NOT_FOUND: " + path, path);
        if (info.Length > MaxBytes) throw new InvalidDataException("OL_E_SPEC_TOO_LARGE: LaunchSpec files are limited to 1 MiB.");
        var bytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        return Parse(bytes);
    }

    public static LaunchSpec Parse(ReadOnlyMemory<byte> json)
    {
        using (var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, MaxDepth = 32 }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("OL_E_SPEC_INVALID: the LaunchSpec root must be an object.");
            Validate(document.RootElement, typeof(LaunchSpec), "$");
        }
        return JsonSerializer.Deserialize<LaunchSpec>(json.Span, Options) ?? throw new InvalidDataException("OL_E_SPEC_INVALID: Invalid LaunchSpec JSON.");
    }

    private static void Validate(JsonElement element, Type type, string path)
    {
        if (element.ValueKind != JsonValueKind.Object) return;
        if (typeof(IDictionary).IsAssignableFrom(type) || IsGenericDictionary(type)) return;
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var member in element.EnumerateObject())
        {
            var property = properties.FirstOrDefault(candidate => string.Equals(candidate.Name, member.Name, StringComparison.OrdinalIgnoreCase));
            if (property is null) throw new InvalidDataException("OL_E_SPEC_UNKNOWN_PROPERTY: " + path + "." + member.Name);
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (member.Value.ValueKind == JsonValueKind.Object && propertyType != typeof(string)) Validate(member.Value, propertyType, path + "." + member.Name);
            // Arrays of records (HofManagement.Operations) get the same strict
            // member check per element.
            if (member.Value.ValueKind == JsonValueKind.Array && ElementType(propertyType) is { } itemType && itemType != typeof(string))
            {
                var index = 0;
                foreach (var item in member.Value.EnumerateArray()) Validate(item, itemType, path + "." + member.Name + "[" + index++ + "]");
            }
        }
    }

    private static Type? ElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();
        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? type : type.GetInterfaces().FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    private static bool IsGenericDictionary(Type type) =>
        type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) || type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        || type.GetInterfaces().Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
}
