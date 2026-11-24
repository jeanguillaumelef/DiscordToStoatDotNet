namespace Domain;

/// <summary>Pure decision logic of an Import: compares a Discord snapshot with a Stoat snapshot and returns the creates needed for Sources not yet imported. Does no I/O.</summary>
public static class ImportPlanner
{
    public static ImportResult Plan(DiscordSnapshot discord, StoatSnapshot stoat)
    {
        var mirroredDiscordCategoryIds = stoat.Categories
            .Select(c => ChannelLink.SourceIdFromCategoryTitle(c.Name))
            .OfType<string>()
            .ToHashSet();

        var importedTextChannelIds = ImportedChannelIds(stoat.TextChannels.Select(c => c.Description));
        var importedVoiceChannelIds = ImportedChannelIds(stoat.VoiceChannels.Select(c => c.Description));

        // A Private Category is mirrored only when it holds at least one public channel (issue #12),
        // so those public channels can stay grouped under a Mirror Category.
        var categoryIdsHoldingPublicChannel = discord.TextChannels
            .Where(c => c.EveryoneCanView && c.CategoryId is not null)
            .Select(c => c.CategoryId!)
            .Concat(discord.VoiceChannels.Where(c => c.EveryoneCanView && c.CategoryId is not null).Select(c => c.CategoryId!))
            .ToHashSet();

        var mirroredCategoryIds = discord.Categories
            .Where(c => c.EveryoneCanView || categoryIdsHoldingPublicChannel.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();

        var changes = new List<Change>();
        var skipped = new List<SkippedItem>();
        var warnings = new List<ImportWarning>();

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

            if (!importedTextChannelIds.Contains(channel.Id))
            {
                var description = ComposeDescription(channel.Id, channel.Description, warnings);
                changes.Add(new CreateChannel(channel.Id, channel.Name, description, MirroredCategoryId(channel.CategoryId, mirroredCategoryIds)));
            }
        }

        foreach (var channel in discord.VoiceChannels)
        {
            if (!channel.EveryoneCanView)
            {
                skipped.Add(new SkippedItem(channel.Id, "Private channel: @everyone cannot view it"));
                continue;
            }

            if (!importedVoiceChannelIds.Contains(channel.Id))
            {
                var description = ChannelLink.ComposeChannelDescription(channel.Id, string.Empty).Text;
                changes.Add(new CreateVoiceChannel(channel.Id, channel.Name, description, MirroredCategoryId(channel.CategoryId, mirroredCategoryIds)));
            }
        }

        foreach (var channel in discord.UnsupportedChannels)
        {
            skipped.Add(new SkippedItem(channel.Id, $"Unsupported channel type: {channel.TypeName}"));
        }

        return new ImportResult(changes, skipped, warnings);
    }

    private static string ComposeDescription(string sourceId, string channelDescription, List<ImportWarning> warnings)
    {
        var composed = ChannelLink.ComposeChannelDescription(sourceId, channelDescription);
        if (composed.WasTruncated)
        {
            warnings.Add(new ImportWarning(sourceId, "Channel Description was truncated to fit the Stoat description limit"));
        }

        return composed.Text;
    }

    private static HashSet<string> ImportedChannelIds(IEnumerable<string> mirrorDescriptions) =>
        mirrorDescriptions.Select(ChannelLink.SourceIdFromChannelDescription).OfType<string>().ToHashSet();

    private static string? MirroredCategoryId(string? categoryId, HashSet<string> mirroredCategoryIds) =>
        categoryId is { } id && mirroredCategoryIds.Contains(id) ? id : null;
}
