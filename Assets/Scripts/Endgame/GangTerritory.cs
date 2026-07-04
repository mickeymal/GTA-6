using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Endgame
{
    /// <summary>
    /// Territory control / gang wars. The city is divided into zones each owned by a
    /// gang. Entering a rival zone and killing enough members triggers a takeover
    /// wave; hold the zone to claim it. Owned zones pay tribute daily and can be
    /// attacked back, spawning defense missions.
    /// </summary>
    public class GangTerritory : MonoBehaviour
    {
        public static GangTerritory Instance { get; private set; }

        [Serializable]
        public class Zone
        {
            public string zoneName;
            public Bounds area;
            public string owner = "Cartel";     // "Player" once claimed
            public long dailyTribute = 1500;
            public int enemyStrength = 10;       // kills needed to trigger takeover
            [NonSerialized] public int killsInWave;
            [NonSerialized] public bool contested;
        }

        public List<Zone> zones = new();
        public GameObject gangMemberPrefab;
        public Transform player;

        public int OwnedCount { get { int c = 0; foreach (var z in zones) if (z.owner == "Player") c++; return c; } }

        EconomyManager Economy => GameManager.Instance.economy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnEnable() { GameEvents.NewDay += OnNewDay; }
        void OnDisable() { GameEvents.NewDay -= OnNewDay; }

        public Zone ZoneAt(Vector3 pos) => zones.Find(z => z.area.Contains(pos));

        /// <summary>Called when the player kills a gang member; progresses a takeover.</summary>
        public void ReportGangKill(Vector3 where, string gangKilled)
        {
            var z = ZoneAt(where);
            if (z == null || z.owner == "Player") return;
            if (!z.contested)
            {
                z.contested = true;
                z.killsInWave = 0;
                GameEvents.RaiseNotify($"Gang war started in {z.zoneName}!", NotifyType.Danger);
            }
            z.killsInWave++;
            if (z.killsInWave >= z.enemyStrength) ClaimZone(z);
        }

        void ClaimZone(Zone z)
        {
            z.owner = "Player";
            z.contested = false;
            GameEvents.RaiseNotify($"You now control {z.zoneName}! Daily tribute unlocked.", NotifyType.Success);
        }

        void OnNewDay(int day)
        {
            long tribute = 0;
            foreach (var z in zones)
            {
                if (z.owner != "Player") continue;
                tribute += z.dailyTribute;
                // rivals may try to retake a zone
                if (UnityEngine.Random.value < 0.1f)
                {
                    z.owner = "Cartel";
                    GameEvents.RaiseNotify($"{z.zoneName} was retaken! Defend it to reclaim tribute.", NotifyType.Warning);
                }
            }
            if (tribute > 0) { Economy.AddCash(tribute, dirty: true); GameEvents.RaiseNotify($"Gang tribute: ${tribute:N0}", NotifyType.Money); }
        }
    }
}
