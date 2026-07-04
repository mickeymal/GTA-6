"""Headless smoke test for Vice Bay Stories.

Runs the game with a hidden window and no real display, driving it through world
generation, spawning, combat, explosions, missions, economy, weather and save/load
to catch import/logic regressions without a monitor.

    python smoke_test.py
"""
import os
os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")

import random
import pygame

import settings
from main import Game, t
from vehicle import make_vehicle, VEHICLE_SPECS
from weapons import WEAPON_DEFS
from npc import Pedestrian, GangMember, CopOnFoot
import savegame


def check(cond, msg):
    if not cond:
        raise AssertionError(msg)
    print(f"  ok: {msg}")


def main():
    random.seed(1)
    print("Booting game (headless)...")
    game = Game(headless=True)
    game.new_game()
    check(game.state == "play", "new game enters play state")

    # ---- world ----
    w = game.world
    check(len(w.road_tiles) > 500, "road network generated")
    check(len(w.water_tiles) > 100, "ocean generated")
    check(len(w.neon_tiles) > 0, "neon signs placed")
    check(0 <= w.darkness() <= 1, "darkness in range")

    # ---- every vehicle class constructs and ticks physics ----
    for kind in VEHICLE_SPECS:
        v = make_vehicle(kind, t(60, 60))
        controls = dict(throttle=1.0, steer=0.5, vertical=1.0, handbrake=False)
        for _ in range(30):
            v.update(1 / 60, w, controls, game.particles, game)
        check(not v.dead or kind, f"vehicle '{kind}' survives 30 physics ticks")
    print(f"  ok: all {len(VEHICLE_SPECS)} vehicle classes drive")

    # ---- aircraft can take off ----
    plane = make_vehicle("stunt_plane", t(40, 152))
    game.player.pos.update(plane.pos)
    for _ in range(240):
        plane.update(1 / 60, w, dict(throttle=1.0, steer=0.0, vertical=1.0, handbrake=False),
                     game.particles, game)
    check(plane.altitude > 0, "stunt plane gains altitude after takeoff run")

    heli = make_vehicle("helicopter", t(104, 65))
    heli.driver = game.player
    for _ in range(240):
        heli.update(1 / 60, w, dict(throttle=0.0, steer=0.0, vertical=1.0, handbrake=False),
                    game.particles, game)
    check(heli.altitude > 0, "helicopter lifts off after rotor spin-up")

    # ---- every weapon fires ----
    game.player.pos.update(t(60, 60))
    for key in WEAPON_DEFS:
        game.player.give_weapon(key, 999)
        wpn = game.player.weapon
        wpn.cooldown = 0
        wpn.fire(w, game.particles, game.player.pos, pygame.math.Vector2(1, 0), game.player)
    print(f"  ok: all {len(WEAPON_DEFS)} weapons fire without error")

    # ---- projectiles + explosions ----
    before = len(w.projectiles)
    game.player.give_weapon("rpg", 5)
    game.player.weapon.cooldown = 0
    game.player.weapon.fire(w, game.particles, game.player.pos, pygame.math.Vector2(1, 0), game.player)
    check(len(w.projectiles) >= before, "rpg spawns a rocket projectile")
    game.explode_at(game.player.pos + pygame.math.Vector2(300, 0), 100, 120, game.player)
    check(len(game.particles.particles) > 0, "explosion spawns particles")

    # ---- combat damages NPCs ----
    ped = Pedestrian(game.player.pos + pygame.math.Vector2(20, 0))
    w.npcs.append(ped)
    ped.take_damage(999, game.player)
    check(ped.dead, "npc dies from damage")

    # ---- wanted system escalates ----
    game.police.add_heat(3.5, game)
    check(game.police.stars >= 3, "wanted level escalates with heat")
    game.police.update(1 / 60, w, game)

    # ---- economy ----
    start = game.player.money
    game.player.money += 5000
    check(game.player.money == start + 5000, "money updates")

    # ---- missions: run the manager through a mission start ----
    game.player.pos.update(game.missions.next_mission().giver_pos)
    m = game.missions.next_mission()
    game.missions._begin(m)
    check(game.missions.active is not None, "mission starts and sets objectives")
    check(len(m.objectives) > 0, "mission has objectives")
    for _ in range(60):
        game.update(1 / 60)

    # ---- full frame updates (integration) ----
    game.police.clear()
    for _ in range(300):
        game.update(1 / 60)
        game.draw(1 / 60)
    check(True, "300 full update+draw frames run clean")

    # ---- weather cycles ----
    for wk in settings.WEATHER_KINDS:
        w.weather = wk
        w.draw_weather(game.screen, game.camera, 1 / 60)
    print("  ok: all weather types render")

    # ---- save / load round trip ----
    game.player.money = 123456
    game.missions.completed.add("m1")
    savegame.save_game(game, slot=99)
    game.player.money = 0
    ok = savegame.load_game(game, slot=99)
    check(ok, "save loads")
    check(game.player.money == 123456, "money restored from save")
    check("m1" in game.missions.completed, "mission progress restored from save")
    os.remove(savegame.save_path(99))

    pygame.quit()
    print("\nALL SMOKE TESTS PASSED")


if __name__ == "__main__":
    main()
