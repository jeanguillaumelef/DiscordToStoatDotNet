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

        var publicCategoryIds = discord.Categories
            .Where(c => c.EveryoneCanView)
            .Select(c => c.Id)
            .ToHashSet();

        var changes = new List<Change>();
        var skipped = new List<SkippedItem>();

        foreach (var category in discord.Categories)
        {
            if (!category.EveryoneCanView)
            {
                skipped.Add(new SkippedItem(category.Id, "Private category: @everyone cannot view it"));
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
                changes.Add(new CreateChannel(channel.Id, channel.Name, ChannelLink.ChannelDescription(channel.Id), MirroredCategoryId(channel, publicCategoryIds)));
            }
        }

        return new ReconcileResult(changes, skipped);
    }

    private static string? MirroredCategoryId(DiscordTextChannel channel, HashSet<string> publicCategoryIds) =>
        channel.CategoryId is { } categoryId && publicCategoryIds.Contains(categoryId) ? categoryId : null;
}
