namespace Domain;

/// <param name="ChannelIds">The Stoat channels in this category, in the order Stoat shows them.</param>
public sealed record StoatCategory(string Id, string Name, IReadOnlyList<string> ChannelIds);
