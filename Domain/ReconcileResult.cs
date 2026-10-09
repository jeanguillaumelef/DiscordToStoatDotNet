namespace Domain;

public sealed record ReconcileResult(IReadOnlyList<Change> Changes, IReadOnlyList<SkippedItem> Skipped, IReadOnlyList<ReconcileWarning> Warnings);
