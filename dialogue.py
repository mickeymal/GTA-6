"""Branching dialogue: node trees with choices that set reputation, flags and
story branches. Rendered as a bottom panel; choices picked with number keys."""
import pygame
from settings import *

# A dialogue is {node_id: {"speaker","text","choices":[(label, next_id, effects)]}}
# effects: dict(rep=+n, flag="x", money=+n)  |  next_id None ends the dialogue.

DIALOGUES = {
    "intro": {
        "start": dict(speaker="Rico", text="So you're the new blood. Vice Bay eats people like you for breakfast.",
                      choices=[("I eat back.", "tough", dict(rep=2)),
                               ("Just show me the ropes.", "humble", dict(rep=1)),
                               ("Whatever, where's the money?", "greedy", dict(flag="greedy"))]),
        "tough": dict(speaker="Rico", text="Ha! I like it. Get to the safehouse in Little Haiti. Try not to die.",
                      choices=[("On my way.", None, {})]),
        "humble": dict(speaker="Rico", text="Smart. Humble lives longer here. Safehouse is in Little Haiti — move.",
                       choices=[("Got it.", None, {})]),
        "greedy": dict(speaker="Rico", text="Money comes to those who survive. Safehouse. Little Haiti. Now.",
                       choices=[("Fine.", None, {})]),
    },
    "repo": {
        "start": dict(speaker="Rico", text="A Banshee GT is parked downtown. The owner missed payments... on a loan he never took.",
                      choices=[("So it's a robbery.", "rob", dict(rep=1)),
                               ("I don't ask questions.", "quiet", dict(rep=2))]),
        "rob": dict(speaker="Rico", text="It's a *repossession*, kid. Semantics keep lawyers rich. Bring it to the docks garage, clean.",
                    choices=[("Consider it repossessed.", None, {})]),
        "quiet": dict(speaker="Rico", text="That attitude will make you rich. Docks garage. No scratches, no cops.",
                      choices=[("Done.", None, {})]),
    },
    "chop": {
        "start": dict(speaker="Marisol", text="Cartel dealers moved into Little Haiti. My streets. You clear them out, I owe you.",
                      choices=[("Loud or quiet?", "loud", {}),
                               ("What do I get?", "pay", dict(flag="greedy"))]),
        "loud": dict(speaker="Marisol", text="Loud sends a message. Three dealers, corner of the market. Erase them.",
                     choices=[("They're gone.", None, dict(rep=1))]),
        "pay": dict(speaker="Marisol", text="$5,000 and my respect. In Vice Bay the second is worth more. Three dealers. Market corner.",
                    choices=[("Deal.", None, {})]),
    },
    "ocean": {
        "start": dict(speaker="Rico", text="Package floating in the bay — don't ask what's in it. Grab a boat from the marina, quick before the coast guard does.",
                      choices=[("What's in the package?", "asked", {}),
                               ("Already gone.", None, dict(rep=1))]),
        "asked": dict(speaker="Rico", text="...I said don't ask. Marina. Boat. Package. Docks. Clock's ticking.",
                      choices=[("Fine, fine.", None, {})]),
    },
    "coop": {
        "start": dict(speaker="Marisol", text="There's a stunt plane fueled up at the airport. I need a pilot to fly a... survey route.",
                      choices=[("I can fly.", "fly", dict(rep=1)),
                               ("A 'survey' route. Sure.", "wink", {})]),
        "fly": dict(speaker="Marisol", text="Hit every checkpoint and land it back on the runway in one piece.",
                    choices=[("Easy money.", None, {})]),
        "wink": dict(speaker="Marisol", text="Photos of shipping lanes sell well. Checkpoints, then land on the runway. Don't stall.",
                     choices=[("Copy.", None, {})]),
    },
    "skies": {
        "start": dict(speaker="Rico", text="A snitch is escaping by speedboat. There's a Sparrow on the downtown helipad. You see where this is going.",
                      choices=[("Boats can't outrun rotors.", None, dict(rep=1)),
                               ("How much?", "pay", dict(flag="greedy"))]),
        "pay": dict(speaker="Rico", text="Fifteen grand and the city's gratitude. Sink that boat before it leaves the bay.",
                    choices=[("Airborne.", None, {})]),
    },
    "bank": {
        "start": dict(speaker="Marisol", text="The Vice National downtown. Vault's being moved tonight — skeleton security. One-time window.",
                      choices=[("A heist? I'm in.", "in", dict(rep=2)),
                               ("This is insane.", "sane", {})]),
        "in": dict(speaker="Marisol", text="Get in, grab the cash cart, get out. The response will be... enthusiastic. Have an exit plan.",
                   choices=[("Born ready.", None, {})]),
        "sane": dict(speaker="Marisol", text="Insane pays triple. Grab the cash, survive the heat, lose the cops. Simple.",
                     choices=[("...I'm in.", None, {})]),
    },
    "convoy": {
        "start": dict(speaker="Rico", text="Cartel's moving product in three armored trucks on the highway. Marisol wants them gone. Permanently.",
                      choices=[("I'll need heavy hardware.", "rpg", {}),
                               ("Three trucks? Easy.", None, dict(rep=1))]),
        "rpg": dict(speaker="Rico", text="There's an RPG waiting by the road. Trucks won't stop for traffic lights — you have four minutes.",
                    choices=[("Boom.", None, {})]),
    },
    "setup": {
        "start": dict(speaker="Marisol", text="Rico sold us out. Cops knew about the bank. He meets his handler tonight at the marina.",
                      choices=[("Rico would never.", "doubt", {}),
                               ("I'll handle it.", "loyal", dict(rep=1))]),
        "doubt": dict(speaker="Marisol", text="Then go see for yourself. Marina. Tonight. Decide whose side you're on.",
                      choices=[("Fine.", None, {})]),
        "loyal": dict(speaker="Marisol", text="Good. Whatever you find there... make the right call.",
                      choices=[("Understood.", None, {})]),
        "confront": dict(speaker="Rico", text="Kid! It's not what it looks like. Marisol's setting US both up. Help me and we split her empire.",
                         choices=[("Sorry, Rico. Business.", "kill_rico", dict(flag="sided_marisol")),
                                  ("Let's take her down.", "join_rico", dict(flag="sided_rico", rep=-1))]),
        "kill_rico": dict(speaker="Rico", text="...should've known. Vice Bay eats everyone in the end.",
                          choices=[("Nothing personal.", None, {})]),
        "join_rico": dict(speaker="Rico", text="Ha! Now we bury the queen. Her mansion, tomorrow. Bring everything you have.",
                          choices=[("See you there.", None, {})]),
    },
    "kingpin": {
        "start": dict(speaker="???", text="This is it. The mansion on the north shore. Everything ends tonight.",
                      choices=[("Let's finish this.", None, dict(rep=1))]),
    },
    "shop_clothes": {
        "start": dict(speaker="Clerk", text="Welcome to Vice Threads! New look, new life — cops barely recognize a fresh outfit.",
                      choices=[("Just browsing.", None, {})]),
    },
}


class DialogueManager:
    def __init__(self, game):
        self.game = game
        self.active = None          # (tree_id, node_id)
        self.on_finish = None
        self.font = pygame.font.SysFont("verdana", 17)
        self.name_font = pygame.font.SysFont("verdana", 15, bold=True)

    @property
    def running(self):
        return self.active is not None

    def start(self, tree_id, node_id="start", on_finish=None):
        if tree_id in DIALOGUES and node_id in DIALOGUES[tree_id]:
            self.active = (tree_id, node_id)
            self.on_finish = on_finish

    def choose(self, index):
        if not self.active:
            return
        tree_id, node_id = self.active
        node = DIALOGUES[tree_id][node_id]
        if index >= len(node["choices"]):
            return
        label, next_id, effects = node["choices"][index]
        p = self.game.player
        p.reputation += effects.get("rep", 0)
        if "flag" in effects:
            p.flags.add(effects["flag"])
        if "money" in effects:
            p.money += effects["money"]
        if next_id:
            self.active = (tree_id, next_id)
        else:
            self.active = None
            cb, self.on_finish = self.on_finish, None
            if cb:
                cb()

    def handle_key(self, key):
        if key in (pygame.K_1, pygame.K_KP1):
            self.choose(0)
        elif key in (pygame.K_2, pygame.K_KP2):
            self.choose(1)
        elif key in (pygame.K_3, pygame.K_KP3):
            self.choose(2)

    def draw(self, surf):
        if not self.active:
            return
        tree_id, node_id = self.active
        node = DIALOGUES[tree_id][node_id]
        h = 130 + 24 * len(node["choices"])
        panel = pygame.Rect(SCREEN_W // 2 - 380, SCREEN_H - h - 24, 760, h)
        box = pygame.Surface(panel.size, pygame.SRCALPHA)
        box.fill(UI_BG)
        pygame.draw.rect(box, UI_ACCENT, box.get_rect(), 2, border_radius=8)
        surf.blit(box, panel.topleft)
        surf.blit(self.name_font.render(node["speaker"], True, UI_ACCENT),
                  (panel.x + 16, panel.y + 12))
        # word-wrap text
        words = node["text"].split()
        lines, cur = [], ""
        for w in words:
            test = (cur + " " + w).strip()
            if self.font.size(test)[0] > panel.w - 32:
                lines.append(cur)
                cur = w
            else:
                cur = test
        lines.append(cur)
        y = panel.y + 36
        for line in lines:
            surf.blit(self.font.render(line, True, UI_TEXT), (panel.x + 16, y))
            y += 22
        y += 8
        for i, (label, _, _) in enumerate(node["choices"]):
            surf.blit(self.font.render(f"[{i + 1}] {label}", True, UI_ACCENT2), (panel.x + 28, y))
            y += 24
