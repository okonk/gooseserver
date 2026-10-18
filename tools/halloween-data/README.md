# Halloween event data

`build.py` turns the event's rows into a batch for `gsheets.py`. See
`docs/plans/2026-10-18-halloween-part4-data.md`.

```bash
python3 tools/halloween-data/build.py                      # reads next ids from the sheet
python3 .agents/skills/goose-game-data/scripts/gsheets.py batch tools/halloween-data/out/batch.json --dry-run
python3 .agents/skills/goose-game-data/scripts/gsheets.py batch tools/halloween-data/out/batch.json
python3 tools/halloween-data/build.py --in-place           # writes the ids into halloween.json
```

Outputs in `out/`: `batch.json` (sheet creates and updates), `manual.md` (warp edits the helper
can't make, and the after-event steps), `revert.json` (Grove pumpkins back to normal),
`halloween.json` (patched config, unless `--in-place`), `ids.json` (key → allocated id).

Ids come from the sheet's `nextid` at build time, so rebuild right before applying and apply the
batch you just built. Apply after the Parts 2-3 scripts are merged: `/reloadsql` compiles every
script a row references.
