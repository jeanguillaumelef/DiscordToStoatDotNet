using Domain;

namespace Domain.Tests;

public sealed class FakeDiscordRepository : IDiscordRepository
{
    private readonly DiscordCategory[] _categories;
    private readonly DiscordTextChannel[] _textChannels;

    public FakeDiscordRepository(params DiscordCategory[] categories)
        : this(categories, [])
    {
    }

    public FakeDiscordRepository(DiscordCategory[] categories, DiscordTextChannel[] textChannels)
    {
        _categories = categories;
        _textChannels = textChannels;
    }

    public Task<DiscordSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new DiscordSnapshot(_categories, _textChannels));
}
