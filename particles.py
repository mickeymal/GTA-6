"""Particle system: explosions, muzzle flash, smoke, fire, blood, sparks, splashes."""
import random
import pygame


class Particle:
    __slots__ = ("pos", "vel", "life", "max_life", "size", "color", "kind", "drag", "grow")

    def __init__(self, pos, vel, life, size, color, kind="dot", drag=0.98, grow=0.0):
        self.pos = pygame.math.Vector2(pos)
        self.vel = pygame.math.Vector2(vel)
        self.life = life
        self.max_life = life
        self.size = size
        self.color = color
        self.kind = kind
        self.drag = drag
        self.grow = grow


class ParticleSystem:
    def __init__(self):
        self.particles = []

    def update(self, dt):
        alive = []
        for p in self.particles:
            p.life -= dt
            if p.life <= 0:
                continue
            p.pos += p.vel * dt
            p.vel *= p.drag
            p.size += p.grow * dt
            alive.append(p)
        self.particles = alive

    def draw(self, surf, camera):
        vis = camera.visible_rect()
        for p in self.particles:
            if not vis.collidepoint(p.pos.x, p.pos.y):
                continue
            sx, sy = camera.world_to_screen(p.pos)
            frac = max(0.0, p.life / p.max_life)
            size = max(1, int(p.size * camera.zoom))
            if p.kind == "smoke":
                a = int(120 * frac)
                s = pygame.Surface((size * 2, size * 2), pygame.SRCALPHA)
                pygame.draw.circle(s, (*p.color, a), (size, size), size)
                surf.blit(s, (sx - size, sy - size))
            elif p.kind == "fire":
                c = (255, int(120 + 120 * frac), 40)
                pygame.draw.circle(surf, c, (int(sx), int(sy)), size)
            elif p.kind == "flash":
                pygame.draw.circle(surf, p.color, (int(sx), int(sy)), size)
            else:
                c = tuple(int(ch * (0.4 + 0.6 * frac)) for ch in p.color)
                pygame.draw.circle(surf, c, (int(sx), int(sy)), size)

    # ------------------------------------------------------------ effects
    def burst(self, pos, count, color, speed=120, life=0.6, size=3, kind="dot"):
        for _ in range(count):
            ang = random.uniform(0, 6.283)
            spd = random.uniform(0.2, 1.0) * speed
            vel = (spd * pygame.math.Vector2(1, 0)).rotate_rad(ang)
            self.particles.append(Particle(pos, vel, random.uniform(0.5, 1.0) * life,
                                           random.uniform(0.6, 1.3) * size, color, kind))

    def explosion(self, pos, scale=1.0):
        self.burst(pos, int(26 * scale), (255, 200, 60), 260 * scale, 0.5, 6 * scale, "fire")
        self.burst(pos, int(18 * scale), (255, 120, 30), 180 * scale, 0.7, 5 * scale, "fire")
        self.burst(pos, int(20 * scale), (90, 90, 90), 90 * scale, 1.8, 9 * scale, "smoke")
        self.burst(pos, int(14 * scale), (255, 255, 200), 380 * scale, 0.25, 2, "flash")

    def muzzle_flash(self, pos, direction):
        d = pygame.math.Vector2(direction)
        if d.length_squared() > 0:
            d = d.normalize()
        for _ in range(4):
            vel = d.rotate(random.uniform(-18, 18)) * random.uniform(180, 320)
            self.particles.append(Particle(pos, vel, 0.07, 3, (255, 230, 120), "flash", 0.8))

    def blood(self, pos):
        self.burst(pos, 10, (170, 20, 25), 110, 0.5, 2.5)

    def sparks(self, pos):
        self.burst(pos, 8, (255, 220, 120), 200, 0.3, 2)

    def smoke(self, pos, scale=1.0):
        vel = (random.uniform(-12, 12), random.uniform(-30, -12))
        self.particles.append(Particle(pos, vel, random.uniform(0.8, 1.6),
                                       5 * scale, (110, 110, 110), "smoke", 0.99, 6 * scale))

    def splash(self, pos):
        self.burst(pos, 8, (170, 210, 240), 90, 0.45, 2.5)

    def fire_patch(self, pos):
        vel = (random.uniform(-10, 10), random.uniform(-46, -20))
        self.particles.append(Particle(pos, vel, random.uniform(0.4, 0.9), 5, (255, 140, 40), "fire", 0.97))
