"""Vice Bay Stories - a GTA-style open-world crime game.

Run:  python main.py
"""
import math
import random
import sys
import pygame

from settings import *
from camera import Camera
from world import World
from player import Player
from particles import ParticleSystem
from hud import HUD
from police import PoliceSystem
from dialogue import DialogueManager
from missions import MissionManager
from economy import ShopMenu, SHOPS, property_income
from activities import ActivityManager
from npc import Pedestrian, GangMember, TrafficDriver
from vehicle import make_vehicle, CAR_KINDS
from weapons import FirePatch
import savegame

STATE_MENU = "menu"
STATE_PLAY = "play"
STATE_PAUSE = "pause"
STATE_WHEEL = "wheel"
STATE_MAP = "map"

DRIVEBY_CATS = {"handgun", "smg"}


def t(x, y):
    return pygame.math.Vector2(x * TILE + TILE // 2, y * TILE + TILE // 2)


class Game:
    def __init__(self, headless=False):
        pygame.init()
        flags = pygame.HIDDEN if headless else 0
        self.screen = pygame.display.set_mode((SCREEN_W, SCREEN_H), flags)
        pygame.display.set_caption(TITLE)
        self.clock = pygame.time.Clock()
        self.state = STATE_MENU
        self.menu_cursor = 0
        self.pause_cursor = 0
        self.show_controls = False
        self.ads = False
        self.running = True
        self.wasted_t = 0.0
        self._day_marker = 0
        self._new_session()

    # ------------------------------------------------------------ session setup
    def _new_session(self):
        self.world = World()
        self.camera = Camera()
        self.particles = ParticleSystem()
        self.player = Player(t(38, 38))
        self.world.player_ref = self.player
        self.hud = HUD(self)
        self.police = PoliceSystem()
        self.dialogue = DialogueManager(self)
        self.missions = MissionManager(self)
        self.shop = ShopMenu(self)
        self.activities = ActivityManager(self)
        self.fires = []
        self._traffic_t = 0.0
        self._ped_t = 0.0
        self._day_marker = int(self.world.time_of_day)
        self._spawn_parked_vehicles()
        self._spawn_initial_peds()
        self.camera.pos.update(self.player.pos)
        self.camera.set_target(self.player.pos)

    def new_game(self):
        self._new_session()
        self.player.give_weapon("pistol", 36)
        self.hud.notify("Welcome to Vice Bay. Find the pink marker.", UI_ACCENT, 6)
        self.hud.notify("F1 shows controls at any time", UI_TEXT, 6)
        self.state = STATE_PLAY

    def continue_game(self):
        self._new_session()
        if savegame.load_game(self):
            self.hud.notify("Game loaded", UI_ACCENT2)
            self.state = STATE_PLAY
        else:
            self.new_game()

    def _spawn_parked_vehicles(self):
        rng = random.Random(WORLD_SEED + 1)
        w = self.world
        # street-parked cars
        for _ in range(46):
            pos = w.random_road_pos(rng)
            kind = rng.choice(CAR_KINDS + ["motorcycle", "bicycle"])
            w.vehicles.append(make_vehicle(kind, pos, rng.choice([0, 90, 180, 270])))
        # police cruisers at the downtown precinct
        for off in ((0, 0), (60, 0)):
            w.vehicles.append(make_vehicle("police", t(80, 49) + off))
        # marina boats
        w.vehicles.append(make_vehicle("speedboat", t(176, 62), 90))
        w.vehicles.append(make_vehicle("jetski", t(178, 64), 90))
        w.vehicles.append(make_vehicle("fishing", t(175, 66), 90))
        w.vehicles.append(make_vehicle("yacht", t(181, 72), 90))
        w.vehicles.append(make_vehicle("jetski", t(112, 182), 180))
        # airport aircraft
        w.vehicles.append(make_vehicle("stunt_plane", t(34, 152)))
        w.vehicles.append(make_vehicle("jet", t(58, 152)))
        w.vehicles.append(make_vehicle("helicopter", t(62, 146)))
        # downtown helipad
        w.vehicles.append(make_vehicle("helicopter", t(104, 65)))

    def _spawn_initial_peds(self):
        rng = random.Random(WORLD_SEED + 2)
        for _ in range(14):
            self.world.npcs.append(Pedestrian(self.world.random_sidewalk_pos(rng)))
        # ambient gang presence in Little Haiti
        for _ in range(4):
            for _try in range(20):
                pos = self.world.random_sidewalk_pos(rng)
                if self.world.district_at(pos.x, pos.y) == "haiti":
                    self.world.npcs.append(GangMember(pos, "haitians"))
                    break

    # ================================================================ events
    def handle_events(self):
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                self.running = False
            elif event.type == pygame.KEYDOWN:
                self._keydown(event.key)
            elif event.type == pygame.KEYUP:
                if event.key == pygame.K_TAB and self.state == STATE_WHEEL:
                    hovered = self.hud.draw_weapon_wheel(self.screen)  # recompute hover
                    if hovered and hovered in self.player.weapons:
                        self.player.select_category(hovered)
                    self.state = STATE_PLAY
            elif event.type == pygame.MOUSEWHEEL and self.state == STATE_PLAY:
                self.player.cycle_weapon(-event.y if event.y else 1)
            elif event.type == pygame.MOUSEBUTTONDOWN and self.state == STATE_PLAY:
                if event.button == 3:
                    self.ads = True
            elif event.type == pygame.MOUSEBUTTONUP:
                if event.button == 3:
                    self.ads = False

    def _keydown(self, key):
        if self.state == STATE_MENU:
            self._menu_key(key)
            return
        if self.state == STATE_PAUSE:
            self._pause_key(key)
            return
        if self.state == STATE_MAP:
            if key in (pygame.K_m, pygame.K_ESCAPE):
                self.state = STATE_PLAY
            return
        if self.dialogue.running:
            self.dialogue.handle_key(key)
            return
        if self.shop.running:
            self.shop.handle_key(key)
            return
        # -------- gameplay keys
        if key == pygame.K_ESCAPE:
            self.state = STATE_PAUSE
            self.pause_cursor = 0
        elif key == pygame.K_TAB:
            self.state = STATE_WHEEL
        elif key == pygame.K_m:
            self.state = STATE_MAP
        elif key in (pygame.K_e, pygame.K_f):
            if not self.shop.try_open_nearby() or key == pygame.K_f:
                self.player.try_enter_vehicle(self.world, self.hud)
        elif key == pygame.K_r:
            if self.player.weapon:
                self.player.weapon.start_reload()
        elif key == pygame.K_RETURN:
            if not self.missions.try_start_at_marker():
                if not self.missions.active:
                    self.activities.try_start_nearby()
        elif key == pygame.K_v:
            self.activities.toggle_vigilante()
        elif key == pygame.K_b:
            self.shop.try_open_nearby()
        elif key == pygame.K_F1:
            self.show_controls = not self.show_controls
        elif key == pygame.K_F5:
            savegame.save_game(self)
            self.hud.notify("Game saved", UI_ACCENT2)
        elif key == pygame.K_F9:
            if savegame.load_game(self):
                self.hud.notify("Game loaded", UI_ACCENT2)
            else:
                self.hud.notify("No save found", UI_WARN)

    def _menu_key(self, key):
        options = self._menu_options()
        if key in (pygame.K_UP, pygame.K_w):
            self.menu_cursor = (self.menu_cursor - 1) % len(options)
        elif key in (pygame.K_DOWN, pygame.K_s):
            self.menu_cursor = (self.menu_cursor + 1) % len(options)
        elif key in (pygame.K_RETURN, pygame.K_SPACE):
            options[self.menu_cursor][1]()

    def _menu_options(self):
        opts = [("New Game", self.new_game)]
        if savegame.has_save():
            opts.append(("Continue", self.continue_game))
        opts.append(("Quit", lambda: setattr(self, "running", False)))
        return opts

    def _pause_key(self, key):
        options = [
            ("Resume", lambda: setattr(self, "state", STATE_PLAY)),
            ("Save Game", lambda: (savegame.save_game(self), self.hud.notify("Game saved", UI_ACCENT2))),
            ("Load Game", lambda: savegame.load_game(self) and self.hud.notify("Game loaded", UI_ACCENT2)),
            ("Controls", lambda: setattr(self, "show_controls", not self.show_controls)),
            ("Quit to Menu", lambda: setattr(self, "state", STATE_MENU)),
        ]
        if key == pygame.K_ESCAPE:
            self.state = STATE_PLAY
        elif key in (pygame.K_UP, pygame.K_w):
            self.pause_cursor = (self.pause_cursor - 1) % len(options)
        elif key in (pygame.K_DOWN, pygame.K_s):
            self.pause_cursor = (self.pause_cursor + 1) % len(options)
        elif key == pygame.K_RETURN:
            options[self.pause_cursor][1]()

    # ================================================================ update
    def update(self, dt):
        if self.state != STATE_PLAY:
            return
        world_frozen = self.dialogue.running or self.shop.running
        keys = pygame.key.get_pressed()
        mouse = pygame.mouse.get_pressed()

        if not world_frozen:
            self.world.update(dt)
            self._property_income_tick()
            self._update_player_and_vehicle(dt, keys, mouse)
            self._update_entities(dt)
            self.police.update(dt, self.world, self)
            for u in list(self.police.units):
                u.update(dt, self.world, self)
            self.missions.update(dt)
            self.activities.update(dt)
            self._ambient_spawns(dt)
            self._handle_death(dt)
        self.particles.update(dt)
        self.hud.update(dt)
        self._update_camera(dt)

    def _update_player_and_vehicle(self, dt, keys, mouse):
        p = self.player
        p.update(dt, self.world, keys, self)
        v = p.vehicle
        if v and not p.dead:
            controls = {}
            controls["throttle"] = (1.0 if keys[pygame.K_w] else 0.0) - (1.0 if keys[pygame.K_s] else 0.0)
            controls["steer"] = (1.0 if keys[pygame.K_d] else 0.0) - (1.0 if keys[pygame.K_a] else 0.0)
            controls["handbrake"] = keys[pygame.K_SPACE] and v.spec["cls"] in ("car", "bike")
            if v.spec["cls"] in ("plane", "heli"):
                controls["vertical"] = ((1.0 if keys[pygame.K_SPACE] else 0.0)
                                        - (1.0 if keys[pygame.K_LCTRL] or keys[pygame.K_c] else 0.0))
            v.update(dt, self.world, controls, self.particles, self)
            if v.kind in ("police", "military", "police_heli"):
                v.siren = keys[pygame.K_LSHIFT]
        # shooting
        if mouse[0] and not p.dead:
            self._try_fire()

    def _try_fire(self):
        p = self.player
        w = p.weapon
        if not w:
            return
        if p.vehicle:
            cls = p.vehicle.spec["cls"]
            allowed = (cls == "boat") or (w.d["cat"] in DRIVEBY_CATS and cls in ("car", "bike"))
            if not allowed or p.vehicle.airborne:
                return
            origin = pygame.math.Vector2(p.vehicle.pos)
        else:
            origin = pygame.math.Vector2(p.pos)
        aim = self.camera.screen_to_world(pygame.mouse.get_pos()) - origin
        noise = w.fire(self.world, self.particles, origin, aim, p, ads=self.ads)
        if noise is not None:
            self.activities.on_shot_fired(self.camera.screen_to_world(pygame.mouse.get_pos()))
            if noise > 0.1:
                for n in self.world.npcs:
                    if hasattr(n, "panic") and (n.pos - origin).length() < 380:
                        n.panic(p)
                self.police.report_crime(0.08, self, silent=w.suppressed)
            if w.d["cat"] in ("sniper", "heavy"):
                self.camera.shake(4, 0.15)

    def _update_entities(self, dt):
        w = self.world
        p = self.player
        # vehicles without an AI/player driver still tick (burning, coasting)
        for v in w.vehicles:
            if v.driver is None and not v.dead:
                v.update(dt, w, {}, self.particles, self)
        for v in list(w.vehicles):
            if v.dead:
                if p.vehicle is v:
                    p.vehicle = None
                w.vehicles.remove(v)
        # npcs + AI drivers
        for n in list(w.npcs):
            if (n.pos - p.pos).length() < 2200 or getattr(n, "KIND", "") == "cop":
                n.update(dt, w, self)
            if getattr(n, "dead", False):
                self._on_npc_dead(n)
                if getattr(n, "corpse_t", 0) <= 0 or not hasattr(n, "corpse_t"):
                    w.npcs.remove(n)
        # projectiles & fires
        for pr in list(w.projectiles):
            pr.update(dt, w, self.particles, self)
            if pr.dead:
                w.projectiles.remove(pr)
        for f in list(self.fires):
            f.update(dt, w, self.particles, self)
            if f.dead:
                self.fires.remove(f)

    def _on_npc_dead(self, n):
        if getattr(n, "_counted", False) or not hasattr(n, "KIND"):
            return
        n._counted = True
        killer = getattr(n, "attacker", None)
        if killer is self.player:
            if n.KIND == "cop":
                self.police.report_crime(1.0, self)
                self.hud.notify("Officer down!", UI_WARN)
            elif n.KIND == "ped":
                self.police.report_crime(0.6, self)
            else:
                self.police.report_crime(0.35, self)
            self.player.money += random.randint(10, 60)   # loose cash drops

    def _ambient_spawns(self, dt):
        w, p = self.world, self.player
        # traffic
        self._traffic_t -= dt
        traffic = [n for n in w.npcs if isinstance(n, TrafficDriver)]
        for d in traffic:
            if (d.vehicle.pos - p.pos).length() > 1900:
                d.dead = True
                d.vehicle.dead = True
        if self._traffic_t <= 0 and len(traffic) < 10:
            self._traffic_t = 1.2
            pos = self.police._road_pos_near(w, p.pos, 700, 1400)
            if pos:
                kind = random.choice(CAR_KINDS + CAR_KINDS + ["police", "motorcycle"])
                v = make_vehicle(kind, pos)
                dirs = w.road_dirs(int(pos.x // TILE), int(pos.y // TILE))
                if dirs:
                    d = pygame.math.Vector2(dirs[0])
                    v.heading = math.degrees(math.atan2(d.y, d.x))
                w.vehicles.append(v)
                w.npcs.append(TrafficDriver(v))
        # pedestrians
        self._ped_t -= dt
        peds = [n for n in w.npcs if isinstance(n, Pedestrian)]
        for pd in peds:
            if (pd.pos - p.pos).length() > 1500:
                pd.dead = True
                pd.corpse_t = 0
        if self._ped_t <= 0 and len(peds) < 22:
            self._ped_t = 0.7
            for _try in range(10):
                pos = w.random_sidewalk_pos()
                d = (pos - p.pos).length()
                if 450 < d < 1100:
                    w.npcs.append(Pedestrian(pos))
                    break

    def _property_income_tick(self):
        day = int(self.world.time_of_day)
        if day == 8 and self._day_marker != 8:
            income = property_income(self.player)
            if income:
                self.player.money += income
                self.hud.notify(f"Property income +${income:,}", UI_MONEY)
        self._day_marker = day

    def _handle_death(self, dt):
        p = self.player
        if not p.dead:
            return
        if self.wasted_t <= 0:
            self.wasted_t = 3.5
            if self.missions.active:
                self.missions.fail("You were wasted")
        self.wasted_t -= dt
        if self.wasted_t <= 0:
            p.money = max(0, int(p.money * 0.9))
            p.heal_full()
            p.armor = 0
            p.vehicle = None
            p.pos.update(t(36, 48))
            self.police.clear()
            self.hud.notify("Wasted. You wake up at the safehouse, $-10%", UI_WARN, 5)

    def _update_camera(self, dt):
        p = self.player
        v = p.vehicle
        if v:
            look = v.pos + v.vel * 0.35
            zoom = 0.55 if (v.spec["cls"] in ("plane", "heli") and v.airborne) else 0.8
        else:
            look = p.pos
            zoom = 1.0
        if self.ads and not v:
            w = p.weapon
            if w and not w.is_melee:
                zoom *= 1.9 if (w.d["cat"] == "sniper" and "scope" in w.mods) else 1.35
                look = p.pos + p.aim_dir(self) * 60
        self.camera.set_target(look, zoom)
        self.camera.update(dt)

    # ================================================================ explosions / fire
    def explode_at(self, pos, radius, damage, source=None):
        pos = pygame.math.Vector2(pos)
        self.particles.explosion(pos, radius / 80.0)
        self.camera.shake(10, 0.5)
        for n in self.world.npcs:
            if hasattr(n, "hp") and not n.dead:
                d = (n.pos - pos).length()
                if d < radius:
                    n.take_damage(damage * (1 - d / radius), source)
        for v in self.world.vehicles:
            d = (v.pos - pos).length()
            if d < radius + v.radius:
                v.take_damage(damage * 0.8 * (1 - min(1, d / (radius + v.radius))), source)
        p = self.player
        d = (p.pos - pos).length()
        if d < radius and not p.vehicle:
            p.take_damage(damage * (1 - d / radius), source)
        if source is self.player:
            self.police.report_crime(1.0, self)
        # scorch fire
        if random.random() < 0.5:
            self.fires.append(FirePatch(pos, radius * 0.4, 15, 3.0, source))

    def ignite_at(self, pos, radius, dps, duration, source=None):
        self.fires.append(FirePatch(pos, radius, dps, duration, source))
        if source is self.player:
            self.police.report_crime(0.5, self)

    # ================================================================ draw
    def draw(self, dt):
        s = self.screen
        if self.state == STATE_MENU:
            self._draw_menu(s)
            pygame.display.flip()
            return
        # world + entities
        self.world.draw(s, self.camera)
        self.activities.draw_world(s, self.camera)
        vis = self.camera.visible_rect()
        ground, air = [], []
        for v in self.world.vehicles:
            if vis.colliderect(pygame.Rect(v.pos.x - 80, v.pos.y - 80, 160, 160)):
                (air if v.airborne else ground).append(v)
        for v in ground:
            v.draw(s, self.camera)
        for n in self.world.npcs:
            if vis.collidepoint(n.pos.x, n.pos.y):
                n.draw(s, self.camera)
        self.player.draw(s, self.camera)
        for pr in self.world.projectiles:
            pr.draw(s, self.camera)
        self.missions.draw_markers(s, self.camera)
        self.particles.draw(s, self.camera)
        for v in air:
            v.draw(s, self.camera)
        # lighting & weather overlays
        overlay = self.world.draw_lighting(s, self.camera)
        if overlay:
            s.blit(overlay, (0, 0))
        self.world.draw_weather(s, self.camera, dt)
        # UI
        self.hud.draw(s)
        self.activities.draw_hud(s)
        self.dialogue.draw(s)
        self.shop.draw(s)
        if self.player.dead:
            self._draw_wasted(s)
        if self.state == STATE_WHEEL:
            self.hud.draw_weapon_wheel(s)
        elif self.state == STATE_MAP:
            self.hud.draw_big_map(s)
        elif self.state == STATE_PAUSE:
            self._draw_pause(s)
        if self.show_controls:
            self._draw_controls(s)
        pygame.display.flip()

    def _draw_menu(self, s):
        s.fill((10, 8, 24))
        tt = pygame.time.get_ticks() * 0.001
        # neon skyline
        rng = random.Random(4)
        for i in range(40):
            x = rng.randint(0, SCREEN_W)
            h = rng.randint(60, 260)
            w = rng.randint(24, 70)
            c = rng.choice([(30, 24, 60), (40, 30, 70), (24, 30, 66)])
            pygame.draw.rect(s, c, (x, SCREEN_H - h, w, h))
            if rng.random() < 0.6:
                nc = rng.choice(NEON_COLORS)
                glow = 0.6 + 0.4 * math.sin(tt * 2 + i)
                pygame.draw.rect(s, tuple(int(ch * glow) for ch in nc),
                                 (x + 4, SCREEN_H - h + 8, w - 8, 4))
        title = self.hud.title.render("VICE BAY", True, UI_ACCENT)
        title2 = self.hud.title.render("STORIES", True, UI_ACCENT2)
        s.blit(title, (SCREEN_W // 2 - title.get_width() // 2, 130))
        s.blit(title2, (SCREEN_W // 2 - title2.get_width() // 2, 190))
        sub = self.hud.font.render("an open-world crime story", True, (180, 180, 200))
        s.blit(sub, (SCREEN_W // 2 - sub.get_width() // 2, 258))
        for i, (label, _) in enumerate(self._menu_options()):
            sel = i == self.menu_cursor
            color = UI_ACCENT if sel else UI_TEXT
            txt = self.hud.big.render(("> " if sel else "  ") + label, True, color)
            s.blit(txt, (SCREEN_W // 2 - 80, 340 + i * 44))
        hint = self.hud.small.render("W/S + ENTER", True, (140, 140, 160))
        s.blit(hint, (SCREEN_W // 2 - hint.get_width() // 2, SCREEN_H - 40))

    def _draw_pause(self, s):
        dim = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
        dim.fill((0, 0, 12, 180))
        s.blit(dim, (0, 0))
        title = self.hud.title.render("PAUSED", True, UI_ACCENT)
        s.blit(title, (SCREEN_W // 2 - title.get_width() // 2, 120))
        options = ["Resume", "Save Game", "Load Game", "Controls", "Quit to Menu"]
        for i, label in enumerate(options):
            sel = i == self.pause_cursor
            color = UI_ACCENT2 if sel else UI_TEXT
            txt = self.hud.big.render(("> " if sel else "  ") + label, True, color)
            s.blit(txt, (SCREEN_W // 2 - 100, 240 + i * 44))

    def _draw_controls(self, s):
        lines = [
            "ON FOOT: WASD move · SHIFT sprint · Mouse aim · LMB shoot · RMB aim-down-sights",
            "R reload · TAB (hold) weapon wheel · scroll cycle weapons · E/F enter vehicle",
            "VEHICLE: W/S gas/brake · A/D steer · SPACE handbrake | AIRCRAFT: SPACE climb · CTRL descend",
            "SHIFT siren (police cars) · V vigilante (in police vehicle) · drive-by with pistol/SMG",
            "ENTER start mission/race at markers · B/E open shop at markers · M map",
            "F5 save · F9 load · ESC pause · F1 hide this help",
        ]
        pad = pygame.Surface((SCREEN_W - 160, 30 + 24 * len(lines)), pygame.SRCALPHA)
        pad.fill((10, 10, 18, 215))
        s.blit(pad, (80, SCREEN_H // 2 - 90))
        for i, line in enumerate(lines):
            txt = self.hud.small.render(line, True, UI_TEXT)
            s.blit(txt, (100, SCREEN_H // 2 - 75 + i * 24))

    def _draw_wasted(self, s):
        dim = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
        dim.fill((60, 0, 0, 130))
        s.blit(dim, (0, 0))
        txt = self.hud.title.render("WASTED", True, (255, 60, 60))
        s.blit(txt, (SCREEN_W // 2 - txt.get_width() // 2, SCREEN_H // 2 - 40))

    # ================================================================ loop
    def run(self):
        while self.running:
            dt = min(0.05, self.clock.tick(FPS) / 1000.0)
            self.handle_events()
            self.update(dt)
            self.draw(dt)
        pygame.quit()
        sys.exit()


if __name__ == "__main__":
    Game().run()
