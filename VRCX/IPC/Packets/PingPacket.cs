using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StandaloneNotifier.VRCX.IPC.Packets
{
    public class PingPacket
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } = "MsgPing";

        public PingPacket()
        {
        }
    }

    [JsonSerializable(typeof(PingPacket))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class PingPacketContext : JsonSerializerContext { }
}
