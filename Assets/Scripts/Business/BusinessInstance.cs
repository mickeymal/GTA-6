using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Business
{
    /// <summary>
    /// A running, owned business. Holds mutable state (upgrade level, staff, supplies,
    /// stockpiled product) and computes daily income. Legal businesses pay steady safe
    /// cash; illicit ones accumulate goods the player must run to a buyer (bigger pay,
    /// but risk of being raided and drawing heat).
    /// </summary>
    [System.Serializable]
    public class BusinessInstance
    {
        public BusinessData data;
        public int upgradeLevel;
        public int staff;
        public float supply = 1f;          // 0..1, illicit production needs supplies
        public long storedProduct;         // units awaiting a sale run (illicit)
        public bool underRaid;

        public BusinessInstance(BusinessData data)
        {
            this.data = data;
            staff = Mathf.Min(2, data.maxStaff);
        }

        public long NextUpgradeCost =>
            upgradeLevel < data.upgradeCosts.Length ? data.upgradeCosts[upgradeLevel] : -1;

        public bool CanUpgrade => NextUpgradeCost >= 0;

        public void Upgrade()
        {
            if (CanUpgrade) upgradeLevel++;
        }

        /// <summary>Advance one in-game day. Returns net cash change (income minus wages).</summary>
        public long TickDay()
        {
            long wages = staff * data.staffDailyWage;
            long income = 0;

            if (data.isLegal)
            {
                income = data.baseDailyIncome * data.IncomeMultiplierForLevel(upgradeLevel)
                         * Mathf.Max(1, staff) / Mathf.Max(1, 2);
                GameEvents.RaiseBusinessIncome(data.id, income);
            }
            else
            {
                if (supply > 0f && staff > 0)
                {
                    long produced = data.producedGoodsPerDay
                                    * data.IncomeMultiplierForLevel(upgradeLevel);
                    storedProduct += produced;
                    supply = Mathf.Max(0f, supply - data.supplyConsumedPerDay);
                }
                // random raid on illicit ops
                if (!underRaid && Random.value < data.raidChancePerDay)
                {
                    underRaid = true;
                    GameEvents.RaiseNotify($"{data.displayName} is being raided! Defend it.", NotifyType.Danger);
                }
            }

            return income - wages;
        }

        /// <summary>Sell accumulated illicit product. Returns cash earned (dirty).</summary>
        public long SellProduct()
        {
            if (data.isLegal || storedProduct <= 0) return 0;
            long earned = storedProduct * data.goodsSaleValue;
            storedProduct = 0;
            GameEvents.RaiseCrimeCommitted(data.heatOnSaleRun * 0.15f, data.worldLocation);
            return earned;
        }

        public void Resupply() => supply = 1f;

        public void ResolveRaid(bool defended)
        {
            underRaid = false;
            if (!defended)
            {
                long lost = storedProduct;
                storedProduct = 0;
                supply = Mathf.Max(0f, supply - 0.5f);
                GameEvents.RaiseNotify($"Raid succeeded — lost {lost} units of product.", NotifyType.Danger);
            }
            else GameEvents.RaiseNotify($"{data.displayName} defended successfully.", NotifyType.Success);
        }
    }
}
