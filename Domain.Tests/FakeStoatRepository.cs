using Domain;

namespace Domain.Tests;

public sealed class FakeStoatRepository : IStoatRepository
{
    private readonly List<StoatCategory> _categories;
    private readonly List<StoatTextChannel> _textChannels;
    private readonly List<StoatVoiceChannel> _voiceChannels;
    private readonly List<string> _createdCategoryTitles = [];
    private readonly List<CreatedTextChannel> _createdTextChannels = [];
    private readonly List<CreatedVoiceChannel> _createdVoiceChannels = [];

    public FakeStoatRepository(params StoatCategory[] categories)
        : this(categories, [])
    {
    }

    public FakeStoatRepository(StoatCategory[] categories, StoatTextChannel[] textChannels)
        : this(categories, textChannels, [])
    {
    }

    public FakeStoatRepository(StoatCategory[] categories, StoatTextChannel[] textChannels, StoatVoiceChannel[] voiceChannels)
    {
        _categories = [.. categories];
        _textChannels = [.. textChannels];
        _voiceChannels = [.. voiceChannels];
    }

    public IReadOnlyList<CreatedTextChannel> CreatedTextChannels => _createdTextChannels;

    public IReadOnlyList<CreatedVoiceChannel> CreatedVoiceChannels => _createdVoiceChannels;

    public IReadOnlyList<string> CreatedCategoryTitles => _createdCategoryTitles;

    public int SetCategoryOrderCallCount { get; private set; }

    public Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StoatSnapshot([.. _categories], [.. _textChannels], [.. _voiceChannels]));

    public Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        _createdCategoryTitles.Add(title);
        _categories.Add(new StoatCategory($"created-{_categories.Count}", title, []));
        return Task.CompletedTask;
    }

    public Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        var id = $"created-channel-{_textChannels.Count}";
        _createdTextChannels.Add(new CreatedTextChannel(name, description, categoryId));
        _textChannels.Add(new StoatTextChannel(id, name, description));
        AddToCategory(id, categoryId);
        return Task.CompletedTask;
    }

    public Task CreateVoiceChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        var id = $"created-voice-channel-{_voiceChannels.Count}";
        _createdVoiceChannels.Add(new CreatedVoiceChannel(name, description, categoryId));
        _voiceChannels.Add(new StoatVoiceChannel(id, name, description));
        AddToCategory(id, categoryId);
        return Task.CompletedTask;
    }

    public Task SetCategoryOrderAsync(IReadOnlyList<CategoryOrder> categories, CancellationToken cancellationToken)
    {
        SetCategoryOrderCallCount++;
        var byId = _categories.ToDictionary(c => c.Id);
        _categories.Clear();
        _categories.AddRange(categories.Select(o => byId[o.CategoryId] with { ChannelIds = o.ChannelIds }));
        return Task.CompletedTask;
    }

    // Stoat appends a channel placed in a category to the end of that category's channel list.
    private void AddToCategory(string channelId, string? categoryId)
    {
        var index = _categories.FindIndex(c => c.Id == categoryId);
        if (index >= 0)
        {
            _categories[index] = _categories[index] with { ChannelIds = [.. _categories[index].ChannelIds, channelId] };
        }
    }
}

public sealed record CreatedTextChannel(string Name, string Description, string? CategoryId);

public sealed record CreatedVoiceChannel(string Name, string Description, string? CategoryId);
