#!/usr/bin/env python3

from __future__ import annotations

import argparse
import hashlib
import html
import json
from html.parser import HTMLParser
import math
import re
import sqlite3
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass, replace
from pathlib import Path
from statistics import median
from typing import Any, Iterable, Sequence

try:
    from .report_renderer import render_report
except ImportError:
    from report_renderer import render_report

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_DATABASE = REPOSITORY_ROOT / "Goose/bin/Debug/AsperetaGoose.db"
DEFAULT_SETTINGS = REPOSITORY_ROOT / "Goose/GooseSettings.json"
DEFAULT_OUTPUT = REPOSITORY_ROOT / "reports/game_balance/leveling-balance-report.html"
MAX_LEVEL = 50
PHAT_LEWTZ_NPC_ID = 170
NPC_TYPE_MONSTER = 2
ITEM_USE_TYPE_ARMOR = 2
ITEM_USE_TYPE_WEAPON = 3
ITEM_USE_TYPE_SCROLL = 4
QUEST_REQUIREMENT_ITEM = 1
QUEST_REQUIREMENT_KILL = 2
QUEST_REQUIREMENT_TALK_TO_NPC = 3
QUEST_REWARD_ITEM = 1
QUEST_REWARD_LEARN_SPELL = 20

REQUIRED_COLUMNS = {
    "maps": {
        "map_id",
        "map_name",
        "min_level",
        "max_level",
        "min_experience",
        "max_experience",
    },
    "npc_templates": {
        "npc_id",
        "npc_type",
        "npc_name",
        "npc_level",
        "experience",
        "attack_range",
        "attack_speed",
        "invincible",
        "npc_hp",
        "npc_mp",
        "npc_sp",
        "class_id",
        "stat_ac",
        "stat_str",
        "stat_sta",
        "stat_dex",
        "stat_int",
        "res_fire",
        "res_water",
        "res_spirit",
        "res_air",
        "res_earth",
        "weapon_damage",
        "armor_pierce",
        "hp_percent_regen",
        "hp_static_regen",
        "mp_percent_regen",
        "mp_static_regen",
        "credit_dealer",
        "quest_ids",
    },
    "npc_spawns": {"npc_id", "map_id", "map_x", "map_y"},
    "npc_drops": {"npc_template_id", "item_template_id", "stack", "droprate"},
    "npc_vendor_items": {
        "npc_template_id",
        "item_template_id",
        "stack",
        "stats_visible",
        "slot",
    },
    "item_templates": {
        "item_template_id",
        "item_usetype",
        "item_name",
        "player_hp",
        "player_mp",
        "player_sp",
        "stat_ac",
        "stat_str",
        "stat_sta",
        "stat_dex",
        "stat_int",
        "res_fire",
        "res_water",
        "res_spirit",
        "res_air",
        "res_earth",
        "min_experience",
        "min_level",
        "max_experience",
        "max_level",
        "weapon_damage",
        "weapon_delay",
        "item_slot",
        "item_type",
        "item_value",
        "class_restrictions",
        "learn_spell_id",
    },
    "item_titles": {
        "id", "name", "min_level", "max_level", "min_experience",
        "max_experience", "item_usetype", "item_slot", "chance", "script_params",
    },
    "item_surnames": {
        "id", "name", "min_level", "max_level", "min_experience",
        "max_experience", "item_usetype", "item_slot", "chance", "script_params",
    },
    "quests": {
        "id",
        "name",
        "class_restrictions",
        "min_experience",
        "max_experience",
        "min_level",
        "max_level",
        "repeatable",
        "prerequisite_quests",
    },
    "quest_requirements": {
        "id",
        "quest_id",
        "requirement_type",
        "requirement_value",
        "requirement_value2",
    },
    "quest_rewards": {
        "id",
        "quest_id",
        "reward_type",
        "long_value",
        "long_value2",
    },
    "combinations": {
        "combination_id",
        "combination_name",
        "min_level",
        "max_level",
        "min_experience",
        "max_experience",
        "class_restrictions",
    },
    "combination_item_required": {"combination_id", "item_template_id"},
    "combination_item_results": {"combination_id", "item_template_id"},
    "classes": {"class_id", "class_name", "ac_multiplier"},
    "class_info": {
        "class_id",
        "level",
        "level_up_exp",
        "player_hp",
        "player_mp",
        "player_sp",
        "stat_ac",
        "stat_str",
        "stat_sta",
        "stat_dex",
        "stat_int",
        "res_fire",
        "res_water",
        "res_spirit",
        "res_air",
        "res_earth",
        "hp_percent_regen",
        "hp_static_regen",
        "mp_percent_regen",
        "mp_static_regen",
        "haste",
        "spell_damage",
        "spell_crit",
        "melee_damage",
        "melee_crit",
        "damage_reduce",
    },
    "classes_levelup_spells": {"class_id", "level", "spell_id"},
    "spells": {
        "spell_id",
        "spell_name",
        "class_restrictions",
        "spell_effect_id",
        "spell_aether",
        "hp_static_cost",
        "hp_percent_cost",
        "mp_static_cost",
        "mp_percent_cost",
        "sp_static_cost",
        "sp_percent_cost",
    },
    "spell_effects": {
        "spell_effect_id",
        "spell_effect_name",
        "effect_type",
        "effect_duration",
        "spell_damage_effects",
        "hp_change_formula",
        "mp_change_formula",
        "sp_change_formula",
        "hp",
        "mp",
        "sp",
        "stat_ac",
        "stat_str",
        "stat_sta",
        "stat_dex",
        "stat_int",
        "haste",
        "spell_damage",
        "spell_crit",
        "melee_damage",
        "melee_crit",
        "damage_reduce",
        "target_type",
        "target_size",
        "script_path",
    },
}


@dataclass(frozen=True)
class ItemSourcePath:
    item_id: int
    source_kind: str
    eligible: bool
    exclusion_reason: str | None = None
    npc_id: int | None = None
    quest_id: int | None = None
    combination_id: int | None = None
    map_ids: tuple[int, ...] = ()
    eligible_map_ids: tuple[int, ...] = ()
    ingredient_ids: tuple[int, ...] = ()
    quantity: int | None = None
    drop_rate: float | None = None
    vendor_slot: int | None = None
    effective_level: int | None = None


@dataclass(frozen=True)
class SpellGrant:
    spell_id: int
    source_kind: str
    level: int
    eligible: bool
    class_id: int | None = None
    item_id: int | None = None
    quest_id: int | None = None


@dataclass(frozen=True)
class NPCMetrics:
    npc_id: int
    level: int
    hp: int
    mp: int
    sp: int
    ac: int
    strength: int
    stamina: int
    dexterity: int
    intelligence: int
    res_fire: int
    res_water: int
    res_spirit: int
    res_air: int
    res_earth: int
    hp_percent_regen: float
    hp_static_regen: int
    mp_percent_regen: float
    mp_static_regen: int
    haste: float
    spell_damage: float
    spell_crit: float
    melee_damage: float
    melee_crit: float
    damage_reduction: float
    same_level_hit: float
    attacks_per_second: float
    pressure: float
    xp_per_hp: float | None
    incremental_level_xp: int | None
    live_experience: int
    live_kills: float | None


@dataclass(frozen=True)
class ReportData:
    maps: dict[int, dict[str, Any]]
    npcs: dict[int, dict[str, Any]]
    items: dict[int, dict[str, Any]]
    item_titles: tuple[dict[str, Any], ...]
    item_surnames: tuple[dict[str, Any], ...]
    quests: dict[int, dict[str, Any]]
    combinations: dict[int, dict[str, Any]]
    classes: dict[int, dict[str, Any]]
    class_info: dict[tuple[int, int], dict[str, Any]]
    spells: dict[int, dict[str, Any]]
    spell_effects: dict[int, dict[str, Any]]
    experience_modifier: float
    npc_map_ids: dict[int, tuple[int, ...]]
    npc_eligible_map_ids: dict[int, tuple[int, ...]]
    quest_npc_ids: dict[int, tuple[int, ...]]
    quest_requirements: dict[int, tuple[dict[str, Any], ...]]
    quest_rewards: dict[int, tuple[dict[str, Any], ...]]
    combination_requirements: dict[int, tuple[int, ...]]
    combination_results: dict[int, tuple[int, ...]]
    item_sources: dict[int, tuple[ItemSourcePath, ...]]
    effective_item_levels: dict[int, int]
    effective_quest_levels: dict[int, int]
    npc_metrics: dict[int, NPCMetrics]
    scoped_item_ids: frozenset[int]
    included_item_ids: frozenset[int]
    excluded_item_reasons: dict[int, str]
    spawned_npc_ids: frozenset[int]
    eligible_spawned_npc_ids: frozenset[int]
    gated_only_npc_ids: frozenset[int]
    leveling_npc_ids: frozenset[int]
    transition_npc_ids: frozenset[int]
    endgame_npc_ids: frozenset[int]
    class_spell_grants: tuple[SpellGrant, ...]
    scroll_spell_grants: tuple[SpellGrant, ...]
    quest_spell_grants: tuple[SpellGrant, ...]
    excluded_vendor_only_gear_ids: frozenset[int]
    retained_dual_source_item_ids: frozenset[int]


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def open_database_read_only(path: Path) -> sqlite3.Connection:
    connection = sqlite3.connect(f"{path.resolve().as_uri()}?mode=ro", uri=True)
    connection.row_factory = sqlite3.Row
    connection.execute("PRAGMA query_only = ON")
    assert connection.execute("PRAGMA query_only").fetchone()[0] == 1
    return connection


def assert_schema(connection: sqlite3.Connection) -> None:
    table_names = {
        row[0]
        for row in connection.execute(
            "SELECT name FROM sqlite_schema WHERE type = 'table'"
        )
    }
    missing_tables = sorted(REQUIRED_COLUMNS.keys() - table_names)
    assert not missing_tables, f"missing required tables: {', '.join(missing_tables)}"
    for table_name, required_columns in REQUIRED_COLUMNS.items():
        actual_columns = {
            row[1]
            for row in connection.execute(f'PRAGMA table_info("{table_name}")')
        }
        missing_columns = sorted(required_columns - actual_columns)
        assert not missing_columns, (
            f"table {table_name} is missing required columns: "
            f"{', '.join(missing_columns)}"
        )


def row_sort_key(row: dict[str, Any]) -> str:
    return json.dumps(row, sort_keys=True, separators=(",", ":"), default=str)


def load_rows(
    connection: sqlite3.Connection, table_name: str
) -> tuple[dict[str, Any], ...]:
    assert table_name in REQUIRED_COLUMNS
    rows = tuple(dict(row) for row in connection.execute(f'SELECT * FROM "{table_name}"'))
    return tuple(sorted(rows, key=row_sort_key))


def index_rows(
    rows: Iterable[dict[str, Any]], key: str
) -> dict[int, dict[str, Any]]:
    row_list = tuple(rows)
    indexed = {int(row[key]): row for row in row_list}
    assert len(indexed) == len(row_list), f"duplicate values in key column {key}"
    return indexed


def group_rows(
    rows: Iterable[dict[str, Any]], key: str
) -> dict[int, tuple[dict[str, Any], ...]]:
    grouped: defaultdict[int, list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        grouped[int(row[key])].append(row)
    return {
        group_key: tuple(sorted(grouped[group_key], key=row_sort_key))
        for group_key in sorted(grouped)
    }


def parse_id_list(value: Any) -> tuple[int, ...]:
    return tuple(
        sorted(int(token) for token in str(value or "").replace(",", " ").split())
    )


def enabled(value: Any) -> bool:
    return str(value) != "0"


def class_floor(mask: int) -> int | None:
    if mask == 0 or mask & (1 << 1):
        return 1
    if mask & sum(1 << class_id for class_id in range(2, 6)):
        return 5
    return None


def source_sort_key(source: ItemSourcePath) -> tuple[Any, ...]:
    return (
        source.source_kind,
        source.item_id,
        source.npc_id if source.npc_id is not None else -1,
        source.quest_id if source.quest_id is not None else -1,
        source.combination_id if source.combination_id is not None else -1,
        source.map_ids,
        source.eligible_map_ids,
        source.ingredient_ids,
        source.quantity if source.quantity is not None else -1,
        source.drop_rate if source.drop_rate is not None else -1.0,
        source.vendor_slot if source.vendor_slot is not None else -1,
        source.effective_level if source.effective_level is not None else -1,
        source.exclusion_reason or "",
    )


def load_settings(path: Path) -> dict[str, Any]:
    text = "\n".join(line.split("//", 1)[0] for line in path.read_text().splitlines())
    return json.loads(text)


def index_class_info(rows: Iterable[dict[str, Any]]) -> dict[tuple[int, int], dict[str, Any]]:
    row_list = tuple(rows)
    indexed = {(int(row["class_id"]), int(row["level"])): row for row in row_list}
    assert len(indexed) == len(row_list)
    return dict(sorted(indexed.items()))


def compute_effective_levels(
    maps: dict[int, dict[str, Any]],
    items: dict[int, dict[str, Any]],
    quests: dict[int, dict[str, Any]],
    npcs: dict[int, dict[str, Any]],
    combinations: dict[int, dict[str, Any]],
    npc_eligible_map_ids: dict[int, tuple[int, ...]],
    quest_npc_ids: dict[int, tuple[int, ...]],
    quest_requirements: dict[int, tuple[dict[str, Any], ...]],
    item_sources: dict[int, tuple[ItemSourcePath, ...]],
    scoped_item_ids: frozenset[int],
) -> tuple[dict[int, int], dict[int, int], dict[int, tuple[ItemSourcePath, ...]]]:
    def map_level(map_ids: Iterable[int]) -> int | None:
        levels = [max(1, int(maps[map_id]["min_level"])) for map_id in map_ids]
        return min(levels) if levels else None

    def item_floor(item_id: int) -> int | None:
        item = items[item_id]
        availability = class_floor(int(item["class_restrictions"]))
        return max(1, availability, int(item["min_level"])) if availability is not None else None

    def path_level(source: ItemSourcePath, item_id: int, item_levels: dict[int, int], quest_levels: dict[int, int]) -> int | None:
        if not source.eligible:
            return None
        floor = item_floor(item_id)
        if floor is None:
            return None
        if source.source_kind == "drop" and source.npc_id is not None:
            availability = map_level(source.eligible_map_ids)
            return max(floor, int(npcs[source.npc_id]["npc_level"]), availability) if availability is not None else None
        if source.source_kind == "vendor" and source.npc_id is not None:
            availability = map_level(source.eligible_map_ids)
            return max(floor, availability) if availability is not None else None
        if source.source_kind == "quest" and source.quest_id is not None:
            level = quest_levels.get(source.quest_id)
            return max(floor, level) if level is not None else None
        if source.source_kind == "craft" and source.combination_id is not None:
            combination = combinations[source.combination_id]
            recipe_floor = class_floor(int(combination["class_restrictions"]))
            if recipe_floor is None or not all(ingredient in item_levels for ingredient in source.ingredient_ids):
                return None
            return max(floor, 1, recipe_floor, int(combination["min_level"]), *(item_levels[ingredient] for ingredient in source.ingredient_ids))
        return None

    def quest_level(quest_id: int, item_levels: dict[int, int], quest_levels: dict[int, int]) -> int | None:
        quest = quests[quest_id]
        if int(quest["min_experience"]) != 0 or int(quest["min_level"]) > MAX_LEVEL:
            return None
        giver_levels = [availability for npc_id in quest_npc_ids.get(quest_id, ()) if (availability := map_level(npc_eligible_map_ids.get(npc_id, ()))) is not None]
        availability_floor = class_floor(int(quest["class_restrictions"]))
        if not giver_levels or availability_floor is None:
            return None
        levels = [max(1, availability_floor, int(quest["min_level"])), min(giver_levels)]
        for prerequisite_id in parse_id_list(quest["prerequisite_quests"]):
            assert prerequisite_id in quests
            if prerequisite_id not in quest_levels:
                return None
            levels.append(quest_levels[prerequisite_id])
        for requirement in quest_requirements.get(quest_id, ()):
            requirement_type = int(requirement["requirement_type"])
            target_id = int(requirement["requirement_value"])
            if requirement_type == QUEST_REQUIREMENT_ITEM:
                assert target_id in items
                if target_id not in item_levels:
                    return None
                levels.append(item_levels[target_id])
            elif requirement_type == QUEST_REQUIREMENT_KILL:
                assert target_id in npcs
                target = npcs[target_id]
                availability = map_level(npc_eligible_map_ids.get(target_id, ()))
                if availability is None or int(target["npc_type"]) != NPC_TYPE_MONSTER or enabled(target["invincible"]):
                    return None
                levels.append(max(availability, int(target["npc_level"])))
            elif requirement_type == QUEST_REQUIREMENT_TALK_TO_NPC:
                assert target_id in npcs
                availability = map_level(npc_eligible_map_ids.get(target_id, ()))
                if availability is None:
                    return None
                levels.append(availability)
        return max(levels)

    item_levels: dict[int, int] = {}
    quest_levels: dict[int, int] = {}
    for _ in range(10000):
        changed = False
        for item_id in sorted(scoped_item_ids):
            candidates = [level for source in item_sources.get(item_id, ()) if (level := path_level(source, item_id, item_levels, quest_levels)) is not None]
            if candidates and item_levels.get(item_id) != min(candidates):
                item_levels[item_id] = min(candidates)
                changed = True
        for quest_id in sorted(quests):
            level = quest_level(quest_id, item_levels, quest_levels)
            if level is not None and quest_levels.get(quest_id) != level:
                quest_levels[quest_id] = level
                changed = True
        if not changed:
            break
    else:
        raise AssertionError("effective-level fixed point did not converge")

    annotated: dict[int, tuple[ItemSourcePath, ...]] = {}
    for item_id in sorted(item_sources):
        paths = []
        for source in item_sources[item_id]:
            effective_level = path_level(source, item_id, item_levels, quest_levels)
            if source.eligible and effective_level is None:
                source = replace(source, eligible=False, exclusion_reason=f"{source.source_kind}_unavailable")
            paths.append(replace(source, effective_level=effective_level))
        annotated[item_id] = tuple(sorted(paths, key=source_sort_key))
    return dict(sorted(item_levels.items())), dict(sorted(quest_levels.items())), annotated


def compute_npc_metrics(
    npcs: dict[int, dict[str, Any]],
    npc_ids: Iterable[int],
    class_info: dict[tuple[int, int], dict[str, Any]],
    experience_modifier: float,
) -> dict[int, NPCMetrics]:
    stat_pairs = {
        "hp": "npc_hp",
        "mp": "npc_mp",
        "sp": "npc_sp",
        "ac": "stat_ac",
        "strength": "stat_str",
        "stamina": "stat_sta",
        "dexterity": "stat_dex",
        "intelligence": "stat_int",
        "res_fire": "res_fire",
        "res_water": "res_water",
        "res_spirit": "res_spirit",
        "res_air": "res_air",
        "res_earth": "res_earth",
        "hp_static_regen": "hp_static_regen",
        "mp_static_regen": "mp_static_regen",
    }
    player_thresholds = {
        level: int(row["level_up_exp"])
        for (class_id, level), row in class_info.items()
        if class_id == 2
    }
    metrics: dict[int, NPCMetrics] = {}
    for npc_id in sorted(npc_ids):
        npc = npcs[npc_id]
        level = int(npc["npc_level"])
        baseline = class_info[(int(npc["class_id"]), level)]
        values = {
            name: int(npc[column]) + int(baseline[column if column != "npc_hp" and column != "npc_mp" and column != "npc_sp" else {"npc_hp": "player_hp", "npc_mp": "player_mp", "npc_sp": "player_sp"}[column]])
            for name, column in stat_pairs.items()
        }
        hp_percent_regen = float(npc["hp_percent_regen"]) + float(baseline["hp_percent_regen"])
        mp_percent_regen = float(npc["mp_percent_regen"]) + float(baseline["mp_percent_regen"])
        haste = float(baseline["haste"])
        spell_damage = float(baseline["spell_damage"])
        spell_crit = float(baseline["spell_crit"])
        melee_damage = float(baseline["melee_damage"])
        melee_crit = float(baseline["melee_crit"])
        damage_reduction = float(baseline["damage_reduce"])
        same_level_hit = values["strength"] + int(npc["weapon_damage"]) + level
        attacks_per_second = 1.0 / float(npc["attack_speed"]) if float(npc["attack_speed"]) > 0 and int(npc["attack_range"]) > 0 else 0.0
        pressure = same_level_hit * attacks_per_second * (1 + melee_damage) * (1 + melee_crit)
        raw_experience = int(npc["experience"])
        live_experience = int(raw_experience * experience_modifier)
        incremental_level_xp = None
        if level < MAX_LEVEL and level in player_thresholds:
            previous = player_thresholds.get(level - 1, 0)
            incremental_level_xp = player_thresholds[level] - previous
        metrics[npc_id] = NPCMetrics(
            npc_id=npc_id,
            level=level,
            hp=values["hp"],
            mp=values["mp"],
            sp=values["sp"],
            ac=values["ac"],
            strength=values["strength"],
            stamina=values["stamina"],
            dexterity=values["dexterity"],
            intelligence=values["intelligence"],
            res_fire=values["res_fire"],
            res_water=values["res_water"],
            res_spirit=values["res_spirit"],
            res_air=values["res_air"],
            res_earth=values["res_earth"],
            hp_percent_regen=hp_percent_regen,
            hp_static_regen=values["hp_static_regen"],
            mp_percent_regen=mp_percent_regen,
            mp_static_regen=values["mp_static_regen"],
            haste=haste,
            spell_damage=spell_damage,
            spell_crit=spell_crit,
            melee_damage=melee_damage,
            melee_crit=melee_crit,
            damage_reduction=damage_reduction,
            same_level_hit=same_level_hit,
            attacks_per_second=attacks_per_second,
            pressure=pressure,
            xp_per_hp=(raw_experience / values["hp"] if values["hp"] > 0 else None),
            incremental_level_xp=incremental_level_xp,
            live_experience=live_experience,
            live_kills=(incremental_level_xp / live_experience if incremental_level_xp is not None and live_experience > 0 else None),
        )
    return metrics


def build_report_data(connection: sqlite3.Connection) -> ReportData:
    assert_schema(connection)
    table_rows = {
        table_name: load_rows(connection, table_name)
        for table_name in REQUIRED_COLUMNS
    }
    maps = index_rows(table_rows["maps"], "map_id")
    npcs = index_rows(table_rows["npc_templates"], "npc_id")
    items = index_rows(table_rows["item_templates"], "item_template_id")
    quests = index_rows(table_rows["quests"], "id")
    combinations = index_rows(table_rows["combinations"], "combination_id")
    classes = index_rows(table_rows["classes"], "class_id")
    class_info = index_class_info(table_rows["class_info"])
    spells = index_rows(table_rows["spells"], "spell_id")
    spell_effects = index_rows(table_rows["spell_effects"], "spell_effect_id")
    settings = load_settings(DEFAULT_SETTINGS)
    experience_modifier = float(settings["ExperienceModifier"])
    assert float(settings["ItemTitleChancePercent"]) == 0.5
    assert float(settings["ItemSurnameChancePercent"]) == 0.5

    spawn_maps: defaultdict[int, set[int]] = defaultdict(set)
    for spawn in table_rows["npc_spawns"]:
        npc_id = int(spawn["npc_id"])
        map_id = int(spawn["map_id"])
        assert npc_id in npcs, f"spawn references unknown NPC {npc_id}"
        assert map_id in maps, f"spawn references unknown map {map_id}"
        spawn_maps[npc_id].add(map_id)
    npc_map_ids = {npc_id: tuple(sorted(map_ids)) for npc_id, map_ids in spawn_maps.items()}
    npc_eligible_map_ids = {
        npc_id: tuple(
            map_id
            for map_id in map_ids
            if int(maps[map_id]["min_experience"]) == 0
        )
        for npc_id, map_ids in npc_map_ids.items()
    }
    spawned_npc_ids = frozenset(npc_map_ids)
    eligible_spawned_npc_ids = frozenset(
        npc_id for npc_id, map_ids in npc_eligible_map_ids.items() if map_ids
    )
    gated_only_npc_ids = spawned_npc_ids - eligible_spawned_npc_ids

    quest_npcs: defaultdict[int, set[int]] = defaultdict(set)
    for npc_id, npc in npcs.items():
        for quest_id in parse_id_list(npc["quest_ids"]):
            assert quest_id in quests, f"NPC {npc_id} references unknown quest {quest_id}"
            quest_npcs[quest_id].add(npc_id)
    quest_npc_ids = {
        quest_id: tuple(sorted(npc_ids)) for quest_id, npc_ids in quest_npcs.items()
    }

    quest_requirements = group_rows(table_rows["quest_requirements"], "quest_id")
    quest_rewards = group_rows(table_rows["quest_rewards"], "quest_id")
    for quest_id in set(quest_requirements) | set(quest_rewards):
        assert quest_id in quests, f"quest data references unknown quest {quest_id}"

    combination_requirements_lists: defaultdict[int, list[int]] = defaultdict(list)
    for row in table_rows["combination_item_required"]:
        combination_id = int(row["combination_id"])
        item_id = int(row["item_template_id"])
        assert combination_id in combinations, (
            f"ingredient references unknown combination {combination_id}"
        )
        assert item_id in items, f"combination references unknown item {item_id}"
        combination_requirements_lists[combination_id].append(item_id)
    combination_result_lists: defaultdict[int, list[int]] = defaultdict(list)
    for row in table_rows["combination_item_results"]:
        combination_id = int(row["combination_id"])
        item_id = int(row["item_template_id"])
        assert combination_id in combinations, (
            f"result references unknown combination {combination_id}"
        )
        assert item_id in items, f"combination references unknown item {item_id}"
        combination_result_lists[combination_id].append(item_id)
    combination_requirements = {
        combination_id: tuple(item_ids)
        for combination_id, item_ids in combination_requirements_lists.items()
    }
    combination_results = {
        combination_id: tuple(item_ids)
        for combination_id, item_ids in combination_result_lists.items()
    }
    for combination_id in combinations:
        assert combination_requirements.get(combination_id), (
            f"combination {combination_id} has no ingredients"
        )
        assert combination_results.get(combination_id), (
            f"combination {combination_id} has no results"
        )

    scoped_item_ids = frozenset(
        item_id
        for item_id, item in items.items()
        if int(item["min_experience"]) == 0 and int(item["min_level"]) <= MAX_LEVEL
    )
    sources: defaultdict[int, list[ItemSourcePath]] = defaultdict(list)

    for source_kind, table_name in (
        ("drop", "npc_drops"),
        ("vendor", "npc_vendor_items"),
    ):
        for row in table_rows[table_name]:
            npc_id = int(row["npc_template_id"])
            item_id = int(row["item_template_id"])
            assert npc_id in npcs, f"{source_kind} references unknown NPC {npc_id}"
            assert item_id in items, f"{source_kind} references unknown item {item_id}"
            map_ids = npc_map_ids.get(npc_id, ())
            eligible_map_ids = npc_eligible_map_ids.get(npc_id, ())
            exclusion_reason = None
            if npc_id == PHAT_LEWTZ_NPC_ID:
                exclusion_reason = "phat_lewtz"
            elif enabled(npcs[npc_id]["credit_dealer"]):
                exclusion_reason = "credit_dealer"
            elif not map_ids:
                exclusion_reason = "unspawned_npc"
            elif not eligible_map_ids:
                exclusion_reason = "xp_gated_maps"
            elif source_kind == "drop" and int(npcs[npc_id]["npc_type"]) != NPC_TYPE_MONSTER:
                exclusion_reason = "non_monster_drop"
            elif source_kind == "drop" and enabled(npcs[npc_id]["invincible"]):
                exclusion_reason = "invincible_drop"
            elif source_kind == "drop" and float(row["droprate"]) <= 0:
                exclusion_reason = "zero_rate_drop"
            sources[item_id].append(
                ItemSourcePath(
                    item_id=item_id,
                    source_kind=source_kind,
                    eligible=exclusion_reason is None,
                    exclusion_reason=exclusion_reason,
                    npc_id=npc_id,
                    map_ids=map_ids,
                    eligible_map_ids=eligible_map_ids,
                    quantity=int(row["stack"]),
                    drop_rate=(
                        float(row["droprate"]) if source_kind == "drop" else None
                    ),
                    vendor_slot=(
                        int(row["slot"]) if source_kind == "vendor" else None
                    ),
                )
            )

    for quest_id, rewards in quest_rewards.items():
        quest = quests[quest_id]
        attached_npc_ids = quest_npc_ids.get(quest_id, ())
        map_ids = tuple(
            sorted(
                {
                    map_id
                    for npc_id in attached_npc_ids
                    for map_id in npc_map_ids.get(npc_id, ())
                }
            )
        )
        eligible_map_ids = tuple(
            map_id
            for map_id in map_ids
            if int(maps[map_id]["min_experience"]) == 0
        )
        exclusion_reason = None
        if int(quest["min_experience"]) != 0:
            exclusion_reason = "quest_min_experience"
        elif int(quest["min_level"]) > MAX_LEVEL:
            exclusion_reason = "quest_level_above_scope"
        elif not attached_npc_ids:
            exclusion_reason = "unattached_quest"
        elif not map_ids:
            exclusion_reason = "unspawned_quest_npc"
        elif not eligible_map_ids:
            exclusion_reason = "xp_gated_quest_maps"
        for reward in rewards:
            if int(reward["reward_type"]) != QUEST_REWARD_ITEM:
                continue
            item_id = int(reward["long_value"])
            assert item_id in items, f"quest {quest_id} rewards unknown item {item_id}"
            sources[item_id].append(
                ItemSourcePath(
                    item_id=item_id,
                    source_kind="quest",
                    eligible=exclusion_reason is None,
                    exclusion_reason=exclusion_reason,
                    quest_id=quest_id,
                    map_ids=map_ids,
                    eligible_map_ids=eligible_map_ids,
                    quantity=int(reward["long_value2"]),
                )
            )

    for combination_id in sorted(combinations):
        combination = combinations[combination_id]
        ingredient_ids = combination_requirements[combination_id]
        exclusion_reason = None
        if int(combination["min_experience"]) != 0:
            exclusion_reason = "combination_min_experience"
        elif int(combination["min_level"]) > MAX_LEVEL:
            exclusion_reason = "combination_level_above_scope"
        for item_id in combination_results[combination_id]:
            sources[item_id].append(
                ItemSourcePath(
                    item_id=item_id,
                    source_kind="craft",
                    eligible=exclusion_reason is None,
                    exclusion_reason=exclusion_reason,
                    combination_id=combination_id,
                    ingredient_ids=ingredient_ids,
                )
            )

    item_sources = {
        item_id: tuple(sorted(sources[item_id], key=source_sort_key))
        for item_id in sorted(sources)
    }
    effective_item_levels, effective_quest_levels, item_sources = compute_effective_levels(
        maps,
        items,
        quests,
        npcs,
        combinations,
        npc_eligible_map_ids,
        quest_npc_ids,
        quest_requirements,
        item_sources,
        scoped_item_ids,
    )
    included_item_ids = frozenset(effective_item_levels)
    excluded_item_reasons: dict[int, str] = {}
    for item_id, item in items.items():
        if int(item["min_experience"]) != 0:
            excluded_item_reasons[item_id] = "item_min_experience"
        elif int(item["min_level"]) > MAX_LEVEL:
            excluded_item_reasons[item_id] = "item_level_above_scope"
        elif item_id not in included_item_ids:
            paths = item_sources.get(item_id, ())
            if paths and all(
                path.source_kind == "vendor"
                and path.exclusion_reason in {"phat_lewtz", "credit_dealer"}
                for path in paths
            ):
                excluded_item_reasons[item_id] = "excluded_vendor_only"
            elif paths and all(path.exclusion_reason == "xp_gated_maps" for path in paths):
                excluded_item_reasons[item_id] = "xp_gated_sources_only"
            else:
                excluded_item_reasons[item_id] = "no_eligible_source"

    class_spell_grants_list: list[SpellGrant] = []
    for row in table_rows["classes_levelup_spells"]:
        class_id = int(row["class_id"])
        spell_id = int(row["spell_id"])
        level = max(5, int(row["level"]))
        assert class_id in classes, f"spell grant references unknown class {class_id}"
        assert spell_id in spells, f"spell grant references unknown spell {spell_id}"
        class_spell_grants_list.append(
            SpellGrant(
                spell_id=spell_id,
                source_kind="class_level",
                level=level,
                eligible=level <= MAX_LEVEL,
                class_id=class_id,
            )
        )

    scroll_spell_grants_list: list[SpellGrant] = []
    for item_id, item in items.items():
        spell_id = int(item["learn_spell_id"])
        if spell_id == 0:
            continue
        assert int(item["item_usetype"]) == ITEM_USE_TYPE_SCROLL, (
            f"spell-learning item {item_id} is not a scroll"
        )
        assert spell_id in spells, f"scroll {item_id} references unknown spell {spell_id}"
        scroll_spell_grants_list.append(
            SpellGrant(
                spell_id=spell_id,
                source_kind="scroll",
                level=max(
                    5,
                    effective_item_levels.get(item_id, max(1, int(item["min_level"]))),
                ),
                eligible=item_id in effective_item_levels,
                item_id=item_id,
            )
        )

    quest_spell_grants_list: list[SpellGrant] = []
    for quest_id, rewards in quest_rewards.items():
        quest = quests[quest_id]
        for reward in rewards:
            if int(reward["reward_type"]) != QUEST_REWARD_LEARN_SPELL:
                continue
            spell_id = int(reward["long_value"])
            assert spell_id in spells, f"quest {quest_id} references unknown spell {spell_id}"
            eligible = quest_id in effective_quest_levels
            quest_spell_grants_list.append(
                SpellGrant(
                    spell_id=spell_id,
                    source_kind="quest",
                    level=max(
                        5,
                        effective_quest_levels.get(quest_id, max(1, int(quest["min_level"]))),
                    ),
                    eligible=eligible,
                    quest_id=quest_id,
                )
            )

    monster_ids = {
        npc_id for npc_id, npc in npcs.items() if int(npc["npc_type"]) == NPC_TYPE_MONSTER
    }
    leveling_npc_ids = frozenset(
        npc_id
        for npc_id in monster_ids & eligible_spawned_npc_ids
        if int(npcs[npc_id]["npc_level"]) <= MAX_LEVEL
    )
    transition_npc_ids = frozenset(
        npc_id
        for npc_id in leveling_npc_ids
        if int(npcs[npc_id]["npc_level"]) == MAX_LEVEL
    )
    endgame_npc_ids = frozenset(
        npc_id
        for npc_id in monster_ids & gated_only_npc_ids
        if int(npcs[npc_id]["npc_level"]) == MAX_LEVEL
    )

    excluded_vendor_only_gear_ids = frozenset(
        item_id
        for item_id in scoped_item_ids
        if int(items[item_id]["item_usetype"])
        in {ITEM_USE_TYPE_ARMOR, ITEM_USE_TYPE_WEAPON}
        and excluded_item_reasons.get(item_id) == "excluded_vendor_only"
    )
    retained_dual_source_item_ids = frozenset(
        item_id
        for item_id in included_item_ids
        if any(path.eligible for path in item_sources.get(item_id, ()))
        and any(not path.eligible for path in item_sources.get(item_id, ()))
    )
    npc_metrics = compute_npc_metrics(
        npcs,
        leveling_npc_ids | endgame_npc_ids,
        class_info,
        experience_modifier,
    )

    data = ReportData(
        maps=maps,
        npcs=npcs,
        items=items,
        item_titles=table_rows["item_titles"],
        item_surnames=table_rows["item_surnames"],
        quests=quests,
        combinations=combinations,
        classes=classes,
        class_info=class_info,
        spells=spells,
        spell_effects=spell_effects,
        experience_modifier=experience_modifier,
        npc_map_ids=npc_map_ids,
        npc_eligible_map_ids=npc_eligible_map_ids,
        quest_npc_ids=quest_npc_ids,
        quest_requirements=quest_requirements,
        quest_rewards=quest_rewards,
        combination_requirements=combination_requirements,
        combination_results=combination_results,
        item_sources=item_sources,
        effective_item_levels=effective_item_levels,
        effective_quest_levels=effective_quest_levels,
        npc_metrics=npc_metrics,
        scoped_item_ids=scoped_item_ids,
        included_item_ids=included_item_ids,
        excluded_item_reasons=excluded_item_reasons,
        spawned_npc_ids=spawned_npc_ids,
        eligible_spawned_npc_ids=eligible_spawned_npc_ids,
        gated_only_npc_ids=gated_only_npc_ids,
        leveling_npc_ids=leveling_npc_ids,
        transition_npc_ids=transition_npc_ids,
        endgame_npc_ids=endgame_npc_ids,
        class_spell_grants=tuple(
            sorted(
                class_spell_grants_list,
                key=lambda grant: (grant.class_id or -1, grant.level, grant.spell_id),
            )
        ),
        scroll_spell_grants=tuple(
            sorted(scroll_spell_grants_list, key=lambda grant: (grant.level, grant.item_id or -1))
        ),
        quest_spell_grants=tuple(
            sorted(quest_spell_grants_list, key=lambda grant: (grant.level, grant.quest_id or -1))
        ),
        excluded_vendor_only_gear_ids=excluded_vendor_only_gear_ids,
        retained_dual_source_item_ids=retained_dual_source_item_ids,
    )
    assert_extraction_invariants(data)
    return data


def assert_extraction_invariants(data: ReportData) -> None:
    assert data.excluded_vendor_only_gear_ids.isdisjoint(data.included_item_ids)
    assert data.retained_dual_source_item_ids <= data.included_item_ids
    assert data.included_item_ids <= data.scoped_item_ids
    assert data.transition_npc_ids.isdisjoint(data.endgame_npc_ids)
    for npc_id in data.transition_npc_ids:
        assert data.npc_eligible_map_ids[npc_id]
    for npc_id in data.endgame_npc_ids:
        assert npc_id in data.gated_only_npc_ids
    for item_paths in data.item_sources.values():
        for path in item_paths:
            if not path.eligible or path.source_kind not in {"drop", "vendor"}:
                continue
            assert path.npc_id is not None
            assert path.npc_id != PHAT_LEWTZ_NPC_ID
            assert not enabled(data.npcs[path.npc_id]["credit_dealer"])
            if path.source_kind == "drop":
                assert int(data.npcs[path.npc_id]["npc_type"]) == NPC_TYPE_MONSTER
                assert not enabled(data.npcs[path.npc_id]["invincible"])
                assert path.drop_rate is not None and path.drop_rate > 0
    equipment_ids = {
        item_id
        for item_id in data.scoped_item_ids
        if int(data.items[item_id]["item_usetype"])
        in {ITEM_USE_TYPE_ARMOR, ITEM_USE_TYPE_WEAPON}
    }
    valid_equipment_ids = equipment_ids & data.included_item_ids
    assert valid_equipment_ids <= equipment_ids
    assert all(level >= 1 for level in data.effective_item_levels.values())
    assert all(level >= 1 for level in data.effective_quest_levels.values())
    assert set(data.effective_item_levels) == set(data.included_item_ids)
    assert all(
        grant.level >= 5
        for grant in data.class_spell_grants
        + data.scroll_spell_grants
        + data.quest_spell_grants
    )
    for npc_id, metrics in data.npc_metrics.items():
        npc = data.npcs[npc_id]
        baseline = data.class_info[(int(npc["class_id"]), int(npc["npc_level"]))]
        assert metrics.hp == int(npc["npc_hp"]) + int(baseline["player_hp"])
        assert metrics.mp == int(npc["npc_mp"]) + int(baseline["player_mp"])
        assert metrics.ac == int(npc["stat_ac"]) + int(baseline["stat_ac"])
    for item_id in data.included_item_ids:
        eligible_paths = tuple(path for path in data.item_sources[item_id] if path.eligible)
        assert eligible_paths
        assert data.effective_item_levels[item_id] == min(path.effective_level for path in eligible_paths)
        for path in data.item_sources[item_id]:
            if not path.eligible or path.source_kind not in {"drop", "vendor"}:
                continue
            assert path.npc_id in data.spawned_npc_ids
            assert path.eligible_map_ids
            assert all(
                int(data.maps[map_id]["min_experience"]) == 0
                for map_id in path.eligible_map_ids
            )


def summary(data: ReportData, database_digest: str) -> dict[str, Any]:
    source_counts = Counter(
        path.source_kind
        for item_id in data.included_item_ids
        for path in data.item_sources[item_id]
        if path.eligible
    )
    excluded_counts = Counter(data.excluded_item_reasons.values())
    equipment_ids = {
        item_id
        for item_id in data.scoped_item_ids
        if int(data.items[item_id]["item_usetype"])
        in {ITEM_USE_TYPE_ARMOR, ITEM_USE_TYPE_WEAPON}
    }
    valid_equipment_ids = equipment_ids & data.included_item_ids
    return {
        "database_sha256": database_digest,
        "experience_modifier": data.experience_modifier,
        "maps": {
            "total": len(data.maps),
            "xp_gated": sum(
                int(map_row["min_experience"]) > 0 for map_row in data.maps.values()
            ),
        },
        "npcs": {
            "total": len(data.npcs),
            "spawned": len(data.spawned_npc_ids),
            "spawned_on_leveling_maps": len(data.eligible_spawned_npc_ids),
            "gated_only": len(data.gated_only_npc_ids),
            "leveling_monsters": len(data.leveling_npc_ids),
            "level_50_transition_monsters": len(data.transition_npc_ids),
            "level_50_endgame_monsters": len(data.endgame_npc_ids),
            "metrics": len(data.npc_metrics),
        },
        "items": {
            "total": len(data.items),
            "in_level_xp_scope": len(data.scoped_item_ids),
            "included": len(data.included_item_ids),
            "excluded": len(data.items) - len(data.included_item_ids),
            "no_xp_equipment": len(equipment_ids),
            "valid_source_equipment": len(valid_equipment_ids),
            "pre_50_equipment": sum(
                data.effective_item_levels[item_id] < MAX_LEVEL
                for item_id in valid_equipment_ids
            ),
            "first_available_at_50_equipment": sum(
                data.effective_item_levels[item_id] == MAX_LEVEL
                for item_id in valid_equipment_ids
            ),
            "no_valid_source_equipment": len(equipment_ids - valid_equipment_ids),
            "eligible_sources": dict(sorted(source_counts.items())),
            "excluded_reasons": dict(sorted(excluded_counts.items())),
            "excluded_vendor_only_gear": len(data.excluded_vendor_only_gear_ids),
            "retained_dual_source": len(data.retained_dual_source_item_ids),
        },
        "quests": {
            "total": len(data.quests),
            "attached": len(data.quest_npc_ids),
            "reachable": len(data.effective_quest_levels),
            "item_reward_paths": sum(
                path.source_kind == "quest"
                for item_paths in data.item_sources.values()
                for path in item_paths
            ),
        },
        "crafting": {
            "combinations": len(data.combinations),
            "reachable_paths": sum(
                path.source_kind == "craft" and path.eligible
                for item_paths in data.item_sources.values()
                for path in item_paths
            ),
        },
        "spells": {
            "total": len(data.spells),
            "class_grants": len(data.class_spell_grants),
            "scroll_grants": len(data.scroll_spell_grants),
            "eligible_scroll_grants": sum(
                grant.eligible for grant in data.scroll_spell_grants
            ),
            "quest_grants": len(data.quest_spell_grants),
            "eligible_quest_grants": sum(
                grant.eligible for grant in data.quest_spell_grants
            ),
        },
    }


def generate(database_path: Path) -> tuple[ReportData, dict[str, Any]]:
    database_path = database_path.resolve()
    assert database_path.is_file(), f"database not found: {database_path}"
    wal_path = Path(f"{database_path}-wal")
    before_digest = file_sha256(database_path)
    before_wal_digest = file_sha256(wal_path) if wal_path.exists() else None
    with open_database_read_only(database_path) as connection:
        connection.execute("BEGIN")
        data = build_report_data(connection)
        connection.rollback()
    after_digest = file_sha256(database_path)
    after_wal_digest = file_sha256(wal_path) if wal_path.exists() else None
    assert before_digest == after_digest, "database changed while generating report data"
    assert before_wal_digest == after_wal_digest, "database WAL changed while generating report data"
    return data, summary(data, after_digest)


SLOT_NAMES = {0: "Helmet", 1: "Shield", 2: "One-handed", 3: "Two-handed", 4: "Ring", 5: "Necklace", 6: "Pauldrons", 7: "Cloak", 8: "Belt", 9: "Gloves", 10: "Chest", 11: "Pants", 12: "Shoes", 13: "Mount", 20: "Misc"}


def item_budget(item: dict[str, Any]) -> float:
    return int(item["player_hp"]) / 25 + int(item["player_mp"]) / 25 + int(item["player_sp"]) / 25 + int(item["stat_ac"]) / 10 + sum(int(item[column]) for column in ("stat_str", "stat_sta", "stat_dex", "stat_int")) + sum(int(item[column]) for column in ("res_fire", "res_water", "res_spirit", "res_air", "res_earth")) / 5


def weapon_throughput(item: dict[str, Any]) -> float:
    delay = int(item["weapon_delay"])
    return 10 * (int(item["weapon_damage"]) + int(item["stat_str"])) / delay if delay > 0 else 0.0


def display(value: Any) -> str:
    if value is None:
        return "—"
    return f"{value:.3f}" if isinstance(value, float) else str(value)


def report_table(table_id: str, title: str, headers: Sequence[str], rows: Iterable[Sequence[Any]]) -> str:
    headings = "".join(f'<th tabindex="0">{html.escape(header)}</th>' for header in headers)
    body = "".join("<tr>" + "".join(f"<td>{html.escape(display(value))}</td>" for value in row) + "</tr>" for row in rows)
    escaped_title = html.escape(title)
    return f'<article class="panel"><h3>{escaped_title}</h3><input class="search" data-table="{table_id}" placeholder="Search {escaped_title}" aria-label="Search {escaped_title}"><div class="table-wrap"><table id="{table_id}"><thead><tr>{headings}</tr></thead><tbody>{body}</tbody></table></div></article>'


def report_chart(chart_id: str, title: str, values: Sequence[tuple[str, float]]) -> str:
    values = tuple(values[:20])
    maximum = max((value for _, value in values), default=1) or 1
    height = max(90, 45 + len(values) * 23)
    marks = []
    for index, (label, value) in enumerate(values):
        y = 28 + index * 23
        width = 480 * value / maximum
        marks.extend((f'<text x="4" y="{y + 12}">{html.escape(label)}</text>', f'<rect x="145" y="{y}" width="{width:.2f}" height="15"><title>{html.escape(label)}: {value:.3f}</title></rect>', f'<text x="{150 + width:.2f}" y="{y + 12}">{value:.2f}</text>'))
    return f'<article class="panel chart"><h3>{html.escape(title)}</h3><svg id="{chart_id}" viewBox="0 0 700 {height}" role="img" aria-label="{html.escape(title)}">{"".join(marks)}</svg></article>'


def usable_by_class(item: dict[str, Any], class_id: int) -> bool:
    mask = int(item["class_restrictions"])
    return mask == 0 or bool(mask & (1 << class_id))


def _compact_report(data: ReportData, report_summary: dict[str, Any]) -> str:
    equipment = sorted(item_id for item_id, item in data.items.items() if int(item["min_experience"]) == 0 and int(item["item_usetype"]) in {ITEM_USE_TYPE_ARMOR, ITEM_USE_TYPE_WEAPON})
    valid = [item_id for item_id in equipment if item_id in data.included_item_ids]
    classes = [class_id for class_id in sorted(data.classes) if 2 <= class_id <= 5]
    item_rows = []
    for item_id in valid:
        item = data.items[item_id]
        class_list = ", ".join(str(data.classes[class_id]["class_name"]) for class_id in classes if usable_by_class(item, class_id))
        item_rows.append((item_id, item["item_name"], SLOT_NAMES.get(int(item["item_slot"]), item["item_slot"]), class_list, item["min_level"], data.effective_item_levels[item_id], item["player_hp"], item["stat_sta"], int(item["player_hp"]) + 25 * int(item["stat_sta"]), item["player_mp"], item["stat_int"], int(item["player_mp"]) + 25 * int(item["stat_int"]), item["stat_ac"], weapon_throughput(item), item_budget(item)))
    excluded_rows = [(item_id, data.items[item_id]["item_name"], SLOT_NAMES.get(int(data.items[item_id]["item_slot"]), data.items[item_id]["item_slot"]), data.excluded_item_reasons.get(item_id, "unavailable")) for item_id in equipment if item_id not in data.included_item_ids]
    source_rows = [(item_id, data.items[item_id]["item_name"], path.source_kind, path.effective_level, path.npc_id, path.quest_id, path.combination_id, ",".join(map(str, path.ingredient_ids)), path.drop_rate) for item_id in sorted(data.item_sources) for path in data.item_sources[item_id] if path.eligible]
    slot_rows = []
    gap_rows = []
    for class_id in classes:
        for slot in sorted(SLOT_NAMES):
            levels = sorted({data.effective_item_levels[item_id] for item_id in valid if int(data.items[item_id]["item_slot"]) == slot and usable_by_class(data.items[item_id], class_id)})
            slot_rows.append((data.classes[class_id]["class_name"], SLOT_NAMES[slot], levels[0] if levels else None, levels[-1] if levels else None, len(levels)))
            gaps = [f"{left}→{right}" for left, right in zip(levels, levels[1:]) if right - left >= 10]
            if gaps or not levels:
                gap_rows.append((data.classes[class_id]["class_name"], SLOT_NAMES[slot], ", ".join(gaps) if gaps else "No valid source"))
    npc_rows = []
    endgame_rows = []
    hp_by_level: defaultdict[int, list[float]] = defaultdict(list)
    pressure_by_level: defaultdict[int, list[float]] = defaultdict(list)
    efficiency_by_level: defaultdict[int, list[float]] = defaultdict(list)
    combat_ids = []
    for npc_id in sorted(data.npc_metrics):
        npc = data.npcs[npc_id]
        metrics = data.npc_metrics[npc_id]
        map_names = ", ".join(str(data.maps[map_id]["map_name"]) for map_id in data.npc_map_ids[npc_id])
        row = (npc_id, npc["npc_name"], metrics.level, map_names, metrics.hp, metrics.ac, metrics.same_level_hit, metrics.attacks_per_second, metrics.pressure, npc["experience"], metrics.live_experience, metrics.xp_per_hp, metrics.live_kills)
        (endgame_rows if npc_id in data.endgame_npc_ids else npc_rows).append(row)
        if npc_id in data.leveling_npc_ids and not enabled(npc["invincible"]) and int(npc["experience"]) > 0:
            combat_ids.append(npc_id)
            hp_by_level[metrics.level].append(metrics.hp)
            pressure_by_level[metrics.level].append(metrics.pressure)
            if metrics.xp_per_hp is not None:
                efficiency_by_level[metrics.level].append(metrics.xp_per_hp)
    quest_rows = []
    quest_mismatches = []
    for quest_id in sorted(data.quests):
        quest = data.quests[quest_id]
        effective = data.effective_quest_levels.get(quest_id)
        gold = sum(int(reward["long_value"]) for reward in data.quest_rewards.get(quest_id, ()) if int(reward["reward_type"]) == 0)
        raw_xp = sum(int(reward["long_value"]) for reward in data.quest_rewards.get(quest_id, ()) if int(reward["reward_type"]) == 5)
        mismatch = effective - int(quest["min_level"]) if effective is not None else None
        if mismatch is not None:
            quest_mismatches.append((f"Q{quest_id}", float(abs(mismatch))))
        requirements = ", ".join(f'{int(requirement["requirement_value2"])}×{int(requirement["requirement_value"])} type {int(requirement["requirement_type"])}' for requirement in data.quest_requirements.get(quest_id, ())) or "None"
        quest_rows.append((quest_id, quest["name"], quest["min_level"], effective, mismatch, quest["repeatable"], gold, raw_xp, int(raw_xp * data.experience_modifier), requirements, quest["prerequisite_quests"] or "None"))
    grants_by_spell: defaultdict[int, list[SpellGrant]] = defaultdict(list)
    for grant in data.class_spell_grants + data.scroll_spell_grants + data.quest_spell_grants:
        grants_by_spell[grant.spell_id].append(grant)
    spell_rows = []
    spell_bands: Counter[int] = Counter()
    for spell_id in sorted(data.spells):
        spell = data.spells[spell_id]
        grants = [grant for grant in grants_by_spell.get(spell_id, ()) if grant.eligible]
        unlock = min((grant.level for grant in grants), default=None)
        if unlock is not None:
            spell_bands[(unlock - 1) // 5 * 5 + 1] += 1
        effect = data.spell_effects[int(spell["spell_effect_id"])]
        family = re.sub(r"\s+(?:I+|\d+)$", "", str(spell["spell_name"])).strip()
        sources = ", ".join(f"{grant.source_kind}@{grant.level}" for grant in sorted(grants, key=lambda value: (value.level, value.source_kind))) or "Unavailable"
        spell_rows.append((spell_id, spell["spell_name"], family, unlock, spell["spell_aether"], spell["mp_static_cost"], spell["sp_static_cost"], effect["hp_change_formula"] or effect["hp"], sources))
    missing_levels = [level for level in range(1, 50) if level not in hp_by_level]
    outliers = sorted((data.npc_metrics[npc_id].xp_per_hp or 0, npc_id) for npc_id in combat_ids)
    modifier_rate = float(load_settings(DEFAULT_SETTINGS)["ItemTitleChancePercent"]) * 100
    equipment_summary = report_summary["items"]
    findings = [
        ("Equipment scope", f"{equipment_summary['no_xp_equipment']} no-XP equipment; {equipment_summary['valid_source_equipment']} valid-source; {equipment_summary['pre_50_equipment']} pre-50; {equipment_summary['first_available_at_50_equipment']} first at 50; {equipment_summary['no_valid_source_equipment']} have no valid source."),
        ("Slot coverage", "; ".join(f"{row[1]} first {row[2] or 'none'}, last {row[3] or 'none'}" for row in slot_rows)),
        ("Monster coverage", "Missing eligible combat monster levels: " + (", ".join(map(str, missing_levels)) or "none") + "."),
        ("XP outliers", "; ".join(f'{data.npcs[npc_id]["npc_name"]} ({ratio:.3f} XP/HP)' for ratio, npc_id in outliers[:3] + outliers[-3:])),
        ("Quest mismatches", f"{sum(value > 0 for _, value in quest_mismatches)} reachable quests differ from their authored minimum after dependencies."),
        ("Random modifiers", f"Title and surname outer gates roll independently at {modifier_rate:g}%. Each applicable modifier independently enters a candidate pool using its authored chance; an empty pool applies nothing, and one successful candidate is chosen uniformly. Modifier templates require min_level >=1, so authored min_level=0 equipment is ineligible."),
    ]
    modifier_rows = [("Title", row["id"], row["name"], row["min_level"], row["item_usetype"], row["chance"], row["script_params"]) for row in data.item_titles] + [("Surname", row["id"], row["name"], row["min_level"], row["item_usetype"], row["chance"], row["script_params"]) for row in data.item_surnames]
    level_bands = Counter((data.effective_item_levels[item_id] - 1) // 5 * 5 + 1 for item_id in valid)
    weapon_bands: defaultdict[int, list[float]] = defaultdict(list)
    armor_bands: defaultdict[int, list[float]] = defaultdict(list)
    for item_id in valid:
        band = (data.effective_item_levels[item_id] - 1) // 5 * 5 + 1
        item = data.items[item_id]
        if int(item["item_usetype"]) == ITEM_USE_TYPE_WEAPON:
            weapon_bands[band].append(weapon_throughput(item))
        else:
            armor_bands[band].append(item_budget(item))
    charts = [
        report_chart("chart-item-availability", "Equipment availability", [(f"{band}-{min(50, band + 4)}", count) for band, count in sorted(level_bands.items())]),
        report_chart("chart-weapon-frontier", "Weapon throughput by band", [(str(band), max(values)) for band, values in sorted(weapon_bands.items())]),
        report_chart("chart-armor-frontier", "Armor budget by band", [(str(band), max(values)) for band, values in sorted(armor_bands.items())]),
        report_chart("chart-npc-hp", "Median NPC HP", [(str(level), median(hp_by_level[level])) for level in sorted(hp_by_level)]),
        report_chart("chart-npc-pressure", "Median NPC pressure", [(str(level), median(pressure_by_level[level])) for level in sorted(pressure_by_level)]),
        report_chart("chart-npc-xp-efficiency", "Median NPC XP/HP", [(str(level), median(efficiency_by_level[level])) for level in sorted(efficiency_by_level)]),
        report_chart("chart-quest-rewards", "Quest authored/effective mismatch", sorted(quest_mismatches, key=lambda value: (-value[1], value[0]))),
        report_chart("chart-spell-pacing", "Spell unlocks by band", [(f"{band}-{band + 4}", count) for band, count in sorted(spell_bands.items())]),
    ]
    scope_rows = [(f"{section}.{key}", value) for section, values in report_summary.items() if isinstance(values, dict) for key, value in values.items() if not isinstance(value, dict)]
    tables = [
        report_table("table-scope", "Scope summary", ("Metric", "Value"), scope_rows),
        report_table("table-items", "Included equipment", ("ID", "Item", "Slot", "Classes", "Authored", "Effective", "HP", "STA", "HP proxy", "MP", "INT", "MP proxy", "AC", "Weapon proxy", "Budget"), item_rows),
        report_table("table-excluded-items", "Excluded equipment", ("ID", "Item", "Slot", "Reason"), excluded_rows),
        report_table("table-sources", "Eligible source paths", ("Item", "Name", "Kind", "Level", "NPC", "Quest", "Recipe", "Ingredients", "Rate"), source_rows),
        report_table("table-slots", "Slot availability", ("Class", "Slot", "First", "Last", "Levels"), slot_rows),
        report_table("table-gaps", "Progression gaps", ("Class", "Slot", "Gap"), gap_rows),
        report_table("table-npcs", "Leveling NPC metrics", ("ID", "NPC", "Level", "Maps", "HP", "AC", "Hit", "APS", "Pressure", "Raw XP", "Live XP", "XP/HP", "Kills"), npc_rows),
        report_table("table-endgame", "XP-gated level-50 NPCs", ("ID", "NPC", "Level", "Maps", "HP", "AC", "Hit", "APS", "Pressure", "Raw XP", "Live XP", "XP/HP", "Kills"), endgame_rows),
        report_table("table-quests", "Quest analytics", ("ID", "Quest", "Authored", "Effective", "Mismatch", "Repeat", "Gold", "Raw XP", "Live XP", "Requirements", "Prerequisites"), quest_rows),
        report_table("table-spells", "Spell pacing", ("ID", "Spell", "Family", "Unlock", "Cooldown ms", "MP", "SP", "Effect", "Sources"), spell_rows),
        report_table("table-modifiers", "Random modifiers", ("Kind", "ID", "Name", "Min level", "Use", "Candidate chance", "Effect"), modifier_rows),
        report_table("table-findings", "Prioritized findings", ("Finding", "Evidence"), findings),
    ]
    cards = "".join(f'<article class="card"><strong>{html.escape(title)}</strong><p>{html.escape(text)}</p></article>' for title, text in findings)
    style = "body{margin:0;background:#0b1220;color:#dce8f5;font:14px system-ui,sans-serif}main{max-width:1500px;margin:auto;padding:24px}h1,h2,h3{color:#fff}.hero{background:#17345c;padding:28px;border-radius:14px}.cards,.charts{display:grid;grid-template-columns:repeat(auto-fit,minmax(350px,1fr));gap:14px}.card,.panel{background:#121d2f;border:1px solid #263753;border-radius:10px;padding:14px;margin:14px 0}.cards .card{margin:0}.formula{font-family:ui-monospace,monospace;background:#09101c;padding:10px}.search{padding:8px;background:#0b1424;color:#fff;border:1px solid #405575}.table-wrap{overflow:auto;max-height:520px;margin-top:8px}table{border-collapse:collapse;width:100%;font-size:12px}th{position:sticky;top:0;background:#21324d;cursor:pointer}th,td{padding:6px;border-bottom:1px solid #293a54;white-space:nowrap;text-align:left}svg{width:100%;background:#0c1728}svg rect{fill:#39a0ed}svg text{fill:#dce8f5;font-size:11px}"
    script = "document.querySelectorAll('.search').forEach(i=>i.addEventListener('input',()=>{const q=i.value.toLowerCase();document.querySelectorAll('#'+i.dataset.table+' tbody tr').forEach(r=>r.hidden=!r.textContent.toLowerCase().includes(q))}));document.querySelectorAll('th').forEach(h=>h.addEventListener('click',()=>{const t=h.closest('table'),b=t.tBodies[0],n=[...h.parentNode.children].indexOf(h),a=h.dataset.a!=='1';[...b.rows].sort((x,y)=>{const p=x.cells[n].textContent.trim(),q=y.cells[n].textContent.trim(),pn=Number(p),qn=Number(q);return(a?1:-1)*(!Number.isNaN(pn)&&!Number.isNaN(qn)?pn-qn:p.localeCompare(q))}).forEach(r=>b.appendChild(r));h.dataset.a=a?'1':'0'}));"
    return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Goose Leveling Balance Report</title><style>{style}</style></head><body><main><header class="hero"><h1>Goose level 1–50 balance report</h1><p>Read-only deterministic analysis of database {html.escape(report_summary["database_sha256"])}. XP-gated level-50 endgame is separate from transition content.</p></header><section id="executive"><h2>Verified findings</h2><div class="cards">{cards}</div></section><section id="guidelines" class="panel"><h2>Practical formulas</h2><p class="formula">Item = max(item/class floor, min(valid drop, vendor, reachable quest, reachable recipe)); recipe = max(1, recipe/class floor, every ingredient). Quest = max(authored/class floor, giver map, prerequisites, required items, kill NPC/map). Talk checks an eligible spawn without NPC level.</p><p class="formula">NPC exact stats = template + same-class/same-level class_info; no STA→HP or INT→MP. Hit = STR + weapon damage + level; APS = 1/attack_speed; pressure = hit × (1+melee damage) × (1+melee crit) × APS; live kills = incremental cumulative XP/(raw XP×{data.experience_modifier:g}).</p><p class="formula">Item proxies: HP+25×STA; MP+25×INT; weapon = 10×(damage+STR)/delay; budget = HP/25+MP/25+SP/25+AC/10+attributes+resistances/5.</p><p>Anchors: Mouse 30 HP/5 kills; Nagan Sentry 8,700 HP; Weak Skeleton 300 AC; quests 17/35/49 at 28/50/33.</p></section><div class="charts">{"".join(charts)}</div><section id="items"><h2>Items</h2>{"".join(tables[:6])}</section><section id="npcs"><h2>NPCs</h2>{"".join(tables[6:8])}</section><section id="quests"><h2>Quests</h2>{tables[8]}</section><section id="spells"><h2>Spells</h2>{tables[9]}</section><section id="caveats"><h2>Modifiers and caveats</h2>{"".join(tables[10:])}<p>Raw metrics and labeled authoring proxies are deliberately separate. Random titles/surnames are rolled on drops, purchases, crafting and quest rewards; min_level=0 templates fail modifier minimum 1.</p></section></main><script>{script}</script></body></html>'


class ReportStructureParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.ids: list[str] = []
        self.svg_count = 0
        self.table_count = 0

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        attributes = dict(attrs)
        if "id" in attributes and attributes["id"] is not None:
            self.ids.append(attributes["id"])
        if tag == "svg":
            self.svg_count += 1
        elif tag == "table":
            self.table_count += 1


def assert_report_invariants(document: str) -> None:
    parser = ReportStructureParser()
    parser.feed(document)
    parser.close()
    assert parser.svg_count >= 7
    assert parser.table_count >= 10
    assert len(parser.ids) == len(set(parser.ids))
    required_ids = {
        "executive",
        "items",
        "npcs",
        "quests",
        "spells",
        "guidelines",
        "caveats",
        "chart-item-availability",
        "chart-weapon-frontier",
        "chart-armor-frontier",
        "chart-npc-hp",
        "chart-npc-xp-efficiency",
        "chart-quest-rewards",
        "chart-quest-coverage",
        "chart-spell-pacing",
        "chart-level50-regimes",
        "table-equipment",
        "table-items",
        "table-excluded-equipment",
        "table-excluded-items",
        "table-slot-gaps",
        "table-class-slot",
        "table-gap-actions",
        "table-npcs",
        "table-npc-outliers",
        "table-quests",
        "table-spells",
        "table-spell-anomalies",
        "table-authoring-anchors",
    }
    assert required_ids <= set(parser.ids)
    assert document.count("class=sortable") == parser.table_count
    assert "id=global-search" in document
    assert "querySelectorAll('table.sortable').forEach(table=>table.querySelectorAll('th').forEach((th,index)=>" in document
    assert "search.addEventListener('input',applySearch)" in document
    assert "title and surname outer rolls are independently 50%" in document
    assert "Each applicable modifier independently enters a candidate pool" in document
    assert "one successful candidate is chosen uniformly" in document
    assert "min_level=0</code> are ineligible" in document
    assert "0.5%" not in document
    assert not re.search(r"(?:https?:)?//", document, re.IGNORECASE)
    assert not re.search(r"\b(?:nan|[+-]?inf(?:inity)?)\b", document, re.IGNORECASE)
    assert not re.search(r"\b(?:todo|tbd|lorem ipsum)\b|\{\{", document, re.IGNORECASE)
    assert not re.search(r">\s*(?:None|null|undefined)\s*<", document)


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", type=Path, default=DEFAULT_DATABASE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    return parser.parse_args(argv)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)
    data, report_summary = generate(args.database)
    document = render_report(data, report_summary)
    assert_report_invariants(document)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(document, encoding="utf-8", newline="\n")
    print(json.dumps(report_summary, indent=2, sort_keys=True))
    print(f"wrote {args.output.resolve()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
