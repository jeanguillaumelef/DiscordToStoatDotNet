namespace Domain;

public sealed record DiscordVoiceChannel(string Id, string Name, string? CategoryId, bool EveryoneCanView);
