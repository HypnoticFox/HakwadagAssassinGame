using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace HakwadagAssassinGame.Infrastructure.Persistence.Json;

/// <summary>Serializes domain values using the source-generated game JSON metadata.</summary>
internal static class RedisJsonSerializer
{
    /// <summary>Serializes a value to JSON.</summary>
    internal static string Serialize<T>(T value, JsonTypeInfo<T> typeInfo) =>
        JsonSerializer.Serialize(value, typeInfo);

    /// <summary>Deserializes JSON, returning null for empty or invalid values.</summary>
    internal static T? Deserialize<T>(string? json, JsonTypeInfo<T> typeInfo)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
