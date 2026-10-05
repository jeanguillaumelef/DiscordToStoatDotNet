using Domain;
using StoatSharp;
using StoatSharp.Rest;

namespace StoatRepository;

public sealed class StoatRepository(StoatRepositoryOptions options) : IStoatRepository
{
    private readonly StoatClient _client = new(ClientMode.Http);
    private bool _loggedIn;

    public async Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var server = await GetServerAsync(cancellationToken);
        
        var categories = server.Categories
            .OrderBy(c => c.Position)
            .Select(c => new StoatCategory(c.Id, c.Name))
            .ToList();
        
        // Server.TextChannels reads the websocket cache, which is null in ClientMode.Http, so
        // read the server's channel IDs over REST and fetch each channel instead.
        var textChannels = new List<StoatTextChannel>();
        foreach (var channelId in await GetChannelIdsAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _client.Rest.GetChannelAsync(channelId) is TextChannel textChannel)
            {
                textChannels.Add(new StoatTextChannel(
                    textChannel.Id,
                    textChannel.Name,
                    textChannel.Description ?? string.Empty));
            }
        }

        return new StoatSnapshot(categories, textChannels);
    }

    public async Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        var server = await GetServerAsync(cancellationToken);
        await server.AddCategoryAsync(title, server.Categories.Count);
    }

    public async Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        var server = await GetServerAsync(cancellationToken);
        var channel = await server.CreateTextChannelAsync(name, description);

        if (categoryId is null)
        {
            return;
        }

        var category = server.Categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException($"Stoat category {categoryId} was not found.");
        // category.ModifyAsync resolves its server through the websocket cache, which is null in ClientMode.Http.
        await _client.Rest.ModifyServerCategoryAsync(server, categoryId, channels: new Option<string[]>([.. category.ChannelIds ?? [], channel.Id]));
    }

    private async Task<IReadOnlyList<string>> GetChannelIdsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = await _client.Rest.SendRequestAsync<ServerChannelsResponse>(
            RequestType.Get,
            $"/servers/{options.ServerId}");

        return response?.Channels ?? [];
    }

    private async Task<Server> GetServerAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_loggedIn)
        {
            await _client.LoginAsync(options.BotToken, AccountType.Bot);
            _loggedIn = true;
        }

        return await _client.Rest.GetServerAsync(options.ServerId)
            ?? throw new InvalidOperationException($"Stoat server {options.ServerId} was not found or the bot is not a member.");
    }
}
