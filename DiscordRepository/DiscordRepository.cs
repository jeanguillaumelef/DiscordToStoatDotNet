using Discord;
using Discord.Rest;
using Domain;

namespace DiscordRepository;

public sealed class DiscordRepository(DiscordRepositoryOptions options) : IDiscordRepository, IAsyncDisposable
{
    private readonly DiscordRestClient _client = new();
    private bool _loggedIn;

    public async Task<DiscordSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var requestOptions = new RequestOptions { CancelToken = cancellationToken };

        if (!_loggedIn)
        {
            await _client.LoginAsync(TokenType.Bot, options.BotToken);
            _loggedIn = true;
        }

        var guild = await _client.GetGuildAsync(options.GuildId, requestOptions)
            ?? throw new InvalidOperationException($"Discord guild {options.GuildId} was not found or the bot is not a member.");

        var channels = await guild.GetChannelsAsync(requestOptions);
        var categories = channels
            .OfType<RestCategoryChannel>()
            .OrderBy(c => c.Position)
            .Select(c => new DiscordCategory(c.Id.ToString(), c.Name, EveryoneCanView(guild, c)))
            .ToList();

        var textChannels = channels
            .OfType<RestTextChannel>()
            .OrderBy(c => c.Position)
            .Select(c => new DiscordTextChannel(c.Id.ToString(), c.Name, c.CategoryId?.ToString(), EveryoneCanView(guild, c)))
            .ToList();

        return new DiscordSnapshot(categories, textChannels);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }

    private static bool EveryoneCanView(RestGuild guild, RestGuildChannel channel)
    {
        var everyone = guild.EveryoneRole;
        var overwrite = channel.GetPermissionOverwrite(everyone);
        return overwrite?.ViewChannel switch
        {
            PermValue.Allow => true,
            PermValue.Deny => false,
            _ => everyone.Permissions.ViewChannel,
        };
    }
}
