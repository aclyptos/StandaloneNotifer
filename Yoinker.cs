using System.Text.Json.Serialization;

namespace StandaloneNotifier
{
    internal class Yoinker
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("isYoinker")]
        public bool IsYoinker { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    [JsonSerializable(typeof(Yoinker))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    internal partial class YoinkerJsonContext : JsonSerializerContext { }
}
