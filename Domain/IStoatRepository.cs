namespace Domain;

public interface IStoatRepository
{
    Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    Task CreateCategoryAsync(string title, CancellationToken cancellationToken);

    /// <param name="categoryId">The Stoat category to place the channel in, or null to leave it wherever Stoat puts it.</param>
    Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken);

    /// <param name="categoryId">The Stoat category to place the channel in, or null to leave it wherever Stoat puts it.</param>
    Task CreateVoiceChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken);
}
