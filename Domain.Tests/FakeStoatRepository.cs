using Domain;

namespace Domain.Tests;

public sealed class FakeStoatRepository(params StoatCategory[] categories) : IStoatRepository
{
    private readonly List<StoatCategory> _categories = [.. categories];
    private readonly List<string> _createdCategoryTitles = [];

    public IReadOnlyList<string> CreatedCategoryTitles => _createdCategoryTitles;

    public Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StoatSnapshot([.. _categories]));

    public Task CreateCategoryAsync(string title, CancellationToken cancellationToken)
    {
        _createdCategoryTitles.Add(title);
        _categories.Add(new StoatCategory($"created-{_categories.Count}", title));
        return Task.CompletedTask;
    }
}
