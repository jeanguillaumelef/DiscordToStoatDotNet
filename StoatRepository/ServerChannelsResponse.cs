namespace StoatRepository;

/// <summary>Minimal shape of the Stoat <c>GET /servers/{id}</c> payload: just the channel IDs. StoatSharp keeps <c>Server.ChannelIds</c> internal, so the adapter reads them itself.</summary>
internal sealed class ServerChannelsResponse
{
    public string[]? Channels { get; set; }
}
