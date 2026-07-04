using System.Collections.Generic;
using UnityEngine;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Escalating police response, 0..5 stars, driven by CrimeCommitted events.
    /// Escalation tiers mirror GTA: foot cops -> cruisers -> roadblocks/SWAT ->
    /// helicopter -> military. Heat decays when the player breaks line of sight.
    /// A separate <see cref="FederalAttention"/> track handles cyber-crime heat.
    /// </summary>
    public class WantedSystem : MonoBehaviour
    {
        [Range(0, 5)] public int stars;
        [Range(0f, 1f)] public float heat;           // progress to next star
        public int FederalAttention { get; private set; }

        public float sightRadius = 60f;
        public float decayPerSecond = 0.04f;         // when unseen
        public Transform player;

        [Header("Response prefabs (assign in inspector)")]
        public GameObject copOnFootPrefab;
        public GameObject policeCarPrefab;
        public GameObject swatPrefab;
        public GameObject policeHeliPrefab;
        public GameObject militaryPrefab;

        readonly List<Transform> activeUnits = new();
        float spawnTimer;
        float unseenTimer;

        void OnEnable() => GameEvents.CrimeCommitted += OnCrime;
        void OnDisable() => GameEvents.CrimeCommitted -= OnCrime;

        void OnCrime(float severity, Vector3 pos)
        {
            AddHeat(severity * 0.6f);
        }

        public void AddHeat(float amount)
        {
            heat += amount;
            while (heat >= 1f && stars < 5) { heat -= 1f; SetStars(stars + 1); }
            heat = Mathf.Clamp01(heat);
        }

        public void AddFederalAttention(int amount)
        {
            FederalAttention = Mathf.Clamp(FederalAttention + amount, 0, 100);
            if (FederalAttention >= 100)
                GameEvents.RaiseNotify("FBI raid incoming!", NotifyType.Danger);
        }

        void SetStars(int s)
        {
            stars = Mathf.Clamp(s, 0, 5);
            GameEvents.RaiseWantedChanged(stars);
            if (stars > 0) GameEvents.RaiseNotify($"Wanted: {new string('★', stars)}", NotifyType.Danger);
        }

        public void Clear()
        {
            stars = 0; heat = 0f;
            GameEvents.RaiseWantedChanged(0);
            foreach (var u in activeUnits) if (u) Destroy(u.gameObject);
            activeUnits.Clear();
        }

        void Update()
        {
            if (stars == 0 || player == null) return;

            activeUnits.RemoveAll(u => u == null);
            bool seen = false;
            foreach (var u in activeUnits)
                if ((u.position - player.position).sqrMagnitude < sightRadius * sightRadius) { seen = true; break; }

            if (seen) unseenTimer = 0f;
            else
            {
                unseenTimer += Time.deltaTime;
                heat -= decayPerSecond * Time.deltaTime;
                if (heat < 0f)
                {
                    heat = 0.9f;
                    SetStars(stars - 1);
                    if (stars == 0) { Clear(); GameEvents.RaiseNotify("You lost the cops.", NotifyType.Success); return; }
                }
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Mathf.Max(2.5f, 9f - stars * 1.4f);
                SpawnResponse();
            }
        }

        void SpawnResponse()
        {
            if (player == null) return;
            GameObject prefab = stars switch
            {
                1 => copOnFootPrefab,
                2 => policeCarPrefab,
                3 => swatPrefab != null ? swatPrefab : policeCarPrefab,
                4 => policeHeliPrefab,
                _ => militaryPrefab != null ? militaryPrefab : policeHeliPrefab
            };
            if (prefab == null) return;
            Vector3 offset = Random.insideUnitSphere * 80f; offset.y = 0f;
            var go = Instantiate(prefab, player.position + offset, Quaternion.identity);
            activeUnits.Add(go.transform);
        }
    }
}
