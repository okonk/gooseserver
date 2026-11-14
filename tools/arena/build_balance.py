import sys
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.workbook.defined_name import DefinedName
from openpyxl.comments import Comment
from openpyxl.formatting.rule import FormulaRule
from openpyxl.utils import get_column_letter as L

out = sys.argv[1]
wb = Workbook()
F = "Arial"
BLUE = Font(name=F, color="0000FF")
BLACK = Font(name=F)
BOLD = Font(name=F, bold=True)
HEAD = Font(name=F, bold=True, color="FFFFFF")
HEADFILL = PatternFill("solid", fgColor="44546A")
GUESS = PatternFill("solid", fgColor="FFFF00")
SECTION = Font(name=F, bold=True, size=12)
thin = Side(style="thin", color="BFBFBF")

NAMES = {}
def name(ws, ref, nm):
    NAMES[nm] = f"{ws.title}!{ref}"

# ---------------- Guide ----------------
g = wb.active; g.title = "Guide"
lines = [
    ("Arena balance sandbox", SECTION),
    ("", None),
    ("How to use", BOLD),
    ("1. Change the blue numbers on the Inputs sheet. Everything on Rounds recalculates.", None),
    ("2. Yellow cells are guesses (player stats, XP proposal). Replace them with real numbers from logs or /arena preview.", None),
    ("3. Rounds shows, per round: mob HP, how long the group takes to clear, how fast each player profile dies, coins and proposed XP.", None),
    ("4. The summary at the top of Rounds shows the first round the group runs out of time, and the first round each profile dies before the wave is cleared.", None),
    ("", None),
    ("What the model matches in the server", BOLD),
    ("- Power fraction s = clamp(xpSold, minXpSold, topXpSold) / topXpSold  (ArenaMath.PowerFraction)", None),
    ("- Player damage to mobs = raw x s^(-a)  (ArenaMath.EffectiveDamage)", None),
    ("- Mob HP = anchorMobHp x hpGrowth^(round - anchorRound) x tier mult; boss also x (1 + bossHpPerPlayer x (alive - 1))", None),
    ("- Mob weapon damage = anchorMobWeaponDamage x dmgGrowth^(round - anchorRound) x tier mult x s^b", None),
    ("- Hit landed = weapon x (1 - DR) x (1 - AC x classMult / MaxAC) - AC x classMult / 25  (NPC.MitigateMelee; DR soft cap applied)", None),
    ("- Wave = baseCount + perPlayer x alive; boss rounds spawn ceil(that x bossRoundAddFraction) adds plus the boss", None),
    ("- Elite chance = 0 before eliteFromRound, then eliteChance + eliteChanceGrowth x rounds since, capped at eliteChanceMax", None),
    ("- Coins per player = roundBase + roundStep x round, plus the wave's bounty split evenly (the server splits by damage)", None),
    ("", None),
    ("Simplifications (not in the server)", BOLD),
    ("- Damage is averaged: no crits on mobs, no misses/dodges, no spells from class mobs, no movement or downtime.", None),
    ("- Group DPS is applied to the whole wave's HP pool, so overkill and walking between mobs are ignored. Add a slack factor on Inputs.", None),
    ("- 'Mobs on you' is how many mobs are hitting one player at once; real fights vary.", None),
    ("- XP columns are a proposal. The server currently gives each mob's template XP (909 - 120,000) with no round scaling.", None),
]
for i, (t, f) in enumerate(lines, 1):
    c = g.cell(i, 1, t); c.font = f or BLACK
g.column_dimensions["A"].width = 130

# ---------------- Inputs ----------------
ws = wb.create_sheet("Inputs")
ws.column_dimensions["A"].width = 30; ws.column_dimensions["B"].width = 16; ws.column_dimensions["C"].width = 70
r = 1
ws.cell(r, 1, "Arena settings (arena.json)").font = SECTION; r += 1
ws.cell(r, 1, "Blue = edit me. Defaults are the values shipped in arena.json today.").font = Font(name=F, italic=True); r += 2
settings = [
    ("Wave", None, None),
    ("baseCount", 4, "Mobs per wave before players"),
    ("perPlayer", 2, "Extra mobs per alive player"),
    ("roundSeconds", 180, "Time limit per round"),
    ("breakSeconds", 15, "Break between rounds"),
    ("Elites", None, None),
    ("eliteFromRound", 3, ""),
    ("eliteChance", 0.05, "Chance per mob at eliteFromRound"),
    ("eliteChanceGrowth", 0.02, "Added per round after that"),
    ("eliteChanceMax", 0.30, ""),
    ("eliteHpMult", 5, ""),
    ("eliteDmgMult", 2, ""),
    ("Bosses", None, None),
    ("bossEvery", 5, ""),
    ("bossRoundAddFraction", 0.5, "Boss rounds spawn this fraction of a normal wave as adds"),
    ("bossHpMult", 40, ""),
    ("bossHpPerPlayer", 0.25, "Boss HP +25% per extra alive player"),
    ("bossDmgMult", 3, ""),
    ("Scaling", None, None),
    ("minXpSold", 4000000, "Floor for the power fraction"),
    ("topXpSold", 3000000000, "XP sold that counts as full power (s = 1)"),
    ("a", 0.75, "Catch-up on damage dealt (higher = more help for low players)"),
    ("b", 0.75, "Catch-up on damage taken (higher = low players take less)"),
    ("anchorRound", 20, "Round where the anchor numbers apply"),
    ("anchorMobHp", 12000000, "Normal mob HP at the anchor round"),
    ("anchorMobWeaponDamage", 65000, "Normal mob weapon damage at the anchor round, vs a full-power player"),
    ("hpGrowth", 1.10, "Mob HP multiplier per round"),
    ("dmgGrowth", 1.08, "Mob damage multiplier per round"),
    ("Coins", None, None),
    ("roundBase", 5, ""),
    ("roundStep", 1, ""),
    ("mobBounty", 1, "Coins per normal kill (split between attackers)"),
    ("eliteBountyMult", 5, ""),
    ("bossBountyMult", 25, ""),
    ("Server settings", None, None),
    ("maxAC", 3500, "GooseSettings MaxAC"),
    ("drSoftCap", 0.5, "GooseSettings DamageReductionSoftCap"),
    ("mobAttackSeconds", 1.4, "Seconds between mob melee hits (template attack_speed; arena mobs are 1 - 1.8)"),
    ("Model assumptions", None, None),
    ("mobsOnYou", 2, "How many mobs hit one player at once"),
    ("dpsEfficiency", 0.7, "Fraction of the time players are actually hitting (walking, targeting, overkill)"),
    ("Proposed XP (not in the server yet)", None, None),
    ("xpAnchor", 50000, "XP per normal kill at the anchor round"),
    ("xpGrowth", 1.08, "XP multiplier per round"),
    ("eliteXpMult", 5, ""),
    ("bossXpMult", 25, ""),
    ("benchmarkXpPerHour", 20000000, "XP/hour at the best normal hunting spot, for comparison"),
]
for key, val, note in settings:
    if val is None:
        r += 1 if key != "Wave" else 0
        ws.cell(r, 1, key).font = BOLD; r += 1; continue
    ws.cell(r, 1, key).font = BLACK
    c = ws.cell(r, 2, val); c.font = BLUE
    if isinstance(val, float) and val < 1 and key not in ("a", "b"):
        c.number_format = "0.00"
    elif isinstance(val, int) and val >= 1000:
        c.number_format = "#,##0"
    if key.startswith("xp") or key in ("eliteXpMult", "bossXpMult", "benchmarkXpPerHour", "mobsOnYou", "dpsEfficiency", "mobAttackSeconds"):
        c.fill = GUESS
    ws.cell(r, 3, note).font = BLACK
    name(ws, f"$B${r}", key)
    r += 1

r += 1
ws.cell(r, 1, "Player profiles").font = SECTION; r += 1
ws.cell(r, 1, "Yellow = guesses. Raw hit is a typical hit before arena scaling, including crits on average. Replace with logged numbers.").font = Font(name=F, italic=True); r += 1
hdr = ["Profile", "In group", "XP sold", "Raw hit", "Hits / sec", "Max HP", "AC", "Class AC mult", "Damage reduction", "Regen + heals / sec",
       "s (power)", "Effective DPS", "Notes"]
pr0 = r
for i, h in enumerate(hdr, 1):
    c = ws.cell(r, i, h); c.font = HEAD; c.fill = HEADFILL; c.alignment = Alignment(wrap_text=True, vertical="center")
ws.row_dimensions[r].height = 30
profiles = [
    ("Top", 1, 3000000000, 5000000, 1.0, 60000, 1500, 1.0, 0.30, 800, "Design doc's round-20 example; raw hit is a guess"),
    ("Mid", 2, 300000000, 500000, 1.0, 25000, 900, 0.7, 0.15, 300, "Guess"),
    ("Low", 1, 20000000, 35000, 1.0, 8000, 400, 0.5, 0.05, 80, "Guess"),
]
for p in profiles:
    r += 1
    for i, v in enumerate(p[:10], 1):
        c = ws.cell(r, i, v); c.font = BLUE if i > 1 else BOLD
        if i > 2: c.fill = GUESS
        if i in (3, 4, 6, 7, 10): c.number_format = "#,##0"
        if i == 9: c.number_format = "0%"
    ws.cell(r, 11, f"=MIN(MAX(C{r},minXpSold),topXpSold)/topXpSold").number_format = "0.0000"
    ws.cell(r, 12, f"=D{r}*E{r}*K{r}^(-a)").number_format = "#,##0"
    ws.cell(r, 13, p[10]).font = BLACK
    for col in (11, 12): ws.cell(r, col).font = BLACK
pr1 = r
name(ws, f"$B${pr0+1}:$B${pr1}", "pCount")
name(ws, f"$L${pr0+1}:$L${pr1}", "pDps")
r += 1
ws.cell(r, 1, "Group").font = BOLD
ws.cell(r, 2, f"=SUM(B{pr0+1}:B{pr1})").font = BOLD
ws.cell(r, 11, "Group DPS").font = BOLD
c = ws.cell(r, 12, f"=SUMPRODUCT(B{pr0+1}:B{pr1},L{pr0+1}:L{pr1})*dpsEfficiency"); c.font = BOLD; c.number_format = "#,##0"
ws.cell(r, 13, "Sum of effective DPS x players x dpsEfficiency").font = BLACK
name(ws, f"$B${r}", "alive"); name(ws, f"$L${r}", "groupDps")
for i, w in enumerate([12, 10, 15, 13, 10, 11, 9, 12, 13, 14, 11, 14, 45], 1):
    if i > 3: ws.column_dimensions[L(i)].width = max(ws.column_dimensions[L(i)].width or 0, w)
PROF = [(p[0], pr0 + 1 + i) for i, p in enumerate(profiles)]

# ---------------- Rounds ----------------
rs = wb.create_sheet("Rounds")
rs.freeze_panes = "B7"
rs.cell(1, 1, "Per-round model").font = SECTION
rs.cell(2, 1, "First round the group can't clear in time:").font = BOLD
cols = ["Round", "Boss?", "Mobs", "Elite chance", "Exp. elites", "Normal HP", "Elite HP", "Boss HP", "Wave HP",
        "Clear time (s)", "Over limit?"]
for nm, _ in PROF:
    cols += [f"{nm}: hit taken", f"{nm}: dmg/sec in", f"{nm}: time to die (s)", f"{nm}: dies first?"]
cols += ["Coins / player", "Coins cumulative", "Exp. kills / player", "XP / player (proposed)", "XP / hour (proposed)", "XP/hr vs benchmark"]
HR = 6
for i, h in enumerate(cols, 1):
    c = rs.cell(HR, i, h); c.font = HEAD; c.fill = HEADFILL
    c.alignment = Alignment(wrap_text=True, vertical="center", horizontal="center")
    rs.column_dimensions[L(i)].width = 12
rs.row_dimensions[HR].height = 45
idx = {h: i for i, h in enumerate(cols, 1)}
C = lambda h: L(idx[h])
first, last = HR + 1, HR + 40
for n in range(1, 41):
    r = HR + n
    def put(h, f, fmt=None):
        c = rs.cell(r, idx[h], f); c.font = BLACK
        if fmt: c.number_format = fmt
        c.border = Border(bottom=thin)
    put("Round", n)
    put("Boss?", f"=IF(MOD(A{r},bossEvery)=0,\"Boss\",\"\")")
    put("Mobs", f"=IF({C('Boss?')}{r}=\"Boss\",ROUNDUP((baseCount+perPlayer*alive)*bossRoundAddFraction,0),baseCount+perPlayer*alive)")
    put("Elite chance", f"=IF(A{r}<eliteFromRound,0,MIN(eliteChance+eliteChanceGrowth*(A{r}-eliteFromRound),eliteChanceMax))", "0%")
    put("Exp. elites", f"={C('Mobs')}{r}*{C('Elite chance')}{r}", "0.0")
    put("Normal HP", f"=anchorMobHp*hpGrowth^(A{r}-anchorRound)", "#,##0")
    put("Elite HP", f"={C('Normal HP')}{r}*eliteHpMult", "#,##0")
    put("Boss HP", f"=IF({C('Boss?')}{r}=\"Boss\",{C('Normal HP')}{r}*bossHpMult*(1+bossHpPerPlayer*(alive-1)),0)", "#,##0;-#,##0;-")
    put("Wave HP", f"=({C('Mobs')}{r}-{C('Exp. elites')}{r})*{C('Normal HP')}{r}+{C('Exp. elites')}{r}*{C('Elite HP')}{r}+{C('Boss HP')}{r}", "#,##0")
    put("Clear time (s)", f"=IF(groupDps>0,{C('Wave HP')}{r}/groupDps,99999)", "#,##0")
    put("Over limit?", f"=IF({C('Clear time (s)')}{r}>roundSeconds,\"OVER\",\"\")")
    for nm, pr in PROF:
        s = f"Inputs!$K${pr}"; ac = f"Inputs!$G${pr}*Inputs!$H${pr}"; dr = f"Inputs!$I${pr}"
        effdr = f"IF({dr}<=drSoftCap,{dr},1-(1-drSoftCap)/(1+({dr}-drSoftCap)/(1-drSoftCap)))"
        weapon = f"anchorMobWeaponDamage*dmgGrowth^(A{r}-anchorRound)*{s}^b"
        tiermix = f"(1+{C('Elite chance')}{r}*(eliteDmgMult-1))"
        put(f"{nm}: hit taken", f"=MAX(0,{weapon}*{tiermix}*(1-{effdr})*(1-{ac}/maxAC)-{ac}/25)", "#,##0")
        put(f"{nm}: dmg/sec in", f"={C(nm+': hit taken')}{r}*mobsOnYou/mobAttackSeconds", "#,##0")
        put(f"{nm}: time to die (s)", f"=IF({C(nm+': dmg/sec in')}{r}-Inputs!$J${pr}<=0,99999,Inputs!$F${pr}/({C(nm+': dmg/sec in')}{r}-Inputs!$J${pr}))", "#,##0")
        put(f"{nm}: dies first?", f"=IF({C(nm+': time to die (s)')}{r}<MIN({C('Clear time (s)')}{r},roundSeconds),\"DIES\",\"\")")
    bounty = f"(({C('Mobs')}{r}-{C('Exp. elites')}{r})*mobBounty+{C('Exp. elites')}{r}*mobBounty*eliteBountyMult+IF({C('Boss?')}{r}=\"Boss\",mobBounty*bossBountyMult,0))"
    put("Coins / player", f"=roundBase+roundStep*A{r}+{bounty}/alive", "#,##0.0")
    put("Coins cumulative", f"=SUM(${C('Coins / player')}${first}:{C('Coins / player')}{r})", "#,##0")
    kills = f"({C('Mobs')}{r}+IF({C('Boss?')}{r}=\"Boss\",1,0))"
    put("Exp. kills / player", f"={kills}/alive", "0.0")
    xpw = f"(({C('Mobs')}{r}-{C('Exp. elites')}{r})+{C('Exp. elites')}{r}*eliteXpMult+IF({C('Boss?')}{r}=\"Boss\",bossXpMult,0))*xpAnchor*xpGrowth^(A{r}-anchorRound)"
    put("XP / player (proposed)", f"={xpw}/alive", "#,##0")
    put("XP / hour (proposed)", f"={C('XP / player (proposed)')}{r}*3600/(MIN({C('Clear time (s)')}{r},roundSeconds)+breakSeconds)", "#,##0")
    put("XP/hr vs benchmark", f"=IF(benchmarkXpPerHour>0,{C('XP / hour (proposed)')}{r}/benchmarkXpPerHour,0)", "0.00x")

def firstflag(col, text):
    return f"=IFERROR(INDEX($A${first}:$A${last},MATCH(\"{text}\",{col}${first}:{col}${last},0)),\"none by 40\")"
rs.cell(2, 5, firstflag(C("Over limit?"), "OVER")).font = BOLD
for i, (nm, _) in enumerate(PROF):
    rs.cell(3 + i, 1, f"First round {nm} dies before the wave is cleared:").font = BOLD
    rs.cell(3 + i, 5, firstflag(C(nm + ": dies first?"), "DIES")).font = BOLD
red = PatternFill("solid", fgColor="F4CCCC")
for h in ["Over limit?"] + [nm + ": dies first?" for nm, _ in PROF]:
    rng = f"{C(h)}{first}:{C(h)}{last}"
    rs.conditional_formatting.add(rng, FormulaRule(formula=[f'LEN({C(h)}{first})>0'], fill=red))
bossfill = PatternFill("solid", fgColor="EDEDED")
rs.conditional_formatting.add(f"A{first}:{L(len(cols))}{last}", FormulaRule(formula=[f'$B{first}="Boss"'], fill=bossfill))
rs.column_dimensions["A"].width = 8
for h in ("Normal HP", "Elite HP", "Boss HP", "Wave HP"): rs.column_dimensions[C(h)].width = 15

for sh in wb.worksheets:
    for row in sh.iter_rows():
        for c in row:
            if c.font and c.font.name != F:
                c.font = Font(name=F, bold=c.font.bold, italic=c.font.italic, color=c.font.color, size=c.font.size)
import re
pat = re.compile(r"(?<![A-Za-z0-9_!$])(" + "|".join(sorted(map(re.escape, NAMES), key=len, reverse=True)) + r")(?![A-Za-z0-9_(])")
for sh in wb.worksheets:
    for row in sh.iter_rows():
        for c in row:
            if isinstance(c.value, str) and c.value.startswith("="):
                c.value = pat.sub(lambda m: NAMES[m.group(1)], c.value)
wb.move_sheet("Inputs", offset=-1)
wb.active = 0
wb.calculation.fullCalcOnLoad = True
wb.save(out)
