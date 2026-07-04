using System;
using UnityEngine;

namespace ViceBayEmpire.Data
{
    public enum BusinessType
    {
        Nightclub, StripClub, Arcade, GunShop, CarDealership,     // front / legal
        DrugLab, CounterfeitFactory, SmugglingWarehouse, ChopShop, HackFarm  // illicit
    }

    /// <summary>
    /// Definition for a purchasable business. A running instance (upgrade level,
    /// staff, stockpile) lives in BusinessInstance; this is the immutable template.
    /// </summary>
    [CreateAssetMenu(menuName = "ViceBay/Business", fileName = "Biz_")]
    public class BusinessData : ScriptableObject
    {
        public string id;
        public string displayName;
        public BusinessType type;
        public Sprite icon;
        public Vector3 worldLocation;
        public bool isLegal;

        [Header("Purchase & upgrades")]
        public long purchaseCost = 200000;
        public long[] upgradeCosts = { 50000, 120000, 300000 };
        public int maxStaff = 8;
        public long staffDailyWage = 500;

        [Header("Production / income")]
        // legal businesses pay steady safe income; illicit ones produce goods you
        // must sell (higher payout, but sell runs can be raided / attract heat)
        public long baseDailyIncome = 3000;               // legal
        public long producedGoodsPerDay = 10;             // illicit
        public long goodsSaleValue = 5000;                // per unit when sold
        public float supplyConsumedPerDay = 0.25f;        // needs resupply
        public float raidChancePerDay = 0.05f;            // illicit only
        public int heatOnSaleRun = 2;

        public long IncomeMultiplierForLevel(int level) => 1 + level;   // simple scaling hook
    }
}
