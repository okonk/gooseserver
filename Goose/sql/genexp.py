import math

rates = [(2, 150000, 150000), (3, 100000, 200000), (4, 200000, 100000), (5, 180000, 120000)]

f = open('classes.sql', 'w')

for rate in rates:
    level = 1
    hp = 30
    mp = 30
    level_up_exp = 200
    tnl = level_up_exp
    while level != 51:
        f.write("INSERT INTO class_info (class_id, level, level_up_exp, player_hp, player_mp)" +
                " VALUES (" + str(rate[0]) + "," +
                             str(level) + "," +
                             str(level_up_exp) + "," +
                             str(hp) + "," +
                             str(mp) + ")\n")

        level = level + 1
        tnl = tnl + 200 * level
        level_up_exp = level_up_exp + tnl
        if level == 50: level_up_exp = 0
        hp = hp + int(math.ceil(level * 2 * (300000.0 / rate[1])))
        mp = mp + int(math.ceil(level * 2 * (300000.0 / rate[2])))
            

f.close()

