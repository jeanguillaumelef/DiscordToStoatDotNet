using Domain;

namespace Domain.Tests;

public sealed class FakeDiscordRepository : IDiscordRepository
{
    private readonly Category[] _categories;
    private readonly TextChannel[] _textChannels;
    private readonly DiscordVoiceChannel[] _voiceChannels;
    private readonly DiscordUnsupportedChannel[] _unsupportedChannels;

    public FakeDiscordRepository(params Category[] categories)
        : this(categories, [])
    {
    }

    public FakeDiscordRepository(Category[] categories, TextChannel[] textChannels)
        : this(categories, textChannels, [])
    {
    }

    public FakeDiscordRepository(Category[] categories, TextChannel[] textChannels, DiscordVoiceChannel[] voiceChannels)
        : this(categories, textChannels, voiceChannels, [])
    {
    }

    public FakeDiscordRepository(
        Category[] categories,
        TextChannel[] textChannels,
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
