using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StoatRepository;

/// <summary>Raw shape of the Stoat <c>GET /channels/{id}</c> payload. StoatSharp's typed channels drop the <c>voice</c> field, so the adapter reads it itself (ADR 0003).</summary>
internal sealed class StoatChannelResponse
{
    [JsonProperty("channel_type")]
    public string? ChannelType { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public JObject? Voice { get; set; }
}
