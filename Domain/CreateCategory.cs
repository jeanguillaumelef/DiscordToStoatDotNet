namespace Domain;

public sealed record CreateCategory(string SourceId, string Title) : Change;
