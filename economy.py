"""Economy: gun shop (weapons + attachments + ammo), clothes shop, car dealership,
property purchases, Pay'n'Spray, and stolen-vehicle export at the docks."""
import pygame
from settings import *
from weapons import WEAPON_DEFS, ATTACHMENTS
from vehicle import VEHICLE_SPECS, make_vehicle


def t(x, y):
    return pygame.math.Vector2(x * TILE + TILE // 2, y * TILE + TILE // 2)


SHOPS = {
    "gun":      dict(pos=t(85, 60), name="Ammu-Nation", color=(230, 120, 40), key="G"),
    "clothes":  dict(pos=t(150, 72), name="Vice Threads", color=(240, 80, 200), key="C"),
    "cars":     dict(pos=t(121, 96), name="Sunshine Autos", color=(80, 200 , 240), key="A"),
    "property": dict(pos=t(97, 61), name="Vice Realty", color=(120, 240, 120), key="P"),
    "spray":    dict(pos=t(61, 96), name="Pay'n'Spray", color=(200, 200, 90), key="S"),
    "export":   dict(pos=t(123, 171), name="Dock Export", color=(160, 160, 220), key="E"),
}

PROPERTIES = [
    ("Ocean View Condo", 50000, t(150, 60)),
    ("Downtown Loft", 120000, t(96, 60)),
    ("Starfish Marina House", 200000, t(168, 60)),
]

DEALER_STOCK = ["bicycle", "motorcycle", "sedan", "suv", "muscle", "sports",
                "jetski", "speedboat", "fishing", "yacht", "stunt_plane", "helicopter", "jet"]

OUTFITS = ["Street Casual", "Vice Pink", "White Linen", "Night Ops"]


class ShopMenu:
    """Generic scrolling buy-menu; built per shop when opened."""

    def __init__(self, game):
        self.game = game
        self.open_shop = None
        self.items = []              # (label, price_or_None, callback)
        self.cursor = 0
        self.font = pygame.font.SysFont("verdana", 17)
        self.title_font = pygame.font.SysFont("verdana", 22, bold=True)

    @property
    def running(self):
        return self.open_shop is not None

    # ------------------------------------------------------------ open
    def try_open_nearby(self):
        p = self.game.player
        for sid, s in SHOPS.items():
            if (p.pos - s["pos"]).length() < 64:
                self.open(sid)
                return True
        return False

    def open(self, sid):
        self.open_shop = sid
        self.cursor = 0
        build = getattr(self, f"_build_{sid}")
        self.items = build()

    def close(self):
        self.open_shop = None

    # ------------------------------------------------------------ stock builders
    def _build_gun(self):
        g = self.game
        items = []
        for key, d in WEAPON_DEFS.items():
            if d["price"] <= 0:
                continue
            def buy(k=key):
                dd = WEAPON_DEFS[k]
                ammo = dd.get("mag", 1) * 3 if dd["cat"] != "thrown" else 5
                g.player.give_weapon(k, ammo)
                g.hud.notify(f"Bought {dd['name']}", UI_MONEY)
            items.append((f"{d['name']} ({d['cat']})", d["price"], buy))
        for key, d in WEAPON_DEFS.items():
            if d.get("ammo_price") and d["cat"] not in ("melee",):
                def buy_ammo(k=key):
                    amt = 5 if WEAPON_DEFS[k]["cat"] == "thrown" else WEAPON_DEFS[k].get("mag", 10) * 2
                    g.player.add_ammo(k, amt)
                    g.hud.notify("Ammo purchased", UI_MONEY)
                items.append((f"Ammo: {d['name']}", d["ammo_price"], buy_ammo))
        for akey, a in ATTACHMENTS.items():
            def buy_mod(k=akey):
                w = g.player.weapon
                if w and w.d["cat"] in ATTACHMENTS[k]["fits"]:
                    w.mods.add(k)
                    g.hud.notify(f"{ATTACHMENTS[k]['name']} fitted to {w.d['name']}", UI_MONEY)
                else:
                    g.hud.notify("Doesn't fit your equipped weapon", UI_WARN)
                    g.player.money += ATTACHMENTS[k]["price"]   # refund
            items.append((f"Mod: {a['name']} (equipped weapon)", a["price"], buy_mod))
        items.append(("Body Armor", 800, lambda: (setattr(g.player, "armor", PLAYER_MAX_ARMOR),
                                                  g.hud.notify("Armor equipped", UI_MONEY))))
        return items

    def _build_clothes(self):
        g = self.game
        items = []
        for i, name in enumerate(OUTFITS):
            def wear(idx=i):
                g.player.outfit = idx
                if g.police.stars > 0 and g.police.stars <= 2:
                    g.police.clear()
                    g.hud.notify("Fresh look - the heat lost your trail", UI_ACCENT2)
                g.hud.notify(f"Wearing: {OUTFITS[idx]}", UI_MONEY)
            items.append((f"Outfit: {name}", 200 + i * 300, wear))
        return items

    def _build_cars(self):
        g = self.game
        items = []
        for kind in DEALER_STOCK:
            spec = VEHICLE_SPECS[kind]
            def buy(k=kind):
                spawn = self._delivery_spot(k)
                v = make_vehicle(k, spawn)
                v.stolen = False
                g.world.vehicles.append(v)
                g.player.owned_vehicles.append(k)
                g.hud.notify(f"{VEHICLE_SPECS[k]['name']} delivered nearby", UI_MONEY)
            items.append((f"{spec['name']} [{spec['cls']}]", spec["price"], buy))
        return items

    def _delivery_spot(self, kind):
        g = self.game
        cls = VEHICLE_SPECS[kind]["cls"]
        if cls == "boat":
            return t(176, 63)                       # marina water
        if cls in ("plane", "heli"):
            return t(50, 152)                       # airport runway
        return SHOPS["cars"]["pos"] + pygame.math.Vector2(0, 80)

    def _build_property(self):
        g = self.game
        items = []
        for name, price, pos in PROPERTIES:
            if name in g.player.owned_properties:
                continue
            def buy(n=name):
                g.player.owned_properties.append(n)
                g.hud.notify(f"Purchased {n} - new save point + income", UI_MONEY)
            items.append((name, price, buy))
        if not items:
            items.append(("You own the whole portfolio.", None, lambda: None))
        return items

    def _build_spray(self):
        g = self.game

        def spray():
            v = g.player.vehicle
            if v:
                v.hp = v.max_hp
                v.burn_timer = None
            g.police.clear()
            g.hud.notify("Resprayed - clean slate", UI_ACCENT2)
        return [("Respray + full repair (clears wanted)", 500, spray)]

    def _build_export(self):
        g = self.game
        v = g.player.vehicle
        items = []
        if v:
            value = int(VEHICLE_SPECS[v.kind]["price"] * (0.25 if v.stolen else 0.6)
                        * (v.hp / v.max_hp))
            if value > 0:
                def sell():
                    g.player.exit_vehicle(g.world, g.hud)
                    v.dead = True
                    g.player.money += value
                    g.hud.notify(f"Exported {v.name} for ${value:,}", UI_MONEY)
                items.append((f"Export {v.name} (condition {int(100 * v.hp / v.max_hp)}%)",
                              -value, sell))
        if not items:
            items.append(("Drive a vehicle here to export it.", None, lambda: None))
        return items

    # ------------------------------------------------------------ input / draw
    def handle_key(self, key):
        if key in (pygame.K_ESCAPE, pygame.K_b):
            self.close()
        elif key in (pygame.K_UP, pygame.K_w):
            self.cursor = (self.cursor - 1) % len(self.items)
        elif key in (pygame.K_DOWN, pygame.K_s):
            self.cursor = (self.cursor + 1) % len(self.items)
        elif key in (pygame.K_RETURN, pygame.K_e):
            label, price, cb = self.items[self.cursor]
            p = self.game.player
            if price is None:
                cb()
            elif price < 0:                       # negative = you get paid
                cb()
                self.items = self._build_export()
                self.cursor = 0
            elif p.money >= price:
                p.money -= price
                cb()
                if self.open_shop in ("property", "export"):
                    self.open(self.open_shop)     # rebuild stock
            else:
                self.game.hud.notify("Not enough cash", UI_WARN)

    def draw(self, surf):
        if not self.running:
            return
        s = SHOPS[self.open_shop]
        h = min(560, 90 + 26 * len(self.items))
        panel = pygame.Rect(SCREEN_W // 2 - 300, 80, 600, h)
        box = pygame.Surface(panel.size, pygame.SRCALPHA)
        box.fill(UI_BG)
        pygame.draw.rect(box, s["color"], box.get_rect(), 2, border_radius=10)
        surf.blit(box, panel.topleft)
        surf.blit(self.title_font.render(s["name"], True, s["color"]), (panel.x + 20, panel.y + 12))
        money = self.font.render(f"${self.game.player.money:,}", True, UI_MONEY)
        surf.blit(money, (panel.right - money.get_width() - 20, panel.y + 16))
        y = panel.y + 52
        first = max(0, self.cursor - 15)
        for i, (label, price, _) in enumerate(self.items[first:first + 17]):
            idx = first + i
            sel = idx == self.cursor
            color = UI_ACCENT2 if sel else UI_TEXT
            prefix = "> " if sel else "   "
            ptxt = "" if price is None else (f"  +${-price:,}" if price < 0 else f"  ${price:,}")
            surf.blit(self.font.render(prefix + label + ptxt, True, color), (panel.x + 20, y))
            y += 26
        hint = self.font.render("W/S select · ENTER buy · ESC close", True, (160, 160, 170))
        surf.blit(hint, (panel.x + 20, panel.bottom - 28))


def property_income(player):
    """Daily income from owned properties (called once per in-game day)."""
    return 800 * len(player.owned_properties)
