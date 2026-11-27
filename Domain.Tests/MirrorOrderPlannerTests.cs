using Domain;

namespace Domain.Tests;

public class MirrorOrderPlannerTests
{
    [Fact]
    public void CategoriesWithoutAKnownSource_GoAfterMirrorCategoriesInTheirCurrentOrder()
    {
        var discord = new DiscordSnapshot(
            [new Category("111", "General", true), new Category("222", "Gaming", true)],
            [], [], []);
        var stoat = new StoatSnapshot(
            [
                new StoatCategory("u1", "Hand-made", []),
                new StoatCategory("s2", "Gaming [222]", []),
                new StoatCategory("gone", "Deleted on Discord [999]", []),
                new StoatCategory("s1", "General [111]", []),
                new StoatCategory("u2", "Also hand-made", []),
            ],
            [], []);

        var order = MirrorOrderPlanner.Plan(discord, stoat);

        Assert.Equal(new[] { "s1", "s2", "u1", "gone", "u2" }, order.Select(o => o.CategoryId));
    }

    [Fact]
    public void ChannelsWhoseSourceIsNotInThatCategory_StayInItAfterTheMirrorsInTheirCurrentOrder()
    {
        var discord = new DiscordSnapshot(
            [new Category("111", "General", true), new Category("222", "Gaming", true)],
            [new TextChannel("501", "chat", "111", true, ""), new TextChannel("502", "moved", "222", true, "")],
            [],
            []);
        var stoat = new StoatSnapshot(
            [new StoatCategory("s1", "General [111]", ["unlinked", "moved", "t1", "gone"]), new StoatCategory("s2", "Gaming [222]", [])],
            [
                new StoatTextChannel("unlinked", "hand-made", "no link here"),
                new StoatTextChannel("moved", "moved", "502"),
                new StoatTextChannel("t1", "chat", "501"),
                new StoatTextChannel("gone", "old", "999"),
            ],
            []);

        var order = MirrorOrderPlanner.Plan(discord, stoat);

        Assert.Equal(new[] { "t1", "unlinked", "moved", "gone" }, order[0].ChannelIds);
        Assert.Empty(order[1].ChannelIds);
    }

    [Fact]
    public void MirrorsWhoseSourceIsNowPrivate_GoAfterTheMirrorsInTheirCurrentOrder()
    {
        var discord = new DiscordSnapshot(
            [new Category("222", "Staff", false), new Category("111", "General", true)],
            [new TextChannel("501", "secret", "111", false, ""), new TextChannel("502", "chat", "111", true, "")],
            [],
            []);
        var stoat = new StoatSnapshot(
            [new StoatCategory("s2", "Staff [222]", []), new StoatCategory("s1", "General [111]", ["t1", "t2"])],
            [new StoatTextChannel("t1", "secret", "501"), new StoatTextChannel("t2", "chat", "502")],
            []);

        var order = MirrorOrderPlanner.Plan(discord, stoat);

        Assert.Equal(new[] { "s1", "s2" }, order.Select(o => o.CategoryId));
        Assert.Equal(new[] { "t2", "t1" }, order[0].ChannelIds);
    }
}
