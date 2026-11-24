using Domain;
using DiscordRepository;
using Microsoft.Extensions.Configuration;
using StoatRepository;

namespace IntegrationTests;

[Trait("Category", "Integration")]
[Collection(LiveServersCollection.Name)]
public class PublicChannelMirrorTests
{
    [Fact]
    public async Task Import_MirrorsPublicChannelUnderItsCategory_CategoryFirst()
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

            var sourceChannel = await discordGuild.CreateTextChannelAsync($"{prefix}-channel", sourceCategory.Id, "chat topic");
            ledger.Add($"Discord channel {sourceChannel.Name}", () => sourceChannel.DeleteAsync());

            // The run may create mirrors for other public channels in the guild too; teardown removes everything new since this snapshot.
            var before = await stoat.GetSnapshotAsync(CancellationToken.None);
            ledger.Add("Stoat resources created by the run", () => DeleteStoatResourcesCreatedSinceAsync(stoat, stoatServer, before));

            await new ImportRunner(discord, stoat).RunAsync(CancellationToken.None);

            var after = await stoat.GetSnapshotAsync(CancellationToken.None);

            var mirrorCategory = Assert.Single(after.Categories,
                c => c.Name == ChannelLink.CategoryTitle(prefix, sourceCategory.Id.ToString()));
            var mirrorChannel = Assert.Single(after.TextChannels,
                c => ChannelLink.SourceIdFromChannelDescription(c.Description) == sourceChannel.Id.ToString());

            Assert.Contains(mirrorChannel.Id, await stoatServer.GetCategoryChannelIdsAsync(mirrorCategory.Id));

            // The Channel Description is written after the Channel Link, and "\n" must round-trip through Stoat unchanged.
            Assert.Equal($"{sourceChannel.Id}\nchat topic", mirrorChannel.Description);

            // An Import is create-only (ADR 0004): a Channel Description edited on Discord leaves the existing Mirror unchanged.
            await sourceChannel.ModifyAsync(properties => properties.Topic = "edited topic");
            await new ImportRunner(discord, stoat).RunAsync(CancellationToken.None);
            var second = await stoat.GetSnapshotAsync(CancellationToken.None);
            var unchanged = Assert.Single(second.TextChannels,
                c => ChannelLink.SourceIdFromChannelDescription(c.Description) == sourceChannel.Id.ToString());
            Assert.Equal(mirrorChannel, unchanged);

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

        var channelIdsBefore = before.TextChannels.Select(c => c.Id)
            .Concat(before.VoiceChannels.Select(c => c.Id))
            .ToHashSet();
        var categoryIdsBefore = before.Categories.Select(c => c.Name).ToHashSet();

        // Channels go first, so each category is empty when it is removed.
        var newChannelIds = after.TextChannels.Select(c => c.Id)
            .Concat(after.VoiceChannels.Select(c => c.Id))
            .Where(id => !channelIdsBefore.Contains(id));

        foreach (var channelId in newChannelIds)
        {
            await stoatServer.DeleteChannelAsync(channelId);
        }

        foreach (var category in after.Categories.Where(c => !categoryIdsBefore.Contains(c.Name)))
        {
            await stoatServer.RemoveCategoryAsync(category.Name);
        }
    }
}
