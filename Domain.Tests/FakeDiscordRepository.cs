using Domain;

namespace Domain.Tests;

public sealed class FakeDiscordRepository(params DiscordCategory[] categories) : IDiscordRepository
{
    public Task<DiscordSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new DiscordSnapshot(categories));
}
