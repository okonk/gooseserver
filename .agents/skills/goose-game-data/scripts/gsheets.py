#!/usr/bin/env python3
import argparse
import json
import os
import re
import subprocess
import sys
import time
from pathlib import Path

SPREADSHEET_ID = "1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU"
API = ["sheets", "spreadsheets"]


def repo_root():
    for parent in Path(__file__).resolve().parents:
        if (parent / "Goose.sln").exists():
            return parent
    sys.exit("gsheets: Goose.sln not found above this script")


def config_dir():
    override = os.environ.get("GOOSE_GWS_CONFIG_DIR")
    return Path(override) if override else repo_root() / ".gws"


def gws(subcommand, params=None, body=None, dry_run=False):
    cfg = config_dir()
    if not cfg.is_dir():
        sys.exit(
            f"gsheets: no gws config directory at {cfg}\n"
            "gws cannot run as-is: it rewrites its token cache in ~/.config/gws on every call,\n"
            "and that path is read-only under the workspace sandbox. Bootstrap a writable copy:\n"
            f"  cp -a ~/.config/gws {cfg} && chmod -R u+rwX {cfg}"
        )
    cmd = ["gws", *API, *subcommand]
    if params is not None:
        cmd += ["--params", json.dumps(params)]
    if body is not None:
        cmd += ["--json", json.dumps(body)]
    if dry_run:
        cmd.append("--dry-run")
    env = dict(os.environ, GOOGLE_WORKSPACE_CLI_CONFIG_DIR=str(cfg))
    for attempt in range(4):
        proc = subprocess.run(cmd, capture_output=True, text=True, env=env)
        if proc.returncode == 0:
            return proc.stdout.strip()
        combined = f"{proc.stderr}\n{proc.stdout}"
        # The Sheets read quota is per minute, and a batch of appends can trip it mid-run.
        if attempt < 3 and ("Quota exceeded" in combined or '"code": 429' in combined):
            time.sleep(20 * (attempt + 1))
            continue
        sys.exit(f"gsheets: gws {' '.join(subcommand)} failed\n{proc.stderr.strip()}\n{proc.stdout.strip()}")


def loads(text):
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        sys.exit(f"gsheets: expected JSON from gws, got:\n{text[:2000]}")


def col_letter(index):
    letters = ""
    while True:
        letters = chr(65 + index % 26) + letters
        index = index // 26 - 1
        if index < 0:
            return letters


def col_index(letters):
    index = 0
    for ch in letters.upper():
        index = index * 26 + (ord(ch) - 64)
    return index - 1


def norm(text):
    return re.sub(r"[^a-z0-9]", "", re.sub(r"\(.*?\)", "", str(text)).lower())


_SHEETS = {}
_GRID = {}


def sheet_properties(title):
    if not _SHEETS:
        data = loads(gws(["get"], {"spreadsheetId": SPREADSHEET_ID, "fields": "sheets.properties"}))
        _SHEETS.update({s["properties"]["title"]: s["properties"] for s in data["sheets"]})
    if title not in _SHEETS:
        sys.exit(f"gsheets: no worksheet named {title!r}. Worksheets: {', '.join(sorted(_SHEETS))}")
    return _SHEETS[title]


def invalidate(title):
    for key in [k for k in _GRID if k[0] == title]:
        del _GRID[key]


def goose_schema(title):
    source = (repo_root() / "tools/DataEditor/schema.js").read_text()
    data = json.loads(source[source.index("{"):source.rindex("}") + 1])
    for sheet in data["sheets"]:
        if sheet["sheet"] == title:
            return sheet
    sys.exit(f"gsheets: {title!r} is not in GOOSE_SCHEMA")


def grid(title, render="UNFORMATTED_VALUE"):
    if (title, render) in _GRID:
        return _GRID[(title, render)]
    props = sheet_properties(title)
    rows = props["gridProperties"]["rowCount"]
    cols = props["gridProperties"]["columnCount"]
    data = loads(gws(["values", "get"], {
        "spreadsheetId": SPREADSHEET_ID,
        "range": f"'{title}'!A1:{col_letter(cols - 1)}{rows}",
        "valueRenderOption": render,
    }))
    values = [list(r) + [""] * (cols - len(r)) for r in (data.get("values") or [])]
    _GRID[(title, render)] = values
    return values


def table(title):
    schema = goose_schema(title)
    cols = schema["columns"]
    values = grid(title)
    labels = values[0][:len(cols)] if values else [None] * len(cols)
    rows = []
    for index, row in enumerate(values[1:], start=2):
        if any(str(cell).strip() for cell in row[:len(cols)] if cell is not None):
            rows.append((index, row))
    return schema, cols, labels, rows


def column(title, key):
    schema = goose_schema(title)
    cols = schema["columns"]
    labels = grid(title)[0]
    wanted = norm(key)
    for col in cols:
        if norm(col["name"]) == wanted:
            return col
    for index, label in enumerate(labels[:len(cols)]):
        if label is not None and norm(label) == wanted:
            return cols[index]
    if re.fullmatch(r"[A-Za-z]{1,3}", key) and col_index(key) < len(cols):
        return cols[col_index(key)]
    listed = ", ".join(f"{col_letter(i)}:{c['name']}" for i, c in enumerate(cols))
    sys.exit(f"gsheets: {key!r} matches no column of {title!r}. Columns: {listed}")


def helper_columns(title):
    schema = goose_schema(title)
    width = len(schema["columns"])
    labels = grid(title)[0]
    return [i for i in range(width, len(labels)) if labels[i] is not None and str(labels[i]).strip()]


def coerce(col, value):
    kind = col["kind"]
    if kind in ("Id", "Int"):
        try:
            return int(value)
        except (TypeError, ValueError):
            sys.exit(f"gsheets: {col['name']} expects a whole number, got {value!r}")
    if kind == "Double":
        try:
            return float(value)
        except (TypeError, ValueError):
            sys.exit(f"gsheets: {col['name']} expects a number, got {value!r}")
    if kind == "Bool":
        text = str(value).strip().lower()
        if text in ("1", "true", "yes"):
            return 1
        if text in ("0", "false", "no", ""):
            return 0
        sys.exit(f"gsheets: {col['name']} expects 0 or 1, got {value!r}")
    if kind == "Enum":
        names = col.get("enumNames") or []
        if str(value) not in names:
            sys.exit(f"gsheets: {col['name']} has no member {value!r}. Allowed: {', '.join(names)}")
        return str(value)
    return str(value)


def parse_sets(pairs):
    sets = {}
    for pair in pairs:
        if "=" not in pair:
            sys.exit(f"gsheets: --set expects column=value, got {pair!r}")
        key, value = pair.split("=", 1)
        sets[key.strip()] = value
    return sets


def next_id(title):
    _, cols, _, rows = table(title)
    pk = next((i for i, col in enumerate(cols) if col.get("pk")), None)
    if pk is None:
        sys.exit(f"gsheets: {title!r} has no primary key; supply the key columns explicitly")
    highest = 0
    for _, row in rows:
        try:
            highest = max(highest, int(float(str(row[pk]).replace(",", ""))))
        except (TypeError, ValueError):
            continue
    return highest + 1


def shift_formula(formula, old_row, new_row):
    return re.sub(
        r"(?<![A-Za-z0-9_])(\$?[A-Z]{1,3}\$?)" + str(old_row) + r"(?![0-9])",
        lambda m: m.group(1) + str(new_row),
        formula,
    )


def write_range(title, first_row, first_col, values, render_option):
    last_col = first_col + len(values) - 1
    gws(["values", "update"], {
        "spreadsheetId": SPREADSHEET_ID,
        "range": f"'{title}'!{col_letter(first_col)}{first_row}:{col_letter(last_col)}{first_row}",
        "valueInputOption": render_option,
    }, {"values": [values]})
    invalidate(title)


def report(title, cols, labels, rows):
    print(f"{title}: {len(rows)} row(s)")
    for row_number, row in rows:
        fields = []
        for index, col in enumerate(cols):
            cell = row[index]
            if cell is None or str(cell).strip() == "":
                continue
            label = labels[index] if index < len(labels) and labels[index] else col["name"]
            fields.append(f"{label}={cell}")
        print(f"  row {row_number}: " + " | ".join(fields))


def cmd_doctor(args):
    cfg = config_dir()
    print(f"config_dir: {cfg}")
    print(f"config_exists: {cfg.is_dir()}")
    print(gws(["values", "get"], {"spreadsheetId": SPREADSHEET_ID, "range": "A1:A1"}).strip() or "{}")
    data = loads(gws(["get"], {"spreadsheetId": SPREADSHEET_ID, "fields": "properties.title"}))
    print(f"spreadsheet: {data['properties']['title']}")


def cmd_tabs(args):
    data = loads(gws(["get"], {"spreadsheetId": SPREADSHEET_ID, "fields": "sheets.properties"}))
    for sheet in data["sheets"]:
        props = sheet["properties"]
        grid_props = props["gridProperties"]
        print(f"{props['title']:<28} sheetId={props['sheetId']:<12} rows={grid_props['rowCount']:<6} cols={grid_props['columnCount']}")


def cmd_schema(args):
    schema = goose_schema(args.sheet)
    print(f"{schema['sheet']} -> {schema['table']}")
    for index, col in enumerate(schema["columns"]):
        parts = [f"{col_letter(index):>3}", f"{col['name']:<28}", f"{col['kind']:<7}"]
        if col.get("required"):
            parts.append("REQUIRED")
        if col.get("pk"):
            parts.append("PK")
        if col.get("enumNames"):
            parts.append("ENUM[" + "|".join(col["enumNames"]) + "]")
        if "default" in col:
            parts.append(f"default={col['default']}")
        print("  " + " ".join(parts))
    helpers = helper_columns(args.sheet)
    if helpers:
        print("  helper columns (formulas, not imported): " + ", ".join(col_letter(i) for i in helpers))


def cmd_read(args):
    schema, cols, labels, rows = table(args.sheet)
    if args.id is not None:
        rows = [(n, r) for n, r in rows if str(r[0]).strip() == str(args.id)]
    for pair in args.where or []:
        col = column(args.sheet, pair.split("=", 1)[0])
        index = cols.index(col)
        want = pair.split("=", 1)[1]
        rows = [(n, r) for n, r in rows if str(r[index]).strip() == want]
    if args.limit:
        rows = rows[:args.limit]
    if args.json:
        print(json.dumps([{cols[i]["name"]: r[i] for i in range(len(cols))} for _, r in rows], indent=2))
    else:
        report(args.sheet, cols, labels, rows)


def cmd_nextid(args):
    print(next_id(args.sheet))


def apply_create(title, sets, dry_run=False, helper_formulas=True):
    schema = goose_schema(title)
    cols = schema["columns"]
    provided = {cols.index(column(title, key)): value for key, value in sets.items()}
    row = [""] * len(cols)
    for index, value in provided.items():
        row[index] = coerce(cols[index], value)
    pk = next((i for i, col in enumerate(cols) if col.get("pk")), None)
    if pk is not None and row[pk] == "":
        row[pk] = next_id(title)
    missing = [col["name"] for i, col in enumerate(cols) if col.get("required") and row[i] == ""]
    if missing:
        sys.exit(f"gsheets: {title} requires: {', '.join(missing)}")
    _, _, _, rows = table(title)
    target = (rows[-1][0] if rows else 1) + 1
    props = sheet_properties(title)
    if target > props["gridProperties"]["rowCount"]:
        gws(["batchUpdate"], {"spreadsheetId": SPREADSHEET_ID}, {"requests": [{"appendDimension": {
            "sheetId": props["sheetId"], "dimension": "ROWS", "length": 1}}]})
        # The grid grew, so the cached rowCount would truncate every later read in this
        # process — and a truncated read makes the next create in a batch reuse a taken row.
        _SHEETS.clear()
        invalidate(title)
    # Helper formulas are read before the write: invalidate() after it would cost another fetch.
    helpers = helper_columns(title) if helper_formulas else []
    templates = grid(title, render="FORMULA") if helpers else None
    if dry_run:
        return target, row
    # RAW: USER_ENTERED would turn text like "1-2" into a date and "01" into the number 1.
    write_range(title, target, 0, row, "RAW")
    if templates and 0 <= target - 2 < len(templates):
        source = templates[target - 2]
        formulas = [""] * len(helpers)
        for position, index in enumerate(helpers):
            cell = source[index]
            if isinstance(cell, str) and cell.startswith("="):
                formulas[position] = shift_formula(cell, target - 1, target)
        if any(formulas):
            write_range(title, target, helpers[0], formulas, "USER_ENTERED")
    return target, row


def apply_update(title, record_id, sets, dry_run=False):
    schema = goose_schema(title)
    cols = schema["columns"]
    _, _, _, rows = table(title)
    pk = next((i for i, col in enumerate(cols) if col.get("pk")), None)
    if pk is None:
        sys.exit(f"gsheets: {title!r} has no primary key; edit it by explicit range instead")
    matches = [(n, r) for n, r in rows if str(r[pk]).strip() == str(record_id)]
    if len(matches) != 1:
        sys.exit(f"gsheets: id {record_id} matches {len(matches)} row(s) of {title}")
    row_number, current = matches[0]
    changes = {}
    for key, value in sets.items():
        col = column(title, key)
        index = cols.index(col)
        new = coerce(col, value)
        if str(current[index]) != str(new):
            changes[index] = new
    if not changes or dry_run:
        return row_number, changes
    # One cell per update: the helper columns hold formulas that a wider write would overwrite.
    for index, value in sorted(changes.items()):
        write_range(title, row_number, index, [value], "RAW")
    return row_number, changes


def cmd_create(args):
    title = args.sheet
    target, row = apply_create(title, parse_sets(args.set), args.dry_run, not args.no_helper_formulas)
    if args.dry_run:
        print(f"would write {title}!A{target}:{col_letter(len(row) - 1)}{target}")
        print(json.dumps(row))
        return
    _, cols, labels, rows = table(title)
    report(title, cols, labels, [(n, r) for n, r in rows if n == target])


def cmd_update(args):
    title = args.sheet
    row_number, changes = apply_update(title, args.id, parse_sets(args.set), args.dry_run)
    if not changes:
        print(f"{title} row {row_number}: nothing to change")
        return
    if args.dry_run:
        for index, value in sorted(changes.items()):
            print(f"would set {col_letter(index)}{row_number} = {value}")
        return
    _, cols, labels, rows = table(title)
    report(title, cols, labels, [(n, r) for n, r in rows if n == row_number])


def cmd_batch(args):
    operations = json.loads(Path(args.file).read_text())
    applied = []
    for operation in operations:
        kind = operation.get("op")
        title = operation["sheet"]
        verb = "would" if args.dry_run else "did"
        if kind == "create":
            target, row = apply_create(title, operation["set"], args.dry_run)
            applied.append((title, target))
            identifier = f"row {target}"
            if row and row[0] != "":
                identifier += f" (id {row[0]})"
            print(f"{verb} create {title} {identifier}")
        elif kind == "update":
            row_number, changes = apply_update(title, operation["id"], operation["set"], args.dry_run)
            applied.append((title, row_number))
            detail = ", ".join(f"{col_letter(i)}={v}" for i, v in sorted(changes.items())) or "no change"
            print(f"{verb} update {title} id {operation['id']} (row {row_number}): {detail}")
        else:
            sys.exit(f"gsheets: unknown op {kind!r} in {args.file}")
    if args.dry_run:
        return
    print()
    for title in dict.fromkeys(t for t, _ in applied):
        invalidate(title)
        _, cols, labels, rows = table(title)
        wanted = {n for t, n in applied if t == title}
        report(title, cols, labels, [(n, r) for n, r in rows if n in wanted])


def main():
    parser = argparse.ArgumentParser(prog="gsheets", description="Read and write the Goose game data spreadsheet.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("doctor", help="check gws config and spreadsheet access").set_defaults(func=cmd_doctor)
    subparsers.add_parser("tabs", help="list worksheets").set_defaults(func=cmd_tabs)

    schema_parser = subparsers.add_parser("schema", help="show a worksheet's columns")
    schema_parser.add_argument("sheet")
    schema_parser.set_defaults(func=cmd_schema)

    read_parser = subparsers.add_parser("read", help="read rows")
    read_parser.add_argument("sheet")
    read_parser.add_argument("--id")
    read_parser.add_argument("--where", action="append", metavar="COLUMN=VALUE")
    read_parser.add_argument("--limit", type=int)
    read_parser.add_argument("--json", action="store_true")
    read_parser.set_defaults(func=cmd_read)

    nextid_parser = subparsers.add_parser("nextid", help="print the next free id")
    nextid_parser.add_argument("sheet")
    nextid_parser.set_defaults(func=cmd_nextid)

    create_parser = subparsers.add_parser("create", help="append a new row")
    create_parser.add_argument("sheet")
    create_parser.add_argument("--set", action="append", required=True, metavar="COLUMN=VALUE")
    create_parser.add_argument("--dry-run", action="store_true")
    create_parser.add_argument("--no-helper-formulas", action="store_true")
    create_parser.set_defaults(func=cmd_create)

    update_parser = subparsers.add_parser("update", help="change values on an existing row")
    update_parser.add_argument("sheet")
    update_parser.add_argument("--id", required=True)
    update_parser.add_argument("--set", action="append", required=True, metavar="COLUMN=VALUE")
    update_parser.add_argument("--dry-run", action="store_true")
    update_parser.set_defaults(func=cmd_update)

    batch_parser = subparsers.add_parser("batch", help="apply a JSON list of creates and updates in one pass")
    batch_parser.add_argument("file")
    batch_parser.add_argument("--dry-run", action="store_true")
    batch_parser.set_defaults(func=cmd_batch)

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
