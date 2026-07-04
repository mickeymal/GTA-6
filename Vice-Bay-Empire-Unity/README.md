# Vice Bay Empire (Unity)

A GTA-style 3D open-world crime sandbox for **Unity 2022.3 LTS+**, focused on
GTA V-level *post-story* depth — a Dark Web marketplace, fraud & cyber-crime,
a business empire, property & yacht ownership, interactive robberies, and
endgame systems (stock market, gang wars, sports, contracts).

> **What this repository is.** This is a complete, clean **code architecture** and
> a set of **working gameplay systems** you drop into a Unity project — managers,
> ScriptableObject data, event bus, save system, and the prioritized crime/economy
> systems. It is *not* a pre-built binary or a repo full of 3D art: you supply the
> city mesh, character models, and vehicle prefabs (see **Assets & placeholders**),
> then wire the scripts to them in the Editor. Everything here is designed to
> compile as one assembly and run once the scene references are hooked up.
>
> A separate, **fully runnable 2D version** of this concept lives in the repo root
> (`Vice Bay Stories`, Python/Pygame) if you want something playable immediately.

## Requirements

- Unity **2022.3 LTS** or newer (URP or HDRP; scripts are render-pipeline agnostic).
- Packages: **AI Navigation** (NavMesh, used by `NPCReaction`), **TextMeshPro** or
  uGUI (HUD uses uGUI `Text`/`Image` — swap for TMP if preferred), **Input System**
  optional (scripts use the legacy `Input` API for portability).

## Setup

1. Create a new 3D (URP) project in Unity 2022.3+.
2. Copy the `Assets/` folder from here over your project's `Assets/`.
3. Install **AI Navigation** via Package Manager (for pedestrian NavMesh).
4. Build the data assets: right-click in the Project window →
   `Create ▸ ViceBay ▸ …` to author `WeaponData`, `VehicleData`, `DarkWebItemData`,
   `BusinessData`, `PropertyData`, `DrugData` assets. A recommended starter set is
   listed in **Content to author** below.
5. Open a scene, add the **Bootstrap** hierarchy (see **Scene setup**), and press Play.

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
