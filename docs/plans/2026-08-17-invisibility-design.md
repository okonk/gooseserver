# Invisibility Feature Design

Date: 2026-08-17
Status: Approved

## Overview

Add player/NPC invisibility to the server, distinct from and independent of GM
invisibility (`IsGMInvisible`).

- A character is invisible while it has one or more `Invisible` buff effects active.
- A character can see invisible characters if it has one or more `SeeInvisible` buff
  effects active, or (for players) if it has GM access, or (for NPCs) if its template
  has the new `see_invisible` flag.
- Invisible characters are hidden client-side via the existing "Invis thing" field in
  MKC/CHP (currently hardcoded `0`), and via a new `SINVS` packet that tells a client
  whether it can currently see invisible characters.
- Melee attacks and successful spell casts break the actor's invisibility.
- NPCs that can't see invisible players don't aggro on them.

## Wire protocol

- **MKC / CHP "Invis thing" field**: `0` visible, `1` invisible. Currently hardcoded `0`
  in six places in `Goose/Packets.cs`; becomes the character's actual state.
- **SINVS**: new packet, bare `SINVS1` (can see invisible) / `SINVS0` (can't).

## Invis state & buff lifecycle

Both `Player` and `NPC` get (deliberately duplicated per-class, no shared helper):

- `InvisibleBuffCount` and `SeeInvisibleBuffCount` (int).
- `IsInvisible => InvisibleBuffCount > 0`.

`AddBuff` / `RemoveBuff` on each class bump/decrement the counters when
`buff.SpellEffect?.EffectType` is `Invisible` or `SeeInvisible`. All other buff types are
untouched. `Pet : Player`, so pets inherit the Player path.

### Invisible 0→1 transition (in `AddBuff`, after increment)

- If the target is a `Player`: clear aggro on in-range NPCs that **can't see** the
  player (NPCs with `CanSeeInvisible` keep aggro). This moves the existing aggro-clear
  out of `SpellEffect.CastBuffSpell` (SpellEffect.cs:772-780) so scripts granting the
  buff outside a spell cast get identical behavior.
- Broadcast CHP to all players in range (`P.UpdateCharacter` for players,
  `P.UpdateNPC` for NPCs/pets). Everyone in range already holds an MKC, so the client
  toggles from the field.

### Invisible 1→0 transition (in `RemoveBuff`)

- Broadcast CHP to in-range players.

### SeeInvisible transitions (Player only sends packets)

- 0→1: send `SINVS1` to the player.
- 1→0: send `SINVS0` only if the player is not a GM (GMs always see).
- On every map load (the path where initial MKC/world packets go out, i.e.
  `DoneLoadingMapEvent`'s flow), send `SINVS` based on
  `SeeInvisibleBuffCount > 0 || Access > Normal`. Covers GM login, map changes while
  buffed, and (if buffs ever persist) buffed relogs.

## Breaking invisibility on attack/cast

`Player` and `NPC` each get `BreakInvisibility(world)`: removes **all** of its own buffs
whose `SpellEffect.EffectType == EffectTypes.Invisible` via the normal `RemoveBuff`
(snapshot the buff list first — mutation during iteration). Counters, CHP broadcast, and
see-invis bookkeeping flow through the existing remove path.

Call sites (three, nothing else):

1. `Player.Attack` (Player.cs:1640) — caller breaks its own invis. Covers players and pets.
2. `NPC.Attack` (NPC.cs:1370) — caller breaks its own invis.
3. `SpellEffect.Cast` — after a cast succeeds, the caster breaks its own invis. Any
   spell type reveals the caster (buffs included).

No `NPC.Attacked` hook. Ranged *weapon* attacks do **not** break invis (conscious
exclusion; ranged magic does, via the cast path).

## NPC see-invisible flag & aggro

- New `see_invisible` column (bool, default false) on the NPC template schema, defined
  in `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs` (pattern: `Col.Bool("stunnable", def: false)`).
  The owner updates the spreadsheet to add the column; `Goose/sql/npcs.sql` is stale and
  is NOT updated in this work — it will be regenerated from the CSV pipeline later.
- `NPCHandler` loads the column into `NPCTemplate.SeeInvisible`; `LoadFromTemplate`
  copies it onto the `NPC` instance as a base flag.
- `NPC.CanSeeInvisible` = base template flag **or** `SeeInvisibleBuffCount > 0`. This
  leaves room for future "blind" debuffs on NPCs via the same count.
- Gating in one place: `AggroIfInRange` (NPC.cs:988) returns early when
  `player.IsInvisible && !this.CanSeeInvisible`. Both movement-loop call sites
  (NPC.cs:543, 704) funnel through it; existing `!player.IsGMInvisible` checks stay.
- An invisible player hitting an NPC still aggros it: the attack breaks the player's
  invis first, so `Attacked` needs no special case.
- Allied splash aggro is intentionally NOT gated: when an NPC that can see an invisible
  player aggros, allies that can't see invisible still aggro via the splash loops
  (they effectively know about the threat).

## Packet changes

- `Packets.cs`: the six `"0" + "," + // Invis thing` sites (`MakePlayerCharacter`,
  `UpdateCharacter`, `UpdatePet`, `UpdateNPC`, `MakeNPCCharacter`, `MakePetCharacter`)
  become `"1"` when the character's `IsInvisible`, else `"0"`.
- New `P.SeeInvisible(bool)` helper → `"SINVS1"` / `"SINVS0"`.
- `HairdyeCommandEvent`, `CustomCommandEvent`, `GMHaxCommandEvent` hand-built packets
  keep their hardcoded `0` (out of scope, per design decision).

## Testing (Goose.Tests, fixture-based, no DB)

1. Counters: add/remove `Invisible` and `SeeInvisible` buffs on Player and NPC update
   the counts; other buff types don't touch them; multiple stacks.
2. SINVS: 0→1 sends `SINVS1`; 1→0 sends `SINVS0` for normal players, nothing for GMs;
   map load sends correct status for a GM and a buffed player.
3. Invis transition: 0→1 broadcasts CHP to in-range players and clears aggro on
   in-range NPCs that can't see (kept for NPCs that can); 1→0 broadcasts CHP.
4. Breaking invis: `Player.Attack` and `NPC.Attack` remove all Invisible buffs; a
   successful `SpellEffect.Cast` removes the caster's.
5. Aggro gating: `AggroIfInRange` skips invisible players when the NPC can't see;
   aggros normally when it can (template flag and buff, independently).
6. Packets: MKC/CHP carry `1`/`0` in the invis field; `P.SeeInvisible` format.
7. CsvToSql: `NpcCsvToSql` gains `see_invisible`; snapshot fixtures regenerated to
   include the column header (default false keeps other output unchanged).

## Settled decisions / deferred gaps

- Invis state is buff-derived (count > 0), not a standalone flag.
- Invisibility applies to players, pets (via `Pet : Player`), and NPCs.
- NPCs break their own invis on attack/cast, same as players.
- GMs always see invisible (SINVS1 semantics include GM access).
- Toggles use CHP broadcast, not ERC/MKC hide-show.
- Aggro-clear boundary: only NPCs within range at the 0→1 transition are considered;
  out-of-range NPCs can't newly aggro anyway (section gating).
- NPC/pet invisibility has no aggro side effects (players don't track NPC aggro
  server-side) — only the CHP broadcast applies.
- Force-removing buffs with `updateCharacter: false` (logout) still triggers the CHP
  broadcast on an invis transition; harmless because the character is erased.
- G6 (deferred): GM access changed mid-session without a map change doesn't re-send
  SINVS until the next map load.
- G10 (out of scope): invisible players still appear in `/who`.
