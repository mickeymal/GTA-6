using UnityEngine;
using ViceBayEmpire.Play;
using ViceBayEmpire.World;      // ATMInteractable, ShopVendor (reused stubs)

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Procedurally builds the playable sample city from primitives: land, a water bay
    /// you can swim/boat in, a road grid, scattered neon buildings, a robbable bank and
    /// convenience store (with teller counters + vault), an ATM (skimmer target), and a
    /// small airstrip. Everything is generated in code so the scene needs no art.
    /// </summary>
    public static class CityBuilder
    {
        public static Vector3 Build()
        {
            var city = new GameObject("City").transform;

            // ---- land (top at y=0) ----
            Box(city, "Land", new Vector3(0, -0.5f, 0), new Vector3(200, 1, 140),
                new Color(0.28f, 0.32f, 0.28f), 0f);

            // ---- water bay to the north (z > 70) ----
            Box(city, "Seabed", new Vector3(0, -3f, 100), new Vector3(200, 1, 60), new Color(0.15f, 0.2f, 0.25f), 0f);
            var water = Box(city, "Water", new Vector3(0, 0.4f, 100), new Vector3(200, 0.2f, 60),
                            new Color(0.15f, 0.4f, 0.6f), 0.7f);
            foreach (var c in water.GetComponentsInChildren<Collider>()) Object.Destroy(c);   // swim-through

            // ---- beach transition ----
            Box(city, "Beach", new Vector3(0, 0.01f, 68), new Vector3(200, 0.1f, 8), new Color(0.8f, 0.75f, 0.5f), 0f);

            // ---- road grid ----
            for (int i = -2; i <= 2; i++)
            {
                Box(city, "RoadX", new Vector3(0, 0.03f, i * 28), new Vector3(200, 0.06f, 6), new Color(0.12f, 0.12f, 0.13f), 0f);
                Box(city, "RoadZ", new Vector3(i * 28, 0.03f, 0), new Vector3(6, 0.06f, 140), new Color(0.12f, 0.12f, 0.13f), 0f);
            }

            // ---- scattered neon buildings ----
            var rng = new System.Random(6);
            Color[] neon = { new(1f, 0.3f, 0.7f), new(0.3f, 0.85f, 1f), new(0.7f, 0.4f, 1f), new(1f, 0.6f, 0.3f) };
            for (int i = 0; i < 26; i++)
            {
                float x = rng.Next(-90, 90), z = rng.Next(-60, 60);
                if (Mathf.Abs(x % 28) < 8 || Mathf.Abs(z % 28) < 8) continue;   // keep roads clear
                float h = rng.Next(6, 26);
                var b = Box(city, "Building", new Vector3(x, h / 2f, z), new Vector3(rng.Next(6, 12), h, rng.Next(6, 12)),
                            new Color(0.2f, 0.2f, 0.26f), 0.2f);
                // neon crown
                Box(b.transform, "Neon", new Vector3(0, 0.5f, 0), new Vector3(1.02f, 0.06f, 1.02f),
                    neon[rng.Next(neon.Length)], 0.5f, emissive: true, localSpace: true);
            }

            // ---- bank (robbable) ----
            BuildRobbery(city, "Vice National Bank", new Vector3(34, 0, 6), isBank: true,
                         new Color(0.55f, 0.5f, 0.35f), register: 3000, vault: 60000);

            // ---- convenience store (robbable) ----
            BuildRobbery(city, "Rob's Liquor", new Vector3(-34, 0, 6), isBank: false,
                         new Color(0.3f, 0.4f, 0.5f), register: 1500, vault: 0);

            // ---- ATM (skimmer target) ----
            var atm = Box(city, "ATM", new Vector3(-20, 1f, -8), new Vector3(1f, 2f, 0.6f), new Color(0.2f, 0.5f, 0.4f), 0.4f);
            atm.AddComponent<ATMInteractable>();

            // ---- shop signs (flavor / hooks) ----
            var gun = Box(city, "Ammu-Nation", new Vector3(20, 1.5f, -20), new Vector3(3, 3, 3), new Color(0.6f, 0.3f, 0.15f), 0.3f);
            gun.AddComponent<ShopVendor>().kind = ShopVendor.ShopKind.GunStore;

            // ---- airstrip ----
            Box(city, "Runway", new Vector3(-70, 0.04f, -30), new Vector3(10, 0.08f, 80), new Color(0.1f, 0.1f, 0.11f), 0f);

            return new Vector3(0, 1.2f, -34);   // player spawn
        }

        static void BuildRobbery(Transform parent, string name, Vector3 basePos, bool isBank,
                                 Color color, long register, long vault)
        {
            var b = Box(parent, name, basePos + new Vector3(0, 5, 6), new Vector3(16, 10, 14), color, 0.2f);
            // teller counter facing the street (player robs from outside)
            var counter = Box(parent, name + " Counter", basePos + new Vector3(0, 0.6f, -1),
                              new Vector3(6, 1.2f, 1), new Color(0.4f, 0.3f, 0.2f), 0.2f);
            var desk = counter.AddComponent<RobberyDesk>();
            desk.isBank = isBank; desk.locationName = name; desk.registerCash = register; desk.vaultCash = vault;
            // teller capsule
            var teller = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            teller.name = "Teller"; teller.transform.SetParent(parent);
            teller.transform.position = basePos + new Vector3(0, 1, 1.5f);
            MaterialFactory.Paint(teller, new Color(0.9f, 0.8f, 0.6f));
            Object.Destroy(teller.GetComponent<Collider>());
            // vault point (banks)
            if (isBank)
            {
                var vaultGo = new GameObject("Vault");
                vaultGo.transform.SetParent(parent);
                vaultGo.transform.position = basePos + new Vector3(0, 1, 10);
                desk.vaultPoint = vaultGo.transform;
                Box(parent, "VaultDoor", basePos + new Vector3(0, 1.5f, 11), new Vector3(3, 3, 0.5f),
                    new Color(0.5f, 0.45f, 0.2f), 0.6f);
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Color color,
                              float smoothness, bool emissive = false, bool localSpace = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            if (localSpace) { go.transform.localPosition = pos; go.transform.localScale = scale; }
            else { go.transform.position = pos; go.transform.localScale = scale; }
            MaterialFactory.Paint(go, color, smoothness, emissive);
            return go;
        }
    }
}
