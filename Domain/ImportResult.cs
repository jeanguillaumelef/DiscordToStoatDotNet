namespace Domain;

public sealed record ImportResult(IReadOnlyList<Change> Changes, IReadOnlyList<SkippedItem> Skipped, IReadOnlyList<ImportWarning> Warnings);
