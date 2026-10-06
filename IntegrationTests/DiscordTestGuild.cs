using Discord;
using Discord.Rest;

namespace IntegrationTests;

/// <summary>Creates and deletes channels in the test guild with Discord.Net directly. DiscordRepository only reads, so the tests need their own write path.</summary>
public sealed class DiscordTestGuild(string botToken, ulong guildId) : IAsyncDisposable
{
    private readonly DiscordRestClient _client = new();
    private RestGuild? _guild;

    public async Task<RestCategoryChannel> CreateCategoryAsync(string name)
    {
        var guild = await GetGuildAsync();
        return await guild.CreateCategoryChannelAsync(name);
    }

    public async Task<RestTextChannel> CreateTextChannelAsync(string name, ulong categoryId)
    {
        var guild = await GetGuildAsync();
        return await guild.CreateTextChannelAsync(name, properties => properties.CategoryId = categoryId);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    private async Task<RestGuild> GetGuildAsync()
    {
        if (_guild is null)
        {
            await _client.LoginAsync(TokenType.Bot, botToken);
            _guild = await _client.GetGuildAsync(guildId)
                ?? throw new InvalidOperationException($"Discord guild {guildId} was not found or the bot is not a member.");
        }

        return _guild;
    }
}
