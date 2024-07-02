using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ChatLink.Models.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum MessageType
{
    None,

    Text,

    Image,

    Audio,

    Video,

    File
}
