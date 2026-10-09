using Domain;

namespace Domain.Tests;

public sealed class FakeDiscordRepository : IDiscordRepository
{
    private readonly Category[] _categories;
    private readonly DiscordTextChannel[] _textChannels;
    private readonly DiscordVoiceChannel[] _voiceChannels;
    private readonly DiscordUnsupportedChannel[] _unsupportedChannels;

    public FakeDiscordRepository(params Category[] categories)
        : this(categories, [])
    {
    }

    public FakeDiscordRepository(Category[] categories, DiscordTextChannel[] textChannels)
        : this(categories, textChannels, [])
    {
    }

    public FakeDiscordRepository(Category[] categories, DiscordTextChannel[] textChannels, DiscordVoiceChannel[] voiceChannels)
        : this(categories, textChannels, voiceChannels, [])
    {
    }

    public FakeDiscordRepository(
        Category[] categories,
        DiscordTextChannel[] textChannels,
        DiscordVoiceChannel[] voiceChannels,
        DiscordUnsupportedChannel[] unsupportedChannels)
    {
        _categories = categories;
        _textChannels = textChannels;
        _voiceChannels = voiceChannels;
        _unsupportedChannels = unsupportedChannels;
    }

    public Task<DiscordSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new DiscordSnapshot(_categories, _textChannels, _voiceChannels, _unsupportedChannels));
}
