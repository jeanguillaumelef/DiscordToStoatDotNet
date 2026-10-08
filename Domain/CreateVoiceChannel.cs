namespace Domain;

public sealed record CreateVoiceChannel(string SourceId, string Name, string Description, string? SourceCategoryId) : Change;
