# Facing Direction Enum Implementation Plan

**Goal:** Replace integer character-facing state with a typed direction enum while preserving numeric database and client-protocol compatibility.

**Architecture:** Add one public `Direction` enum in the `Goose` namespace and use it for the canonical runtime facing state and movement APIs. Keep raw packet and database values numeric at their boundaries, converting explicitly so enum names can never leak into SQL or wire packets. Update runtime-compiled scripts and regression tests with the same enum members.

**Tech Stack:** C# 14, .NET 10, xUnit, SQLite, Roslyn C# scripting

---

## APIs verified

- Canonical character-facing property: `Goose/ICharacter.cs:33`.
- Implementations and template state: `Goose/Player.cs:147`, `Goose/NPC.cs:165`, `Goose/NPCTemplate.cs:54`; `Pet` inherits `Player.Facing`.
- Player load and persistence boundaries: `Goose/Player.cs:748`, `Goose/Player.cs:1031`, `Goose/Player.cs:1120`.
- NPC template database load: `Goose/NPCHandler.cs:67-90`.
- Pet persistence boundaries: `Goose/Pet.cs:315-400`.
- Client packet serializers: `Goose/Packets.cs:80-116`, `Goose/Packets.cs:171-219`, `Goose/Packets.cs:340-343`.
- Raw facing input and client-specific conversion: `Goose/Events/FacingEvent.cs:18-50`, `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx:243`.
- Path-selection APIs: `Goose/NPC.cs:770`, `Goose/NPC.cs:860`, `Goose/Pet.cs:566`, `Goose/Pet.cs:717`.

### Task 1: Add the canonical enum and regression tests

**Files:**
- Create: `Goose/Direction.cs`
- Create: `Goose.Tests/DirectionTests.cs`
- Modify: `Goose.Tests/InvisibilityPacketTests.cs`
- Modify: `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs`

**Mutation impact:**
- Source of truth changed: `ICharacter.Facing` and its concrete implementations.
- Important readers: packet serialization, SQL generation, combat targeting, item pickup, movement, and scripts.
- Derived/cached state affected: No cached or derived state found; packets and SQL are generated directly from current state.
- Required propagation sequence: assign enum state, use enum members in runtime logic, cast to `int` only while loading/saving or serializing external numeric representations.
- Invariants to preserve: values remain Up=1, Right=2, Down=3, Left=4; packets and SQL contain numbers rather than enum names; existing stored values require no schema migration.
- Observable proof required: tests assert exact numeric enum values and numeric packet/SQL output for a named enum value.

**Steps:**
1. Add failing tests pinning enum values and proving a named direction serializes numerically in `MKC`, `CHH`, player INSERT, and player UPDATE output.
2. Run the focused tests and confirm compile/test failure before the enum exists.
3. Add the public enum with explicit values.
4. Run the focused tests after Tasks 2 and 3 complete.

| Invariant | Proved by |
|-----------|-----------|
| Numeric meanings remain stable | `DirectionTests.Values_match_server_protocol` |
| Enum names never enter packets | packet assertions using `Direction.Right` |
| Enum names never enter player SQL | persistence assertions using `Direction.Right` |

### Task 2: Migrate runtime state and movement logic

**Files:**
- Modify: `Goose/ICharacter.cs`
- Modify: `Goose/Player.cs`
- Modify: `Goose/NPC.cs`
- Modify: `Goose/NPCTemplate.cs`
- Modify: `Goose/NPCHandler.cs`
- Modify: `Goose/Pet.cs`
- Modify: `Goose/Events/MoveEvent.cs`
- Modify: `Goose/Events/PetMoveEvent.cs`
- Modify: `Goose/Events/FacingEvent.cs`
- Modify: `Goose/Events/PickupItemEvent.cs`
- Modify: `Goose/Events/PlayerAttackEvent.cs`
- Modify: `Goose/SpellEffect.cs`

**Mutation impact:**
- Source of truth changed: runtime `Facing` properties and local direction values used to assign them.
- Important readers: coordinate switches, heading broadcasts, NPC/pet movement, spell targeting.
- Derived/cached state affected: No derived state found. `P.ChangeHeading` reports the assigned state, while movement packet publication remains in the existing `MoveTo`/`FaceTo` sequence.
- Required propagation sequence: convert validated raw input to `Direction`; assign it; preserve existing `P.ChangeHeading` and movement broadcasts after assignment.
- Invariants to preserve: invalid raw facing packets remain ignored; movement deltas and random selection are unchanged; broadcasts occur in the same order.
- Observable proof required: compilation plus existing movement/combat tests and focused facing conversion tests.

**Steps:**
1. Change `Facing` declarations to `Direction` and cast database integers at load boundaries.
2. Change `NextStepTo`, `FaceTo`, direction locals, random direction generation, and spatial switches to enum types/members.
3. Change `FacingConverter` to `Func<int, Direction>` so raw client values remain protocol integers and canonical output is typed.
4. Build `Goose/Goose.csproj` to catch all static API mismatches.

| Invariant | Proved by |
|-----------|-----------|
| Invalid network values remain rejected | existing range check plus focused event test if fixture permits |
| Spatial meanings remain unchanged | enum value test and existing movement/combat suite |
| All character implementations expose the enum | compile-time interface enforcement |

### Task 3: Preserve protocol/persistence output and migrate scripts

**Files:**
- Modify: `Goose/Packets.cs`
- Modify: `Goose/Commands/CustomCommand.cs`
- Modify: `Goose/Commands/HairdyeCommand.cs`
- Modify: `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx`
- Modify: `Goose/Data/Illutia/Scripts/Spell/Backstab.csx`
- Modify: `Goose/Data/Illutia/Scripts/NPC/ZombieNPC.csx`
- Modify: `Goose/Data/Illutia/Scripts/Item/OkonkIllusionSword.csx`
- Modify: `Goose/Data/Illutia/Scripts/Item/ZombieLegIllusion.csx`
- Modify: `Goose.IntegrationTests/BackstabScriptTests.cs`

**Mutation impact:**
- Source of truth changed: scripts consume the typed `ICharacter.Facing` API.
- Important readers: Roslyn-compiled shipped scripts and custom packet builders.
- Derived/cached state affected: No derived state found.
- Required propagation sequence: replace numeric cases with enum members; explicitly cast enum values wherever a packet or SQL string is assembled.
- Invariants to preserve: wire payloads stay byte-compatible; Aspereta remapping remains 1→Up, 2→Down, 3→Left, 4→Right; Backstab behavior remains identical.
- Observable proof required: packet/persistence tests, Backstab integration tests, and startup/full-suite script compilation.

**Steps:**
1. Add explicit integer casts at every packet and SQL string boundary.
2. Replace script numeric direction switches with `Direction` members.
3. Update Backstab integration test inputs/helpers to use `Direction`.
4. Search production C# and C# script files for remaining numeric facing comparisons or integer `Facing` declarations.

| Invariant | Proved by |
|-----------|-----------|
| Wire output remains numeric | focused packet tests |
| Aspereta remapping is unchanged | converter code and full script startup coverage |
| Backstab direction and bonus are unchanged | `BackstabScriptTests` |

### Task 4: Verification and review

**Files:**
- Review all changed files.

**Steps:**
1. Run `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore`.
2. Run `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore`.
3. Run `dotnet test Goose.sln --no-restore`.
4. Search all tracked `.cs` and `.csx` files for remaining facing integers and unsafe enum concatenation.
5. Review `git diff --check`, the complete diff, and worktree status.

**Persistence strategy:** No schema migration. Existing `SMALLINT` columns and values 1-4 remain unchanged; only the in-memory C# type changes.

**Commit boundary:** One coherent commit after all runtime, script, boundary, and regression changes pass together, because intermediate enum API changes intentionally do not compile until consumers are migrated.
