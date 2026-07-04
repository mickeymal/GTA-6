"""Save/load: serializes player, weapons, missions, world time/weather to JSON."""
import json
import os
from settings import SAVE_DIR
from weapons import WeaponInstance


def save_path(slot=1):
    return os.path.join(SAVE_DIR, f"slot{slot}.json")


def save_game(game, slot=1):
    p = game.player
    data = {
        "version": 1,
        "player": {
            "pos": [p.pos.x, p.pos.y],
            "hp": p.hp,
            "armor": p.armor,
            "money": p.money,
            "reputation": p.reputation,
            "flags": sorted(p.flags),
            "outfit": p.outfit,
            "owned_properties": p.owned_properties,
            "owned_vehicles": p.owned_vehicles,
            "current_cat": p.current_cat,
            "weapons": {
                cat: dict(key=w.key, mag=w.mag_ammo, reserve=w.reserve, mods=sorted(w.mods))
                for cat, w in p.weapons.items()
            },
        },
        "missions_completed": game.missions.state(),
        "world": {
            "time_of_day": game.world.time_of_day,
            "weather": game.world.weather,
        },
        "police_stars": game.police.stars,
    }
    os.makedirs(SAVE_DIR, exist_ok=True)
    with open(save_path(slot), "w") as f:
        json.dump(data, f, indent=2)
    return True


def load_game(game, slot=1):
    path = save_path(slot)
    if not os.path.exists(path):
        return False
    with open(path) as f:
        data = json.load(f)

    p = game.player
    pd = data["player"]
    p.pos.update(pd["pos"])
    p.hp = pd["hp"]
    p.armor = pd["armor"]
    p.money = pd["money"]
    p.reputation = pd["reputation"]
    p.flags = set(pd["flags"])
    p.outfit = pd["outfit"]
    p.owned_properties = pd["owned_properties"]
    p.owned_vehicles = pd["owned_vehicles"]
    p.dead = False
    p.vehicle = None
    p.weapons = {}
    for cat, wd in pd["weapons"].items():
        w = WeaponInstance(wd["key"], wd["reserve"])
        w.mods = set(wd["mods"])
        w.mag_ammo = min(wd["mag"], w.mag_size)
        p.weapons[cat] = w
    p.current_cat = pd["current_cat"] if pd["current_cat"] in p.weapons else "melee"

    game.missions.restore(data["missions_completed"])
    game.world.time_of_day = data["world"]["time_of_day"]
    game.world.weather = data["world"]["weather"]
    game.police.clear()
    game.police.stars = data.get("police_stars", 0)
    return True


def has_save(slot=1):
    return os.path.exists(save_path(slot))
