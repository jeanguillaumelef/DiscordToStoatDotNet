namespace Domain;

public sealed record DiscordTextChannel(string Id, string Name, string? CategoryId, bool EveryoneCanView);
