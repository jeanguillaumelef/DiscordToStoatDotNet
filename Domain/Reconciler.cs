namespace Domain;

/// <summary>Pure decision logic of Reconcile: compares a Discord snapshot with a Stoat snapshot and returns the changes needed. Does no I/O.</summary>
public static class Reconciler
{
    public static ReconcileResult Reconcile(DiscordSnapshot discord, StoatSnapshot stoat)
    {
        var mirroredDiscordCategoryIds = stoat.Categories
            .Select(c => ChannelLink.SourceIdFromCategoryTitle(c.Name))
            .OfType<string>()
            .ToHashSet();

        var mirrorTextChannels = new Dictionary<string, StoatTextChannel>();
        foreach (var mirror in stoat.TextChannels)
        {
            if (ChannelLink.SourceIdFromChannelDescription(mirror.Description) is { } id)
            {
                mirrorTextChannels.TryAdd(id, mirror);
            }
        }

        var mirrorVoiceChannels = new Dictionary<string, StoatVoiceChannel>();
        foreach (var mirror in stoat.VoiceChannels)
        {
            if (ChannelLink.SourceIdFromChannelDescription(mirror.Description) is { } id)
            {
                mirrorVoiceChannels.TryAdd(id, mirror);
            }
        }

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
        var warnings = new List<ReconcileWarning>();

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

            var description = ComposeDescription(channel.Id, channel.Description, warnings);
            if (mirrorTextChannels.TryGetValue(channel.Id, out var mirror))
            {
                if (mirror.Description != description)
                {
                    changes.Add(new UpdateChannelDescription(mirror.Id, description));
                }
            }
            else
            {
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

            var description = ComposeDescription(channel.Id, channel.Description, warnings);
            if (mirrorVoiceChannels.TryGetValue(channel.Id, out var mirror))
            {
                if (mirror.Description != description)
                {
                    changes.Add(new UpdateChannelDescription(mirror.Id, description));
                }
            }
            else
            {
                changes.Add(new CreateVoiceChannel(channel.Id, channel.Name, description, MirroredCategoryId(channel.CategoryId, mirroredCategoryIds)));
            }
        }

        foreach (var channel in discord.UnsupportedChannels)
        {
            skipped.Add(new SkippedItem(channel.Id, $"Unsupported channel type: {channel.TypeName}"));
        }

        return new ReconcileResult(changes, skipped, warnings);
    }

    private static string ComposeDescription(string sourceId, string channelDescription, List<ReconcileWarning> warnings)
    {
        var composed = ChannelLink.ComposeChannelDescription(sourceId, channelDescription);
        if (composed.WasTruncated)
        {
            warnings.Add(new ReconcileWarning(sourceId, "Channel Description was truncated to fit the Stoat description limit"));
        }

        return composed.Text;
    }

    private static string? MirroredCategoryId(string? categoryId, HashSet<string> mirroredCategoryIds) =>
        categoryId is { } id && mirroredCategoryIds.Contains(id) ? id : null;
}
