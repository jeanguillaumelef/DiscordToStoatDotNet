namespace Domain;

public sealed record DiscordSnapshot(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<DiscordTextChannel> TextChannels,
    IReadOnlyList<DiscordVoiceChannel> VoiceChannels,
    IReadOnlyList<DiscordUnsupportedChannel> UnsupportedChannels);
