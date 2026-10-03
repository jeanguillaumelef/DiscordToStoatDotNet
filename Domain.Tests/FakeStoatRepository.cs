using Domain;

namespace Domain.Tests;

public sealed class FakeStoatRepository(params StoatCategory[] categories) : IStoatRepository
{
    public Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new StoatSnapshot(categories));

    public Task CreateCategoryAsync(string title, CancellationToken cancellationToken) => Task.CompletedTask;
}
