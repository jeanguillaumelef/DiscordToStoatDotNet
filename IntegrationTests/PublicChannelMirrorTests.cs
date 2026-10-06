using Domain;
using DiscordRepository;
using Microsoft.Extensions.Configuration;
using StoatRepository;

namespace IntegrationTests;

[Trait("Category", "Integration")]
public class PublicChannelMirrorTests
{
    [Fact]
    public async Task Reconcile_MirrorsPublicChannelUnderItsCategory_CategoryFirst()
    {
        var settings = IntegrationSettings.Load(new ConfigurationBuilder()
            .AddUserSecrets<PublicChannelMirrorTests>(optional: true)
            .Build());
        var guildId = ulong.Parse(settings.DiscordGuildId);

        // Every run gets its own name prefix, so its resources are easy to spot if a teardown ever misses one.
        // Kept short: the Mirror Category title is "{name} [{discordId}]", and Stoat rejects titles over its length limit (issue #8).
        var prefix = $"it-{Guid.NewGuid():N}"[..9];
        var ledger = new TeardownLedger();

        await using var discordGuild = new DiscordTestGuild(settings.DiscordBotToken, guildId);
        await using var discord = new DiscordRepository.DiscordRepository(
            new DiscordRepositoryOptions(settings.DiscordBotToken, guildId));
        var stoat = new StoatRepository.StoatRepository(
            new StoatRepositoryOptions(settings.StoatBotToken, settings.StoatServerId));
        var stoatServer = new StoatTestServer(settings.StoatBotToken, settings.StoatServerId);

        try
        {
            await RunAsync();
        }
        catch
        {
            // Report the original failure; a teardown error here would hide it.
            try { await ledger.RunAsync(); } catch { /* already reported as the test failure */ }
            throw;
        }

        // Teardown errors surface as the test failure when the body passed.
        await ledger.RunAsync();

        async Task RunAsync()
        {
            var sourceCategory = await discordGuild.CreateCategoryAsync(prefix);
            ledger.Add($"Discord category {sourceCategory.Name}", () => sourceCategory.DeleteAsync());

            var sourceChannel = await discordGuild.CreateTextChannelAsync($"{prefix}-channel", sourceCategory.Id);
            ledger.Add($"Discord channel {sourceChannel.Name}", () => sourceChannel.DeleteAsync());

            // The run may create mirrors for other public channels in the guild too; teardown removes everything new since this snapshot.
            var before = await stoat.GetSnapshotAsync(CancellationToken.None);
            ledger.Add("Stoat resources created by the run", () => DeleteStoatResourcesCreatedSinceAsync(stoat, stoatServer, before));

            await new ReconcileRunner(discord, stoat).RunAsync(CancellationToken.None);

            var after = await stoat.GetSnapshotAsync(CancellationToken.None);

            var mirrorCategory = Assert.Single(after.Categories,
                c => c.Title == ChannelLink.CategoryTitle(prefix, sourceCategory.Id.ToString()));
            var mirrorChannel = Assert.Single(after.TextChannels,
                c => ChannelLink.SourceIdFromChannelDescription(c.Description) == sourceChannel.Id.ToString());

            Assert.Contains(mirrorChannel.Id, await stoatServer.GetCategoryChannelIdsAsync(mirrorCategory.Id));

            // Stoat IDs are ULIDs, which sort by creation time, so the category was created before the channel.
            Assert.True(string.CompareOrdinal(mirrorCategory.Id, mirrorChannel.Id) < 0,
                $"Mirror Category {mirrorCategory.Id} should be created before Mirror Channel {mirrorChannel.Id}.");
        }
    }

    private static async Task DeleteStoatResourcesCreatedSinceAsync(
        IStoatRepository stoat,
        StoatTestServer stoatServer,
        StoatSnapshot before)
    {
        var after = await stoat.GetSnapshotAsync(CancellationToken.None);

        var channelIdsBefore = before.TextChannels.Select(c => c.Id).ToHashSet();
        var categoryIdsBefore = before.Categories.Select(c => c.Id).ToHashSet();

        // Channels go first, so each category is empty when it is removed.
        foreach (var channel in after.TextChannels.Where(c => !channelIdsBefore.Contains(c.Id)))
        {
            await stoatServer.DeleteChannelAsync(channel.Id);
        }

        foreach (var category in after.Categories.Where(c => !categoryIdsBefore.Contains(c.Id)))
        {
            await stoatServer.RemoveCategoryAsync(category.Id);
        }
    }
}
