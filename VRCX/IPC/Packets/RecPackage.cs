using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StandaloneNotifier.VRCX.IPC.Packets
{
    public class RecPackage
    {
        [JsonPropertyName("Data")]
        public string? Data { get; set; }

        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("MsgType")]
        public string? MsgType { get; set; }

        public RecPackage()
        {
        }
    }

    [JsonSerializable(typeof(RecPackage))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class RecPackageContext : JsonSerializerContext { }
}
