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
            .Select(c => new Category(c.Id.ToString(), c.Name, EveryoneCanView(guild, c)))
            .ToList();

        var textChannels = new List<DiscordTextChannel>();
        var voiceChannels = new List<DiscordVoiceChannel>();
        var unsupportedChannels = new List<DiscordUnsupportedChannel>();

        foreach (var channel in channels.Where(c => c.ChannelType != ChannelType.Category).OrderBy(c => c.Position))
        {
            switch (channel.ChannelType)
            {
                case ChannelType.Text:
                    textChannels.Add(new DiscordTextChannel(channel.Id.ToString(), channel.Name, ((INestedChannel)channel).CategoryId?.ToString(), EveryoneCanView(guild, channel)));
                    break;
                case ChannelType.Voice:
                    voiceChannels.Add(new DiscordVoiceChannel(channel.Id.ToString(), channel.Name, ((INestedChannel)channel).CategoryId?.ToString(), EveryoneCanView(guild, channel)));
                    break;
                default:
                    unsupportedChannels.Add(new DiscordUnsupportedChannel(channel.Id.ToString(), UnsupportedTypeName(channel.ChannelType)));
                    break;
            }
        }

        // Threads aren't returned by GetChannelsAsync (the "list guild channels" endpoint excludes them);
        // each container channel (text/news/forum) must be asked for its own active threads. RestVoiceChannel
        // also implements IThreadContainerChannel (inherited via RestTextChannel) but throws NotSupportedException
        // if actually called, since voice channels don't support threads.
        foreach (var container in channels.OfType<IThreadContainerChannel>().Where(c => c is not RestVoiceChannel))
        {
            foreach (var thread in await container.GetActiveThreadsAsync(requestOptions))
            {
                unsupportedChannels.Add(new DiscordUnsupportedChannel(thread.Id.ToString(), "thread"));
            }
        }

        return new DiscordSnapshot(categories, textChannels, voiceChannels, unsupportedChannels);
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

    private static string UnsupportedTypeName(ChannelType type) => type switch
    {
        ChannelType.News => "announcement",
        ChannelType.Stage => "stage",
        ChannelType.Forum or ChannelType.Media => "forum",
        ChannelType.PublicThread or ChannelType.PrivateThread or ChannelType.NewsThread => "thread",
        _ => type.ToString(),
    };
}
