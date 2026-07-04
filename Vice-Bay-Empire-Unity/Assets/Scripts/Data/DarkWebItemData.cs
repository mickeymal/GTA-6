using UnityEngine;

namespace ViceBayEmpire.Data
{
    public enum DarkWebCategory { Weapon, Drug, FraudTool, Service, Counterfeit, Data, Contract }

    /// <summary>
    /// A listing on the Dark Web marketplace. Vendors are semi-anonymous; some are
    /// scammers. Purchases cost crypto and can raise federal attention / heat.
    /// </summary>
    [CreateAssetMenu(menuName = "ViceBay/DarkWeb Item", fileName = "DW_")]
    public class DarkWebItemData : ScriptableObject
    {
        public string id;
        public string listingTitle;
        [TextArea] public string description;
        public DarkWebCategory category;
        public Sprite thumbnail;

        [Header("Vendor")]
        public string vendorAlias = "Gh0stMarket";
        [Range(0f, 1f)] public float vendorTrust = 0.8f;     // lower = more scam risk
        [Range(0f, 1f)] public float baseScamChance = 0.1f;

        [Header("Cost (in ViceCoin) & risk")]
        public double priceVC = 1.0;
        public int federalAttention = 5;                     // added on purchase
        public float wantedHeatOnDelivery = 0f;              // physical goods can get intercepted

        [Header("Payload — what you actually receive")]
        // exactly one of these is used depending on category
        public WeaponData weaponReward;
        public string drugId;                                // matches DrugData.id
        public string fraudToolId;                           // enables a fraud minigame/business
        public long cashPayout;                              // e.g. bank-cred dumps yield cash
        public double cryptoPayout;
        public int quantity = 1;

        [Header("Availability")]
        public int minReputation = 0;                        // gated behind DW reputation
        public bool oneTimePurchase = false;
    }
}
