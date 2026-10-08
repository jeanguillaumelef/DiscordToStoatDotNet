namespace Domain;

/// <param name="TypeName">A human-readable channel type, e.g. "forum", "announcement", "stage", "thread".</param>
public sealed record DiscordUnsupportedChannel(string Id, string TypeName);
