# Revive System — Part 2: Client Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Goose2ClientGodot draws dead players lying on the ground with a countdown over their name, stops the local corpse from acting, and shows a respawn dialog with a "Respawn now" button.

**Architecture:** MKC/CHP parsers read a trailing `RespawnSeconds` (0 = alive). `Character` keeps `IsDead` plus a local deadline; `CharacterMotion.State` returns `"death"` for corpses and `PlayCurrent` holds each slot on the last `death-<dir>` frame (slots without art are hidden by the existing blank rule). Local input is gated in `Character.ProcessLocalInput`, `SpellTargetManager`, `GameManager.CanCastSpellsHere` and a `NetworkClient` backstop. `RespawnWindow` follows `ConfirmWindow` and reacts to `GameManager.CharacterUpdated`.

**Tech Stack:** Godot 4.6 C# (GodotSharp), xUnit (`tests/Goose2Client.Tests`, compiles `Scripts/**/*.cs`; pure logic only — no engine calls in tests).

Work in the client worktree: `/home/hayden/code/Goose2ClientGodot/.worktrees/revive` (branch `revive`).
Depends on Part 1 (`docs/plans/2026-11-15-revive-part1-server.md`): player MKC/CHP end with
`,<respawnSeconds>`; dead players are sent with their base body; hand-built preview MKCs now
include hide_name (Part 1 Task 4b); client sends `RSPN`.

Follow `AGENTS.md`: no new comments or doc strings unless the "why" is non-obvious (wire-format
details are allowed).

Test command (from the client worktree):

```bash
dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~<Name>
dotnet test tests/Goose2Client.Tests
```

Baseline in this worktree: 1813 pass, 1 fail — `StylistCatalogTests.EveryCatalogId_HasAnimationAsset`
fails only because `Assets/*` is gitignored and the worktree lacks generated sprites (passes in
the main checkout). Treat it as pre-existing; nothing else may fail.

---

## APIs verified (client repo paths)

| API | Where |
|---|---|
| `MakeCharacterPacket.Parse` (layered branch, `EndsWith('*')` hide_name fallback, non-layered `TokensRemaining() > 2` fallback, `ParseItem` zero-mount early return) | `Scripts/Network/Packets/MakeCharacterPacket.cs:39-110`, `124-146` |
| `UpdateCharacterPacket.Parse`, its own `ParseItem` (same zero-mount early return, line 96) | `Scripts/Network/Packets/UpdateCharacterPacket.cs` |
| `PacketParser.GetInt32/GetBool/GetString/Peek/TokensRemaining` (`TokensRemaining` is 0 at end; a trailing `,` does not count as a token) | `Scripts/Network/PacketParser.cs:63-120` |
| Existing MKC parser tests (hide_name + "without hide_name" fallback cases) | `tests/Goose2Client.Tests/CharacterPacketHideNameTests.cs`, `CharacterPacketInvisibleTests.cs`, `CharacterPacketAppearanceTests.cs` |
| `Character.SetAppearance(MakeCharacterPacket)` / `SetAppearance(UpdateCharacterPacket)` | `Scripts/Character/Character.cs:174-200`, `234-248` |
| `Character._nameLabel`, `EnsureNameLabel`, `FullName` | `Scripts/Character/Character.cs:13`, `56`, `103-120` |
| `Character._Process`, `ProcessLocalInput`, `TryChainLocalStep`, `BeginLock`, `TeleportTo`, `PlayCurrent`, `ResolveClip` | `Scripts/Character/Character.cs:~547`, `640`, `706`, `474`, `536`, `780-812`, `818-828` |
| `CharacterMotion.State(bool isMoving, string? lockedMotion, bool isMounted)` | `Scripts/Character/CharacterMotion.cs:7-12`; tests `tests/Goose2Client.Tests/CharacterMotionTests.cs` |
| `AnimationNames.Candidates` default arm `_ => new List<string> { motion }` (so `"death"` → `death-<dir>`) | `Scripts/Character/AnimationNames.cs:36-60`; tests `tests/Goose2Client.Tests/AnimationNamesTests.cs` |
| Death clips exist for player bodies and armour; heights listed | `Assets/Sprites/Bodies/{1,11,21,221}/animations.tres` (`death-up/down/left/right`), `Assets/Resources/AnimationHeights.txt` |
| `MapManager.OnMakeCharacter` / `OnUpdateCharacter` call `GameManager.Instance.OnCharacterUpdated(c)` for the local player | `Scripts/MapManager.cs:182-194`, `232-240` |
| `GameManager.CharacterUpdated` event, `CanCastSpellsHere()`, `CurrentMapManager`, `Hud` | `Scripts/GameManager.cs:67-73`, `98-99` |
| `SpellTargetManager.Cast(SpellInfo)`, `RepeatHoldCast`, `RecastLastSpellOn` (calls `CanCastSpellsHere`), `CastOnTarget` | `Scripts/SpellTargetManager.cs:93`, `131`, `147-171`, `~350` |
| `NetworkClient.Send/Move/Face/Attack/UseItem/ToggleMount/Drop/Pickup/CastSpell/Emote`; `StylistChange` one-liner style | `Scripts/Network/NetworkClient.cs:90`, `192-266`, `332`, `366` |
| `ConfirmWindow` (BaseWindow subclass: `DefaultVisible`, `_Ready` node lookups, `UiScaleApplier.Instance!.ApplyFontSize(..., 9)`, `ScaleRegister()`, `OnClosePressed`) and its scene | `Scripts/UI/ConfirmWindow.cs`, `Scenes/UI/ConfirmWindow.tscn` |
| `BaseWindow._Process` (hover fade — call `base._Process`), `Title`, `Activate()`, `OnClosePressed()` | `Scripts/UI/BaseWindow.cs:57`, `205`, `483`, `551` |
| HUD registration `Add<T>(path)`, property list | `Scripts/UI/GameHud.cs:34`, `41-46`, `110` |
| `DefaultWindowLayout.IsDialog(name)` list; theory test | `Scripts/UI/DefaultWindowLayout.cs:34-35`; `tests/Goose2Client.Tests/DefaultWindowLayoutTests.cs:36` |
| Godot time: `Time.GetTicksMsec()` (already used in `Character.SetVitals`) | `Scripts/Character/Character.cs:89` |

### Design deltas (decided during planning)

- **Left/right clicks are not gated client-side.** The client cannot tell an NPC tile from a player
  tile without duplicating the server's lookup; the server refuses NPC clicks on a corpse with the
  "You can't do that while dead." notice (Part 1 Task 5), and clicking players keeps working.
- The client's hide_name fallback is removed only because Part 1 Task 4b makes the last
  hide_name-less MKCs (preview MKCs) send it.

---

### Task 1: Parse trailing `RespawnSeconds`; drop the hide_name fallback

**Files:**
- Modify: `Scripts/Network/Packets/MakeCharacterPacket.cs`
- Modify: `Scripts/Network/Packets/UpdateCharacterPacket.cs`
- Modify: `tests/Goose2Client.Tests/CharacterPacketHideNameTests.cs` (delete the four fallback tests)
- Test: `tests/Goose2Client.Tests/CharacterPacketRespawnTests.cs` (create)

**Step 1: Write the failing tests**

Sample strings follow the server builders in Part 1 Task 4 (layered player tail
`invis,hide,face,speed,gm,<mount>,respawn`; CHP `invis,face,speed,<mount>,respawn`;
non-layered player MKC `invis,hide,speed,gm,respawn`, CHP `invis,speed,respawn`; layered NPC/pet mount `0,0,0,0,0` and no respawn token).

```csharp
using Goose2Client.Network;
using Goose2Client.Network.Packets;
using Xunit;

namespace Goose2Client.Network.Packets.Tests;

public class CharacterPacketRespawnTests
{
    private const string LayeredHead = "MKC42,1,Asp,T1,S1,G1,10,20,2,75,1,10,20,30,40,4,10070,"
        + "0,*,0,*,0,*,0,*,0,*,0,*,111,222,33,44,0,0,70,320,0,";

    private static MakeCharacterPacket Mkc(string raw)
        => (MakeCharacterPacket)new MakeCharacterPacket().Parse(new PacketParser(raw, "MKC"));

    private static UpdateCharacterPacket Chp(string raw)
        => (UpdateCharacterPacket)new UpdateCharacterPacket().Parse(new PacketParser(raw, "CHP"));

    [Theory]
    [InlineData("0,*,0", 0)]
    [InlineData("0,*,47", 47)]
    [InlineData("273,*,12", 12)]
    [InlineData("273,1,2,3,4,9", 9)]
    public void Layered_player_mkc_reads_respawn_after_mount(string tail, int expected)
    {
        var p = Mkc(LayeredHead + tail);
        Assert.Equal(expected, p.RespawnSeconds);
        Assert.Equal(70, p.FaceId);
        Assert.Equal(320, p.MoveSpeed);
    }

    [Fact]
    public void Layered_player_mkc_without_respawn_token_is_alive()
        => Assert.Equal(0, Mkc(LayeredHead + "0,*").RespawnSeconds);

    [Fact]
    public void Non_layered_player_mkc_reads_respawn()
    {
        var p = Mkc("MKC7,1,Morph,,,,1,1,1,0,150,0,0,0,0,3,0,0,320,0,33");
        Assert.Equal(33, p.RespawnSeconds);
        Assert.Equal(320, p.MoveSpeed);
        Assert.False(p.IsGM);
    }

    [Fact]
    public void Layered_npc_mkc_is_alive()
    {
        var p = Mkc("MKC20001,2,Guard,,,,1,1,1,100,1,0,0,0,0,3,10070,"
            + "0,*,0,*,0,*,0,*,0,*,0,*,0,0,0,0,0,1,70,320,0,0,0,0,0,0");
        Assert.Equal(0, p.RespawnSeconds);
        Assert.True(p.HideName);
    }

    [Theory]
    [InlineData("0,*,0", 0)]
    [InlineData("0,*,58", 58)]
    [InlineData("273,1,2,3,4,9", 9)]
    public void Layered_chp_reads_respawn_after_mount(string tail, int expected)
    {
        var p = Chp("CHP42,1,10,20,30,40,4,10070,0,*,0,*,0,*,0,*,0,*,0,*,111,222,33,44,0,70,320," + tail);
        Assert.Equal(expected, p.RespawnSeconds);
        Assert.Equal(320, p.MoveSpeed);
    }

    [Fact]
    public void Non_layered_chp_reads_respawn()
    {
        var p = Chp("CHP8,150,255,0,0,255,0,0,888,21");
        Assert.Equal(888, p.MoveSpeed);
        Assert.Equal(21, p.RespawnSeconds);
    }
}
```

Delete from `CharacterPacketHideNameTests.cs`: `MonsterMkc_WithoutHideNameToken_DefaultsHideNameFalse`,
`LayeredMkc_WithoutHideNameToken_DefaultsHideNameFalse`,
`LayeredMkc_WithoutHideNameTokenAndColorlessMount_DefaultsHideNameFalse`,
`MonsterMkc_WithoutTrailingIsGmToken_DefaultsIsGmFalse` (they pin the fallback being removed).

**Step 2:** Run `--filter FullyQualifiedName~CharacterPacket` → compile FAIL (`RespawnSeconds`).

**Step 3: Implement**

`MakeCharacterPacket`: add `public int RespawnSeconds { get; set; }`. Layered branch, replacing
the block from the `// Older servers omit hide_name` comment through `ParseItem(packet.DisplayedEquipment, 6, p);`:

```csharp
                packet.HideName = p.GetInt32() != 0;
                packet.FaceId = p.GetInt32();
                packet.MoveSpeed = p.GetInt32();
                packet.IsGM = p.GetBool();

                // mount
                ParseItem(packet.DisplayedEquipment, 6, p);
```

Non-layered branch, replacing the hide_name/move-speed/is_gm reads:

```csharp
                packet.HideName = p.GetInt32() != 0;
                packet.MoveSpeed = p.GetInt32();
                packet.IsGM = p.GetBool();
```

After the `if/else` (before `return packet;`):

```csharp
            if (p.TokensRemaining() > 0)
                packet.RespawnSeconds = p.GetInt32();
```

`ParseItem`'s zero-mount early return (both files) must also eat the player's `*` so the
respawn token is next:

```csharp
            if (id == 0 && i == 6)
            {
                if (p.TokensRemaining() > 0 && p.Peek() == '*') p.GetString();
                return;
            }
```

(NPC/pet layered mounts are `0,0,0,0,0`: the four leftover `0`s are not consumed, so the
trailing read sees `0` = alive, which is correct.)

`UpdateCharacterPacket`: add `public int RespawnSeconds { get; set; }` and the same trailing
`if (p.TokensRemaining() > 0) packet.RespawnSeconds = p.GetInt32();` before `return packet;`.

**Step 4:** Run `--filter FullyQualifiedName~CharacterPacket` → PASS. Full suite → only the
known `StylistCatalogTests` failure.

**Step 5: Commit**

```bash
git add Scripts/Network/Packets tests/Goose2Client.Tests
git commit -m "feat(revive): parse respawn seconds on MKC/CHP"
```

---

### Task 2: Death motion, countdown helper and dialog registration in layout

**Files:**
- Modify: `Scripts/Character/CharacterMotion.cs:7-12`
- Create: `Scripts/Character/RespawnCountdown.cs`
- Test: `tests/Goose2Client.Tests/CharacterMotionTests.cs`, `tests/Goose2Client.Tests/AnimationNamesTests.cs`, `tests/Goose2Client.Tests/RespawnCountdownTests.cs` (create)

**Step 1: Write the failing tests**

`CharacterMotionTests.cs` — add:

```csharp
    [Theory]
    [InlineData(false, null, false)]
    [InlineData(true, "attack", false)]
    [InlineData(true, null, true)]
    public void Dead_is_always_death(bool moving, string? locked, bool mounted)
        => Assert.Equal("death", CharacterMotion.State(moving, locked, mounted, isDead: true));
```

`AnimationNamesTests.cs` — add:

```csharp
    [Theory]
    [InlineData(Direction.Down, "death-down")]
    [InlineData(Direction.Left, "death-left")]
    public void Death_candidates_are_the_directional_death_clip(Direction d, string expected)
        => Assert.Equal(new[] { expected }, AnimationNames.Candidates("death", 4, d));
```

`RespawnCountdownTests.cs`:

```csharp
using Goose2Client.Character;
using Xunit;

public class RespawnCountdownTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(7, "0:07")]
    [InlineData(47, "0:47")]
    [InlineData(60, "1:00")]
    [InlineData(-3, "0:00")]
    public void Format_is_minutes_and_padded_seconds(int seconds, string expected)
        => Assert.Equal(expected, RespawnCountdown.Format(seconds));

    [Theory]
    [InlineData(10_000UL, 10_000UL, 0)]
    [InlineData(10_000UL, 9_001UL, 1)]
    [InlineData(10_000UL, 9_000UL, 1)]
    [InlineData(10_000UL, 8_999UL, 2)]
    [InlineData(10_000UL, 12_000UL, 0)]
    public void SecondsLeft_rounds_up_and_clamps(ulong deadline, ulong now, int expected)
        => Assert.Equal(expected, RespawnCountdown.SecondsLeft(deadline, now));

    [Fact]
    public void Deadline_adds_seconds()
        => Assert.Equal(65_000UL, RespawnCountdown.Deadline(5_000UL, 60));

    [Fact]
    public void NameLabel_appends_the_countdown()
        => Assert.Equal("Asp (0:47)", RespawnCountdown.NameLabel("Asp", 47));
}
```

**Step 2:** Run `--filter "FullyQualifiedName~CharacterMotionTests|FullyQualifiedName~AnimationNamesTests|FullyQualifiedName~RespawnCountdownTests"` → compile FAIL. (`AnimationNames` already passes once compiling — it pins existing behaviour.)

**Step 3: Implement**

`CharacterMotion.State`:

```csharp
        public static string State(bool isMoving, string? lockedMotion, bool isMounted, bool isDead = false)
        {
            if (isDead) return "death";
            if (lockedMotion != null) return lockedMotion;
            if (isMounted) return isMoving ? "mounted-walk" : "mounted-idle";
            return isMoving ? "walk" : "idle";
        }
```

`Scripts/Character/RespawnCountdown.cs`:

```csharp
namespace Goose2Client.Character
{
    public static class RespawnCountdown
    {
        public static ulong Deadline(ulong nowMsec, int seconds)
            => nowMsec + (ulong)System.Math.Max(0, seconds) * 1000UL;

        public static int SecondsLeft(ulong deadlineMsec, ulong nowMsec)
            => deadlineMsec <= nowMsec ? 0 : (int)((deadlineMsec - nowMsec + 999UL) / 1000UL);

        public static string Format(int seconds)
        {
            int s = System.Math.Max(0, seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        public static string NameLabel(string name, int seconds) => $"{name} ({Format(seconds)})";
    }
}
```

Godot generates a `.uid` sidecar for new `.cs` files when the editor opens the project; commit
it if one appears (other scripts have `*.cs.uid` checked in).

**Step 4:** Run the filter → PASS.

**Step 5: Commit**

```bash
git add Scripts/Character tests/Goose2Client.Tests
git commit -m "feat(revive): death motion state and respawn countdown helper"
```

---

### Task 3: Dead pose and name countdown on `Character`

**Files:**
- Modify: `Scripts/Character/Character.cs`

No unit test: `Character` is a Godot node (not constructible in the test project). The pure
pieces (`CharacterMotion.State`, `AnimationNames`, `RespawnCountdown`) are covered in Task 2;
this task is verified by the manual check in Task 6.

**Step 1: State**

After `public bool NameHiddenByServer { get; private set; }` (line ~23):

```csharp
        public bool IsDead { get; private set; }
        private ulong _respawnDeadlineMsec;
        private int _shownCountdown = -1;
        public int RespawnSecondsLeft => IsDead ? RespawnCountdown.SecondsLeft(_respawnDeadlineMsec, Time.GetTicksMsec()) : 0;
        public ulong RespawnDeadlineMsec => _respawnDeadlineMsec;
```

**Step 2: Apply from packets**

New methods (next to `ApplyInvisibility`):

```csharp
        private void ApplyDeadState(int respawnSeconds)
        {
            bool dead = respawnSeconds > 0;
            if (dead)
            {
                _respawnDeadlineMsec = RespawnCountdown.Deadline(Time.GetTicksMsec(), respawnSeconds);
                _lockedMotion = null;
                if (_moving) TeleportTo(X, Y);
            }
            IsDead = dead;
            RefreshNameText();
        }

        private void RefreshNameText()
        {
            if (_nameLabel == null) return;
            int secs = RespawnSecondsLeft;
            _nameLabel.Text = IsDead ? RespawnCountdown.NameLabel(FullName, secs) : FullName;
            _shownCountdown = secs;
            _nameLabel.Layout(this);
        }
```

`SetAppearance(MakeCharacterPacket p)`: call `ApplyDeadState(p.RespawnSeconds);` immediately
after `CharacterType = p.CharacterType;` (before `ApplyAppearance`, so the following
`PlayState()` draws the pose), and replace

```csharp
            if (EnsureNameLabel() && _nameLabel != null) { _nameLabel.Text = FullName; _nameLabel.Layout(this); }
```

with

```csharp
            if (EnsureNameLabel()) RefreshNameText();
```

`SetAppearance(UpdateCharacterPacket p)`: call `ApplyDeadState(p.RespawnSeconds);` right after
`IsInvisible = p.Invisible != 0;`.

**Step 3: Pose**

`PlayCurrent` (line ~786): pass the dead flag:

```csharp
                string motion = CharacterMotion.State(IsMoving, _lockedMotion, slotMounted, IsDead);
```

and in the frame-selection block, before the existing `if (s.Sprite.SpriteFrames.GetFrameCount(clip) <= 1)`:

```csharp
                if (IsDead)
                {
                    if (s.Sprite.IsPlaying()) s.Sprite.Stop();
                    s.Sprite.Animation = clip;
                    s.Sprite.Frame = s.Sprite.SpriteFrames.GetFrameCount(clip) - 1;
                }
                else if (s.Sprite.SpriteFrames.GetFrameCount(clip) <= 1)
```

(keep the rest of that `if/else if` chain as is). Slots without a `death-<dir>` clip already
resolve to `null` in `ResolveClip` and are hidden.

`BeginLock` (line ~474) — first line:

```csharp
            if (IsDead) return;
```

**Step 4: Countdown tick and input**

`_Process`, after `ApplyBarVisibility();`:

```csharp
            if (IsDead && RespawnSecondsLeft != _shownCountdown) RefreshNameText();
```

`ProcessLocalInput` and `TryChainLocalStep` — after their `if (!IsLocalPlayer) return …;` line:

```csharp
            if (IsDead) return;          // ProcessLocalInput
            if (IsDead) return false;    // TryChainLocalStep
```

**Step 5:** Build and run the suite: `dotnet build && dotnet test tests/Goose2Client.Tests` → only the known failure.

**Step 6: Commit**

```bash
git add Scripts/Character/Character.cs
git commit -m "feat(revive): corpses lie on the ground with a respawn countdown"
```

---

### Task 4: Gate local actions while dead

**Files:**
- Modify: `Scripts/GameManager.cs:67-73`
- Modify: `Scripts/SpellTargetManager.cs:93`, `131`, `~350` (`CastOnTarget`)
- Modify: `Scripts/Network/NetworkClient.cs:192-266`, `366`

**Step 1: Implement**

`GameManager`, above `CanCastSpellsHere`:

```csharp
        public bool IsLocalPlayerDead => CurrentMapManager?.LocalPlayer?.IsDead == true;
```

and first line of `CanCastSpellsHere()`:

```csharp
            if (IsLocalPlayerDead) return false;
```

`SpellTargetManager`:
- `Cast(SpellInfo info)` first line: `if (GameManager.Instance.IsLocalPlayerDead) return;`
- `RepeatHoldCast` first line: `if (GameManager.Instance.IsLocalPlayerDead) return false;`
- `CastOnTarget` condition: `if (_target != null && spell != null && !GameManager.Instance.IsLocalPlayerDead && !GameManager.Instance.IsLoadingMap)`
- `RecastLastSpellOn` is already covered by `CanCastSpellsHere` (line 160).

`NetworkClient` — add

```csharp
        private static bool LocalPlayerDead => GameManager.Instance?.IsLocalPlayerDead == true;
```

and make the first line of `Move`, `Face`, `Attack`, `UseItem`, `ToggleMount`, `Drop`, `Pickup`,
`CastSpell` and `Emote`:

```csharp
            if (LocalPlayerDead) return;
```

(`GameManager.Instance` is null in the test project, so the gate is inert there.) Chat,
commands, inventory moves, window sends, clicks and `RSPN` are not gated.

**Step 2:** Build + full suite → only the known failure.

**Step 3: Commit**

```bash
git add Scripts
git commit -m "feat(revive): local corpse cannot move, attack, cast or use items"
```

---

### Task 5: `RespawnWindow` and `RSPN`

**Files:**
- Modify: `Scripts/Network/NetworkClient.cs` (send helper)
- Create: `Scripts/UI/RespawnWindow.cs`, `Scenes/UI/RespawnWindow.tscn`
- Modify: `Scripts/UI/GameHud.cs:34`, `110`
- Modify: `Scripts/UI/DefaultWindowLayout.cs:34-35`
- Test: `tests/Goose2Client.Tests/DefaultWindowLayoutTests.cs` (add `[InlineData("Respawn", true)]` to the `IsDialog_OnlyTransientDialogFamily` theory)

**Step 1: Failing test** — add the `InlineData` above; run `--filter FullyQualifiedName~DefaultWindowLayoutTests` → FAIL.

**Step 2: Implement**

`DefaultWindowLayout.IsDialog`: append `or "Respawn"` to the pattern.

`NetworkClient`, next to `StylistChange`:

```csharp
        public void RequestRespawn() => Send("RSPN");
```

`Scripts/UI/RespawnWindow.cs`:

```csharp
using Godot;
using Goose2Client.Character;

namespace Goose2Client.UI;

public partial class RespawnWindow : BaseWindow
{
    protected override bool DefaultVisible => false;

    private Label _prompt = null!;
    private Button _respawnButton = null!;
    private Character.Character? _player;

    public override void _Ready()
    {
        base._Ready();

        Visible = false;
        Title = "Dead";

        _prompt = GetNode<Label>("Content/Prompt");
        _respawnButton = GetNode<Button>("Content/RespawnButton");
        _respawnButton.Pressed += OnRespawnPressed;
        GetNode<Button>("TitleBar/CloseButton").Visible = false;

        UiScaleApplier.Instance!.ApplyFontSize(GetNode<Label>("TitleBar/TitleLabel"), 9);
        ScaleRegister();

        GameManager.Instance.CharacterUpdated += OnCharacterUpdated;
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null) GameManager.Instance.CharacterUpdated -= OnCharacterUpdated;
    }

    private void OnCharacterUpdated(Character.Character c)
    {
        if (!c.IsLocalPlayer) return;

        if (!c.IsDead)
        {
            _player = null;
            Hide();
            return;
        }

        _player = c;
        _respawnButton.Disabled = false;
        Visible = true;
        Activate();
        UpdatePrompt();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Visible) UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
        int secs = _player.RespawnSecondsLeft;
        _prompt.Text = $"You have died. Respawning in {RespawnCountdown.Format(secs)}...";
        if (secs == 0) _respawnButton.Disabled = true;
    }

    private void OnRespawnPressed()
    {
        _respawnButton.Disabled = true;
        GameManager.Instance.NetworkClient.RequestRespawn();
    }

    protected override void OnClosePressed() { }
}
```

`BaseWindow` does not override `_ExitTree`; `EnchantWindow.cs:56` overrides it directly without a base call — same pattern here.

`Scenes/UI/RespawnWindow.tscn` — copy `Scenes/UI/ConfirmWindow.tscn` and change:
- `[ext_resource ... path="res://Scripts/UI/RespawnWindow.cs" id="1_rw"]`, root node
  `name="RespawnWindow"`, `script = ExtResource("1_rw")`, `WindowName = "Respawn"`.
- `offset_bottom = 104.0` on the root.
- `Prompt` label: `offset_bottom = 70.0`.
- Replace `OkButton` and `CancelButton` with one button:

```
[node name="RespawnButton" type="Button" parent="Content"]
layout_mode = 2
anchors_preset = 0
offset_left = 56.0
offset_top = 76.0
offset_right = 164.0
offset_bottom = 98.0
text = "Respawn now"
```

Remove the `uid=` attribute from the header if copying produces a duplicate uid (let Godot assign one).

`GameHud`: property `public RespawnWindow Respawn { get; private set; } = null!;` after `Confirm`,
and `Respawn = Add<RespawnWindow>("res://Scenes/UI/RespawnWindow.tscn");` after the `Confirm = …` line.

**Step 3:** Run the filter → PASS; `dotnet build`; full suite → only the known failure.

**Step 4: Commit**

```bash
git add Scripts Scenes tests/Goose2Client.Tests
git commit -m "feat(revive): respawn dialog with countdown and Respawn now"
```

---

### Task 6: Manual end-to-end check (against a Part 1 server)

Run the Part 1 server branch locally and two clients (`./run.sh`; see the client README for
pointing at a local server). Give character B a Revive spell (spell effect `effect_type =
Revive`, `spell_effected = 4`, `works_not_in_pvp = TRUE`). Verify and note results in the PR:

1. A dies to a mob: A lies on the ground on both screens (armour lying too, weapon/shield hidden),
   name shows `A (1:00)` counting down on both screens; A's respawn dialog counts down.
2. A cannot move, turn, attack, cast (hotbar, spellbook, hold-cast), use/drop/pick up items or
   mount; chat works; clicking an NPC shows "You can't do that while dead."
3. B casts Revive on A: A stands up in place at half HP/MP, gets "B revived you.", dialog closes,
   countdown disappears on both screens.
4. A dies again and presses "Respawn now": A warps to bind, dialog closes.
5. A dies and waits the full timer: same as 4.
6. A dies, B walks out of range and back: A is still lying with the correct remaining time.
7. A dies and a GM `/summon`s A: A stays dead on the new tile with the countdown continuing.
8. A dies and logs out: on login A is alive at the bind point.
9. A illusioned by an item dies: lies with the real body; on revive the illusion returns.
10. Arena: dying in the arena still teleports to the lobby instantly.

Fix any defect with a failing test first where the logic is testable, then commit.

---

## Done criteria

- `dotnet test tests/Goose2Client.Tests`: all pass except the known worktree-only
  `StylistCatalogTests` asset failure.
- Task 6 checklist completed against a Part 1 server.
