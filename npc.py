"""NPCs: pedestrians, gang members, cops on foot, and road-following traffic drivers."""
import math
import random
import pygame
from settings import *
from weapons import WeaponInstance


class NPC:
    """Base walking NPC with a tiny state machine: wander / flee / attack / dead."""

    KIND = "ped"

    def __init__(self, pos):
        self.pos = pygame.math.Vector2(pos)
        self.heading = random.uniform(0, 360)
        self.hp = 40.0
        self.dead = False
        self.state = "wander"
        self.target = None
        self.speed = random.uniform(50, 80)
        self.color = random.choice([(200, 170, 140), (150, 110, 180), (110, 160, 190),
                                    (220, 200, 120), (170, 90, 90), (90, 140, 90)])
        self._decision_t = random.uniform(0.5, 2.0)
        self._move_dir = pygame.math.Vector2(1, 0).rotate(self.heading)
        self.weapon = None
        self.corpse_t = 12.0

    def take_damage(self, dmg, source=None):
        if self.dead:
            return
        self.hp -= dmg
        self.attacker = source
        if self.hp <= 0:
            self.dead = True
        elif self.KIND == "ped":
            self.state = "flee"
            self.target = source

    def on_carjacked(self, vehicle):
        self.state = "flee"

    def panic(self, source):
        if self.KIND == "ped" and self.state != "flee":
            self.state = "flee"
            self.target = source

    def update(self, dt, world, game):
        if self.dead:
            self.corpse_t -= dt
            return
        self._decision_t -= dt
        if self.state == "wander":
            if self._decision_t <= 0:
                self._decision_t = random.uniform(1.0, 3.0)
                self._move_dir = pygame.math.Vector2(1, 0).rotate(random.uniform(0, 360))
            self._walk(dt, world, self._move_dir, self.speed * 0.6)
        elif self.state == "flee":
            src_pos = self._target_pos(game)
            if src_pos is None or (self.pos - src_pos).length() > 620:
                self.state = "wander"
            else:
                away = self.pos - src_pos
                if away.length_squared() > 0:
                    self._walk(dt, world, away.normalize(), self.speed * 1.6)
        elif self.state == "attack":
            self._attack_logic(dt, world, game)

    def _target_pos(self, game):
        t = self.target
        if t is None:
            return None
        if getattr(t, "dead", False):
            return None
        return pygame.math.Vector2(t.pos)

    def _walk(self, dt, world, direction, speed):
        new_pos = self.pos + direction * speed * dt
        if not world.blocked_for_foot(new_pos.x, self.pos.y) and not world.is_water(new_pos.x, self.pos.y):
            self.pos.x = max(8, min(MAP_W - 8, new_pos.x))
        else:
            self._move_dir = pygame.math.Vector2(1, 0).rotate(random.uniform(0, 360))
        if not world.blocked_for_foot(self.pos.x, new_pos.y) and not world.is_water(self.pos.x, new_pos.y):
            self.pos.y = max(8, min(MAP_H - 8, new_pos.y))
        if direction.length_squared() > 0:
            self.heading = math.degrees(math.atan2(direction.y, direction.x))

    def _attack_logic(self, dt, world, game):
        pass

    def draw(self, surf, camera):
        z = camera.zoom
        sx, sy = camera.world_to_screen(self.pos)
        if self.dead:
            pygame.draw.circle(surf, (120, 30, 30), (int(sx), int(sy)), max(2, int(7 * z)))
            return
        pygame.draw.circle(surf, self.color, (int(sx), int(sy)), max(2, int(8 * z)))
        f = pygame.math.Vector2(1, 0).rotate(self.heading)
        pygame.draw.circle(surf, (230, 195, 160),
                           (int(sx + f.x * 4 * z), int(sy + f.y * 4 * z)), max(1, int(3 * z)))
        self._draw_extra(surf, camera, sx, sy, z)

    def _draw_extra(self, surf, camera, sx, sy, z):
        pass


class Pedestrian(NPC):
    KIND = "ped"


class GangMember(NPC):
    """Territorial: attacks the player when provoked or when player has low gang rep in turf."""

    KIND = "gang"

    def __init__(self, pos, gang="haitians"):
        super().__init__(pos)
        self.gang = gang
        self.hp = 70.0
        self.color = (200, 40, 60) if gang == "cartel" else (250, 150, 40)
        self.weapon = WeaponInstance("pistol", 9999)
        self.aggro_range = 300.0

    def take_damage(self, dmg, source=None):
        super().take_damage(dmg, source)
        if not self.dead:
            self.state = "attack"
            self.target = source

    def update(self, dt, world, game):
        if not self.dead and self.state != "attack":
            p = game.player
            if not p.dead and (p.pos - self.pos).length() < self.aggro_range and "gang_hostile" in p.flags:
                self.state = "attack"
                self.target = p
        super().update(dt, world, game)

    def _attack_logic(self, dt, world, game):
        tp = self._target_pos(game)
        if tp is None or (self.pos - tp).length() > 700:
            self.state = "wander"
            return
        delta = tp - self.pos
        dist = delta.length()
        self.heading = math.degrees(math.atan2(delta.y, delta.x))
        if dist > 180:
            self._walk(dt, world, delta.normalize(), self.speed * 1.3)
        if self.weapon:
            self.weapon.update(dt)
            if dist < 420 and self.weapon.can_fire() and random.random() < 1.2 * dt * 60 * 0.02:
                aim = delta.rotate(random.uniform(-7, 7))
                self.weapon.fire(world, game.particles, self.pos, aim, self)

    def _draw_extra(self, surf, camera, sx, sy, z):
        pygame.draw.circle(surf, (20, 20, 20), (int(sx), int(sy)), max(2, int(8 * z)), 1)


class CopOnFoot(NPC):
    KIND = "cop"

    def __init__(self, pos):
        super().__init__(pos)
        self.hp = 80.0
        self.color = (50, 80, 170)
        self.weapon = WeaponInstance("pistol", 9999)
        self.state = "attack"

    def update(self, dt, world, game):
        if self.dead:
            self.corpse_t -= dt
            return
        self.target = game.player
        self._attack_logic(dt, world, game)

    def _attack_logic(self, dt, world, game):
        p = game.player
        if p.dead:
            return
        delta = p.pos - self.pos
        dist = delta.length()
        self.heading = math.degrees(math.atan2(delta.y, delta.x))
        if dist > 140:
            self._walk(dt, world, delta.normalize() if dist else pygame.math.Vector2(1, 0), self.speed * 1.5)
        if self.weapon:
            self.weapon.update(dt)
            if dist < POLICE_SIGHT and self.weapon.can_fire() and random.random() < 0.9 * dt:
                aim = delta.rotate(random.uniform(-8, 8))
                self.weapon.fire(world, game.particles, self.pos, aim, self)

    def _draw_extra(self, surf, camera, sx, sy, z):
        pygame.draw.circle(surf, (220, 220, 240), (int(sx), int(sy)), max(2, int(4 * z)), 1)


class TrafficDriver:
    """Invisible AI 'driver' that steers a civilian vehicle along road tiles."""

    def __init__(self, vehicle):
        self.vehicle = vehicle
        vehicle.driver = self
        self.dead = False
        self.pos = vehicle.pos                  # so carjack yank etc. works
        self.dir = pygame.math.Vector2(1, 0).rotate(vehicle.heading)
        self._repath_t = 0.0
        self.panicked = False

    def on_carjacked(self, vehicle):
        self.dead = True                        # driver flees, despawn

    def take_damage(self, dmg, source=None):
        self.panicked = True

    def update(self, dt, world, game):
        v = self.vehicle
        if v.dead or v.driver is not self:
            self.dead = True
            return
        self._repath_t -= dt
        tx, ty = int(v.pos.x // TILE), int(v.pos.y // TILE)
        if self._repath_t <= 0:
            self._repath_t = 0.35
            if world.tile_at(v.pos.x, v.pos.y) != T_ROAD:
                # drifted off road: head to nearest road dir
                dirs = world.road_dirs(tx, ty)
                if dirs:
                    d = random.choice(dirs)
                    self.dir = pygame.math.Vector2(d)
            else:
                ahead = v.pos + self.dir * TILE * 2.2
                if world.tile_at(ahead.x, ahead.y) != T_ROAD:
                    dirs = world.road_dirs(tx, ty)
                    options = [pygame.math.Vector2(d) for d in dirs
                               if pygame.math.Vector2(d).dot(self.dir) > -0.5]
                    self.dir = random.choice(options) if options else -self.dir

        # steer toward desired direction
        desired = math.degrees(math.atan2(self.dir.y, self.dir.x))
        diff = (desired - v.heading + 180) % 360 - 180
        steer = max(-1.0, min(1.0, diff / 40.0))
        throttle = 0.75 if self.panicked else 0.45
        # brake if something's close ahead
        probe = v.pos + v.forward * (v.radius + 40)
        p = game.player
        if (not p.vehicle and (p.pos - probe).length() < 40) or any(
                o is not v and not o.dead and (o.pos - probe).length() < o.radius + 24
                for o in world.vehicles):
            throttle = -0.6
        v.update(dt, world, dict(throttle=throttle, steer=steer), game.particles, game)

    def draw(self, surf, camera):
        pass
