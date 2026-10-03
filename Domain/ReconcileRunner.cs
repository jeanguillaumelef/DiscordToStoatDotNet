namespace Domain;

public sealed class ReconcileRunner(IDiscordRepository discord, IStoatRepository stoat)
{
    public async Task<ReconcileResult> RunAsync(CancellationToken cancellationToken)
    {
        var discordSnapshot = await discord.GetSnapshotAsync(cancellationToken);
        var stoatSnapshot = await stoat.GetSnapshotAsync(cancellationToken);

        var result = Reconciler.Reconcile(discordSnapshot, stoatSnapshot);

        foreach (var change in result.Changes)
        {
            switch (change)
            {
                case CreateCategory create:
                    await stoat.CreateCategoryAsync(create.Title, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled change type {change.GetType().Name}");
            }
        }

        return result;
    }
}
