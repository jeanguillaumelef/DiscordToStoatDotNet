namespace Domain;

/// <summary>Pure decision logic for the last step of an Import: the order that puts every Mirror in Discord's order. Does no I/O.</summary>
public static class MirrorOrderPlanner
{
    /// <returns>Every Stoat category, in order, each with its channels in order.</returns>
    public static IReadOnlyList<CategoryOrder> Plan(DiscordSnapshot discord, StoatSnapshot stoat)
    {
        // Only Sources the Migration mirrors are ordered; a Mirror whose Source is now private counts as an "other" item.
        var mirroredCategoryIds = MirroredCategories.Ids(discord);
        var discordCategoryPositions = discord.Categories
            .Where(c => mirroredCategoryIds.Contains(c.Id))
            .Select((category, index) => (category.Id, index))
            .ToDictionary(p => p.Id, p => p.index);

        // The Discord snapshot lists channels in position order, and Discord shows a category's text channels before its voice channels.
        var discordChannelPositions = discord.TextChannels
            .Where(c => c.EveryoneCanView)
            .Select(c => (c.Id, c.CategoryId))
            .Concat(discord.VoiceChannels.Where(c => c.EveryoneCanView).Select(c => (c.Id, c.CategoryId)))
            .Select((channel, index) => (channel.Id, channel.CategoryId, index))
            .ToDictionary(c => c.Id, c => (c.CategoryId, c.index));

        var sourceIdsByMirrorChannelId = stoat.TextChannels
            .Select(c => (c.Id, c.Description))
            .Concat(stoat.VoiceChannels.Select(c => (c.Id, c.Description)))
            .Select(c => (c.Id, SourceId: ChannelLink.SourceIdFromChannelDescription(c.Description)))
            .Where(c => c.SourceId is not null)
            .ToDictionary(c => c.Id, c => c.SourceId!);

        return OrderKeepingOthersLast(stoat.Categories, category => SourceCategoryId(category) is { } id && discordCategoryPositions.TryGetValue(id, out var position) ? position : null)
            .Select(category =>
            {
                var sourceCategoryId = SourceCategoryId(category);
                var channelIds = OrderKeepingOthersLast(category.ChannelIds, channelId =>
                    sourceCategoryId is not null
                    && sourceIdsByMirrorChannelId.TryGetValue(channelId, out var sourceId)
                    && discordChannelPositions.TryGetValue(sourceId, out var source)
                    && source.CategoryId == sourceCategoryId
                        ? source.index
                        : null);
                return new CategoryOrder(category.Id, channelIds);
            })
            .ToList();
    }

    /// <summary>Sorts items that have a Discord position by it; items without one go after them, in their current order.</summary>
    private static List<T> OrderKeepingOthersLast<T>(IEnumerable<T> items, Func<T, int?> discordPosition) =>
        items
            .Select((item, index) => (item, position: discordPosition(item) ?? int.MaxValue, index))
            .OrderBy(i => i.position)
            .ThenBy(i => i.index)
            .Select(i => i.item)
            .ToList();

    private static string? SourceCategoryId(StoatCategory category) => ChannelLink.SourceIdFromCategoryTitle(category.Name);
}
