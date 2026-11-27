using Newtonsoft.Json;

namespace StoatRepository;

/// <summary>One category in a Stoat server's category list.</summary>
internal sealed class ServerCategoryJson
{
    [JsonProperty("id")]
    public required string Id { get; init; }

    [JsonProperty("title")]
    public required string Title { get; init; }

    [JsonProperty("channels")]
    public required string[] Channels { get; init; }
}
