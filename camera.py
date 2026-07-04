"""Smooth follow camera with zoom (used for ADS and aircraft altitude view)."""
import pygame
from settings import SCREEN_W, SCREEN_H, MAP_W, MAP_H


class Camera:
    def __init__(self):
        self.pos = pygame.math.Vector2(MAP_W / 2, MAP_H / 2)
        self.target = pygame.math.Vector2(self.pos)
        self.zoom = 1.0
        self.target_zoom = 1.0
        self.shake_t = 0.0
        self.shake_mag = 0.0
        self._shake_off = pygame.math.Vector2()

    def set_target(self, world_pos, zoom=None):
        self.target.update(world_pos)
        if zoom is not None:
            self.target_zoom = zoom

    def shake(self, magnitude=6.0, duration=0.3):
        self.shake_mag = max(self.shake_mag, magnitude)
        self.shake_t = max(self.shake_t, duration)

    def update(self, dt):
        # exponential smoothing toward target
        lerp = min(1.0, 6.0 * dt)
        self.pos += (self.target - self.pos) * lerp
        self.zoom += (self.target_zoom - self.zoom) * min(1.0, 4.0 * dt)
        if self.shake_t > 0:
            import random
            self.shake_t -= dt
            self._shake_off.update(random.uniform(-1, 1) * self.shake_mag,
                                   random.uniform(-1, 1) * self.shake_mag)
            self.shake_mag *= 0.94
        else:
            self._shake_off.update(0, 0)

    # ------------------------------------------------------------ transforms
    def world_to_screen(self, world_pos):
        x = (world_pos[0] - self.pos.x + self._shake_off.x) * self.zoom + SCREEN_W / 2
        y = (world_pos[1] - self.pos.y + self._shake_off.y) * self.zoom + SCREEN_H / 2
        return (x, y)

    def screen_to_world(self, screen_pos):
        x = (screen_pos[0] - SCREEN_W / 2) / self.zoom + self.pos.x - self._shake_off.x
        y = (screen_pos[1] - SCREEN_H / 2) / self.zoom + self.pos.y - self._shake_off.y
        return pygame.math.Vector2(x, y)

    def visible_rect(self):
        w = SCREEN_W / self.zoom
        h = SCREEN_H / self.zoom
        return pygame.Rect(self.pos.x - w / 2 - 64, self.pos.y - h / 2 - 64, w + 128, h + 128)
