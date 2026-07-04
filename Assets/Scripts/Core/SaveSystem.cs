using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Serializable snapshot of everything the player owns and has progressed.
    /// Any system that needs persistence contributes to / restores from this.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public long cash;
        public double crypto;
        public long dirtyMoney;
        public bool storyComplete;
        public int mainMissionIndex;
        public float hour = 8f;
        public int day = 1;

        public Vector3 playerPosition;
        public float playerHealth = 100f;

        public List<string> ownedProperties = new();
        public List<string> ownedVehicles = new();
        public List<string> ownedWeapons = new();
        public List<string> ownedOutfits = new();
        public List<string> unlockedBusinesses = new();

        public List<BusinessSave> businesses = new();
        public List<string> completedStrangerMissions = new();
        public List<string> storyMissionsCompleted = new();   // mission ids finished
        public int playerReputation;                          // reputation from missions
        public List<string> collectiblesFound = new();
        public int gangTerritoriesOwned;
        public int darkWebReputation;      // higher = fewer scams, better stock
        public int federalAttention;       // parallel heat from cyber-crime
    }

    [Serializable]
    public class BusinessSave
    {
        public string businessId;
        public int upgradeLevel;
        public int staffCount;
        public long storedProduct;
        public float supplyPercent;
    }

    /// <summary>JSON save/load to persistentDataPath. Call Gather/Apply on managers.</summary>
    public static class SaveSystem
    {
        static string PathFor(int slot) =>
            System.IO.Path.Combine(Application.persistentDataPath, $"vbe_slot{slot}.json");

        public static bool SaveExists(int slot = 0) => File.Exists(PathFor(slot));

        public static void Save(SaveData data, int slot = 0)
        {
            try
            {
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(data, true));
                GameEvents.RaiseNotify("Game saved.", NotifyType.Success);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
                GameEvents.RaiseNotify("Save failed.", NotifyType.Danger);
            }
        }

        public static SaveData Load(int slot = 0)
        {
            if (!SaveExists(slot)) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot)));
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                return null;
            }
        }

        public static void Delete(int slot = 0)
        {
            if (SaveExists(slot)) File.Delete(PathFor(slot));
        }
    }
}
