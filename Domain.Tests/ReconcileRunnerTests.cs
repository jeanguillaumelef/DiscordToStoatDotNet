using Domain;

namespace Domain.Tests;

public class ReconcileRunnerTests
{
    [Fact]
    public async Task MissingPublicCategories_AreCreatedWithMirrorTitles()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                new DiscordCategory("111", "General", true),
                new DiscordCategory("222", "Gaming", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "General [111]", "Gaming [222]" }, stoat.CreatedCategoryTitles);
    }

    [Fact]
    public async Task SecondRun_CreatesNothing()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(new DiscordCategory("111", "General", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);
        var second = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "General [111]" }, stoat.CreatedCategoryTitles);
        Assert.Empty(second.Changes);
    }

    [Fact]
    public async Task PrivateCategories_AreNotCreatedAndAreSkipped()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                new DiscordCategory("111", "Public", true),
                new DiscordCategory("222", "Staff", false)),
            stoat);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "Public [111]" }, stoat.CreatedCategoryTitles);
        Assert.Equal("222", Assert.Single(result.Skipped).SourceId);
    }

    [Fact]
    public async Task ExistingMirrorCategories_AreNotRecreated()
    {
        var stoat = new FakeStoatRepository(new StoatCategory("s1", "A [111]"));
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                new DiscordCategory("111", "A", true),
                new DiscordCategory("222", "B", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "B [222]" }, stoat.CreatedCategoryTitles);
    }
}
