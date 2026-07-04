using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Audio;
using ViceBayEmpire.Play;
using ViceBayEmpire.Data;
using ViceBayEmpire.DarkWeb;
using ViceBayEmpire.Business;
using ViceBayEmpire.Property;
using ViceBayEmpire.Endgame;
using ViceBayEmpire.Player;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// THE ENTRY POINT. Put this one component on an empty GameObject in an empty scene
    /// and press Play — it builds the entire playable game at runtime: managers, city,
    /// player (capsule) with camera/movement/shooting/interaction, vehicles, NPCs, HUD,
    /// menus, audio, lighting + day/night, and starting content (a pistol, some Dark Web
    /// listings, a business, a property). No prefabs, no scene wiring, no art assets.
    ///
    /// Because all gameplay data (WeaponData, BusinessData, …) are ScriptableObjects,
    /// we build a starter set in code here via ScriptableObject.CreateInstance so the
    /// game is populated even with no authored .asset files.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            BuildManagers();
            BuildEnvironment();
            var spawn = CityBuilder.Build();
            var player = BuildPlayer(spawn);
            BuildCamera(player);
            SeedContent(player);
            SpawnVehicles();
            SpawnNPCs();
            BuildUI();

            GameManager.Instance.StartNewGame();
            GameEvents.RaiseNotify("Vice Bay Empire — walk (WASD), drive (F), rob (aim+E), phone (P).", NotifyType.Info);
        }

        // ---------------------------------------------------------------- managers
        void BuildManagers()
        {
            var root = new GameObject("~Managers");
            var gm = root.AddComponent<GameManager>();
            gm.economy = root.AddComponent<EconomyManager>();
            gm.clock = root.AddComponent<WorldClock>();

            var wanted = root.AddComponent<WantedSystem>();

            root.AddComponent<AudioManager>();
            root.AddComponent<DialogueSystem>();
            root.AddComponent<DarkWebMarketplace>();
            root.AddComponent<FraudCenter>();
            root.AddComponent<ContractBoard>();
            root.AddComponent<BusinessManager>();
            root.AddComponent<PropertyManager>();
            root.AddComponent<StockMarket>();
            root.AddComponent<SaveCoordinator>();

            var police = root.AddComponent<PoliceResponse>();
            police.wanted = wanted;

            // link ambient audio to weather
            GameEvents.HourChanged += _ => { };
        }

        // ---------------------------------------------------------------- environment
        void BuildEnvironment()
        {
            // sun + day/night driver
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50, 150, 0);
            sun.shadows = LightShadows.Soft;
            GameManager.Instance.clock.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.5f, 0.55f, 0.65f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 320f;

            // simple weather → rain audio + fog density hook
            var clock = GameManager.Instance.clock;
            var weatherGo = new GameObject("WeatherAudio");
            weatherGo.AddComponent<WeatherAudioLink>().clock = clock;
        }

        // ---------------------------------------------------------------- player
        GameObject BuildPlayer(Vector3 spawn)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = spawn;
            Destroy(player.GetComponent<Collider>());
            MaterialFactory.Paint(player, new Color(0.2f, 0.6f, 0.9f));

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.center = new Vector3(0, 1, 0); cc.radius = 0.4f;

            var move = player.AddComponent<PlayerMovement3D>();
            var status = player.AddComponent<PlayerStatus>();
            var loadout = player.AddComponent<PlayerLoadout>();
            var shooter = player.AddComponent<PlayerShooter>();
            player.AddComponent<PlayerInteractor3D>();

            // tracer line for shots
            var tracer = player.AddComponent<LineRenderer>();
            tracer.material = MaterialFactory.Solid(new Color(1f, 0.9f, 0.4f), 0.5f, true);
            tracer.widthMultiplier = 0.04f; tracer.enabled = false; tracer.positionCount = 2;
            shooter.tracer = tracer;

            PlayRefs.Player = player.transform;
            PlayRefs.Status = status;
            PlayRefs.Movement = move;
            PlayRefs.Shooter = shooter;
            return player;
        }

        void BuildCamera(GameObject player)
        {
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;

            PlayRefs.Cam = cam;
            PlayRefs.Movement.cam = tpc;
            PlayRefs.Shooter.cam = cam;
            player.GetComponent<PlayerInteractor3D>().cam = cam;
        }

        // ---------------------------------------------------------------- content
        void SeedContent(GameObject player)
        {
            // starter pistol
            var pistol = MakeWeapon("pistol", "Pistol", WeaponCategory.Pistol, dmg: 18, rate: 3, mag: 12, auto: false);
            player.GetComponent<PlayerLoadout>().GiveWeapon(pistol, 60);
            var smg = MakeWeapon("smg", "Micro SMG", WeaponCategory.SMG, dmg: 12, rate: 12, mag: 30, auto: true);
            player.GetComponent<PlayerLoadout>().GiveWeapon(smg, 120);

            // dark web listings
            var market = DarkWebMarketplace.Instance;
            var rifle = MakeWeapon("rifle", "Assault Rifle", WeaponCategory.Rifle, dmg: 24, rate: 9, mag: 30, auto: true);
            market.catalog.Add(MakeDW("dw_rifle", "Assault Rifle (untraceable)", DarkWebCategory.Weapon, 9, weapon: rifle, fed: 8, trust: 0.85f));
            market.catalog.Add(MakeDW("dw_skimmer", "ATM Skimmer Kit", DarkWebCategory.FraudTool, 4, fraudTool: "skimmer_kit", fed: 5, trust: 0.7f));
            market.catalog.Add(MakeDW("dw_phish", "Phishing Kit", DarkWebCategory.FraudTool, 3, fraudTool: "phishing_kit", fed: 5, trust: 0.75f));
            market.catalog.Add(MakeDW("dw_counterfeit", "Counterfeit Plates", DarkWebCategory.FraudTool, 6, fraudTool: "counterfeit_plates", fed: 6, trust: 0.6f));
            market.catalog.Add(MakeDW("dw_cred", "Bank Credential Dump", DarkWebCategory.Data, 5, cashPayout: 12000, fed: 12, trust: 0.5f));
            market.catalog.Add(MakeDW("dw_scam", "'Free' Money Doubler", DarkWebCategory.Service, 2, fed: 0, trust: 0.05f, scam: 0.9f));

            // businesses
            var bm = BusinessManager.Instance;
            bm.availableBusinesses.Add(MakeBiz("biz_club", "Neon Nightclub", BusinessType.Nightclub, legal: true, cost: 180000, daily: 4000, new Vector3(30, 0, -20)));
            bm.availableBusinesses.Add(MakeBiz("biz_lab", "Coastal Drug Lab", BusinessType.DrugLab, legal: false, cost: 220000, daily: 0, new Vector3(-60, 0, -30)));
            bm.availableBusinesses.Add(MakeBiz("biz_hack", "Hack Farm", BusinessType.HackFarm, legal: false, cost: 150000, daily: 0, new Vector3(50, 0, 20)));

            // properties
            var pm = PropertyManager.Instance;
            pm.allProperties.Add(MakeProp("prop_safe", "Little Haiti Safehouse", PropertyType.Safehouse, 0, new Vector3(0, 1, -50)));
            pm.allProperties.Add(MakeProp("prop_pent", "Ocean Penthouse", PropertyType.Penthouse, 350000, new Vector3(40, 1, -10)));
            pm.allProperties.Add(MakeProp("prop_yacht", "Mega Yacht", PropertyType.Yacht, 1200000, new Vector3(0, 1, 100)));
            pm.owned.Add("prop_safe"); pm.activeHomeId = "prop_safe";
            PlayRefs.Status.respawnPoint = new Vector3(0, 1.2f, -34);
        }

        // ---------------------------------------------------------------- spawns
        void SpawnVehicles()
        {
            VehicleFactory.Build(DriveableVehicle.Mode.Car, new Vector3(6, 0.6f, -28), new Color(0.8f, 0.2f, 0.3f), "Banshee");
            VehicleFactory.Build(DriveableVehicle.Mode.Car, new Vector3(-8, 0.6f, -28), new Color(0.2f, 0.3f, 0.8f), "Sedan");
            VehicleFactory.Build(DriveableVehicle.Mode.Car, new Vector3(14, 0.6f, 0), new Color(0.9f, 0.8f, 0.2f), "Muscle");
            VehicleFactory.Build(DriveableVehicle.Mode.Boat, new Vector3(0, 0.6f, 78), new Color(0.9f, 0.9f, 0.95f), "Speedboat");
            VehicleFactory.Build(DriveableVehicle.Mode.Plane, new Vector3(-70, 0.8f, -55), new Color(0.9f, 0.3f, 0.3f), "Stunt Plane");
            VehicleFactory.Build(DriveableVehicle.Mode.Helicopter, new Vector3(-70, 0.8f, 10), new Color(0.3f, 0.6f, 0.8f), "Sparrow");
        }

        void SpawnNPCs()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 16; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Pedestrian";
                go.transform.position = new Vector3(rng.Next(-80, 80), 1, rng.Next(-60, 60));
                MaterialFactory.Paint(go, new Color(0.6f + (float)rng.NextDouble() * 0.3f, 0.6f, 0.5f));
                var npc = go.AddComponent<CityNPC>();
                npc.role = rng.Next(5) == 0 ? CityNPC.Role.Gang : CityNPC.Role.Civilian;
            }
            // one driver you can carjack: park a car and mark its "driver"
            var jackCar = VehicleFactory.Build(DriveableVehicle.Mode.Car, new Vector3(0, 0.6f, 26), new Color(0.3f, 0.7f, 0.3f), "Taxi");
            var driver = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            driver.name = "Driver"; driver.transform.position = jackCar.transform.position + Vector3.up;
            MaterialFactory.Paint(driver, new Color(0.8f, 0.7f, 0.5f));
            var dn = driver.AddComponent<CityNPC>();
            dn.car = jackCar;
        }

        void BuildUI()
        {
            var ui = new GameObject("~UI");
            var wanted = FindObjectOfType<WantedSystem>();
            var hud = ui.AddComponent<GameHUD>();
            hud.wanted = wanted;
            hud.player = PlayRefs.Player;
            ui.AddComponent<MenuSystem>();

            // late wiring that needs the player to already exist
            if (wanted != null) wanted.player = PlayRefs.Player;      // enables heat decay / line-of-sight
            if (SaveCoordinator.Instance != null) SaveCoordinator.Instance.player = PlayRefs.Player;
        }

        // ---------------------------------------------------------------- SO factories
        static WeaponData MakeWeapon(string id, string name, WeaponCategory cat, float dmg, float rate, int mag, bool auto)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.id = id; w.displayName = name; w.category = cat;
            w.damage = dmg; w.fireRate = rate; w.magazineSize = mag; w.automatic = auto;
            w.range = 150; w.reloadTime = 1.6f; w.spreadDegrees = cat == WeaponCategory.SMG ? 4f : 2f;
            w.price = 1000; w.ammoPrice = 40;
            return w;
        }

        static DarkWebItemData MakeDW(string id, string title, DarkWebCategory cat, double vc,
            WeaponData weapon = null, string fraudTool = null, long cashPayout = 0, int fed = 5,
            float trust = 0.8f, float scam = 0.1f)
        {
            var d = ScriptableObject.CreateInstance<DarkWebItemData>();
            d.id = id; d.listingTitle = title; d.category = cat; d.priceVC = vc;
            d.weaponReward = weapon; d.fraudToolId = fraudTool; d.cashPayout = cashPayout;
            d.federalAttention = fed; d.vendorTrust = trust; d.baseScamChance = scam;
            return d;
        }

        static BusinessData MakeBiz(string id, string name, BusinessType type, bool legal, long cost, long daily, Vector3 loc)
        {
            var b = ScriptableObject.CreateInstance<BusinessData>();
            b.id = id; b.displayName = name; b.type = type; b.isLegal = legal;
            b.purchaseCost = cost; b.baseDailyIncome = daily; b.worldLocation = loc;
            b.upgradeCosts = new long[] { cost / 4, cost / 2, cost };
            if (!legal) { b.producedGoodsPerDay = 8; b.goodsSaleValue = 6000; b.raidChancePerDay = 0.05f; }
            return b;
        }

        static PropertyData MakeProp(string id, string name, PropertyType type, long cost, Vector3 pos)
        {
            var p = ScriptableObject.CreateInstance<PropertyData>();
            p.id = id; p.displayName = name; p.type = type; p.purchaseCost = cost;
            p.entrancePosition = pos; p.spawnInsidePosition = pos; p.isSavePoint = true;
            p.hasWeaponStorage = type == PropertyType.Yacht || type == PropertyType.Mansion;
            return p;
        }
    }

    /// <summary>Links weather to rain audio and a bit of fog.</summary>
    public class WeatherAudioLink : MonoBehaviour
    {
        public WorldClock clock;
        void Update()
        {
            if (clock == null) return;
            bool rain = clock.weather == WorldClock.Weather.Rain || clock.weather == WorldClock.Weather.Storm;
            AudioManager.Instance?.SetRain(rain);
            RenderSettings.fogEndDistance = clock.weather == WorldClock.Weather.Fog ? 120f : 320f;
        }
    }
}
