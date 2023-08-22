using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StandaloneNotifier.VRCX.IPC.Packets
{
    public class VrcxMessagePacket
    {
        public enum MessageType
        {
            VrcxMessage,
            Noty,
            CustomTag,
            External
        }

        [JsonPropertyName("Data")]
        public string? Data { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } = "VrcxMessage";

        [JsonPropertyName("UserId")]
        public string? UserId { get; set; }

        [JsonPropertyName("DisplayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("Tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("TagColour")]
        public string? TagColour { get; set; }

        [JsonPropertyName("MsgType")]
        public string? MsgType { get; set; }

        public VrcxMessagePacket(MessageType messageType)
        {
            MsgType = messageType.ToString();
        }
    }

    [JsonSerializable(typeof(VrcxMessagePacket))]
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, WriteIndented = false)]
    public partial class VrcxMessagePacketContext : JsonSerializerContext { }
}
