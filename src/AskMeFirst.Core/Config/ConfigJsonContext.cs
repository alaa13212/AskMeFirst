using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskMeFirst.Core.Config;

[JsonSourceGenerationOptions(
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(BrowserSpec))]
internal partial class ConfigJsonContext : JsonSerializerContext;