"""PoliceSystem: wanted stars 1-5 with escalating response.

1★ cops on foot · 2★ cruiser pursuit · 3★ roadblocks + SWAT · 4★ police helicopter
5★ military patriots. Heat decays while out of sight; Pay'n'Spray clears it.
"""
import math
import random
import pygame
from settings import *
from npc import CopOnFoot
from vehicle import make_vehicle
from weapons import WeaponInstance


class PursuitDriver:
    """AI driver chasing the player in a cruiser / military vehicle."""

    def __init__(self, vehicle, aggressive=False):
        self.vehicle = vehicle
        vehicle.driver = self
        vehicle.siren = True
        self.dead = False
        self.pos = vehicle.pos
        self.aggressive = aggressive
        self.weapon = WeaponInstance("smg" if aggressive else "pistol", 9999)

    def on_carjacked(self, vehicle):
        self.dead = True

    def take_damage(self, dmg, source=None):
        pass

    def update(self, dt, world, game):
        v = self.vehicle
        if v.dead or v.driver is not self:
            self.dead = True
            return
        p = game.player
        target = p.vehicle.pos if p.vehicle else p.pos
        delta = pygame.math.Vector2(target) - v.pos
        dist = delta.length()
        desired = math.degrees(math.atan2(delta.y, delta.x))
        diff = (desired - v.heading + 180) % 360 - 180
        steer = max(-1.0, min(1.0, diff / 35.0))
        throttle = 1.0 if dist > 90 else 0.2      # ram range: keep pushing
        if abs(diff) > 120:
            throttle = -0.5                        # turn around
        v.update(dt, world, dict(throttle=throttle, steer=steer), game.particles, game)
        # drive-by from the cruiser at 3★+
        self.weapon.update(dt)
        if game.police.stars >= 3 and dist < 380 and self.weapon.can_fire() and random.random() < 0.7 * dt:
            self.weapon.fire(world, game.particles, v.pos, delta.rotate(random.uniform(-9, 9)), self)

    def draw(self, surf, camera):
        pass


class HeliPilot:
    """Police helicopter: hovers near the player and rains fire at 4★+."""

    def __init__(self, vehicle):
        self.vehicle = vehicle
        vehicle.driver = self
        vehicle.altitude = 240.0
        self.dead = False
        self.pos = vehicle.pos
        self.weapon = WeaponInstance("assault_rifle", 9999)

    def on_carjacked(self, vehicle):
        self.dead = True

    def take_damage(self, dmg, source=None):
        pass

    def update(self, dt, world, game):
        v = self.vehicle
        if v.dead or v.driver is not self:
            self.dead = True
            return
        p = game.player
        target = pygame.math.Vector2(p.vehicle.pos if p.vehicle else p.pos)
        delta = target - v.pos
        dist = delta.length()
        desired = math.degrees(math.atan2(delta.y, delta.x))
        diff = (desired - v.heading + 180) % 360 - 180
        steer = max(-1.0, min(1.0, diff / 40.0))
        throttle = 1.0 if dist > 200 else 0.0
        vert = 1.0 if v.altitude < 200 else 0.0
        v.update(dt, world, dict(throttle=throttle, steer=steer, vertical=vert), game.particles, game)
        self.weapon.update(dt)
        if dist < 460 and self.weapon.can_fire() and random.random() < 1.4 * dt:
            self.weapon.fire(world, game.particles, v.pos, delta.rotate(random.uniform(-10, 10)), self)

    def draw(self, surf, camera):
        pass


class PoliceSystem:
    def __init__(self):
        self.stars = 0
        self.heat = 0.0                     # partial progress toward next star
        self.decay_t = 0.0
        self.units = []                     # AI controllers (drivers/pilots), cops live in world.npcs
        self._spawn_t = 0.0
        self._roadblock_t = 0.0

    # ------------------------------------------------------------ heat
    def add_heat(self, amount, game=None):
        self.heat += amount
        while self.heat >= 1.0 and self.stars < MAX_STARS:
            self.heat -= 1.0
            self.stars += 1
            if game:
                game.hud.notify(f"Wanted level: {STAR_CHAR * self.stars}", UI_WARN)
        self.heat = max(0.0, min(self.heat, 1.0))
        self.decay_t = 0.0

    def report_crime(self, severity, game=None, silent=False):
        """severity roughly: 0.1 punch, 0.35 gunshot, 0.6 kill, 1.0 cop kill / explosion."""
        if silent:
            severity *= 0.3
        if self.stars == 0 and severity < 0.3:
            self.heat += severity
            if self.heat >= 0.6:
                self.add_heat(0.5, game)
        else:
            self.add_heat(severity * 0.6, game)

    def clear(self):
        self.stars = 0
        self.heat = 0.0
        for u in self.units:
            u.dead = True
            u.vehicle.siren = False
            if u.vehicle.driver is u:
                u.vehicle.driver = None

    # ------------------------------------------------------------ update
    def update(self, dt, world, game):
        self.units = [u for u in self.units if not u.dead and not u.vehicle.dead]
        p = game.player
        if self.stars == 0:
            return

        # line-of-sight decay: no cop nearby -> lose stars over time
        seen = any((u.vehicle.pos - p.pos).length() < POLICE_SIGHT for u in self.units)
        seen = seen or any(n.KIND == "cop" and not n.dead and (n.pos - p.pos).length() < POLICE_SIGHT
                           for n in world.npcs if hasattr(n, "KIND"))
        if seen:
            self.decay_t = 0.0
        else:
            self.decay_t += dt
            if self.decay_t > WANTED_DECAY_TIME:
                self.decay_t = 0.0
                self.stars -= 1
                if self.stars <= 0:
                    self.clear()
                    game.hud.notify("You lost the heat", UI_ACCENT2)
                return

        # ------------------------------------------------------- spawning
        self._spawn_t -= dt
        if self._spawn_t <= 0:
            self._spawn_t = max(2.5, 9.0 - self.stars * 1.4)
            self._spawn_response(world, game)

        if self.stars >= 3:
            self._roadblock_t -= dt
            if self._roadblock_t <= 0:
                self._roadblock_t = 16.0
                self._spawn_roadblock(world, game)

    def _spawn_response(self, world, game):
        p = game.player
        n_cars = sum(1 for u in self.units if isinstance(u, PursuitDriver))
        n_heli = sum(1 for u in self.units if isinstance(u, HeliPilot))
        n_foot = sum(1 for n in world.npcs if getattr(n, "KIND", "") == "cop" and not n.dead)

        # cops on foot (1★+)
        if n_foot < self.stars * 2:
            pos = self._offscreen_pos(game, 380)
            if pos and not world.is_water(pos.x, pos.y) and not world.blocked_for_foot(pos.x, pos.y):
                world.npcs.append(CopOnFoot(pos))

        # cruisers (2★+)
        cap = {2: 1, 3: 2, 4: 3, 5: 3}.get(self.stars, 0)
        if n_cars < cap:
            pos = self._road_pos_near(world, p.pos, 500, 900)
            if pos:
                cruiser = make_vehicle("police", pos)
                world.vehicles.append(cruiser)
                self.units.append(PursuitDriver(cruiser, aggressive=self.stars >= 4))

        # military (5★)
        if self.stars >= 5 and n_cars < 5:
            pos = self._road_pos_near(world, p.pos, 500, 900)
            if pos:
                jeep = make_vehicle("military", pos)
                world.vehicles.append(jeep)
                self.units.append(PursuitDriver(jeep, aggressive=True))

        # helicopter (4★+)
        if self.stars >= 4 and n_heli < 1:
            edge = pygame.math.Vector2(p.pos) + pygame.math.Vector2(1, 0).rotate(
                random.uniform(0, 360)) * 900
            edge.x = max(40, min(MAP_W - 40, edge.x))
            edge.y = max(40, min(MAP_H - 40, edge.y))
            heli = make_vehicle("police_heli", edge)
            heli.altitude = 240
            world.vehicles.append(heli)
            self.units.append(HeliPilot(heli))
            game.hud.notify("Police helicopter inbound!", UI_WARN)

    def _spawn_roadblock(self, world, game):
        """Parked cruisers across the road ahead of the player's travel direction."""
        p = game.player
        vel = p.vehicle.vel if p.vehicle else pygame.math.Vector2()
        if vel.length() < 60:
            return
        ahead = p.pos + vel.normalize() * 620
        pos = self._road_pos_near(world, ahead, 0, 160)
        if not pos:
            return
        perp = vel.normalize().rotate(90)
        for off in (-30, 0, 30):
            block = make_vehicle("police", pos + perp * off,
                                 heading=math.degrees(math.atan2(perp.y, perp.x)))
            block.siren = True
            world.vehicles.append(block)
        for off in (-22, 22):
            world.npcs.append(CopOnFoot(pos + perp * off + vel.normalize() * 30))
        game.hud.notify("Roadblock ahead!", UI_WARN)

    def _road_pos_near(self, world, center, dmin, dmax):
        for _ in range(24):
            tx, ty = random.choice(world.road_tiles)
            pos = pygame.math.Vector2(tx * TILE + 16, ty * TILE + 16)
            d = (pos - pygame.math.Vector2(center)).length()
            if dmin <= d <= dmax:
                return pos
        return None

    def _offscreen_pos(self, game, dist):
        ang = random.uniform(0, 360)
        pos = pygame.math.Vector2(game.player.pos) + pygame.math.Vector2(1, 0).rotate(ang) * dist
        if 20 < pos.x < MAP_W - 20 and 20 < pos.y < MAP_H - 20:
            return pos
        return None
