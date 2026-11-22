# DiscordToStoat

Migrates the channel structure of a Discord server onto a new Stoat server, once, ahead of a cutover from Discord to Stoat.

## Language

**Migration**:
The application that copies one Discord server's channel structure onto one Stoat server, for a one-time cutover. It does not keep the two in step afterwards.
_Avoid_: Bridge, sync tool, bot (a bot account is only how the Migration authenticates)

**Import**:
One run of the Migration: it creates a Mirror for every Source that has none yet, then puts every Mirror in Discord's order. A Mirror that already exists is never changed, so a failed Import is finished by running another.
_Avoid_: Reconcile, sync, refresh

**Source Channel**:
A text or voice channel on the Discord server that the Migration mirrors. Other Discord channel types are not mirrored.
_Avoid_: Discord channel

**Private Channel**:
A Discord channel that `@everyone` cannot view. The Migration never mirrors one.
_Avoid_: Hidden channel, restricted channel

**Source Category**:
A Discord category that groups Source Channels.
_Avoid_: Discord folder, group

**Private Category**:
A Source Category that `@everyone` cannot view. The Migration mirrors it only when it holds at least one public Source Channel, so those channels can stay grouped; its name is mirrored like any other category's.
_Avoid_: Hidden category, restricted category

**Mirror Channel**:
The Stoat text or voice channel that corresponds to one Source Channel.
_Avoid_: Stoat channel, copy, target channel

**Mirror Category**:
The Stoat category that corresponds to one Source Category.
_Avoid_: Stoat group

**Channel Description**:
The free-text description of a text Source Channel (Discord calls it the channel topic; voice channels have none, so a voice Mirror Channel carries only the Channel Link). The Import copies it onto the Mirror Channel's description, after the Channel Link line, when it creates the Mirror. If it does not fit the Stoat length limit it is truncated and the Import reports a warning; the Channel Link line is never cut.
_Avoid_: Topic (the Discord term; use Channel Description in the domain), Mirror description (that is the whole Stoat field, which also holds the Channel Link)

**Channel Link**:
The association between a Source Channel or Source Category and its Mirror. For a channel, it is the Discord ID on the first line of the Mirror Channel's description. For a category, it is the Discord ID in the Mirror Category's title. A Source whose Channel Link is already on Stoat counts as imported.
_Avoid_: Mapping, binding
