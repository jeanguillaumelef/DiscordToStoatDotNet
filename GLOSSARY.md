# DiscordToStoat

Mirrors the channel structure of a Discord server onto a Stoat server, one way, from Discord to Stoat.

## Language

**Bridge**:
The running application that keeps one Stoat server's channels in step with one Discord server's channels.
_Avoid_: Sync tool, bot (a bot account is only how the Bridge authenticates)

**Source Channel**:
A text or voice channel on the Discord server that the Bridge mirrors. Other Discord channel types are not mirrored.
_Avoid_: Discord channel

**Private Channel**:
A Discord channel that `@everyone` cannot view. The Bridge never mirrors one.
_Avoid_: Hidden channel, restricted channel

**Source Category**:
A Discord category that groups Source Channels.
_Avoid_: Discord folder, group

**Private Category**:
A Source Category that `@everyone` cannot view. The Bridge mirrors it only when it holds at least one public Source Channel, so those channels can stay grouped; its name is mirrored like any other category's.
_Avoid_: Hidden category, restricted category

**Mirror Channel**:
The Stoat text or voice channel that corresponds to one Source Channel.
_Avoid_: Stoat channel, copy, target channel

**Mirror Category**:
The Stoat category that corresponds to one Source Category.
_Avoid_: Stoat group

**Channel Description**:
The free-text description of a Source Channel (Discord calls it the channel topic). The Bridge copies it onto the Mirror Channel's description, after the Channel Link line. Discord wins: Reconcile overwrites whatever a Stoat admin wrote there. If it does not fit the Stoat length limit it is truncated and the Bridge reports a warning; the Channel Link line is never cut.
_Avoid_: Topic (the Discord term; use Channel Description in the domain), Mirror description (that is the whole Stoat field, which also holds the Channel Link)

**Channel Link**:
The association between a Source Channel or Source Category and its Mirror. For a channel, it is the Discord ID on the first line of the Mirror Channel's description. For a category, it is the Discord ID in the Mirror Category's title.
_Avoid_: Mapping, binding

**Reconcile**:
Bringing every Mirror Channel and Mirror Category into line with the current Discord structure (names, order, category membership, Channel Descriptions). Runs at startup and again on each live Discord change.
_Avoid_: Sync, refresh

**Archived Channel**:
A Mirror Channel or Mirror Category whose Source was deleted. Its name gets the suffix "(source archived)" and the Bridge never deletes it.
_Avoid_: Deleted channel, orphan
