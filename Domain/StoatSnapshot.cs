namespace Domain;

public sealed record StoatSnapshot(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<StoatTextChannel> TextChannels,
    IReadOnlyList<StoatVoiceChannel> VoiceChannels);
