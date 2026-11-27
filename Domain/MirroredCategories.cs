namespace Domain;

/// <summary>Which Source Categories the Migration mirrors: public ones, and a Private Category only while it holds at least one public Source Channel (issue #12), so those channels stay grouped.</summary>
public static class MirroredCategories
{
    public static HashSet<string> Ids(DiscordSnapshot discord)
    {
        var categoryIdsHoldingPublicChannel = discord.TextChannels
            .Where(c => c.EveryoneCanView && c.CategoryId is not null)
            .Select(c => c.CategoryId!)
            .Concat(discord.VoiceChannels.Where(c => c.EveryoneCanView && c.CategoryId is not null).Select(c => c.CategoryId!))
            .ToHashSet();

        return discord.Categories
            .Where(c => c.EveryoneCanView || categoryIdsHoldingPublicChannel.Contains(c.Id))
            .Select(c => c.Id)
            .ToHashSet();
    }
}
