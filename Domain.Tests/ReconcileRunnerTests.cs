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
                new Category("111", "General", true),
                new Category("222", "Gaming", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "General [111]", "Gaming [222]" }, stoat.CreatedCategoryTitles);
    }

    [Fact]
    public async Task SecondRun_CreatesNothing()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(new Category("111", "General", true)),
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
                new Category("111", "Public", true),
                new Category("222", "Staff", false)),
            stoat);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "Public [111]" }, stoat.CreatedCategoryTitles);
        Assert.Equal("222", Assert.Single(result.Skipped).SourceId);
    }

    [Fact]
    public async Task ExistingMirrorCategories_AreNotRecreated()
    {
        var stoat = new FakeStoatRepository(new Category("s1", "A [111]", true));
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                new Category("111", "A", true),
                new Category("222", "B", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "B [222]" }, stoat.CreatedCategoryTitles);
    }

    [Fact]
    public async Task PublicTextChannel_IsCreatedInItsMirrorCategoryWithLinkAsDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        var mirrorCategory = Assert.Single(snapshot.Categories);
        var created = Assert.Single(stoat.CreatedTextChannels);
        Assert.Equal(new CreatedTextChannel("chat", "501", mirrorCategory.Name), created);
    }

    [Fact]
    public async Task UncategorizedTextChannel_IsCreatedWithoutCategory()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new CreatedTextChannel("chat", "501", null), Assert.Single(stoat.CreatedTextChannels));
    }

    [Fact]
    public async Task SecondRun_CreatesNoChannels()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);
        var second = await runner.RunAsync(CancellationToken.None);

        Assert.Single(stoat.CreatedTextChannels);
        Assert.Empty(second.Changes);
    }

    [Fact]
    public async Task PrivateTextChannels_AreNotCreated()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("502", "staff", null, false)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.CreatedTextChannels);
    }

    [Fact]
    public async Task PublicVoiceChannel_IsCreatedInItsMirrorCategoryWithLinkAsDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        var mirrorCategory = Assert.Single(snapshot.Categories);
        var created = Assert.Single(stoat.CreatedVoiceChannels);
        Assert.Equal(new CreatedVoiceChannel("lounge", "601", mirrorCategory.Name), created);
    }

    [Fact]
    public async Task PrivateVoiceChannels_AreNotCreated()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [], [new DiscordVoiceChannel("602", "staff-voice", null, false)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.CreatedVoiceChannels);
    }
}
