"""HUD: minimap with blips, health/armor/money/ammo/wanted UI, notifications,
weapon wheel overlay, fullscreen map, vehicle speedo/altitude, crosshair."""
import math
import pygame
from settings import *
from weapons import WHEEL_ORDER, WHEEL_LABEL
from economy import SHOPS
from activities import RACES


class HUD:
    def __init__(self, game):
        self.game = game
        self.font = pygame.font.SysFont("verdana", 16)
        self.small = pygame.font.SysFont("verdana", 13)
        self.big = pygame.font.SysFont("verdana", 24, bold=True)
        self.title = pygame.font.SysFont("verdana", 52, bold=True)
        self.notifications = []          # [text, color, ttl]
        self.mm_scale = MINIMAP_SIZE / MAP_TILES
        self.minimap_base = None

    def notify(self, text, color=UI_TEXT, ttl=NOTIFY_TIME):
        self.notifications.append([text, color, ttl])
        if len(self.notifications) > 5:
            self.notifications.pop(0)

    def update(self, dt):
        for n in self.notifications:
            n[2] -= dt
        self.notifications = [n for n in self.notifications if n[2] > 0]

    # ================================================================ draw
    def draw(self, surf):
        g = self.game
        self._draw_minimap(surf)
        self._draw_vitals(surf)
        self._draw_money_wanted(surf)
        self._draw_weapon(surf)
        self._draw_objective(surf)
        self._draw_notifications(surf)
        if g.player.vehicle:
            self._draw_vehicle_info(surf)
        else:
            self._draw_crosshair(surf)

    # ---------------------------------------------------------------- minimap
    def _draw_minimap(self, surf):
        g = self.game
        if self.minimap_base is None:
            self.minimap_base = pygame.transform.scale(
                g.world.minimap, (MINIMAP_SIZE, MINIMAP_SIZE))
        x0, y0 = 16, SCREEN_H - MINIMAP_SIZE - 16
        frame = pygame.Rect(x0 - 3, y0 - 3, MINIMAP_SIZE + 6, MINIMAP_SIZE + 6)
        pygame.draw.rect(surf, (10, 10, 16), frame, border_radius=4)
        surf.blit(self.minimap_base, (x0, y0))
        pygame.draw.rect(surf, UI_ACCENT, frame, 2, border_radius=4)

        def blip(world_pos, color, r=3, ring=False):
            bx = x0 + world_pos[0] / MAP_W * MINIMAP_SIZE
            by = y0 + world_pos[1] / MAP_H * MINIMAP_SIZE
            bx = max(x0, min(x0 + MINIMAP_SIZE, bx))
            by = max(y0, min(y0 + MINIMAP_SIZE, by))
            if ring:
                pygame.draw.circle(surf, color, (int(bx), int(by)), r, 1)
            else:
                pygame.draw.circle(surf, color, (int(bx), int(by)), r)

        for sid, s in SHOPS.items():
            blip(s["pos"], s["color"], 2)
        if not g.missions.active:
            for d in RACES.values():
                blip(d["start"], (60, 220, 255), 2, ring=True)
        mpos = g.missions.marker_pos()
        if mpos:
            pulse = 3 + int(1.5 + 1.5 * math.sin(pygame.time.get_ticks() * 0.006))
            blip(mpos, (255, 220, 60) if g.missions.active else UI_ACCENT, pulse)
        if g.police.stars > 0:
            for u in g.police.units:
                blip(u.vehicle.pos, (80, 120, 255), 2)
        for v in g.world.vehicles:
            if not v.dead and v.driver is None:
                blip(v.pos, (200, 200, 210), 1)
        # player arrow
        px = x0 + g.player.pos.x / MAP_W * MINIMAP_SIZE
        py = y0 + g.player.pos.y / MAP_H * MINIMAP_SIZE
        hd = g.player.vehicle.heading if g.player.vehicle else g.player.heading
        f = pygame.math.Vector2(1, 0).rotate(hd)
        tip = (px + f.x * 6, py + f.y * 6)
        l = (px + f.rotate(140).x * 4, py + f.rotate(140).y * 4)
        r = (px + f.rotate(-140).x * 4, py + f.rotate(-140).y * 4)
        pygame.draw.polygon(surf, (255, 255, 255), (tip, l, r))

    # ---------------------------------------------------------------- vitals
    def _draw_vitals(self, surf):
        g = self.game
        x0 = 16 + MINIMAP_SIZE + 10
        y0 = SCREEN_H - 52
        for i, (val, mx, color) in enumerate((
                (g.player.hp, PLAYER_MAX_HP, (90, 220, 110)),
                (g.player.armor, PLAYER_MAX_ARMOR, (120, 160, 240)))):
            bar = pygame.Rect(x0, y0 + i * 18, 150, 12)
            pygame.draw.rect(surf, (20, 20, 28), bar, border_radius=3)
            fill = bar.copy()
            fill.w = int(bar.w * max(0, val) / mx)
            pygame.draw.rect(surf, color, fill, border_radius=3)
            pygame.draw.rect(surf, (0, 0, 0), bar, 1, border_radius=3)

    # ---------------------------------------------------------------- money/wanted
    def _draw_money_wanted(self, surf):
        g = self.game
        money = self.big.render(f"${g.player.money:,}", True, UI_MONEY)
        surf.blit(money, (SCREEN_W - money.get_width() - 20, 14))
        stars = ""
        for i in range(MAX_STARS):
            stars += STAR_CHAR
        star_surf = self.big.render(stars, True, (60, 60, 70))
        sx = SCREEN_W - star_surf.get_width() - 20
        surf.blit(star_surf, (sx, 46))
        if g.police.stars > 0:
            lit = self.big.render(STAR_CHAR * g.police.stars, True, (255, 210, 60))
            surf.blit(lit, (sx, 46))
        # clock + weather
        h = int(g.world.time_of_day)
        m = int((g.world.time_of_day - h) * 60)
        wtxt = self.font.render(f"{h:02d}:{m:02d}  {g.world.weather.upper()}  "
                                f"{g.world.district_at(g.player.pos.x, g.player.pos.y).title()}",
                                True, UI_TEXT)
        surf.blit(wtxt, (SCREEN_W - wtxt.get_width() - 20, 80))

    # ---------------------------------------------------------------- weapon
    def _draw_weapon(self, surf):
        w = self.game.player.weapon
        if not w:
            return
        name = w.d["name"]
        ammo = w.total_ammo_text()
        if w.reload_t > 0:
            ammo = "RELOADING"
        mods = " +".join(sorted(m.split("_")[0] for m in w.mods))
        line = f"{name}{(' [' + mods + ']') if mods else ''}  {ammo}"
        txt = self.font.render(line, True, UI_TEXT)
        pad = pygame.Surface((txt.get_width() + 16, 26), pygame.SRCALPHA)
        pad.fill(UI_BG)
        surf.blit(pad, (SCREEN_W - txt.get_width() - 32, SCREEN_H - 40))
        surf.blit(txt, (SCREEN_W - txt.get_width() - 24, SCREEN_H - 36))

    # ---------------------------------------------------------------- objective
    def _draw_objective(self, surf):
        g = self.game
        text = g.missions.objective_text()
        if not text:
            return
        txt = self.font.render(text, True, UI_TEXT)
        pad = pygame.Surface((txt.get_width() + 24, 30), pygame.SRCALPHA)
        pad.fill(UI_BG)
        x = SCREEN_W // 2 - pad.get_width() // 2
        surf.blit(pad, (x, 12))
        surf.blit(txt, (x + 12, 18))
        if g.missions.active:
            pygame.draw.rect(surf, (255, 220, 60), (x, 12, 4, 30))

    # ---------------------------------------------------------------- notifications
    def _draw_notifications(self, surf):
        y = 56
        for text, color, ttl in reversed(self.notifications):
            a = min(1.0, ttl / 0.5)
            txt = self.font.render(text, True, color)
            pad = pygame.Surface((txt.get_width() + 16, 24), pygame.SRCALPHA)
            pad.fill((12, 12, 20, int(170 * a)))
            surf.blit(pad, (16, y))
            txt.set_alpha(int(255 * a))
            surf.blit(txt, (24, y + 3))
            y += 28

    # ---------------------------------------------------------------- vehicle info
    def _draw_vehicle_info(self, surf):
        v = self.game.player.vehicle
        speed = int(v.speed * 0.22)          # px/s -> "mph"
        line = f"{v.name}  {speed} mph"
        if v.spec["cls"] in ("plane", "heli"):
            line += f"  ALT {int(v.altitude)}"
            if v.spec["cls"] == "plane" and v.airborne and v.speed < v.spec["stall"]:
                line += "  [STALL]"
        hp_frac = v.hp / v.max_hp
        txt = self.font.render(line, True, UI_TEXT if hp_frac > 0.35 else UI_WARN)
        pad = pygame.Surface((txt.get_width() + 16, 42), pygame.SRCALPHA)
        pad.fill(UI_BG)
        x = SCREEN_W - pad.get_width() - 16
        surf.blit(pad, (x, SCREEN_H - 92))
        surf.blit(txt, (x + 8, SCREEN_H - 88))
        bar = pygame.Rect(x + 8, SCREEN_H - 64, pad.get_width() - 16, 8)
        pygame.draw.rect(surf, (20, 20, 28), bar, border_radius=2)
        fill = bar.copy()
        fill.w = int(bar.w * max(0, hp_frac))
        pygame.draw.rect(surf, (90, 220, 110) if hp_frac > 0.35 else UI_WARN, fill, border_radius=2)

    # ---------------------------------------------------------------- crosshair
    def _draw_crosshair(self, surf):
        g = self.game
        w = g.player.weapon
        if not w or w.is_melee:
            return
        mx, my = pygame.mouse.get_pos()
        gap = 5 + int(w.spread * (2 if not g.ads else 0.8))
        color = UI_ACCENT2 if g.ads else (240, 240, 240)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            pygame.draw.line(surf, color, (mx + dx * gap, my + dy * gap),
                             (mx + dx * (gap + 7), my + dy * (gap + 7)), 2)
        if g.ads and w.d["cat"] == "sniper":
            pygame.draw.circle(surf, color, (mx, my), 34, 1)

    # ================================================================ weapon wheel
    def draw_weapon_wheel(self, surf):
        g = self.game
        dim = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
        dim.fill((0, 0, 10, 140))
        surf.blit(dim, (0, 0))
        cx, cy = SCREEN_W // 2, SCREEN_H // 2
        n = len(WHEEL_ORDER)
        mouse = pygame.math.Vector2(pygame.mouse.get_pos()) - (cx, cy)
        hovered = None
        if mouse.length() > 40:
            ang = math.degrees(math.atan2(mouse.y, mouse.x)) % 360
            hovered = WHEEL_ORDER[int(((ang + 360 / n / 2) % 360) / (360 / n))]
        for i, cat in enumerate(WHEEL_ORDER):
            ang = math.radians(i * 360 / n)
            px = cx + math.cos(ang) * 190
            py = cy + math.sin(ang) * 190
            owned = cat in g.player.weapons
            sel = cat == g.player.current_cat
            hov = cat == hovered
            r = 52 if hov else 44
            color = UI_ACCENT if sel else (UI_ACCENT2 if hov else (60, 60, 80))
            pygame.draw.circle(surf, (16, 16, 26), (int(px), int(py)), r)
            pygame.draw.circle(surf, color if owned else (40, 40, 50), (int(px), int(py)), r, 3)
            label = self.small.render(WHEEL_LABEL[cat], True,
                                      UI_TEXT if owned else (90, 90, 100))
            surf.blit(label, (px - label.get_width() / 2, py - 18))
            if owned:
                w = g.player.weapons[cat]
                wname = self.small.render(w.d["name"], True, UI_ACCENT2 if sel else (170, 170, 180))
                surf.blit(wname, (px - wname.get_width() / 2, py))
                ammo = self.small.render(w.total_ammo_text(), True, (150, 150, 160))
                surf.blit(ammo, (px - ammo.get_width() / 2, py + 15))
        hint = self.font.render("Point at a slot and release TAB", True, (170, 170, 180))
        surf.blit(hint, (cx - hint.get_width() // 2, SCREEN_H - 60))
        return hovered

    # ================================================================ big map
    def draw_big_map(self, surf):
        g = self.game
        size = min(SCREEN_W, SCREEN_H) - 80
        x0 = (SCREEN_W - size) // 2
        y0 = (SCREEN_H - size) // 2
        dim = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
        dim.fill((0, 0, 10, 200))
        surf.blit(dim, (0, 0))
        mm = pygame.transform.scale(g.world.minimap, (size, size))
        surf.blit(mm, (x0, y0))
        pygame.draw.rect(surf, UI_ACCENT, (x0 - 2, y0 - 2, size + 4, size + 4), 2)

        def blip(world_pos, color, r=5, label=None):
            bx = x0 + world_pos[0] / MAP_W * size
            by = y0 + world_pos[1] / MAP_H * size
            pygame.draw.circle(surf, color, (int(bx), int(by)), r)
            if label:
                surf.blit(self.small.render(label, True, color), (bx + 7, by - 7))

        for sid, s in SHOPS.items():
            blip(s["pos"], s["color"], 4, s["name"])
        for rid, d in RACES.items():
            blip(d["start"], (60, 220, 255), 4, d["name"])
        mpos = g.missions.marker_pos()
        if mpos:
            blip(mpos, (255, 220, 60), 7, "MISSION")
        blip(g.player.pos, (255, 255, 255), 6, "YOU")
        hint = self.font.render("M / ESC to close", True, (170, 170, 180))
        surf.blit(hint, (SCREEN_W // 2 - hint.get_width() // 2, y0 + size + 8))
