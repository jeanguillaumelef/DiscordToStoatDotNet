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
            .Select(c => new Category(c.Id, c.Name, true)) // Stoat does not support per-category permissions, so assume everyone can view.
            .ToList();

        // Server.TextChannels reads the websocket cache, which is null in ClientMode.Http, so
        // read the server's channel IDs over REST and fetch each channel instead.
        var textChannels = new List<StoatTextChannel>();
        var voiceChannels = new List<StoatVoiceChannel>();
        foreach (var channelId in await GetChannelIdsAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Stoat returns a voice channel as a "TextChannel" carrying a "voice" object, which StoatSharp's
            // typed channels drop, so read the raw payload (ADR 0003). "VoiceChannel" is the legacy type.
            var channel = await _client.Rest.SendRequestAsync<StoatChannelResponse>(RequestType.Get, $"/channels/{channelId}");
            var name = channel?.Name ?? string.Empty;
            var description = channel?.Description ?? string.Empty;

            switch (channel?.ChannelType)
            {
                case "TextChannel" when channel.Voice is not null:
                case "VoiceChannel":
                    voiceChannels.Add(new StoatVoiceChannel(channelId, name, description));
                    break;
                case "TextChannel":
                    textChannels.Add(new StoatTextChannel(channelId, name, description));
                    break;
            }
        }

        return new StoatSnapshot(categories, textChannels, voiceChannels);
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
        await PlaceInCategoryAsync(server, channel.Id, categoryId);
    }

    public async Task CreateVoiceChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        var server = await GetServerAsync(cancellationToken);
        var channel = await server.CreateVoiceChannelAsync(name, description);
        await PlaceInCategoryAsync(server, channel.Id, categoryId);
    }

    private async Task PlaceInCategoryAsync(Server server, string channelId, string? categoryId)
    {
        if (categoryId is null)
        {
            return;
        }

        var category = server.Categories.FirstOrDefault(c => c.Id == categoryId)
            ?? throw new InvalidOperationException($"Stoat category {categoryId} was not found.");
        // category.ModifyAsync resolves its server through the websocket cache, which is null in ClientMode.Http.
        await _client.Rest.ModifyServerCategoryAsync(server, categoryId, channels: new Option<string[]>([.. category.ChannelIds ?? [], channelId]));
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
