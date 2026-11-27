namespace Domain;

public sealed record StoatSnapshot(
    IReadOnlyList<StoatCategory> Categories,
    IReadOnlyList<StoatTextChannel> TextChannels,
    IReadOnlyList<StoatVoiceChannel> VoiceChannels);
