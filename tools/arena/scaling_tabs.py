import json
import sys

sys.path.insert(0, "/home/hayden/code/illutiagooseserver/.agents/skills/goose-game-data/scripts")
import gsheets

SID = "1r73altQ4j1rNu0TjKbHR68CUBz6zCUfOloboGOACpAI"
DRY = "--dry" in sys.argv


def I(row):
    return f"Inputs!$B${row}"


BASE, PER, ELITE_FROM, ELITE_CH, ELITE_GROW, ELITE_MAX = I(5), I(6), I(11), I(12), I(13), I(14)
ELITE_HP, ELITE_DMG = I(15), I(16)
BOSS_EVERY, BOSS_FRAC, BOSS_HP, BOSS_PER, BOSS_CURVE = I(19), I(20), I(21), I(22), I(23)
BOSS_GROW, BOSS_SOFT, BOSS_LATE, BOSS_DMG = I(24), I(25), I(26), I(27)
MIN_XP, TOP_XP, A, B, ANCHOR = I(30), I(31), I(32), I(33), I(34)
ANCHOR_HP, ANCHOR_DMG, HP_GROW, SOFT, LATE, DMG_GROW = I(35), I(36), I(37), I(38), I(39), I(40)
XP_ANCHOR, XP_GROW, ELITE_XP, BOSS_XP = I(59), I(60), I(61), I(62)
TOP_ROW = 68


def hp_curve(r):
    return (f"{HP_GROW}^(MIN({r},{SOFT})-MIN({ANCHOR},{SOFT}))"
            f"*{LATE}^(MAX({r},{SOFT})-MAX({ANCHOR},{SOFT}))")


def normal_hp(r):
    return f"{ANCHOR_HP}*{hp_curve(r)}"


def boss_curve(r):
    own = (f"{BOSS_GROW}^(MIN({r},{BOSS_SOFT})-MIN({ANCHOR},{BOSS_SOFT}))"
           f"*{BOSS_LATE}^(MAX({r},{BOSS_SOFT})-MAX({ANCHOR},{BOSS_SOFT}))")
    return f"IF({BOSS_GROW}>0,{own},{hp_curve(r)})"


def boss_base_hp(r):
    return f"{ANCHOR_HP}*{boss_curve(r)}"


def weapon(r):
    return f"{ANCHOR_DMG}*{DMG_GROW}^({r}-{ANCHOR})"


def normal_xp(r):
    return f"{XP_ANCHOR}*{XP_GROW}^({r}-{ANCHOR})"


def is_boss(r):
    return f"MOD({r},{BOSS_EVERY})=0"


def elite_chance(r):
    return f"IF({r}<{ELITE_FROM},0,MIN({ELITE_CH}+{ELITE_GROW}*({r}-{ELITE_FROM}),{ELITE_MAX}))"


def boss_mult(n):
    return f"{BOSS_HP}*(1+{BOSS_PER}*MAX(0,{n}-1)^{BOSS_CURVE})"


def wave_xp(r, n):
    mobs = f"IF({is_boss(r)},ROUNDUP(({BASE}+{PER}*{n})*{BOSS_FRAC},0),{BASE}+{PER}*{n})"
    per_mob = f"(1+{elite_chance(r)}*({ELITE_XP}-1))"
    boss = f"IF({is_boss(r)},{BOSS_XP},0)"
    return f"{normal_xp(r)}*({mobs}*{per_mob}+{boss})"


def col(i):
    s = ""
    i += 1
    while i:
        i, rem = divmod(i - 1, 26)
        s = chr(65 + rem) + s
    return s


ROUNDS = 40
SIZES = list(range(1, 13))
tabs = {}
fmt = {}


def player_power():
    top_eff = f"(Inputs!$D${TOP_ROW}*Inputs!$K${TOP_ROW}^(-{A}))"
    v = [
        ["Player power scaling"],
        ["Raw hits grow with XP sold, then the arena multiplies them by s^-a. Raw hits are estimated from the Top profile on Inputs (its XP sold and raw hit) using the slope below. Blue cells are inputs."],
        ["Raw hit slope", 0.97, "Raw hit ~ XP sold ^ slope (design doc: 0.97 measured for spell damage)"],
        ["Round", 20, "Round used for the mob columns"],
        ["Fixed raw hit", 500000, "Comparison: everyone hitting for the same raw number"],
        [],
        ["XP sold", "s (power)", "Est. raw hit", "Damage dealt x", "Effective hit", "vs Top profile",
         "Normal mob HP", "Hits to kill a normal", "Effective hit (fixed raw)", "Hits to kill (fixed raw)",
         "Damage taken x", "Normal weapon dmg", "Elite weapon dmg", "Boss weapon dmg"],
    ]
    for i, xp in enumerate([20e6, 50e6, 100e6, 250e6, 500e6, 1e9, 2e9, 3e9, 5e9, 10e9]):
        r = 8 + i
        v.append([
            int(xp),
            f"=MIN(MAX(A{r},{MIN_XP}),{TOP_XP})/{TOP_XP}",
            f"=Inputs!$D${TOP_ROW}*(A{r}/Inputs!$C${TOP_ROW})^$B$3",
            f"=B{r}^(-{A})",
            f"=C{r}*D{r}",
            f"=E{r}/{top_eff}",
            f"={normal_hp('$B$4')}",
            f"=G{r}/E{r}",
            f"=$B$5*D{r}",
            f"=G{r}/I{r}",
            f"=B{r}^{B}",
            f"={weapon('$B$4')}*K{r}",
            f"=L{r}*{ELITE_DMG}",
            f"=L{r}*{BOSS_DMG}",
        ])
    v.append([])
    v.append(["a = 1 with a slope of 1 would make everyone equal. a = 0.75 keeps about (XP sold ratio)^0.25 of the grind advantage."])
    v.append(["Weapon damage is before armor and damage reduction. The Rounds tab applies mitigation per profile."])
    tabs["Player Power"] = v
    fmt["Player Power"] = dict(header=7, inputs=[("B3", "0.00"), ("B4:B5", "#,##0"), ("A8:A17", "#,##0")],
                               formats=[("B8:B17", "0.0000"), ("C8:C17", "#,##0"), ("D8:D17", "0.00"),
                                        ("E8:E17", "#,##0"), ("F8:F17", "0.00"), ("G8:G17", "#,##0"),
                                        ("H8:H17", "#,##0.0"), ("I8:I17", "#,##0"), ("J8:J17", "#,##0.0"),
                                        ("K8:K17", "0.000"), ("L8:N17", "#,##0")], freeze=7)


def mob_curves():
    v = [
        ["Mob scaling by round"],
        ["HP, weapon damage (at full power, s = 1) and XP per kill for each tier. Boss HP uses the boss growth curve (bossHpGrowth on Inputs)."],
        ["Round", "Boss?", "HP x anchor", "HP growth this round", "Boss HP x anchor", "Boss HP growth this round",
         "Normal HP", "Elite HP", "Boss HP (solo)", "Normal weapon dmg", "Elite weapon dmg", "Boss weapon dmg",
         "Normal XP", "Elite XP", "Boss XP", "Normal XP per 1,000 HP"],
    ]
    for i in range(ROUNDS):
        r = 4 + i
        v.append([
            i + 1,
            f'=IF({is_boss(f"A{r}")},"Boss","")',
            f"={hp_curve(f'A{r}')}",
            "" if i == 0 else f"=C{r}/C{r-1}",
            f"={boss_curve(f'A{r}')}",
            "" if i == 0 else f"=E{r}/E{r-1}",
            f"={ANCHOR_HP}*C{r}",
            f"=G{r}*{ELITE_HP}",
            f"={ANCHOR_HP}*E{r}*{boss_mult(1)}",
            f"={weapon(f'A{r}')}",
            f"=J{r}*{ELITE_DMG}",
            f"=J{r}*{BOSS_DMG}",
            f"={normal_xp(f'A{r}')}",
            f"=M{r}*{ELITE_XP}",
            f"=M{r}*{BOSS_XP}",
            f"=M{r}/G{r}*1000",
        ])
    last = 3 + ROUNDS
    tabs["Mob Curves"] = v
    fmt["Mob Curves"] = dict(header=3, inputs=[], formats=[(f"C4:C{last}", "#,##0.00"), (f"D4:D{last}", "0.000"),
                                                       (f"E4:E{last}", "#,##0.00"), (f"F4:F{last}", "0.000"),
                                                       (f"G4:O{last}", "#,##0"), (f"P4:P{last}", "#,##0.00")], freeze=3)


def boss_hp():
    last_c = col(len(SIZES))
    v = [
        ["Boss HP by players alive"],
        ["Boss HP = anchorMobHp x boss growth curve x bossHpMult x (1 + bossHpPerPlayer x (alive - 1)^bossHpPlayerCurve). Blue player counts are editable."],
        ["Players alive"] + SIZES,
        ["Boss HP multiplier"] + [f"={boss_mult(f'{col(j)}$3')}" for j in range(1, len(SIZES) + 1)],
        ["x solo boss"] + [f"={col(j)}4/$B$4" for j in range(1, len(SIZES) + 1)],
        ["Per-player share x solo"] + [f"={col(j)}5/{col(j)}3" for j in range(1, len(SIZES) + 1)],
        [],
        ["Boss HP"],
        ["Round"] + [f"={col(j)}3" for j in range(1, len(SIZES) + 1)],
    ]
    bosses = ROUNDS // 5
    for k in range(bosses):
        r = 10 + k
        v.append([f"={BOSS_EVERY}*{k + 1}"] + [f"={boss_base_hp(f'$A{r}')}*{col(j)}$4" for j in range(1, len(SIZES) + 1)])
    v.append([])
    start = 10 + bosses + 1
    v.append(["Boss HP per player"])
    v.append(["Round"] + [f"={col(j)}3" for j in range(1, len(SIZES) + 1)])
    for k in range(bosses):
        r = start + 2 + k
        src = 10 + k
        v.append([f"=A{src}"] + [f"={col(j)}{src}/{col(j)}$3" for j in range(1, len(SIZES) + 1)])
    end = start + 1 + bosses
    tabs["Boss HP"] = v
    fmt["Boss HP"] = dict(header=[3, 9, start + 1], inputs=[(f"B3:{last_c}3", "0")],
                          formats=[(f"B4:{last_c}4", "0.00"), (f"B5:{last_c}6", "0.00"),
                                   (f"B10:{last_c}{9 + bosses}", "#,##0"), (f"B{start + 2}:{last_c}{end}", "#,##0")],
                          bold=[8, start], freeze=3)


def group_xp():
    n = len(SIZES)
    last_c = col(n)
    v = [
        ["Group XP"],
        ["Server rules: grouped players each get kill XP x groupFactor^(members - 1). Ungrouped kills go to the top damage dealer, so XP splits roughly evenly. A player more than 9 levels above the mob gets 10%; that is not modelled here."],
        ["groupFactor", 0.91, "Group.GainExperience (Group.cs)"],
        [],
        ["Players"] + SIZES,
        ["Each player's share (ungrouped)"] + [f"=1/{col(j)}5" for j in range(1, n + 1)],
        ["Each player's share (grouped)"] + [f"=$B$3^({col(j)}5-1)" for j in range(1, n + 1)],
        ["Grouped / ungrouped"] + [f"={col(j)}7/{col(j)}6" for j in range(1, n + 1)],
        ["Total XP made per kill (grouped)"] + [f"={col(j)}5*{col(j)}7" for j in range(1, n + 1)],
        [],
        ["XP per player per wave, grouped"] + [""] * n + ["", "XP per player per wave, ungrouped"],
        ["Round"] + [f"={col(j)}$5" for j in range(1, n + 1)] + ["", "Round"]
        + [f"={col(j)}$5" for j in range(1, n + 1)],
    ]
    off = n + 2
    for i in range(ROUNDS):
        r = 13 + i
        row = [i + 1]
        row += [f"={wave_xp(f'$A{r}', f'{col(j)}$5')}*$B$3^({col(j)}$5-1)" for j in range(1, n + 1)]
        row += ["", f"=A{r}"]
        row += [f"={wave_xp(f'$A{r}', f'{col(j)}$5')}/{col(j)}$5" for j in range(1, n + 1)]
        v.append(row)
    last = 12 + ROUNDS
    tabs["Group XP"] = v
    fmt["Group XP"] = dict(header=[5, 12], inputs=[("B3", "0.00"), (f"B5:{last_c}5", "0")],
                           formats=[(f"B6:{last_c}8", "0.000"), (f"B9:{last_c}9", "0.00"),
                                    (f"B13:{last_c}{last}", "#,##0"),
                                    (f"{col(off + 1)}13:{col(off + n)}{last}", "#,##0")],
                           bold=[11], freeze=12)


player_power()
mob_curves()
boss_hp()
group_xp()

meta = json.loads(gsheets.gws(["get"], {"spreadsheetId": SID, "fields": "sheets.properties"}))
existing = {s["properties"]["title"]: s["properties"]["sheetId"] for s in meta["sheets"]}

reqs = []
for title in tabs:
    if title in existing:
        reqs.append({"deleteSheet": {"sheetId": existing[title]}})
for title in tabs:
    reqs.append({"addSheet": {"properties": {"title": title}}})
if DRY:
    print(json.dumps(tabs["Group XP"][12][:3], indent=1))
    sys.exit()
res = json.loads(gsheets.gws(["batchUpdate"], {"spreadsheetId": SID}, {"requests": reqs}))
ids = {}
for rep in res["replies"]:
    if "addSheet" in rep:
        p = rep["addSheet"]["properties"]
        ids[p["title"]] = p["sheetId"]

for t, v in tabs.items():
    for start in range(0, len(v), 4):
        gsheets.gws(["values", "update"],
                    {"spreadsheetId": SID, "range": f"'{t}'!A{start + 1}", "valueInputOption": "USER_ENTERED"},
                    {"values": v[start:start + 4]})


def grid(sheet_id, a1):
    import re
    m = re.fullmatch(r"([A-Z]+)(\d+)(?::([A-Z]+)(\d+))?", a1)
    c1, r1, c2, r2 = m.group(1), int(m.group(2)), m.group(3) or m.group(1), int(m.group(4) or m.group(2))

    def ci(c):
        n = 0
        for ch in c:
            n = n * 26 + ord(ch) - 64
        return n - 1
    return {"sheetId": sheet_id, "startRowIndex": r1 - 1, "endRowIndex": r2,
            "startColumnIndex": ci(c1), "endColumnIndex": ci(c2) + 1}


BLUE = {"red": 0, "green": 0, "blue": 1}
YELLOW = {"red": 1, "green": 1, "blue": 0.8}
fr = []
for title, f in fmt.items():
    sid = ids[title]
    fr.append({"repeatCell": {"range": {"sheetId": sid},
                              "cell": {"userEnteredFormat": {"textFormat": {"fontFamily": "Arial", "fontSize": 10}}},
                              "fields": "userEnteredFormat.textFormat"}})
    fr.append({"repeatCell": {"range": {"sheetId": sid, "startRowIndex": 0, "endRowIndex": 1},
                              "cell": {"userEnteredFormat": {"textFormat": {"fontFamily": "Arial", "fontSize": 14, "bold": True}}},
                              "fields": "userEnteredFormat.textFormat"}})
    headers = f["header"] if isinstance(f["header"], list) else [f["header"]]
    for h in headers + f.get("bold", []):
        fr.append({"repeatCell": {"range": {"sheetId": sid, "startRowIndex": h - 1, "endRowIndex": h},
                                  "cell": {"userEnteredFormat": {"textFormat": {"fontFamily": "Arial", "bold": True},
                                                                 "wrapStrategy": "WRAP"}},
                                  "fields": "userEnteredFormat.textFormat,userEnteredFormat.wrapStrategy"}})
    for a1, pattern in f["formats"]:
        fr.append({"repeatCell": {"range": grid(sid, a1),
                                  "cell": {"userEnteredFormat": {"numberFormat": {"type": "NUMBER", "pattern": pattern}}},
                                  "fields": "userEnteredFormat.numberFormat"}})
    for a1, pattern in f["inputs"]:
        fr.append({"repeatCell": {"range": grid(sid, a1),
                                  "cell": {"userEnteredFormat": {"numberFormat": {"type": "NUMBER", "pattern": pattern},
                                                                 "backgroundColor": YELLOW,
                                                                 "textFormat": {"fontFamily": "Arial", "foregroundColor": BLUE}}},
                                  "fields": "userEnteredFormat.numberFormat,userEnteredFormat.backgroundColor,userEnteredFormat.textFormat"}})
    fr.append({"updateSheetProperties": {"properties": {"sheetId": sid, "gridProperties": {"frozenRowCount": f["freeze"], "frozenColumnCount": 1}},
                                         "fields": "gridProperties.frozenRowCount,gridProperties.frozenColumnCount"}})
    fr.append({"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": 1},
                                             "properties": {"pixelSize": 210}, "fields": "pixelSize"}})
    fr.append({"updateDimensionProperties": {"range": {"sheetId": sid, "dimension": "COLUMNS", "startIndex": 1, "endIndex": 30},
                                             "properties": {"pixelSize": 120}, "fields": "pixelSize"}})
gsheets.gws(["batchUpdate"], {"spreadsheetId": SID}, {"requests": fr})
print("ok", ids)
