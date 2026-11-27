namespace Domain;

/// <summary>Orchestrates one Import: reads both snapshots through the ports, asks ImportPlanner for the changes, applies them to Stoat in order (categories before channels), then puts every Mirror in Discord's order.</summary>
public sealed class ImportRunner(IDiscordRepository discord, IStoatRepository stoat)
{
    public async Task<ImportResult> RunAsync(CancellationToken cancellationToken)
    {
        var discordSnapshot = await discord.GetSnapshotAsync(cancellationToken);
        var stoatSnapshot = await stoat.GetSnapshotAsync(cancellationToken);

        var result = ImportPlanner.Plan(discordSnapshot, stoatSnapshot);

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
                    await stoat.CreateTextChannelAsync(create.Name, create.Description, MirrorCategoryId(create.SourceCategoryId, mirrorCategoryIds), cancellationToken);
                    break;
                case CreateVoiceChannel create:
                    mirrorCategoryIds ??= await GetMirrorCategoryIdsAsync(cancellationToken);
                    await stoat.CreateVoiceChannelAsync(create.Name, create.Description, MirrorCategoryId(create.SourceCategoryId, mirrorCategoryIds), cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled change type {change.GetType().Name}");
            }
        }

        // Last step: put every Mirror, including those from earlier runs, in Discord's order.
        var current = await stoat.GetSnapshotAsync(cancellationToken);
        var order = MirrorOrderPlanner.Plan(discordSnapshot, current);
        if (!IsCurrentOrder(order, current))
        {
            await stoat.SetCategoryOrderAsync(order, cancellationToken);
        }

        return result;
    }

    private static bool IsCurrentOrder(IReadOnlyList<CategoryOrder> order, StoatSnapshot current) =>
        order.Select(o => o.CategoryId).SequenceEqual(current.Categories.Select(c => c.Id))
        && order.Zip(current.Categories).All(pair => pair.First.ChannelIds.SequenceEqual(pair.Second.ChannelIds));

    private static string? MirrorCategoryId(string? sourceCategoryId, Dictionary<string, string> mirrorCategoryIds) =>
        sourceCategoryId is { } id && mirrorCategoryIds.TryGetValue(id, out var mirrorCategoryId) ? mirrorCategoryId : null;

    private async Task<Dictionary<string, string>> GetMirrorCategoryIdsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await stoat.GetSnapshotAsync(cancellationToken);
        var ids = new Dictionary<string, string>();
        foreach (var category in snapshot.Categories)
        {
            if (ChannelLink.SourceIdFromCategoryTitle(category.Name) is { } sourceId)
            {
                ids.TryAdd(sourceId, category.Id);
            }
        }

        return ids;
    }
}
