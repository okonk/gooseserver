# Party member buffs design

## Goal

Show every visible party member's non-item effects below their HP/MP bars, including icon, name, and remaining duration. This work spans `illutiagooseserver` and `Goose2ClientGodot`.

Effects are visible only while the party member is within normal character visibility range. Leaving range clears them, and re-entering range sends a fresh snapshot.

## Protocol

Use three server-to-client packet types:

```text
PBA<loginId>,<effectId>,<graphicId>,<graphicFile>,<remainingMs>,<totalMs>,<name>
PBR<loginId>,<effectId>
PBC<loginId>
```

`PBA` upserts one effect, `PBR` removes one effect, and `PBC` clears every cached effect for one party member. `(loginId, effectId)` identifies an effect. The name is last so the client can parse the complete remaining payload.

Remaining and total duration use milliseconds and the same clamped timer calculation as the regular buff bar. Permanent effects use zero for both values.

When a party member enters visibility, the server sends `PBC` followed by one `PBA` for each current non-item effect. Later changes send only deltas. Old clients ignore the new prefixes; new clients connected to an old server show no party effects.

## Server behavior

A group helper sends a target's party-buff snapshot to one viewer. Snapshot publication follows the existing character-publication boundaries:

- group creation or addition,
- map load,
- same-map warp,
- movement into visibility range.

Packets are sent only where the target character would normally be published, including existing GM-invisibility rules.

Successful `Player.AddBuff` calls publish one `PBA`. Renewals with the same effect ID publish one updated `PBA`. If a stacking upgrade replaces an effect with a different ID, the server sends `PBR` for the old ID followed by `PBA` for the replacement. An actual `RemoveBuff`, including expiration, publishes one `PBR`.

Recipients are ready members of the target's group within normal visibility range, excluding the target. Item buffs are never included. The regular buff bar's `refreshbar` flag does not suppress party deltas. No per-viewer server cache is introduced.

## Client state

`PartyWindow` owns party-effect state keyed by member login ID and effect ID.

- `PBA` creates or updates an effect and resets its local expiry deadline.
- `PBR` removes one matching effect.
- `PBC` removes all effects for the member.
- Replacing or emptying a group slot clears the old member.
- `EraseCharacterPacket` clears that member immediately.
- Packets for IDs that are not current, visible party members are ignored.

At zero remaining time, the icon stays fully swept and red until the authoritative removal arrives. Party effects are informational and cannot be removed through the UI.

A dedicated compact party-effect component reuses existing icon loading, tooltip formatting, and sweep calculations where practical, without the regular buff slot's countdown or double-click behavior.

## UI

Each populated party entry gets one horizontal effect row below its MP bar. The client expands from eight to ten party slots to match the server's configured party-window maximum, so every server-supported member can be represented.

- Icons are 16x16 px at base scale and follow the existing UI scale.
- The row begins at the party frame's left edge.
- Icons have a 1 px gap and extend rightward without wrapping, clipping, scrolling, or widening the 87 px frame.
- Party entries grow from 33 px to approximately 50 px so adjacent entries do not overlap.
- Effects retain insertion order; removal closes the gap.
- Hovering shows `Name (1m 23s remaining)` and updates while hovered.
- Permanent effects show only their name.
- The existing dark bottom-up cooldown sweep turns red during the final 10 seconds.
- Icons have no countdown text or blink.

The unlimited row can overlap unrelated UI to the right of the party window. This is accepted.

## Testing

Server tests cover packet formatting, duration calculations, permanent effects, item-buff filtering, snapshots, add/renew/replacement/remove deltas, group removal, range filtering, visibility re-entry, and character erase/republication paths such as name/title changes.

Client tests cover all packet parsers, state upsert/renew/remove/clear behavior, slot replacement, character erasure, unknown or non-visible IDs, sizing, layout metrics, tooltip formatting, sweep progress, and danger color state.

A Godot runtime self-test verifies ten-slot construction, packet-to-row lifecycle, scene paths, scaling, placement, and unclipped overflow. Manual verification covers icon rendering and live hover behavior.
