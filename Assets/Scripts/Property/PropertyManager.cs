using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Property
{
    /// <summary>
    /// Owns the player's real estate and yachts. Properties are save points, garages,
    /// wardrobes and (yachts/mansions) weapon storage + party venues. Handles purchase,
    /// daily upkeep + passive income, and the active "current home" used for respawn.
    /// </summary>
    public class PropertyManager : MonoBehaviour
    {
        public static PropertyManager Instance { get; private set; }

        public List<PropertyData> allProperties = new();
        public List<string> owned = new();
        public string activeHomeId;

        [Header("Garage: kind ids stored per property")]
        public SerializableGarage garage = new();

        EconomyManager Economy => GameManager.Instance.economy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnEnable() => GameEvents.NewDay += OnNewDay;
        void OnDisable() => GameEvents.NewDay -= OnNewDay;

        public bool Owns(string id) => owned.Contains(id);
        public PropertyData GetData(string id) => allProperties.Find(p => p.id == id);

        public bool Purchase(PropertyData data)
        {
            if (Owns(data.id)) return false;
            if (!Economy.Spend(data.purchaseCost))
            {
                GameEvents.RaiseNotify("Can't afford this property.", NotifyType.Warning);
                return false;
            }
            owned.Add(data.id);
            if (data.isSavePoint && string.IsNullOrEmpty(activeHomeId)) activeHomeId = data.id;
            GameEvents.RaisePropertyPurchased(data.id);
            GameEvents.RaiseNotify($"Purchased {data.displayName}!", NotifyType.Success);
            return true;
        }

        public void SetActiveHome(string id)
        {
            if (Owns(id)) activeHomeId = id;
        }

        public Vector3 GetRespawnPoint()
        {
            var home = GetData(activeHomeId);
            return home != null ? home.spawnInsidePosition : Vector3.zero;
        }

        // ----- garage --------------------------------------------------------
        public bool StoreVehicle(string propertyId, string vehicleKindId)
        {
            var data = GetData(propertyId);
            if (data == null || !Owns(propertyId)) return false;
            var slots = garage.Get(propertyId);
            if (slots.Count >= data.garageSlots) { GameEvents.RaiseNotify("Garage full.", NotifyType.Warning); return false; }
            slots.Add(vehicleKindId);
            return true;
        }

        public IReadOnlyList<string> GetGarage(string propertyId) => garage.Get(propertyId);

        // ----- daily upkeep / income ----------------------------------------
        void OnNewDay(int day)
        {
            long income = 0, upkeep = 0;
            foreach (var id in owned)
            {
                var d = GetData(id);
                if (d == null) continue;
                income += d.passiveIncome;
                upkeep += d.dailyUpkeep;
            }
            if (income > 0) Economy.AddCash(income);
            if (upkeep > 0) Economy.SpendUpTo(upkeep);
            if (owned.Count > 0 && (income - upkeep) != 0)
                GameEvents.RaiseNotify($"Property ledger: ${income - upkeep:N0}", NotifyType.Info);
        }

        [System.Serializable]
        public class SerializableGarage
        {
            [System.Serializable] public class Entry { public string propertyId; public List<string> vehicles = new(); }
            public List<Entry> entries = new();

            public List<string> Get(string propertyId)
            {
                var e = entries.Find(x => x.propertyId == propertyId);
                if (e == null) { e = new Entry { propertyId = propertyId }; entries.Add(e); }
                return e.vehicles;
            }
        }
    }
}
