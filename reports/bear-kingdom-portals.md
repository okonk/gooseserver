# Bear Kingdom hole portals

Hole tile: graphic `710376` (layer 0) on `Map10043.map`. 617 hole tiles; 593 are walkable, not a warp, not an NPC spawn, and more than 3 tiles from an existing warp.
98 of them are used below, at least 4 tiles apart, with a fixed seed (43).

Destinations skip maps needing more than Bear Kingdom (200M exp): Winter Heights 1/2, Ancients Dungeon 4 (both) and 5. Also skipped: event/arena/GM/shop maps.

Status: **SAFE** = outside every mob aggro range even after max wander (spawn ±10); **SAFE-UNLESS-WANDER** = outside aggro of every spawn point, but a wandering mob could reach it; **IN-AGGRO** = no safe tile within 40 steps, so this is the tile with the fewest mobs in range.

## Destinations

| Dest map | Spot (map,x,y) | Near | Status | Walk to target |
|---|---|---|---|---|
| 1 Minita | `1,85,16` | MOBS Lamb x27 (lvl 2) | SAFE | 1 |
| 1 Minita | `1,89,12` | BOSS Black Sheep (lvl 7, 220 exp) | SAFE | 1 |
| 1 Minita | `1,89,73` | BOSS The Big Cheese (lvl 5, 180 exp) | SAFE | 1 |
| 2 Its the Bat Cave Robin | `2,30,23` | BOSS Nightfang (lvl 16, 400 exp) | SAFE-UNLESS-WANDER | 6 |
| 2 Its the Bat Cave Robin | `2,46,74` | BOSS Unmourned (lvl 40, 900 exp) | SAFE | 7 |
| 2 Its the Bat Cave Robin | `2,71,83` | MOBS Unloved x71 (lvl 35) | SAFE | 4 |
| 2 Its the Bat Cave Robin | `2,93,54` | BOSS Nibbles (lvl 45, 1200 exp) | SAFE | 9 |
| 3 Graveyard | `3,57,68` | MOBS Weak Zombie x71 (lvl 20) | SAFE-UNLESS-WANDER | 7 |
| 3 Graveyard | `3,98,80` | BOSS The Unburied (lvl 27, 570 exp) | SAFE-UNLESS-WANDER | 8 |
| 6 Undead Hallway | `6,2,42` | BOSS Rattlebones (lvl 38, 780 exp) | SAFE | 18 |
| 6 Undead Hallway | `6,8,80` | BOSS Misplaced Spouse (lvl 43, 1100 exp) | SAFE-UNLESS-WANDER | 17 |
| 6 Undead Hallway | `6,13,5` | BOSS Boney (lvl 42, 1000 exp) | SAFE-UNLESS-WANDER | 17 |
| 6 Undead Hallway | `6,97,74` | BOSS Record Keeper (lvl 45, 1200 exp); MOBS Servant of the Dead x39 (lvl 40) | SAFE | 7 |
| 7 Slime Kingdom | `7,35,6` | BOSS Sliminator (lvl 22, 448 exp) | SAFE-UNLESS-WANDER | 31 |
| 7 Slime Kingdom | `7,93,60` | BOSS Goo Jr. (lvl 28, 600 exp); MOBS Blue Slime x23 (lvl 20) | SAFE-UNLESS-WANDER | 7 |
| 7 Slime Kingdom | `7,97,91` | BOSS King Goo (lvl 20, 448 exp) | IN-AGGRO | 6 |
| 8 Roadkill Woods | `8,27,5` | BOSS Frantic Monkey (lvl 35, 600 exp) | SAFE-UNLESS-WANDER | 5 |
| 8 Roadkill Woods | `8,69,38` | MOBS Asp x57 (lvl 6) | SAFE-UNLESS-WANDER | 9 |
| 8 Roadkill Woods | `8,92,100` | BOSS Coilfang (lvl 8, 230 exp) | SAFE | 27 |
| 9 Punchys Playhouse | `9,41,12` | BOSS ImaBitStale (lvl 50, 1210 exp) | SAFE | 7 |
| 9 Punchys Playhouse | `9,80,59` | MOBS Ghast x128 (lvl 50) | SAFE | 5 |
| 9 Punchys Playhouse | `9,91,16` | BOSS Punchy (lvl 50, 2510 exp) | SAFE | 13 |
| 9 Punchys Playhouse | `9,97,92` | BOSS Ecto (lvl 50, 1820 exp) | SAFE | 6 |
| 10 Dead Forest | `10,3,58` | MOBS Naga Rogue x19 (lvl 30) | SAFE | 21 |
| 10 Dead Forest | `10,20,99` | BOSS Nagan Sentry (lvl 50, 2829 exp) | SAFE-UNLESS-WANDER | 5 |
| 11 Otherlands | `11,2,85` | BOSS Sergeant Persecution (lvl 45, 1309 exp) | SAFE | 4 |
| 11 Otherlands | `11,46,14` | BOSS Persecution Drillmaster (lvl 43, 1100 exp) | SAFE | 17 |
| 11 Otherlands | `11,93,6` | MOBS Strong Persecution x6 (lvl 50) | SAFE | 10 |
| 12 Mindless Mines | `12,46,44` | MOBS Strong Persecution x76 (lvl 50) | SAFE-UNLESS-WANDER | 5 |
| 12 Mindless Mines | `12,94,91` | BOSS Complex (lvl 50, 1210 exp) | SAFE | 16 |
| 13 Hay and Frays Stronghold | `13,6,7` | BOSS Hay (lvl 50, 3000 exp) | SAFE | 7 |
| 13 Hay and Frays Stronghold | `13,22,7` | BOSS Fray (lvl 50, 2710 exp) | SAFE | 7 |
| 13 Hay and Frays Stronghold | `13,40,51` | MOBS One with Head Thingy x236 (lvl 50) | SAFE | 11 |
| 13 Hay and Frays Stronghold | `13,94,8` | BOSS Little Fat Bastard (lvl 50, 1100 exp) | SAFE | 4 |
| 14 Chiisaiyama | `14,34,10` | BOSS Mad Cow (lvl 29, 590 exp) | SAFE | 14 |
| 14 Chiisaiyama | `14,43,17` | MOBS Cow x36 (lvl 21) | SAFE | 1 |
| 14 Chiisaiyama | `14,77,60` | BOSS Cranky Ewe (lvl 50, 2000 exp) | SAFE | 1 |
| 16 Arctic Lands | `16,50,46` | MOBS Rabid Rabbit x59 (lvl 8) | SAFE | 3 |
| 17 Northern Arctic Lands | `17,49,66` | MOBS Melty x31 (lvl 28) | SAFE | 4 |
| 17 Northern Arctic Lands | `17,57,14` | BOSS Fluffzilla (lvl 32, 608 exp) | SAFE | 8 |
| 17 Northern Arctic Lands | `17,99,87` | BOSS Carrots (lvl 33, 608 exp) | SAFE | 5 |
| 18 Frozen Lake | `18,21,6` | BOSS Chilly (lvl 36, 700 exp) | SAFE | 5 |
| 18 Frozen Lake | `18,27,19` | MOBS Frozen Waste x74 (lvl 35) | SAFE | 3 |
| 18 Frozen Lake | `18,74,8` | BOSS Frosty (lvl 45, 2109 exp) | SAFE-UNLESS-WANDER | 11 |
| 19 Frigid Maze | `19,37,55` | BOSS Roger (lvl 14, 356 exp) | SAFE | 6 |
| 19 Frigid Maze | `19,68,49` | MOBS Cute Defenseless Stalker x72 (lvl 15) | SAFE | 3 |
| 19 Frigid Maze | `19,96,42` | BOSS Leafeater (lvl 17, 406 exp) | SAFE | 5 |
| 24 The Ancients Dungeon 2 | `24,32,95` | BOSS Ancient Sentinel (lvl 50, 80000 exp) | SAFE | 7 |
| 24 The Ancients Dungeon 2 | `24,46,55` | BOSS Doom (lvl 50, 108000 exp) | SAFE | 5 |
| 24 The Ancients Dungeon 2 | `24,49,79` | MOBS Ancient Defender x37 (lvl 50) | SAFE | 4 |
| 24 The Ancients Dungeon 2 | `24,76,97` | BOSS Ancient Sentinel (lvl 50, 80000 exp) | SAFE | 4 |
| 25 Boondocks | `25,8,76` | BOSS Flying Piglet (lvl 23, 600 exp) | SAFE | 14 |
| 25 Boondocks | `25,26,3` | BOSS Flying Pipsqueek (lvl 14, 400 exp) | SAFE-UNLESS-WANDER | 16 |
| 25 Boondocks | `25,28,98` | BOSS Flying Piglet (lvl 23, 600 exp) | SAFE | 14 |
| 25 Boondocks | `25,55,97` | MOBS Piglet x42 (lvl 21) | SAFE | 22 |
| 25 Boondocks | `25,97,20` | BOSS Flying Pipsqueek (lvl 14, 400 exp) | SAFE-UNLESS-WANDER | 7 |
| 27 Nagan Oasis | `27,54,44` | MOBS Nagan Beast x99 (lvl 50) | SAFE | 3 |
| 27 Nagan Oasis | `27,76,98` | BOSS Udyana (lvl 50, 6000 exp) | SAFE | 15 |
| 28 Savage Isle | `28,43,70` | BOSS Insanity (lvl 50, 2610 exp); BOSS Showboat (lvl 50, 2310 exp); BOSS Copycat (lvl 50, 1910 exp) | SAFE | 7 |
| 28 Savage Isle | `28,55,70` | MOBS Paranoid Savage x30 (lvl 50) | SAFE | 5 |
| 28 Savage Isle | `28,61,9` | BOSS Beefcake (lvl 50, 6000 exp) | SAFE | 12 |
| 29 The Ancients Dungeon | `29,8,19` | BOSS Ztwel Tahp (lvl 50, 100000 exp) | SAFE-UNLESS-WANDER | 10 |
| 29 The Ancients Dungeon | `29,34,95` | BOSS Hairy Fairy Princess (lvl 50, 100000 exp) | SAFE | 32 |
| 29 The Ancients Dungeon | `29,63,89` | MOBS Comissioned Blacksmith x6 (lvl 50) | SAFE | 32 |
| 29 The Ancients Dungeon | `29,73,72` | BOSS Dalph Von'Ownu (lvl 50, 100000 exp) | SAFE | 21 |
| 29 The Ancients Dungeon | `29,98,8` | BOSS Lady Slither (lvl 50, 100000 exp); BOSS Sir Insidious (lvl 50, 100000 exp) | SAFE-UNLESS-WANDER | 28 |
| 32 Sorrows Grove | `32,34,100` | MOBS Pancake x53 (lvl 50) | SAFE | 20 |
| 32 Sorrows Grove | `32,89,100` | BOSS Sorrows Lover (lvl 50, 8000 exp) | IN-AGGRO | 8 |
| 33 The Passing | `33,16,100` | BOSS Invisible 1 (lvl 50, 32000 exp) | IN-AGGRO | 15 |
| 33 The Passing | `33,27,48` | MOBS Giant Wraith x119 (lvl 50) | IN-AGGRO | 9 |
| 38 Pumpkin Grove | `38,34,43` | MOBS Chief Pumpkin x233 (lvl 30) | SAFE | 2 |
| 38 Pumpkin Grove | `38,47,13` | BOSS Boss Pumpkin (lvl 35, 2000 exp) | SAFE | 5 |
| 38 Pumpkin Grove | `38,95,73` | BOSS Fail Pumpkin (lvl 25, 1000 exp) | SAFE | 2 |
| 42 Rugged Valley | `42,1,6` | BOSS Gramps (lvl 50, 16400 exp) | SAFE-UNLESS-WANDER | 13 |
| 42 Rugged Valley | `42,21,39` | BOSS Betsy the Bear Charmer (lvl 50, 7210 exp) | SAFE-UNLESS-WANDER | 9 |
| 42 Rugged Valley | `42,50,39` | MOBS Young Bear x155 (lvl 50) | SAFE-UNLESS-WANDER | 4 |
| 42 Rugged Valley | `42,99,49` | BOSS Berry Stealer (lvl 50, 1400 exp) | SAFE-UNLESS-WANDER | 9 |
| 44 Starving Pits | `44,51,79` | BOSS The Patriarch (lvl 50, 15410 exp) | SAFE-UNLESS-WANDER | 8 |
| 44 Starving Pits | `44,63,99` | MOBS Starved Bear x25 (lvl 50) | SAFE-UNLESS-WANDER | 17 |
| 45 Maroon Bay | `45,43,64` | BOSS Captain Persecution (lvl 46, 1300 exp) | SAFE-UNLESS-WANDER | 7 |
| 45 Maroon Bay | `45,94,37` | MOBS Charred Persecution x20 (lvl 47) | SAFE-UNLESS-WANDER | 40 |
| 45 Maroon Bay | `45,100,37` | BOSS Admiral Persecution (lvl 48, 1500 exp) | SAFE-UNLESS-WANDER | 30 |
| 46 Minita Sewers | `46,17,6` | BOSS The Big Ketchup (lvl 50, 10258 exp) | SAFE | 6 |
| 46 Minita Sewers | `46,33,43` | BOSS Sir Rots-a-Lot (lvl 50, 15358 exp) | SAFE-UNLESS-WANDER | 5 |
| 46 Minita Sewers | `46,36,46` | MOBS Moldy Purple x9 (lvl 50) | SAFE-UNLESS-WANDER | 13 |
| 46 Minita Sewers | `46,94,28` | BOSS Barbie Goo (lvl 50, 10987 exp) | SAFE-UNLESS-WANDER | 6 |
| 49 The Ancients Dungeon 3 | `49,17,19` | BOSS Disciple Von Chief (lvl 50, 108000 exp) | SAFE | 5 |
| 49 The Ancients Dungeon 3 | `49,34,47` | BOSS General Vor Chez (lvl 50, 108000 exp) | SAFE | 7 |
| 49 The Ancients Dungeon 3 | `49,88,39` | BOSS General Mehoff (lvl 50, 108000 exp); MOBS Ancient Guard x37 (lvl 50) | SAFE | 23 |
| 49 The Ancients Dungeon 3 | `49,92,20` | BOSS Disciple Von Bangs (lvl 50, 108000 exp) | SAFE | 5 |
| 50 Its the Bat Cave Robin | `50,81,22` | MOBS Wraith x35 (lvl 50) | SAFE | 15 |
| 50 Its the Bat Cave Robin | `50,81,32` | BOSS Nibbles II (lvl 50, 8000 exp) | SAFE | 15 |
| 51 Sorrows Grove 2 | `51,14,26` | BOSS Sorrows Hero (lvl 50, 16000 exp) | SAFE | 15 |
| 51 Sorrows Grove 2 | `51,77,31` | MOBS Flapjack x82 (lvl 50) | SAFE | 17 |
| 52 The Passing 2 | `52,71,13` | BOSS Invisible 2 (lvl 50, 64000 exp) | IN-AGGRO | 17 |
| 52 The Passing 2 | `52,85,48` | MOBS Returned x143 (lvl 50) | IN-AGGRO | 9 |
| 54 Crystal Cavern | `54,27,59` | MOBS Elder Cave Bear x18 (lvl 50) | SAFE-UNLESS-WANDER | 4 |
| 54 Crystal Cavern | `54,64,42` | BOSS Grand Gloop (lvl 50, 25000 exp) | SAFE-UNLESS-WANDER | 10 |

## Hole → destination (Warptiles rows)

| BK x | BK y | warp map | warp x | warp y | dest | near |
|---|---|---|---|---|---|---|
| 78 | 3 | 8 | 69 | 38 | Roadkill Woods | MOBS Asp x57 (lvl 6) |
| 70 | 5 | 38 | 47 | 13 | Pumpkin Grove | BOSS Boss Pumpkin (lvl 35, 2000 exp) |
| 72 | 10 | 9 | 41 | 12 | Punchys Playhouse | BOSS ImaBitStale (lvl 50, 1210 exp) |
| 42 | 11 | 45 | 94 | 37 | Maroon Bay | MOBS Charred Persecution x20 (lvl 47) |
| 35 | 12 | 18 | 21 | 6 | Frozen Lake | BOSS Chilly (lvl 36, 700 exp) |
| 77 | 12 | 8 | 27 | 5 | Roadkill Woods | BOSS Frantic Monkey (lvl 35, 600 exp) |
| 46 | 13 | 33 | 27 | 48 | The Passing | MOBS Giant Wraith x119 (lvl 50) |
| 21 | 14 | 13 | 40 | 51 | Hay and Frays Stronghold | MOBS One with Head Thingy x236 (lvl 50) |
| 25 | 17 | 49 | 88 | 39 | The Ancients Dungeon 3 | BOSS General Mehoff (lvl 50, 108000 exp) |
| 18 | 19 | 10 | 3 | 58 | Dead Forest | MOBS Naga Rogue x19 (lvl 30) |
| 47 | 21 | 29 | 73 | 72 | The Ancients Dungeon | BOSS Dalph Von'Ownu (lvl 50, 100000 exp) |
| 39 | 22 | 2 | 46 | 74 | Its the Bat Cave Robin | BOSS Unmourned (lvl 40, 900 exp) |
| 78 | 23 | 32 | 89 | 100 | Sorrows Grove | BOSS Sorrows Lover (lvl 50, 8000 exp) |
| 22 | 24 | 29 | 98 | 8 | The Ancients Dungeon | BOSS Lady Slither (lvl 50, 100000 exp) |
| 26 | 24 | 1 | 89 | 73 | Minita | BOSS The Big Cheese (lvl 5, 180 exp) |
| 30 | 24 | 11 | 2 | 85 | Otherlands | BOSS Sergeant Persecution (lvl 45, 1309 exp) |
| 71 | 25 | 6 | 8 | 80 | Undead Hallway | BOSS Misplaced Spouse (lvl 43, 1100 exp) |
| 86 | 25 | 27 | 54 | 44 | Nagan Oasis | MOBS Nagan Beast x99 (lvl 50) |
| 48 | 28 | 28 | 43 | 70 | Savage Isle | BOSS Insanity (lvl 50, 2610 exp) |
| 13 | 29 | 38 | 95 | 73 | Pumpkin Grove | BOSS Fail Pumpkin (lvl 25, 1000 exp) |
| 36 | 29 | 54 | 27 | 59 | Crystal Cavern | MOBS Elder Cave Bear x18 (lvl 50) |
| 40 | 31 | 28 | 61 | 9 | Savage Isle | BOSS Beefcake (lvl 50, 6000 exp) |
| 50 | 32 | 9 | 97 | 92 | Punchys Playhouse | BOSS Ecto (lvl 50, 1820 exp) |
| 66 | 32 | 51 | 77 | 31 | Sorrows Grove 2 | MOBS Flapjack x82 (lvl 50) |
| 70 | 32 | 25 | 97 | 20 | Boondocks | BOSS Flying Pipsqueek (lvl 14, 400 exp) |
| 89 | 32 | 50 | 81 | 22 | Its the Bat Cave Robin | MOBS Wraith x35 (lvl 50) |
| 57 | 33 | 29 | 63 | 89 | The Ancients Dungeon | MOBS Comissioned Blacksmith x6 (lvl 50) |
| 14 | 34 | 6 | 97 | 74 | Undead Hallway | BOSS Record Keeper (lvl 45, 1200 exp) |
| 68 | 37 | 14 | 43 | 17 | Chiisaiyama | MOBS Cow x36 (lvl 21) |
| 77 | 37 | 24 | 76 | 97 | The Ancients Dungeon 2 | BOSS Ancient Sentinel (lvl 50, 80000 exp) |
| 85 | 37 | 25 | 55 | 97 | Boondocks | MOBS Piglet x42 (lvl 21) |
| 18 | 38 | 19 | 96 | 42 | Frigid Maze | BOSS Leafeater (lvl 17, 406 exp) |
| 25 | 38 | 29 | 34 | 95 | The Ancients Dungeon | BOSS Hairy Fairy Princess (lvl 50, 100000 exp) |
| 36 | 39 | 42 | 1 | 6 | Rugged Valley | BOSS Gramps (lvl 50, 16400 exp) |
| 63 | 39 | 17 | 49 | 66 | Northern Arctic Lands | MOBS Melty x31 (lvl 28) |
| 7 | 40 | 45 | 43 | 64 | Maroon Bay | BOSS Captain Persecution (lvl 46, 1300 exp) |
| 32 | 40 | 11 | 46 | 14 | Otherlands | BOSS Persecution Drillmaster (lvl 43, 1100 exp) |
| 51 | 40 | 24 | 46 | 55 | The Ancients Dungeon 2 | BOSS Doom (lvl 50, 108000 exp) |
| 91 | 40 | 45 | 100 | 37 | Maroon Bay | BOSS Admiral Persecution (lvl 48, 1500 exp) |
| 86 | 41 | 50 | 81 | 32 | Its the Bat Cave Robin | BOSS Nibbles II (lvl 50, 8000 exp) |
| 71 | 42 | 44 | 63 | 99 | Starving Pits | MOBS Starved Bear x25 (lvl 50) |
| 26 | 46 | 6 | 2 | 42 | Undead Hallway | BOSS Rattlebones (lvl 38, 780 exp) |
| 32 | 46 | 3 | 57 | 68 | Graveyard | MOBS Weak Zombie x71 (lvl 20) |
| 18 | 49 | 49 | 92 | 20 | The Ancients Dungeon 3 | BOSS Disciple Von Bangs (lvl 50, 108000 exp) |
| 23 | 50 | 8 | 92 | 100 | Roadkill Woods | BOSS Coilfang (lvl 8, 230 exp) |
| 28 | 50 | 42 | 50 | 39 | Rugged Valley | MOBS Young Bear x155 (lvl 50) |
| 34 | 50 | 14 | 34 | 10 | Chiisaiyama | BOSS Mad Cow (lvl 29, 590 exp) |
| 72 | 51 | 38 | 34 | 43 | Pumpkin Grove | MOBS Chief Pumpkin x233 (lvl 30) |
| 80 | 51 | 2 | 71 | 83 | Its the Bat Cave Robin | MOBS Unloved x71 (lvl 35) |
| 41 | 53 | 24 | 49 | 79 | The Ancients Dungeon 2 | MOBS Ancient Defender x37 (lvl 50) |
| 49 | 53 | 52 | 85 | 48 | The Passing 2 | MOBS Returned x143 (lvl 50) |
| 20 | 54 | 54 | 64 | 42 | Crystal Cavern | BOSS Grand Gloop (lvl 50, 25000 exp) |
| 36 | 54 | 49 | 17 | 19 | The Ancients Dungeon 3 | BOSS Disciple Von Chief (lvl 50, 108000 exp) |
| 86 | 56 | 12 | 94 | 91 | Mindless Mines | BOSS Complex (lvl 50, 1210 exp) |
| 45 | 57 | 2 | 93 | 54 | Its the Bat Cave Robin | BOSS Nibbles (lvl 45, 1200 exp) |
| 4 | 58 | 3 | 98 | 80 | Graveyard | BOSS The Unburied (lvl 27, 570 exp) |
| 11 | 58 | 24 | 32 | 95 | The Ancients Dungeon 2 | BOSS Ancient Sentinel (lvl 50, 80000 exp) |
| 17 | 58 | 51 | 14 | 26 | Sorrows Grove 2 | BOSS Sorrows Hero (lvl 50, 16000 exp) |
| 78 | 58 | 7 | 93 | 60 | Slime Kingdom | BOSS Goo Jr. (lvl 28, 600 exp) |
| 42 | 61 | 42 | 99 | 49 | Rugged Valley | BOSS Berry Stealer (lvl 50, 1400 exp) |
| 23 | 62 | 46 | 17 | 6 | Minita Sewers | BOSS The Big Ketchup (lvl 50, 10258 exp) |
| 48 | 62 | 25 | 26 | 3 | Boondocks | BOSS Flying Pipsqueek (lvl 14, 400 exp) |
| 79 | 63 | 42 | 21 | 39 | Rugged Valley | BOSS Betsy the Bear Charmer (lvl 50, 7210 exp) |
| 32 | 64 | 10 | 20 | 99 | Dead Forest | BOSS Nagan Sentry (lvl 50, 2829 exp) |
| 85 | 64 | 19 | 37 | 55 | Frigid Maze | BOSS Roger (lvl 14, 356 exp) |
| 75 | 65 | 44 | 51 | 79 | Starving Pits | BOSS The Patriarch (lvl 50, 15410 exp) |
| 42 | 67 | 2 | 30 | 23 | Its the Bat Cave Robin | BOSS Nightfang (lvl 16, 400 exp) |
| 23 | 68 | 16 | 50 | 46 | Arctic Lands | MOBS Rabid Rabbit x59 (lvl 8) |
| 73 | 69 | 32 | 34 | 100 | Sorrows Grove | MOBS Pancake x53 (lvl 50) |
| 5 | 71 | 46 | 94 | 28 | Minita Sewers | BOSS Barbie Goo (lvl 50, 10987 exp) |
| 14 | 71 | 28 | 55 | 70 | Savage Isle | MOBS Paranoid Savage x30 (lvl 50) |
| 41 | 72 | 25 | 8 | 76 | Boondocks | BOSS Flying Piglet (lvl 23, 600 exp) |
| 82 | 72 | 13 | 22 | 7 | Hay and Frays Stronghold | BOSS Fray (lvl 50, 2710 exp) |
| 86 | 73 | 11 | 93 | 6 | Otherlands | MOBS Strong Persecution x6 (lvl 50) |
| 31 | 74 | 17 | 57 | 14 | Northern Arctic Lands | BOSS Fluffzilla (lvl 32, 608 exp) |
| 47 | 76 | 14 | 77 | 60 | Chiisaiyama | BOSS Cranky Ewe (lvl 50, 2000 exp) |
| 56 | 76 | 9 | 80 | 59 | Punchys Playhouse | MOBS Ghast x128 (lvl 50) |
| 67 | 76 | 13 | 94 | 8 | Hay and Frays Stronghold | BOSS Little Fat Bastard (lvl 50, 1100 exp) |
| 19 | 77 | 13 | 6 | 7 | Hay and Frays Stronghold | BOSS Hay (lvl 50, 3000 exp) |
| 36 | 77 | 7 | 97 | 91 | Slime Kingdom | BOSS King Goo (lvl 20, 448 exp) |
| 82 | 77 | 18 | 74 | 8 | Frozen Lake | BOSS Frosty (lvl 45, 2109 exp) |
| 78 | 78 | 49 | 34 | 47 | The Ancients Dungeon 3 | BOSS General Vor Chez (lvl 50, 108000 exp) |
| 94 | 78 | 46 | 36 | 46 | Minita Sewers | MOBS Moldy Purple x9 (lvl 50) |
| 71 | 79 | 46 | 33 | 43 | Minita Sewers | BOSS Sir Rots-a-Lot (lvl 50, 15358 exp) |
| 86 | 79 | 17 | 99 | 87 | Northern Arctic Lands | BOSS Carrots (lvl 33, 608 exp) |
| 55 | 80 | 29 | 8 | 19 | The Ancients Dungeon | BOSS Ztwel Tahp (lvl 50, 100000 exp) |
| 65 | 81 | 25 | 28 | 98 | Boondocks | BOSS Flying Piglet (lvl 23, 600 exp) |
| 78 | 82 | 52 | 71 | 13 | The Passing 2 | BOSS Invisible 2 (lvl 50, 64000 exp) |
| 15 | 84 | 1 | 89 | 12 | Minita | BOSS Black Sheep (lvl 7, 220 exp) |
| 26 | 84 | 27 | 76 | 98 | Nagan Oasis | BOSS Udyana (lvl 50, 6000 exp) |
| 33 | 85 | 18 | 27 | 19 | Frozen Lake | MOBS Frozen Waste x74 (lvl 35) |
| 80 | 86 | 7 | 35 | 6 | Slime Kingdom | BOSS Sliminator (lvl 22, 448 exp) |
| 6 | 87 | 33 | 16 | 100 | The Passing | BOSS Invisible 1 (lvl 50, 32000 exp) |
| 89 | 88 | 9 | 91 | 16 | Punchys Playhouse | BOSS Punchy (lvl 50, 2510 exp) |
| 83 | 90 | 19 | 68 | 49 | Frigid Maze | MOBS Cute Defenseless Stalker x72 (lvl 15) |
| 49 | 92 | 12 | 46 | 44 | Mindless Mines | MOBS Strong Persecution x76 (lvl 50) |
| 10 | 94 | 1 | 85 | 16 | Minita | MOBS Lamb x27 (lvl 2) |
| 4 | 95 | 6 | 13 | 5 | Undead Hallway | BOSS Boney (lvl 42, 1000 exp) |
