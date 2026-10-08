# Stoat channels are read as raw JSON to tell voice from text

When the Stoat adapter builds its snapshot, it fetches each channel with a raw `GET /channels/{id}` request and reads the payload itself (`StoatChannelResponse`), instead of using StoatSharp's typed `GetChannelAsync`.

Stoat no longer returns a separate voice channel type. A voice channel comes back as `"channel_type": "TextChannel"` with a `"voice"` object; a text channel has no `voice` key. StoatSharp 9.0.6 builds its typed channels from `channel_type` alone and its channel model has no `voice` field, so every voice channel comes back as a `TextChannel`. Verified against the test server:

```json
{ "channel_type": "TextChannel", "name": "it-scratch-voice", "description": "scratch", "voice": {} }
{ "channel_type": "TextChannel", "name": "it-scratch-text",  "description": "scratch" }
```

Without the raw read, a voice Mirror Channel shows up as a text Mirror, so Reconcile never finds its voice Mirror and creates a new one on every run.

The adapter treats `TextChannel` with `voice` as a voice channel, `TextChannel` without it as a text channel, and the legacy `VoiceChannel` type as a voice channel. Creating a voice channel still goes through StoatSharp's `CreateVoiceChannelAsync`, which works correctly.

## Considered Options

- **Keep the typed StoatSharp call.** Rejected: it cannot see the `voice` field, so it cannot tell the two apart.
- **Patch or fork StoatSharp.** Rejected for now: it's more to maintain than one small DTO. Revisit, and go back to the typed call, once StoatSharp exposes voice on text channels.

## Consequences

- `StoatChannelResponse` uses Newtonsoft's `[JsonProperty]` and `JObject`. Newtonsoft is not a direct package reference: it comes through StoatSharp, which uses it to deserialize `SendRequestAsync` responses. We accept this coupling to StoatSharp's serializer.
- The adapter depends on the shape of Stoat's channel payload, not on StoatSharp's model. `PublicVoiceChannelMirrorTests` catches it if that shape changes.
