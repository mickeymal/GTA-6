using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.DarkWeb;
using ViceBayEmpire.Business;
using ViceBayEmpire.Property;
using ViceBayEmpire.Endgame;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// The in-game phone/laptop: an IMGUI hub for all the meta systems so they're
    /// playable immediately with no UI prefabs. Open with [P]. Tabs: Dark Web (crypto
    /// buys with scam/heat), Businesses (buy/upgrade/sell/resupply), Properties, Fraud
    /// (phishing timing minigame, skimmer harvest, counterfeit), Stocks, and Save/Load.
    /// Also renders the weapon wheel while [Tab] is held.
    /// </summary>
    public class MenuSystem : MonoBehaviour
    {
        enum Panel { None, DarkWeb, Business, Property, Fraud, Stocks, System }
        Panel panel = Panel.None;
        Vector2 scroll;

        // fraud phishing minigame state
        bool phishing;
        float phishMarker, phishTarget, phishSpeed = 1.2f;

        EconomyManager Eco => GameManager.Instance.economy;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
                SetPanel(panel == Panel.None ? Panel.DarkWeb : Panel.None);
            if (Input.GetKeyDown(KeyCode.Escape) && panel != Panel.None)
                SetPanel(Panel.None);

            // weapon wheel holds Tab
            if (Input.GetKeyDown(KeyCode.Tab)) { wheelOpen = true; PlayRefs.UIBlocking = true; Time.timeScale = 0.25f; }
            if (Input.GetKeyUp(KeyCode.Tab)) { wheelOpen = false; if (panel == Panel.None) { PlayRefs.UIBlocking = false; Time.timeScale = 1f; } }

            if (phishing) phishMarker = Mathf.PingPong(Time.unscaledTime * phishSpeed, 1f);
        }

        void SetPanel(Panel p)
        {
            panel = p;
            bool open = p != Panel.None;
            PlayRefs.UIBlocking = open || wheelOpen;
            Time.timeScale = (open || wheelOpen) ? (open ? 0f : 0.25f) : 1f;
        }

        // ================================================================ GUI
        bool wheelOpen;

        void OnGUI()
        {
            if (wheelOpen) DrawWeaponWheel();
            if (panel == Panel.None) return;

            float w = 640, h = 460;
            var win = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
            GUI.Box(win, "  VICE PHONE");

            // tab buttons
            string[] tabs = { "Dark Web", "Business", "Property", "Fraud", "Stocks", "System" };
            for (int i = 0; i < tabs.Length; i++)
                if (GUI.Button(new Rect(win.x + 10 + i * 100, win.y + 26, 96, 26), tabs[i]))
                    panel = (Panel)(i + 1);

            GUI.Label(new Rect(win.x + 14, win.y + 58, w - 28, 22),
                $"Cash ${Eco.Cash:N0}     Crypto {Eco.Crypto:F2} VC     (1 VC ≈ ${Eco.viceCoinRate:F0})");

            var body = new Rect(win.x + 12, win.y + 84, w - 24, h - 130);
            GUILayout.BeginArea(body);
            scroll = GUILayout.BeginScrollView(scroll);
            switch (panel)
            {
                case Panel.DarkWeb: DrawDarkWeb(); break;
                case Panel.Business: DrawBusiness(); break;
                case Panel.Property: DrawProperty(); break;
                case Panel.Fraud: DrawFraud(); break;
                case Panel.Stocks: DrawStocks(); break;
                case Panel.System: DrawSystem(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            if (GUI.Button(new Rect(win.xMax - 90, win.yMax - 34, 80, 26), "Close (P)")) SetPanel(Panel.None);
        }

        void DrawDarkWeb()
        {
            var market = DarkWebMarketplace.Instance;
            if (market == null) { GUILayout.Label("Marketplace offline."); return; }

            if (GUILayout.Button("Convert $1,000 → ViceCoin")) Eco.BuyCryptoWithCash(1000);
            GUILayout.Space(6);
            GUILayout.Label($"Marketplace reputation: {market.reputation}   (higher = fewer scams)");
            GUILayout.Space(6);

            foreach (var item in market.VisibleListings())
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.BeginVertical();
                GUILayout.Label($"{item.listingTitle}  —  {item.priceVC:F2} VC");
                GUILayout.Label($"<vendor {item.vendorAlias} · trust {item.vendorTrust:P0} · +{item.federalAttention} fed>",
                    new GUIStyle(GUI.skin.label) { fontSize = 11 });
                GUILayout.EndVertical();
                if (GUILayout.Button("Buy", GUILayout.Width(70), GUILayout.Height(38)))
                    market.Buy(item);
                GUILayout.EndHorizontal();
            }
        }

        void DrawBusiness()
        {
            var bm = BusinessManager.Instance;
            if (bm == null) return;
            foreach (var def in bm.availableBusinesses)
            {
                bool owned = bm.Owns(def.id);
                GUILayout.BeginVertical("box");
                GUILayout.Label($"{def.displayName}  ({(def.isLegal ? "legal" : "illicit")})");
                if (!owned)
                {
                    if (GUILayout.Button($"Buy — ${def.purchaseCost:N0}")) bm.Purchase(def);
                }
                else
                {
                    var inst = bm.Get(def.id);
                    GUILayout.Label($"Lvl {inst.upgradeLevel} · staff {inst.staff} · supply {inst.supply:P0} · stock {inst.storedProduct}");
                    GUILayout.BeginHorizontal();
                    if (inst.CanUpgrade && GUILayout.Button($"Upgrade ${inst.NextUpgradeCost:N0}")) bm.Upgrade(def.id);
                    if (!def.isLegal && GUILayout.Button("Resupply")) bm.Resupply(def.id);
                    if (!def.isLegal && GUILayout.Button("Sell product")) bm.SellProduct(def.id);
                    if (GUILayout.Button("Hire staff")) bm.HireStaff(def.id, 1);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.Label($"Projected daily: ${bm.TotalDailyProjection():N0}");
        }

        void DrawProperty()
        {
            var pm = PropertyManager.Instance;
            if (pm == null) return;
            foreach (var prop in pm.allProperties)
            {
                bool owned = pm.Owns(prop.id);
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"{prop.displayName} ({prop.type})  {(owned ? "[OWNED]" : $"${prop.purchaseCost:N0}")}");
                if (!owned && GUILayout.Button("Buy", GUILayout.Width(70))) pm.Purchase(prop);
                else if (owned && GUILayout.Button("Set home", GUILayout.Width(90))) pm.SetActiveHome(prop.id);
                GUILayout.EndHorizontal();
            }
        }

        void DrawFraud()
        {
            var fc = FraudCenter.Instance;
            if (fc == null) return;
            GUILayout.Label("Buy fraud tools on the Dark Web to unlock these.");
            GUILayout.Space(4);

            if (!phishing)
            {
                if (GUILayout.Button("Start phishing campaign (timing minigame)"))
                { phishing = true; phishTarget = Random.Range(0.2f, 0.8f); }
            }
            else
            {
                GUILayout.Label("Stop the marker inside the target zone for a better campaign:");
                Rect bar = GUILayoutUtility.GetRect(400, 26);
                GUI.Box(bar, GUIContent.none);
                GUI.color = new Color(0.3f, 0.8f, 0.4f, 0.6f);
                GUI.DrawTexture(new Rect(bar.x + phishTarget * bar.width - 30, bar.y, 60, bar.height), Texture2D.whiteTexture);
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(bar.x + phishMarker * bar.width - 2, bar.y, 4, bar.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (GUILayout.Button("SEND", GUILayout.Width(80)))
                {
                    float quality = 1f - Mathf.Clamp01(Mathf.Abs(phishMarker - phishTarget) * 3f);
                    fc.RunPhishingCampaign(quality);
                    phishing = false;
                }
            }
            GUILayout.Space(8);
            if (GUILayout.Button("Harvest ATM skimmers")) fc.HarvestSkimmers();
            if (GUILayout.Button("Print counterfeit bills (x50)")) fc.PrintCounterfeit(50, 0.7f);
            if (GUILayout.Button("Cash out stolen account")) fc.CashOutStolenAccount("premium");
        }

        void DrawStocks()
        {
            var sm = StockMarket.Instance;
            if (sm == null) return;
            GUILayout.Label($"Portfolio value: ${sm.PortfolioValue():N0}");
            foreach (var s in sm.stocks)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"{s.symbol} {s.company}  ${s.price:F2}  (own {sm.Shares(s.symbol)})");
                if (GUILayout.Button("Buy 10", GUILayout.Width(70))) sm.Buy(s.symbol, 10);
                if (GUILayout.Button("Sell 10", GUILayout.Width(70))) sm.Sell(s.symbol, 10);
                GUILayout.EndHorizontal();
            }
        }

        void DrawSystem()
        {
            if (GUILayout.Button("Save game")) SaveCoordinator.Instance?.Save();
            if (GUILayout.Button("Load game")) SaveCoordinator.Instance?.Load();
            GUILayout.Space(10);
            GUILayout.Label("Controls:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label(
                "WASD move · Shift sprint · Space jump/climb (or swim up) · Mouse look\n" +
                "LMB shoot · RMB aim · R reload · Hold Tab weapon wheel · Alt+scroll cycle\n" +
                "F enter/exit vehicle · E interact/rob/mug/carjack · P phone · Esc close");
        }

        // ================================================================ weapon wheel
        void DrawWeaponWheel()
        {
            var loadout = PlayRefs.Shooter ? PlayRefs.Shooter.GetComponent<ViceBayEmpire.Player.PlayerLoadout>() : null;
            if (loadout == null || loadout.weapons.Count == 0) return;

            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            GUI.color = new Color(0, 0, 0, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int n = loadout.weapons.Count;
            for (int i = 0; i < n; i++)
            {
                float ang = i / (float)n * Mathf.PI * 2f - Mathf.PI / 2f;
                float x = cx + Mathf.Cos(ang) * 170 - 70;
                float y = cy + Mathf.Sin(ang) * 170 - 26;
                var slot = loadout.weapons[i];
                bool sel = i == loadout.currentIndex;
                GUI.color = sel ? new Color(1f, 0.4f, 0.7f) : Color.white;
                if (GUI.Button(new Rect(x, y, 140, 52), $"{slot.data.displayName}\n{slot.magAmmo}/{slot.reserveAmmo}"))
                    loadout.Select(i);
                GUI.color = Color.white;
            }
        }
    }
}
