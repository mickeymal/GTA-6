using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.DarkWeb
{
    /// <summary>
    /// Fraud & cyber-crime hub. Tools unlocked from the Dark Web enable activities:
    /// ATM skimming, credit-card theft, phishing campaigns, identity theft, money
    /// laundering, and counterfeit production. Each is a short skill-check minigame
    /// resolved here (a real project wires these to bespoke UI); success pays out
    /// (often dirty money) and adds federal attention. Getting caught spikes heat.
    /// </summary>
    public class FraudCenter : MonoBehaviour
    {
        public static FraudCenter Instance { get; private set; }

        readonly HashSet<string> unlockedTools = new();

        EconomyManager Economy => GameManager.Instance.economy;
        WantedSystem Wanted => FindObjectOfType<WantedSystem>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        public void UnlockTool(string toolId)
        {
            if (string.IsNullOrEmpty(toolId)) return;
            unlockedTools.Add(toolId);
            GameEvents.RaiseNotify($"Fraud tool unlocked: {toolId}", NotifyType.Success);
        }

        public bool HasTool(string toolId) => unlockedTools.Contains(toolId);

        // ------------------------------------------------------------------ ATM skimming
        /// <summary>Install a skimmer at an ATM. Later "harvest" yields cards over time.</summary>
        public void InstallSkimmer(Vector3 atmPosition)
        {
            if (!HasTool("skimmer_kit")) { NeedTool("skimmer_kit"); return; }
            var skim = new SkimmerJob { position = atmPosition, installedAt = Time.time };
            activeSkimmers.Add(skim);
            GameEvents.RaiseNotify("Skimmer installed. Return later to harvest cards.", NotifyType.Info);
        }

        readonly List<SkimmerJob> activeSkimmers = new();
        class SkimmerJob { public Vector3 position; public float installedAt; }

        public void HarvestSkimmers()
        {
            long total = 0;
            activeSkimmers.RemoveAll(s =>
            {
                float hours = (Time.time - s.installedAt) / 60f;   // 1 real min ~ 1 "hour"
                if (hours < 1f) return false;
                long cards = (long)Mathf.Min(hours, 24f);
                total += cards * UnityEngine.Random.Range(80, 260);
                // risk the skimmer was discovered
                if (UnityEngine.Random.value < 0.15f) Wanted?.AddFederalAttention(5);
                return true;
            });
            if (total > 0)
            {
                Economy.AddCash(total, dirty: true);
                Wanted?.AddFederalAttention(6);
                GameEvents.RaiseNotify($"Harvested skimmed cards: ${total:N0}", NotifyType.Money);
            }
            else GameEvents.RaiseNotify("No skimmers ready to harvest.", NotifyType.Info);
        }

        // ------------------------------------------------------------------ phishing minigame
        /// <summary>
        /// Resolve a phishing campaign. <paramref name="craftedQuality"/> 0..1 is the
        /// result of the minigame (matching a convincing template). Higher quality =
        /// more victims and bigger take, but big campaigns raise federal attention.
        /// </summary>
        public long RunPhishingCampaign(float craftedQuality)
        {
            if (!HasTool("phishing_kit")) { NeedTool("phishing_kit"); return 0; }
            craftedQuality = Mathf.Clamp01(craftedQuality);
            int victims = Mathf.RoundToInt(Mathf.Lerp(0, 40, craftedQuality) * UnityEngine.Random.Range(0.6f, 1.2f));
            long take = victims * UnityEngine.Random.Range(120, 500);
            if (take > 0)
            {
                Economy.AddCash(take, dirty: true);
                Wanted?.AddFederalAttention(Mathf.Clamp(victims / 3, 2, 30));
                GameEvents.RaiseNotify($"Phishing netted {victims} victims: ${take:N0}", NotifyType.Money);
            }
            else GameEvents.RaiseNotify("Campaign flagged as spam. No hits.", NotifyType.Warning);
            return take;
        }

        // ------------------------------------------------------------------ identity theft / accounts
        public long CashOutStolenAccount(string accountTier)
        {
            if (!HasTool("cred_stuffer")) { NeedTool("cred_stuffer"); return 0; }
            long value = accountTier switch { "premium" => 8000, "business" => 20000, _ => 2500 };
            // banks freeze some accounts
            if (UnityEngine.Random.value < 0.35f)
            {
                Wanted?.AddFederalAttention(10);
                GameEvents.RaiseNotify("Account frozen mid-transfer. Federal flag raised.", NotifyType.Danger);
                return 0;
            }
            Economy.AddCash(value, dirty: true);
            Wanted?.AddFederalAttention(8);
            GameEvents.RaiseNotify($"Drained account: ${value:N0}", NotifyType.Money);
            return value;
        }

        // ------------------------------------------------------------------ counterfeiting
        public void PrintCounterfeit(int bills, float quality)
        {
            if (!HasTool("counterfeit_plates")) { NeedTool("counterfeit_plates"); return; }
            // low-quality bills get rejected when spent; represented as dirty cash discount
            long face = bills * 100L;
            long usable = (long)(face * Mathf.Lerp(0.3f, 0.9f, Mathf.Clamp01(quality)));
            Economy.AddCash(usable, dirty: true);
            Wanted?.AddFederalAttention(bills / 20);
            GameEvents.RaiseNotify($"Printed ${face:N0} face value → ${usable:N0} passable.", NotifyType.Money);
        }

        void NeedTool(string tool) =>
            GameEvents.RaiseNotify($"You need '{tool}' from the Dark Web first.", NotifyType.Warning);
    }
}
