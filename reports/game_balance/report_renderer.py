from __future__ import annotations

import html
import json
import math
import re
from collections import Counter, defaultdict
from dataclasses import asdict
from statistics import median
from typing import Any, Iterable

MAX_LEVEL = 50
PLAYER_CLASS_IDS = (2, 3, 4, 5)
SLOT_NAMES = {
    0: "Helmet",
    1: "Shield",
    2: "One-handed",
    3: "Two-handed",
    4: "Ring",
    5: "Necklace",
    6: "Pauldrons",
    7: "Cloak",
    8: "Belt",
    9: "Gloves",
    10: "Chest",
    11: "Pants",
    12: "Shoes",
    13: "Mount",
    20: "Misc",
}
REWARD_NAMES = {
    0: "gold",
    1: "item",
    2: "title",
    3: "surname",
    4: "teleport",
    5: "raw XP",
    6: "face",
    7: "body",
    8: "hair",
    9: "hair colour",
    10: "body colour",
    11: "class change",
    12: "HP",
    13: "MP",
    14: "AC",
    15: "STA",
    16: "STR",
    17: "DEX",
    18: "INT",
    19: "spell buff",
    20: "learn spell",
    21: "script",
}
REQUIREMENT_NAMES = {
    0: "gold",
    1: "item",
    2: "kill",
    3: "talk",
    4: "banked XP",
    5: "sold XP",
    6: "nothing equipped",
    7: "script",
}


def e(value: Any) -> str:
    return html.escape(str(value), quote=True)


def n(value: float | int | None, digits: int = 1) -> str:
    if value is None:
        return "—"
    if isinstance(value, int):
        return f"{value:,}"
    rounded = round(value, digits)
    return f"{rounded:,.{digits}f}"


def family_name(name: str) -> str:
    return re.sub(r"\s+\d+$", "", name).strip()


def rank_number(name: str) -> int | None:
    match = re.search(r"\s+(\d+)$", name)
    return int(match.group(1)) if match else None


def allowed_classes(mask: Any, classes: dict[int, dict[str, Any]]) -> tuple[int, ...]:
    value = int(mask)
    return tuple(class_id for class_id in PLAYER_CLASS_IDS if value == 0 or value & (1 << class_id))


def class_label(mask: Any, classes: dict[int, dict[str, Any]]) -> str:
    value = int(mask)
    if value == 0:
        return "All"
    ids = tuple(class_id for class_id in range(1, 6) if class_id in classes and value & (1 << class_id))
    return ", ".join(str(classes[class_id]["class_name"]) for class_id in ids) or "No normal class"


def item_metrics(item: dict[str, Any]) -> dict[str, float]:
    hp = int(item["player_hp"])
    mp = int(item["player_mp"])
    sp = int(item["player_sp"])
    ac = int(item["stat_ac"])
    strength = int(item["stat_str"])
    stamina = int(item["stat_sta"])
    dexterity = int(item["stat_dex"])
    intelligence = int(item["stat_int"])
    resistance = sum(int(item[key]) for key in ("res_fire", "res_water", "res_spirit", "res_air", "res_earth"))
    delay = int(item["weapon_delay"])
    throughput = 10 * (int(item["weapon_damage"]) + strength) / delay if delay > 0 else 0.0
    return {
        "effective_hp": hp + 25 * stamina,
        "effective_mp": mp + 25 * intelligence,
        "dodge_pct_points": min(50.0, dexterity / 100),
        "throughput": throughput,
        "budget": hp / 25 + mp / 25 + sp / 25 + ac / 5 + strength + stamina + dexterity + intelligence + resistance / 2,
    }


def source_text(data: Any, item_id: int, eligible_only: bool = False) -> str:
    parts = []
    for source in data.item_sources.get(item_id, ()):
        if eligible_only and not source.eligible:
            continue
        if source.source_kind in {"drop", "vendor"} and source.npc_id is not None:
            name = data.npcs[source.npc_id]["npc_name"]
            detail = f"{source.source_kind}: {name} #{source.npc_id}"
            if source.drop_rate is not None:
                detail += f" ({n(source.drop_rate)}%)"
        elif source.source_kind == "quest" and source.quest_id is not None:
            detail = f"quest: {data.quests[source.quest_id]['name']} Q{source.quest_id}"
        elif source.source_kind == "craft" and source.combination_id is not None:
            detail = f"craft: {data.combinations[source.combination_id]['combination_name']} C{source.combination_id}"
        else:
            detail = source.source_kind
        if source.effective_level is not None:
            detail += f" @L{source.effective_level}"
        if not source.eligible:
            detail += f" [{source.exclusion_reason}]"
        parts.append(detail)
    return "; ".join(parts) or "no modeled source"


def render_table(table_id: str, title: str, columns: list[str], rows: Iterable[Iterable[Any]], note: str = "") -> str:
    body = []
    for row in rows:
        body.append("<tr>" + "".join(f"<td>{e(cell)}</td>" for cell in row) + "</tr>")
    note_html = f"<p class=table-note>{e(note)}</p>" if note else ""
    head = "".join(f"<th tabindex=0>{e(column)}<span aria-hidden=true>↕</span></th>" for column in columns)
    return f"<section class=table-card><h3>{e(title)}</h3>{note_html}<div class=table-wrap><table id={e(table_id)} class=sortable><thead><tr>{head}</tr></thead><tbody>{''.join(body)}</tbody></table></div></section>"


def line_chart(chart_id: str, title: str, series: list[tuple[str, list[tuple[int, float]], str]], y_label: str) -> str:
    width, height = 900, 330
    left, right, top, bottom = 72, 25, 32, 54
    points = [point for _, values, _ in series for point in values]
    max_x = max((x for x, _ in points), default=1)
    min_x = min((x for x, _ in points), default=1)
    max_y = max((y for _, y in points), default=1)
    max_y = max(max_y, 1)
    plot_w, plot_h = width - left - right, height - top - bottom
    sx = lambda x: left + (x - min_x) / max(1, max_x - min_x) * plot_w
    sy = lambda y: top + plot_h - y / max_y * plot_h
    grid = []
    for step in range(6):
        value = max_y * step / 5
        y = sy(value)
        grid.append(f"<line x1='{left}' y1='{y:.2f}' x2='{width-right}' y2='{y:.2f}' class='grid'/><text x='{left-8}' y='{y+4:.2f}' text-anchor='end'>{e(n(value, 0))}</text>")
    lines = []
    legend = []
    for index, (label, values, color) in enumerate(series):
        coords = " ".join(f"{sx(x):.2f},{sy(y):.2f}" for x, y in values)
        lines.append(f"<polyline points='{coords}' fill='none' stroke='{color}' stroke-width='3'/>")
        lines.extend(f"<circle cx='{sx(x):.2f}' cy='{sy(y):.2f}' r='3' fill='{color}'><title>{e(label)} L{x}: {e(n(y))}</title></circle>" for x, y in values)
        legend.append(f"<g transform='translate({left + index * 190},12)'><rect width='14' height='4' y='4' fill='{color}'/><text x='20' y='10'>{e(label)}</text></g>")
    ticks = []
    start = int(math.ceil(min_x / 5) * 5)
    for x in range(start, int(max_x) + 1, 5):
        ticks.append(f"<text x='{sx(x):.2f}' y='{height-25}' text-anchor='middle'>L{x}</text>")
    return f"<figure class=chart-card><figcaption>{e(title)}</figcaption><svg id={e(chart_id)} class=chart viewBox='0 0 {width} {height}' role=img aria-label='{e(title)}'><title>{e(title)}</title>{''.join(grid)}{''.join(lines)}{''.join(legend)}{''.join(ticks)}<text transform='translate(16,{top+plot_h/2}) rotate(-90)' text-anchor='middle'>{e(y_label)}</text></svg></figure>"


def bar_chart(chart_id: str, title: str, values: list[tuple[str, float]], y_label: str) -> str:
    width, height = 900, 330
    left, right, top, bottom = 72, 25, 32, 78
    max_y = max((value for _, value in values), default=1)
    max_y = max(max_y, 1)
    plot_w, plot_h = width - left - right, height - top - bottom
    gap = plot_w / max(1, len(values))
    bars = []
    for index, (label, value) in enumerate(values):
        bar_h = value / max_y * plot_h
        x = left + index * gap + gap * 0.14
        y = top + plot_h - bar_h
        bars.append(f"<rect x='{x:.2f}' y='{y:.2f}' width='{gap*0.72:.2f}' height='{bar_h:.2f}' rx='3'><title>{e(label)}: {e(n(value))}</title></rect>")
        bars.append(f"<text x='{x+gap*0.36:.2f}' y='{height-50}' text-anchor='end' transform='rotate(-35 {x+gap*0.36:.2f} {height-50})'>{e(label)}</text>")
    return f"<figure class=chart-card><figcaption>{e(title)}</figcaption><svg id={e(chart_id)} class=chart viewBox='0 0 {width} {height}' role=img aria-label='{e(title)}'><title>{e(title)}</title><line x1='{left}' y1='{top+plot_h}' x2='{width-right}' y2='{top+plot_h}' class='axis'/>{''.join(bars)}<text transform='translate(16,{top+plot_h/2}) rotate(-90)' text-anchor='middle'>{e(y_label)}</text></svg></figure>"


def quest_effort(data: Any, quest_id: int) -> tuple[str, float | None, int]:
    descriptions = []
    expected_kills = 0.0
    gold_input = 0
    for requirement in data.quest_requirements.get(quest_id, ()):
        kind = int(requirement["requirement_type"])
        target_id = int(requirement["requirement_value"])
        amount = int(requirement["requirement_value2"])
        if kind == 1 and target_id in data.items:
            item = data.items[target_id]
            descriptions.append(f"{amount}× {item['item_name']} #{target_id}")
            rates = [source.drop_rate for source in data.item_sources.get(target_id, ()) if source.eligible and source.source_kind == "drop" and source.drop_rate and source.drop_rate > 0]
            if rates:
                expected_kills += amount * 100 / max(rates)
            vendor_sources = [source for source in data.item_sources.get(target_id, ()) if source.eligible and source.source_kind == "vendor"]
            if vendor_sources:
                gold_input += amount * int(item["item_value"])
        elif kind in {2, 3} and target_id in data.npcs:
            verb = "kill" if kind == 2 else "talk to"
            descriptions.append(f"{verb} {amount or 1}× {data.npcs[target_id]['npc_name']} #{target_id}")
            if kind == 2:
                expected_kills += amount
        else:
            descriptions.append(f"{REQUIREMENT_NAMES.get(kind, 'type '+str(kind))}: {target_id} / {amount}")
    return "; ".join(descriptions) or "none", expected_kills or None, gold_input


def reward_text(data: Any, quest_id: int) -> str:
    parts = []
    for reward in data.quest_rewards.get(quest_id, ()):
        kind = int(reward["reward_type"])
        value = int(reward["long_value"])
        amount = int(reward["long_value2"])
        if kind == 1 and value in data.items:
            parts.append(f"{amount}× {data.items[value]['item_name']} #{value}")
        elif kind == 20 and value in data.spells:
            parts.append(f"learn {data.spells[value]['spell_name']} #{value}")
        else:
            parts.append(f"{n(value)} {REWARD_NAMES.get(kind, 'type '+str(kind))}")
    return "; ".join(parts) or "none"


def spell_magnitude(effect: dict[str, Any]) -> tuple[float | None, str, dict[str, float]]:
    formula_fields = (
        ("HP formula", str(effect["hp_change_formula"]).strip()),
        ("MP formula", str(effect["mp_change_formula"]).strip()),
        ("SP formula", str(effect["sp_change_formula"]).strip()),
    )
    active_formulas = [(label, value) for label, value in formula_fields if value not in {"", "0", "0.0"}]
    if active_formulas:
        if len(active_formulas) != 1:
            return None, "N/A — multiple formula channels", {}
        label, value = active_formulas[0]
        if label == "SP formula":
            return None, "N/A — SP formula is not evaluated by runtime", {}
        if re.fullmatch(r"[-+]?\d+(?:\.\d+)?", value):
            magnitude = abs(float(value))
            basis = f"{label} absolute constant"
            return magnitude, basis, {basis: magnitude}
        linear = re.fullmatch(r"([-+]?\d+(?:\.\d+)?)\s*\*\s*(.+)", value)
        if linear:
            magnitude = abs(float(linear.group(1)))
            expression = re.sub(r"\s+", "", linear.group(2))
            basis = f"{label} coefficient × {expression}"
            return magnitude, basis, {basis: magnitude}
        return None, f"N/A — nonconstant {label.lower()}", {}
    fields = (
        ("HP", "hp"),
        ("MP", "mp"),
        ("SP", "sp"),
        ("AC", "stat_ac"),
        ("STR", "stat_str"),
        ("STA", "stat_sta"),
        ("DEX", "stat_dex"),
        ("INT", "stat_int"),
        ("haste", "haste"),
        ("spell damage", "spell_damage"),
        ("spell crit", "spell_crit"),
        ("melee damage", "melee_damage"),
        ("melee crit", "melee_crit"),
        ("damage reduction", "damage_reduce"),
    )
    components = {label: abs(float(effect[key])) for label, key in fields if float(effect[key]) != 0}
    if len(components) == 1:
        label, magnitude = next(iter(components.items()))
        return magnitude, f"{label} direct magnitude", components
    if components:
        vector = ", ".join(f"{label}={n(value, 2)}" for label, value in components.items())
        return None, f"multi-stat vector ({vector})", components
    if str(effect["script_path"]):
        return None, "N/A — scripted effect without comparable numeric fields", {}
    return None, "N/A — utility or no numeric magnitude", {}


def spell_rows(data: Any) -> tuple[list[dict[str, Any]], dict[int, list[tuple[str, int]]]]:
    grants: defaultdict[int, list[tuple[str, int]]] = defaultdict(list)
    for grant in data.class_spell_grants:
        if grant.eligible:
            grants[grant.spell_id].append((f"automatic {data.classes[grant.class_id]['class_name']}", grant.level))
    for grant in data.scroll_spell_grants:
        if grant.eligible and grant.item_id in data.effective_item_levels:
            grants[grant.spell_id].append((f"scroll item #{grant.item_id}", max(5, data.effective_item_levels[grant.item_id])))
    for grant in data.quest_spell_grants:
        if grant.eligible and grant.quest_id in data.effective_quest_levels:
            grants[grant.spell_id].append((f"quest Q{grant.quest_id}", max(5, data.effective_quest_levels[grant.quest_id])))
    rows = []
    for spell_id in sorted(data.spells):
        spell = data.spells[spell_id]
        effect = data.spell_effects[int(spell["spell_effect_id"])]
        eligible_grants = sorted(grants.get(spell_id, ()), key=lambda entry: (entry[1], entry[0]))
        all_grants = [grant for grant in (*data.class_spell_grants, *data.scroll_spell_grants, *data.quest_spell_grants) if grant.spell_id == spell_id]
        nominal_levels = []
        for grant in all_grants:
            if grant.item_id is not None:
                nominal_levels.append(max(1, int(data.items[grant.item_id]["min_level"])))
            elif grant.quest_id is not None:
                nominal_levels.append(max(1, int(data.quests[grant.quest_id]["min_level"])))
            else:
                nominal_levels.append(grant.level)
        nominal = min(nominal_levels, default=None)
        practical = min((level for _, level in eligible_grants), default=None)
        class_ids = allowed_classes(spell["class_restrictions"], data.classes)
        cost_pcts = []
        if practical is not None:
            for class_id in class_ids:
                level = min(MAX_LEVEL, max(1, practical))
                baseline = data.class_info.get((class_id, level))
                if not baseline:
                    continue
                hp = int(baseline["player_hp"])
                mp = int(baseline["player_mp"])
                static_hp = int(spell["hp_static_cost"])
                static_mp = int(spell["mp_static_cost"])
                pct_hp = float(spell["hp_percent_cost"])
                pct_mp = float(spell["mp_percent_cost"])
                components = []
                if hp > 0 and (static_hp or pct_hp):
                    components.append((static_hp + int(max(0, hp - static_hp) * pct_hp / 100)) / hp * 100)
                if mp > 0 and (static_mp or pct_mp):
                    components.append((static_mp + int(max(0, mp - static_mp) * pct_mp / 100)) / mp * 100)
                if components:
                    cost_pcts.append(max(components))
        magnitude, magnitude_basis, magnitude_components = spell_magnitude(effect)
        buff_values = [f"{key}={effect[key]}" for key in ("hp", "mp", "sp", "stat_ac", "stat_str", "stat_sta", "stat_dex", "stat_int", "haste", "spell_damage", "melee_damage", "damage_reduce") if float(effect[key]) != 0]
        formulas = "; ".join(value for value in (str(effect["hp_change_formula"]), str(effect["mp_change_formula"]), str(effect["sp_change_formula"])) if value not in {"", "0"})
        rows.append({
            "id": spell_id,
            "name": str(spell["spell_name"]),
            "family": family_name(str(spell["spell_name"])),
            "rank": rank_number(str(spell["spell_name"])),
            "classes": class_label(spell["class_restrictions"], data.classes),
            "nominal": nominal,
            "practical": practical,
            "sources": "; ".join(f"{kind} @L{level}" for kind, level in eligible_grants) or "no eligible modeled source",
            "cooldown": float(spell["spell_aether"]) / 1000,
            "cost_pct": max(cost_pcts) if cost_pcts else None,
            "costs": f"HP {spell['hp_static_cost']}+{spell['hp_percent_cost']}%; MP {spell['mp_static_cost']}+{spell['mp_percent_cost']}%; SP {spell['sp_static_cost']}+{spell['sp_percent_cost']}%",
            "target": f"type {effect['target_type']}; size {effect['target_size']}",
            "magnitude": magnitude,
            "magnitude_basis": magnitude_basis,
            "magnitude_components": magnitude_components,
            "growth": "N/A — first rank or unranked",
            "effect": f"type {effect['effect_type']}; duration {effect['effect_duration']}; {formulas or ', '.join(buff_values) or 'script/utility'}",
            "damage_scaled": str(effect["spell_damage_effects"]) != "0",
        })
    families: defaultdict[str, list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        families[row["family"]].append(row)
    for family_rows in families.values():
        ranked_rows = sorted(family_rows, key=lambda row: (row["rank"] if row["rank"] is not None else 0, row["id"]))
        for prior, current in zip(ranked_rows, ranked_rows[1:]):
            rank_label = f"rank {prior['rank']}" if prior["rank"] is not None else prior["name"]
            if prior["magnitude"] is not None and current["magnitude"] is not None and prior["magnitude_basis"] == current["magnitude_basis"]:
                delta = current["magnitude"] - prior["magnitude"]
                ratio = current["magnitude"] / prior["magnitude"] if prior["magnitude"] != 0 else None
                current["growth"] = f"prior→current Δ {delta:+.2f}; ratio {ratio:.3f}× vs {rank_label}" if ratio is not None else f"prior→current Δ {delta:+.2f}; ratio N/A vs {rank_label}"
            elif prior["magnitude_components"] and set(prior["magnitude_components"]) == set(current["magnitude_components"]):
                changes = []
                for component in prior["magnitude_components"]:
                    previous = prior["magnitude_components"][component]
                    value = current["magnitude_components"][component]
                    ratio = value / previous if previous != 0 else None
                    ratio_text = f"{ratio:.3f}×" if ratio is not None else "ratio N/A"
                    changes.append(f"{component} Δ {value - previous:+.2f}, {ratio_text}")
                current["growth"] = f"prior→current vector vs {rank_label}: " + "; ".join(changes)
            elif prior["magnitude"] is None or current["magnitude"] is None:
                current["growth"] = f"N/A — scripted/incomparable ({prior['magnitude_basis']} → {current['magnitude_basis']})"
            else:
                current["growth"] = f"N/A — magnitude basis changed ({prior['magnitude_basis']} → {current['magnitude_basis']})"
    return rows, grants


def render_report(data: Any, summary: dict[str, Any]) -> str:
    gear_ids = sorted(item_id for item_id, item in data.items.items() if int(item["item_usetype"]) in {2, 3} and int(item["min_experience"]) == 0 and int(item["min_level"]) <= MAX_LEVEL)
    included_gear = [item_id for item_id in gear_ids if item_id in data.included_item_ids]
    excluded_gear = [item_id for item_id in gear_ids if item_id not in data.included_item_ids]
    item_rows = []
    for item_id in included_gear:
        item = data.items[item_id]
        metrics = item_metrics(item)
        item_rows.append({
            "id": item_id,
            "name": item["item_name"],
            "use": "Weapon" if int(item["item_usetype"]) == 3 else "Armor",
            "slot": SLOT_NAMES.get(int(item["item_slot"]), str(item["item_slot"])),
            "classes": class_label(item["class_restrictions"], data.classes),
            "authored": int(item["min_level"]),
            "effective": data.effective_item_levels[item_id],
            "stats": f"HP {item['player_hp']}; MP {item['player_mp']}; SP {item['player_sp']}; AC {item['stat_ac']}; STR {item['stat_str']}; STA {item['stat_sta']}; DEX {item['stat_dex']}; INT {item['stat_int']}; res F/W/S/A/E {item['res_fire']}/{item['res_water']}/{item['res_spirit']}/{item['res_air']}/{item['res_earth']}; DMG {item['weapon_damage']}; delay {item['weapon_delay']}",
            "metrics": metrics,
            "sources": source_text(data, item_id, True),
        })
    slot_rows = []
    gap_rows = []
    class_slot_rows = []
    for slot_id in range(14):
        slot_name = SLOT_NAMES[slot_id]
        ids = [item_id for item_id in included_gear if int(data.items[item_id]["item_slot"]) == slot_id]
        levels = sorted({data.effective_item_levels[item_id] for item_id in ids})
        intervals = [b - a for a, b in zip(levels, levels[1:])]
        longest = max(intervals, default=None)
        note = ""
        if not ids:
            note = "No sourced item"
        elif levels[0] == 50:
            note = "No pre-50 coverage"
        elif levels[-1] < 40:
            note = "Late-game desert"
        elif longest is not None and longest >= 15:
            note = "Long upgrade interval"
        slot_rows.append((slot_name, len(ids), n(levels[0] if levels else None), n(levels[-1] if levels else None), n(longest), ", ".join(str(level) for level in levels) or "—", note or "coverage present"))
        if not levels:
            gap_rows.append(("Critical", "All", slot_name, "L1–50", "No modeled source", "Add a baseline source before L15"))
        elif levels[0] >= 20:
            gap_rows.append(("High", "All", slot_name, f"L1–{levels[0]-1}", f"First source L{levels[0]}", "Add an early quest/vendor baseline"))
        for before, after in zip(levels, levels[1:]):
            if after - before >= 15:
                gap_rows.append(("High" if after - before >= 25 else "Medium", "All", slot_name, f"L{before+1}–{after-1}", f"Frontiers L{before} → L{after}", f"Target a meaningful upgrade near L{round((before+after)/2)}"))
        for class_id in PLAYER_CLASS_IDS:
            class_name = data.classes[class_id]["class_name"]
            usable = [item_id for item_id in ids if class_id in allowed_classes(data.items[item_id]["class_restrictions"], data.classes)]
            usable_levels = sorted({data.effective_item_levels[item_id] for item_id in usable})
            class_slot_rows.append((class_name, slot_name, len(usable), n(usable_levels[0] if usable_levels else None), n(usable_levels[-1] if usable_levels else None), ", ".join(map(str, usable_levels)) or "—"))
    for start in range(1, 50, 5):
        end = min(49, start + 4)
        count = sum(start <= data.effective_item_levels[item_id] <= end for item_id in included_gear)
        if count < 7:
            gap_rows.append(("Medium", "All", "All slots", f"L{start}–{end}", f"Only {count} new sourced templates in five-level band", "Fill class/slot holes before raising the global frontier"))
    frontier_rows = []
    frontier_ids = set()
    class_frontiers: defaultdict[tuple[int, int], list[tuple[int, str]]] = defaultdict(list)
    for class_id in PLAYER_CLASS_IDS:
        for slot_id in range(14):
            candidates = [row for row in item_rows if int(data.items[row["id"]]["item_slot"]) == slot_id and class_id in allowed_classes(data.items[row["id"]]["class_restrictions"], data.classes)]
            best = -1.0
            for effective_level in sorted({row["effective"] for row in candidates}):
                level_rows = [row for row in candidates if row["effective"] == effective_level]
                row = max(level_rows, key=lambda candidate: (candidate["metrics"]["throughput"] if candidate["use"] == "Weapon" else candidate["metrics"]["budget"], -candidate["id"]))
                score = row["metrics"]["throughput"] if row["use"] == "Weapon" else row["metrics"]["budget"]
                if score > best * 1.05:
                    frontier_ids.add(row["id"])
                    class_frontiers[(class_id, slot_id)].append((row["effective"], f"{row['name']} #{row['id']}"))
                    frontier_rows.append((data.classes[class_id]["class_name"], row["slot"], row["effective"], f"{row['name']} #{row['id']}", n(score, 2), "throughput" if row["use"] == "Weapon" else "budget"))
                    best = score
    for class_id in PLAYER_CLASS_IDS:
        class_name = str(data.classes[class_id]["class_name"])
        for slot_id in range(14):
            frontiers = class_frontiers.get((class_id, slot_id), [])
            if not frontiers:
                gap_rows.append(("High", class_name, SLOT_NAMES[slot_id], "L1–50", "No meaningful same-class/slot frontier", "Add a conservative baseline source"))
                continue
            for (before_level, before_name), (after_level, after_name) in zip(frontiers, frontiers[1:]):
                if after_level - before_level >= 10:
                    gap_rows.append(("High" if after_level - before_level >= 20 else "Medium", class_name, SLOT_NAMES[slot_id], f"L{before_level+1}–{after_level-1}", f"Meaningful frontiers {before_name} L{before_level} → {after_name} L{after_level}", f"Interpolate near L{round((before_level+after_level)/2)}"))
    for row in item_rows:
        row["frontier"] = "yes" if row["id"] in frontier_ids else "no"
    included_item_table_rows = []
    for item_id in sorted(data.included_item_ids):
        item = data.items[item_id]
        metrics = item_metrics(item)
        stats = f"HP {item['player_hp']}; MP {item['player_mp']}; SP {item['player_sp']}; AC {item['stat_ac']}; STR {item['stat_str']}; STA {item['stat_sta']}; DEX {item['stat_dex']}; INT {item['stat_int']}; res F/W/S/A/E {item['res_fire']}/{item['res_water']}/{item['res_spirit']}/{item['res_air']}/{item['res_earth']}; DMG {item['weapon_damage']}; delay {item['weapon_delay']}"
        included_item_table_rows.append((item_id, item["item_name"], item["item_usetype"], SLOT_NAMES.get(int(item["item_slot"]), item["item_slot"]), item["item_type"], class_label(item["class_restrictions"], data.classes), item["min_level"], data.effective_item_levels[item_id], stats, n(metrics["effective_hp"]), n(metrics["effective_mp"]), n(metrics["dodge_pct_points"], 2), n(metrics["throughput"], 2), n(metrics["budget"], 2), "yes" if item_id in frontier_ids else "no", source_text(data, item_id, True)))
    level_item_counts = Counter(row["effective"] for row in item_rows)
    cumulative_weapon = []
    cumulative_armor = []
    best_weapon = 0.0
    best_armor = 0.0
    for level in range(1, 51):
        best_weapon = max([best_weapon] + [row["metrics"]["throughput"] for row in item_rows if row["use"] == "Weapon" and row["effective"] == level])
        best_armor = max([best_armor] + [row["metrics"]["budget"] for row in item_rows if row["use"] == "Armor" and row["effective"] == level])
        cumulative_weapon.append((level, best_weapon))
        cumulative_armor.append((level, best_armor))
    npc_ids = sorted(data.leveling_npc_ids | data.endgame_npc_ids)
    npc_level_rows = []
    for level in sorted({data.npc_metrics[npc_id].level for npc_id in npc_ids}):
        leveling = [data.npc_metrics[npc_id] for npc_id in data.leveling_npc_ids if data.npc_metrics[npc_id].level == level]
        gated = [data.npc_metrics[npc_id] for npc_id in data.endgame_npc_ids if data.npc_metrics[npc_id].level == level]
        for regime, values in (("leveling/transition", leveling), ("XP-gated endgame", gated)):
            if values:
                npc_level_rows.append((level, regime, len(values), n(median(metric.hp for metric in values), 0), n(median(metric.ac for metric in values), 0), n(median(int(data.npcs[metric.npc_id]["experience"]) for metric in values), 0), n(median(metric.xp_per_hp for metric in values if metric.xp_per_hp is not None), 3), n(median(metric.pressure for metric in values), 1)))
    npc_outliers = []
    for npc_id in sorted(data.leveling_npc_ids):
        metric = data.npc_metrics[npc_id]
        peers = [data.npc_metrics[peer_id] for peer_id in data.leveling_npc_ids if data.npc_metrics[peer_id].level == metric.level]
        median_hp = median(peer.hp for peer in peers)
        median_xp = median(int(data.npcs[peer.npc_id]["experience"]) for peer in peers)
        hp_ratio = metric.hp / median_hp if median_hp else 0
        xp_ratio = int(data.npcs[npc_id]["experience"]) / median_xp if median_xp else 0
        if hp_ratio >= 1.8 or hp_ratio <= 0.6 or xp_ratio >= 1.8 or xp_ratio <= 0.6 or npc_id in {20, 24, 28, 40, 169}:
            npc_outliers.append((npc_id, data.npcs[npc_id]["npc_name"], metric.level, n(metric.hp), n(metric.ac), n(data.npcs[npc_id]["experience"]), n(hp_ratio, 2), n(xp_ratio, 2), n(metric.xp_per_hp, 3)))
    pre50_levels = sorted({data.npc_metrics[npc_id].level for npc_id in data.leveling_npc_ids if data.npc_metrics[npc_id].level < 50})
    missing_levels = sorted(set(range(1, 50)) - set(pre50_levels))
    npc_hp_series = [(level, median(data.npc_metrics[npc_id].hp for npc_id in data.leveling_npc_ids if data.npc_metrics[npc_id].level == level)) for level in pre50_levels]
    npc_xp_efficiency = [(level, median(data.npc_metrics[npc_id].xp_per_hp for npc_id in data.leveling_npc_ids if data.npc_metrics[npc_id].level == level and data.npc_metrics[npc_id].xp_per_hp is not None)) for level in pre50_levels]
    one_time_quests = [quest_id for quest_id, quest in data.quests.items() if not (str(quest["repeatable"]) != "0")]
    thresholds = {level: int(row["level_up_exp"]) for (class_id, level), row in data.class_info.items() if class_id == 2}
    quest_rows = []
    quest_xp_by_level = Counter()
    quest_xp_by_authored_level = Counter()
    quest_gold_by_level = Counter()
    for quest_id in sorted(data.quests):
        quest = data.quests[quest_id]
        effective = data.effective_quest_levels[quest_id]
        rewards = data.quest_rewards.get(quest_id, ())
        raw_xp = sum(int(reward["long_value"]) for reward in rewards if int(reward["reward_type"]) == 5)
        gold = sum(int(reward["long_value"]) for reward in rewards if int(reward["reward_type"]) == 0)
        live_xp = int(raw_xp * data.experience_modifier)
        denominator_level = min(49, max(1, effective))
        incremental = thresholds.get(denominator_level, 0) - thresholds.get(denominator_level - 1, 0)
        raw_pct = raw_xp / incremental * 100 if incremental > 0 else None
        live_pct = live_xp / incremental * 100 if incremental > 0 else None
        requirements, expected_kills, gold_input = quest_effort(data, quest_id)
        quest_rows.append((quest_id, quest["name"], class_label(quest["class_restrictions"], data.classes), int(quest["min_level"]), effective, effective - int(quest["min_level"]), "yes" if str(quest["repeatable"]) != "0" else "no", n(raw_xp), n(live_xp), n(gold), n(raw_pct, 1) + "%" if raw_pct is not None else "—", n(live_pct, 1) + "%" if live_pct is not None else "—", requirements, n(expected_kills, 1), n(gold_input), reward_text(data, quest_id)))
        if quest_id in one_time_quests:
            quest_xp_by_level[effective] += raw_xp
            quest_xp_by_authored_level[int(quest["min_level"])] += raw_xp
            quest_gold_by_level[effective] += gold
    total_raw_xp = sum(quest_xp_by_level.values())
    total_gold = sum(quest_gold_by_level.values())
    practical_coverage = []
    authored_coverage = []
    practical_cumulative = 0
    authored_cumulative = 0
    for level in range(1, 50):
        practical_cumulative += quest_xp_by_level[level]
        authored_cumulative += quest_xp_by_authored_level[level]
        practical_coverage.append((level, practical_cumulative / thresholds[level] * 100 if thresholds[level] else 0))
        authored_coverage.append((level, authored_cumulative / thresholds[level] * 100 if thresholds[level] else 0))
    coverage_rows = []
    for level in (5, 10, 20, 30, 40, 49):
        practical_available = sum(value for quest_level, value in quest_xp_by_level.items() if quest_level <= level)
        authored_available = sum(value for quest_level, value in quest_xp_by_authored_level.items() if quest_level <= level)
        coverage_rows.append((level, n(thresholds[level]), n(authored_available), n(authored_available / thresholds[level] * 100, 1) + "%", n(practical_available), n(practical_available / thresholds[level] * 100, 1) + "%"))
    reward_counts = Counter()
    reward_values = Counter()
    for quest_id in sorted(data.quests):
        for reward in data.quest_rewards.get(quest_id, ()):
            kind = REWARD_NAMES.get(int(reward["reward_type"]), f"type {reward['reward_type']}")
            reward_counts[kind] += 1
            reward_values[kind] += int(reward["long_value"])
    spells, spell_grants = spell_rows(data)
    spell_family_rows = []
    grouped_spells: defaultdict[str, list[dict[str, Any]]] = defaultdict(list)
    for row in spells:
        grouped_spells[row["family"]].append(row)
    for family in sorted(grouped_spells):
        rows = sorted(grouped_spells[family], key=lambda row: (row["rank"] if row["rank"] is not None else 0, row["id"]))
        levels = [row["practical"] for row in rows if row["practical"] is not None]
        gaps = [b - a for a, b in zip(levels, levels[1:])]
        spell_family_rows.append((family, len(rows), ", ".join(n(level) for level in levels) or "—", n(max(gaps) if gaps else None), ", ".join(n(row["cooldown"], 0) for row in rows), "; ".join(f"{row['name']}: {row['magnitude_basis']} = {n(row['magnitude'],2)}; {row['growth']}" for row in rows), "; ".join(f"{row['name']}: {row['sources']}" for row in rows)))
    practical_counts = Counter(row["practical"] for row in spells if row["practical"] is not None)
    auto_counts = Counter(data.classes[grant.class_id]["class_name"] for grant in data.class_spell_grants)
    spell_summary_rows = [(data.classes[class_id]["class_name"], auto_counts[data.classes[class_id]["class_name"]], sum(class_id in allowed_classes(data.spells[row["id"]]["class_restrictions"], data.classes) and row["practical"] is not None for row in spells), sum(class_id in allowed_classes(data.spells[row["id"]]["class_restrictions"], data.classes) and row["practical"] is None for row in spells)) for class_id in PLAYER_CLASS_IDS]
    late_spells = [row for row in spells if row["nominal"] is not None and 39 <= row["nominal"] <= 49 and row["practical"] in {45, 50} and row["practical"] > row["nominal"]]
    anomaly_rows = [
        ("Critical", "Elemental Strike", "Rank 8: 250 formula damage, 400 MP, 4s; rank 9: 300 damage, 350 MP, 1s", "Rank 8 is strictly dominated on scalar damage/cost/cooldown; its line geometry differs from rank 9 single-target geometry"),
        ("High", "Backstab", "Cooldowns 18 / 23 / 27 / 23 / 18 seconds", "Cooldown reverses direction twice across ranks"),
        ("Medium", "Fortify", "Rank 1 cooldown 30s; ranks 2–5 cooldown 5s", "Starter rank has six times the later cooldown"),
        ("High", "Late scroll sourcing", f"{len(late_spells)} nominal L39–49 spells resolve at L45 or L50", ", ".join(f"{row['name']} {row['nominal']}→{row['practical']}" for row in late_spells) or "No rows"),
    ]
    item_outlier_rows = []
    for item_id in (451, 18, 214, 132, 140, 161, 163, 115):
        item = data.items[item_id]
        metrics = item_metrics(item)
        item_outlier_rows.append((item_id, item["item_name"], data.effective_item_levels.get(item_id, "—"), SLOT_NAMES.get(int(item["item_slot"]), item["item_slot"]), n(metrics["throughput"], 2), n(metrics["budget"], 2), n(metrics["effective_hp"]), source_text(data, item_id, True)))
    anchors = []
    anchor_specs = (
        ("Warrior one-hand", 2, 30, 3, "throughput"),
        ("Rogue one-hand", 2, 35, 2, "throughput"),
        ("Shield", 1, 28, None, "budget"),
        ("Cloak", 7, 32, None, "budget"),
        ("Ring", 4, 40, None, "budget"),
    )
    for label, slot_id, target, class_id, metric_name in anchor_specs:
        candidates = [
            row
            for row in item_rows
            if row["id"] in frontier_ids
            and int(data.items[row["id"]]["item_slot"]) == slot_id
            and (class_id is None or class_id in allowed_classes(data.items[row["id"]]["class_restrictions"], data.classes))
        ]
        before = [row for row in candidates if row["effective"] <= target]
        after = [row for row in candidates if row["effective"] >= target]
        prior = max(before, key=lambda row: (row["effective"], row["metrics"][metric_name], -row["id"]), default=None)
        following = min(after, key=lambda row: (row["effective"], -row["metrics"][metric_name], row["id"]), default=None)
        assert prior is not None and following is not None and prior["effective"] < target < following["effective"]
        prior_level = prior["effective"]
        next_level = following["effective"]
        prior_score = prior["metrics"][metric_name]
        next_score = following["metrics"][metric_name]
        fraction = (target - prior_level) / (next_level - prior_level)
        target_score = prior_score + fraction * (next_score - prior_score)
        formula = f"{n(prior_score,2)} + {target-prior_level}/{next_level-prior_level} × ({n(next_score,2)} − {n(prior_score,2)}) = {n(target_score,2)}"
        adjustment = f"vendor/common 90%={n(target_score*0.9,2)}; quest/craft 100%={n(target_score,2)}; rare/boss 110%={n(target_score*1.1,2)}"
        anchors.append((label, target, metric_name, f"{prior['name']} #{prior['id']}", prior_level, n(prior_score, 2), f"{following['name']} #{following['id']}", next_level, n(next_score, 2), n(fraction, 3), formula, adjustment))
    tables = []
    tables.append(render_table("table-priorities", "Prioritized interventions", ["Priority", "Area", "Evidence", "Recommended action"], [
        ("P0", "Equipment access", "No pre-50 necklace; no sourced mount; ring begins around L23–25", "Add sourced baseline necklace and mount; add an early ring without eclipsing L25"),
        ("P0", "Equipment outliers", "Practice Katana #451 beats Long Sword #18; Thick Skin #160 and Poo Flinger armor outliers #132/#140/#161/#163 dominate frontiers", "Reprice/relevel or trim vectors before adding stronger successors"),
        ("P0", "Searing Whip inversion", "Searing Whip #214 at L25 has throughput 60, beating later L27 class-specific Bastard Sword 45, Malignant Dagger 36, and Brilliant Hammer 19", "Reduce damage/STR or move the source after its intended successors"),
        ("P0", "Spell rank regression", "Elemental Strike 8 is strictly dominated by rank 9", "Reduce rank-8 cost/cooldown or move rank 9 later"),
        ("P1", "NPC same-level fairness", "L12 and L30 peers share XP despite large HP/AC differences", "Normalize XP to effective durability and pressure"),
        ("P1", "Quest workload", "Q52 ≈167 expected kills; Q41 ≈333", "Raise drop rates, lower counts, or raise durable rewards"),
        ("P1", "Late quest coverage", "Authored one-time raw XP covers only 18.6% of the L49 cumulative threshold", "Add quest chains in L34–37, 41–44, and 46–49"),
    ]))
    tables.append(render_table("table-slot-gaps", "Slot coverage and long gaps", ["Slot", "Sourced", "First", "Last", "Longest gap", "Effective levels", "Finding"], slot_rows))
    tables.append(render_table("table-class-slot", "Class × slot coverage", ["Class", "Slot", "Sourced", "First", "Last", "Levels"], class_slot_rows, "Class masks are allow-lists; zero is unrestricted."))
    tables.append(render_table("table-gap-actions", "Actionable equipment gaps", ["Priority", "Class", "Slot", "Gap", "Evidence", "Proposed target"], gap_rows))
    tables.append(render_table("table-frontiers", "Meaningful item frontiers", ["Class", "Slot", "Effective level", "Item", "Proxy score", "Proxy"], frontier_rows, "A meaningful frontier exceeds the prior same-class/slot maximum by more than 5%."))
    tables.append(render_table("table-equipment", "Focused included equipment detail", ["ID", "Name", "Kind", "Slot", "Type", "Classes", "Authored L", "Effective L", "Raw vector", "Effective HP", "Effective MP", "Dodge pp", "Weapon throughput", "Budget", "Frontier", "Eligible sources"], [(row["id"], row["name"], row["use"], row["slot"], data.items[row["id"]]["item_type"], row["classes"], row["authored"], row["effective"], row["stats"], n(row["metrics"]["effective_hp"]), n(row["metrics"]["effective_mp"]), n(row["metrics"]["dodge_pct_points"], 2), n(row["metrics"]["throughput"], 2), n(row["metrics"]["budget"], 2), row["frontier"], row["sources"]) for row in item_rows], "All 189 modeled-source equipment templates; non-equipment remains in the full included-item audit."))
    tables.append(render_table("table-items", "Full included-item audit", ["ID", "Name", "Use type", "Slot", "Type", "Classes", "Authored L", "Effective L", "Raw vector", "Effective HP", "Effective MP", "Dodge pp", "Weapon throughput", "Budget", "Frontier", "Eligible sources"], included_item_table_rows, "All 372 templates with a valid modeled acquisition chain."))
    tables.append(render_table("table-item-outliers", "Verified equipment anchors and outliers", ["ID", "Item", "Effective L", "Slot", "Throughput", "Budget", "Effective HP", "Source"], item_outlier_rows))
    tables.append(render_table("table-excluded-equipment", "Focused excluded equipment audit", ["ID", "Name", "Authored L", "Min XP", "Slot", "Classes", "Reason", "All modeled paths"], [(item_id, data.items[item_id]["item_name"], data.items[item_id]["min_level"], data.items[item_id]["min_experience"], SLOT_NAMES.get(int(data.items[item_id]["item_slot"]), data.items[item_id]["item_slot"]), class_label(data.items[item_id]["class_restrictions"], data.classes), data.excluded_item_reasons[item_id], source_text(data, item_id)) for item_id in excluded_gear], "All 120 scoped no-XP equipment templates without a valid modeled source."))
    tables.append(render_table("table-excluded-items", "Full excluded-item audit", ["ID", "Name", "Use type", "Authored L", "Min XP", "Slot", "Classes", "Reason", "All modeled paths"], [(item_id, data.items[item_id]["item_name"], data.items[item_id]["item_usetype"], data.items[item_id]["min_level"], data.items[item_id]["min_experience"], SLOT_NAMES.get(int(data.items[item_id]["item_slot"]), data.items[item_id]["item_slot"]), class_label(data.items[item_id]["class_restrictions"], data.classes), data.excluded_item_reasons[item_id], source_text(data, item_id)) for item_id in sorted(data.excluded_item_reasons)], "All 287 excluded templates, including direct-scope source failures and templates outside the level/XP scope; premium-only and XP-gated-only paths remain visible."))
    tables.append(render_table("table-npc-levels", "NPC scaling by exact level and regime", ["Level", "Regime", "Templates", "Median HP", "Median AC", "Median raw XP", "Median XP/HP", "Median pressure"], npc_level_rows))
    tables.append(render_table("table-npc-outliers", "NPC same-level outliers", ["ID", "NPC", "Level", "HP", "AC", "Raw XP", "HP / median", "XP / median", "XP / HP"], npc_outliers))
    tables.append(render_table("table-npcs", "NPC detail", ["ID", "Name", "Level", "Regime", "Class", "Maps", "HP", "MP", "AC", "STR", "Effective vector", "Weapon", "Seconds/attack", "APS", "Hit proxy", "Pressure", "Raw XP", "Default-live XP", "XP/HP", "Same-level kills"], [(npc_id, data.npcs[npc_id]["npc_name"], data.npc_metrics[npc_id].level, "XP-gated endgame" if npc_id in data.endgame_npc_ids else "L50 transition" if npc_id in data.transition_npc_ids else "leveling", data.classes[int(data.npcs[npc_id]["class_id"])]["class_name"], ", ".join(str(map_id) for map_id in data.npc_map_ids[npc_id]), n(data.npc_metrics[npc_id].hp), n(data.npc_metrics[npc_id].mp), n(data.npc_metrics[npc_id].ac), n(data.npc_metrics[npc_id].strength), f"STA {data.npc_metrics[npc_id].stamina}; DEX {data.npc_metrics[npc_id].dexterity}; INT {data.npc_metrics[npc_id].intelligence}; res F/W/S/A/E {data.npc_metrics[npc_id].res_fire}/{data.npc_metrics[npc_id].res_water}/{data.npc_metrics[npc_id].res_spirit}/{data.npc_metrics[npc_id].res_air}/{data.npc_metrics[npc_id].res_earth}", n(data.npcs[npc_id]["weapon_damage"]), n(data.npcs[npc_id]["attack_speed"]), n(data.npc_metrics[npc_id].attacks_per_second, 3), n(data.npc_metrics[npc_id].same_level_hit), n(data.npc_metrics[npc_id].pressure), n(data.npcs[npc_id]["experience"]), n(data.npc_metrics[npc_id].live_experience), n(data.npc_metrics[npc_id].xp_per_hp, 3), n(data.npc_metrics[npc_id].live_kills, 1)) for npc_id in npc_ids]))
    tables.append(render_table("table-quest-coverage", "One-time quest XP coverage", ["Through level", "Cumulative threshold", "Authored raw XP", "Authored coverage", "Practical raw XP", "Practical coverage"], coverage_rows))
    tables.append(render_table("table-quests", "Quest detail", ["ID", "Quest", "Classes", "Authored L", "Practical L", "Mismatch", "Repeatable", "Raw XP", "Default-live XP", "Gold", "Raw % incremental", "Live % incremental", "Requirements", "Expected kills", "Vendor input", "All rewards"], quest_rows))
    tables.append(render_table("table-rewards", "Quest reward inventory", ["Reward type", "Rows", "Long-value total"], [(kind, reward_counts[kind], n(reward_values[kind])) for kind in sorted(reward_counts)]))
    tables.append(render_table("table-spell-summary", "Spell access by class", ["Class", "Automatic grants", "Modeled reachable", "No modeled source"], spell_summary_rows))
    tables.append(render_table("table-spell-families", "Ranked spell pacing", ["Family", "Ranks", "Practical unlocks", "Longest spacing", "Cooldown seconds", "Formula/buff growth", "Sources"], spell_family_rows))
    tables.append(render_table("table-spell-anomalies", "Spell pacing anomalies", ["Priority", "Family", "Evidence", "Finding"], anomaly_rows))
    tables.append(render_table("table-spells", "Spell detail", ["ID", "Spell", "Family", "Rank", "Classes", "Nominal L", "Practical L", "Source", "Cooldown s", "Modeled cost %", "Raw costs", "Target geometry", "Magnitude basis", "Magnitude", "Prior→current growth", "Effect/formula", "Damage scaling"], [(row["id"], row["name"], row["family"], n(row["rank"]), row["classes"], n(row["nominal"]), n(row["practical"]), row["sources"], n(row["cooldown"], 1), n(row["cost_pct"], 1) + "%" if row["cost_pct"] is not None else "—", row["costs"], row["target"], row["magnitude_basis"], n(row["magnitude"], 2), row["growth"], row["effect"], "yes" if row["damage_scaled"] else "no") for row in spells]))
    tables.append(render_table("table-authoring-anchors", "Worked interpolation anchors for new items", ["Worked case", "Target L", "Proxy", "Prior item", "Prior L", "Prior proxy", "Next item", "Next L", "Next proxy", "Fraction", "Interpolation calculation", "Source-tier adjustment"], anchors))
    tables.append(render_table("table-source-tiers", "Source-tier budget guidance", ["Source tier", "Target vs interpolated frontier", "Use"], [
        ("Guaranteed vendor/common drop", "90–95%", "Baseline access and gap filling"),
        ("One-time quest/craft", "100%", "Predictable meaningful upgrade"),
        ("Uncommon drop", "105%", "Optional chase reward"),
        ("Rare/boss drop", "110–115%", "Short-lived aspirational ceiling; avoid permanent dominance"),
    ]))
    class_colors = ("#e56b6f", "#5aa9e6", "#65b891", "#f4a261")
    weapon_class_series = []
    armor_class_series = []
    for class_id, color in zip(PLAYER_CLASS_IDS, class_colors):
        weapon_values = []
        armor_values = []
        best_weapon = 0.0
        best_armor = 0.0
        for level in range(1, 51):
            best_weapon = max([best_weapon] + [row["metrics"]["throughput"] for row in item_rows if row["use"] == "Weapon" and row["effective"] == level and class_id in allowed_classes(data.items[row["id"]]["class_restrictions"], data.classes)])
            best_armor = max([best_armor] + [row["metrics"]["budget"] for row in item_rows if row["use"] == "Armor" and row["effective"] == level and class_id in allowed_classes(data.items[row["id"]]["class_restrictions"], data.classes)])
            weapon_values.append((level, best_weapon))
            armor_values.append((level, best_armor))
        class_name = str(data.classes[class_id]["class_name"])
        weapon_class_series.append((class_name, weapon_values, color))
        armor_class_series.append((class_name, armor_values, color))
    charts = [
        bar_chart("chart-item-availability", "Equipment availability by effective-level band", [(f"L{start}–{min(50,start+4)}", sum(level_item_counts[level] for level in range(start, min(50, start+4)+1))) for start in range(1, 51, 5)], "new sourced templates"),
        line_chart("chart-weapon-frontier", "Best reachable weapon-throughput proxy by class", weapon_class_series, "proxy throughput"),
        line_chart("chart-armor-frontier", "Best reachable armor comparable-budget proxy by class", armor_class_series, "budget units"),
        line_chart("chart-npc-hp", "Pre-50 NPC median effective HP", [("literal template + class HP", npc_hp_series, "#65b891")], "HP"),
        line_chart("chart-npc-xp-efficiency", "Pre-50 NPC median raw XP per HP", [("raw XP / HP", npc_xp_efficiency, "#f4a261")], "raw XP per HP"),
        bar_chart("chart-quest-rewards", "One-time quest raw XP by practical-level band", [(f"L{start}–{min(49,start+4)}", sum(value for level, value in quest_xp_by_level.items() if start <= level <= min(49,start+4))) for start in range(1, 50, 5)], "raw XP"),
        line_chart("chart-quest-coverage", "Cumulative one-time quest XP vs Rogue threshold", [("authored-level coverage", authored_coverage, "#9b5de5"), ("practical-level coverage", practical_coverage, "#f4a261")], "coverage percent"),
        bar_chart("chart-spell-pacing", "Reachable spell unlocks by practical-level band", [(f"L{start}–{min(50,start+4)}", sum(practical_counts[level] for level in range(start, min(50,start+4)+1))) for start in range(1, 51, 5)], "spell templates"),
        bar_chart("chart-level50-regimes", "Level-50 population is two distinct regimes", [("Ungated transition", len(data.transition_npc_ids)), ("XP-gated endgame", len(data.endgame_npc_ids))], "monster templates"),
    ]
    css = """
:root{color-scheme:dark;--bg:#10151d;--panel:#18212d;--panel2:#202c3a;--text:#e8edf3;--muted:#aebdca;--accent:#63d2ff;--warn:#ffca58;--danger:#ff6b6b;--line:#354657}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,-apple-system,Segoe UI,sans-serif}header{padding:3rem max(4vw,1rem);background:linear-gradient(135deg,#18283b,#16202b 60%,#25364c)}main{max-width:1500px;margin:auto;padding:1.5rem}h1{font-size:clamp(2rem,5vw,4rem);margin:0 0 .5rem}h2{font-size:2rem;margin-top:3rem;border-bottom:1px solid var(--line);padding-bottom:.5rem}h3{margin:.2rem 0 .5rem}p,li{max-width:95ch}.lede{font-size:1.15rem;color:var(--muted)}nav{display:flex;gap:.6rem;flex-wrap:wrap;margin-top:1.3rem}nav a{color:var(--accent);text-decoration:none;border:1px solid var(--line);padding:.35rem .65rem;border-radius:99px}.toolbar{position:sticky;top:0;z-index:10;background:#10151def;padding:.7rem max(4vw,1rem);border-bottom:1px solid var(--line);backdrop-filter:blur(9px)}input{width:min(650px,100%);padding:.75rem 1rem;border-radius:8px;border:1px solid var(--line);background:var(--panel);color:var(--text)}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:1rem}.card,.callout,.formula{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:1rem}.card strong{display:block;font-size:1.8rem;color:var(--accent)}.callout{border-left:5px solid var(--warn)}.danger{border-left-color:var(--danger)}.charts{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(550px,100%),1fr));gap:1rem}.chart-card,.table-card{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:1rem;margin:1rem 0;overflow:hidden}.chart-card figcaption{font-weight:700;font-size:1.05rem}.chart{width:100%;height:auto}.chart text{fill:var(--muted);font-size:11px}.chart rect{fill:#5aa9e6}.grid{stroke:#354657;stroke-width:1}.axis{stroke:#91a3b0;stroke-width:1}.table-wrap{overflow:auto;max-height:620px}table{border-collapse:collapse;width:100%;font-size:.86rem}th{position:sticky;top:0;background:var(--panel2);cursor:pointer;text-align:left;white-space:nowrap}th,td{border-bottom:1px solid var(--line);padding:.45rem .55rem;vertical-align:top}tbody tr:nth-child(even){background:#1b2633}tbody tr:hover{background:#263649}.table-note,.muted{color:var(--muted)}code{background:#0b1118;padding:.12rem .3rem;border-radius:4px;color:#b9f4ff}.formula-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(290px,1fr));gap:1rem}.pill{display:inline-block;border-radius:99px;padding:.15rem .5rem;background:#29394a;color:#c8e8f5}.hidden-row{display:none}footer{padding:2rem;color:var(--muted);text-align:center}@media print{.toolbar{display:none}.table-wrap{max-height:none;overflow:visible}body{background:#fff;color:#111}.card,.callout,.formula,.chart-card,.table-card{break-inside:avoid;background:#fff;border-color:#bbb}.chart text{fill:#333}}
"""
    js = """
const search=document.getElementById('global-search');function applySearch(){const q=search.value.trim().toLowerCase();document.querySelectorAll('tbody tr').forEach(r=>r.classList.toggle('hidden-row',q&&!r.textContent.toLowerCase().includes(q)))}search.addEventListener('input',applySearch);document.querySelectorAll('table.sortable').forEach(table=>table.querySelectorAll('th').forEach((th,index)=>{const sort=()=>{const body=table.tBodies[0],rows=[...body.rows],ascending=th.dataset.direction!=='asc';rows.sort((a,b)=>{const x=a.cells[index].textContent.trim().replace(/[,:%]/g,''),y=b.cells[index].textContent.trim().replace(/[,:%]/g,''),xn=Number(x),yn=Number(y);return Number.isFinite(xn)&&Number.isFinite(yn)?(xn-yn)*(ascending?1:-1):x.localeCompare(y,undefined,{numeric:true})*(ascending?1:-1)});rows.forEach(r=>body.appendChild(r));th.dataset.direction=ascending?'asc':'desc'};th.addEventListener('click',sort);th.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();sort()}})}));
"""
    return f"""<!doctype html>
<html lang=en><head><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>Goose Leveling Balance Report</title><style>{css}</style></head>
<body><header><span class=pill>Deterministic offline analysis</span><h1>Leveling Balance Report</h1><p class=lede>Effective acquisition, combat scaling, quest economy, and spell pacing through level 50. Base templates are separated from runtime randomness and the two level-50 regimes are never fit to one curve.</p><nav><a href=#executive>Executive</a><a href=#items>Items</a><a href=#npcs>NPCs</a><a href=#quests>Quests</a><a href=#spells>Spells</a><a href=#guidelines>Authoring guide</a><a href=#caveats>Caveats</a></nav></header>
<div class=toolbar><label for=global-search>Search every detail table </label><input id=global-search type=search placeholder='Try Searing Whip, Rogue, L50, no_eligible_source…'></div><main>
<section id=executive><h2>Executive findings</h2><div class=cards><div class=card><strong>{summary['items']['total']}</strong>item templates</div><div class=card><strong>{summary['items']['in_level_xp_scope']}</strong>direct-scope items</div><div class=card><strong>{summary['items']['included']}</strong>modeled eligible chains</div><div class=card><strong>309</strong>no-XP equipment templates</div><div class=card><strong>189</strong>valid modeled sources</div><div class=card><strong>101 / 88</strong>effective &lt;50 / =50</div><div class=card><strong>120</strong>no valid source</div><div class=card><strong>58 / 38 / 48</strong>pre-50 / ungated L50 / gated L50 monsters</div><div class=card><strong>{n(total_raw_xp)}</strong>one-time raw quest XP</div><div class=card><strong>{n(total_gold)}</strong>one-time quest gold</div><div class=card><strong>{len(data.spells)}</strong>spell templates</div></div>
<div class='callout danger'><strong>Progression holes are structural.</strong> There is no pre-50 necklace, no sourced mount, the first ring is around L23–25, pauldrons begin at L33, and cloak has only L15 until L50. Practice Katana #451 already beats Long Sword #18; simply adding stronger top-end gear would deepen existing inversions.</div>
<div class=callout><strong>Combat rewards are not consistently aligned.</strong> Green Slime L12 has 492 HP / 120 AC while Lost Wabbit and Pipsqueek have 261 HP / 0 AC at the same 246 raw XP. Naga Rogue L30 has 1,886 HP / 0 AC versus same-level warrior baselines near 2,814 HP / 300 AC at about 609 XP. Boss Pumpkin L35 has the median 3,804 HP but 2,000 XP, about 2.82× the level median XP and therefore about 2.82× median XP/HP.</div>
{tables[0]}</section>
<section id=items><h2>Item acquisition, scaling, and gaps</h2><p>Effective level is the earliest eligible source, bounded by the item requirement. Monster drops use the monster/map gate; vendors use the earliest eligible map gate; quest rewards use practical quest level; crafts recursively require the recipe and every ingredient. Equipment counts reproduce the verified 309 / 189 / 101 / 88 / 120 split. Moon Shield #115 is a weak 25-AC source-level-50 reward despite its authored L45 requirement.</p><div class=charts>{charts[0]}{charts[1]}{charts[2]}</div>{''.join(tables[1:10])}</section>
<section id=npcs><h2>NPC scaling and outliers</h2><p>There are 58 exact L1–49 monster templates, 38 non-XP-gated L50 transition templates, and 48 gated-only L50 endgame templates. Missing exact monster levels are {e(', '.join(map(str, missing_levels)))}. Level 50 is a mixed regime and is reported separately rather than included in the pre-50 median curve.</p><div class=charts>{charts[3]}{charts[4]}{charts[8]}</div>{''.join(tables[10:13])}</section>
<section id=quests><h2>Quest economy and coverage</h2><p>The 48 one-time quests award {n(total_raw_xp)} raw XP and {n(total_gold)} gold. By authored minimum, raw one-time XP reaches 75.0% of the cumulative Rogue threshold through L10 but only 18.6% through L49. Dependency-adjusted practical coverage is 19.3% and 16.4% at those anchors. Content gaps are concentrated at L34–37, L41–44, and L46–49.</p><div class=callout>Q17 is authored L22 but practical L28. Q49 and Q50 are authored L30 but practical L33. Q52 requires about 167 expected kills at the best modeled 3% Ruby rate; Q41 requires about 333 for ten Pearls. Repeatable Q27 consumes 40 vendor potions costing 2,000 gold and returns 1,600 gold plus 1,000 raw XP: a net conversion of 400g to 1,000 raw XP.</div><div class=charts>{charts[5]}{charts[6]}</div>{''.join(tables[13:16])}</section>
<section id=spells><h2>Spell pacing and anomalies</h2><p>Automatic grants are Rogue 10, Warrior 15, Magus 0, Priest 0. Scroll/quest access uses a class floor of L5. Resource percentage uses a full-resource authoring model: static cost is removed first, then percent cost is truncated from the remainder; the table reports the largest eligible-class HP/MP share at practical unlock and is not a full cast simulation. Rank growth uses absolute constants, comparable linear coefficients, single direct fields, and per-component deltas for stable multi-stat vectors. Truly scripted effects without numeric fields, nonlinear formulas, multiple formula channels, and changed component sets remain explicitly N/A. Delta and ratio are calculated only when adjacent ranks share a comparable magnitude basis or component set, with duration and target geometry kept separate.</p><div class=charts>{charts[7]}</div>{''.join(tables[16:20])}</section>
<section id=guidelines><h2>Formulas and authoring guidelines</h2><div class=formula-grid><div class=formula><h3>Player item contribution</h3><code>effective HP = direct HP + 25 × STA</code><br><code>effective MP = direct MP + 25 × INT</code><p>STA/INT conversion applies through player item stat addition, not to NPC templates.</p></div><div class=formula><h3>Weapon authoring proxy</h3><code>10 × (damage + STR) / delay / (1 − haste)</code><p>Templates have no direct haste column here, so charted base-template haste is zero. Runtime interval is <code>delay / 10 × (1 − min(0.95, haste)) × 0.9</code>; the proxy is not runtime DPS.</p></div><div class=formula><h3>Dodge</h3><code>display proxy = min(DEX / 100, 50) percentage points</code><p>Runtime RNG is approximately <code>(min(DEX, 5000)+1) / 10001</code>, including a tiny chance at zero DEX.</p></div><div class=formula><h3>Comparable-item budget</h3><code>HP/25 + MP/25 + SP/25 + AC/5 + STR + STA + DEX + INT + total resist/2</code><p>This disclosed authoring proxy excludes weapon throughput and does not claim equal combat value for unlike stats.</p></div><div class=formula><h3>NPC pressure</h3><code>APS = 1 / attack_speed</code><br><code>same-level hit = STR + weapon + level</code><br><code>pressure = hit × APS × (1+melee damage) × (1+crit)</code><p>Player AC uses MaxAC 3500 with a class multiplier, armor pierce, proportional absorption, and flat subtraction; target-specific mitigation is intentionally outside this proxy.</p></div><div class=formula><h3>New-item interpolation</h3><code>target = prior + (next − prior) × level position</code><p>Interpolate only within the same class/slot proxy, then multiply by the source-tier band. Preserve each raw vector and check Pareto/frontier impact before publication.</p></div></div>{tables[20]}{tables[21]}</section>
<section id=caveats><h2>Source, runtime, and exclusion caveats</h2><ul><li><strong>Source snapshot:</strong> <code>Goose/bin/Debug/AsperetaGoose.db</code>, SHA-256 <code>{e(summary['database_sha256'])}</code>. Opened with SQLite URI read-only mode and hashed before/after extraction.</li><li><strong>Scope:</strong> level ≤50 and item <code>min_experience=0</code>. XP-gated-only map sources and Phat Lewtz/credit-only vendor paths are excluded unless another eligible source exists. The 30 premium templates include 19 gear templates; do not conflate those populations.</li><li><strong>Reachability:</strong> spawn/map presence is modeled, but travel topology, warp chains, required-map items, scripts, upper level/XP gates, stock economics, and player coordination are not fully simulated.</li><li><strong>Random items:</strong> title and surname outer rolls are independently 50% on eligible drops, purchases, crafts, and quest rewards: the configured <code>0.5</code> is passed to <code>RollChance</code> as a probability fraction, not as a percent-valued setting. Each applicable modifier independently enters a candidate pool using its authored chance; an empty pool applies nothing, and one successful candidate is chosen uniformly. Modifier application requires template <code>min_level ≥ 1</code>, so templates authored with <code>min_level=0</code> are ineligible. Curves use unmodified templates, not best/worst rolled outcomes.</li><li><strong>NPCs:</strong> effective HP/MP/AC and attributes are literal template plus <code>class_info</code>. STA/INT are not converted again. Pressure omits target defense, scripts, buffs, movement, regeneration timing, and encounter mechanics.</li><li><strong>Quest runtime:</strong> raw XP passes through normal experience gain. “Default-live” means configured ×{n(data.experience_modifier,0)}, but player bonuses, modifier limits, dynamic player-count modifiers, scripts, shared kills, item competition, and the quest-window iteration behavior can change outcomes.</li><li><strong>Spell runtime:</strong> aether is milliseconds. Static and percent costs are applied in runtime order and formula effects may be scripted. <code>spell_damage_effects</code> conditionally applies spell damage/crit; global damage modifies HP formula results but not MP. Practical source level does not prove a player will discover or afford the scroll.</li><li><strong>Thresholds:</strong> <code>class_info.level_up_exp</code> is cumulative; incremental percentages use Rogue current-minus-prior thresholds. L50 has no next-level threshold, so L50 quest rows use the L49 increment as a clearly bounded comparison proxy.</li><li><strong>Source references:</strong> mechanics were checked in <code>Goose/Player.cs</code>, <code>Goose/NPC.cs</code>, <code>Goose/Events/PlayerAttackEvent.cs</code>, <code>Goose/SpellEffect.cs</code>, <code>Goose/Quests/QuestWindow.cs</code>, <code>Goose/Class.cs</code>, and <code>Goose/GooseSettings.json</code>.</li></ul>
<h3>Exclusion population audit</h3><div class=cards><div class=card><strong>{summary['items']['excluded_reasons'].get('item_min_experience',0)}</strong>item XP gate</div><div class=card><strong>{summary['items']['excluded_reasons'].get('xp_gated_sources_only',0)}</strong>XP-gated source only</div><div class=card><strong>{summary['items']['excluded_reasons'].get('excluded_vendor_only',0)}</strong>premium/credit vendor only</div><div class=card><strong>{summary['items']['excluded_reasons'].get('no_eligible_source',0)}</strong>other no source</div></div></section>
</main><footer>Generated deterministically from the local source snapshot. No network resources required.</footer><script>{js}</script></body></html>"""
