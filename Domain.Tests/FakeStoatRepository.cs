using Domain;

namespace Domain.Tests;

public sealed class FakeStoatRepository : IStoatRepository
{
    private readonly List<StoatCategory> _categories;
    private readonly List<StoatTextChannel> _textChannels;
    private readonly List<string> _createdCategoryTitles = [];
    private readonly List<CreatedTextChannel> _createdTextChannels = [];

    public FakeStoatRepository(params StoatCategory[] categories)
        : this(categories, [])
    {
    }

    public FakeStoatRepository(StoatCategory[] categories, StoatTextChannel[] textChannels)
    {
        _categories = [.. categories];
        _textChannels = [.. textChannels];
    }

    public IReadOnlyList<CreatedTextChannel> CreatedTextChannels => _createdTextChannels;

    public IReadOnlyList<string> CreatedCategoryTitles => _createdCategoryTitles;

    public Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StoatSnapshot([.. _categories], [.. _textChannels]));

    public Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        _createdCategoryTitles.Add(title);
        _categories.Add(new StoatCategory($"created-{_categories.Count}", title));
        return Task.CompletedTask;
    }

    public Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken)
    {
        _createdTextChannels.Add(new CreatedTextChannel(name, description, categoryId));
        _textChannels.Add(new StoatTextChannel($"created-channel-{_textChannels.Count}", name, description));
        return Task.CompletedTask;
    }
}

public sealed record CreatedTextChannel(string Name, string Description, string? CategoryId);
