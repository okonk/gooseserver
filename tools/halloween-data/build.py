#!/usr/bin/env python3
import argparse
import json
import random
import struct
import subprocess
import sys
import textwrap
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
OUT = HERE / "out"
GSHEETS = ROOT / ".agents/skills/goose-game-data/scripts/gsheets.py"
MAPS_DIR = ROOT / "Goose/Data/Illutia/Maps"
SCRIPTS_DIR = ROOT / "Goose/Data/Illutia"
CONFIG = ROOT / "Goose/Data/Illutia/Scripts/Global/Halloween/halloween.json"

GROVE, BASEMENT, MINITA = 38, 39, 1
EVENT_END = "2026-11-08T00:00:00"
TIERS = [6000, 20000, 45000]

SHEETS = {
    "effect": "Spell Effects", "spell": "Spells", "item": "Items", "npc": "NPCs",
    "quest": "Quests", "req": "Quest Reqs", "reward": "Quest Rewards", "map": "Maps",
    "goal": "Community Goals",
}


def fail(message):
    sys.exit(f"build: {message}")


def gsheets(*args):
    result = subprocess.run([sys.executable, str(GSHEETS), *args], capture_output=True, text=True, cwd=ROOT)
    if result.returncode != 0:
        fail(f"gsheets {' '.join(args)} failed:\n{result.stderr or result.stdout}")
    return result.stdout


def read_sheet(title):
    return json.loads(gsheets("read", title, "--json"))


def load_map(map_file):
    data = (MAPS_DIR / map_file).read_bytes()
    _, _, width, height = struct.unpack_from("<hhii", data, 0)
    offset = 12
    free = set()
    for y in range(1, height + 1):
        for x in range(1, width + 1):
            flags = struct.unpack_from("<i", data, offset)[0]
            offset += 4
            drawn = False
            for _ in range(5):
                graphic, _sheet = struct.unpack_from("<ih", data, offset)
                offset += 6
                drawn = drawn or graphic != 0
            if drawn and not flags & 2:
                free.add((x, y))
    return free


def wrap(text):
    lines = []
    for paragraph in text.strip().split("\n\n"):
        lines.extend(textwrap.wrap(" ".join(paragraph.split()), 50))
        lines.append("")
    return "\\n".join(lines[:-1])


def reachable(free, start):
    seen, frontier = {start}, [start]
    while frontier:
        x, y = frontier.pop()
        for step in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if step in free and step not in seen:
                seen.add(step)
                frontier.append(step)
    return seen


def eq(chest=0, head=0, legs=0, feet=0, shield=0, weapon=0):
    parts = []
    for slot in (chest, head, legs, feet, shield, weapon):
        if isinstance(slot, tuple):
            parts.append(",".join(str(v) for v in slot))
        else:
            parts.append(f"{slot},*")
    return ",".join(parts)


class Ids:
    def __init__(self, next_ids):
        self.next = dict(next_ids)
        self.ids = {}

    def alloc(self, kind, key):
        ref = f"{kind}:{key}"
        if ref in self.ids:
            fail(f"duplicate key {ref}")
        self.ids[ref] = self.next[kind]
        self.next[kind] += 1
        return self.ids[ref]

    def __call__(self, kind, key):
        ref = f"{kind}:{key}"
        if ref not in self.ids:
            fail(f"unknown reference @{ref}")
        return self.ids[ref]


# ---------------------------------------------------------------- definitions

GEAR_BUFF = {"spell_effected": "Self", "effect_type": "Buff", "works_not_in_pvp": 1,
             "buff_graphic": 810035, "buff_graphic_file": 20107}

EFFECTS = [
    ("sword", "Pumpkin Sword", {**GEAR_BUFF, "spell_damage": 0.10, "spell_crit": 0.05}),
    ("boots", "Rootstompers", {**GEAR_BUFF, "spell_damage": 0.05, "spell_crit": 0.02}),
    ("grit", "Groundskeeper's Grit", {**GEAR_BUFF, "damage_reduce": 0.04, "spell_damage": 0.01}),
    ("chainsaw", "Bloody Chainsaw", {**GEAR_BUFF, "melee_crit": 0.10, "hp_static_regen": 100}),
    ("staff", "Pumpkin Staff", {**GEAR_BUFF, "spell_damage": 0.05, "mp_static_regen": 100}),
    ("helmTop", "Pumpkin Helm", {**GEAR_BUFF, "spell_damage": 0.10, "hp_percent_regen": 0.01, "mp_percent_regen": 0.01}),
    ("witch", "Witch Mask", {**GEAR_BUFF, "damage_reduce": 0.03, "spell_damage": 0.01, "spell_crit": 0.01}),
    ("pie", "Pumpkin Pie", {**GEAR_BUFF, "effect_duration": 10, "hp_percent_regen": 0.05, "buff_graphic": 810008,
                            "oneffect_text": "You feel warm and full."}),
    ("brew", "Witch's Brew", {**GEAR_BUFF, "effect_duration": 10, "mp_percent_regen": 0.05, "buff_graphic": 810032,
                              "oneffect_text": "Your mind fizzes."}),
    ("illusion", "Illusion: Pumpkin", {"spell_effected": "SelfPlayer", "effect_type": "Buff", "effect_duration": 300,
                                       "body_id": 127, "spell_animation": 815014, "works_not_in_pvp": 1,
                                       "buff_graphic": 810007, "buff_graphic_file": 20107}),
    ("weeperCap", "Weeper's Cap", {"spell_effected": "Self", "effect_type": "Buff", "buff_removable": 0,
                                   "script_path": "Scripts/Spell/HalloweenDamageCap.csx"}),
    ("gkCurve", "Groundskeeper's Curve", {"spell_effected": "Self", "effect_type": "Buff", "buff_removable": 0,
                                          "script_path": "Scripts/Spell/HalloweenDamageCurve.csx"}),
    ("ghostSnare", "Ghostly Chill", {"spell_effected": "Player", "effect_type": "Snare", "effect_duration": 4,
                                     "snare_percent": 50, "buff_graphic": 810023, "buff_graphic_file": 20107}),
    ("ghostSnareOnHit", "Ghostly Touch", {"spell_effected": "Self", "effect_type": "OnAttack", "buff_removable": 0,
                                          "on_attack_spell_effect_id": "@effect:ghostSnare",
                                          "on_attack_spell_chance": 100}),
]

SPELLS = [
    ("illusion", {"spell_name": "Illusion: Pumpkin", "spell_target": "Self", "spell_aether": 60000,
                  "spellbook_graphic": 810007, "spellbook_graphic_file": 20107, "mp_static_cost": 100,
                  "spell_effect_id": "@effect:illusion"}),
]

COSTUME_SCRIPT = "Scripts/Item/HalloweenCostume.csx"
COSTUME_LOOKS = [{"graphic": g, "r": 0, "g": 0, "b": 0, "a": 0} for g in (11, 17, 19, 26, 90, 143)]


def stats(n):
    return {"stat_str": n, "stat_sta": n, "stat_dex": n, "stat_int": n}


def icon(sheet, tile):
    return {"graphic_file": sheet, "graphic_tile": tile}


def bound():
    return {"lore": 1, "bindonpickup": 1}


def armor(slot, kind, hp, mp, ac):
    return {"item_usetype": "Armor", "item_slot": slot, "item_type": kind, "player_hp": hp, "player_mp": mp,
            "stat_ac": ac, "min_level": 50}


def mask(name, effect, equip, desc, extra=None):
    return {**armor("Helmet", "Cloth", 1000, 1000, 100), "item_name": name, "spell_effect_id": effect,
            "graphic_equip": equip, **icon(2278, 332240), "script_path": COSTUME_SCRIPT,
            "item_description": desc, **(extra or {})}


ITEMS = [
    ("candy", {"item_usetype": "NoUse", "item_name": "Candy", "stack_size": 9999, "bindonpickup": 1,
               **icon(2281, 332500), "item_description": "Sweet, sticky and strangely warm."}),
    ("costume", {**armor("Helmet", "Cloth", 2000, 2000, 150), "min_level": 1, **stats(10), **bound(),
                 "item_name": "Party Costume", "spell_effect_id": 70, "graphic_equip": 11,
                 **icon(2281, 332509), "properties": json.dumps({"hideOtherSlots": True}),
                 "script_path": COSTUME_SCRIPT, "script_params": json.dumps(COSTUME_LOOKS, separators=(",", ":")),
                 "item_description": "Nobody will recognise you. Probably."}),
    ("goblin", mask("Goblin Mask", 70, 72, "Warty, green and a little too warm inside.")),
    ("zombie", mask("Zombie Mask", 81, 115, "It smells exactly like it looks.")),
    ("mummy", mask("Mummy Mask", 80, 159, "Old linen that never quite stops unravelling.")),
    ("clown", mask("Mad Clown Mask", 76, 112, "The smile is painted on. Mostly.", bound())),
    ("witch", mask("Witch Mask", "@effect:witch", 114, "The Collector's own. Handle with care.", bound())),
    ("sword", {"item_usetype": "Weapon", "item_slot": "OneHanded", "item_type": "OneHandedSword", "min_level": 50,
               "weapon_damage": 175, "player_hp": 1500, "player_mp": 1500, **stats(15), **bound(),
               "item_name": "Pumpkin Sword", "spell_effect_id": "@effect:sword", "graphic_equip": 33,
               "body_state": 4, **icon(2281, 332511), "item_description": "The Countess's own blade, still sticky."}),
    ("boots", {**armor("Shoes", "Leather", 1750, 1750, 125), **stats(15), **bound(), "item_name": "Rootstompers",
               "spell_effect_id": "@effect:boots", "graphic_equip": 13, **icon(2278, 332213),
               "item_description": "Roots still twitch in the soles."}),
    ("shield", {**armor("Shield", "Plate", 1500, 1500, 175), **stats(10), **bound(),
                "item_name": "Jack-o'-Lantern Shield", "spell_effect_id": 70, "graphic_equip": 63,
                **icon(2281, 332502), "item_description": "The candle inside never goes out."}),
    ("chaps", {**armor("Pants", "Leather", 2000, 2000, 150), **stats(15), **bound(), "item_name": "Chainsaw Chaps",
               "spell_effect_id": "@effect:grit", "graphic_equip": 31, "graphic_r": 200, "graphic_g": 70,
               "graphic_b": 60, "graphic_a": 140, **icon(2278, 332204),
               "item_description": "Stained. You don't want to know with what."}),
    ("hockey", {**armor("Helmet", "Plate", 2000, 1000, 150), "stat_str": 25, "stat_sta": 25, **bound(),
                "item_name": "Hockey Mask", "spell_effect_id": "@effect:grit", "graphic_equip": 37,
                **icon(2278, 332240), "item_description": "Cracked down the middle. Still warm."}),
    ("chainsaw", {"item_usetype": "Weapon", "item_slot": "TwoHanded", "item_type": "TwoHandedSword", "min_level": 25,
                  "weapon_damage": 180, "player_hp": 500, "player_mp": 100, "stat_ac": 50, **stats(20),
                  "item_name": "Bloody Chainsaw", "spell_effect_id": "@effect:chainsaw", "graphic_equip": 256,
                  "body_state": 6, **icon(20397, 820015), "item_description": "It still starts first time."}),
    ("staff", {"item_usetype": "Weapon", "item_slot": "OneHanded", "item_type": "OneHandedBlunt", "min_level": 25,
               "weapon_damage": 100, "player_hp": 100, "player_mp": 500, **stats(20), "item_name": "Pumpkin Staff",
               "spell_effect_id": "@effect:staff", "graphic_equip": 35, "body_state": 5, **icon(2281, 332508),
               "item_description": "Carved from a vine that grew through a grave."}),
    ("robe", {**armor("Chest", "Cloth", 3000, 3000, 200), **stats(15), **bound(), "item_name": "Collector's Robe",
              "graphic_equip": 34, **icon(2278, 332201), "item_description": "Every pocket holds a mask."}),
    ("helmTop", {**armor("Helmet", "Plate", 2500, 2500, 175), **stats(20), **bound(), "item_name": "Pumpkin Helm",
                 "spell_effect_id": "@effect:helmTop", "graphic_equip": 28, **icon(2281, 332506),
                 "item_description": "Worn by the sweetest tooth in the land."}),
    ("helmDyed", {**armor("Helmet", "Plate", 1500, 1500, 150), **stats(10), **bound(), "item_name": "Pumpkin Helm",
                  "spell_effect_id": 71, "graphic_equip": 28, "graphic_r": 120, "graphic_g": 60, "graphic_b": 160,
                  "graphic_a": 160, **icon(2281, 332506), "item_description": "For everyone who filled the bowl."}),
    ("pie", {"item_usetype": "OneTime", "item_name": "Pumpkin Pie", "stack_size": 100, "item_value": 200,
             "spell_effect_id": "@effect:pie", **icon(2272, 331646),
             "item_description": "Heals 25% of your health over 10 seconds."}),
    ("brew", {"item_usetype": "OneTime", "item_name": "Witch's Brew", "stack_size": 100, "item_value": 200,
              "spell_effect_id": "@effect:brew", **icon(2272, 331609),
              "item_description": "Restores 25% of your mana over 10 seconds."}),
    ("candle", {"item_usetype": "NoUse", "item_name": "Ritual Candle", "stack_size": 20, **icon(20408, 821125),
                "item_description": "Black wax, half-melted. It burns brightest where the floor is marked."}),
    ("ecto", {"item_usetype": "NoUse", "item_name": "Ectoplasm", "stack_size": 100, **icon(2271, 331509),
              "graphic_r": 86, "graphic_g": 179, "graphic_b": 11, "graphic_a": 200,
              "item_description": "Cold, green and faintly humming."}),
    ("scroll", {"item_usetype": "Scroll", "item_name": "Scroll: Pumpkin Illusion", "item_value": 200,
                "learn_spell_id": "@spell:illusion", **icon(20398, 820110),
                "item_description": "Teaches Illusion: Pumpkin."}),
    ("claw", {"item_usetype": "NoUse", "item_name": "Weeper's Claw", **icon(2269, 331370),
              "item_description": "Still wet with his tears."}),
    ("shroud", {"item_usetype": "NoUse", "item_name": "Phantom Shroud", **icon(20399, 820224),
                "item_description": "Pink, weightless and very cold."}),
    ("heart", {"item_usetype": "NoUse", "item_name": "Harvest Heart", **icon(2281, 332507),
               "item_description": "It beats once a minute."}),
]
for _key, _item in ITEMS:
    _item["event"] = 1
    if "item_description" in _item:
        _item["item_description"] = wrap(_item["item_description"])

MOB = {"npc_type": "Monster", "npc_level": 50, "class_id": 3, "respawn_time": 30, "attack_range": 1,
       "attack_speed": 1, "stunnable": 1, "slowable": 1, "see_invisible": 1, "aggro_range": 3}


def mob(name, hp, hit, move, xp, **extra):
    return {**MOB, "npc_name": name, "npc_hp": hp, "weapon_damage": hit, "move_speed": move, "experience": xp, **extra}


def guest(name, face, helm, hp, hit, xp, stationary=True):
    return mob(name, hp, hit, 1.0, xp, body_id=1, face_id=face, hair_id=0, equipped_items=eq(head=helm),
               **({"stationary": 1} if stationary else {}))


FRIENDLY = {"npc_type": "Quest", "npc_level": 50, "class_id": 3, "respawn_time": 30, "stationary": 1,
            "invincible": 1, "weapon_damage": 70000, "hp_percent_regen": 0.2}
GK_LOOK = {"body_id": 1, "body_state": 6, "face_id": 1, "hair_id": 0,
           "equipped_items": eq(chest=(34, 80, 70, 60, 200), head=37, legs=(31, 200, 70, 60, 140), weapon=256)}
LOOK_COLUMNS = ["body_state", "body_id", "body_r", "body_g", "body_b", "body_a", "face_id", "hair_id",
                "hair_r", "hair_g", "hair_b", "hair_a", "equipped_items"]

NPCS = [
    ("penguin", guest("Penguin Guest", 1, 11, 17000, 1400, 2400)),
    ("frog", guest("Frog Guest", 16, 17, 17000, 1400, 2400)),
    ("bunny", guest("Bunny Guest", 5, 90, 17000, 1400, 2400)),
    ("purple", guest("Purple Ghost Guest", 2, 143, 17000, 1400, 2400)),
    ("ghostGuest", guest("Ghost Guest", 4, 19, 47000, 2300, 3000)),
    ("devil", guest("Devil Guest", 13, 26, 79000, 3500, 4400, stationary=False)),
    ("setting", mob("Possessed Place Setting", 10000, 1200, 0.8, 1700, body_id=307, stationary=1)),
    ("cook", mob("Ghoul Cook", 47000, 2800, 1.5, 3000, body_id=169)),
    ("wailing", mob("Wailing Ghost", 47000, 3000, 1.0, 3000, body_id=220)),
    ("flickering", mob("Flickering Ghost", 10000, 1800, 0.8, 1700, body_id=306)),
    ("phantom", mob("Pink Phantom", 150000, 10000, 1.0, 6000, body_id=308)),
    ("bloody", mob("Bloody Ghost", 79000, 10000, 1.0, 4400, body_id=305)),
    ("bloodyAdd", mob("Bloody Ghost", 79000, 10000, 1.0, 4400, body_id=305)),
    ("bones", mob("Restless Bones", 47000, 2300, 1.0, 3000, body_id=10106)),
    ("weeper", mob("The Weeper", 12000000, 1, 1.2, 100000, body_id=231, respawn_time=1500, aggro_range=6,
                   attack_speed=2, stunnable=0, slowable=0, script_path="Scripts/NPC/Weeper.csx")),
    ("stalker", mob("The Groundskeeper", 1, 1, 0.34, 0, **GK_LOOK, invincible=1, aggro_range=0, attack_speed=0,
                    stunnable=0, slowable=0, script_path="Scripts/NPC/Stalker.csx")),
    ("gk", mob("The Groundskeeper", 8000000, 1, 1.0, 7500, **GK_LOOK, aggro_range=12, attack_speed=2.5,
               stunnable=0, slowable=0, script_path="Scripts/NPC/Groundskeeper.csx")),
    ("countess", {**FRIENDLY, "npc_name": "The Countess", "body_id": 154}),
    ("count", {**FRIENDLY, "npc_name": "The Count", "body_id": 155}),
    ("medium", {**FRIENDLY, "npc_name": "The Medium", "body_id": 220, "body_r": 150, "body_g": 200,
                "body_b": 255, "body_a": 180}),
    ("kid", {**FRIENDLY, "npc_name": "Trick-or-Treater", "body_id": 130}),
    ("doorman", {**FRIENDLY, "npc_name": "The Doorman", "body_id": 1, "face_id": 5, "hair_id": 0,
                 "equipped_items": eq(head=35)}),
    ("collector", {**FRIENDLY, "npc_name": "The Collector", "body_id": 1, "face_id": 1, "hair_id": 23,
                   "equipped_items": eq(chest=34, head=114)}),
    ("farmer", {**FRIENDLY, "npc_name": "Pumpkin Farmer", "copy_look": 186}),
    ("butler", {**FRIENDLY, "npc_name": "The Butler", "body_id": 1, "face_id": 1, "hair_id": 2,
                "equipped_items": eq(chest=(34, 20, 20, 20, 200))}),
]
GUESTS = ["penguin", "frog", "bunny", "purple", "ghostGuest", "devil"]

GROVE_UPDATES = {
    166: {"npc_level": 50, "npc_hp": 3200, "weapon_damage": 1200, "experience": 1250},
    168: {"npc_level": 50, "npc_hp": 3200, "weapon_damage": 1200, "experience": 1250},
    169: {"npc_name": "The Pumpkin King", "npc_level": 50, "npc_hp": 1000000, "weapon_damage": 20000,
          "experience": 7500, "body_id": 127, "body_r": 255, "body_g": 140, "body_b": 0, "body_a": 160},
    167: {"npc_name": "Jack O'Malice", "npc_level": 50, "npc_hp": 700000, "weapon_damage": 12000, "experience": 5000},
}
PUMPKINS, CHIEFS, KING, JACK = 166, 168, 169, 167

TOWNSFOLK = [
    (150, "Wendell the Scribe", "Scriber"),
    (105, "Brask the Weaponsmith", "Weapon Guy"),
    (226, None, "Slim Pickins"),
    (100, "Agnes the Apothecary", "Potion Guy"),
    (181, "Pip the Pet Trainer", "Pet Trainer"),
]

DROPS = (
    [(g, m, 1, 0, 1) for g in GUESTS for m in ("goblin", "zombie", "mummy")]
    + [(g, "brew", 1, 0, 1) for g in GUESTS]
    + [("cook", "pie", 1, 0, 2), ("wailing", "ecto", 1, 0, 25), ("flickering", "ecto", 1, 0, 25),
       ("phantom", "candle", 1, 0, 50), ("phantom", "shroud", 1, 0, 50),
       ("weeper", "claw", 1, 0, 100), ("weeper", "candle", 1, 2, 100)]
    + [("gk", g, 1, 0, 10) for g in ("chaps", "hockey", "chainsaw", "staff")]
    + [(KING, "heart", 1, 2, 100), (KING, "boots", 1, 0, 10), (JACK, "shield", 1, 0, 10)]
)

ROOMS = [
    ("Foyer", [27, 42, 51, 62], 0, "house"),
    ("Library", [2, 2, 35, 26], 2, "house"),
    ("Statue Gallery", [27, 27, 51, 41], 2, "house"),
    ("Kids' Bedroom", [37, 2, 66, 21], 1, "house"),
    ("Master Bedroom", [68, 2, 94, 21], 1, "master"),
    ("Dining Room", [2, 29, 25, 47], 1, "house"),
    ("Kitchen", [2, 48, 25, 65], 0.5, "house"),
    ("Ballroom", [52, 28, 100, 61], 0.5, "house"),
    ("Garden", [54, 62, 83, 90], 0.5, "house"),
]
ROAMING = [
    ("Ballroom", "devil", 10), ("Library", "devil", 5), ("Kids' Bedroom", "devil", 3),
    ("Master Bedroom", "devil", 3), ("Statue Gallery", "devil", 2), ("Garden", "devil", 4),
    ("Kitchen", "cook", 14),
]
BALLROOM_GUESTS = 48
PLAN_MOBS = {"Penguin": "penguin", "Frog": "frog", "Bunny": "bunny", "Purple ghost": "purple",
             "Ghost": "ghostGuest", "Place Setting": "setting"}
FOYER_SPAWN = (43, 53)
NO_ROAM = [((14, 61), (17, 65)), ((62, 70), (75, 81))]

FRIENDLY_SPAWNS = [
    ("countess", "house", 42, 51), ("count", "house", 44, 51), ("collector", "house", 36, 48),
    ("medium", "house", 19, 59), ("butler", "house", 46, 48), ("weeper", "house", 38, 36),
    ("doorman", GROVE, 38, 36), ("farmer", GROVE, 97, 21), ("kid", MINITA, 53, 58),
]

WEARING = "Scripts/Quest/HalloweenWearing.csx"
DOORMAN = "Scripts/Quest/HalloweenDoorman.csx"


def kill(npc, count):
    return ("Kill", npc, count)


def item_req(item, count, keep=0):
    return ("Item", f"@item:{item}", count, keep)


def candy(n):
    return ("Item", "@item:candy", n)


QUESTS = [
    dict(key="q1", giver="kid", name="Dress Up", min_level=1,
         desc="""Everybody's going to the party at the house on the hill! But you can't go like THAT.

         I've got a costume that'll fit. Five thousand gold and it's yours. No refunds.""",
         passed="""Ooh, it suits you! Every time you put it on it looks a bit different. That's the magic.""",
         failed="""Five thousand gold. That's the price of a good costume.""",
         reqs=[("Gold", 5000, None)], rewards=[("Item", "@item:costume", 1)]),
    dict(key="q2", giver="kid", name="Trick or Treat!", min_level=1, prereq=["q1"],
         desc="""Now we go trick-or-treating! The shopkeepers in Minita always have something sweet under
         the counter. Visit Wendell the Scribe, Brask the Weaponsmith, Slim Pickins, Agnes the
         Apothecary and Pip the Pet Trainer.

         And you HAVE to wear your costume, or it doesn't count.""",
         passed="""Look at all that! You're a natural. Here, have some of mine too.""",
         failed="""Five shopkeepers, and your costume on. Those are the rules.""",
         reqs=[("TalkToNPC", npc_id, 1) for npc_id, _, _ in TOWNSFOLK] + [("Script", WEARING)],
         rewards=[("Experience", 10000), ("Gold", 2000), candy(25), ("Item", "@item:pie", 3)]),
    dict(key="q3", giver="kid", name="The House on the Hill", min_level=1, prereq=["q2"],
         desc="""The party's at the big house past the Pumpkin Grove. The Doorman only lets in people in
         costume.

         Go and talk to him. I'm too scared.""",
         passed="""Another guest. Splendid. Do keep your costume on inside. The house notices.""",
         failed="""Find the Doorman at the front of the house in the Pumpkin Grove.""",
         reqs=[("TalkToNPC", "@npc:doorman", 1)], rewards=[("Experience", 1000)]),
    dict(key="q4", giver="doorman", name="Trick or Treat?", min_level=40, repeatable=True,
         desc="""Costumes only, I'm afraid. House rules.

         Show me you're dressed for the occasion and I'll let you in.""",
         passed="""Welcome to the party. The door locks behind you.""",
         failed="""No costume, no party. Put it on and come back.""",
         reqs=[("Script", WEARING)], rewards=[("Script", DOORMAN)]),
    dict(key="q5", giver="countess", name="Uninvited Guests",
         desc="""So many guests, and I invited none of them. They won't leave, and they're eating
         everything.

         Thin them out for me. Ten of each kind: Penguins, Frogs, Bunnies, Purple Ghosts, Ghosts and
         Devils.""",
         passed="""Much quieter. You may stay as long as you like, darling.""",
         failed="""There are still far too many guests. Ten of each, please.""",
         reqs=[kill(f"@npc:{g}", 10) for g in GUESTS],
         rewards=[("Experience", 120000), ("Gold", 25000), candy(50)]),
    dict(key="q6", giver="countess", name="Table Manners",
         desc="""The dinner service has got ideas above its station. Forks with opinions. Plates that
         bite.

         Break twenty of the Possessed Place Settings in the Dining Room.""",
         passed="""We'll eat off the floor tonight. Better than being eaten by the plates.""",
         failed="""Twenty of the Place Settings, please. Mind the knives.""",
         reqs=[kill("@npc:setting", 20)], rewards=[("Experience", 60000), ("Gold", 15000), candy(20)]),
    dict(key="q7", giver="countess", name="Spirits in the Walls",
         desc="""The walls ooze at night. It's ghosts, of course. They leave Ectoplasm everywhere.

         Bring me five pieces of it. I have a use for it.""",
         passed="""Lovely. It goes in the punch.""",
         failed="""Five Ectoplasm. The ghosts in the house leave it behind.""",
         reqs=[item_req("ecto", 5)], rewards=[("Experience", 40000), ("Gold", 15000), candy(20)]),
    dict(key="q8", giver="countess", name="Something Pink",
         desc="""Something pink haunts the Master Bedroom. It isn't one of mine.

         Get rid of it.""",
         passed="""Pink. In my house. The cheek of it.""",
         failed="""The pink thing still haunts the Master Bedroom.""",
         reqs=[kill("@npc:phantom", 1)], rewards=[("Experience", 80000), ("Gold", 20000), candy(30)]),
    dict(key="q9", giver="countess", name="The Weeper",
         desc="""You'll hear him before you see him. He walks the halls weeping and won't stop.

         Bring friends. He doesn't like people on their own.""",
         passed="""The house is quiet. For now. He always comes back.""",
         failed="""The Weeper still walks the halls.""",
         reqs=[kill("@npc:weeper", 1)], rewards=[("Experience", 150000), ("Gold", 30000), candy(40)]),
    dict(key="q10", giver="countess", name="Harvest Heart",
         desc="""The Pumpkin King rules the Grove outside, and he's been trampling my guests.

         Put an end to him.""",
         passed="""The Grove will be quiet tonight. Thank you.""",
         failed="""The Pumpkin King still rules the Grove.""",
         reqs=[kill(KING, 1)], rewards=[("Experience", 120000), ("Gold", 30000), candy(40)]),
    dict(key="q11", giver="countess", name="Lights Out", prereq=["q5", "q6", "q7", "q8", "q9", "q10"],
         desc="""There is one guest I truly fear. He keeps the grounds, and he lives below.

         When the clock strikes midnight he comes up to tidy. End him, and my sword is yours.""",
         passed="""You did it. Take the sword. It was never really mine.""",
         failed="""The Groundskeeper still keeps the grounds.""",
         reqs=[kill("@npc:gk", 1)], rewards=[("Item", "@item:sword", 1), ("Experience", 200000), ("Gold", 50000)]),
    dict(key="q12", giver="count", name="Party Crashers", repeatable=True,
         desc="""My wife wants them gone. I want them gone more quietly.

         Five of each kind of guest, if you'd be so kind. Again tomorrow, no doubt.""",
         passed="""Splendid. Have a drink on me.""",
         failed="""Five of each kind of guest, please.""",
         reqs=[kill(f"@npc:{g}", 5) for g in GUESTS],
         rewards=[("Experience", 30000), ("Gold", 8000), ("Item", "@item:brew", 3), candy(10)]),
    dict(key="q13", giver="count", name="Fresh Pies", repeatable=True,
         desc="""The cooks have turned. They still bake, mind you. Dreadful manners, wonderful pies.

         Twenty of them, and I'll share the spoils.""",
         passed="""Still warm. Take a couple.""",
         failed="""Twenty Ghoul Cooks, in the Kitchen.""",
         reqs=[kill("@npc:cook", 20)],
         rewards=[("Experience", 20000), ("Gold", 5000), ("Item", "@item:pie", 2), candy(15)]),
    dict(key="q14", giver="collector", name="Masquerade: Goblin",
         desc="""I collect faces. Other people's, ideally.

         The guests sometimes drop a Goblin Mask. Bring me one and I'll trade you my own.""",
         passed="""Marvellous. Here, wear mine. It suits you better.""",
         failed="""A Goblin Mask. The guests drop them now and then.""",
         reqs=[item_req("goblin", 1)], rewards=[("Item", "@item:witch", 1), ("Experience", 25000), ("Gold", 10000)]),
    dict(key="q15", giver="collector", name="Masquerade: Mummy",
         desc="""A Mummy Mask next, if you please. The guests drop those as well.""",
         passed="""Lovely wrapping. Have this one. He laughs at night.""",
         failed="""A Mummy Mask. The guests drop them now and then.""",
         reqs=[item_req("mummy", 1)], rewards=[("Item", "@item:clown", 1), ("Experience", 25000), ("Gold", 10000)]),
    dict(key="q16", giver="collector", name="Masquerade: Zombie",
         desc="""A Zombie Mask completes the set. Nobody ever brings me the Zombie.""",
         passed="""The set is complete. You, my friend, are a Masquerader.""",
         failed="""A Zombie Mask. The guests drop them now and then.""",
         reqs=[item_req("zombie", 1)],
         rewards=[("Title", "Masquerader"), ("Experience", 25000), ("Gold", 10000), candy(20)]),
    dict(key="q17", giver="collector", name="The Finishing Touch", prereq=["q14", "q15", "q16"],
         desc="""One last piece for my robe. A Phantom Shroud, a Weeper's Claw and a Harvest Heart.

         Bring all three and the robe is yours.""",
         passed="""Perfect. Wear it well.""",
         failed="""A Phantom Shroud, a Weeper's Claw and a Harvest Heart.""",
         reqs=[item_req("shroud", 1), item_req("claw", 1), item_req("heart", 1)],
         rewards=[("Item", "@item:robe", 1), ("Experience", 50000), ("Gold", 20000)]),
    dict(key="q18", giver="medium", name="Ghost Story",
         desc="""The spirits here are restless. I can tell you why, but I need something to hold them
         still. Three pieces of Ectoplasm.""",
         passed="""He kept the grounds of this house for forty years. He fixed the roof, he dug the
         graves, he wound the clock.

         Then one night the family went down to the basement with candles, and they did not come back
         up. He did.

         He still winds the clock.""",
         failed="""Three Ectoplasm, and I will tell you his story.""",
         reqs=[item_req("ecto", 3)], rewards=[("Experience", 40000), ("Gold", 10000)]),
    dict(key="q19", giver="medium", name="The Medium's Warning", prereq=["q18"],
         desc="""You want to meet him. I can see it.

         Bring me five Ritual Candles and I will tell you what I did. If you have none, ask the pink
         spirit in the Master Bedroom. It carries them.""",
         passed="""Five flames. I lit them for him once, one at every point, and waited for the clock.

         Never again.""",
         failed="""Five Ritual Candles. The pink spirit in the Master Bedroom carries them.""",
         reqs=[item_req("candle", 5, keep=1)], rewards=[("Experience", 10000)]),
    dict(key="q20", giver="medium", name="Every Midnight", prereq=["q19"],
         desc="""He comes back. He always comes back.

         Put him down ten times and maybe he will stay down.""",
         passed="""Ten times. You are a Survivor, and there are not many of those.""",
         failed="""He still comes back. Ten times, I said.""",
         reqs=[kill("@npc:gk", 10)], rewards=[("Title", "Survivor"), ("Experience", 300000), ("Gold", 75000)]),
    dict(key="q21", giver="farmer", name="Pumpkin Patch",
         desc="""My pumpkins have gone bad this year. They walk about and bite.

         Cull ten Pumpkins and ten Chief Pumpkins for me.""",
         passed="""That's a start. They'll be back by morning.""",
         failed="""Ten Pumpkins and ten Chief Pumpkins.""",
         reqs=[kill(PUMPKINS, 10), kill(CHIEFS, 10)], rewards=[("Experience", 70000), ("Gold", 18000), candy(20)]),
    dict(key="q22", giver="farmer", name="Pumpkin Slayer", prereq=["q21"],
         desc="""Two hundred and fifty of each. Then I'll believe the Grove is safe.""",
         passed="""You're a proper Pumpkin Slayer. Take this scroll. Don't let it go to your head.""",
         failed="""Two hundred and fifty Pumpkins and Chief Pumpkins.""",
         reqs=[kill(PUMPKINS, 250), kill(CHIEFS, 250)],
         rewards=[("Title", "Pumpkin Slayer"), ("Item", "@item:scroll", 1)]),
]
GIVER_ORDER = {
    "kid": ["q1", "q2", "q3"], "doorman": ["q4", "q3"],
    "countess": ["q5", "q6", "q7", "q8", "q9", "q10", "q11"], "count": ["q12", "q13"],
    "collector": ["q14", "q15", "q16", "q17"], "medium": ["q18", "q19", "q20"], "farmer": ["q21", "q22"],
}

FAKE_SCARES = {
    "house": [
        "The statue was facing the other way a moment ago.", "A music box starts, then stops.",
        "Someone laughs in the next room.", "The candles all lean towards you.",
        "Wet footprints lead under the door.", "The portrait's eyes follow you…", "A door slams upstairs.",
        "A cold hand brushes your neck.", "The clock chimes thirteen.", "Glass shatters somewhere below.",
        "A child whispers your name.", "The rug ripples as if something moved under it.",
        "A chair scrapes back from the table.", "Every light dims for a heartbeat.",
        "Someone hums along to the music, off-key.",
    ],
    "basement": [
        "Something drips in the dark.", "Chains rattle behind the wall.", "The bones shift.",
        "A chainsaw coughs once, far away.", "Your torch gutters.", "Scratching from inside the walls.",
        "The air smells of wet earth.", "A shovel scrapes stone.",
        "Something heavy is dragged across the floor.", "You hear breathing that isn't yours.",
    ],
    "directional": [
        "Footsteps behind you.", "Something moves to your left.", "A shape darts past on your right.",
        "Breath on the back of your neck.",
    ],
}


# ---------------------------------------------------------------- build

def resolve(value, ids):
    if isinstance(value, str) and value.startswith("@"):
        kind, key = value[1:].split(":", 1)
        return ids(kind, key)
    if isinstance(value, int) and not isinstance(value, bool):
        return value
    return value


def ref(value, ids):
    if isinstance(value, int):
        return value
    return resolve(value, ids)


def build(next_ids, npc_rows, warp_rows):
    ids = Ids(next_ids)
    ops = []
    maps = {"house": ids.alloc("map", "house")}

    def map_id(m):
        return maps[m] if isinstance(m, str) else m

    for key, _name, _ in EFFECTS:
        ids.alloc("effect", key)
    for key, _ in SPELLS:
        ids.alloc("spell", key)
    for key, _ in ITEMS:
        ids.alloc("item", key)
    for key, _ in NPCS:
        ids.alloc("npc", key)
    for quest in QUESTS:
        ids.alloc("quest", quest["key"])
    ids.alloc("goal", "candy")

    for key, name, values in EFFECTS:
        ops.append(create("Spell Effects", {"spell_effect_id": ids("effect", key), "spell_effect_name": name,
                                            **{k: resolve(v, ids) for k, v in values.items()}}))
    for key, values in SPELLS:
        ops.append(create("Spells", {"spell_id": ids("spell", key), **{k: resolve(v, ids) for k, v in values.items()}}))
    for key, values in ITEMS:
        ops.append(create("Items", {"item_template_id": ids("item", key),
                                    **{k: resolve(v, ids) for k, v in values.items()}}))

    npcs_by_id = {int(r["npc_id"]): r for r in npc_rows}
    quest_ids = {giver: " ".join(str(ids("quest", q)) for q in order) for giver, order in GIVER_ORDER.items()}
    for key, values in NPCS:
        row = {k: v for k, v in values.items() if k != "copy_look"}
        if "copy_look" in values:
            source = npcs_by_id[values["copy_look"]]
            row.update({c: source[c] for c in LOOK_COLUMNS if source.get(c) not in ("", None)})
        if key in quest_ids:
            row["quest_ids"] = quest_ids[key]
        ops.append(create("NPCs", {"npc_id": ids("npc", key), **{k: resolve(v, ids) for k, v in row.items()}}))

    revert = []
    for npc_id, changes in GROVE_UPDATES.items():
        ops.append({"op": "update", "sheet": "NPCs", "id": npc_id, "set": changes})
        original = npcs_by_id[npc_id]
        revert.append({"op": "update", "sheet": "NPCs", "id": npc_id,
                       "set": {k: blank_as_default(original.get(k, "")) for k in changes}})

    for npc_id, new_name, old_name in TOWNSFOLK:
        current = npcs_by_id[npc_id]
        if current["npc_name"] not in (old_name, new_name or old_name):
            fail(f"NPC {npc_id} is {current['npc_name']!r}, expected {old_name!r}")
        if str(current.get("quest_ids", "")).strip():
            fail(f"NPC {npc_id} already has quests {current['quest_ids']!r}")
        changes = {"quest_ids": str(ids("quest", "q2"))}
        if new_name:
            changes["npc_name"] = new_name
        ops.append({"op": "update", "sheet": "NPCs", "id": npc_id, "set": changes})
        revert.append({"op": "update", "sheet": "NPCs", "id": npc_id, "set": {"quest_ids": ""}})

    for npc, item, stack, max_stack, rate in DROPS:
        row = {"npc_template_id": ref(npc if isinstance(npc, int) else f"@npc:{npc}", ids),
               "item_template_id": ids("item", item), "stack": stack, "droprate": rate}
        if max_stack:
            row["max_stack"] = max_stack
        ops.append(create("NPC Drops", row))

    spawns, counts = plan_spawns(maps)
    for npc, m, x, y, props in spawns:
        row = {"npc_id": ids("npc", npc), "map_id": map_id(m), "map_x": x, "map_y": y}
        if props:
            row["properties"] = json.dumps(props)
        ops.append(create("NPC Spawns", row))

    ops.append(create("Maps", {"map_id": maps["house"], "map_name": "Haunted House", "map_filename": "Map10048.map",
                               "min_level": 40, "script_path": "Scripts/Map/HauntedHouse.csx"}))
    ops.append({"op": "update", "sheet": "Maps", "id": BASEMENT,
                "set": {"map_name": "Haunted Basement", "script_path": "Scripts/Map/HauntedBasement.csx"}})
    for x, to_x in ((14, 93), (15, 94), (16, 94), (17, 95)):
        ops.append(create("Warptiles", {"map_id": maps["house"], "map_x": x, "map_y": 65,
                                        "warp_id": BASEMENT, "warp_x": to_x, "warp_y": 99}))

    next_req, next_reward = next_ids["req"], next_ids["reward"]
    for quest in QUESTS:
        qid = ids("quest", quest["key"])
        ops.append(create("Quests", {
            "id": qid, "name": quest["name"], "description": wrap(quest["desc"]),
            "pass_text": wrap(quest["passed"]), "fail_text": wrap(quest["failed"]),
            "min_level": quest.get("min_level", 50), "repeatable": 1 if quest.get("repeatable") else 0,
            "show_progress": 1,
            "prerequisite_quests": " ".join(str(ids("quest", p)) for p in quest.get("prereq", [])),
        }))
        for req in quest["reqs"]:
            ops.append(create("Quest Reqs", {"id": next_req, "quest_id": qid, **requirement(req, ids)}))
            next_req += 1
        for reward in quest["rewards"]:
            ops.append(create("Quest Rewards", {"id": next_reward, "quest_id": qid, **reward_row(reward, ids)}))
            next_reward += 1

    ops.append(create("Community Goals", {
        "id": ids("goal", "candy"), "name": "The Candy Count", "npc_template_id": ids("npc", "butler"),
        "description": "The house keeps a bowl by the door, and it is never full. Every piece of candy you "
                       "bring the Butler counts towards the whole server's haul: more Experience at "
                       f"{TIERS[0]:,}, the basement opens at {TIERS[1]:,}, and at {TIERS[2]:,} the party "
                       "spills into town. The biggest givers win a Pumpkin Helm.",
        "script_path": "Scripts/CommunityGoal/HalloweenCandyGoal.csx",
        "properties": json.dumps(goal_properties(ids), separators=(",", ":")),
    }))

    manual = manual_steps(maps, warp_rows)
    config = config_patch(ids, maps)
    return ops, revert, manual, config, counts, ids


def blank_as_default(value):
    return 0 if value in ("", None) else value


def create(sheet, row):
    return {"op": "create", "sheet": sheet, "set": {k: v for k, v in row.items() if v not in ("", None)}}


def requirement(req, ids):
    kind = req[0]
    if kind == "Script":
        items = [ids("item", k) for k in ("costume", "goblin", "zombie", "mummy", "clown", "witch")]
        return {"requirement_type": "Script", "requirement_value": 0, "script_path": req[1],
                "script_params": json.dumps({"items": items})}
    if kind == "Gold":
        return {"requirement_type": "Gold", "requirement_value": req[1], "keep_requirement": 0}
    if kind == "Item":
        return {"requirement_type": "Item", "requirement_value": resolve(req[1], ids), "requirement_value2": req[2],
                "keep_requirement": req[3]}
    return {"requirement_type": kind, "requirement_value": ref(req[1], ids), "requirement_value2": req[2]}


def reward_row(reward, ids):
    kind = reward[0]
    if kind == "Script":
        return {"reward_type": "Script", "script_path": reward[1]}
    if kind == "Title":
        return {"reward_type": "Title", "string_value": reward[1]}
    if kind == "Item":
        return {"reward_type": "Item", "long_value": resolve(reward[1], ids), "long_value2": reward[2]}
    return {"reward_type": kind, "long_value": reward[1]}


def goal_properties(ids):
    def prize(item, title):
        return [{"type": "Item", "value": ids("item", item), "value2": 1}, {"type": "Title", "value": title}]
    return {
        "requirements": [{"key": "candy", "type": "Item", "item": ids("item", "candy"), "label": "Candy"}],
        "closesAt": EVENT_END,
        "brackets": [
            {"rank": 1, "rewards": prize("helmTop", "Candy Baron")},
            {"rank": 2, "rewards": prize("helmTop", "Sugar Fiend")},
            {"rank": 3, "rewards": prize("helmTop", "Sweet Tooth")},
            {"minScore": 100, "rewards": prize("helmDyed", "Trick or Treater")},
        ],
    }


def in_rect(x, y, rect):
    return rect[0] <= x <= rect[2] and rect[1] <= y <= rect[3]


def plan_spawns(maps):
    free = {"house": load_map("Map10048.map"), GROVE: load_map("Map10038.map"), MINITA: load_map("Map10001.map")}
    free["house"] = reachable(free["house"], FOYER_SPAWN)
    taken = {m: set() for m in free}
    spawns, counts = [], {}

    def place(npc, m, x, y, props=None, room=None):
        if (x, y) not in free[m]:
            fail(f"{npc} at {m}:({x},{y}) is not a free tile reachable on foot")
        if (x, y) in taken[m]:
            fail(f"two spawns on {m}:({x},{y})")
        taken[m].add((x, y))
        spawns.append((npc, m, x, y, props))
        counts[room or str(m)] = counts.get(room or str(m), 0) + 1

    for npc, m, x, y in FRIENDLY_SPAWNS:
        place(npc, m, x, y, room="friendly")

    plan = json.loads((HERE / "spawn_plan.json").read_text())["plan"]
    for cluster in plan:
        for mob_name, (x, y) in zip(cluster["mobs"], cluster["tiles"]):
            place(PLAN_MOBS[mob_name], "house", x, y, room=cluster["room"])

    rng = random.Random(31)
    rects = {name: rect for name, rect, _, _ in ROOMS}

    def roam_tile(room):
        candidates = sorted(t for t in free["house"] if in_rect(*t, rects[room])
                            and not any(in_rect(*t, a + b) for a, b in NO_ROAM)
                            and all(max(abs(t[0] - u[0]), abs(t[1] - u[1])) >= 2 for u in taken["house"]))
        if not candidates:
            fail(f"no free roaming tile left in {room}")
        return rng.choice(candidates)

    ballroom_types = ["penguin", "frog", "bunny", "purple", "ghostGuest"]
    for i in range(BALLROOM_GUESTS):
        place(ballroom_types[i % 5], "house", *roam_tile("Ballroom"), {"canMove": True}, room="Ballroom")
    for room, npc, count in ROAMING:
        for _ in range(count):
            place(npc, "house", *roam_tile(room), room=room)
    return spawns, counts


def manual_steps(maps, warp_rows):
    house = maps["house"]

    def find(values):
        matches = [r for r in warp_rows if all(str(r[k]) == str(v) for k, v in values.items())]
        if len(matches) != 1:
            fail(f"expected one warp matching {values}, found {len(matches)}")
        return matches[0]

    def describe(r):
        return f"map {r['map_id']} ({r['map_x']},{r['map_y']}) -> map {r['warp_id']} ({r['warp_x']},{r['warp_y']})"

    lines = ["# Halloween data: manual sheet edits", "",
             "`Warptiles` has no primary key, so `gsheets.py` can't update or delete these rows.",
             "Edit them in the sheet after applying `batch.json`. Match all six values, not just the row number.", "",
             "## Delete", ""]
    for x, y, wx, wy in ((37, 35, 5, 98), (38, 35, 6, 98), (39, 35, 7, 98)):
        lines.append(f"- {describe(find({'map_id': GROVE, 'map_x': x, 'map_y': y, 'warp_id': BASEMENT, 'warp_x': wx, 'warp_y': wy}))}  (Grove door; the Doorman replaces it)")
    for x, wx in ((40, 93), (41, 94), (42, 95)):
        lines.append(f"- {describe(find({'map_id': BASEMENT, 'map_x': x, 'map_y': 4, 'warp_id': BASEMENT, 'warp_x': wx, 'warp_y': 99}))}  (old upper floor to basement)")
    lines += ["", "## Change", ""]
    for x, hx in ((93, 15), (94, 16), (95, 17)):
        r = find({"map_id": BASEMENT, "map_x": x, "map_y": 100})
        lines.append(f"- {describe(r)}  ->  set `warp_id` {house}, `warp_x` {hx}, `warp_y` 60 (basement exit to the Kitchen stairs)")
    lines += ["", "## Keep", "",
              "- The three `map 39 (5..7,99) -> map 38` exits, so anyone saved on the old upper floor can walk out.",
              "", "## After the event", "",
              "1. `/halloween off`.",
              "2. `gsheets.py batch tools/halloween-data/out/revert.json` (Grove pumpkins back to their old values).",
              f"3. Delete the Pumpkin King ({KING}) and Jack O'Malice ({JACK}) event drops (Harvest Heart, Rootstompers, Jack-o'-Lantern Shield).",
              "4. Restore the three Grove door warps above if the old Haunted House should come back."]
    return "\n".join(lines) + "\n"


def config_patch(ids, maps):
    def npc(key):
        return ids("npc", key)

    candy_by_template = {str(npc(k)): [1, 3] for k in GUESTS + ["setting", "cook"]}
    candy_by_template.update({str(p): [1, 3] for p in (PUMPKINS, CHIEFS, KING, JACK)})
    candy_by_template.update({str(npc("weeper")): [15, 25], str(npc("phantom")): [15, 25], str(npc("gk")): [50, 80]})
    return {
        "maps": {"house": maps["house"], "basement": BASEMENT, "grove": GROVE, "basementArrival": [94, 98]},
        "items": {"candy": ids("item", "candy"), "partyCostume": ids("item", "costume"),
                  "masks": [ids("item", k) for k in ("goblin", "zombie", "mummy", "clown", "witch")],
                  "ritualCandle": ids("item", "candle")},
        "npcs": {"stalker": npc("stalker"), "groundskeeper": npc("gk"), "weeper": npc("weeper"),
                 "wailingGhost": npc("wailing"), "flickeringGhost": npc("flickering"),
                 "pinkPhantom": npc("phantom"), "bloodyGhost": npc("bloody"), "restlessBones": npc("bones")},
        "candy": {"byTemplate": candy_by_template},
        "rooms": [{"name": n, "rect": r, "weight": w, "table": t} for n, r, w, t in ROOMS],
        "ambush": {"fakeScares": FAKE_SCARES},
        "invasion": {"towns": [{"map": MINITA, "rect": [35, 30, 71, 62]}], "templates": [npc(k) for k in GUESTS[:5] + ["wailing", "flickering"]]},
        "goal": {"tiers": TIERS},
        "effects": {"weeperCap": ids("effect", "weeperCap"), "groundskeeperCurve": ids("effect", "gkCurve"),
                    "ghostSnareOnHit": ids("effect", "ghostSnareOnHit")},
        "groundskeeper": {"adds": {"placeSettings": [npc("setting"), 8], "ghosts": [npc("wailing"), 6],
                                   "bloody": [npc("bloodyAdd"), 2]}},
    }


def merge(base, patch):
    for key, value in patch.items():
        if isinstance(value, dict) and isinstance(base.get(key), dict) and key not in ("byTemplate", "fakeScares", "adds"):
            merge(base[key], value)
        else:
            base[key] = value
    return base


def check_scripts(ops):
    missing = set()
    for op in ops:
        path = op["set"].get("script_path")
        if path and not (SCRIPTS_DIR / path).exists():
            missing.add(path)
    return sorted(missing)


def check_text(ops):
    long = []
    for op in ops:
        if op["sheet"] == "Community Goals":
            continue
        for column in ("description", "pass_text", "fail_text", "item_description"):
            for line in str(op["set"].get(column, "")).split("\\n"):
                if len(line) > 50:
                    long.append(f"{op['sheet']} {column}: {line}")
    return long


def main():
    parser = argparse.ArgumentParser(description="Build the Halloween event's sheet batch and config.")
    parser.add_argument("--offline", metavar="DIR",
                        help="read next_ids.json, npcs.json and warptiles.json from DIR instead of the sheet")
    parser.add_argument("--config", default=str(CONFIG), help="halloween.json to start from")
    parser.add_argument("--in-place", action="store_true", help="write the patched config back to --config")
    args = parser.parse_args()

    if args.offline:
        source = Path(args.offline)
        next_ids = json.loads((source / "next_ids.json").read_text())
        npc_rows = json.loads((source / "npcs.json").read_text())
        warp_rows = json.loads((source / "warptiles.json").read_text())
    else:
        next_ids = {kind: int(gsheets("nextid", sheet).strip()) for kind, sheet in SHEETS.items()}
        next_ids["req"] = int(gsheets("nextid", "Quest Reqs").strip())
        next_ids["reward"] = int(gsheets("nextid", "Quest Rewards").strip())
        npc_rows = read_sheet("NPCs")
        warp_rows = read_sheet("Warptiles")

    ops, revert, manual, config, counts, ids = build(next_ids, npc_rows, warp_rows)
    long = check_text(ops)
    if long:
        fail("text lines over 50 characters:\n  " + "\n  ".join(long))

    OUT.mkdir(exist_ok=True)
    (OUT / "batch.json").write_text(json.dumps(ops, indent=1, ensure_ascii=False) + "\n")
    (OUT / "revert.json").write_text(json.dumps(revert, indent=1, ensure_ascii=False) + "\n")
    (OUT / "manual.md").write_text(manual)
    (OUT / "ids.json").write_text(json.dumps(ids.ids, indent=1) + "\n")
    config_path = Path(args.config)
    merged = merge(json.loads(config_path.read_text()), config)
    target = config_path if args.in_place else OUT / "halloween.json"
    target.write_text(json.dumps(merged, indent=2, ensure_ascii=False) + "\n")

    per_sheet = {}
    for op in ops:
        per_sheet[f"{op['op']} {op['sheet']}"] = per_sheet.get(f"{op['op']} {op['sheet']}", 0) + 1
    print("operations:", json.dumps(per_sheet, indent=1))
    print("spawns by room:", json.dumps(counts, indent=1))
    missing = check_scripts(ops)
    if missing:
        print("scripts not in this checkout yet (fine before Parts 2-3 merge):\n  " + "\n  ".join(missing))


if __name__ == "__main__":
    main()
