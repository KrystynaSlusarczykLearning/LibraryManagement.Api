using System.Text.Json.Serialization;

namespace LibraryManagement.IntegrationTests.Utils;

internal record JsonPatchOperation(
    [property: JsonPropertyName("op")] string Op,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("value")] object? Value = null)
{
    public static JsonPatchOperation Replace(string path, object? value) =>
        new("replace", path, value);
}
