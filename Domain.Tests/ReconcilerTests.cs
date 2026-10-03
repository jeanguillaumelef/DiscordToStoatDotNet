using Domain;

namespace Domain.Tests;

public class ReconcilerTests
{
    private static async Task<ReconcileResult> ReconcileAsync(
        FakeDiscordRepository discord, FakeStoatRepository stoat)
    {
        var d = await discord.GetSnapshotAsync(CancellationToken.None);
        var s = await stoat.GetSnapshotAsync(CancellationToken.None);
        return Reconciler.Reconcile(d, s);
    }

    [Fact]
    public async Task PublicCategoriesWithoutMirror_ProduceCreateChangePerCategory()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(
                new DiscordCategory("111", "General", true),
                new DiscordCategory("222", "Gaming", true)),
            new FakeStoatRepository());

        Assert.Equal(
            new Change[] { new CreateCategory("111", "General [111]"), new CreateCategory("222", "Gaming [222]") },
            result.Changes);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task PrivateCategory_ProducesNoCreateAndIsSkipped()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(
                new DiscordCategory("111", "Public", true),
                new DiscordCategory("222", "Staff", false)),
            new FakeStoatRepository());

        Assert.Equal(new Change[] { new CreateCategory("111", "Public [111]") }, result.Changes);
        var skipped = Assert.Single(result.Skipped);
        Assert.Equal("222", skipped.SourceId);
        Assert.False(string.IsNullOrWhiteSpace(skipped.Reason));
    }

    [Fact]
    public async Task PrivateCategoryWithExistingMirror_IsStillSkipped()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("222", "Staff", false)),
            new FakeStoatRepository(new StoatCategory("s1", "Staff [222]")));

        Assert.Empty(result.Changes);
        Assert.Equal("222", Assert.Single(result.Skipped).SourceId);
    }

    [Fact]
    public async Task EmptyPublicCategory_IsStillMirrored()
    {
        // The snapshot carries no channels at all; the category must be mirrored regardless.
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("333", "Empty", true)),
            new FakeStoatRepository());

        Assert.Equal(new Change[] { new CreateCategory("333", "Empty [333]") }, result.Changes);
        Assert.Empty(result.Skipped);
    }

    [Theory]
    [InlineData("General")]
    [InlineData("General []")]
    [InlineData("General [abc]")]
    [InlineData("General [111] extra")]
    [InlineData("[111] General")]
    [InlineData("General 111")]
    public async Task MirrorWithoutValidChannelLink_IsIgnoredAndCreateStillProduced(string stoatTitle)
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("111", "General", true)),
            new FakeStoatRepository(new StoatCategory("s1", stoatTitle)));

        Assert.Equal(new Change[] { new CreateCategory("111", "General [111]") }, result.Changes);
    }

    [Fact]
    public async Task MirrorWithValidChannelLink_SuppressesCreate()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("111", "General", true)),
            new FakeStoatRepository(new StoatCategory("s1", "General [111]")));

        Assert.Empty(result.Changes);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task MirrorLinkMatchesOnIdNotName_SoRenamedMirrorSuppressesCreate()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("111", "New Name", true)),
            new FakeStoatRepository(new StoatCategory("s1", "Old Name [111]")));

        Assert.Empty(result.Changes);
    }

    [Fact]
    public async Task MirrorForDifferentSourceId_DoesNotSuppressCreate()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(new DiscordCategory("111", "General", true)),
            new FakeStoatRepository(new StoatCategory("s1", "General [999]")));

        Assert.Equal(new Change[] { new CreateCategory("111", "General [111]") }, result.Changes);
    }

    [Fact]
    public async Task OnlyMissingMirrorsAreCreated_WhenSomeAlreadyExist()
    {
        var result = await ReconcileAsync(
            new FakeDiscordRepository(
                new DiscordCategory("111", "A", true),
                new DiscordCategory("222", "B", true)),
            new FakeStoatRepository(new StoatCategory("s1", "A [111]")));

        Assert.Equal(new Change[] { new CreateCategory("222", "B [222]") }, result.Changes);
    }

    [Fact]
    public async Task NoCategories_ProducesNothing()
    {
        var result = await ReconcileAsync(new FakeDiscordRepository(), new FakeStoatRepository());

        Assert.Empty(result.Changes);
        Assert.Empty(result.Skipped);
    }
}
