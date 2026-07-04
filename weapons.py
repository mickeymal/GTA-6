"""Weapons: arsenal definitions, projectiles with bullet drop, recoil/spread, reload,
ADS, thrown explosives, attachments, and weapon-wheel category data."""
import math
import random
import pygame

# ---------------------------------------------------------------- definitions
# category: melee / handgun / smg / rifle / shotgun / sniper / heavy / thrown
WEAPON_DEFS = {
    "fists":      dict(name="Fists", cat="melee", damage=8, range=28, rate=0.4, price=0),
    "bat":        dict(name="Baseball Bat", cat="melee", damage=20, range=36, rate=0.55, price=120),
    "knife":      dict(name="Knife", cat="melee", damage=26, range=30, rate=0.4, price=200),
    "machete":    dict(name="Machete", cat="melee", damage=38, range=38, rate=0.6, price=450),

    "pistol":     dict(name="Pistol", cat="handgun", damage=16, mag=12, reload=1.2, rate=0.28,
                       speed=900, spread=3.0, recoil=1.5, range=520, price=600, ammo_price=30),
    "combat_pistol": dict(name="Combat Pistol", cat="handgun", damage=22, mag=16, reload=1.1, rate=0.22,
                          speed=950, spread=2.5, recoil=1.8, range=560, price=1400, ammo_price=40),

    "micro_smg":  dict(name="Micro SMG", cat="smg", damage=11, mag=30, reload=1.6, rate=0.075,
                       speed=850, spread=6.5, recoil=2.2, range=420, price=2200, ammo_price=60),
    "smg":        dict(name="SMG", cat="smg", damage=14, mag=32, reload=1.7, rate=0.085,
                       speed=900, spread=5.0, recoil=2.0, range=480, price=3600, ammo_price=70),

    "assault_rifle": dict(name="Assault Rifle", cat="rifle", damage=22, mag=30, reload=2.1, rate=0.105,
                          speed=1100, spread=3.6, recoil=2.6, range=680, price=7500, ammo_price=110),
    "carbine":    dict(name="Carbine Rifle", cat="rifle", damage=26, mag=30, reload=2.0, rate=0.115,
                       speed=1180, spread=2.8, recoil=2.3, range=740, price=11000, ammo_price=130),

    "shotgun":    dict(name="Pump Shotgun", cat="shotgun", damage=12, mag=8, reload=2.4, rate=0.75,
                       speed=760, spread=11.0, recoil=6.0, range=260, pellets=7, price=3000, ammo_price=90),

    "sniper":     dict(name="Sniper Rifle", cat="sniper", damage=95, mag=6, reload=2.8, rate=1.3,
                       speed=1600, spread=0.4, recoil=8.0, range=1500, drop=True, price=16000, ammo_price=220),

    "rpg":        dict(name="RPG", cat="heavy", damage=200, mag=1, reload=3.2, rate=1.6,
                       speed=520, spread=1.5, recoil=10.0, range=1000, rocket=True, price=32000, ammo_price=900),

    "grenade":    dict(name="Grenade", cat="thrown", damage=140, rate=1.0, fuse=2.2,
                       speed=340, range=340, price=500, ammo_price=250),
    "molotov":    dict(name="Molotov", cat="thrown", damage=60, rate=1.0, fuse=0.0,
                       speed=300, range=300, fire=True, price=350, ammo_price=180),
}

WHEEL_ORDER = ["melee", "handgun", "smg", "shotgun", "rifle", "sniper", "heavy", "thrown"]
WHEEL_LABEL = {"melee": "Melee", "handgun": "Handguns", "smg": "SMGs", "shotgun": "Shotguns",
               "rifle": "Rifles", "sniper": "Snipers", "heavy": "Heavy", "thrown": "Thrown"}

ATTACHMENTS = {
    "suppressor": dict(name="Suppressor", price=2500, fits=("handgun", "smg", "rifle", "sniper")),
    "extended_mag": dict(name="Extended Mag", price=1800, fits=("handgun", "smg", "rifle", "shotgun")),
    "scope": dict(name="Scope", price=3200, fits=("rifle", "sniper")),
}


class WeaponInstance:
    """A weapon the player (or NPC) owns: tracks ammo, mods, reload state."""

    def __init__(self, key, ammo=0):
        self.key = key
        self.d = WEAPON_DEFS[key]
        self.mods = set()
        self.mag_ammo = self.mag_size if self.uses_ammo else 0
        self.reserve = ammo
        self.reload_t = 0.0
        self.cooldown = 0.0

    # ------------------------------------------------------------ properties
    @property
    def uses_ammo(self):
        return self.d["cat"] not in ("melee",) and self.d["cat"] != "thrown"

    @property
    def mag_size(self):
        base = self.d.get("mag", 0)
        return int(base * 1.5) if "extended_mag" in self.mods else base

    @property
    def spread(self):
        s = self.d.get("spread", 0)
        if "scope" in self.mods:
            s *= 0.6
        return s

    @property
    def damage(self):
        dmg = self.d["damage"]
        if "suppressor" in self.mods:
            dmg *= 0.92
        return dmg

    @property
    def suppressed(self):
        return "suppressor" in self.mods

    @property
    def is_thrown(self):
        return self.d["cat"] == "thrown"

    @property
    def is_melee(self):
        return self.d["cat"] == "melee"

    def total_ammo_text(self):
        if self.is_melee:
            return ""
        if self.is_thrown:
            return f"x{self.reserve}"
        return f"{self.mag_ammo}/{self.reserve}"

    # ------------------------------------------------------------ actions
    def update(self, dt):
        if self.cooldown > 0:
            self.cooldown -= dt
        if self.reload_t > 0:
            self.reload_t -= dt
            if self.reload_t <= 0:
                need = self.mag_size - self.mag_ammo
                take = min(need, self.reserve)
                self.mag_ammo += take
                self.reserve -= take

    def start_reload(self):
        if self.uses_ammo and self.reload_t <= 0 and self.reserve > 0 and self.mag_ammo < self.mag_size:
            self.reload_t = self.d["reload"]

    def can_fire(self):
        if self.cooldown > 0 or self.reload_t > 0:
            return False
        if self.is_melee:
            return True
        if self.is_thrown:
            return self.reserve > 0
        return self.mag_ammo > 0

    def fire(self, world, particles, pos, aim_dir, owner, ads=False):
        """Returns noise level (0 silent .. 1 loud) or None if couldn't fire."""
        if not self.can_fire():
            if self.uses_ammo and self.mag_ammo == 0:
                self.start_reload()
            return None
        self.cooldown = self.d["rate"]
        d = pygame.math.Vector2(aim_dir)
        if d.length_squared() == 0:
            d = pygame.math.Vector2(1, 0)
        d = d.normalize()

        if self.is_melee:
            return self._melee(world, particles, pos, d, owner)
        if self.is_thrown:
            self.reserve -= 1
            world.projectiles.append(ThrownProjectile(pos, d, self, owner))
            return 0.2

        self.mag_ammo -= 1
        spread = self.spread * (0.45 if ads else 1.0)
        pellets = self.d.get("pellets", 1)
        for _ in range(pellets):
            ang = random.uniform(-spread, spread)
            vel = d.rotate(ang) * self.d["speed"]
            world.projectiles.append(Bullet(pos + d * 16, vel, self, owner))
        if not self.suppressed:
            particles.muzzle_flash(pos + d * 18, d)
        if self.mag_ammo == 0:
            self.start_reload()
        return 0.15 if self.suppressed else 1.0

    def _melee(self, world, particles, pos, d, owner):
        hit_pos = pygame.math.Vector2(pos) + d * self.d["range"]
        for target in list(world.npcs) + list(world.vehicles):
            if target is owner or getattr(target, "dead", False):
                continue
            if (pygame.math.Vector2(target.pos) - hit_pos).length() < 26:
                target.take_damage(self.damage, owner)
                particles.sparks(target.pos)
                break
        else:
            player = getattr(world, "player_ref", None)
            if player and owner is not player and (player.pos - hit_pos).length() < 26:
                player.take_damage(self.damage, owner)
        return 0.05


# ---------------------------------------------------------------- projectiles
class Bullet:
    def __init__(self, pos, vel, weapon, owner):
        self.pos = pygame.math.Vector2(pos)
        self.vel = pygame.math.Vector2(vel)
        self.weapon = weapon
        self.owner = owner
        self.traveled = 0.0
        self.max_range = weapon.d["range"]
        self.damage = weapon.damage
        self.drop = weapon.d.get("drop", False)
        self.rocket = weapon.d.get("rocket", False)
        self.dead = False

    def update(self, dt, world, particles, game):
        if self.dead:
            return
        step = self.vel * dt
        self.pos += step
        self.traveled += step.length()
        if self.rocket:
            particles.smoke(self.pos, 0.6)
        if self.drop and self.traveled > self.max_range * 0.55:
            # bullet drop: sniper rounds sink and lose energy past mid-range
            self.vel *= 0.985
            self.pos.y += 26 * dt
            self.damage *= 0.995
        if self.traveled >= self.max_range:
            if self.rocket:
                self._explode(world, particles, game)
            self.dead = True
            return
        # world collision
        if world.blocked_for_car(self.pos.x, self.pos.y) and not world.is_water(self.pos.x, self.pos.y):
            if self.rocket:
                self._explode(world, particles, game)
            else:
                particles.sparks(self.pos)
            self.dead = True
            return
        # entity collision
        for target in world.npcs:
            if target is self.owner or target.dead:
                continue
            if (target.pos - self.pos).length_squared() < 14 * 14:
                target.take_damage(self.damage, self.owner)
                particles.blood(target.pos)
                if self.rocket:
                    self._explode(world, particles, game)
                self.dead = True
                return
        for veh in world.vehicles:
            if veh is self.owner or getattr(self.owner, "vehicle", None) is veh:
                continue
            if (veh.pos - self.pos).length_squared() < veh.radius * veh.radius:
                veh.take_damage(self.damage * (2.5 if self.rocket else 0.8), self.owner)
                particles.sparks(self.pos)
                if self.rocket:
                    self._explode(world, particles, game)
                self.dead = True
                return
        player = getattr(world, "player_ref", None)
        if player and self.owner is not player and player.vehicle is None:
            if (player.pos - self.pos).length_squared() < 13 * 13:
                player.take_damage(self.damage, self.owner)
                particles.blood(player.pos)
                self.dead = True

    def _explode(self, world, particles, game):
        game.explode_at(self.pos, radius=90, damage=self.damage, source=self.owner)

    def draw(self, surf, camera):
        s = camera.world_to_screen(self.pos)
        tail = camera.world_to_screen(self.pos - self.vel * 0.015)
        color = (255, 200, 80) if not self.rocket else (255, 120, 40)
        pygame.draw.line(surf, color, tail, s, 3 if self.rocket else 2)


class ThrownProjectile:
    """Grenades and molotovs: arc, fuse, then explode / ignite."""

    def __init__(self, pos, direction, weapon, owner):
        self.pos = pygame.math.Vector2(pos)
        self.vel = pygame.math.Vector2(direction) * weapon.d["speed"]
        self.weapon = weapon
        self.owner = owner
        self.fuse = weapon.d.get("fuse", 2.0)
        self.flight = weapon.d["range"] / weapon.d["speed"]
        self.is_fire = weapon.d.get("fire", False)
        self.damage = weapon.d["damage"]
        self.dead = False

    def update(self, dt, world, particles, game):
        if self.dead:
            return
        if self.flight > 0:
            self.flight -= dt
            self.pos += self.vel * dt
            self.vel *= 0.96
            if world.blocked_for_car(self.pos.x, self.pos.y) and not world.is_water(self.pos.x, self.pos.y):
                self.vel *= -0.3
                self.flight = 0
        else:
            self.vel *= 0.8
            self.pos += self.vel * dt
            if self.is_fire:
                self._detonate(world, particles, game)
                return
            self.fuse -= dt
            if self.fuse <= 0:
                self._detonate(world, particles, game)

    def _detonate(self, world, particles, game):
        self.dead = True
        if world.is_water(self.pos.x, self.pos.y):
            particles.splash(self.pos)
            return
        if self.is_fire:
            game.ignite_at(self.pos, radius=70, dps=self.damage, duration=5.0, source=self.owner)
        else:
            game.explode_at(self.pos, radius=110, damage=self.damage, source=self.owner)

    def draw(self, surf, camera):
        s = camera.world_to_screen(self.pos)
        color = (200, 60, 40) if self.is_fire else (70, 90, 70)
        pygame.draw.circle(surf, color, (int(s[0]), int(s[1])), max(2, int(4 * camera.zoom)))


class FirePatch:
    """Burning ground area left by molotovs / wrecks."""

    def __init__(self, pos, radius, dps, duration, source):
        self.pos = pygame.math.Vector2(pos)
        self.radius = radius
        self.dps = dps
        self.duration = duration
        self.source = source
        self.dead = False

    def update(self, dt, world, particles, game):
        self.duration -= dt
        if self.duration <= 0:
            self.dead = True
            return
        for _ in range(2):
            off = pygame.math.Vector2(random.uniform(-1, 1), random.uniform(-1, 1)) * self.radius * 0.7
            particles.fire_patch(self.pos + off)
        for target in world.npcs:
            if not target.dead and (target.pos - self.pos).length() < self.radius:
                target.take_damage(self.dps * dt, self.source)
        player = getattr(world, "player_ref", None)
        if player and (player.pos - self.pos).length() < self.radius:
            player.take_damage(self.dps * dt, self.source)
        for veh in world.vehicles:
            if (veh.pos - self.pos).length() < self.radius + veh.radius:
                veh.take_damage(self.dps * 0.5 * dt, self.source)

    def draw(self, surf, camera):
        pass  # rendered purely via particles
