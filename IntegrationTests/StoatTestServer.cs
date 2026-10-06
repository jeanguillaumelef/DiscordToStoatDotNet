using StoatSharp;
using StoatSharp.Rest;

namespace IntegrationTests;

/// <summary>Reads and deletes categories and channels in the test server with StoatSharp directly. StoatRepository only creates and reads, so the tests need their own delete path.</summary>
public sealed class StoatTestServer(string botToken, string serverId)
{
    private readonly StoatClient _client = new(ClientMode.Http);
    private bool _loggedIn;

    public async Task<IReadOnlyList<string>> GetCategoryChannelIdsAsync(string categoryId)
    {
        var server = await GetServerAsync();
        var category = server.Categories.Single(c => c.Id == categoryId);
        return [.. category.ChannelIds ?? []];
    }

    public async Task DeleteChannelAsync(string channelId)
    {
        await GetServerAsync();
        await _client.Rest.DeleteChannelAsync(channelId);
    }

    public async Task RemoveCategoryAsync(string categoryId)
    {
        var server = await GetServerAsync();
        await _client.Rest.RemoveServerCategory(server, categoryId);
    }

    private async Task<Server> GetServerAsync()
    {
        if (!_loggedIn)
        {
            await _client.LoginAsync(botToken, AccountType.Bot);
            _loggedIn = true;
        }

        return await _client.Rest.GetServerAsync(serverId)
            ?? throw new InvalidOperationException($"Stoat server {serverId} was not found or the bot is not a member.");
    }
}
