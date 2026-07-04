using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.DarkWeb
{
    /// <summary>
    /// The Dark Web marketplace, reached from the in-game phone/laptop. Everything is
    /// paid in ViceCoin. Buying illicit goods raises federal attention; physical goods
    /// can be intercepted (adds wanted heat on "delivery"). Some vendors are scammers:
    /// you pay and receive nothing. Building marketplace reputation (successful, non-
    /// disputed buys) lowers scam odds and unlocks higher-tier listings.
    ///
    /// This is the manager/logic layer; a UI (DarkWebUI) binds to it via events.
    /// </summary>
    public class DarkWebMarketplace : MonoBehaviour
    {
        public static DarkWebMarketplace Instance { get; private set; }

        [Header("Catalog (assign DarkWebItemData assets)")]
        public List<DarkWebItemData> catalog = new();

        [Header("State")]
        public int reputation = 0;               // gates listings, reduces scams
        public bool isOpen;

        readonly HashSet<string> purchasedOneTime = new();
        readonly Dictionary<string, int> inventory = new();   // itemId -> qty owned

        public event Action CatalogChanged;
        public event Action<DarkWebItemData, PurchaseResult> PurchaseCompleted;

        EconomyManager Economy => GameManager.Instance.economy;
        WantedSystem Wanted => FindObjectOfType<WantedSystem>();

        public enum PurchaseResult { Delivered, Scammed, Intercepted, Failed }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        // --------------------------------------------------------------- browsing
        public IEnumerable<DarkWebItemData> VisibleListings()
        {
            foreach (var item in catalog)
            {
                if (item.minReputation > reputation) continue;
                if (item.oneTimePurchase && purchasedOneTime.Contains(item.id)) continue;
                yield return item;
            }
        }

        public void Open() { isOpen = true; GameManager.Instance.SetState(GameState.Paused); }
        public void Close() { isOpen = false; GameManager.Instance.TogglePause(); }

        // --------------------------------------------------------------- buying
        public PurchaseResult Buy(DarkWebItemData item)
        {
            if (item == null) return PurchaseResult.Failed;
            if (item.minReputation > reputation)
            {
                GameEvents.RaiseNotify("This vendor won't deal with you yet.", NotifyType.Warning);
                return PurchaseResult.Failed;
            }
            if (Economy.Crypto < item.priceVC)
            {
                GameEvents.RaiseNotify("Not enough ViceCoin.", NotifyType.Warning);
                return PurchaseResult.Failed;
            }

            Economy.SpendCrypto(item.priceVC);

            // scam check — worse for low-trust vendors, better as your rep climbs
            float scam = Mathf.Clamp01(item.baseScamChance * (1f - item.vendorTrust) * 2f - reputation * 0.01f);
            if (UnityEngine.Random.value < scam)
            {
                reputation = Mathf.Max(0, reputation - 1);
                GameEvents.RaiseNotify($"SCAMMED by {item.vendorAlias}. Coins gone.", NotifyType.Danger);
                Wanted?.AddFederalAttention(item.federalAttention / 2);
                var res = PurchaseResult.Scammed;
                PurchaseCompleted?.Invoke(item, res);
                return res;
            }

            // federal attention always rises for illicit purchases
            Wanted?.AddFederalAttention(item.federalAttention);

            // physical-goods interception risk raises street heat
            PurchaseResult result = PurchaseResult.Delivered;
            if (item.wantedHeatOnDelivery > 0f && UnityEngine.Random.value < 0.2f)
            {
                Wanted?.AddHeat(item.wantedHeatOnDelivery);
                GameEvents.RaiseNotify("Package flagged by customs — cops en route!", NotifyType.Danger);
                result = PurchaseResult.Intercepted;
            }

            GrantPayload(item);
            reputation += 1;
            if (item.oneTimePurchase) purchasedOneTime.Add(item.id);

            GameEvents.RaiseNotify($"Delivered: {item.listingTitle}", NotifyType.Success);
            PurchaseCompleted?.Invoke(item, result);
            CatalogChanged?.Invoke();
            return result;
        }

        void GrantPayload(DarkWebItemData item)
        {
            switch (item.category)
            {
                case DarkWebCategory.Weapon:
                    if (item.weaponReward != null)
                        FindObjectOfType<Player.PlayerLoadout>()?.GiveWeapon(item.weaponReward, item.weaponReward.magazineSize * 4);
                    break;

                case DarkWebCategory.Drug:
                    AddInventory(item.drugId, item.quantity);
                    break;

                case DarkWebCategory.FraudTool:
                    // unlocks a fraud minigame / boosts a business (e.g. HackFarm, Counterfeit)
                    FraudCenter.Instance?.UnlockTool(item.fraudToolId);
                    AddInventory(item.fraudToolId, item.quantity);
                    break;

                case DarkWebCategory.Data:       // card dumps / bank creds -> direct cash (risky)
                    if (item.cashPayout > 0) Economy.AddCash(item.cashPayout, dirty: true);
                    if (item.cryptoPayout > 0) Economy.AddCrypto(item.cryptoPayout);
                    break;

                case DarkWebCategory.Counterfeit:
                    AddInventory("counterfeit_bills", item.quantity);
                    break;

                case DarkWebCategory.Service:    // hacking service, laundering, etc.
                    if (item.cryptoPayout > 0) Economy.AddCrypto(item.cryptoPayout);
                    break;

                case DarkWebCategory.Contract:   // hit contract -> tracked by ContractBoard
                    ContractBoard.Instance?.AcceptContract(item.id);
                    break;
            }
        }

        // --------------------------------------------------------------- selling
        /// <summary>Sell your own produced goods (drugs, counterfeit) back to the market.</summary>
        public bool Sell(string itemId, int qty, double vcPerUnit)
        {
            if (!inventory.TryGetValue(itemId, out int have) || have < qty) return false;
            inventory[itemId] -= qty;
            Economy.AddCrypto(vcPerUnit * qty);
            Wanted?.AddFederalAttention(qty);      // moving product draws eyes
            GameEvents.RaiseNotify($"Sold {qty}x {itemId} for {vcPerUnit * qty:F2} VC", NotifyType.Money);
            return true;
        }

        // --------------------------------------------------------------- inventory
        public void AddInventory(string id, int qty)
        {
            if (string.IsNullOrEmpty(id)) return;
            inventory[id] = inventory.GetValueOrDefault(id, 0) + qty;
        }

        public int GetInventory(string id) => inventory.GetValueOrDefault(id, 0);
    }
}
