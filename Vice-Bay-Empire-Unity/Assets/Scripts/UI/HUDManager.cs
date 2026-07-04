using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.UI
{
    /// <summary>
    /// Binds UGUI elements to game events: health/armor bars, cash + crypto, wanted
    /// stars, interaction prompt, and a scrolling notification feed. Assign the UI
    /// references in the inspector. Minimap is a separate MinimapCamera render.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Vitals")]
        public Image healthBar;
        public Image armorBar;

        [Header("Money")]
        public Text cashText;
        public Text cryptoText;

        [Header("Wanted")]
        public Text wantedText;
        public Text federalText;

        [Header("Prompt & notifications")]
        public Text promptText;
        public RectTransform notificationParent;
        public Text notificationPrefab;
        public float notificationLifetime = 4f;

        readonly List<(Text text, float dieAt)> notifications = new();

        void OnEnable()
        {
            GameEvents.HealthChanged += OnHealth;
            GameEvents.MoneyChanged += OnMoney;
            GameEvents.CryptoChanged += OnCrypto;
            GameEvents.WantedChanged += OnWanted;
            GameEvents.InteractionPrompt += OnPrompt;
            GameEvents.Notify += OnNotify;
        }

        void OnDisable()
        {
            GameEvents.HealthChanged -= OnHealth;
            GameEvents.MoneyChanged -= OnMoney;
            GameEvents.CryptoChanged -= OnCrypto;
            GameEvents.WantedChanged -= OnWanted;
            GameEvents.InteractionPrompt -= OnPrompt;
            GameEvents.Notify -= OnNotify;
        }

        void OnHealth(float hp, float max)
        {
            if (healthBar) healthBar.fillAmount = Mathf.Clamp01(hp / max);
        }

        void OnMoney(long cash) { if (cashText) cashText.text = $"${cash:N0}"; }
        void OnCrypto(double vc) { if (cryptoText) cryptoText.text = $"{vc:F2} VC"; }

        void OnWanted(int stars)
        {
            if (wantedText) wantedText.text = stars > 0 ? new string('★', stars) : "";
        }

        void OnPrompt(string text)
        {
            if (!promptText) return;
            promptText.text = text ?? "";
            promptText.enabled = !string.IsNullOrEmpty(text);
        }

        void OnNotify(string text, NotifyType type)
        {
            if (!notificationPrefab || !notificationParent) { Debug.Log($"[Notify:{type}] {text}"); return; }
            var t = Instantiate(notificationPrefab, notificationParent);
            t.text = text;
            t.color = type switch
            {
                NotifyType.Success => new Color(0.4f, 0.9f, 0.45f),
                NotifyType.Warning => new Color(1f, 0.8f, 0.3f),
                NotifyType.Danger => new Color(1f, 0.35f, 0.3f),
                NotifyType.Money => new Color(0.45f, 0.9f, 0.5f),
                _ => Color.white
            };
            notifications.Add((t, Time.unscaledTime + notificationLifetime));
        }

        void Update()
        {
            for (int i = notifications.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime >= notifications[i].dieAt)
                {
                    if (notifications[i].text) Destroy(notifications[i].text.gameObject);
                    notifications.RemoveAt(i);
                }
            }

            var federal = FindObjectOfType<WantedSystem>();
            if (federalText && federal) federalText.text = federal.FederalAttention > 0 ? $"FED {federal.FederalAttention}%" : "";
        }
    }
}
