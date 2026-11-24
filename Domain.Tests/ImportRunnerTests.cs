using Domain;

namespace Domain.Tests;

public class ImportRunnerTests
{
    [Fact]
    public async Task MissingPublicCategories_AreCreatedWithMirrorTitles()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
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
        var runner = new ImportRunner(
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
        var runner = new ImportRunner(
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
        var stoat = new FakeStoatRepository(new StoatCategory("s1", "A [111]", []));
        var runner = new ImportRunner(
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
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, "")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        var mirrorCategory = Assert.Single(snapshot.Categories);
        var created = Assert.Single(stoat.CreatedTextChannels);
        Assert.Equal(new CreatedTextChannel("chat", "501", mirrorCategory.Id), created);
    }

    [Fact]
    public async Task UncategorizedTextChannel_IsCreatedWithoutCategory()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new CreatedTextChannel("chat", "501", null), Assert.Single(stoat.CreatedTextChannels));
    }

    [Fact]
    public async Task SecondRun_CreatesNoChannels()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, "")]),
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
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [new TextChannel("502", "staff", null, false, "")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.CreatedTextChannels);
    }

    [Fact]
    public async Task PublicVoiceChannel_IsCreatedInItsMirrorCategoryWithLinkAsDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        var mirrorCategory = Assert.Single(snapshot.Categories);
        var created = Assert.Single(stoat.CreatedVoiceChannels);
        Assert.Equal(new CreatedVoiceChannel("lounge", "601", mirrorCategory.Id), created);
    }

    [Fact]
    public async Task PrivateVoiceChannels_AreNotCreated()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [], [new DiscordVoiceChannel("602", "staff-voice", null, false)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.CreatedVoiceChannels);
    }

    [Fact]
    public async Task TextChannel_IsCreatedWithLinkThenChannelDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [],
                [new TextChannel("501", "chat", null, true, "Talk about anything")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal("501\nTalk about anything", Assert.Single(stoat.CreatedTextChannels).Description);
    }

    [Fact]
    public async Task TextChannel_WithoutChannelDescription_IsCreatedWithBareId()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "")]),
            stoat);

        var result = await runner.RunAsync(CancellationToken.None);

        Assert.Equal("501", Assert.Single(stoat.CreatedTextChannels).Description);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task TextChannel_WithTooLongChannelDescription_IsTruncatedToStoatLimitAndWarns()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, new string('a', 2000))]),
            stoat);

        var result = await runner.RunAsync(CancellationToken.None);

        var description = Assert.Single(stoat.CreatedTextChannels).Description;
        Assert.Equal("501\n" + new string('a', 1020), description);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("501", warning.SourceId);
        Assert.Contains("truncated", warning.Message);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task TextChannel_TruncationDoesNotSplitASurrogatePair()
    {
        var stoat = new FakeStoatRepository();
        // 1019 chars fit after "501\n"; the emoji (2 UTF-16 units) would straddle the 1024 limit.
        var topic = new string('a', 1019) + "😀" + "tail";
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, topic)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal("501\n" + new string('a', 1019), Assert.Single(stoat.CreatedTextChannels).Description);
    }

    [Fact]
    public async Task VoiceChannel_IsCreatedWithBareIdAsDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository([], [], [new DiscordVoiceChannel("601", "lounge", null, true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal("601", Assert.Single(stoat.CreatedVoiceChannels).Description);
    }

    [Fact]
    public async Task PartlyImportedServer_GetsOnlyTheMissingMirrors()
    {
        var stoat = new FakeStoatRepository(
            [new StoatCategory("s1", "General [111]", [])],
            [new StoatTextChannel("t1", "chat", "501")],
            [new StoatVoiceChannel("v1", "lounge", "601")]);
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true), new Category("222", "Gaming", true)],
                [new TextChannel("501", "chat", "111", true, ""), new TextChannel("502", "news", "111", true, "")],
                [new DiscordVoiceChannel("601", "lounge", "111", true), new DiscordVoiceChannel("602", "raid", "222", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "Gaming [222]" }, stoat.CreatedCategoryTitles);
        Assert.Equal(new[] { "news" }, stoat.CreatedTextChannels.Select(c => c.Name));
        Assert.Equal(new[] { "raid" }, stoat.CreatedVoiceChannels.Select(c => c.Name));
    }

    [Fact]
    public async Task ChannelsWhoseCategoryWasImportedEarlier_GoIntoThatMirrorCategory()
    {
        var stoat = new FakeStoatRepository(new StoatCategory("s1", "General [111]", []));
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, "")],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.CreatedCategoryTitles);
        Assert.Equal("s1", Assert.Single(stoat.CreatedTextChannels).CategoryId);
        Assert.Equal("s1", Assert.Single(stoat.CreatedVoiceChannels).CategoryId);
    }

    [Fact]
    public async Task SecondImportOfFullyImportedServer_MakesNoChanges()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, "topic")],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);
        var second = await runner.RunAsync(CancellationToken.None);

        Assert.Empty(second.Changes);
        Assert.Single(stoat.CreatedCategoryTitles);
        Assert.Single(stoat.CreatedTextChannels);
        Assert.Single(stoat.CreatedVoiceChannels);
    }

    [Fact]
    public async Task MirrorCategories_FollowDiscordCategoryOrder()
    {
        var stoat = new FakeStoatRepository(
            new StoatCategory("s2", "Gaming [222]", []),
            new StoatCategory("s1", "General [111]", []));
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                new Category("111", "General", true),
                new Category("222", "Gaming", true)),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        Assert.Equal(new[] { "s1", "s2" }, snapshot.Categories.Select(c => c.Id));
    }

    [Fact]
    public async Task MirrorChannels_FollowDiscordOrderWithinTheirCategory()
    {
        var stoat = new FakeStoatRepository(
            [new StoatCategory("s1", "General [111]", ["v1", "t2", "t1"])],
            [new StoatTextChannel("t1", "chat", "501"), new StoatTextChannel("t2", "news", "502")],
            [new StoatVoiceChannel("v1", "lounge", "601")]);
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, ""), new TextChannel("502", "news", "111", true, "")],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        Assert.Equal(new[] { "t1", "t2", "v1" }, Assert.Single(snapshot.Categories).ChannelIds);
    }

    [Fact]
    public async Task ResumedImport_EndsWithNewAndEarlierMirrorsInDiscordOrder()
    {
        var stoat = new FakeStoatRepository(
            [new StoatCategory("s1", "General [111]", ["t1"])],
            [new StoatTextChannel("t1", "news", "502")]);
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("000", "Welcome", true), new Category("111", "General", true)],
                [new TextChannel("501", "chat", "111", true, ""), new TextChannel("502", "news", "111", true, "")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        var snapshot = await stoat.GetSnapshotAsync(CancellationToken.None);
        Assert.Equal(new[] { "Welcome [000]", "General [111]" }, snapshot.Categories.Select(c => c.Name));
        var chat = Assert.Single(snapshot.TextChannels, c => c.Name == "chat");
        Assert.Equal(new[] { chat.Id, "t1" }, snapshot.Categories[1].ChannelIds);
    }

    [Fact]
    public async Task MirrorsAlreadyInDiscordOrder_AreNotReordered()
    {
        var stoat = new FakeStoatRepository(
            [new StoatCategory("s1", "General [111]", ["t1", "v1"]), new StoatCategory("s2", "Gaming [222]", [])],
            [new StoatTextChannel("t1", "chat", "501")],
            [new StoatVoiceChannel("v1", "lounge", "601")]);
        var runner = new ImportRunner(
            new FakeDiscordRepository(
                [new Category("111", "General", true), new Category("222", "Gaming", true)],
                [new TextChannel("501", "chat", "111", true, "")],
                [new DiscordVoiceChannel("601", "lounge", "111", true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(0, stoat.SetCategoryOrderCallCount);
    }
}
