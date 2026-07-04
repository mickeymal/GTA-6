# Assets

The game is fully playable with **zero external assets** — every sprite, tile and
effect is drawn procedurally. This folder is the drop-in point for upgrading the
look and sound. If a file listed below exists, wire it up in the matching module
(each section notes the hook point).

## Folder structure

```
assets/
├── sprites/
│   ├── player/          # player.py -> Player.draw
│   │   ├── ped_walk_{0..7}.png      32x32, 8-frame walk cycle, top-down
│   │   ├── ped_shoot.png            32x32, two-hand aim pose
│   │   └── ped_swim.png             32x32
│   ├── vehicles/        # vehicle.py -> Vehicle._draw_body
│   │   ├── sports.png               88x44 top-down sports car, neutral gray for tinting
│   │   ├── muscle.png / sedan.png / suv.png / truck.png / police.png
│   │   ├── motorcycle.png bicycle.png
│   │   ├── speedboat.png jetski.png yacht.png fishing.png
│   │   ├── stunt_plane.png jet.png   with separate prop_blur.png overlay
│   │   └── helicopter.png + rotor.png (128x8 blade strip)
│   ├── npcs/            # npc.py -> NPC.draw
│   │   ├── ped_{a..f}.png           32x32 civilian variants
│   │   ├── gang_haitian.png gang_cartel.png cop.png swat.png
│   ├── weapons/         # hud.py weapon wheel icons, 48x48 each
│   │   └── icon_{pistol,smg,rifle,shotgun,sniper,rpg,grenade,molotov,melee}.png
│   └── fx/              # particles.py
│       ├── explosion_{0..7}.png     96x96 frame anim
│       ├── muzzle_flash.png smoke.png fire.png splash.png
├── tiles/               # world.py -> World.draw
│   ├── road_{straight,corner,t,cross}.png   32x32
│   ├── sidewalk.png sand.png grass.png water_{0..3}.png (animated)
│   ├── building_{downtown,beach,haiti,port}_{0..3}.png  rooftops, 32x32
│   ├── runway.png dock.png palm.png
│   └── neon_signs/{0..9}.png        additive-blend glow sprites, 64x32
├── ui/                  # hud.py
│   ├── minimap_frame.png star.png crosshair.png
│   └── font/vice.ttf                display font (fallback: verdana)
├── audio/               # add a sound.py mixer wrapper; hook points listed below
│   ├── sfx/
│   │   ├── gun_{pistol,smg,rifle,shotgun,sniper,rpg}.ogg   weapons.py fire()
│   │   ├── reload.ogg explosion.ogg glass.ogg
│   │   ├── engine_{car,bike,boat,plane,heli}_loop.ogg      vehicle.py update()
│   │   ├── skid.ogg crash.ogg horn.ogg siren_loop.ogg      police.py
│   │   ├── rain_loop.ogg thunder.ogg ocean_loop.ogg        world.py weather
│   │   └── ui_{move,select,buy}.ogg cash.ogg
│   └── music/
│       ├── menu_synthwave.ogg                              main.py menu
│       ├── radio_{pop,rock,latin,synth}.ogg                per-vehicle radio
│       └── mission_tension.ogg wasted.ogg
└── README.md            # this file
```

## Placeholder descriptions (art direction)

- **Palette**: hot pink `#FF3CB4`, cyan `#3CDCFF`, violet, sunset orange on deep
  navy nights — 80s-Miami synthwave. Daytime: washed pastels, strong sand/teal.
- **Vehicles**: clean top-down silhouettes with baked ambient occlusion; leave the
  body panels near-white so code can tint per `VEHICLE_SPECS[...]["colors"]`.
- **Buildings**: rooftop view w/ AC units, pools on beach hotels, container stacks
  in the port. Downtown rooftops get neon edge trims (paired sign sprite).
- **Water**: 4-frame loop, subtle foam at shorelines; docks cast short shadows.
- **FX**: explosion frames orange-white core to black smoke; muzzle flash is a
  3-frame star; rain is handled in code (streak overlay) — no sprite needed.
