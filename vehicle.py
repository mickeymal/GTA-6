"""Vehicles: base physics + Car / Motorcycle / Bicycle / Boat / Plane / Helicopter.

Ground vehicles use a forward/lateral velocity split so grip loss (handbrake)
produces real drifting. Aircraft add altitude, takeoff speed and stalling.
Boats only move on water and wallow with heavy lateral slip.
Damage reduces performance; at zero HP vehicles burn then explode.
"""
import math
import random
import pygame
from settings import *


# spec: max_speed px/s, accel, brake, turn deg/s, grip (lateral keep), radius, price
VEHICLE_SPECS = {
    # ------------- cars
    "sports":   dict(cls="car", name="Banshee GT", max_speed=520, accel=340, brake=520, turn=170,
                     grip=0.86, hp=100, size=(44, 22), radius=22, price=95000,
                     colors=[(255, 60, 120), (60, 220, 255), (250, 220, 60)]),
    "muscle":   dict(cls="car", name="Sabre Fury", max_speed=460, accel=300, brake=440, turn=140,
                     grip=0.90, hp=120, size=(46, 23), radius=23, price=42000,
                     colors=[(180, 40, 40), (40, 40, 46), (240, 130, 40)]),
    "sedan":    dict(cls="car", name="Presidente", max_speed=380, accel=210, brake=380, turn=120,
                     grip=0.92, hp=110, size=(46, 22), radius=23, price=18000,
                     colors=[(180, 180, 190), (90, 110, 160), (60, 80, 70)]),
    "suv":      dict(cls="car", name="Seabreeze XL", max_speed=350, accel=190, brake=360, turn=110,
                     grip=0.93, hp=140, size=(48, 26), radius=25, price=26000,
                     colors=[(40, 60, 90), (140, 140, 150), (80, 50, 40)]),
    "truck":    dict(cls="car", name="Hauler", max_speed=290, accel=130, brake=300, turn=85,
                     grip=0.95, hp=220, size=(60, 26), radius=30, price=30000,
                     colors=[(120, 120, 130), (150, 90, 50)]),
    "police":   dict(cls="car", name="Police Cruiser", max_speed=470, accel=310, brake=500, turn=155,
                     grip=0.88, hp=130, size=(46, 22), radius=23, price=0,
                     colors=[(240, 240, 245)]),
    "armored":  dict(cls="car", name="Armored Stockade", max_speed=300, accel=150, brake=320, turn=90,
                     grip=0.95, hp=380, size=(56, 26), radius=28, price=0,
                     colors=[(70, 74, 80)]),
    "military": dict(cls="car", name="Military Patriot", max_speed=400, accel=250, brake=420, turn=130,
                     grip=0.92, hp=260, size=(50, 26), radius=25, price=0,
                     colors=[(70, 84, 60)]),
    # ------------- two wheels
    "motorcycle": dict(cls="bike", name="PCJ 900", max_speed=540, accel=380, brake=460, turn=210,
                       grip=0.84, hp=60, size=(34, 12), radius=15, price=15000,
                       colors=[(230, 60, 160), (40, 200, 230), (20, 20, 24)]),
    "bicycle":  dict(cls="bike", name="BMX", max_speed=200, accel=140, brake=240, turn=220,
                     grip=0.9, hp=40, size=(26, 10), radius=12, price=800,
                     colors=[(90, 200, 90), (230, 200, 60)]),
    # ------------- boats
    "speedboat": dict(cls="boat", name="Squalo", max_speed=430, accel=200, brake=180, turn=120,
                      grip=0.965, hp=110, size=(52, 22), radius=26, price=55000,
                      colors=[(240, 240, 245), (60, 200, 255)]),
    "jetski":   dict(cls="boat", name="Speedophile", max_speed=380, accel=230, brake=160, turn=170,
                     grip=0.95, hp=50, size=(28, 13), radius=14, price=12000,
                     colors=[(255, 220, 60), (255, 90, 160)]),
    "yacht":    dict(cls="boat", name="Marquis Yacht", max_speed=200, accel=70, brake=90, turn=45,
                     grip=0.985, hp=320, size=(96, 34), radius=48, price=240000,
                     colors=[(245, 245, 250)]),
    "fishing":  dict(cls="boat", name="Reefer", max_speed=170, accel=70, brake=90, turn=60,
                     grip=0.98, hp=180, size=(64, 26), radius=32, price=28000,
                     colors=[(150, 120, 80), (90, 130, 140)]),
    # ------------- aircraft
    "stunt_plane": dict(cls="plane", name="Mallard Stunt", max_speed=620, accel=170, brake=140, turn=110,
                        grip=0.99, hp=90, size=(52, 46), radius=26, price=130000,
                        takeoff=300, stall=210, colors=[(230, 60, 60), (250, 210, 60)]),
    "jet":      dict(cls="plane", name="Shamal Jet", max_speed=820, accel=220, brake=160, turn=70,
                     grip=0.995, hp=140, size=(72, 58), radius=36, price=450000,
                     takeoff=380, stall=280, colors=[(240, 240, 250)]),
    "helicopter": dict(cls="heli", name="Sparrow", max_speed=420, accel=200, brake=200, turn=160,
                       grip=0.93, hp=110, size=(48, 20), radius=26, price=180000,
                       colors=[(70, 160, 200), (200, 70, 70)]),
    "police_heli": dict(cls="heli", name="Police Maverick", max_speed=440, accel=210, brake=200, turn=150,
                        grip=0.93, hp=150, size=(50, 22), radius=27, price=0,
                        colors=[(40, 60, 130)]),
}

CAR_KINDS = ["sports", "muscle", "sedan", "suv", "truck"]


def make_vehicle(kind, pos, heading=0.0):
    spec = VEHICLE_SPECS[kind]
    cls = {"car": Car, "bike": Motorcycle, "boat": Boat, "plane": Plane, "heli": Helicopter}[spec["cls"]]
    return cls(kind, pos, heading)


class Vehicle:
    def __init__(self, kind, pos, heading=0.0):
        self.kind = kind
        self.spec = VEHICLE_SPECS[kind]
        self.name = self.spec["name"]
        self.pos = pygame.math.Vector2(pos)
        self.heading = heading                  # degrees, 0 = +x
        self.vel = pygame.math.Vector2()
        self.hp = float(self.spec["hp"])
        self.max_hp = float(self.spec["hp"])
        self.radius = self.spec["radius"]
        self.color = random.choice(self.spec["colors"])
        self.driver = None                       # player or NPC
        self.dead = False
        self.burn_timer = None                   # counts down to explosion
        self.siren = False
        self.horn_t = 0.0
        self.stolen = False
        self.altitude = 0.0

    # ------------------------------------------------------------ helpers
    @property
    def forward(self):
        return pygame.math.Vector2(1, 0).rotate(self.heading)

    @property
    def speed(self):
        return self.vel.length()

    @property
    def airborne(self):
        return self.altitude > 2.0

    def perf(self):
        """Damage cuts top speed and acceleration."""
        frac = max(0.35, self.hp / self.max_hp)
        return frac

    def take_damage(self, dmg, source=None):
        if self.dead:
            return
        self.hp -= dmg
        self.last_attacker = source
        if self.hp <= 0 and self.burn_timer is None:
            self.burn_timer = random.uniform(1.5, 3.0)

    # ------------------------------------------------------------ physics
    def update(self, dt, world, controls, particles, game):
        if self.dead:
            return
        if self.burn_timer is not None:
            self.burn_timer -= dt
            particles.fire_patch(self.pos + pygame.math.Vector2(random.uniform(-10, 10), random.uniform(-8, 8)))
            if self.burn_timer <= 0:
                self.dead = True
                game.explode_at(self.pos, radius=100, damage=120, source=getattr(self, "last_attacker", None))
                return
        elif self.hp < self.max_hp * 0.35:
            if random.random() < 4 * dt:
                particles.smoke(self.pos, 1.2)
        self._physics(dt, world, controls, particles, game)

    def _physics(self, dt, world, controls, particles, game):
        raise NotImplementedError

    def _ground_physics(self, dt, world, controls, particles, game, on_water_ok=False):
        throttle = controls.get("throttle", 0.0)
        steer = controls.get("steer", 0.0)
        handbrake = controls.get("handbrake", False)
        perf = self.perf()

        fwd = self.forward
        f_speed = self.vel.dot(fwd)

        # steering scales with speed (no turning while parked)
        speed_frac = min(1.0, abs(f_speed) / 140.0)
        turn = self.spec["turn"] * steer * speed_frac * (1 if f_speed >= 0 else -1)
        if handbrake:
            turn *= 1.5
        self.heading += turn * dt
        fwd = self.forward

        # accelerate / brake
        if throttle > 0:
            f_speed += self.spec["accel"] * perf * throttle * dt
        elif throttle < 0:
            if f_speed > 10:
                f_speed += self.spec["brake"] * throttle * dt      # braking
            else:
                f_speed += self.spec["accel"] * 0.5 * throttle * dt  # reverse
        else:
            f_speed *= (1 - 0.6 * dt)

        # surface slowdown
        tile = world.tile_at(self.pos.x, self.pos.y)
        max_sp = self.spec["max_speed"] * perf
        if tile in SLOW_TILES:
            max_sp *= 0.55
        f_speed = max(-max_sp * 0.4, min(max_sp, f_speed))

        # rebuild velocity: keep some lateral (drift) component
        lat = self.vel - self.vel.dot(fwd) * fwd
        grip = 0.62 if handbrake else self.spec["grip"]
        lat *= grip ** (dt * 60)
        if handbrake and lat.length() > 60 and random.random() < 10 * dt:
            particles.smoke(self.pos - fwd * self.spec["size"][0] * 0.4, 0.7)
        self.vel = fwd * f_speed + lat

        self._move_and_collide(dt, world, particles, game, on_water_ok)

    def _move_and_collide(self, dt, world, particles, game, on_water_ok):
        new_pos = self.pos + self.vel * dt
        blocked = False
        if not on_water_ok and world.is_water(new_pos.x, new_pos.y):
            blocked = True
        if world.tile_at(new_pos.x, new_pos.y) == T_BUILDING:
            blocked = True
        if not (0 <= new_pos.x < MAP_W and 0 <= new_pos.y < MAP_H):
            blocked = True
        if blocked:
            impact = self.speed
            if impact > 160:
                self.take_damage(impact * 0.06)
                particles.sparks(self.pos)
                if game and game.player.vehicle is self:
                    game.camera.shake(impact * 0.02, 0.25)
            self.vel *= -0.25
            self.pos += self.vel * dt
        else:
            self.pos = new_pos
        # vehicle vs vehicle
        for other in world.vehicles:
            if other is self or other.dead or other.airborne != self.airborne:
                continue
            delta = self.pos - other.pos
            dist = delta.length()
            min_d = self.radius + other.radius
            if 0 < dist < min_d:
                push = delta.normalize() * (min_d - dist) * 0.5
                self.pos += push
                other.pos -= push
                rel = (self.vel - other.vel).length()
                if rel > 180:
                    dmg = rel * 0.05
                    self.take_damage(dmg, other.driver)
                    other.take_damage(dmg, self.driver)
                    particles.sparks((self.pos + other.pos) / 2)
                self.vel, other.vel = self.vel * 0.6 + other.vel * 0.3, other.vel * 0.6 + self.vel * 0.3

    # ------------------------------------------------------------ drawing
    def draw(self, surf, camera):
        if self.dead:
            return
        z = camera.zoom
        scale = 1.0 + self.altitude / 260.0        # aircraft appear larger when high? no: shadow trick
        sx, sy = camera.world_to_screen(self.pos)
        if self.airborne:
            # shadow on the ground
            sh_off = self.altitude * 0.35
            shx, shy = camera.world_to_screen((self.pos.x + sh_off, self.pos.y + sh_off))
            self._draw_body(surf, (shx, shy), z * 0.9, shadow=True)
            sy -= self.altitude * 0.25 * z
        self._draw_body(surf, (sx, sy), z)

    def _poly(self, center, pts, z):
        rot = []
        for px, py in pts:
            v = pygame.math.Vector2(px, py).rotate(self.heading) * z
            rot.append((center[0] + v.x, center[1] + v.y))
        return rot

    def _draw_body(self, surf, center, z, shadow=False):
        w, h = self.spec["size"]
        body = [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)]
        color = (14, 14, 18) if shadow else self._damaged_color()
        pygame.draw.polygon(surf, color, self._poly(center, body, z))
        if shadow:
            return
        self._draw_details(surf, center, z)

    def _damaged_color(self):
        frac = self.hp / self.max_hp
        if frac > 0.6:
            return self.color
        # scorched look as damage grows
        f = max(0.35, frac + 0.2)
        return tuple(int(c * f) for c in self.color)

    def _draw_details(self, surf, center, z):
        pass


class Car(Vehicle):
    def _physics(self, dt, world, controls, particles, game):
        self._ground_physics(dt, world, controls, particles, game)

    def _draw_details(self, surf, center, z):
        w, h = self.spec["size"]
        # windshield
        ws = [(w * 0.05, -h * 0.36), (w * 0.3, -h * 0.36), (w * 0.3, h * 0.36), (w * 0.05, h * 0.36)]
        pygame.draw.polygon(surf, (30, 40, 60), self._poly(center, ws, z))
        # headlights
        for sy_ in (-h * 0.32, h * 0.32):
            p = self._poly(center, [(w * 0.48, sy_)], z)[0]
            pygame.draw.circle(surf, (255, 250, 200), (int(p[0]), int(p[1])), max(1, int(2.4 * z)))
        if self.kind == "police":
            bar = [(-w * 0.1, -h * 0.3), (w * 0.02, -h * 0.3), (w * 0.02, h * 0.3), (-w * 0.1, h * 0.3)]
            flash = (255, 40, 40) if (pygame.time.get_ticks() // 250) % 2 == 0 else (40, 80, 255)
            pygame.draw.polygon(surf, flash if self.siren else (90, 90, 110), self._poly(center, bar, z))
            stripe = [(-w * 0.5, -h * 0.12), (w * 0.5, -h * 0.12), (w * 0.5, h * 0.12), (-w * 0.5, h * 0.12)]
            pygame.draw.polygon(surf, (30, 60, 140), self._poly(center, stripe, z), max(1, int(2 * z)))


class Motorcycle(Vehicle):
    def _physics(self, dt, world, controls, particles, game):
        self._ground_physics(dt, world, controls, particles, game)

    def _draw_details(self, surf, center, z):
        w, h = self.spec["size"]
        if self.driver is not None:
            p = self._poly(center, [(0, 0)], z)[0]
            pygame.draw.circle(surf, (220, 180, 150), (int(p[0]), int(p[1])), max(2, int(4 * z)))


class Boat(Vehicle):
    def _physics(self, dt, world, controls, particles, game):
        # boats coast on water; run aground = stop
        controls = dict(controls)
        self._ground_physics_water(dt, world, controls, particles, game)

    def _ground_physics_water(self, dt, world, controls, particles, game):
        throttle = controls.get("throttle", 0.0)
        steer = controls.get("steer", 0.0)
        perf = self.perf()
        fwd = self.forward
        f_speed = self.vel.dot(fwd)
        speed_frac = min(1.0, abs(f_speed) / 90.0)
        self.heading += self.spec["turn"] * steer * speed_frac * dt
        fwd = self.forward
        if throttle > 0:
            f_speed += self.spec["accel"] * perf * throttle * dt
        elif throttle < 0:
            f_speed += self.spec["brake"] * throttle * dt
        else:
            f_speed *= (1 - 0.35 * dt)
        max_sp = self.spec["max_speed"] * perf
        f_speed = max(-max_sp * 0.3, min(max_sp, f_speed))
        lat = self.vel - self.vel.dot(fwd) * fwd
        lat *= self.spec["grip"] ** (dt * 60)
        self.vel = fwd * f_speed + lat

        new_pos = self.pos + self.vel * dt
        tile = world.tile_at(new_pos.x, new_pos.y)
        if tile == T_WATER or tile == T_DOCK:
            self.pos = new_pos
            if self.speed > 80 and random.random() < 14 * dt:
                particles.splash(self.pos - fwd * self.spec["size"][0] * 0.5)
        else:
            if self.speed > 140:
                self.take_damage(self.speed * 0.04)
                particles.sparks(self.pos)
            self.vel *= -0.2

    def _draw_details(self, surf, center, z):
        w, h = self.spec["size"]
        bow = [(w * 0.5, 0), (w * 0.2, -h * 0.5), (w * 0.2, h * 0.5)]
        pygame.draw.polygon(surf, tuple(min(255, c + 30) for c in self._damaged_color()),
                            self._poly(center, bow, z))
        deck = [(-w * 0.35, -h * 0.3), (w * 0.1, -h * 0.3), (w * 0.1, h * 0.3), (-w * 0.35, h * 0.3)]
        pygame.draw.polygon(surf, (40, 50, 70), self._poly(center, deck, z))


class Plane(Vehicle):
    def _physics(self, dt, world, controls, particles, game):
        throttle = controls.get("throttle", 0.0)
        steer = controls.get("steer", 0.0)
        climb = controls.get("vertical", 0.0)
        perf = self.perf()
        fwd = self.forward
        f_speed = self.vel.dot(fwd)

        turn_frac = min(1.0, abs(f_speed) / 220.0)
        self.heading += self.spec["turn"] * steer * turn_frac * dt
        fwd = self.forward

        if throttle > 0:
            f_speed += self.spec["accel"] * perf * throttle * dt
        elif throttle < 0:
            f_speed -= self.spec["brake"] * dt * (1.0 if not self.airborne else 0.3)
        else:
            f_speed *= (1 - (0.1 if self.airborne else 0.5) * dt)
        f_speed = max(0 if self.airborne else -40, min(self.spec["max_speed"] * perf, f_speed))

        # altitude: need takeoff speed to climb; stall when too slow
        takeoff = self.spec["takeoff"]
        stall = self.spec["stall"]
        if climb > 0 and f_speed >= takeoff:
            self.altitude = min(320, self.altitude + 90 * dt)
        elif climb < 0:
            self.altitude = max(0, self.altitude - 110 * dt)
        if self.airborne and f_speed < stall:
            # stalling: nose drops, altitude bleeds fast
            self.altitude = max(0, self.altitude - 150 * dt)
            if game and game.player.vehicle is self and random.random() < 2 * dt:
                game.hud.notify("STALLING! Throttle up!", UI_WARN)

        lat = self.vel - self.vel.dot(fwd) * fwd
        lat *= self.spec["grip"] ** (dt * 60)
        self.vel = fwd * f_speed + lat

        new_pos = self.pos + self.vel * dt
        if self.airborne:
            self.pos.update(max(20, min(MAP_W - 20, new_pos.x)), max(20, min(MAP_H - 20, new_pos.y)))
        else:
            tile = world.tile_at(new_pos.x, new_pos.y)
            if tile == T_WATER:
                self.take_damage(999)
                particles.splash(self.pos)
            elif tile == T_BUILDING:
                self.take_damage(self.speed * 0.3)
                self.vel *= -0.2
            else:
                self.pos = new_pos
                if f_speed > 60 and tile not in (T_RUNWAY, T_ROAD):
                    self.take_damage(6 * dt * f_speed / 100)   # rough ground wrecks landing gear

    def _draw_details(self, surf, center, z):
        w, h = self.spec["size"]
        # wings
        wings = [(-w * 0.05, -h * 0.5), (w * 0.15, -h * 0.5), (w * 0.05, 0), (w * 0.15, h * 0.5), (-w * 0.05, h * 0.5)]
        pygame.draw.polygon(surf, tuple(max(0, c - 30) for c in self._damaged_color()),
                            self._poly(center, wings, z))
        # fuselage
        fus = [(w * 0.5, 0), (w * 0.3, -h * 0.1), (-w * 0.45, -h * 0.08), (-w * 0.45, h * 0.08), (w * 0.3, h * 0.1)]
        pygame.draw.polygon(surf, self._damaged_color(), self._poly(center, fus, z))
        # tail
        tail = [(-w * 0.45, -h * 0.22), (-w * 0.3, 0), (-w * 0.45, h * 0.22)]
        pygame.draw.polygon(surf, tuple(max(0, c - 40) for c in self._damaged_color()),
                            self._poly(center, tail, z))

    def _draw_body(self, surf, center, z, shadow=False):
        if shadow:
            w, h = self.spec["size"]
            body = [(w * 0.5, 0), (-w * 0.4, -h * 0.4), (-w * 0.4, h * 0.4)]
            pygame.draw.polygon(surf, (14, 14, 18), self._poly(center, body, z))
            return
        self._draw_details(surf, center, z)


class Helicopter(Vehicle):
    def __init__(self, kind, pos, heading=0.0):
        super().__init__(kind, pos, heading)
        self.rotor = 0.0        # spin-up 0..1
        self.rotor_angle = 0.0

    def _physics(self, dt, world, controls, particles, game):
        throttle = controls.get("throttle", 0.0)
        steer = controls.get("steer", 0.0)
        climb = controls.get("vertical", 0.0)
        occupied = self.driver is not None

        # rotor spin-up before flight is possible
        target = 1.0 if occupied else 0.0
        self.rotor += (target - self.rotor) * min(1.0, 0.7 * dt * 3)
        self.rotor_angle += self.rotor * 1400 * dt

        if self.rotor > 0.75:
            if climb > 0:
                self.altitude = min(300, self.altitude + 80 * dt)
            elif climb < 0:
                self.altitude = max(0, self.altitude - 90 * dt)
        else:
            self.altitude = max(0, self.altitude - 60 * dt)

        self.heading += self.spec["turn"] * steer * dt * (0.6 + 0.4 * min(1, self.speed / 100))
        fwd = self.forward
        if self.airborne:
            acc = fwd * self.spec["accel"] * self.perf() * throttle
            self.vel += acc * dt
            self.vel *= (1 - 0.8 * dt)
            if self.vel.length() > self.spec["max_speed"] * self.perf():
                self.vel.scale_to_length(self.spec["max_speed"] * self.perf())
            self.pos += self.vel * dt
            self.pos.update(max(20, min(MAP_W - 20, self.pos.x)), max(20, min(MAP_H - 20, self.pos.y)))
        else:
            self.vel *= (1 - 4 * dt)
            new_pos = self.pos + self.vel * dt
            if world.is_water(new_pos.x, new_pos.y):
                self.take_damage(999)
            elif world.tile_at(new_pos.x, new_pos.y) != T_BUILDING:
                self.pos = new_pos

    def _draw_details(self, surf, center, z):
        w, h = self.spec["size"]
        # tail boom
        boom = [(-w * 0.9, -2), (-w * 0.2, -3), (-w * 0.2, 3), (-w * 0.9, 2)]
        pygame.draw.polygon(surf, tuple(max(0, c - 30) for c in self._damaged_color()),
                            self._poly(center, boom, z))
        # cabin
        cab = [(w * 0.35, 0), (w * 0.15, -h * 0.5), (-w * 0.25, -h * 0.45), (-w * 0.25, h * 0.45), (w * 0.15, h * 0.5)]
        pygame.draw.polygon(surf, self._damaged_color(), self._poly(center, cab, z))
        pygame.draw.polygon(surf, (30, 40, 60),
                            self._poly(center, [(w * 0.32, 0), (w * 0.12, -h * 0.32), (w * 0.12, h * 0.32)], z))
        # main rotor
        ra = self.rotor_angle
        for a in (ra, ra + 90):
            v = pygame.math.Vector2(w * 0.75, 0).rotate(a) * z
            pygame.draw.line(surf, (25, 25, 28), (center[0] - v.x, center[1] - v.y),
                             (center[0] + v.x, center[1] + v.y), max(1, int(2 * z)))

    def _draw_body(self, surf, center, z, shadow=False):
        if shadow:
            pygame.draw.circle(surf, (14, 14, 18), (int(center[0]), int(center[1])),
                               max(2, int(self.radius * 0.8 * z)))
            return
        self._draw_details(surf, center, z)
