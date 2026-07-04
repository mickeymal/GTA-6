"""Side activities: street / boat / air races, vigilante missions (in a police
vehicle), and the Ammu-Nation shooting-range minigame."""
import math
import random
import pygame
from settings import *
from vehicle import make_vehicle


def t(x, y):
    return pygame.math.Vector2(x * TILE + TILE // 2, y * TILE + TILE // 2)


RACES = {
    "street": dict(name="Vice Street Circuit", start=t(120, 97), veh_cls="car", reward=8000,
                   min_alt=0, checkpoints=[t(120, 84), t(96, 84), t(96, 60), t(72, 60),
                                           t(72, 96), t(96, 96), t(120, 97)]),
    "boat": dict(name="Bay Sprint", start=t(176, 66), veh_cls="boat", reward=10000,
                 min_alt=0, checkpoints=[t(180, 90), t(178, 120), t(182, 150),
                                         t(160, 184), t(130, 186), t(110, 183)]),
    "air": dict(name="Airborne Gauntlet", start=t(40, 148), veh_cls="plane", reward=16000,
                min_alt=70, checkpoints=[t(80, 120), t(120, 100), t(150, 80),
                                         t(150, 130), t(100, 150), t(50, 152)]),
}


class Race:
    def __init__(self, game, race_id):
        self.game = game
        self.d = RACES[race_id]
        self.race_id = race_id
        self.checkpoints = list(self.d["checkpoints"])
        self.index = 0
        self.countdown = 3.0
        self.time = 0.0
        # rival "ghosts" progress along the route at randomized pace
        self.rivals = [dict(progress=0.0, speed=random.uniform(0.72, 0.98),
                            color=random.choice(NEON_COLORS)) for _ in range(3)]
        self.finished = False

    def rival_pos(self, r):
        route = [self.d["start"]] + self.checkpoints
        total = r["progress"] * (len(route) - 1)
        i = min(len(route) - 2, int(total))
        frac = total - i
        return route[i].lerp(route[i + 1], frac)

    def update(self, dt):
        g = self.game
        if self.countdown > 0:
            self.countdown -= dt
            if self.countdown <= 0:
                g.hud.notify("GO!", UI_MONEY)
            return
        self.time += dt
        v = g.player.vehicle
        if v is None or v.spec["cls"] != self.d["veh_cls"]:
            g.activities.end_race(False, "You left your vehicle!")
            return
        for r in self.rivals:
            r["progress"] = min(1.0, r["progress"] + r["speed"] * dt / 55.0)
        if self.index < len(self.checkpoints):
            cp = self.checkpoints[self.index]
            ok_alt = v.altitude >= self.d["min_alt"] or self.d["min_alt"] == 0
            if ok_alt and (v.pos - cp).length() < 110:
                self.index += 1
                g.hud.notify(f"Checkpoint {self.index}/{len(self.checkpoints)}", UI_ACCENT2)
        if self.index >= len(self.checkpoints):
            place = 1 + sum(1 for r in self.rivals if r["progress"] >= 1.0)
            won = place == 1
            g.activities.end_race(won, f"Finished P{place} in {self.time:.1f}s")

    def draw(self, surf, camera):
        if self.index < len(self.checkpoints):
            cp = self.checkpoints[self.index]
            sx, sy = camera.world_to_screen(cp)
            r = int((22 + 5 * math.sin(pygame.time.get_ticks() * 0.005)) * camera.zoom)
            pygame.draw.circle(surf, (60, 220, 255), (int(sx), int(sy)), r, 3)
        for r in self.rivals:
            if r["progress"] < 1.0:
                p = self.rival_pos(r)
                sx, sy = camera.world_to_screen(p)
                pygame.draw.circle(surf, r["color"], (int(sx), int(sy)), max(2, int(6 * camera.zoom)))

    def draw_hud(self, surf, font):
        if self.countdown > 0:
            n = font.render(str(max(1, int(self.countdown + 1))), True, UI_ACCENT)
            surf.blit(n, (SCREEN_W // 2 - n.get_width() // 2, 140))
        my_prog = self.index / max(1, len(self.checkpoints))
        place = 1 + sum(1 for r in self.rivals if r["progress"] > my_prog)
        txt = font.render(f"P{place}/4   {self.time:.1f}s   CP {self.index}/{len(self.checkpoints)}",
                          True, UI_TEXT)
        surf.blit(txt, (SCREEN_W // 2 - txt.get_width() // 2, 96))


class Vigilante:
    """In a police vehicle: chase down and destroy escalating criminal vehicles."""

    def __init__(self, game):
        self.game = game
        self.level = 1
        self.timer = 0.0
        self.target = None
        self._spawn_target()

    def _spawn_target(self):
        g = self.game
        from npc import TrafficDriver
        pos = g.police._road_pos_near(g.world, g.player.pos, 400, 800)
        if pos is None:
            pos = g.world.random_road_pos()
        kind = random.choice(["sedan", "muscle", "sports"])
        v = make_vehicle(kind, pos)
        d = TrafficDriver(v)
        d.panicked = True
        g.world.vehicles.append(v)
        g.world.npcs.append(d)
        self.target = v
        self.timer = 60.0
        g.hud.notify(f"Vigilante lvl {self.level}: destroy the fleeing {v.name}!", UI_ACCENT2)

    def update(self, dt):
        g = self.game
        pv = g.player.vehicle
        if pv is None or pv.kind not in ("police", "police_heli", "military"):
            g.activities.vigilante = None
            g.hud.notify("Vigilante cancelled - you left the patrol vehicle", UI_WARN)
            return
        self.timer -= dt
        if self.timer <= 0:
            g.activities.vigilante = None
            g.hud.notify("Vigilante failed - suspect escaped", UI_WARN)
            return
        if self.target.dead:
            reward = 1000 * self.level
            g.player.money += reward
            g.hud.notify(f"Suspect neutralized +${reward:,}", UI_MONEY)
            self.level += 1
            if self.level > 5:
                g.activities.vigilante = None
                g.hud.notify("Vigilante complete! City's finest.", UI_MONEY)
            else:
                self._spawn_target()

    def draw(self, surf, camera):
        if self.target and not self.target.dead:
            sx, sy = camera.world_to_screen(self.target.pos)
            pygame.draw.circle(surf, (255, 60, 60), (int(sx), int(sy)),
                               int(26 * camera.zoom), 2)


class ShootingRange:
    """Timed target practice next to Ammu-Nation."""

    def __init__(self, game):
        self.game = game
        self.timer = 30.0
        self.hits = 0
        self.target_pos = self._new_target()

    def _new_target(self):
        base = self.game.player.pos
        return base + pygame.math.Vector2(random.uniform(-260, 260), random.uniform(-200, 200))

    def check_hit(self, pos):
        if (pygame.math.Vector2(pos) - self.target_pos).length() < 26:
            self.hits += 1
            self.game.hud.notify(f"Hit! x{self.hits}", UI_ACCENT2)
            self.target_pos = self._new_target()

    def update(self, dt):
        g = self.game
        self.timer -= dt
        if self.timer <= 0:
            reward = self.hits * 150
            g.player.money += reward
            g.hud.notify(f"Range done: {self.hits} hits +${reward:,}", UI_MONEY)
            g.activities.range = None

    def draw(self, surf, camera):
        sx, sy = camera.world_to_screen(self.target_pos)
        for rr, c in ((16, (240, 240, 240)), (10, (220, 40, 40)), (4, (240, 240, 240))):
            pygame.draw.circle(surf, c, (int(sx), int(sy)), int(rr * camera.zoom))


class ActivityManager:
    def __init__(self, game):
        self.game = game
        self.race = None
        self.vigilante = None
        self.range = None
        self.font = pygame.font.SysFont("verdana", 26, bold=True)

    @property
    def busy(self):
        return self.race is not None

    # ------------------------------------------------------------ starting
    def try_start_nearby(self):
        """ENTER near an activity marker (when no mission is active)."""
        g = self.game
        p = g.player
        for rid, d in RACES.items():
            if (p.pos - d["start"]).length() < 70:
                v = p.vehicle
                if v is None or v.spec["cls"] != d["veh_cls"]:
                    need = {"car": "a car", "boat": "a boat", "plane": "a plane"}[d["veh_cls"]]
                    g.hud.notify(f"{d['name']}: come back in {need}", UI_WARN)
                    return True
                self.race = Race(g, rid)
                g.hud.notify(f"RACE: {d['name']} - 3 rivals. Get ready...", UI_ACCENT)
                return True
        gun_range = pygame.math.Vector2(t(85, 62))
        if (p.pos - gun_range).length() < 70 and self.range is None:
            self.range = ShootingRange(g)
            g.hud.notify("Shooting range: hit targets for 30s!", UI_ACCENT)
            return True
        return False

    def toggle_vigilante(self):
        g = self.game
        if self.vigilante:
            self.vigilante = None
            g.hud.notify("Vigilante cancelled", UI_WARN)
        elif g.player.vehicle and g.player.vehicle.kind in ("police", "police_heli", "military"):
            self.vigilante = Vigilante(g)
        else:
            g.hud.notify("Vigilante needs a police vehicle (press V inside one)", UI_WARN)

    def end_race(self, won, msg):
        g = self.game
        if won:
            reward = self.race.d["reward"]
            g.player.money += reward
            g.hud.notify(f"RACE WON! +${reward:,}", UI_MONEY, 5)
        else:
            g.hud.notify(f"Race over: {msg}", UI_WARN, 5)
        self.race = None

    # ------------------------------------------------------------ tick/draw
    def update(self, dt):
        if self.race:
            self.race.update(dt)
        if self.vigilante:
            self.vigilante.update(dt)
        if self.range:
            self.range.update(dt)

    def on_shot_fired(self, target_world_pos):
        if self.range:
            self.range.check_hit(target_world_pos)

    def draw_world(self, surf, camera):
        g = self.game
        # start markers (only when idle)
        if not self.busy and not g.missions.active:
            tt = pygame.time.get_ticks() * 0.003
            for rid, d in RACES.items():
                sx, sy = camera.world_to_screen(d["start"])
                if -40 < sx < SCREEN_W + 40 and -40 < sy < SCREEN_H + 40:
                    r = int((13 + 3 * math.sin(tt)) * camera.zoom)
                    pygame.draw.circle(surf, (60, 220, 255), (int(sx), int(sy)), r, 2)
        if self.race:
            self.race.draw(surf, camera)
        if self.vigilante:
            self.vigilante.draw(surf, camera)
        if self.range:
            self.range.draw(surf, camera)

    def draw_hud(self, surf):
        if self.race:
            self.race.draw_hud(surf, self.font)
        if self.vigilante:
            txt = self.font.render(f"VIGILANTE {int(self.vigilante.timer)}s", True, (120, 160, 255))
            surf.blit(txt, (SCREEN_W // 2 - txt.get_width() // 2, 96))
        if self.range:
            txt = self.font.render(f"RANGE {int(self.range.timer)}s  hits: {self.range.hits}",
                                   True, (240, 160, 60))
            surf.blit(txt, (SCREEN_W // 2 - txt.get_width() // 2, 96))
