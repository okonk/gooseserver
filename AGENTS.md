# AGENTS.md

Guidance for AI coding agents working in this repository.

## Comments and doc strings: default to none

Do **not** add comments or doc strings (`///`, `//`, XML docs, test comments) to new or
modified code. The existing codebase is already heavily documented; new code must not
add to that volume. The code should be self-explanatory, and by default it is.

**Allowed — only when the "why" is non-obvious from the code itself:**

- A non-obvious invariant or precondition (e.g. "caller must hold the lock", "ids are
  range-validated upstream", "runs on the game thread only").
- A workaround for a bug or constraint in an external system (cite what is being worked
  around and where).
- Wire-protocol or data-format details not visible in the code (field layout, byte
  offsets, client quirks).
- Why an approach that looks wrong is actually correct.

When the *what* is obvious from the code, omit the comment entirely. Keep any justified
comment to one or two lines.

**When editing existing code:** leave unrelated comments alone — do not bulk-delete
existing documentation as a side effect. If a comment on a line you are touching is now
wrong or redundant, update or remove it.
