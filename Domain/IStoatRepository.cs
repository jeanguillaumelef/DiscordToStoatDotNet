namespace Domain;

public interface IStoatRepository
{
    Task<StoatSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    Task CreateCategoryAsync(string title, CancellationToken cancellationToken);
}
