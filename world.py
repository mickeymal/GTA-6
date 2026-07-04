"""World: procedural Vice Bay city map, districts, day/night cycle, dynamic weather,
neon lighting, tile collision queries and road network helpers for traffic."""
import math
import random
import pygame
from settings import *


class World:
    def __init__(self):
        rng = random.Random(WORLD_SEED)
        self.tiles = [[T_WATER] * MAP_TILES for _ in range(MAP_TILES)]
        self.district_map = [["ocean"] * MAP_TILES for _ in range(MAP_TILES)]
        self.building_color = {}          # (tx,ty) -> color
        self.neon_tiles = []              # [(tx,ty,color)] lit at night
        self.road_tiles = []              # list of (tx,ty) for traffic spawning
        self.sidewalk_tiles = []
        self.water_tiles = []
        self._generate(rng)
        self._detail_seed = WORLD_SEED

        # time & weather
        self.time_of_day = 10.0           # hours, 0..24
        self.weather = WEATHER_CLEAR
        self._weather_timer = rng.uniform(40, 90)
        self.rain_drops = []
        self.lightning_t = 0.0

        # entity registries (populated by Game)
        self.vehicles = []
        self.npcs = []
        self.projectiles = []
        self.pickups = []

        self.minimap = self._render_minimap()

    # ================================================================ generation
    def _generate(self, rng):
        # 1. landmass
        for ty in range(MAP_TILES):
            for tx in range(MAP_TILES):
                if 16 <= tx <= 172 and 16 <= ty <= 176:
                    self.tiles[ty][tx] = T_GRASS
                    self.district_map[ty][tx] = self._district(tx, ty)

        # 2. beach sand strip on the east coast
        for ty in range(20, 172):
            for tx in range(156, 173):
                if self.tiles[ty][tx] != T_WATER:
                    self.tiles[ty][tx] = T_SAND

        # 3. road grid (2-tile-wide roads)
        avenues = list(range(24, 156, 12)) + [154]
        streets = list(range(24, 174, 12))
        for ax in avenues:
            for ty in range(20, 174):
                for w in (0, 1):
                    if self._on_land(ax + w, ty):
                        self.tiles[ty][ax + w] = T_ROAD
        for sy in streets:
            for tx in range(20, 158):
                for w in (0, 1):
                    if self._on_land(tx, sy + w):
                        self.tiles[sy + w][tx] = T_ROAD

        # 4. airport (south-west): flatten, runway + taxiway
        for ty in range(140, 172):
            for tx in range(22, 70):
                if self._on_land(tx, ty):
                    self.tiles[ty][tx] = T_GRASS
                    self.district_map[ty][tx] = "airport"
        for ty in range(150, 155):
            for tx in range(26, 66):
                self.tiles[ty][tx] = T_RUNWAY
        for ty in range(158, 161):
            for tx in range(30, 60):
                self.tiles[ty][tx] = T_RUNWAY
        for tx in range(44, 47):
            for ty in range(150, 161):
                self.tiles[ty][tx] = T_RUNWAY
        # terminal buildings
        for ty in range(163, 168):
            for tx in range(30, 44):
                self.tiles[ty][tx] = T_BUILDING
                self.building_color[(tx, ty)] = rng.choice(DISTRICT_BUILDING["airport"])
        # road access to airport
        for ty in range(140, 164):
            self.tiles[ty][48] = T_ROAD
            self.tiles[ty][49] = T_ROAD

        # 5. docks/port (south): piers extending into the ocean
        for pier_x in (86, 104, 122, 140):
            for ty in range(174, 186):
                for w in (0, 1, 2):
                    self.tiles[ty][pier_x + w] = T_DOCK
        for tx in range(80, 152):
            for ty in range(170, 174):
                if self.tiles[ty][tx] != T_ROAD:
                    self.tiles[ty][tx] = T_DOCK

        # marina on the east beach for boats
        for ty in range(60, 64):
            for tx in range(170, 182):
                self.tiles[ty][tx] = T_DOCK

        # 6. city blocks: buildings with sidewalks
        for ty in range(18, 175):
            for tx in range(18, 156):
                t = self.tiles[ty][tx]
                if t not in (T_GRASS,):
                    continue
                d = self.district_map[ty][tx]
                if d in ("airport",):
                    continue
                near_road = self._adjacent_to(tx, ty, T_ROAD)
                if near_road:
                    self.tiles[ty][tx] = T_SIDEWALK
                    continue
                # interior of block
                if d == "downtown":
                    if rng.random() < 0.92:
                        self._put_building(tx, ty, d, rng, neon=rng.random() < 0.12)
                elif d == "beachside":
                    if rng.random() < 0.75:
                        self._put_building(tx, ty, "beach", rng, neon=rng.random() < 0.2)
                elif d == "haiti":
                    if rng.random() < 0.6:
                        self._put_building(tx, ty, d, rng, neon=rng.random() < 0.03)
                elif d == "suburbs":
                    if rng.random() < 0.45:
                        self._put_building(tx, ty, d, rng)
                elif d == "port":
                    if rng.random() < 0.7:
                        self._put_building(tx, ty, d, rng)
                else:
                    if rng.random() < 0.7:
                        self._put_building(tx, ty, "midtown", rng, neon=rng.random() < 0.05)
                if self.tiles[ty][tx] == T_GRASS and rng.random() < 0.3:
                    self.tiles[ty][tx] = T_PARK

        # 7. central park downtown
        for ty in range(62, 70):
            for tx in range(98, 118):
                if self.tiles[ty][tx] in (T_BUILDING, T_GRASS, T_SIDEWALK, T_PARK):
                    self.tiles[ty][tx] = T_PARK
                    self.building_color.pop((tx, ty), None)

        # 8. index tiles for gameplay queries
        for ty in range(MAP_TILES):
            for tx in range(MAP_TILES):
                t = self.tiles[ty][tx]
                if t == T_ROAD:
                    self.road_tiles.append((tx, ty))
                elif t == T_SIDEWALK:
                    self.sidewalk_tiles.append((tx, ty))
                elif t == T_WATER:
                    self.water_tiles.append((tx, ty))

    def _put_building(self, tx, ty, district, rng, neon=False):
        self.tiles[ty][tx] = T_BUILDING
        self.building_color[(tx, ty)] = rng.choice(DISTRICT_BUILDING[district])
        if neon:
            self.neon_tiles.append((tx, ty, rng.choice(NEON_COLORS)))

    def _district(self, tx, ty):
        if 22 <= tx < 70 and 140 <= ty < 172:
            return "airport"
        if 80 <= tx < 152 and 150 <= ty <= 176:
            return "port"
        if tx >= 148:
            return "beachside"
        if 70 <= tx < 148 and 30 <= ty < 100:
            return "downtown"
        if tx < 70 and ty < 80:
            return "haiti"
        if tx < 70:
            return "suburbs"
        return "midtown"

    def _on_land(self, tx, ty):
        return 0 <= tx < MAP_TILES and 0 <= ty < MAP_TILES and self.tiles[ty][tx] != T_WATER

    def _adjacent_to(self, tx, ty, kind):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = tx + dx, ty + dy
            if 0 <= nx < MAP_TILES and 0 <= ny < MAP_TILES and self.tiles[ny][nx] == kind:
                return True
        return False

    # ================================================================ queries
    def tile_at(self, wx, wy):
        tx, ty = int(wx // TILE), int(wy // TILE)
        if 0 <= tx < MAP_TILES and 0 <= ty < MAP_TILES:
            return self.tiles[ty][tx]
        return T_WATER

    def district_at(self, wx, wy):
        tx, ty = int(wx // TILE), int(wy // TILE)
        if 0 <= tx < MAP_TILES and 0 <= ty < MAP_TILES:
            return self.district_map[ty][tx]
        return "ocean"

    def is_water(self, wx, wy):
        return self.tile_at(wx, wy) == T_WATER

    def blocked_for_foot(self, wx, wy):
        return self.tile_at(wx, wy) in FOOT_BLOCKED

    def blocked_for_car(self, wx, wy):
        return self.tile_at(wx, wy) in CAR_BLOCKED

    def random_road_pos(self, rng=random):
        tx, ty = rng.choice(self.road_tiles)
        return pygame.math.Vector2(tx * TILE + TILE / 2, ty * TILE + TILE / 2)

    def random_sidewalk_pos(self, rng=random):
        tx, ty = rng.choice(self.sidewalk_tiles)
        return pygame.math.Vector2(tx * TILE + TILE / 2, ty * TILE + TILE / 2)

    def road_dirs(self, tx, ty):
        """Directions from a road tile that continue on road (for traffic AI)."""
        out = []
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = tx + dx, ty + dy
            if 0 <= nx < MAP_TILES and 0 <= ny < MAP_TILES and self.tiles[ny][nx] == T_ROAD:
                out.append((dx, dy))
        return out

    # ================================================================ update
    def update(self, dt):
        self.time_of_day = (self.time_of_day + 24.0 * dt / DAY_LENGTH_SECONDS) % 24.0
        self._weather_timer -= dt
        if self._weather_timer <= 0:
            self.weather = random.choice(WEATHER_KINDS)
            self._weather_timer = random.uniform(50, 120)
        if self.weather == WEATHER_STORM:
            if self.lightning_t > 0:
                self.lightning_t -= dt
            elif random.random() < 0.35 * dt:
                self.lightning_t = 0.12

    def darkness(self):
        """0 = full day, 1 = darkest night."""
        h = self.time_of_day
        if 7.0 <= h <= 18.0:
            base = 0.0
        elif h > 18.0:
            base = min(1.0, (h - 18.0) / 3.0)
        else:  # 0..7
            base = min(1.0, (7.0 - h) / 3.0)
        if self.weather == WEATHER_STORM:
            base = min(1.0, base + 0.35)
        elif self.weather == WEATHER_RAIN:
            base = min(1.0, base + 0.2)
        return base

    # ================================================================ draw
    def draw(self, surf, camera):
        vis = camera.visible_rect()
        tx0 = max(0, int(vis.left // TILE))
        ty0 = max(0, int(vis.top // TILE))
        tx1 = min(MAP_TILES - 1, int(vis.right // TILE) + 1)
        ty1 = min(MAP_TILES - 1, int(vis.bottom // TILE) + 1)
        z = camera.zoom
        ts = TILE * z + 1
        dark = self.darkness()
        wet = self.weather in (WEATHER_RAIN, WEATHER_STORM)
        t = pygame.time.get_ticks() * 0.001

        for ty in range(ty0, ty1 + 1):
            for tx in range(tx0, tx1 + 1):
                tile = self.tiles[ty][tx]
                sx, sy = camera.world_to_screen((tx * TILE, ty * TILE))
                r = pygame.Rect(sx, sy, ts, ts)
                h = (tx * 92821 + ty * 68917 + self._detail_seed) & 0xFFFF
                if tile == T_WATER:
                    ripple = int(6 * math.sin(t * 1.4 + (tx + ty) * 0.7))
                    deep = (tx < 12 or tx > 180 or ty < 12 or ty > 184)
                    base = C_WATER_DEEP if deep else C_WATER
                    color = (base[0], min(255, base[1] + 8 + ripple), min(255, base[2] + 14 + ripple))
                elif tile == T_SAND:
                    v = (h % 13) - 6
                    color = (C_SAND[0] + v, C_SAND[1] + v, C_SAND[2] + v)
                elif tile == T_GRASS:
                    v = (h % 15) - 7
                    color = (C_GRASS[0] + v, C_GRASS[1] + v, C_GRASS[2] + v)
                elif tile == T_PARK:
                    v = (h % 15) - 7
                    color = (C_PARK[0] + v, C_PARK[1] + v, C_PARK[2] + v)
                elif tile == T_ROAD:
                    color = C_ROAD
                elif tile == T_SIDEWALK:
                    v = (h % 9) - 4
                    color = (C_SIDEWALK[0] + v, C_SIDEWALK[1] + v, C_SIDEWALK[2] + v)
                elif tile == T_RUNWAY:
                    color = C_RUNWAY
                elif tile == T_DOCK:
                    color = C_DOCK
                else:  # building
                    color = self.building_color.get((tx, ty), (100, 100, 110))
                surf.fill(color, r)

                if tile == T_ROAD:
                    # lane dashes
                    if self.tiles[ty][max(0, tx - 1)] == T_ROAD and self.tiles[ty][min(MAP_TILES - 1, tx + 1)] == T_ROAD:
                        pass
                    if (tx + ty) % 2 == 0:
                        pygame.draw.rect(surf, C_ROAD_LINE,
                                         (r.x + ts * 0.45, r.y + ts * 0.4, max(2, ts * 0.1), max(2, ts * 0.25)))
                    if wet:
                        # rain reflection shimmer on wet roads
                        sh = int(18 + 14 * math.sin(t * 3.0 + h))
                        refl = pygame.Surface((int(ts), int(ts)), pygame.SRCALPHA)
                        refl.fill((120, 160, 220, max(0, sh)))
                        surf.blit(refl, r.topleft)
                elif tile == T_BUILDING:
                    # simple rooftop shading for depth
                    edge = tuple(max(0, ch - 34) for ch in color)
                    pygame.draw.rect(surf, edge, r, max(1, int(2 * z)))
                elif tile == T_RUNWAY and ty % 2 == 0 and (tx % 4) == 0:
                    pygame.draw.rect(surf, (200, 200, 200),
                                     (r.x + ts * 0.4, r.y + ts * 0.45, ts * 0.35, max(2, ts * 0.12)))
                elif tile == T_PARK and h % 7 == 0:
                    # palm tree
                    cx, cy = r.x + ts / 2, r.y + ts / 2
                    pygame.draw.circle(surf, (30, 90, 40), (int(cx), int(cy)), max(2, int(7 * z)))
                    pygame.draw.circle(surf, (96, 70, 40), (int(cx), int(cy)), max(1, int(2 * z)))
                elif tile == T_SAND and h % 11 == 0:
                    cx, cy = r.x + ts / 2, r.y + ts / 2
                    pygame.draw.circle(surf, (44, 110, 52), (int(cx), int(cy)), max(2, int(6 * z)))

    def draw_lighting(self, surf, camera):
        """Night darkness + neon glow + headlight cones handled by main via this overlay."""
        dark = self.darkness()
        if dark <= 0.02 and self.weather != WEATHER_FOG:
            return None
        overlay = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
        if dark > 0.02:
            night = (8, 8, 30, int(178 * dark))
            overlay.fill(night)
            # neon glows punch through the darkness
            vis = camera.visible_rect()
            for (tx, ty, color) in self.neon_tiles:
                wx, wy = tx * TILE + TILE / 2, ty * TILE + TILE / 2
                if not vis.collidepoint(wx, wy):
                    continue
                sx, sy = camera.world_to_screen((wx, wy))
                rad = int(46 * camera.zoom)
                glow = pygame.Surface((rad * 2, rad * 2), pygame.SRCALPHA)
                for rr, aa in ((rad, 26), (int(rad * 0.6), 44), (int(rad * 0.3), 80)):
                    pygame.draw.circle(glow, (*color, int(aa * dark)), (rad, rad), rr)
                overlay.blit(glow, (sx - rad, sy - rad), special_flags=pygame.BLEND_RGBA_SUB)
                pygame.draw.circle(overlay, (0, 0, 0, 0), (int(sx), int(sy)), int(8 * camera.zoom))
        return overlay

    def draw_weather(self, surf, camera, dt):
        if self.weather in (WEATHER_RAIN, WEATHER_STORM):
            n = 90 if self.weather == WEATHER_RAIN else 170
            while len(self.rain_drops) < n:
                self.rain_drops.append([random.uniform(0, SCREEN_W), random.uniform(-SCREEN_H, 0),
                                        random.uniform(500, 800)])
            wind = 60 if self.weather == WEATHER_RAIN else 160
            for d in self.rain_drops:
                d[1] += d[2] * dt
                d[0] += wind * dt
                if d[1] > SCREEN_H:
                    d[0], d[1] = random.uniform(-100, SCREEN_W), random.uniform(-40, -5)
            for d in self.rain_drops:
                pygame.draw.line(surf, (160, 190, 230),
                                 (d[0], d[1]), (d[0] - wind * 0.02, d[1] - 12), 1)
            if self.lightning_t > 0:
                flash = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
                flash.fill((255, 255, 255, 90))
                surf.blit(flash, (0, 0))
        else:
            self.rain_drops.clear()
        if self.weather == WEATHER_FOG:
            fog = pygame.Surface((SCREEN_W, SCREEN_H), pygame.SRCALPHA)
            fog.fill((190, 195, 205, 96))
            surf.blit(fog, (0, 0))

    # ================================================================ minimap
    def _render_minimap(self):
        mm = pygame.Surface((MAP_TILES, MAP_TILES))
        colors = {T_WATER: C_WATER, T_SAND: C_SAND, T_GRASS: C_GRASS, T_ROAD: (90, 90, 96),
                  T_SIDEWALK: C_SIDEWALK, T_BUILDING: (70, 74, 92), T_RUNWAY: C_RUNWAY,
                  T_DOCK: C_DOCK, T_PARK: C_PARK}
        for ty in range(MAP_TILES):
            for tx in range(MAP_TILES):
                mm.set_at((tx, ty), colors[self.tiles[ty][tx]])
        return mm
