namespace IntegrationTests;

public class TeardownLedgerTests
{
    [Fact]
    public async Task RunAsync_RunsEveryRegisteredDeletion_LastRegisteredFirst()
    {
        var ledger = new TeardownLedger();
        var order = new List<string>();
        ledger.Add("category", () => { order.Add("category"); return Task.CompletedTask; });
        ledger.Add("channel", () => { order.Add("channel"); return Task.CompletedTask; });

        await ledger.RunAsync();

        Assert.Equal(["channel", "category"], order);
    }

    [Fact]
    public async Task RunAsync_WhenADeletionFails_StillRunsTheOthersAndThrows()
    {
        var ledger = new TeardownLedger();
        var deleted = new List<string>();
        ledger.Add("category", () => { deleted.Add("category"); return Task.CompletedTask; });
        ledger.Add("channel", () => throw new HttpRequestException("503 from server"));

        var error = await Assert.ThrowsAsync<AggregateException>(() => ledger.RunAsync());

        Assert.Equal(["category"], deleted);
        Assert.Contains("channel", error.InnerExceptions.Single().Message);
    }

    [Fact]
    public async Task RunAsync_WhenEverythingSucceeds_DoesNotThrow()
    {
        var ledger = new TeardownLedger();
        ledger.Add("channel", () => Task.CompletedTask);

        await ledger.RunAsync();
    }
}
