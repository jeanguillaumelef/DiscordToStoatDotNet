namespace IntegrationTests;

/// <summary>Collects the deletions an integration test owes and runs them at teardown. Runs the most recently registered first, so a channel goes before the category it sits in. A failing deletion does not stop the others; RunAsync throws once every deletion has been tried.</summary>
public sealed class TeardownLedger
{
    private readonly List<(string Description, Func<Task> Delete)> _deletions = [];

    public void Add(string description, Func<Task> delete) => _deletions.Add((description, delete));

    public async Task RunAsync()
    {
        var failures = new List<Exception>();

        for (var i = _deletions.Count - 1; i >= 0; i--)
        {
            var (description, delete) = _deletions[i];
            try
            {
                await delete();
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException($"Failed to delete {description}: {ex.Message}", ex));
            }
        }

        _deletions.Clear();

        if (failures.Count > 0)
        {
            throw new AggregateException("Integration test teardown failed.", failures);
        }
    }
}
