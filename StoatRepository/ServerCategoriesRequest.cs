using Newtonsoft.Json;
using StoatSharp.Rest;

namespace StoatRepository;

/// <summary>Body of a Stoat <c>PATCH /servers/{id}</c> that replaces the server's whole category list, in order, each with its channels in order.</summary>
internal sealed class ServerCategoriesRequest : IStoatRequest
{
    [JsonProperty("categories")]
    public required ServerCategoryJson[] Categories { get; init; }
}
