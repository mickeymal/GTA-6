using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Spawns chasing cop capsules to match the current wanted level (read from the
    /// reused WantedSystem). Higher stars = more cops that spawn faster. Clears them
    /// when the heat is gone. Uses CityNPC in Cop role so no prefab wiring is needed.
    /// </summary>
    public class PoliceResponse : MonoBehaviour
    {
        public WantedSystem wanted;
        public Color copColor = new(0.15f, 0.2f, 0.6f);
        float spawnTimer;
        readonly List<CityNPC> cops = new();

        void Update()
        {
            cops.RemoveAll(c => c == null);
            int stars = wanted != null ? wanted.stars : 0;

            if (stars == 0)
            {
                if (cops.Count > 0) { foreach (var c in cops) if (c) Destroy(c.gameObject); cops.Clear(); }
                return;
            }

            int target = stars * 2;
            spawnTimer -= Time.deltaTime;
            if (cops.Count < target && spawnTimer <= 0f)
            {
                spawnTimer = Mathf.Max(1.5f, 6f - stars);
                SpawnCop();
            }
        }

        void SpawnCop()
        {
            if (PlayRefs.Player == null) return;
            Vector3 offset = Random.insideUnitSphere * 35f; offset.y = 0;
            Vector3 pos = PlayRefs.Player.position + offset + Vector3.up;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Cop";
            go.transform.position = pos;
            Bootstrap.MaterialFactory.Paint(go, copColor);
            var npc = go.AddComponent<CityNPC>();
            npc.role = CityNPC.Role.Cop;
            cops.Add(npc);
        }
    }
}
