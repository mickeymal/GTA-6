"""Player: on-foot movement (run/sprint/swim), health/armor, money, weapon inventory,
vehicle enter/exit, drive-by shooting, cover-adjacent damage reduction."""
import math
import pygame
from settings import *
from weapons import WeaponInstance, WEAPON_DEFS, WHEEL_ORDER


class Player:
    def __init__(self, pos):
        self.pos = pygame.math.Vector2(pos)
        self.heading = 0.0
        self.hp = float(PLAYER_MAX_HP)
        self.armor = 0.0
        self.money = 500
        self.dead = False
        self.vehicle = None
        self.reputation = 0                     # story reputation from dialogue choices
        self.flags = set()                      # story flags
        self.outfit = 0
        self.outfits = [(60, 190, 220), (250, 120, 170), (240, 240, 240), (40, 40, 46)]
        self.owned_properties = []
        self.owned_vehicles = []                # kinds bought at dealer

        self.weapons = {"melee": WeaponInstance("fists")}
        self.current_cat = "melee"
        self.swimming = False
        self.in_cover = False
        self._hurt_flash = 0.0

    # ------------------------------------------------------------ weapons
    @property
    def weapon(self):
        return self.weapons.get(self.current_cat)

    def give_weapon(self, key, ammo=0):
        cat = WEAPON_DEFS[key]["cat"]
        existing = self.weapons.get(cat)
        if existing and existing.key == key:
            existing.reserve += ammo
        else:
            w = WeaponInstance(key, ammo)
            if existing:                        # keep mods when upgrading in-category
                w.mods |= {m for m in existing.mods}
            self.weapons[cat] = w
        self.current_cat = cat

    def add_ammo(self, key, amount):
        cat = WEAPON_DEFS[key]["cat"]
        if cat in self.weapons:
            self.weapons[cat].reserve += amount

    def select_category(self, cat):
        if cat in self.weapons:
            self.current_cat = cat

    def cycle_weapon(self, direction):
        owned = [c for c in WHEEL_ORDER if c in self.weapons]
        if not owned:
            return
        i = owned.index(self.current_cat) if self.current_cat in owned else 0
        self.current_cat = owned[(i + direction) % len(owned)]

    # ------------------------------------------------------------ damage
    def take_damage(self, dmg, source=None):
        if self.dead:
            return
        if self.in_cover:
            dmg *= 0.45
        if self.armor > 0:
            absorbed = min(self.armor, dmg * 0.7)
            self.armor -= absorbed
            dmg -= absorbed
        self.hp -= dmg
        self._hurt_flash = 0.25
        if self.hp <= 0:
            self.hp = 0
            self.dead = True

    def heal_full(self):
        self.hp = PLAYER_MAX_HP
        self.dead = False

    # ------------------------------------------------------------ vehicles
    def try_enter_vehicle(self, world, hud):
        if self.vehicle:
            return self.exit_vehicle(world, hud)
        best, best_d = None, 70.0
        for v in world.vehicles:
            if v.dead:
                continue
            d = (v.pos - self.pos).length()
            if d < best_d + v.radius:
                best, best_d = v, d
        if best:
            if best.driver and best.driver is not self:
                best.driver.on_carjacked(best)  # yank out NPC driver
            best.driver = self
            if not best.stolen and best.kind not in self.owned_vehicles:
                best.stolen = True
            self.vehicle = best
            hud.notify(f"{best.name}", UI_ACCENT2)
            return True
        return False

    def exit_vehicle(self, world, hud):
        v = self.vehicle
        if not v:
            return False
        if v.airborne:
            hud.notify("Can't bail out mid-air!", UI_WARN)
            return False
        side = pygame.math.Vector2(0, 1).rotate(v.heading) * (v.radius + 16)
        out = v.pos + side
        if world.is_water(out.x, out.y) and v.spec["cls"] != "boat":
            out = v.pos - side
        self.pos.update(out)
        v.driver = None
        self.vehicle = None
        return True

    # ------------------------------------------------------------ update
    def update(self, dt, world, keys, game):
        if self._hurt_flash > 0:
            self._hurt_flash -= dt
        for w in self.weapons.values():
            w.update(dt)

        if self.vehicle:
            self.pos.update(self.vehicle.pos)
            if self.vehicle.dead:
                self.vehicle = None
                self.take_damage(45)
            return

        move = pygame.math.Vector2()
        if keys[pygame.K_w]:
            move.y -= 1
        if keys[pygame.K_s]:
            move.y += 1
        if keys[pygame.K_a]:
            move.x -= 1
        if keys[pygame.K_d]:
            move.x += 1

        self.swimming = world.is_water(self.pos.x, self.pos.y)
        speed = PLAYER_SWIM if self.swimming else (
            PLAYER_SPRINT if keys[pygame.K_LSHIFT] else PLAYER_SPEED)

        if move.length_squared() > 0:
            move = move.normalize() * speed
            new_pos = self.pos + move * dt
            # axis-separated collision so we slide along walls
            if not world.blocked_for_foot(new_pos.x, self.pos.y):
                self.pos.x = max(8, min(MAP_W - 8, new_pos.x))
            if not world.blocked_for_foot(self.pos.x, new_pos.y):
                self.pos.y = max(8, min(MAP_H - 8, new_pos.y))

        # cover: hugging a building edge while stationary-ish reduces damage
        self.in_cover = False
        if move.length_squared() == 0 and not self.swimming:
            for dx, dy in ((TILE, 0), (-TILE, 0), (0, TILE), (0, -TILE)):
                if world.tile_at(self.pos.x + dx * 0.7, self.pos.y + dy * 0.7) == T_BUILDING:
                    self.in_cover = True
                    break

        # face the mouse
        mouse_world = game.camera.screen_to_world(pygame.mouse.get_pos())
        aim = mouse_world - self.pos
        if aim.length_squared() > 0:
            self.heading = math.degrees(math.atan2(aim.y, aim.x))

    def aim_dir(self, game):
        mouse_world = game.camera.screen_to_world(pygame.mouse.get_pos())
        d = mouse_world - self.pos
        if d.length_squared() == 0:
            d = pygame.math.Vector2(1, 0)
        return d.normalize()

    # ------------------------------------------------------------ draw
    def draw(self, surf, camera):
        if self.vehicle:
            return
        z = camera.zoom
        sx, sy = camera.world_to_screen(self.pos)
        color = self.outfits[self.outfit]
        if self._hurt_flash > 0:
            color = (255, 80, 80)
        if self.swimming:
            pygame.draw.circle(surf, (120, 170, 210), (int(sx), int(sy)), int(PLAYER_RADIUS * 1.3 * z), 2)
        # body
        pygame.draw.circle(surf, color, (int(sx), int(sy)), int(PLAYER_RADIUS * z))
        pygame.draw.circle(surf, (20, 20, 26), (int(sx), int(sy)), int(PLAYER_RADIUS * z), max(1, int(2 * z)))
        # head/facing indicator + weapon
        f = pygame.math.Vector2(1, 0).rotate(self.heading)
        hp = (sx + f.x * PLAYER_RADIUS * 0.6 * z, sy + f.y * PLAYER_RADIUS * 0.6 * z)
        pygame.draw.circle(surf, (232, 190, 160), (int(hp[0]), int(hp[1])), max(2, int(4 * z)))
        if self.weapon and not self.weapon.is_melee:
            gun_end = (sx + f.x * 20 * z, sy + f.y * 20 * z)
            pygame.draw.line(surf, (30, 30, 34), (sx, sy), gun_end, max(2, int(3 * z)))
        if self.in_cover:
            pygame.draw.circle(surf, (255, 255, 255), (int(sx), int(sy)), int(PLAYER_RADIUS * 1.6 * z), 1)
