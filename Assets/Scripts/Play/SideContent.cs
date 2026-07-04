using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.DarkWeb;
using ViceBayEmpire.Endgame;
using ViceBayEmpire.Bootstrap;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Activates the previously-dormant side systems and wires them into the world:
    ///  • Hitman contracts — seeded on the ContractBoard, accepted from the phone or a
    ///    physical board; accepting spawns a killable target that pays out on death.
    ///  • Gang territory wars — visible colored zones with enemy gang members; clear a
    ///    zone to conquer it (daily tribute) via the reused GangTerritory manager.
    ///  • Sports minigames — golf, jet-ski race and parachute jump placed as
    ///    interactable spots in the world.
    /// Called by GameBootstrap after the managers and player exist.
    /// </summary>
    public static class SideContent
    {
        // ---------------------------------------------------------------- contracts
        public static void SeedContracts(Transform world)
        {
            var cb = ContractBoard.Instance;
            if (cb == null) return;
            cb.contracts.Add(new ContractBoard.Contract { id = "hit1", targetName = "Snitch Eddie", lastKnownLocation = new Vector3(-40, 1, 30), rewardVC = 6, active = false });
            cb.contracts.Add(new ContractBoard.Contract { id = "hit2", targetName = "Loan Shark Vinnie", lastKnownLocation = new Vector3(50, 1, 30), rewardVC = 9, active = false });
            cb.contracts.Add(new ContractBoard.Contract { id = "hit3", targetName = "Crooked Cop Diaz", lastKnownLocation = new Vector3(-80, 1, -60), rewardVC = 14, active = false });

            // physical board next to the safehouse
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Hitman Board";
            board.transform.SetParent(world);
            board.transform.position = new Vector3(-70, 1.2f, -42);
            board.transform.localScale = new Vector3(2f, 2.4f, 0.3f);
            MaterialFactory.Paint(board, new Color(0.5f, 0.1f, 0.1f), 0.4f, emissive: true);
            board.AddComponent<HitmanBoard>();
        }

        public static void AcceptContract(string id)
        {
            var cb = ContractBoard.Instance;
            var c = cb != null ? cb.contracts.Find(x => x.id == id) : null;
            if (c == null || c.active || c.completed) return;

            cb.AcceptContract(id);   // marks active + notifies

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Contract Target: " + c.targetName;
            go.transform.position = c.lastKnownLocation + Vector3.up;
            MaterialFactory.Paint(go, new Color(0.6f, 0.1f, 0.5f), 0.4f, emissive: true);
            var npc = go.AddComponent<CityNPC>();
            npc.role = CityNPC.Role.Gang;
            npc.gang = "Contract";        // won't affect territory zones
            npc.health = 100f;
            npc.contractId = id;
            GameEvents.RaiseNotify($"Target marked: {c.targetName}", NotifyType.Warning);
        }

        // ---------------------------------------------------------------- gang wars
        public static void SeedGangZones(Transform world)
        {
            var gt = GangTerritory.Instance;
            if (gt == null) return;
            gt.player = PlayRefs.Player;

            AddZone(gt, world, "Slums (Cartel)", new Vector3(-95, 0, -15), new Vector3(60, 20, 70), "Cartel", new Color(0.7f, 0.2f, 0.2f));
            AddZone(gt, world, "Docks (Cartel)", new Vector3(-20, 0, 60), new Vector3(70, 20, 30), "Cartel", new Color(0.7f, 0.3f, 0.2f));
            AddZone(gt, world, "East Side (Vice Kings)", new Vector3(55, 0, 35), new Vector3(50, 20, 60), "Vice Kings", new Color(0.6f, 0.2f, 0.5f));
        }

        static void AddZone(GangTerritory gt, Transform world, string name, Vector3 center, Vector3 size, string owner, Color col)
        {
            var zone = new GangTerritory.Zone
            {
                zoneName = name,
                area = new Bounds(center, size),
                owner = owner,
                dailyTribute = 1500,
                enemyStrength = 3     // clear 3 members to flip the zone
            };
            gt.zones.Add(zone);

            // flat colored floor marker
            var quad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad.name = "Zone " + name;
            quad.transform.SetParent(world);
            quad.transform.position = new Vector3(center.x, 0.12f, center.z);
            quad.transform.localScale = new Vector3(size.x, 0.2f, size.z);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.AddComponent<GangZoneVisual>().Init(zone, col);

            // spawn a few enemy gang members inside
            for (int i = 0; i < 3; i++)
            {
                var pos = center + new Vector3(Random.Range(-size.x * 0.35f, size.x * 0.35f), 1,
                                               Random.Range(-size.z * 0.35f, size.z * 0.35f));
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Gang " + owner;
                go.transform.position = pos;
                MaterialFactory.Paint(go, col);
                var npc = go.AddComponent<CityNPC>();
                npc.role = CityNPC.Role.Gang;
                npc.gang = owner;
            }
        }

        // ---------------------------------------------------------------- sports
        public static void PlaceSports(Transform world)
        {
            // Golf
            var golf = Marker(world, "Golf Course", new Vector3(60, 1, -55), new Color(0.3f, 0.8f, 0.4f));
            golf.AddComponent<GolfChallenge>();

            // Jet-ski race in the bay
            var jetski = Marker(world, "Jet-Ski Race", new Vector3(-45, 1, 78), new Color(0.3f, 0.8f, 1f));
            var race = jetski.AddComponent<JetSkiRace>();
            race.buoys = new[]
            {
                Buoy(world, new Vector3(0, 0.6f, 96)),
                Buoy(world, new Vector3(35, 0.6f, 100)),
                Buoy(world, new Vector3(-25, 0.6f, 92)),
            };

            // Parachute jump downtown
            var chute = Marker(world, "Parachute Jump", new Vector3(0, 1, 40), new Color(1f, 0.6f, 0.2f));
            var jump = chute.AddComponent<ParachuteJump>();
            var lz = new GameObject("LandingZone").transform;
            lz.SetParent(world); lz.position = new Vector3(0, 1, 55);
            jump.landingZone = lz;
        }

        static GameObject Marker(Transform world, string name, Vector3 pos, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(world);
            go.transform.position = pos + Vector3.up;
            go.transform.localScale = new Vector3(2f, 2f, 2f);
            MaterialFactory.Paint(go, col, 0.5f, emissive: true);
            return go;
        }

        static Transform Buoy(Transform world, Vector3 pos)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Buoy"; b.transform.SetParent(world);
            b.transform.position = pos; b.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Object.Destroy(b.GetComponent<Collider>());
            MaterialFactory.Paint(b, new Color(1f, 0.9f, 0.2f), 0.5f, emissive: true);
            return b.transform;
        }
    }

    /// <summary>Recolors a gang-zone floor marker as ownership changes.</summary>
    public class GangZoneVisual : MonoBehaviour
    {
        GangTerritory.Zone zone;
        Color enemyColor;
        string lastOwner = "";
        bool lastContested;

        public void Init(GangTerritory.Zone z, Color enemy) { zone = z; enemyColor = enemy; }

        void Update()
        {
            if (zone == null) return;
            if (zone.owner == lastOwner && zone.contested == lastContested) return;
            lastOwner = zone.owner; lastContested = zone.contested;
            Color c = zone.owner == "Player" ? new Color(0.2f, 0.7f, 0.3f)
                    : zone.contested ? new Color(0.9f, 0.8f, 0.2f) : enemyColor;
            MaterialFactory.Paint(gameObject, new Color(c.r, c.g, c.b, 1f), 0.4f, emissive: true);
        }
    }

    /// <summary>Physical hitman board: opens the phone's Contracts tab.</summary>
    public class HitmanBoard : MonoBehaviour, Crime.IInteractable
    {
        public string Prompt => "[E] Read the hitman board";
        public void Interact(GameObject interactor) => MenuSystem.Instance?.OpenContracts();
    }
}
