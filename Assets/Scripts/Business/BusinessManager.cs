using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Business
{
    /// <summary>
    /// Owns the player's business empire. Handles purchasing, per-day income ticks
    /// (driven by WorldClock's NewDay event), upgrades, staff, resupply, sale runs,
    /// and raid defense. Bound to the management UI (BusinessUI).
    /// </summary>
    public class BusinessManager : MonoBehaviour
    {
        public static BusinessManager Instance { get; private set; }

        [Header("All businesses that exist in the world")]
        public List<BusinessData> availableBusinesses = new();

        public List<BusinessInstance> owned = new();

        EconomyManager Economy => GameManager.Instance.economy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnEnable() => GameEvents.NewDay += OnNewDay;
        void OnDisable() => GameEvents.NewDay -= OnNewDay;

        public bool Owns(string businessId) => owned.Exists(b => b.data.id == businessId);

        public BusinessInstance Get(string businessId) => owned.Find(b => b.data.id == businessId);

        // --------------------------------------------------------------- purchase
        public bool Purchase(BusinessData data)
        {
            if (Owns(data.id)) return false;
            if (!Economy.Spend(data.purchaseCost))
            {
                GameEvents.RaiseNotify("Can't afford that business.", NotifyType.Warning);
                return false;
            }
            owned.Add(new BusinessInstance(data));
            GameEvents.RaiseNotify($"Acquired {data.displayName}!", NotifyType.Success);
            return true;
        }

        // --------------------------------------------------------------- management
        public bool Upgrade(string businessId)
        {
            var b = Get(businessId);
            if (b == null || !b.CanUpgrade) return false;
            if (!Economy.Spend(b.NextUpgradeCost)) return false;
            b.Upgrade();
            GameEvents.RaiseNotify($"{b.data.displayName} upgraded to level {b.upgradeLevel}.", NotifyType.Success);
            return true;
        }

        public bool HireStaff(string businessId, int count)
        {
            var b = Get(businessId);
            if (b == null) return false;
            b.staff = Mathf.Clamp(b.staff + count, 0, b.data.maxStaff);
            return true;
        }

        public bool Resupply(string businessId, float feeMultiplier = 1f)
        {
            var b = Get(businessId);
            if (b == null || b.data.isLegal) return false;
            long cost = (long)(b.data.purchaseCost * 0.05f * feeMultiplier);
            if (!Economy.Spend(cost)) { GameEvents.RaiseNotify("Can't afford supplies.", NotifyType.Warning); return false; }
            b.Resupply();
            GameEvents.RaiseNotify($"{b.data.displayName} resupplied.", NotifyType.Success);
            return true;
        }

        /// <summary>Do a sale run for an illicit business; proceeds are dirty cash.</summary>
        public long SellProduct(string businessId)
        {
            var b = Get(businessId);
            if (b == null) return 0;
            long earned = b.SellProduct();
            if (earned > 0)
            {
                Economy.AddCash(earned, dirty: true);
                GameEvents.RaiseNotify($"Sale run complete: ${earned:N0} (dirty). Watch the heat.", NotifyType.Money);
            }
            else GameEvents.RaiseNotify("No product to sell.", NotifyType.Info);
            return earned;
        }

        public void ResolveRaid(string businessId, bool defended) => Get(businessId)?.ResolveRaid(defended);

        // --------------------------------------------------------------- daily tick
        void OnNewDay(int day)
        {
            long net = 0;
            foreach (var b in owned) net += b.TickDay();
            if (net > 0) Economy.AddCash(net);
            else if (net < 0) Economy.SpendUpTo(-net);

            if (owned.Count > 0)
                GameEvents.RaiseNotify($"Empire net income today: ${net:N0}", net >= 0 ? NotifyType.Money : NotifyType.Warning);
        }

        public long TotalDailyProjection()
        {
            long sum = 0;
            foreach (var b in owned)
                sum += b.data.isLegal ? b.data.baseDailyIncome * b.data.IncomeMultiplierForLevel(b.upgradeLevel)
                                      : b.storedProduct * b.data.goodsSaleValue;
            return sum;
        }
    }
}
