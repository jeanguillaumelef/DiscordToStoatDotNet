namespace Domain;

/// <summary>Pure decision logic of Reconcile: compares a Discord snapshot with a Stoat snapshot and returns the changes needed. Does no I/O.</summary>
public static class Reconciler
{
    public static ReconcileResult Reconcile(DiscordSnapshot discord, StoatSnapshot stoat)
    {
        var mirroredDiscordCategoryIds = stoat.Categories
            .Select(c => ChannelLink.SourceIdFromCategoryTitle(c.Title))
            .OfType<string>()
            .ToHashSet();

        var mirroredDiscordChannelIds = stoat.TextChannels
            .Select(c => ChannelLink.SourceIdFromChannelDescription(c.Description))
            .OfType<string>()
            .ToHashSet();

        // A Private Category is mirrored only when it holds at least one public channel (issue #12),
        // so those public channels can stay grouped under a Mirror Category.
        var categoryIdsHoldingPublicChannel = discord.TextChannels
            .Where(c => c.EveryoneCanView && c.CategoryId is not null)
            .Select(c => c.CategoryId!)
            .ToHashSet();

        var mirroredCategoryIds = discord.Categories
            .Where(c => c.EveryoneCanView || categoryIdsHoldingPublicChannel.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        var changes = new List<Change>();
        var skipped = new List<SkippedItem>();

        foreach (var category in discord.Categories)
        {
            if (!mirroredCategoryIds.Contains(category.Id))
            {
                skipped.Add(new SkippedItem(category.Id, "Private category: @everyone cannot view it and it holds no public channel"));
                continue;
            }

            if (!mirroredDiscordCategoryIds.Contains(category.Id))
            {
                changes.Add(new CreateCategory(category.Id, ChannelLink.CategoryTitle(category.Name, category.Id)));
            }
        }

        foreach (var channel in discord.TextChannels)
        {
            if (!channel.EveryoneCanView)
            {
                skipped.Add(new SkippedItem(channel.Id, "Private channel: @everyone cannot view it"));
                continue;
            }

            if (!mirroredDiscordChannelIds.Contains(channel.Id))
            {
                changes.Add(new CreateChannel(channel.Id, channel.Name, ChannelLink.ChannelDescription(channel.Id), MirroredCategoryId(channel, mirroredCategoryIds)));
            }
        }

        return new ReconcileResult(changes, skipped);
    }

    private static string? MirroredCategoryId(DiscordTextChannel channel, HashSet<string> mirroredCategoryIds) =>
        channel.CategoryId is { } categoryId && mirroredCategoryIds.Contains(categoryId) ? categoryId : null;
}
