using Domain;
using DiscordRepository;
using Microsoft.Extensions.Configuration;
using StoatRepository;

namespace IntegrationTests;

[Trait("Category", "Integration")]
[Collection(LiveServersCollection.Name)]
public class MirrorOrderTests
{
    [Fact]
    public async Task Import_PutsMirrorsInDiscordOrder_AfterSourcesAreReordered()
    {
        var settings = IntegrationSettings.Load(new ConfigurationBuilder()
            .AddUserSecrets<MirrorOrderTests>(optional: true)
            .Build());
        var guildId = ulong.Parse(settings.DiscordGuildId);

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
            var first = await discordGuild.CreateCategoryAsync($"{prefix}a");
            ledger.Add($"Discord category {first.Name}", () => first.DeleteAsync());
            var second = await discordGuild.CreateCategoryAsync($"{prefix}b");
            ledger.Add($"Discord category {second.Name}", () => second.DeleteAsync());

            var upper = await discordGuild.CreateTextChannelAsync($"{prefix}-upper", first.Id);
            ledger.Add($"Discord channel {upper.Name}", () => upper.DeleteAsync());
            var lower = await discordGuild.CreateTextChannelAsync($"{prefix}-lower", first.Id);
            ledger.Add($"Discord channel {lower.Name}", () => lower.DeleteAsync());

            // The run may create mirrors for other public channels in the guild too; teardown removes everything new since this snapshot.
            var before = await stoat.GetSnapshotAsync(CancellationToken.None);
            ledger.Add("Stoat resources created by the run", () => DeleteStoatResourcesCreatedSinceAsync(stoat, stoatServer, before));

            await new ImportRunner(discord, stoat).RunAsync(CancellationToken.None);

            // The Mirrors exist now; an Import never recreates them, so only the ordering step can move them.
            await discordGuild.SwapPositionsAsync(first, second);
            await discordGuild.SwapPositionsAsync(upper, lower);
            await new ImportRunner(discord, stoat).RunAsync(CancellationToken.None);

            var after = await stoat.GetSnapshotAsync(CancellationToken.None);
            var categoryIds = after.Categories.Select(c => c.Id).ToList();
            var firstMirror = Assert.Single(after.Categories, c => c.Name == ChannelLink.CategoryTitle(first.Name, first.Id.ToString()));
            var secondMirror = Assert.Single(after.Categories, c => c.Name == ChannelLink.CategoryTitle(second.Name, second.Id.ToString()));
            Assert.True(categoryIds.IndexOf(secondMirror.Id) < categoryIds.IndexOf(firstMirror.Id),
                "The Mirror Category of the category moved up on Discord should come first on Stoat.");

            var upperMirror = Assert.Single(after.TextChannels, c => ChannelLink.SourceIdFromChannelDescription(c.Description) == upper.Id.ToString());
            var lowerMirror = Assert.Single(after.TextChannels, c => ChannelLink.SourceIdFromChannelDescription(c.Description) == lower.Id.ToString());
            Assert.Equal(new[] { lowerMirror.Id, upperMirror.Id }, await stoatServer.GetCategoryChannelIdsAsync(firstMirror.Id));
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
