namespace Domain;

public sealed class ReconcileRunner(IDiscordRepository discord, IStoatRepository stoat)
{
    public async Task<ReconcileResult> RunAsync(CancellationToken cancellationToken)
    {
        var discordSnapshot = await discord.GetSnapshotAsync(cancellationToken);
        var stoatSnapshot = await stoat.GetSnapshotAsync(cancellationToken);

        var result = Reconciler.Reconcile(discordSnapshot, stoatSnapshot);

        Dictionary<string, string>? mirrorCategoryIds = null;

        foreach (var change in result.Changes)
        {
            switch (change)
            {
                case CreateCategory create:
                    await stoat.CreateCategoryAsync(create.Title, cancellationToken);
                    break;
                case CreateChannel create:
                    // Categories are created first, so read them back to learn the Stoat IDs of the new ones.
                    mirrorCategoryIds ??= await GetMirrorCategoryIdsAsync(cancellationToken);
                    var categoryId = create.SourceCategoryId is { } sourceCategoryId
                        && mirrorCategoryIds.TryGetValue(sourceCategoryId, out var mirrorCategoryId)
                            ? mirrorCategoryId
                            : null;
                    await stoat.CreateTextChannelAsync(create.Name, create.Description, categoryId, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled change type {change.GetType().Name}");
            }
        }

        return result;
    }

    private async Task<Dictionary<string, string>> GetMirrorCategoryIdsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await stoat.GetSnapshotAsync(cancellationToken);
        var ids = new Dictionary<string, string>();
        foreach (var category in snapshot.Categories)
        {
            if (ChannelLink.SourceIdFromCategoryTitle(category.Title) is { } sourceId)
            {
                ids.TryAdd(sourceId, category.Id);
            }
        }

        return ids;
    }
}
