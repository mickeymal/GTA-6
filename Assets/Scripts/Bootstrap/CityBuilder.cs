using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Play;
using ViceBayEmpire.World;      // ATMInteractable, ShopVendor (reused)
using ViceBayEmpire.Audio;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Builds a small-but-dense Vice City / Miami out of primitives at runtime, laid out
    /// on the shared CityLayout grid: land + swimmable ocean & bay, a connected road grid
    /// with intersections, a neon Downtown of tall towers, a Slums/Industrial west side,
    /// a Beach promenade, a Harbor with piers, an Airport runway + hangar, and two fully
    /// enterable, robbable interiors (a bank with a vault and a convenience store).
    /// Also plants 3D ambient audio (city hum, waves, wind) and a few night lights.
    /// Returns the player's spawn point; dynamic spawns are placed by GameBootstrap.
    /// </summary>
    public static class CityBuilder
    {
        static Transform city;

        public static Vector3 Build()
        {
            city = new GameObject("City").transform;

            BuildTerrain();
            BuildRoads();
            BuildDowntown();
            BuildSlumsIndustrial();
            BuildBeach();
            BuildHarbor();
            BuildAirport();
            BuildBank(new Vector3(-6, 0, -18));
            BuildStore(new Vector3(-88, 0, 6));
            BuildATMsAndShops();
            PlaceAmbientAudio();
            PlaceNightLights();

            return new Vector3(4, 1.4f, -30);   // on a downtown street
        }

        // ================================================================ terrain
        static void BuildTerrain()
        {
            // land slab, top at y = 0
            Box("Land", new Vector3((CityLayout.LandXMin + CityLayout.LandXMax) / 2f, -1f,
                                    (CityLayout.LandZMin + CityLayout.LandZMax) / 2f),
                new Vector3(CityLayout.LandXMax - CityLayout.LandXMin, 2f, CityLayout.LandZMax - CityLayout.LandZMin),
                new Color(0.30f, 0.32f, 0.30f));

            // ocean (east) + bay (north): seabed pit + translucent water (no collider = swim through)
            WaterBody("Ocean", new Vector3(CityLayout.OceanX + 30, 0, -10), new Vector3(60, 0, 210));
            WaterBody("Bay", new Vector3(-20, 0, CityLayout.BayZ + 30), new Vector3(230, 0, 60));
        }

        static void WaterBody(string name, Vector3 center, Vector3 size)
        {
            Box(name + "Seabed", new Vector3(center.x, -3.5f, center.z), new Vector3(size.x, 4f, size.z),
                new Color(0.12f, 0.18f, 0.22f));
            var water = Box(name, new Vector3(center.x, CityLayout.WaterLevelY, center.z),
                            new Vector3(size.x, 0.2f, size.z), new Color(0.15f, 0.42f, 0.6f), 0.75f);
            foreach (var c in water.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }

        // ================================================================ roads
        static void BuildRoads()
        {
            var roadCol = new Color(0.11f, 0.11f, 0.12f);
            var lineCol = new Color(0.7f, 0.65f, 0.2f);
            float zLen = CityLayout.LandZMax - CityLayout.LandZMin;
            float xLen = CityLayout.LandXMax - CityLayout.LandXMin;
            float zMid = (CityLayout.LandZMin + CityLayout.LandZMax) / 2f;
            float xMid = (CityLayout.LandXMin + CityLayout.LandXMax) / 2f;

            foreach (var x in CityLayout.RoadsX)
            {
                Box("RoadNS", new Vector3(x, 0.03f, zMid), new Vector3(CityLayout.RoadWidth, 0.06f, zLen), roadCol);
                Box("Lane", new Vector3(x, 0.05f, zMid), new Vector3(0.2f, 0.06f, zLen), lineCol);
            }
            foreach (var z in CityLayout.RoadsZ)
            {
                Box("RoadEW", new Vector3(xMid, 0.03f, z), new Vector3(xLen, 0.06f, CityLayout.RoadWidth), roadCol);
                Box("Lane", new Vector3(xMid, 0.05f, z), new Vector3(xLen, 0.06f, 0.2f), lineCol);
            }
        }

        // ================================================================ districts
        static void BuildDowntown()
        {
            var rng = new System.Random(11);
            Color[] neon = { new(1f, 0.3f, 0.7f), new(0.3f, 0.85f, 1f), new(0.7f, 0.4f, 1f), new(1f, 0.6f, 0.3f), new(0.4f, 1f, 0.7f) };
            // tall towers in the blocks between the northern roads
            for (int i = 0; i < CityLayout.RoadsX.Length - 1; i++)
            {
                float bx = (CityLayout.RoadsX[i] + CityLayout.RoadsX[i + 1]) / 2f;
                for (float bz = 10; bz <= 65; bz += 22)
                {
                    if (rng.NextDouble() < 0.15) continue;
                    float h = rng.Next(20, 46);
                    var tower = Box("Tower", new Vector3(bx, h / 2f, bz), new Vector3(rng.Next(8, 13), h, rng.Next(8, 13)),
                                    new Color(0.16f, 0.17f, 0.24f), 0.3f);
                    // neon crown + vertical strip
                    Box("Neon", new Vector3(0, 0.5f, 0), new Vector3(1.03f, 0.05f, 1.03f), neon[rng.Next(neon.Length)],
                        0.5f, emissive: true, parent: tower.transform, local: true);
                    Box("NeonStrip", new Vector3(0.5f, 0.1f, 0), new Vector3(0.05f, 0.7f, 0.2f), neon[rng.Next(neon.Length)],
                        0.5f, emissive: true, parent: tower.transform, local: true);
                }
            }
        }

        static void BuildSlumsIndustrial()
        {
            var rng = new System.Random(22);
            // low, drab buildings + warehouses west of x = -60
            for (float x = -125; x < -62; x += 14)
                for (float z = -70; z < 60; z += 16)
                {
                    if (NearRoad(x, z) || rng.NextDouble() < 0.35) continue;
                    float h = rng.Next(4, 9);
                    Box("Slum", new Vector3(x, h / 2f, z), new Vector3(rng.Next(8, 12), h, rng.Next(8, 12)),
                        new Color(0.28f, 0.26f, 0.22f), 0.1f);
                }
            // industrial tanks + a warehouse near the airport road
            for (int i = 0; i < 4; i++)
            {
                var tank = Cylinder("Tank", new Vector3(-110 + i * 8, 3, -50), new Vector3(5, 3, 5),
                                    new Color(0.5f, 0.5f, 0.45f));
            }
            Box("Warehouse", new Vector3(-100, 4, -30), new Vector3(24, 8, 16), new Color(0.35f, 0.33f, 0.3f));
        }

        static void BuildBeach()
        {
            // sand promenade between the city and the ocean
            Box("Beach", new Vector3(CityLayout.OceanX - 6, 0.06f, -10), new Vector3(14, 0.1f, 200),
                new Color(0.82f, 0.76f, 0.52f));
            // palm trees (trunk + canopy)
            var rng = new System.Random(33);
            for (float z = -90; z < 75; z += 12)
            {
                float x = CityLayout.OceanX - 4 + (float)rng.NextDouble() * 4;
                Cylinder("Palm", new Vector3(x, 2.5f, z), new Vector3(0.5f, 2.5f, 0.5f), new Color(0.4f, 0.3f, 0.2f));
                Box("Canopy", new Vector3(x, 5.2f, z), new Vector3(4, 0.4f, 4), new Color(0.2f, 0.6f, 0.3f));
            }
        }

        static void BuildHarbor()
        {
            // wooden piers extending into the bay for boats
            for (int i = 0; i < 3; i++)
            {
                float x = -60 + i * 40;
                Box("Pier", new Vector3(x, 0.5f, CityLayout.BayZ + 8), new Vector3(4, 1f, 24),
                    new Color(0.4f, 0.3f, 0.2f));
            }
            Box("HarborWarehouse", new Vector3(-20, 4, 74), new Vector3(30, 8, 10), new Color(0.3f, 0.35f, 0.4f));
        }

        static void BuildAirport()
        {
            // long runway along X in the south, with markings + a hangar + tower
            Box("Runway", new Vector3(-40, 0.05f, -92), new Vector3(120, 0.1f, 14), new Color(0.1f, 0.1f, 0.11f));
            for (float x = -95; x < 15; x += 10)
                Box("RunwayMark", new Vector3(x, 0.08f, -92), new Vector3(4, 0.06f, 0.6f), new Color(0.9f, 0.9f, 0.9f));
            Box("Hangar", new Vector3(-95, 5, -78), new Vector3(26, 10, 20), new Color(0.45f, 0.47f, 0.5f), 0.2f);
            Box("ControlTower", new Vector3(10, 8, -78), new Vector3(5, 16, 5), new Color(0.5f, 0.5f, 0.55f));
            // access road stub connecting runway to the grid
            Box("AirportRoad", new Vector3(-20, 0.03f, -85), new Vector3(6, 0.06f, 20), new Color(0.11f, 0.11f, 0.12f));
        }

        // ================================================================ interiors
        static void BuildBank(Vector3 basePos)
        {
            var color = new Color(0.5f, 0.46f, 0.34f);
            BuildRoom("Vice National Bank", basePos, new Vector3(20, 6, 18), color);
            // teller counter (robbable), near the back wall, facing the entrance
            var counter = Box("Bank Counter", basePos + new Vector3(0, 0.7f, 4), new Vector3(8, 1.4f, 1),
                              new Color(0.45f, 0.35f, 0.2f));
            var desk = counter.AddComponent<RobberyDesk>();
            desk.isBank = true; desk.locationName = "Vice National Bank"; desk.registerCash = 3500; desk.vaultCash = 65000;
            Teller(basePos + new Vector3(0, 1, 5.5f));
            // vault at the very back
            var vaultGo = new GameObject("Vault"); vaultGo.transform.SetParent(city);
            vaultGo.transform.position = basePos + new Vector3(0, 1, 7.5f);
            desk.vaultPoint = vaultGo.transform;
            Box("VaultDoor", basePos + new Vector3(0, 1.6f, 8), new Vector3(4, 3.2f, 0.4f), new Color(0.55f, 0.5f, 0.25f), 0.6f);
            SignLight(basePos + new Vector3(0, 6.5f, -9), new Color(0.3f, 0.85f, 1f));
        }

        static void BuildStore(Vector3 basePos)
        {
            var color = new Color(0.32f, 0.4f, 0.5f);
            BuildRoom("Rob's Liquor", basePos, new Vector3(12, 5, 12), color);
            var counter = Box("Store Counter", basePos + new Vector3(0, 0.7f, 3), new Vector3(5, 1.4f, 1),
                              new Color(0.4f, 0.3f, 0.2f));
            var desk = counter.AddComponent<RobberyDesk>();
            desk.isBank = false; desk.locationName = "Rob's Liquor"; desk.registerCash = 1800;
            Teller(basePos + new Vector3(0, 1, 4));
            // shelves
            for (int i = -1; i <= 1; i++)
                Box("Shelf", basePos + new Vector3(i * 3, 1, -1), new Vector3(1.5f, 2, 4), new Color(0.5f, 0.45f, 0.4f));
            SignLight(basePos + new Vector3(0, 5.5f, -6), new Color(1f, 0.4f, 0.4f));
        }

        /// <summary>Four walls + floor + roof with a doorway gap on the -Z (south) side.</summary>
        static void BuildRoom(string name, Vector3 center, Vector3 size, Color wallColor)
        {
            var root = new GameObject(name + " Interior"); root.transform.SetParent(city);
            root.transform.position = center;
            float sx = size.x, h = size.y, sz = size.z, t = 0.4f;

            Box("Floor", center + new Vector3(0, 0.05f, 0), new Vector3(sx, 0.1f, sz), new Color(0.25f, 0.25f, 0.26f));
            Box("Roof", center + new Vector3(0, h, 0), new Vector3(sx, 0.2f, sz), wallColor * 0.7f);
            // north / east / west solid walls
            Box("WallN", center + new Vector3(0, h / 2f, sz / 2f), new Vector3(sx, h, t), wallColor);
            Box("WallE", center + new Vector3(sx / 2f, h / 2f, 0), new Vector3(t, h, sz), wallColor);
            Box("WallW", center + new Vector3(-sx / 2f, h / 2f, 0), new Vector3(t, h, sz), wallColor);
            // south wall with a 4-wide doorway gap
            float seg = (sx - 4f) / 2f;
            Box("WallS1", center + new Vector3(-(sx - seg) / 2f, h / 2f, -sz / 2f), new Vector3(seg, h, t), wallColor);
            Box("WallS2", center + new Vector3((sx - seg) / 2f, h / 2f, -sz / 2f), new Vector3(seg, h, t), wallColor);
            // interior fill light so it isn't pitch black at night
            var lg = new GameObject("InteriorLight"); lg.transform.SetParent(root.transform);
            lg.transform.position = center + new Vector3(0, h - 0.5f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = Mathf.Max(sx, sz); l.intensity = 1.2f;
            l.color = new Color(1f, 0.95f, 0.85f);
        }

        static void Teller(Vector3 pos)
        {
            var teller = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teller.name = "Teller"; teller.transform.SetParent(city); teller.transform.position = pos;
            MaterialFactory.Paint(teller, new Color(0.9f, 0.82f, 0.62f));
            Object.Destroy(teller.GetComponent<Collider>());
        }

        // ================================================================ props
        static void BuildATMsAndShops()
        {
            var atm = Box("ATM", new Vector3(-10, 1, -24), new Vector3(1, 2, 0.6f), new Color(0.2f, 0.5f, 0.4f), 0.4f);
            atm.AddComponent<ATMInteractable>();
            var atm2 = Box("ATM", new Vector3(-84, 1, 0), new Vector3(1, 2, 0.6f), new Color(0.2f, 0.5f, 0.4f), 0.4f);
            atm2.AddComponent<ATMInteractable>();

            var gun = Box("Ammu-Nation", new Vector3(24, 1.5f, -6), new Vector3(3, 3, 3), new Color(0.6f, 0.3f, 0.15f), 0.3f);
            gun.AddComponent<ShopVendor>().kind = ShopVendor.ShopKind.GunStore;
        }

        static void PlaceAmbientAudio()
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            am.PlaceAmbient("ambience", CityLayout.Downtown, 0.4f, 90f);       // city hum downtown
            am.PlaceAmbient("waves", new Vector3(CityLayout.OceanX + 4, 0, -10), 0.5f, 70f); // ocean
            am.PlaceAmbient("waves", CityLayout.Harbor, 0.4f, 60f);           // bay
            am.PlaceAmbient("wind", CityLayout.Airport, 0.4f, 80f);          // airport wind
        }

        static void PlaceNightLights()
        {
            // a few point lights so Downtown glows at night (kept small for performance)
            foreach (var pos in new[] { new Vector3(0, 8, 30), new Vector3(-40, 8, 40), new Vector3(40, 8, 20),
                                        CityLayout.Harbor + Vector3.up * 8 })
            {
                var g = new GameObject("StreetLight"); g.transform.SetParent(city); g.transform.position = pos;
                var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 55; l.intensity = 1.4f;
                l.color = new Color(1f, 0.7f, 0.9f);
            }
        }

        // ================================================================ helpers
        static bool NearRoad(float x, float z)
        {
            foreach (var rx in CityLayout.RoadsX) if (Mathf.Abs(x - rx) < 8) return true;
            foreach (var rz in CityLayout.RoadsZ) if (Mathf.Abs(z - rz) < 8) return true;
            return false;
        }

        static GameObject Box(string name, Vector3 pos, Vector3 scale, Color color,
                              float smoothness = 0.15f, bool emissive = false, Transform parent = null, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : city);
            if (local) { go.transform.localPosition = pos; go.transform.localScale = scale; }
            else { go.transform.position = pos; go.transform.localScale = scale; }
            MaterialFactory.Paint(go, color, smoothness, emissive);
            return go;
        }

        static GameObject Cylinder(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name; go.transform.SetParent(city);
            go.transform.position = pos; go.transform.localScale = scale;
            MaterialFactory.Paint(go, color);
            return go;
        }

        static void SignLight(Vector3 pos, Color color)
        {
            Box("Sign", pos, new Vector3(6, 1, 0.3f), color, 0.5f, emissive: true);
        }
    }
}
