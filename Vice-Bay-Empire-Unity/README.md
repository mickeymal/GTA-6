# Vice Bay Empire (Unity)

A GTA-style 3D open-world crime sandbox for **Unity 2022.3 LTS+**, focused on
GTA V-level *post-story* depth — a Dark Web marketplace, fraud & cyber-crime,
a business empire, property & yacht ownership, interactive robberies, and
endgame systems (stock market, gang wars, sports, contracts).

> **This build runs immediately — no scene wiring, no art, no data assets.**
> The whole game **generates itself at runtime** from primitives (capsules for people,
> boxes for cars/buildings) and **procedurally-synthesized audio**, so you can walk,
> drive, shoot, rob a bank, buy on the Dark Web, and manage a business empire the
> moment you press Play. The detailed manager/ScriptableObject architecture is still
> here underneath; the runtime bootstrap just populates it in code so nothing needs
> the Editor. Swap in real models/audio later by pointing the factories at prefabs.
>
> A separate, **fully runnable 2D version** lives in the repo root
> (`Vice Bay Stories`, Python/Pygame) and is verified by an automated smoke test.

## ▶ Play in 30 seconds

1. Create a new **3D** project in Unity **2022.3 LTS+** (Built-in or URP — both work).
2. Copy this project's `Assets/` folder into your project's `Assets/`.
3. **Edit ▸ Project Settings ▸ Player ▸ Active Input Handling** → set to **Both**
   (or *Input Manager (Old)*). The scripts use the legacy `Input` API.
4. From the menu bar, click **ViceBay ▸ Create ViceBay_MainScene**. This generates
   `Assets/Scenes/ViceBay_MainScene.unity` with a single `GameBootstrap` object and
   adds it to Build Settings.
5. Press **Play**. The entire Vice Bay map builds itself.

That's the whole setup. (Prefer to do it by hand? Make an empty scene, add an empty
GameObject, add the **`GameBootstrap`** component, press Play — same result.)

> No URP required, no NavMesh bake, no prefabs, no fonts. If you use URP and things
> look pink, the materials just didn't find the Lit shader — `MaterialFactory` falls
> back automatically, but re-import once and it resolves.

## The map — a small, dense Vice City

`ViceBay_MainScene` builds a compact Miami laid out on a shared road grid
(`CityLayout`), so every district is a short drive apart:

```
                     ~~~~~~~~  BAY / HARBOR (boats, yacht)  ~~~~~~~~
   [ SLUMS /            piers                         Downtown towers   ] O
   [ INDUSTRIAL ]   ============ road grid ============  (neon)         ] C
   [ warehouses ]        bank        penthouse                          ] E
   [ tanks      ]        store                            Beach Mansion ] A
   [------------ AIRPORT runway + hangar ------------]    palms + sand  ] N
```

- **Downtown** — tall neon towers, the penthouse, the robbable **bank**, an ATM and gun shop.
- **Slums / Industrial** (west) — low drab blocks, warehouses, storage tanks, the
  robbable **convenience store**, the starter **safehouse**.
- **Beach** (east coast) — sand promenade, palms, the **mansion**, swimmable **ocean**.
- **Bay / Harbor** (north) — piers, boats/jet ski, the buyable **yacht**, swimmable water.
- **Airport** (south) — a long runway with markings, hangar and control tower; the
  plane and helicopter spawn here.
- **Roads** connect all of it, with **AI traffic** cruising the grid (hop into a moving
  car with `F`).

### Robbable interiors
The **bank** and **store** are real enterable rooms (four walls, a doorway, a lit
interior). Walk in, aim (`RMB`) at the teller, press `E` to hold up, `E` again to
demand — comply/resist/alarm play out, and the bank opens a **vault** you must reach.

### Buyable properties (glowing gold doors)
Safehouse (free, your start home), Downtown Penthouse, Beach Mansion, and a Bay Yacht.
Walk to the door and press `E` to buy; press `E` when owned to set it as your home/
save point.

### Ambient 3D audio
Positional loops are planted in the world: **city hum** downtown, **waves** at the
ocean and bay, **wind** at the airport — all synthesized, no files.

## Controls

| Action | Keys |
|---|---|
| Move / sprint / jump-climb | `WASD` / `Shift` / `Space` |
| Look | Mouse · scroll to zoom |
| Shoot / aim / reload | `LMB` / `RMB` / `R` |
| Weapon wheel / cycle | hold `Tab` (click a slot) · `Alt`+scroll |
| Enter / exit vehicle | `F` |
| Interact — rob, mug, carjack, ATM | `E` (aim a weapon first to rob/mug) |
| Phone (Dark Web, business, property, fraud, stocks, save) | `P` |
| Swim | walk into the bay; `Space`/`Ctrl` up/down |

**Try this first:** grab the red car (`F`), drive to the **Vice National Bank**,
get out, aim (`RMB`) at the teller and press `E`, press `E` again to demand cash,
then run as the stars rise. Press `P` to open the Dark Web and buy a skimmer kit.

## What builds at runtime

`GameBootstrap.Awake()` assembles everything:
- **Managers** — GameManager, EconomyManager (cash + ViceCoin), WorldClock
  (day/night + weather), WantedSystem, DarkWebMarketplace, FraudCenter, ContractBoard,
  BusinessManager, PropertyManager, StockMarket, SaveCoordinator, PoliceResponse.
- **City** (`CityBuilder`) — land, a swimmable water bay, a road grid, neon buildings,
  a robbable **bank** (teller + vault) and **store**, an **ATM** (skimmer target), a
  gun-shop marker, and an airstrip.
- **Player** — capsule with third-person camera, movement (sprint/jump/vault/swim),
  shooting, and interaction; starts with a pistol + SMG.
- **Vehicles** (`VehicleFactory`) — car, sedan, muscle, speedboat, stunt plane,
  helicopter, plus a carjackable taxi, all drivable with class-specific physics.
- **NPCs** — wandering civilians/gang that flee, panic and call cops; police that
  spawn and chase as your wanted level climbs.
- **Audio** (`ProceduralAudio` + `AudioManager`) — synthesized gunfire, engines,
  explosions, footsteps, UI blips, sirens, city ambience, rain, and dialogue voice
  blips. Zero audio files required.
- **UI** — IMGUI HUD (health, cash, crypto, wanted stars, weapon, minimap, prompts,
  notifications) and the phone menu hub.

## Audio & dialogue integration

- **Audio.** All SFX are generated by `ProceduralAudio` and played through
  `AudioManager`. To use real clips instead, assign `.wav/.ogg` files to the override
  fields on the `AudioManager` component (e.g. `gunshotOverride`) — non-null wins over
  the synth. Engine loops are pitched by speed; ambience/rain react to weather.
- **Dialogue.** `DialogueSystem` supports quick **barks** (`Say(speaker, line, pitch)`
  — used by tellers/NPCs during robberies) and full **branching trees** (`StartTree`)
  rendered with number-key/click choices that apply reputation, flags, money and
  callbacks. Each line logs a `[VOICE:speaker] "line"` placeholder and plays a
  synthesized voice blip — drop a real `AudioClip` per line by extending
  `DialogueSystem.SpeakCurrent()` to look the line up in a voice table.

## Editor-integration path (optional, for a "real" build)

The runtime build is self-contained, but the original inspector-driven scripts are
still here for when you move to authored content and 3D art:

## Architecture

Clean, decoupled, data-driven:

- **Event bus** (`Core/GameEvents.cs`) — systems raise/subscribe to static C# events
  (`MoneyChanged`, `CrimeCommitted`, `WantedChanged`, `NewDay`, …) instead of
  referencing each other. The HUD and managers just listen.
- **Singlethreaded managers** — `GameManager` (root state), `EconomyManager`
  (cash + ViceCoin + laundering), `WorldClock` (day/night + weather), `WantedSystem`
  (5-star land/sea/air response + separate federal-attention track).
- **ScriptableObject data** — every weapon, vehicle, dark-web listing, business,
  property and drug is an authored asset, so designers add content without code.
- **Save system** — `SaveData` snapshot serialized to JSON via `SaveSystem`;
  `SaveCoordinator` gathers/restores across all managers.

### Folder structure

```
Assets/
├── Scripts/
│   ├── Core/         GameEvents, GameManager, EconomyManager, WorldClock,
│   │                 WantedSystem, SaveSystem, SaveCoordinator, SceneBootstrap
│   ├── Data/         WeaponData, VehicleData, DarkWebItemData, DrugData,
│   │                 BusinessData, PropertyData  (ScriptableObjects)
│   ├── Player/       PlayerController (walk/sprint/vault/swim/climb/ragdoll),
│   │                 PlayerHealth (+IDamageable), PlayerLoadout, PlayerCombat
│   ├── Vehicles/     VehicleController (car/bike/boat/plane/heli physics),
│   │                 VehicleInteractor (enter/exit, drive-by)
│   ├── Weapons/      Explosions (radial damage helper)
│   ├── Crime/        InteractionManager (+IInteractable), RobberyTarget
│   │                 (store/bank teller interaction), StreetCrime (mug/carjack),
│   │                 NPCReaction (flee/panic/call-police)
│   ├── DarkWeb/      DarkWebMarketplace (crypto buys, scams, federal heat),
│   │                 FraudCenter (skimming/phishing/identity/counterfeit),
│   │                 ContractBoard (hitman contracts)
│   ├── Business/     BusinessInstance, BusinessManager (income, staff, raids)
│   ├── Property/     PropertyManager (real estate, yachts, garages, respawn)
│   ├── Endgame/      StockMarket (+manipulation), SportsChallenge (golf/jetski/
│   │                 parachute samples), GangTerritory (turf wars)
│   ├── World/        WorldInteractables (ATM, DarkWebTerminal, ShopVendor,
│   │                 BusinessDesk, PropertyDoor)
│   └── UI/           HUDManager, WeaponWheelUI, MainMenu
├── Prefabs/          (your player, vehicles, police, NPCs, VFX prefabs)
├── ScriptableObjects/(authored .asset data)
├── Scenes/           MainMenu, ViceBay (the city)
├── Art/  Audio/       (your models, textures, sounds)
```

## Priority systems (as requested)

### Dark Web marketplace — `DarkWeb/DarkWebMarketplace.cs`
Reached from a laptop/phone (`DarkWebTerminal`). Everything is paid in **ViceCoin**.
Categories: weapons, drugs, fraud tools, services, counterfeit, **data** (card
dumps / bank creds that pay out cash), and **contracts**. Buying illicit goods
raises **federal attention**; physical goods can be **intercepted** (street heat).
Some vendors are **scammers** — low trust + low marketplace reputation = higher
odds you pay and get nothing. Successful buys build reputation, cutting scam risk
and unlocking higher-tier listings.

### Fraud & cyber-crime — `DarkWeb/FraudCenter.cs`
Tools bought on the Dark Web unlock activities: **ATM skimming** (install → harvest
over time), **phishing campaigns** (quality-based payout), **identity/account
draining**, and **counterfeit printing** (quality → passable %). Each pays dirty
money and raises federal attention; some fail and spike heat.

### Interactive robbery — `Crime/RobberyTarget.cs`
Aim a weapon at a teller and press interact to start a hold-up. Fire warning shots
to raise the teller's **fear** and improve compliance. Brave tellers **resist** and
trip a **silent alarm**. On compliance you get the register; **banks** additionally
open a **vault** you must reach before the escalating police response boxes you in.
Walking away mid-hold-up fails the job and calls the cops. Wired through
`InteractionManager` (which knows whether a weapon is aimed) and `PlayerCombat`
(warning-shot detection).

### Business empire — `Business/BusinessManager.cs`
Buy legal fronts (nightclub, arcade, gun shop, dealership) and illicit ops (drug
lab, counterfeit factory, smuggling warehouse, chop shop, hack farm). **Legal**
businesses pay steady safe income each in-game day; **illicit** ones produce goods
you stockpile and must run to a buyer (bigger payout, but sale runs add heat and
ops get **raided**). Manage upgrades, staff wages, and resupply. Daily ticks are
driven by `WorldClock`'s `NewDay` event.

### Property & yachts — `Property/PropertyManager.cs`
Safehouses, apartments, penthouses, mansions, garages and **yachts**. Properties
are save points, wardrobes, garages, and (yachts/mansions) weapon storage / party
venues with passive income. Daily upkeep vs. income is auto-reconciled; the active
home is your respawn point.

## Scene setup (minimum to press Play)

1. **Bootstrap** GameObject with `GameManager` (+ child `EconomyManager`, `WorldClock`).
2. Managers: `WantedSystem`, `DarkWebMarketplace`, `FraudCenter`, `ContractBoard`,
   `BusinessManager`, `PropertyManager`, `StockMarket`, `GangTerritory`,
   `SaveCoordinator`, `SceneBootstrap` (assign starting pistol + explosion VFX).
3. **Player** prefab with `CharacterController`, `PlayerController`, `PlayerHealth`,
   `PlayerLoadout`, `PlayerCombat`, `VehicleInteractor`, tag `Player`; a follow camera.
4. **HUD** Canvas with `HUDManager` (assign bars/labels), `WeaponWheelUI`.
5. `InteractionManager` on the player (assign the camera).
6. Bake a **NavMesh** so pedestrians (`NPCReaction`) can path.
7. Place world interactables (`ATMInteractable`, `DarkWebTerminal`, `RobberyTarget`
   on store counters, `PropertyDoor`, `BusinessDesk`) around the city.

## Content to author (starter set)

- **Weapons**: Fists, Bat, Pistol, Combat Pistol, SMG, Assault Rifle, Pump Shotgun,
  Sniper (`bulletDrop = true`), RPG (`isExplosive = true`), Grenade.
- **Vehicles**: a sports car, muscle, sedan, SUV, motorcycle, speedboat, jet ski,
  yacht, stunt plane, helicopter (set class, top speed, `liftSpeed`/`stallSpeed`).
- **Dark Web**: skimmer kit, phishing kit, cred stuffer, counterfeit plates (FraudTool);
  card dump / bank creds (Data → cashPayout); a couple of weapons and a hit contract.
- **Businesses**: Nightclub (legal), Drug Lab + Counterfeit Factory + Hack Farm (illicit).
- **Properties**: a starter safehouse, a penthouse, a mansion, and a mega-yacht.

## Controls (default bindings)

| Context | Keys |
|---|---|
| Move / sprint | `WASD`, `Shift` |
| Jump / vault / swim up | `Space` (dive down `Ctrl` while swimming) |
| Aim / fire / reload | `RMB` / `LMB` / `R` |
| Weapon wheel | hold `Tab` (mouse to pick, scroll to cycle) |
| Enter/exit vehicle | `F` |
| Interact (rob, buy, use ATM, open laptop) | `E` |
| Pause | `Esc` |

## Asset recommendations

Free/paid packs that slot straight into these scripts (all optional):
- **City**: Unity *Windridge City* (free), or Synty *POLYGON City / Heist* for a
  stylized Miami. Any drivable-scale mesh with a baked NavMesh works.
- **Player**: Mixamo character + animations (walk/run/aim/vault/swim/ragdoll).
- **Vehicles**: Synty POLYGON vehicles, or any prefab with a `Rigidbody`; add
  `VehicleController`, set `driverSeat`/`exitPoint`, and a `VehicleData` asset.
- **Weapons/VFX**: Synty weapons; Unity Particle Pack for muzzle flash/explosions
  (assign to `Explosions.ExplosionVfxPrefab` and `PlayerCombat`).
- **Audio**: any gunshot/engine/siren/rain SFX; UI clicks and a synthwave menu track.

## Honest scope note

A shipping AAA open-world game is hundreds of person-years; this delivers the
**systems and architecture** for one, with the crime/economy/endgame loops the
brief prioritized implemented in real, readable C#. Combat/vehicle feel, full mission
scripting, netcode, and polished UI are left as clearly-marked extension points.
```
