namespace Domain;

public sealed record DiscordSnapshot(
    IReadOnlyList<DiscordCategory> Categories,
    IReadOnlyList<DiscordTextChannel> TextChannels,
    IReadOnlyList<DiscordVoiceChannel> VoiceChannels,
    IReadOnlyList<DiscordUnsupportedChannel> UnsupportedChannels);
