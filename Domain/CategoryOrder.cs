namespace Domain;

/// <summary>One Stoat category's place in the server's category order, with its channels in the order they should appear.</summary>
public sealed record CategoryOrder(string CategoryId, IReadOnlyList<string> ChannelIds);
