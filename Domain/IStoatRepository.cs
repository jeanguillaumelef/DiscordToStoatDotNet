namespace Domain;

public interface IStoatRepository
{
    Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    Task CreateCategoryAsync(string title, CancellationToken cancellationToken);

    /// <param name="categoryId">The Stoat category to place the channel in, or null to leave it wherever Stoat puts it.</param>
    Task CreateTextChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken);

    /// <param name="categoryId">The Stoat category to place the channel in, or null to leave it wherever Stoat puts it.</param>
    Task CreateVoiceChannelAsync(string name, string description, string? categoryId, CancellationToken cancellationToken);

    /// <summary>Sets the order of the server's categories and of the channels within each one.</summary>
    /// <param name="categories">Every Stoat category on the server, in order, each with its full channel list in order.</param>
    Task SetCategoryOrderAsync(IReadOnlyList<CategoryOrder> categories, CancellationToken cancellationToken);
}
