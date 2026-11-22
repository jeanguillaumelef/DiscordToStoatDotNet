# One-time Migration, not a live Bridge

The app copies a Discord server's channel structure onto a new, empty Stoat server once, ahead of a cutover after which Discord is retired. It no longer plans to keep Stoat in step with Discord: there are no live Discord events, no periodic runs, and no repair of drift or hand edits on either side.

An Import is create-only. It reads Stoat only to find which Sources already have a Mirror (by Channel Link, ADR 0001), creates the rest, and finally puts every Mirror in Discord's order. It never updates an existing Mirror's name or description. Stoat calls that fail on rate limits, server errors or the network are retried with backoff; when retries run out the Import fails, and running it again resumes, because what was already created is skipped.

## Considered Options

- **Live Bridge (the earlier plan).** Rejected: after a cutover there is nothing left to keep in step, and live sync carried most of the design's cost (gateway events, drift repair, archiving deleted Sources, Discord-wins overwrites).
- **Fire-and-forget into an empty server, wiping it after a failure.** Rejected: a large server means hundreds of rate-limited Stoat calls, so a failure partway through is likely and starting over is costly.
- **A local ledger of what was imported.** Rejected: the Channel Link already records it on Stoat, and a lost or stale ledger could produce duplicates.

## Consequences

- The Channel Link stays, though its original reason (a stateless, self-healing Bridge) is gone: it is what makes an Import resumable, and a later message migration will use it to find each Source Channel's Mirror.
- Changes made on Stoat between two Imports are not handled: duplicate or broken Channel Links, hand-renamed or hand-deleted Mirrors, and unlinked channels are all left as they are.
