namespace Domain;

public static class Reconciler
{
    public static ReconcileResult Reconcile(DiscordSnapshot discord, StoatSnapshot stoat)
    {
        var linked = stoat.Categories
            .Select(c => ChannelLink.SourceIdFromCategoryTitle(c.Title))
            .OfType<string>()
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

            if (!linked.Contains(category.Id))
            {
                changes.Add(new CreateCategory(category.Id, ChannelLink.CategoryTitle(category.Name, category.Id)));
            }
        }

        return new ReconcileResult(changes, skipped);
    }
}
