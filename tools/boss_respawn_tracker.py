#!/usr/bin/env python3
import argparse
import json
import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path

DB_NAME = "AsperetaGoose.db"
BOSS_MIN_RESPAWN = 1500  # seconds, strictly longer than 25 minutes
BOSS_MAX_SPAWNS = 5  # strictly fewer spawn points

# Templates with zero spawn points never appear in the world, so they cannot be bosses.
QUERY = """
SELECT t.npc_id,
       t.npc_name,
       t.respawn_time,
       (SELECT GROUP_CONCAT(d.map_label, ', ')
        FROM (SELECT DISTINCT COALESCE(m.map_name, 'map ' || s2.map_id) AS map_label
              FROM npc_spawns s2
              LEFT JOIN maps m ON m.map_id = s2.map_id
              WHERE s2.npc_id = t.npc_id) d)
FROM npc_templates t
LEFT JOIN npc_spawns s ON s.npc_id = t.npc_id
WHERE t.respawn_time > ?
GROUP BY t.npc_id
HAVING COUNT(s.npc_id) BETWEEN 1 AND ? - 1
ORDER BY t.respawn_time DESC, t.npc_name
"""

TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Goose Boss Respawn Tracker</title>
<style>
  :root {
    --bg: #14171c; --panel: #1d222b; --border: #2c3441; --text: #d7dde6;
    --muted: #8b96a5; --green: #4ade80; --red: #f87171; --yellow: #facc15;
    --accent: #60a5fa;
  }
  * { box-sizing: border-box; }
  body { margin: 0; background: var(--bg); color: var(--text); font: 14px/1.45 system-ui, sans-serif; }
  header { padding: 16px 20px; border-bottom: 1px solid var(--border); display: flex; flex-wrap: wrap; gap: 12px; align-items: baseline; }
  h1 { font-size: 18px; margin: 0; }
  .muted { color: var(--muted); }
  main { padding: 16px 20px; }
  .toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 12px; }
  table { width: 100%; border-collapse: collapse; }
  th, td { padding: 8px 10px; border-bottom: 1px solid var(--border); text-align: left; vertical-align: top; }
  th { color: var(--muted); font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: .04em; position: sticky; top: 0; background: var(--bg); }
  .name { font-weight: 600; }
  .maps { color: var(--muted); font-size: 12px; }
  .early { color: var(--green); }
  .late { color: var(--red); }
  .t { font-variant-numeric: tabular-nums; }
  .badge { display: inline-block; padding: 1px 8px; border-radius: 10px; font-size: 12px; font-weight: 600; }
  .badge.wait { background: #263140; color: var(--accent); }
  .badge.window { background: #3a3315; color: var(--yellow); }
  .badge.back { background: #14351f; color: var(--green); }
  button { background: var(--panel); color: var(--text); border: 1px solid var(--border); border-radius: 6px; padding: 4px 10px; cursor: pointer; font-size: 13px; }
  button:hover { border-color: var(--accent); }
  input, textarea { background: var(--panel); color: var(--text); border: 1px solid var(--border); border-radius: 6px; padding: 4px 8px; font-size: 13px; }
  .killrow { display: flex; gap: 6px; flex-wrap: wrap; align-items: center; }
  footer { padding: 12px 20px; color: var(--muted); font-size: 12px; border-top: 1px solid var(--border); margin-top: 16px; }
</style>
</head>
<body>
<header>
  <h1>Goose Boss Respawn Tracker</h1>
  <span class="muted">generated __GENERATED_AT__ from AsperetaGoose.db &mdash; __BOSS_COUNT__ bosses</span>
</header>
<main>
  <div class="toolbar">
    <input id="filter" type="search" placeholder="Filter bosses&hellip;" style="min-width:200px">
    <button id="exportBtn">Export JSON</button>
    <input type="file" id="importFile" accept=".json,application/json">
    <textarea id="importPaste" placeholder="&hellip;or paste exported JSON here" rows="1" style="flex:1;min-width:200px"></textarea>
    <button id="importBtn">Import</button>
    <button id="clearAllBtn">Clear all</button>
  </div>
  <table>
    <thead><tr>
      <th>Boss</th><th>Respawn</th><th>Early &minus;15%</th><th>Late +15%</th><th>Status</th><th>Actions</th>
    </tr></thead>
    <tbody id="rows"></tbody>
  </table>
</main>
<footer>
  The server randomises each respawn between 85% and 115% of the base time (Goose/NPC.cs AddRespawnEvent):
  green is the earliest the boss can come back, red the latest. Kill times are stored in your browser
  (localStorage); use Export JSON to share them. All times shown in your local timezone.
</footer>
<script>
"use strict";
const BOSSES = __BOSSES_JSON__;
// Server rolls respawn uniformly in [0.85, 1.15] of the template time (NPC.cs AddRespawnEvent).
const EARLY_F = 0.85, LATE_F = 1.15;
const STORE_KEY = "gooseBossTracker.v1";

let kills = loadKills();
const rows = new Map();

function loadKills() {
  try {
    const raw = JSON.parse(localStorage.getItem(STORE_KEY));
    return raw && typeof raw === "object" && !Array.isArray(raw) ? raw : {};
  } catch { return {}; }
}
function saveKills() { localStorage.setItem(STORE_KEY, JSON.stringify(kills)); }

function fmtDur(ms) {
  if (!isFinite(ms) || ms < 0) ms = 0;
  const s = Math.round(ms / 1000);
  const h = Math.floor(s / 3600), m = Math.floor((s % 3600) / 60), sec = s % 60;
  if (h) return h + "h " + m + "m " + sec + "s";
  if (m) return m + "m " + sec + "s";
  return sec + "s";
}
function fmtDurShort(ms) {
  const s = Math.round(ms / 1000);
  const h = Math.floor(s / 3600), m = Math.round((s % 3600) / 60);
  if (h) return m ? h + "h " + m + "m" : h + "h";
  return m + "m";
}
function fmtTime(ms) {
  const d = new Date(ms);
  return d.toDateString() === new Date().toDateString() ? d.toLocaleTimeString() : d.toLocaleString();
}

function toLocalInputValue(iso) {
  const d = new Date(iso);
  const p = n => String(n).padStart(2, "0");
  return d.getFullYear() + "-" + p(d.getMonth() + 1) + "-" + p(d.getDate()) + "T" + p(d.getHours()) + ":" + p(d.getMinutes());
}
function nowLocalInputValue() { return toLocalInputValue(new Date().toISOString()); }

function buildRows() {
  const tbody = document.getElementById("rows");
  for (const b of BOSSES) {
    const tr = document.createElement("tr");
    tr.dataset.id = b.id;
    tr.innerHTML =
      '<td><div class="name"></div><div class="maps"></div></td>' +
      '<td class="t">' + fmtDurShort(b.respawn * 1000) + '</td>' +
      '<td class="early t">' + fmtDurShort(b.respawn * EARLY_F * 1000) + '</td>' +
      '<td class="late t">' + fmtDurShort(b.respawn * LATE_F * 1000) + '</td>' +
      '<td class="status"></td>' +
      '<td><div class="killrow">' +
      '<button class="killnow">Killed now</button>' +
      '<input type="datetime-local" class="killedat" title="Killed at">' +
      '<button class="setkill">Set</button>' +
      '<button class="clearkill">Clear</button>' +
      '</div></td>';
    tr.querySelector(".name").textContent = b.name;
    tr.querySelector(".maps").textContent = b.maps;
    const dtInput = tr.querySelector(".killedat");
    tr.querySelector(".killnow").onclick = () => setKill(b.id, new Date().toISOString());
    tr.querySelector(".setkill").onclick = () => {
      const v = dtInput.value;
      if (!v) { alert("Pick a date and time first."); return; }
      setKill(b.id, new Date(v).toISOString());
    };
    tr.querySelector(".clearkill").onclick = () => setKill(b.id, null);
    tbody.appendChild(tr);
    rows.set(b.id, { boss: b, tr, status: tr.querySelector(".status"), dtInput });
  }
}

function setKill(id, iso) {
  if (iso === null) delete kills[id]; else kills[id] = iso;
  saveKills();
  renderAll();
}

function sortRows() {
  const now = Date.now();
  const order = [...rows.values()].map(r => {
    const iso = kills[r.boss.id];
    const R = r.boss.respawn * 1000;
    if (iso) {
      const killed = new Date(iso).getTime();
      if (now < killed + R * LATE_F) return { tr: r.tr, g: 0, k: killed + R, n: r.boss.name };
    }
    return { tr: r.tr, g: 1, k: r.boss.respawn, n: r.boss.name };
  });
  order.sort((a, b) => a.g - b.g || a.k - b.k || a.n.localeCompare(b.n));
  const tbody = document.getElementById("rows");
  const desired = order.map(o => o.tr);
  if (desired.map(t => t.dataset.id).join(",") !== [...tbody.children].map(t => t.dataset.id).join(","))
    for (const tr of desired) tbody.appendChild(tr);
}

function renderAll() {
  const q = document.getElementById("filter").value.trim().toLowerCase();
  for (const r of rows.values()) {
    r.tr.style.display = (!q || r.boss.name.toLowerCase().includes(q)) ? "" : "none";
    r.dtInput.value = kills[r.boss.id] ? toLocalInputValue(kills[r.boss.id]) : nowLocalInputValue();
    updateStatus(r);
  }
  sortRows();
}

function updateStatus(r) {
  const el = r.status;
  const iso = kills[r.boss.id];
  if (!iso) { el.innerHTML = '<span class="muted">no kill recorded</span>'; return; }
  const killed = new Date(iso).getTime();
  const R = r.boss.respawn * 1000;
  const early = killed + R * EARLY_F, nominal = killed + R, late = killed + R * LATE_F;
  const now = Date.now();
  let badge, note;
  if (now < early) {
    badge = '<span class="badge wait">RESPAWNING</span>';
    note = "earliest in " + fmtDur(early - now);
  } else if (now < late) {
    badge = '<span class="badge window">IN WINDOW</span>';
    note = now < nominal ? "nominal in " + fmtDur(nominal - now) : "late in " + fmtDur(late - now);
  } else {
    badge = '<span class="badge back">RESPAWNED</span>';
    note = "should be back";
  }
  el.innerHTML = badge + ' <span class="muted">' + note + '</span><br>' +
    'killed <span class="t">' + fmtTime(killed) + '</span><br>' +
    '<span class="early t">' + fmtTime(early) + '</span> &ndash; <span class="late t">' + fmtTime(late) + '</span>';
}

function exportJson() {
  const out = { version: 1, exportedAt: new Date().toISOString(), kills: {} };
  for (const [id, iso] of Object.entries(kills)) {
    const b = BOSSES.find(x => x.id === Number(id));
    out.kills[id] = { name: b ? b.name : "?", killedAt: iso };
  }
  const blob = new Blob([JSON.stringify(out, null, 2)], { type: "application/json" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = "goose-boss-kill-times.json";
  a.click();
  URL.revokeObjectURL(a.href);
}

function importJson(text, source) {
  let data;
  try { data = JSON.parse(text); } catch (e) { alert("Import failed: not valid JSON (" + e.message + ")"); return; }
  const src = data && typeof data === "object" && data.kills && typeof data.kills === "object" ? data.kills : data;
  if (!src || typeof src !== "object" || Array.isArray(src)) { alert("Import failed: expected {kills: {…}} or {id: killedAt}."); return; }
  let n = 0;
  for (const [id, v] of Object.entries(src)) {
    const idN = Number(id);
    if (!BOSSES.some(b => b.id === idN)) continue;
    const iso = typeof v === "string" ? v : (v && v.killedAt);
    if (!iso || isNaN(new Date(iso).getTime())) continue;
    kills[idN] = new Date(iso).toISOString();
    n++;
  }
  saveKills();
  renderAll();
  alert("Imported " + n + " kill time" + (n === 1 ? "" : "s") + " from " + source + ".");
}

document.getElementById("exportBtn").onclick = exportJson;
document.getElementById("importFile").onchange = e => {
  const f = e.target.files[0];
  if (!f) return;
  const r = new FileReader();
  r.onload = () => importJson(r.result, f.name);
  r.readAsText(f);
  e.target.value = "";
};
document.getElementById("importBtn").onclick = () => {
  const t = document.getElementById("importPaste").value;
  if (!t.trim()) return;
  importJson(t, "pasted text");
  document.getElementById("importPaste").value = "";
};
document.getElementById("clearAllBtn").onclick = () => {
  if (!Object.keys(kills).length) return;
  if (!confirm("Clear all recorded kill times?")) return;
  kills = {};
  saveKills();
  renderAll();
};
document.getElementById("filter").oninput = renderAll;

buildRows();
renderAll();
setInterval(() => { for (const r of rows.values()) updateStatus(r); sortRows(); }, 1000);
</script>
</body>
</html>
"""


def find_db(explicit):
    if explicit:
        p = Path(explicit)
        if not p.is_file():
            sys.exit(f"error: database not found: {p}")
        return p.resolve()
    here = Path(__file__).resolve().parent
    candidates = [
        here.parent / "Goose" / "bin" / "Debug" / DB_NAME,
        here.parent / "bin" / "Debug" / DB_NAME,
        Path.cwd() / "Goose" / "bin" / "Debug" / DB_NAME,
        Path.cwd() / "bin" / "Debug" / DB_NAME,
    ]
    for c in candidates:
        if c.is_file():
            return c.resolve()
    matches = [m for m in here.parent.rglob(DB_NAME) if "obj" not in m.parts]
    if matches:
        return sorted(matches)[0]
    sys.exit(f"error: could not find {DB_NAME}; pass --db PATH")


def main():
    ap = argparse.ArgumentParser(description="Generate a boss respawn tracker webpage from AsperetaGoose.db")
    ap.add_argument("--db", help="path to AsperetaGoose.db (default: search the repo)")
    ap.add_argument("--out", default="boss_tracker.html", help="output HTML file (default: ./boss_tracker.html)")
    args = ap.parse_args()

    db = find_db(args.db)
    con = sqlite3.connect(f"file:{db}?mode=ro", uri=True)
    try:
        raw = con.execute(QUERY, (BOSS_MIN_RESPAWN, BOSS_MAX_SPAWNS)).fetchall()
    finally:
        con.close()
    if not raw:
        sys.exit(f"error: no bosses found in {db}")

    bosses = [{"id": r[0], "name": r[1], "respawn": r[2], "maps": r[3] or ""} for r in raw]
    payload = json.dumps(bosses, ensure_ascii=False).replace("</", "<\\/")
    html = (
        TEMPLATE.replace("__BOSSES_JSON__", payload)
        .replace("__GENERATED_AT__", datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"))
        .replace("__BOSS_COUNT__", str(len(bosses)))
    )
    out = Path(args.out)
    out.write_text(html, encoding="utf-8")
    print(f"database: {db}")
    print(f"bosses:   {len(bosses)}")
    print(f"written:  {out.resolve()}")


if __name__ == "__main__":
    main()
