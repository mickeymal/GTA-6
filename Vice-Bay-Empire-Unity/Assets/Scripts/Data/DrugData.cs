using UnityEngine;

namespace ViceBayEmpire.Data
{
    /// <summary>
    /// Consumable drug with temporary player effects and a resale economy for the
    /// drug-lab business. Effects are applied by PlayerStatus.
    /// </summary>
    [CreateAssetMenu(menuName = "ViceBay/Drug", fileName = "Drug_")]
    public class DrugData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;

        [Header("Effect while active")]
        public float duration = 30f;
        public float speedMultiplier = 1f;
        public float damageResistance = 0f;      // 0..1 fraction absorbed
        public float healthRegenPerSecond = 0f;
        public float timeScaleEffect = 1f;       // <1 = bullet-time feel
        [Range(0f, 1f)] public float screenDistortion = 0f;
        public float overdoseChance = 0f;        // per use

        [Header("Economy (for the drug business)")]
        public long buyPricePerUnit = 100;       // wholesale
        public long streetValuePerUnit = 400;    // retail
    }
}
