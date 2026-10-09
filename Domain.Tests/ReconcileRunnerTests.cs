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
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "")]),
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
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("502", "staff", null, false, "")]),
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
        Assert.Equal(new CreatedVoiceChannel("lounge", "601", mirrorCategory.Id), created);
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

    [Fact]
    public async Task TextChannel_IsCreatedWithLinkThenChannelDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
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
        var runner = new ReconcileRunner(
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
        var runner = new ReconcileRunner(
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
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, topic)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal("501\n" + new string('a', 1019), Assert.Single(stoat.CreatedTextChannels).Description);
    }

    [Fact]
    public async Task VoiceChannel_IsCreatedWithBareIdAsDescription()
    {
        var stoat = new FakeStoatRepository();
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [], [new DiscordVoiceChannel("601", "lounge", null, true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal("601", Assert.Single(stoat.CreatedVoiceChannels).Description);
    }

    [Fact]
    public async Task TextChannel_WhoseDescriptionDrifted_IsUpdatedWithDiscordChannelDescription()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "chat", "501\nold topic")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "new topic")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new UpdatedDescription("s1", "501\nnew topic"), Assert.Single(stoat.DescriptionUpdates));
        Assert.Empty(stoat.CreatedTextChannels);
    }

    [Fact]
    public async Task TextChannel_ExistingMirrorWithBareId_GetsChannelDescription()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "chat", "501")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "topic")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new UpdatedDescription("s1", "501\ntopic"), Assert.Single(stoat.DescriptionUpdates));
    }

    [Fact]
    public async Task TextChannel_WhoseTopicWasRemoved_IsReducedToBareId()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "chat", "501\nstale")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new UpdatedDescription("s1", "501"), Assert.Single(stoat.DescriptionUpdates));
    }

    [Fact]
    public async Task TextChannel_AlreadyInStep_IsNotUpdated_AndSecondRunChangesNothing()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "chat", "501\nsame"), new StoatTextChannel("s2", "other", "502\nold")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "same"), new TextChannel("502", "other", null, true, "new")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);
        var second = await runner.RunAsync(CancellationToken.None);

        Assert.Equal("s2", Assert.Single(stoat.DescriptionUpdates).ChannelId);
        Assert.Empty(second.Changes);
    }

    [Fact]
    public async Task TextChannel_WithInvalidChannelLink_IsNeverUpdated()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "chat", "not a link\ntopic")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "chat", null, true, "topic")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.DescriptionUpdates);
    }

    [Fact]
    public async Task PrivateTextChannel_ExistingMirror_IsNeverUpdated()
    {
        var stoat = new FakeStoatRepository([], [new StoatTextChannel("s1", "staff", "501")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [new TextChannel("501", "staff", null, false, "secret")]),
            stoat);

        await runner.RunAsync(CancellationToken.None);

        Assert.Empty(stoat.DescriptionUpdates);
    }

    [Fact]
    public async Task VoiceChannel_WhoseDescriptionDrifted_IsResetToBareId()
    {
        var stoat = new FakeStoatRepository([], [], [new StoatVoiceChannel("v1", "lounge", "601\nold")]);
        var runner = new ReconcileRunner(
            new FakeDiscordRepository([], [], [new DiscordVoiceChannel("601", "lounge", null, true)]),
            stoat);

        await runner.RunAsync(CancellationToken.None);
        var second = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(new UpdatedDescription("v1", "601"), Assert.Single(stoat.DescriptionUpdates));
        Assert.Empty(second.Changes);
    }
}
