using UnityEngine;
using ViceBayEmpire.Business;
using ViceBayEmpire.Property;
using ViceBayEmpire.DarkWeb;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Gathers state from every manager into a SaveData and restores it on load.
    /// Central place so systems don't each need to know about serialization.
    /// </summary>
    public class SaveCoordinator : MonoBehaviour
    {
        public static SaveCoordinator Instance { get; private set; }

        public Transform player;
        public int slot = 0;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void Save()
        {
            var gm = GameManager.Instance;
            var eco = gm.economy;
            var data = new SaveData
            {
                cash = eco.Cash,
                crypto = eco.Crypto,
                dirtyMoney = eco.DirtyMoney,
                storyComplete = gm.storyComplete,
                mainMissionIndex = gm.mainMissionIndex,
                hour = gm.clock ? gm.clock.hour : 8f,
                day = gm.clock ? gm.clock.day : 1,
                playerPosition = player ? player.position : Vector3.zero,
                darkWebReputation = DarkWebMarketplace.Instance ? DarkWebMarketplace.Instance.reputation : 0,
                federalAttention = FindObjectOfType<WantedSystem>()?.FederalAttention ?? 0,
            };

            var pm = PropertyManager.Instance;
            if (pm != null) data.ownedProperties.AddRange(pm.owned);

            var bm = BusinessManager.Instance;
            if (bm != null)
                foreach (var b in bm.owned)
                    data.businesses.Add(new BusinessSave
                    {
                        businessId = b.data.id, upgradeLevel = b.upgradeLevel,
                        staffCount = b.staff, storedProduct = b.storedProduct, supplyPercent = b.supply
                    });

            var mm = ViceBayEmpire.Play.MissionManager.Instance;
            if (mm != null)
            {
                data.storyMissionsCompleted.AddRange(mm.CompletedIds());
                data.playerReputation = mm.Reputation;
            }

            SaveSystem.Save(data, slot);
        }

        public bool Load()
        {
            var data = SaveSystem.Load(slot);
            if (data == null) return false;

            var gm = GameManager.Instance;
            gm.economy.ResetTo(data.cash, data.crypto);
            gm.storyComplete = data.storyComplete;
            gm.mainMissionIndex = data.mainMissionIndex;
            if (gm.clock) { gm.clock.hour = data.hour; gm.clock.day = data.day; }
            if (player) player.position = data.playerPosition;

            var pm = PropertyManager.Instance;
            if (pm != null) { pm.owned.Clear(); pm.owned.AddRange(data.ownedProperties); }

            var bm = BusinessManager.Instance;
            if (bm != null)
            {
                bm.owned.Clear();
                foreach (var bs in data.businesses)
                {
                    var def = bm.availableBusinesses.Find(x => x.id == bs.businessId);
                    if (def == null) continue;
                    var inst = new BusinessInstance(def)
                    { upgradeLevel = bs.upgradeLevel, staff = bs.staffCount, storedProduct = bs.storedProduct, supply = bs.supplyPercent };
                    bm.owned.Add(inst);
                }
            }

            if (DarkWebMarketplace.Instance != null) DarkWebMarketplace.Instance.reputation = data.darkWebReputation;

            var mm = ViceBayEmpire.Play.MissionManager.Instance;
            if (mm != null) mm.Restore(data.storyMissionsCompleted, data.playerReputation);

            gm.SetState(data.storyComplete ? GameState.Endgame : GameState.Playing);
            GameEvents.RaiseNotify("Game loaded.", NotifyType.Success);
            return true;
        }
    }
}
