namespace Domain;

public interface IDiscordRepository
{
    Task<DiscordSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
