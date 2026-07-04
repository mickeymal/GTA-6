# Vice Bay Stories (Python / Pygame)

A GTA-style open-world crime game set in a neon-soaked, Miami-inspired Vice Bay.
Built in Python with Pygame — fully procedural graphics, no external assets required.

## Setup & Run

```bash
pip install pygame
python main.py
```

Requires Python 3.9+ and Pygame 2.x. Runs at 1280x720, 60 FPS.

## Features

- **Open world** — a 6144x6144 px seamless city with six districts: neon Downtown,
  Ocean Beach, Little Haiti, the Suburbs, the Port docks, and Vice International Airport.
  Land, sea and air traversal; dynamic traffic, pedestrians, gangs and police.
- **Day/night cycle & dynamic weather** — clear, rain (with wet-road reflections),
  fog, and storms with lightning. Neon signs glow through the night.
- **Vehicles** — sports cars, muscle cars, sedans, SUVs, trucks, police cruisers,
  military jeeps, motorcycles, bicycles, speedboats, jet skis, yachts, fishing boats,
  stunt planes, private jets and helicopters. Per-class physics: drifting with the
  handbrake, plane takeoff/stall/landing, helicopter rotor spin-up, boat wash.
  Visual + performance damage; wrecks burn, then explode.
- **Weapons** — fists, bat, knife, machete, two pistols, two SMGs, two rifles,
  shotgun, sniper (bullet drop), RPG, grenades and molotovs. Weapon wheel (hold TAB),
  recoil, reload, ADS zoom, and gun-shop attachments: scope, suppressor, extended mag.
- **Wanted system** — 5 stars of escalation: foot patrols → cruiser pursuits →
  roadblocks + SWAT → police helicopter → military. Evade line-of-sight to cool off,
  or hit the Pay'n'Spray.
- **10 story missions** — repo jobs, gang hits, sea smuggling, a flight-school
  survey run, a helicopter boat-chase, a bank heist, an RPG convoy ambush, and a
  branching betrayal arc that changes the finale.
- **Branching dialogue** — choices affect reputation, story flags, and which
  kingpin you face at the end.
- **Economy** — earn from missions, activities and property income; buy weapons,
  outfits, vehicles and safehouses; export stolen cars at the docks.
- **Side activities** — street, boat and air races vs. rivals; vigilante missions
  in any police vehicle; Ammu-Nation shooting range.
- **Save/Load** — JSON saves (F5/F9 or via the pause menu).

## Controls

| Context | Keys |
|---|---|
| On foot | `WASD` move, `SHIFT` sprint, mouse aim, `LMB` shoot, `RMB` ADS, `R` reload |
| Weapons | hold `TAB` weapon wheel, mouse-wheel cycle |
| Vehicles | `E`/`F` enter/exit, `W/S` gas/brake, `A/D` steer, `SPACE` handbrake |
| Aircraft | `SPACE` climb, `CTRL`/`C` descend — planes need runway speed to lift off |
| Police vehicles | `SHIFT` siren, `V` start vigilante |
| World | `ENTER` start mission/race at a marker, `B`/`E` shop, `M` map |
| System | `F1` controls overlay, `F5` save, `F9` load, `ESC` pause |

Drive-bys work with pistols/SMGs from cars and bikes; anything goes from a boat.

## Getting started in-game

1. Head to the pulsing **pink marker** (you start next to it) and press `ENTER`.
2. Rico's intro mission unlocks the Little Haiti safehouse.
3. Cyan rings are races, colored dots on the minimap are shops. The yellow marker
   is your current objective.

## Code layout

| File | Contents |
|---|---|
| `main.py` | Game class, state machine, ambient traffic/ped spawning, explosions |
| `settings.py` | All tunables: screen, map, colors, physics constants |
| `world.py` | Procedural city generation, day/night, weather, lighting, minimap |
| `camera.py` | Smooth-follow camera, zoom, screen shake |
| `player.py` | On-foot movement, swimming, cover, inventory, vehicle enter/exit |
| `vehicle.py` | `Vehicle` base + `Car`, `Motorcycle`, `Boat`, `Plane`, `Helicopter` |
| `weapons.py` | Weapon defs, projectiles, thrown explosives, fire patches, attachments |
| `npc.py` | Pedestrians, gang members, cops on foot, traffic AI |
| `police.py` | Wanted stars, pursuit drivers, heli pilots, roadblocks |
| `missions.py` | Objective engine + the 10 story missions |
| `dialogue.py` | Branching dialogue trees + renderer |
| `economy.py` | Shops, properties, Pay'n'Spray, vehicle export |
| `activities.py` | Races, vigilante, shooting range |
| `hud.py` | Minimap, vitals, wanted stars, weapon wheel, big map |
| `particles.py` | Explosions, muzzle flash, smoke, rain, blood, sparks |
| `savegame.py` | JSON save/load |
| `assets/` | Placeholder structure for swapping in real art/audio (see its README) |

## Testing

```bash
python smoke_test.py   # headless: simulates gameplay, missions, combat, save/load
```

The smoke test boots the game with a dummy video driver and drives world generation,
all 18 vehicle classes, all 15 weapons, explosions, combat, the wanted system,
mission flow, weather, and a save/load round-trip — 40 assertions in total.
