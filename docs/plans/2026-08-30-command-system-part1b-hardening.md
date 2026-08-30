# Command System — Part 1b Hardening Implementation Plan

**Goal:** Harden the Part 1 command framework by restoring mixed-command default dispatch, making delegate validation match invocation, routing deprecated slash factories through the command registry, and completing alias-aware, width-safe help formatting.

**Architecture:** Keep the worktree implementation and its immutable `CommandSnapshot` as the source of truth. Make four narrow changes around target selection, delegate parameter metadata, legacy factory publication, and help lookup/wrapping; preserve all current registration boundary checks, finite numeric parsing, permission filtering, and case-insensitive section behavior.

**Tech Stack:** C# / .NET 10, xUnit, existing `Trie<T>`, `CommandRegistry`, `TestWorldFixture`, and shipped Illutia script integration tests.

---

This is the hardening follow-up to `docs/plans/2026-08-29-command-system-part1-framework.md`, before Parts 2 and 3. Execute with `@test-driven-development`; use `@verification-before-completion` before claiming the branch is ready.

## APIs verified

| API / invariant | Current location |
|---|---|
| `CommandEvent.Ready` constructs one `CommandContext`, runs `CheckAccessInternal`, selects a target, binds, and invokes it | `Goose/Commands/CommandEvent.cs:21` |
| The defect: any definition with subcommands takes the subcommand branch before `ExecuteMethod` is considered | `Goose/Commands/CommandEvent.cs:54` |
| Delegate invocation already binds against the delegate type's `Invoke` parameters | `Goose/Commands/CommandEvent.cs:81` |
| Delegate usage text currently takes parameter names from `handler.Method` | `Goose/Commands/CommandEvent.cs:83-89`; `Goose/Commands/HelpFormatter.cs:145-165` |
| Delegate registration incorrectly validates `handler.Method.GetParameters()` | `Goose/Commands/CommandRegistry.cs:143-160` |
| `CommandBinder.IsValidTarget(ParameterInfo[], out string?)` enforces `CommandContext` first, supported types, and final-only `string[]` | `Goose/Commands/CommandBinder.cs:99-133` |
| `CommandBinder.Bind` accepts the same parameter surface and returns only non-context invocation arguments | `Goose/Commands/CommandBinder.cs:9-88` |
| Float and double overflow are already rejected with `IsFinite`; retain this unchanged | `Goose/Commands/CommandBinder.cs:59-71` |
| `CommandRegistry.Publish` validates keys/help, protects privilege downgrades, rebuilds trie/dictionary/order, then atomically swaps `_snapshot` | `Goose/Commands/CommandRegistry.cs:219-308` |
| `CommandDefinition.Keys` owns every alias; `PrimaryKey` remains the canonical usage/help key | `Goose/Commands/CommandDefinition.cs:5-35` |
| Legacy type/factory definitions are dispatched from the command snapshot before the packet trie | `Goose/EventHandler.cs:292-336` |
| Deprecated `RegisterEvent` slash keys currently warn but still enter `packetTrie` | `Goose/EventHandler.cs:243-279` |
| Shipped scripts still use `RegisterEvent` for `/dimension`, `/resetitem`, `/buygold`, `/buyexperience`, and `/givesp` | `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:229-233` |
| Help builds from one captured snapshot and already filters visibility before returning a matching primary definition | `Goose/Commands/HelpFormatter.cs:61-115` |
| Section grouping already uses `StringComparer.OrdinalIgnoreCase`; retain it | `Goose/Commands/CommandRegistry.cs:174-193` |
| `HelpFormatter.AddWrapped` reserves two characters for continuation indentation | `Goose/Commands/HelpFormatter.cs:168-173` |
| `TestWorldFixture.RunCommand` exercises `AddEvent` followed by the real event queue update | `TestSupport/TestWorldFixture.cs:105-111` |
| Both test projects can access internal command seams | `Goose/Goose.csproj:19-26` |
| Synthetic worlds belong in `Goose.Tests`; shipped-script behavior belongs in `Goose.IntegrationTests` | `docs/testing.md:45-57` |
| Existing shipped-script integration coverage executes `/dimension`, `/buygold`, and `/resetitem` after `OnLoaded` registration | `Goose.IntegrationTests/DimensionCommandGateTests.cs:12-38`; `Goose.IntegrationTests/DimensionCurrencyCommandTests.cs:13-48`; `Goose.IntegrationTests/DimensionResetItemTests.cs:15-34,101-114` |

## Scope and retained behavior

- Keep null-handler, empty-help, malformed-key, duplicate-key, unsupported-signature, cross-definition replacement, privilege-downgrade, and non-finite numeric rejection exactly as implemented.
- Keep one immutable-by-construction snapshot publication boundary and lock-free reads.
- Keep class-level `CheckAccess` before target selection and every possible response.
- Keep subcommand-only behavior: bare and unknown selectors show the privilege-filtered list; directly selecting a restricted subcommand remains silent.
- Keep `PrimaryKey` as the displayed usage key even when help was requested through an alias.
- Keep deprecated slash `RegisterEvent` calls working until the shipped scripts migrate; this part changes their storage/routing, not their public API.
- Do not migrate any command event class or `.csx` command to typed `Commands.Register` in this plan.
- Do not add comments or doc strings to production or test code. Existing unrelated comments remain untouched.
- No persistence or schema changes are involved.

### Task 1: Restore default `Execute` dispatch for mixed commands

**Files:**
- Modify: `Goose/Commands/CommandEvent.cs:47-101`
- Test: `Goose.Tests/CommandDispatchTests.cs:8-275`

**Mutation impact:**
- Source of truth changed: runtime target selection in `CommandEvent.Ready`; the definition itself remains owned by `CommandRegistry` and carries both `ExecuteMethod` and `Subcommands` at `Goose/Commands/CommandDefinition.cs:13-16`.
- Important readers: argument binding and invocation in `Goose/Commands/CommandEvent.cs:103-125`; help independently displays both default usage and subcommands in `Goose/Commands/HelpFormatter.cs:128-165`.
- Derived/cached state affected: No derived or cached state. The selected method and token slice are local to one queued event.
- Required propagation sequence:
  1. Build `CommandContext` and run class-level `CheckAccessInternal` exactly as today.
  2. Resolve a case-insensitive subcommand candidate from `args[0]` when present.
  3. If the candidate is usable, select its method and bind `args[1..]`.
  4. If no usable subcommand was selected and `ExecuteMethod` exists, select it and bind the full `args` array.
  5. If the class is subcommand-only, retain the existing list behavior for bare/unknown input and silent denial for a directly selected restricted subcommand.
  6. Generate usage, bind, and invoke only the selected target through the existing paths.
- Invariants to preserve:
  - A matching accessible subcommand wins over default `Execute`.
  - A mixed command's bare and unknown input reaches default `Execute`.
  - For a mixed command, a restricted selector seen by an unprivileged player behaves like an unknown selector and falls through to `Execute`; the privileged player reaches the subcommand.
  - A subcommand-only restricted selector remains silent and never emits the list.
  - `CheckAccess` denial precedes both fallback execution and list output.
- Observable proof required: real `RunCommand` tests assert the final message emitted by the selected handler, not an intermediate selected-method value.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Bare mixed command invokes default | `MixedCommand_bare_key_runs_default_execute` |
| Accessible selector wins | `MixedCommand_matching_selector_runs_subcommand` |
| Unknown selector falls through with its token intact | `MixedCommand_unknown_selector_falls_through_to_execute` |
| Restricted selector is non-probeable and still follows mixed fallback policy | `MixedCommand_restricted_selector_falls_through_for_normal_and_runs_for_gm` |
| Subcommand-only restricted selection remains silent | existing `SubcommandPrivilegeDeniesNormal` plus a focused assertion that no list text is sent |
| Class-level access still gates every branch | existing `CheckAccessDenialPrecedesSubcommandList` |

**Step 1: Write the failing mixed-command tests**

Add a test-only `[Command("/tmixed")]` class with `Execute(CommandContext ctx, string? token = null)`, an open `make` subcommand, and a `secret` subcommand requiring `AccessPrivilege.Ban`. Cover all four mixed cases in the matrix.

Red expectation: the matching-subcommand test passes already; bare and unknown cases receive the subcommand list instead of `default` / `exec bogus`, and the restricted-selector normal-player case is silent instead of reaching the fallback.

**Step 2: Run the focused tests to verify red**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~CommandDispatchTests
```

Expected: only the new mixed fallback assertions fail for the behaviors above.

**Step 3: Implement minimal target selection**

Restructure only the target-selection block. Do not move `CheckAccess`, binding, exception unwrapping, or invocation. Treat these shapes explicitly:

| Definition shape / input | Target |
|---|---|
| Handler definition | delegate handler |
| Accessible matching subcommand | subcommand, tokens after selector |
| Mixed command with no accessible matching subcommand | `ExecuteMethod`, all tokens |
| Subcommand-only, bare or unknown selector | filtered subcommand list |
| Subcommand-only, matching restricted selector | silent return |
| Execute-only definition | `ExecuteMethod`, all tokens |

Keep the worktree's current subcommand-list format; formatting cleanup is not part of this defect.

**Step 4: Run focused tests to verify green**

Run the Task 1 command again.

Expected: all `CommandDispatchTests` pass.

**Step 5: Commit**

```bash
git add Goose/Commands/CommandEvent.cs Goose.Tests/CommandDispatchTests.cs
git commit -m "fix: dispatch mixed commands to their default execute"
```

### Task 2: Validate and bind delegates against one real invocation signature

**Files:**
- Modify: `Goose/Commands/CommandBinder.cs:90-157`
- Modify: `Goose/Commands/CommandRegistry.cs:143-160`
- Modify: `Goose/Commands/CommandEvent.cs:81-104`
- Modify: `Goose/Commands/HelpFormatter.cs:145-165`
- Test: `Goose.Tests/CommandRegistryTests.cs:225-290`
- Test: `Goose.Tests/CommandDispatchTests.cs:23-144`
- Test: `Goose.Tests/HelpTests.cs:71-83`

**Helper extraction contract:**
- Add an internal `CommandBinder.InvocationParameters(Delegate handler)` helper that returns `handler.GetType().GetMethod("Invoke")!.GetParameters()`. It only describes what `DynamicInvoke` accepts; it does not validate, register, publish, bind, invoke, or mutate anything.
- Add an internal `CommandBinder.UsageParameters(Delegate handler)` helper. It returns bound method parameters for names when they align with the invocation signature; for a closed static delegate whose method has one extra leading bound parameter, return the method-parameter suffix; otherwise fall back to the invocation parameters. It does not change accepted types or invocation shape.
- All validation and binding uses `InvocationParameters`. Usage rendering uses `UsageParameters`, preserving names such as `<name>` / `<n>` for ordinary method-group delegates and aligning names correctly for closed static delegates.

**Mutation impact:**
- Source of truth changed: the registration acceptance rule in `CommandRegistry.RegisterKeys`; the delegate type's `Invoke` method becomes authoritative because `DynamicInvoke` consumes that signature.
- Important readers: `CommandEvent` bind/invoke path and `HelpFormatter.UsageText`; both must use the same helpers rather than independently reflecting different surfaces.
- Derived/cached state affected: each successful registration publishes a `CommandDefinition` into `CommandSnapshot`; no additional cached signature state is introduced.
- Required propagation sequence:
  1. Null-check the handler before any reflection.
  2. Resolve invocation parameters from the delegate type.
  3. Validate those parameters with the existing `IsValidTarget` boundary checks.
  4. On failure, return `false` before `Publish`; the snapshot remains byte-for-byte unchanged.
  5. On success, publish normally; dispatch binds the same invocation parameters and `DynamicInvoke`s the resulting arguments.
  6. Dispatch errors and help use aligned display parameters without changing the invocation parameters.
- Invariants to preserve:
  - Null delegates still return `false`, not throw.
  - Ordinary method-group usage keeps source parameter names.
  - A valid closed static delegate is accepted and invoked.
  - An open instance delegate whose `Invoke` signature starts with the receiver type is rejected before publication even though `handler.Method` itself starts with `CommandContext`.
  - Unsupported types and non-final `string[]` remain rejected.
- Observable proof required: verify both registry membership and end-to-end handler output; verify failed registration leaves the previously captured snapshot/reference counts unchanged.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Registration checks the actual `DynamicInvoke` surface | `Register_open_instance_delegate_with_receiver_first_is_refused` |
| Valid closed static binding is not falsely rejected | `Register_closed_static_delegate_is_accepted_and_runs` |
| Closed static usage omits the bound leading parameter and keeps source names | `BuildPages_closed_static_delegate_uses_unbound_parameter_names` and parse-error assertion in the dispatch test |
| Ordinary usage names do not regress | existing `BuildPages_section_page_lines_are_wrapped_with_indent` and `ParseErrorSendsUsage` |
| Failed validation does not publish | open-instance test compares `Snapshot`/`ByKey` before and after |

**Step 1: Write the failing exotic-delegate tests**

Use a test-only static method shaped as `ClosedStatic(Capture capture, CommandContext ctx, int n)` and create a closed `Action<CommandContext, int>` with the `Capture` instance bound to its first argument. Assert registration succeeds, `/closed 7` mutates the capture or emits `7`, and missing input reports `Usage: /closed <n>` rather than including the bound parameter.

Use a test-only instance method shaped as `OpenTarget.Handle(CommandContext ctx)` and create an open `Action<OpenTarget, CommandContext>` from its `MethodInfo`. Assert registration returns `false` and no key or definition is published.

Red expectation: the valid closed static delegate is rejected because `handler.Method` starts with `Capture`; the invalid open instance delegate is accepted because `handler.Method` hides the receiver that appears in `Invoke`.

**Step 2: Run focused tests to verify red**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandRegistryTests|FullyQualifiedName~CommandDispatchTests|FullyQualifiedName~HelpTests"
```

Expected: the new exotic-delegate assertions fail for the reasons above; existing parameter-name assertions remain green.

**Step 3: Add and adopt the signature helpers**

Implement the helper contracts and use them at all three boundaries:

- `CommandRegistry.RegisterKeys`: null check, then `IsValidTarget(InvocationParameters(handler), out error)`.
- `CommandEvent`: bind `InvocationParameters(handler)` and build usage from `UsageParameters(handler)`.
- `HelpFormatter.UsageText`: build handler usage from `UsageParameters(handler)`.

Do not weaken `IsValidTarget`, do not special-case the test delegate types, and do not cache mutable reflection arrays on `CommandDefinition`.

**Step 4: Run focused tests to verify green**

Run the Task 2 command again.

Expected: all selected tests pass.

**Step 5: Commit**

```bash
git add Goose/Commands/CommandBinder.cs Goose/Commands/CommandRegistry.cs Goose/Commands/CommandEvent.cs Goose/Commands/HelpFormatter.cs Goose.Tests/CommandRegistryTests.cs Goose.Tests/CommandDispatchTests.cs Goose.Tests/HelpTests.cs
git commit -m "fix: validate command delegates by invoke signature"
```

### Task 3: Publish deprecated slash factories through `CommandRegistry`

**Files:**
- Modify: `Goose/Commands/CommandRegistry.cs:118-141`
- Modify: `Goose/EventHandler.cs:243-279`
- Test: `Goose.Tests/CommandDispatchTests.cs:232-307`
- Test: `Goose.Tests/EventHandlerTests.cs:8-20`

**Mutation impact:**
- Source of truth changed: slash-key factories registered through `EventHandler.RegisterEvent` move from the mutable `packetTrie` to the immutable `CommandRegistry` snapshot. Non-slash packet factories remain in `packetTrie`.
- Important readers: `EventHandler.AddEvent` reads the command snapshot first and dispatches `LegacyFactory` at `Goose/EventHandler.cs:292-325`; packet fallback remains at `Goose/EventHandler.cs:336-368`.
- Derived/cached state affected: `CommandSnapshot.Trie`, `ByKey`, and `Ordered` are rebuilt together by `CommandRegistry.Publish`. No help section is created because legacy factories retain `Section == null` and empty help.
- Required propagation sequence:
  1. `RegisterEvent` checks whether the key starts with `/`.
  2. For slash keys, log the existing deprecation warning, call the factory-specific `RegisterLegacy` overload with the stated privilege, and return without touching `packetTrie`.
  3. `RegisterLegacy` validates through `Publish`; a successful publication becomes visible only at the single `_snapshot` assignment.
  4. On refusal, leave both the command snapshot and packet trie unchanged; `RegisterEvent` remains `void`, and the registry's error log is the observable failure report.
  5. For non-slash keys, retain the current packet-trie replacement and privilege logic unchanged.
  6. At dispatch, `AddEvent` gates the published command privilege, invokes the legacy factory, marks the event `ClientOriginated`, and enqueues it exactly as today.
- Publication boundary:
  - Creation order: validate key/factory and replacement policy → construct the legacy `CommandDefinition` → rebuild trie/dictionary/order → assign `_snapshot`.
  - Readers see either the old complete snapshot or the new complete snapshot; no partial factory definition is visible.
  - Failure leaves no packet-trie fallback entry and no partial command entry.
- Invariants to preserve:
  - Shipped slash factory registrations still execute through the queue.
  - Re-registering a slash factory replaces the prior open factory, preserving script-reload behavior.
  - A slash factory cannot bypass registry privilege-downgrade protection or shadow a restricted built-in through a second path.
  - Restricted slash factories are swallowed for normal players and run for GMs.
  - Non-slash factory packets such as `GID` still use the packet trie.
  - Legacy factory definitions remain excluded from help.
- Observable proof required: inspect `Commands.TryGet` after slash registration and run the command through `TestWorldFixture.RunCommand`; for non-slash registration, assert `TryGet` misses while dispatch succeeds.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Slash factory is published in the registry and runs | revise `RegisterEventSlashKeyStillWorks` |
| Reload-style registration replaces the old factory | `RegisterEvent_slash_factory_reregistration_runs_only_new_factory` |
| Open slash registration cannot bypass `/shutdown` restriction | revise `RegisterEventOpenFactoryShadowedByRestrictedLegacyCommand` to assert registry ownership remains unchanged |
| Restricted slash factory uses the registry privilege gate | `RegisterEvent_restricted_slash_factory_is_swallowed_for_normal_and_runs_for_gm` |
| Non-slash registration stays out of the registry and still runs | revise `RegisterEventNonSlashKeyStillWorks` |
| Shipped registrations survive the routing change | existing dimension integration tests listed in the verified API table |

**Step 1: Strengthen routing tests**

Update the current slash and non-slash tests to assert where the registration was published, not only that it eventually ran. Add the reload-replacement and restricted slash-factory tests. Preserve the warning assertion in `CommandDispatchLoggingTests`.

Red expectation: slash dispatch still works through the old packet path, but `Commands.TryGet` misses; the restricted slash test cannot observe registry ownership.

**Step 2: Run focused tests to verify red**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandDispatchTests|FullyQualifiedName~EventHandlerTests"
```

Expected: the new registry-publication assertions fail.

**Step 3: Split the legacy registration overloads and route slash keys**

Expose two internal shapes in `CommandRegistry`:

```csharp
internal bool RegisterLegacy(string key, Type eventType, AccessPrivilege? privilege)
internal bool RegisterLegacy(string key, EventHandler.CreateEvent factory, AccessPrivilege? privilege)
```

Both construct definitions with exactly one legacy target: type registrations set only `LegacyType`; factory registrations set only `LegacyFactory`. Neither sets `Handler`, `ExecuteMethod`, or help metadata. The type overload retains the current no-replacement guard used while seeding built-ins. The factory overload calls `Publish` without that guard so reload-style registration replaces the prior definition in place, while `Publish` still refuses restricted-to-open downgrades and cross-definition conflicts.

In both `EventHandler.RegisterEvent` overloads, slash keys warn, call the matching factory overload, and return. Do not insert slash keys into `packetTrie`.

**Step 4: Run focused and shipped-script verification**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~CommandDispatchTests|FullyQualifiedName~EventHandlerTests"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~DimensionCommandGateTests|FullyQualifiedName~DimensionCurrencyCommandTests|FullyQualifiedName~DimensionResetItemTests|FullyQualifiedName~DimensionsScriptTests"
```

Expected: all selected tests pass; the integration selection proves the shipped `.csx` factories register and execute through the changed route.

**Step 5: Commit**

```bash
git add Goose/Commands/CommandRegistry.cs Goose/EventHandler.cs Goose.Tests/CommandDispatchTests.cs Goose.Tests/EventHandlerTests.cs
git commit -m "refactor: route slash factories through command registry"
```

### Task 4: Complete alias-aware and width-safe help behavior

**Files:**
- Modify: `Goose/Commands/HelpFormatter.cs:13-59,61-115`
- Test: `Goose.Tests/HelpTests.cs:23-203`
- Test: `Goose.Tests/CommandRegistryTests.cs:293-304`

**Mutation impact:**
- Source of truth changed: no registry state changes. Help's lookup and formatting rules change how an already-captured `CommandSnapshot` is rendered.
- Important readers: `/help` calls `HelpFormatter.BuildPages`; `HelpWindow` sends every returned line to the client. `CommandDefinition.Keys` supplies aliases; `PrimaryKey` supplies canonical displayed usage.
- Derived/cached state affected: no cached pages or derived registry state exists. Pages are recomputed per request.
- Required propagation sequence:
  1. Normalize a requested command name by trimming trailing spaces and removing one optional leading `/`.
  2. Iterate `snapshot.Ordered`; skip definitions without help sections and definitions unusable by the player before matching.
  3. Match the normalized request against every `def.Keys` entry, case-insensitively.
  4. Render the selected definition with its canonical `PrimaryKey`; then independently resolve and render a same-named visible section as today.
  5. During wrapping, once existing text is emitted as the first line, switch to `continuationBudget` before hard-splitting the next overlong word.
  6. Prefix continuation lines with two spaces only after the raw wrapped segments are guaranteed to fit `MaxLineLength - 2`.
- Invariants to preserve:
  - Any alias resolves to the owning definition, but usage remains canonical.
  - A restricted normalized match never hides a later usable definition or a visible same-named section.
  - Section and command resolution remain independent and case-insensitive.
  - Section grouping remains case-insensitive and preserves the first spelling and registration order.
  - Every emitted help-window line is at most 42 characters, including indentation.
- Observable proof required: assert exact page text and final emitted line lengths rather than testing private helper calls only.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Alias and slash-prefixed alias resolve to canonical help | `BuildPages_alias_name_resolves_definition_with_primary_usage` |
| Restricted match does not hide later usable normalized match | existing `BuildPages_same_name_prefers_first_usable_definition`, extended with an alias-backed restricted definition |
| Restricted command does not hide public same-named section | existing `BuildPages_restricted_command_same_name_as_public_section` |
| Overlong continuation word honors the 40-character raw budget | `BuildPages_overlong_word_after_prefix_fits_with_indent` |
| Case-variant sections are one group with stable display spelling | `Sections_group_case_insensitively_and_preserve_first_spelling` |
| Every final line fits | existing `BuildPages_every_rendered_line_fits_42_chars` plus the new adversarial wrap case |

**Step 1: Write the failing alias and wrap tests**

Register an alias-ready definition through the existing internal `RegisterKeys`, request help by both `alias` and `/ALIAS `, and assert the page displays the primary key's usage.

For wrapping, use logical text with a short first fragment followed by a word longer than the 40-character continuation budget. Assert the exact split and `Assert.All(... line.Length <= HelpFormatter.MaxLineLength)` after indentation. This specifically fails when hard splitting retains the already-consumed 42-character first-line budget.

Add a registry test that registers sections `Admin` and `ADMIN`; assert one `CommandSection`, name `Admin`, with both definitions in registration order.

Red expectation: alias lookup returns `null`; the adversarial wrapped continuation reaches 44 characters after indentation. The section-grouping test should already pass and serves as a preservation pin.

**Step 2: Run focused tests to verify red**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~HelpTests|FullyQualifiedName~CommandRegistryTests"
```

Expected: alias and continuation-wrap tests fail for the stated reasons; the case-insensitive grouping pin passes.

**Step 3: Implement the minimal help changes**

- Extract one small key-normalization helper used for both the request and each registered key.
- Change `FindCommand` from `PrimaryKey` comparison to `def.Keys.Any(...)`, retaining the current visibility-before-match ordering.
- In `Wrap`, when flushing non-empty `current` before hard splitting the current word, set the chunk budget to `continuationBudget` before emitting chunks.
- Do not change pagination height, help ordering, privilege checks, section matching, or canonical usage keys.

**Step 4: Run focused tests to verify green**

Run the Task 4 command again.

Expected: all selected tests pass.

**Step 5: Commit**

```bash
git add Goose/Commands/HelpFormatter.cs Goose.Tests/HelpTests.cs Goose.Tests/CommandRegistryTests.cs
git commit -m "fix: make command help alias-aware and width-safe"
```

### Task 5: Full regression and integration verification

**Files:**
- No source changes expected

**Step 1: Run the complete fast suite**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Expected: all tests pass, including malformed registration, numeric overflow, legacy dispatch, mixed dispatch, delegate signatures, and help tests.

**Step 2: Run the complete integration suite**

Run:

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: all tests pass. This project is outside `Goose.sln`, so it must be invoked explicitly.

**Step 3: Inspect the final diff and retained boundaries**

Verify:

- no production/test comments or doc strings were added;
- no finite-number, null, key, help, signature-type, or privilege validation was weakened;
- slash keys have exactly one registration path;
- `CommandSnapshot` publication remains one final assignment after complete rebuild;
- no command migration, public API removal, schema, or persistence change slipped into Part 1b.

**Step 4: Record verification without an extra code commit**

If formatting or cleanup changes are required, apply them to the owning task's commit and rerun both suites. Otherwise, leave the four coherent commits as the final history.

## Final red-team review

- **Threading/publication:** slash factory registration can occur during script reload. It uses the existing registry lock and immutable snapshot swap; dispatch captures one snapshot reference and cannot observe a partial definition.
- **Lifecycle:** factory-created events still become `ClientOriginated` and enter the existing queue. No worker, stream, or teardown lifecycle is introduced.
- **Failure behavior:** invalid delegates and refused slash replacements leave the old snapshot intact. `RegisterEvent` is `void`, so logs remain its only registration-failure signal.
- **Security:** class, command, and subcommand privilege denials remain silent. Mixed restricted selectors fall through exactly as unknown selectors for users without the privilege, avoiding a selector-existence oracle.
- **Help visibility:** matching checks usability before returning a definition, and command/section results remain independent. Aliases expand names only; they do not bypass privilege checks.
- **Persistence/schema:** none; no migration is needed.
- **Test helper reality:** `TestWorldFixture.CommandPlayerOn` creates a Ready capturing player and `RunCommand` executes `AddEvent` plus `Update`, so dispatch tests exercise final observable output through the real queue.
- **Task order:** mixed selection is isolated first; delegate metadata is unified before help consumes it; slash publication changes after registry validation is pinned; help changes last. Every task can be reviewed and reverted independently.

## Execution note

The relocated worktree's `.git` file currently points at `/home/agent/workspace/...`; ordinary `git` commands fail until that administrative link is repaired. Source inspection and tests work, and Git can currently inspect the tree with explicit `--git-dir=/home/hayden/code/illutiagooseserver/.git/worktrees/command-system --work-tree=/home/hayden/code/illutiagooseserver/.worktrees/command-system`. Repair or recreate the worktree link before using the commit commands above; do not mix that environment repair into a Part 1b feature commit.
