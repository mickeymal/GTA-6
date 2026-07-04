# Vice Bay — GTA-style Crime Games

This repository contains **two** GTA-inspired open-world crime game projects set in
neon-lit, Miami-inspired Vice Bay:

## 1. Vice Bay Stories — Python / Pygame  *(root, fully playable)*

A complete, **runnable** top-down 2D open-world crime game. Land/sea/air vehicles,
a full weapon arsenal + weapon wheel, a 5-star wanted system, 10 branching story
missions, dialogue, an economy with shops/properties, side activities, day/night +
dynamic weather, and save/load — all with procedurally-drawn graphics (no assets
required).

```bash
pip install pygame
python main.py          # play
python smoke_test.py    # headless self-test (verifies all systems)
```

Full details, controls, and code layout: **[README_ViceBayStories.md](README_ViceBayStories.md)**.
The code lives in the repo root (`main.py`, `world.py`, `vehicle.py`, `weapons.py`,
`missions.py`, …) and the `assets/` folder documents the art placeholders.

## 2. Vice Bay Empire — Unity / C#  *(`Vice-Bay-Empire-Unity/`, code architecture)*

A 3D open-world crime **sandbox architecture** for Unity 2022.3+, focused on
GTA V-level *post-story* depth: a **Dark Web marketplace** (crypto, scams, fraud
tools), **fraud & cyber-crime** (skimming, phishing, counterfeiting), **interactive
robberies** (teller hold-ups with compliance/alarm/vault), a **business empire**,
**property & yacht** ownership, and endgame systems (stock market with manipulation,
gang territory wars, sports minigames, hitman contracts).

This is clean, drop-in C# (event bus, ScriptableObject data, managers, save system)
that you wire to your own city/character/vehicle art in the Unity Editor — it is a
codebase to build on, not a pre-built binary.

See **[Vice-Bay-Empire-Unity/README.md](Vice-Bay-Empire-Unity/README.md)** for the
full architecture, folder map, scene setup, and asset recommendations.

---

### Which one runs right now?

The **Pygame** project (root) is immediately playable and passes a 40-check headless
smoke test. The **Unity** project requires the Unity Editor and your own 3D assets to
build and play.
