namespace Domain;

public sealed record CreateChannel(string SourceId, string Name, string Description, string? SourceCategoryId) : Change;
