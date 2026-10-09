using Domain;

namespace Domain.Tests;

public sealed class FakeStoatRepository : IStoatRepository
{
    private readonly List<Category> _categories;
    private readonly List<StoatTextChannel> _textChannels;
    private readonly List<StoatVoiceChannel> _voiceChannels;
    private readonly List<string> _createdCategoryTitles = [];
    private readonly List<CreatedTextChannel> _createdTextChannels = [];
    private readonly List<CreatedVoiceChannel> _createdVoiceChannels = [];
    private readonly List<UpdatedDescription> _descriptionUpdates = [];

    public FakeStoatRepository(params Category[] categories)
        : this(categories, [])
    {
    }

    public FakeStoatRepository(Category[] categories, StoatTextChannel[] textChannels)
        : this(categories, textChannels, [])
    {
    }

    public FakeStoatRepository(Category[] categories, StoatTextChannel[] textChannels, StoatVoiceChannel[] voiceChannels)
    {
        _categories = [.. categories];
        _textChannels = [.. textChannels];
        _voiceChannels = [.. voiceChannels];
    }

    public IReadOnlyList<CreatedTextChannel> CreatedTextChannels => _createdTextChannels;

    public IReadOnlyList<CreatedVoiceChannel> CreatedVoiceChannels => _createdVoiceChannels;

    public IReadOnlyList<UpdatedDescription> DescriptionUpdates => _descriptionUpdates;

    public IReadOnlyList<string> CreatedCategoryTitles => _createdCategoryTitles;

    public Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StoatSnapshot([.. _categories], [.. _textChannels], [.. _voiceChannels]));

    public Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        _createdCategoryTitles.Add(title);
        _categories.Add(new Category($"created-{_categories.Count}", title, true));
        return Task.CompletedTask;
    }

    public Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        _createdTextChannels.Add(new CreatedTextChannel(name, description, categoryId));
        _textChannels.Add(new StoatTextChannel($"created-channel-{_textChannels.Count}", name, description));
        return Task.CompletedTask;
    }

    public Task CreateVoiceChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        _createdVoiceChannels.Add(new CreatedVoiceChannel(name, description, categoryId));
        _voiceChannels.Add(new StoatVoiceChannel($"created-voice-channel-{_voiceChannels.Count}", name, description));
        return Task.CompletedTask;
    }

    public Task SetChannelDescriptionAsync(string channelId, string description, CancellationToken cancellationToken)
    {
        _descriptionUpdates.Add(new UpdatedDescription(channelId, description));
        var text = _textChannels.FindIndex(c => c.Id == channelId);
        if (text >= 0)
        {
            _textChannels[text] = _textChannels[text] with { Description = description };
        }

        var voice = _voiceChannels.FindIndex(c => c.Id == channelId);
        if (voice >= 0)
        {
            _voiceChannels[voice] = _voiceChannels[voice] with { Description = description };
        }

        return Task.CompletedTask;
    }
}

public sealed record CreatedTextChannel(string Name, string Description, string? CategoryId);

public sealed record CreatedVoiceChannel(string Name, string Description, string? CategoryId);
