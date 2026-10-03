using Domain;
using StoatSharp;

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
        return new StoatSnapshot(categories);
    }

    public async Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        var server = await GetServerAsync(cancellationToken);
        await server.AddCategoryAsync(title, server.Categories.Count);
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
