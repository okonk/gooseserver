#!/usr/bin/env python3
import argparse
import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("gsheets", ROOT / ".agents/skills/goose-game-data/scripts/gsheets.py")
gs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gs)

TITLE = "Warptiles"
HOUSE, GROVE, BASEMENT = 55, 38, 39

DELETE = [
    (GROVE, 37, 35, BASEMENT, 5, 98), (GROVE, 38, 35, BASEMENT, 6, 98), (GROVE, 39, 35, BASEMENT, 7, 98),
    (BASEMENT, 40, 4, BASEMENT, 93, 99), (BASEMENT, 41, 4, BASEMENT, 94, 99), (BASEMENT, 42, 4, BASEMENT, 95, 99),
]
REPOINT = [
    ((BASEMENT, 93, 100, BASEMENT, 40, 5), (HOUSE, 15, 60)),
    ((BASEMENT, 94, 100, BASEMENT, 41, 5), (HOUSE, 16, 60)),
    ((BASEMENT, 95, 100, BASEMENT, 42, 5), (HOUSE, 17, 60)),
]


def find(rows, values):
    matches = [n for n, r in rows if tuple(int(float(r[i])) for i in range(6)) == values]
    if len(matches) != 1:
        sys.exit(f"warps: expected one row matching {values}, found {len(matches)}")
    return matches[0]


def main():
    parser = argparse.ArgumentParser(description="Make the Halloween warp edits gsheets.py can't.")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    _, _, _, rows = gs.table(TITLE)
    rows = [(n, r) for n, r in rows if all(str(r[i]).strip() for i in range(6))]
    repoints = [(find(rows, old), new) for old, new in REPOINT]
    deletes = sorted((find(rows, values) for values in DELETE), reverse=True)

    for row_number, new in repoints:
        print(f"{'would' if args.dry_run else 'did'} repoint row {row_number} to map {new[0]} ({new[1]},{new[2]})")
    for row_number in deletes:
        print(f"{'would' if args.dry_run else 'did'} delete row {row_number}")
    if args.dry_run:
        return

    for row_number, new in repoints:
        gs.write_range(TITLE, row_number, 3, list(new), "RAW")
    sheet_id = gs.sheet_properties(TITLE)["sheetId"]
    gs.gws(["batchUpdate"], {"spreadsheetId": gs.SPREADSHEET_ID}, {"requests": [
        {"deleteDimension": {"range": {"sheetId": sheet_id, "dimension": "ROWS",
                                       "startIndex": n - 1, "endIndex": n}}}
        for n in deletes
    ]})


if __name__ == "__main__":
    main()
