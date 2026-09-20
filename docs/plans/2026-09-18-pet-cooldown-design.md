# Pet Death and Recall Cooldown Design

## Goal

Prevent an individual pet from being spawned for 120 seconds after it dies or its owner manually recalls it. Automatic removal during logout, map changes, owner idling, deletion, and internal repositioning must not start a cooldown.

## Existing Behavior

`Pet` already has `RespawnTime` and `NextRespawnTime` properties, and the `pets` table already has a `next_respawn_time` column. `/petspawn` checks `NextRespawnTime`, but no code sets it. The save paths also omit it, and the remaining-time calculation subtracts in the wrong direction. As a result, pets can currently be respawned immediately.

`RespawnTime` is copied from the source NPC when a pet is created. The new rule does not use that per-NPC value; all affected pets receive the fixed 120-second cooldown.

## Cooldown Flow

Keep `Pet.Destroy(GameWorld)` as the cooldown-free removal operation used by automatic cleanup.

Add a dedicated pet method for cooldown-triggering removal. It will:

1. Set `NextRespawnTime` to the current UTC Unix timestamp in seconds plus 120 seconds.
2. Persist the pet immediately.
3. Call the existing `Destroy` method.

Use the dedicated method only when a pet's HP reaches zero and when its owner successfully casts the manual recall spell. Each pet owns its timestamp, so one pet's cooldown does not prevent the owner from spawning another pet.

`/petspawn` will compare `NextRespawnTime` with the current UTC Unix timestamp. A future timestamp blocks spawning and reports the positive number of whole seconds remaining, rounded up. An expired or zero timestamp permits spawning normally. Failed spawn attempts do not extend the cooldown.

## Persistence

Treat `pets.next_respawn_time` as a UTC Unix timestamp in seconds. Add the property to both the pet insert and update statements. No schema migration is needed because the existing `BIGINT` column is sufficient, and existing zero values continue to mean no cooldown.

An immediate save when the cooldown begins prevents reconnecting from bypassing it under normal database operation. Logout and server shutdown do not start or extend cooldowns. Expired timestamps may remain in the database because spawn validation treats them as available.

The server currently never writes `next_respawn_time`, so existing nonzero values are assumed not to use the old `Stopwatch`-tick interpretation.

## Unchanged Behavior

The following operations continue using ordinary `Destroy` and do not start a cooldown:

- Owner logout
- Owner map transition
- Owner idle cleanup
- Pet deletion

## Spawn Refusal for a Pet Already on a Map

A pet that is already on a map cannot be spawned again. `/petspawn` replies "That pet
is already spawned." and returns before the cooldown check, and `Pet.Spawn` itself
returns without touching a live pet. This replaces the earlier repositioning behavior,
where spawning an alive pet tore it down and rebuilt it at its owner.

## Testing

Add focused coverage for:

- Pet death starts a cooldown and removes the pet.
- Manual recall starts a cooldown and removes the pet.
- Spawning before expiry is rejected with a positive remaining time.
- Spawning at or after expiry succeeds.
- Ordinary `Destroy` does not start a cooldown.
- Cooldown state is written and survives reloading the pet.

## Alternatives Considered

Setting the timestamp independently at the death and recall call sites would produce a smaller diff but duplicate policy. Passing a removal-reason enum through every `Destroy` call would make every path explicit but is excessive for a two-case rule. A dedicated cooldown removal method centralizes the policy while preserving the established cleanup behavior.

## Accepted Trade-offs

Persisted cooldowns depend on the server's UTC wall clock, so manually changing that clock can shorten or lengthen an active cooldown. The duration is a code constant rather than a configurable server setting. Both trade-offs are acceptable for this fixed two-minute rule.
