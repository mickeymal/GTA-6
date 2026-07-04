"""Mission system: objective engine (goto / enter / kill / destroy / pickup /
air checkpoints / landing / escape wanted / mid-mission dialogue) + 10 story
missions with timers, protected vehicles, fail states and branching finale."""
import math
import random
import pygame
from settings import *
from vehicle import make_vehicle
from npc import GangMember, TrafficDriver


def t(x, y):
    return pygame.math.Vector2(x * TILE + TILE // 2, y * TILE + TILE // 2)


# ================================================================ objectives
class Objective:
    text = ""
    time_limit = None

    def marker(self, game):
        return None

    def update(self, dt, game):
        """Return True when complete."""
        return False

    def fail_reason(self, game):
        return None

    def on_start(self, game):
        pass


class GoTo(Objective):
    def __init__(self, pos, text, radius=52, need_vehicle=None, time_limit=None):
        self.pos = pygame.math.Vector2(pos)
        self.text = text
        self.radius = radius
        self.need_vehicle = need_vehicle        # None | "boat" | "car" | vehicle instance
        self.time_limit = time_limit

    def marker(self, game):
        return self.pos

    def update(self, dt, game):
        p = game.player
        if (p.pos - self.pos).length() > self.radius:
            return False
        nv = self.need_vehicle
        if nv is None:
            return True
        if isinstance(nv, str):
            return p.vehicle is not None and p.vehicle.spec["cls"] == nv
        return p.vehicle is nv


class EnterVehicle(Objective):
    def __init__(self, vehicle, text, time_limit=None):
        self.vehicle = vehicle
        self.text = text
        self.time_limit = time_limit

    def marker(self, game):
        return self.vehicle.pos

    def update(self, dt, game):
        return game.player.vehicle is self.vehicle

    def fail_reason(self, game):
        if self.vehicle.dead:
            return "The vehicle was destroyed!"
        return None


class KillAll(Objective):
    def __init__(self, targets, text):
        self.targets = targets
        self.text = text

    def marker(self, game):
        alive = [n for n in self.targets if not n.dead]
        return alive[0].pos if alive else None

    def update(self, dt, game):
        return all(n.dead for n in self.targets)


class Destroy(Objective):
    def __init__(self, vehicles, text, escape_check=None, time_limit=None):
        self.vehicles = vehicles
        self.text = text
        self.escape_check = escape_check        # fn(game) -> fail string or None
        self.time_limit = time_limit

    def marker(self, game):
        alive = [v for v in self.vehicles if not v.dead]
        return alive[0].pos if alive else None

    def update(self, dt, game):
        return all(v.dead for v in self.vehicles)

    def fail_reason(self, game):
        return self.escape_check(game) if self.escape_check else None


class Pickup(Objective):
    def __init__(self, pos, text, on_collect=None, radius=40):
        self.pos = pygame.math.Vector2(pos)
        self.text = text
        self.on_collect = on_collect
        self.radius = radius

    def marker(self, game):
        return self.pos

    def update(self, dt, game):
        if (game.player.pos - self.pos).length() < self.radius:
            if self.on_collect:
                self.on_collect(game)
            return True
        return False


class AirCheckpoints(Objective):
    def __init__(self, points, text, min_alt=70, vehicle=None):
        self.points = [pygame.math.Vector2(p) for p in points]
        self.index = 0
        self.base_text = text
        self.min_alt = min_alt
        self.vehicle = vehicle

    @property
    def text(self):
        return f"{self.base_text} ({self.index}/{len(self.points)})"

    def marker(self, game):
        return self.points[self.index] if self.index < len(self.points) else None

    def update(self, dt, game):
        v = game.player.vehicle
        if self.vehicle and v is not self.vehicle:
            return False
        if v and v.altitude >= self.min_alt and self.index < len(self.points):
            if (v.pos - self.points[self.index]).length() < 110:
                self.index += 1
                game.hud.notify(f"Checkpoint {self.index}/{len(self.points)}", UI_ACCENT2)
        return self.index >= len(self.points)

    def fail_reason(self, game):
        if self.vehicle and self.vehicle.dead:
            return "Aircraft destroyed!"
        return None


class Land(Objective):
    def __init__(self, pos, text, vehicle, radius=150):
        self.pos = pygame.math.Vector2(pos)
        self.text = text
        self.vehicle = vehicle
        self.radius = radius

    def marker(self, game):
        return self.pos

    def update(self, dt, game):
        v = self.vehicle
        return (not v.dead and v.altitude < 2 and v.speed < 60
                and (v.pos - self.pos).length() < self.radius)

    def fail_reason(self, game):
        if self.vehicle.dead:
            return "Aircraft destroyed!"
        return None


class EscapeWanted(Objective):
    text = "Lose the cops!"

    def update(self, dt, game):
        return game.police.stars == 0


class TalkTo(Objective):
    """Walk to a point, then run a dialogue; completes when dialogue ends."""

    def __init__(self, pos, tree, node, text):
        self.pos = pygame.math.Vector2(pos)
        self.tree = tree
        self.node = node
        self.text = text
        self.started = False
        self.finished = False

    def marker(self, game):
        return None if self.started else self.pos

    def update(self, dt, game):
        if not self.started and (game.player.pos - self.pos).length() < 52:
            self.started = True
            game.dialogue.start(self.tree, self.node, on_finish=lambda: setattr(self, "finished", True))
        return self.finished


# ================================================================ helper AI
class FleeingBoatDriver:
    """Drives a boat along water waypoints; used for chase-target missions."""

    def __init__(self, vehicle, waypoints):
        self.vehicle = vehicle
        vehicle.driver = self
        self.waypoints = [pygame.math.Vector2(w) for w in waypoints]
        self.dead = False
        self.pos = vehicle.pos

    def on_carjacked(self, vehicle):
        pass

    def take_damage(self, dmg, source=None):
        pass

    def update(self, dt, world, game):
        v = self.vehicle
        if v.dead:
            self.dead = True
            return
        if self.waypoints and (v.pos - self.waypoints[0]).length() < 90:
            self.waypoints.pop(0)
        target = self.waypoints[0] if self.waypoints else v.pos + v.forward * 100
        delta = target - v.pos
        desired = math.degrees(math.atan2(delta.y, delta.x))
        diff = (desired - v.heading + 180) % 360 - 180
        v.update(dt, world, dict(throttle=0.9, steer=max(-1, min(1, diff / 35))), game.particles, game)

    def draw(self, surf, camera):
        pass


# ================================================================ missions
class Mission:
    def __init__(self, mid, title, giver_pos, dialogue_tree, reward):
        self.id = mid
        self.title = title
        self.giver_pos = pygame.math.Vector2(giver_pos)
        self.dialogue_tree = dialogue_tree
        self.reward = reward
        self.objectives = []
        self.index = 0
        self.timer = None
        self.protected = []          # vehicles that must survive
        self.spawned_npcs = []
        self.spawned_vehicles = []

    # -------- override
    def setup(self, game):
        pass

    def on_objective_complete(self, game, obj):
        pass

    def on_complete(self, game):
        pass

    # -------- runtime
    def current(self):
        return self.objectives[self.index] if self.index < len(self.objectives) else None

    def start_objective(self, game):
        obj = self.current()
        if obj:
            obj.on_start(game)
            self.timer = obj.time_limit

    def spawn_gang(self, game, positions, gang="cartel", hp=70):
        out = []
        for pos in positions:
            g = GangMember(pos, gang)
            g.hp = hp
            g.state = "attack"
            g.target = game.player
            game.world.npcs.append(g)
            self.spawned_npcs.append(g)
            out.append(g)
        return out

    def spawn_vehicle(self, game, kind, pos, heading=0.0):
        v = make_vehicle(kind, pos, heading)
        game.world.vehicles.append(v)
        self.spawned_vehicles.append(v)
        return v

    def cleanup(self, game):
        for n in self.spawned_npcs:
            n.dead = True
            n.corpse_t = 0
        for v in self.spawned_vehicles:
            if game.player.vehicle is v:
                game.player.exit_vehicle(game.world, game.hud)
            v.dead = True


# ---------------------------------------------------------------- 10 missions
class M1Welcome(Mission):
    def __init__(self):
        super().__init__("m1", "Welcome to Vice Bay", t(37, 37), "intro", 300)

    def setup(self, game):
        self.objectives = [GoTo(t(36, 48), "Get to the safehouse in Little Haiti")]

    def on_complete(self, game):
        game.player.owned_properties.append("Little Haiti Safehouse")
        game.hud.notify("Safehouse unlocked - walk in to save", UI_ACCENT2)


class M2Repo(Mission):
    def __init__(self):
        super().__init__("m2", "Repo Man", t(37, 37), "repo", 2500)

    def setup(self, game):
        car = self.spawn_vehicle(game, "sports", t(109, 49), heading=90)
        self.protected = [car]
        self.objectives = [
            GoTo(t(109, 49), "The marked Banshee is parked downtown", radius=340),
            EnterVehicle(car, "Steal the Banshee GT"),
            GoTo(t(109, 171), "Deliver it to the docks garage - don't wreck it!",
                 need_vehicle=car),
        ]


class M3Chop(Mission):
    def __init__(self):
        super().__init__("m3", "Chop Chop", t(97, 49), "chop", 5000)

    def setup(self, game):
        self.objectives = [GoTo(t(36, 61), "Reach the market corner in Little Haiti", radius=260)]

    def on_objective_complete(self, game, obj):
        if isinstance(obj, GoTo):
            targets = self.spawn_gang(game, [t(34, 60), t(38, 62), t(36, 59)])
            self.objectives.append(KillAll(targets, "Take out the cartel dealers"))


class M4Ocean(Mission):
    def __init__(self):
        super().__init__("m4", "Ocean Runner", t(37, 37), "ocean", 6000)

    def setup(self, game):
        boat = self.spawn_vehicle(game, "speedboat", t(177, 62), heading=90)
        self.protected = [boat]
        self.objectives = [
            GoTo(t(172, 61), "Get to the marina on Ocean Beach"),
            EnterVehicle(boat, "Take the Squalo speedboat"),
            GoTo(t(178, 110), "Recover the floating package", need_vehicle="boat",
                 radius=70, time_limit=90),
            GoTo(t(108, 182), "Deliver the package to the docks pier", need_vehicle="boat",
                 radius=80, time_limit=110),
        ]


class M5Coop(Mission):
    def __init__(self):
        super().__init__("m5", "Fly the Coop", t(97, 49), "coop", 9000)

    def setup(self, game):
        plane = self.spawn_vehicle(game, "stunt_plane", t(30, 152), heading=0)
        self.protected = [plane]
        rings = [t(70, 120), t(110, 90), t(150, 70), t(150, 130), t(100, 140)]
        self.objectives = [
            GoTo(t(29, 152), "Get to the stunt plane on the airport runway", radius=300),
            EnterVehicle(plane, "Board the Mallard - take off down the runway (hold SPACE to climb)"),
            AirCheckpoints(rings, "Fly through the survey checkpoints", vehicle=plane),
            Land(t(45, 152), "Land back on the runway (slow down, release SPACE / hold CTRL)",
                 plane, radius=220),
        ]


class M6Skies(Mission):
    def __init__(self):
        super().__init__("m6", "Vice Skies", t(37, 37), "skies", 15000)

    def setup(self, game):
        heli = self.spawn_vehicle(game, "helicopter", t(104, 65))
        boat = self.spawn_vehicle(game, "speedboat", t(178, 80), heading=90)
        driver = FleeingBoatDriver(boat, [t(180, 120), t(178, 150), t(160, 182), t(120, 186)])
        game.world.npcs.append(driver)
        self.spawned_npcs.append(driver)
        self.boat = boat

        def escaped(g):
            if not boat.dead and boat.pos.y > MAP_H - 200:
                return "The snitch escaped by sea!"
            return None

        self.objectives = [
            EnterVehicle(heli, "Get to the Sparrow on the downtown helipad"),
            Destroy([boat], "Sink the snitch's speedboat before it escapes the bay!",
                    escape_check=escaped),
        ]


class M7Bank(Mission):
    def __init__(self):
        super().__init__("m7", "Bank Job", t(97, 49), "bank", 40000)

    def setup(self, game):
        def grab_cash(g):
            g.police.stars = 4
            g.police.decay_t = 0
            g.hud.notify("Silent alarm triggered!", UI_WARN)
            g.camera.shake(8, 0.5)

        self.objectives = [
            GoTo(t(85, 49), "Get to the Vice National Bank downtown"),
            Pickup(t(85, 49), "Grab the cash cart", on_collect=grab_cash),
            EscapeWanted(),
            GoTo(t(36, 48), "Get the money to the safehouse"),
        ]
        # second objective needs a tiny walk so both aren't instant
        self.objectives[1].pos += pygame.math.Vector2(0, -3 * TILE)


class M8Convoy(Mission):
    def __init__(self):
        super().__init__("m8", "Convoy Breaker", t(37, 37), "convoy", 20000)

    def setup(self, game):
        game.player.give_weapon("rpg", 6)
        game.hud.notify("RPG delivered", UI_ACCENT2)
        trucks = []
        for i in range(3):
            tr = self.spawn_vehicle(game, "armored", t(72, 36 + i * 3), heading=90)
            d = TrafficDriver(tr)
            d.panicked = True
            game.world.npcs.append(d)
            self.spawned_npcs.append(d)
            trucks.append(tr)
        self.objectives = [
            Destroy(trucks, "Destroy the three armored cartel trucks!", time_limit=240),
        ]


class M9Setup(Mission):
    def __init__(self):
        super().__init__("m9", "The Setup", t(97, 49), "setup", 12000)

    def setup(self, game):
        self.objectives = [
            GoTo(t(172, 61), "Get to the marina and find Rico"),
            TalkTo(t(172, 61) + pygame.math.Vector2(40, 0), "setup", "confront",
                   "Confront Rico"),
        ]

    def on_objective_complete(self, game, obj):
        if isinstance(obj, TalkTo):
            if "sided_rico" in game.player.flags:
                hitters = self.spawn_gang(game, [t(170, 58), t(174, 64), t(168, 63)],
                                          gang="cartel", hp=90)
                self.objectives.append(KillAll(hitters, "Marisol's assassins are here - survive!"))
            else:
                guards = self.spawn_gang(game, [t(170, 58), t(174, 64), t(168, 63)],
                                         gang="haitians", hp=90)
                self.objectives.append(KillAll(guards, "Take out Rico and his guards"))
            self.objectives.append(EscapeWanted())
            game.police.add_heat(2.0, game)


class M10Kingpin(Mission):
    def __init__(self):
        super().__init__("m10", "Kingpin's Fall", t(97, 49), "kingpin", 100000)

    def setup(self, game):
        boss_name = "Marisol" if "sided_rico" in game.player.flags else "El Tiburon"
        self.boss_name = boss_name
        heli = self.spawn_vehicle(game, "helicopter", t(126, 26))
        self.escape_heli = heli
        self.objectives = [
            GoTo(t(120, 25), "Assault the mansion on the north shore", radius=280),
        ]

    def on_objective_complete(self, game, obj):
        if isinstance(obj, GoTo) and not any(isinstance(o, KillAll) for o in self.objectives):
            guards = self.spawn_gang(game, [t(118, 24), t(122, 26), t(119, 27),
                                            t(123, 23), t(117, 26)], hp=90)
            boss = self.spawn_gang(game, [t(120, 22)], hp=420)[0]
            boss.color = (255, 215, 0)
            boss.weapon_key = "assault_rifle"
            from weapons import WeaponInstance
            boss.weapon = WeaponInstance("assault_rifle", 9999)
            self.objectives.append(KillAll(guards + [boss],
                                           f"Kill {self.boss_name} and the guards"))
            self.objectives.append(EnterVehicle(self.escape_heli, "Get to the escape helicopter!"))
            game.police.add_heat(4.0, game)

    def on_complete(self, game):
        game.police.clear()
        game.player.flags.add("story_complete")
        game.hud.notify("VICE BAY IS YOURS. Story complete!", (255, 215, 0), 10.0)


ALL_MISSIONS = [M1Welcome, M2Repo, M3Chop, M4Ocean, M5Coop, M6Skies, M7Bank, M8Convoy, M9Setup, M10Kingpin]


# ================================================================ manager
class MissionManager:
    def __init__(self, game):
        self.game = game
        self.missions = [cls() for cls in ALL_MISSIONS]
        self.completed = set()
        self.active = None

    # -------- save/load helpers
    def state(self):
        return sorted(self.completed)

    def restore(self, completed_ids):
        self.completed = set(completed_ids)
        self.missions = [cls() for cls in ALL_MISSIONS]
        self.active = None

    def next_mission(self):
        for m in self.missions:
            if m.id not in self.completed:
                return m
        return None

    # -------- flow
    def try_start_at_marker(self):
        """Called when player presses ENTER near the mission giver marker."""
        if self.active:
            return False
        m = self.next_mission()
        if not m:
            return False
        if (self.game.player.pos - m.giver_pos).length() < 60:
            self.game.dialogue.start(m.dialogue_tree, on_finish=lambda: self._begin(m))
            return True
        return False

    def _begin(self, mission):
        self.active = mission
        mission.index = 0
        mission.objectives = []
        mission.spawned_npcs = []
        mission.spawned_vehicles = []
        mission.setup(self.game)
        mission.start_objective(self.game)
        self.game.hud.notify(f"MISSION: {mission.title}", UI_ACCENT, 5.0)

    def fail(self, reason):
        m = self.active
        if not m:
            return
        m.cleanup(self.game)
        self.active = None
        self.game.hud.notify(f"MISSION FAILED - {reason}", UI_WARN, 5.0)

    def complete(self):
        m = self.active
        self.game.player.money += m.reward
        self.completed.add(m.id)
        m.on_complete(self.game)
        self.active = None
        self.game.hud.notify(f"MISSION PASSED! +${m.reward:,}", UI_MONEY, 5.0)
        nxt = self.next_mission()
        if nxt:
            self.game.hud.notify(f"New mission available: {nxt.title}", UI_ACCENT2, 5.0)

    def update(self, dt):
        game = self.game
        m = self.active
        if not m:
            return
        if game.player.dead:
            self.fail("You were wasted")
            return
        for v in m.protected:
            if v.dead:
                self.fail("Mission vehicle destroyed")
                return
        obj = m.current()
        if obj is None:
            self.complete()
            return
        if m.timer is not None:
            m.timer -= dt
            if m.timer <= 0:
                self.fail("Out of time")
                return
        reason = obj.fail_reason(game)
        if reason:
            self.fail(reason)
            return
        if obj.update(dt, game):
            m.index += 1
            m.on_objective_complete(game, obj)
            m.start_objective(game)
            if m.current() is None:
                self.complete()

    # -------- rendering data for HUD
    def marker_pos(self):
        if self.active:
            obj = self.active.current()
            return obj.marker(self.game) if obj else None
        m = self.next_mission()
        return m.giver_pos if m else None

    def objective_text(self):
        if self.active:
            obj = self.active.current()
            txt = obj.text if obj else ""
            if self.active.timer is not None:
                txt += f"   [{int(self.active.timer)}s]"
            return txt
        m = self.next_mission()
        if m:
            return f"Next mission: {m.title} - find the pink marker (ENTER to start)"
        return "Story complete. Own the city."

    def draw_markers(self, surf, camera):
        pos = self.marker_pos()
        if pos is None:
            return
        sx, sy = camera.world_to_screen(pos)
        if -60 < sx < SCREEN_W + 60 and -60 < sy < SCREEN_H + 60:
            tt = pygame.time.get_ticks() * 0.004
            r = int((16 + 4 * math.sin(tt)) * camera.zoom)
            color = UI_ACCENT if not self.active else (255, 220, 60)
            pygame.draw.circle(surf, color, (int(sx), int(sy)), r, 3)
            pygame.draw.circle(surf, color, (int(sx), int(sy)), max(1, r // 3))
