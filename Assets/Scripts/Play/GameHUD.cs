using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// IMGUI heads-up display so it renders with zero canvas/prefab/font setup: health &
    /// armor bars, cash + ViceCoin, wanted stars, weapon + ammo, the interaction prompt,
    /// a notification feed, a top-down minimap with blips, and the vehicle speedometer.
    /// Subscribes to the shared event bus for all values.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public WantedSystem wanted;
        public Transform player;

        float hp = 100, maxHp = 100;
        long cash;
        double crypto;
        int stars;
        string prompt;
        readonly List<(string text, Color color, float dieAt)> feed = new();

        Texture2D white;
        GUIStyle label, bold, small;

        void OnEnable()
        {
            GameEvents.HealthChanged += (h, m) => { hp = h; maxHp = m; };
            GameEvents.MoneyChanged += v => cash = v;
            GameEvents.CryptoChanged += v => crypto = v;
            GameEvents.WantedChanged += s => stars = s;
            GameEvents.InteractionPrompt += p => prompt = p;
            GameEvents.Notify += OnNotify;
        }

        void OnNotify(string text, NotifyType type)
        {
            Color c = type switch
            {
                NotifyType.Success => new Color(0.4f, 0.9f, 0.45f),
                NotifyType.Warning => new Color(1f, 0.8f, 0.3f),
                NotifyType.Danger => new Color(1f, 0.35f, 0.3f),
                NotifyType.Money => new Color(0.45f, 0.95f, 0.5f),
                _ => Color.white
            };
            feed.Add((text, c, Time.unscaledTime + 4f));
            if (feed.Count > 6) feed.RemoveAt(0);
        }

        void EnsureStyles()
        {
            if (white == null)
            {
                white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
            }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                bold = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
                small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            }
        }

        void Bar(Rect r, float frac, Color fill)
        {
            var prev = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(r, white);
            GUI.color = fill; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), white);
            GUI.color = prev;
        }

        void OnGUI()
        {
            EnsureStyles();

            // ---- money + crypto ----
            bold.normal.textColor = new Color(0.5f, 0.95f, 0.55f);
            GUI.Label(new Rect(Screen.width - 220, 12, 210, 26), $"${cash:N0}", bold);
            label.normal.textColor = new Color(0.6f, 0.85f, 1f);
            GUI.Label(new Rect(Screen.width - 220, 40, 210, 20), $"{crypto:F2} VC", label);

            // ---- wanted stars ----
            bold.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(Screen.width - 220, 62, 210, 26), stars > 0 ? new string('★', stars) : "", bold);
            if (wanted != null && wanted.FederalAttention > 0)
            {
                small.normal.textColor = new Color(1f, 0.4f, 0.4f);
                GUI.Label(new Rect(Screen.width - 220, 90, 210, 18), $"FED {wanted.FederalAttention}%", small);
            }

            // ---- health / armor ----
            Bar(new Rect(14, Screen.height - 40, 200, 14), hp / maxHp, new Color(0.35f, 0.85f, 0.4f));
            if (PlayRefs.Status != null && PlayRefs.Status.armor > 0)
                Bar(new Rect(14, Screen.height - 58, 200, 12), PlayRefs.Status.armor / PlayRefs.Status.maxArmor,
                    new Color(0.5f, 0.65f, 1f));

            // ---- weapon / ammo ----
            var loadout = PlayRefs.Shooter ? PlayRefs.Shooter.GetComponent<ViceBayEmpire.Player.PlayerLoadout>() : null;
            var slot = loadout ? loadout.Current : null;
            if (slot != null && slot.data != null)
            {
                label.normal.textColor = Color.white;
                string ammo = slot.data.category == ViceBayEmpire.Data.WeaponCategory.Melee
                    ? "" : $"{slot.magAmmo}/{slot.reserveAmmo}";
                GUI.Label(new Rect(Screen.width - 220, Screen.height - 36, 210, 22), $"{slot.data.displayName}  {ammo}", label);
            }

            // ---- vehicle speed ----
            if (PlayRefs.InVehicle)
            {
                var v = PlayRefs.CurrentVehicle;
                label.normal.textColor = v.HealthPct > 0.35f ? Color.white : new Color(1f, 0.4f, 0.3f);
                string extra = (v.mode == DriveableVehicle.Mode.Plane || v.mode == DriveableVehicle.Mode.Helicopter)
                    ? $"  ALT {v.Altitude:F0}" : "";
                GUI.Label(new Rect(Screen.width / 2f - 60, Screen.height - 60, 200, 24),
                    $"{v.displayName}  {v.Speed * 3.6f:F0} km/h{extra}", bold);
            }

            // ---- interaction prompt ----
            if (!string.IsNullOrEmpty(prompt) && !PlayRefs.UIBlocking)
            {
                var s = new GUIStyle(GUI.skin.box) { fontSize = 15 };
                var size = s.CalcSize(new GUIContent(prompt));
                GUI.Box(new Rect(Screen.width / 2f - size.x / 2f - 8, Screen.height / 2f + 40, size.x + 16, 28), prompt, s);
            }

            // ---- mission banner (top center) ----
            var mm = MissionManager.Instance;
            if (mm != null)
            {
                if (mm.Active)
                {
                    var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                    titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
                    var objStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                    objStyle.normal.textColor = Color.white;
                    var box = new Rect(Screen.width / 2f - 250, 8, 500, 46);
                    GUI.color = new Color(0, 0, 0, 0.5f); GUI.DrawTexture(box, white); GUI.color = Color.white;
                    GUI.Label(new Rect(box.x, box.y + 4, box.width, 20), "◆ " + mm.MissionTitle, titleStyle);
                    GUI.Label(new Rect(box.x, box.y + 24, box.width, 20), mm.ObjectiveText, objStyle);
                }
                small.normal.textColor = new Color(1f, 0.8f, 0.4f);
                GUI.Label(new Rect(Screen.width - 220, 108, 210, 18), $"Rep {mm.Reputation}", small);
            }

            // ---- notification feed ----
            float y = 60;
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime > feed[i].dieAt) { feed.RemoveAt(i); continue; }
                label.normal.textColor = feed[i].color;
                GUI.Label(new Rect(16, y, 480, 20), feed[i].text, label);
                y += 22;
            }

            DrawMinimap();
        }

        void DrawMinimap()
        {
            if (player == null) return;
            float size = 150, pad = 14;
            var area = new Rect(Screen.width - size - pad, Screen.height - size - pad, size, size);
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(area, white); GUI.color = Color.white;

            float range = 120f;   // world units mapped to the minimap
            void Blip(Vector3 world, Color c, float r)
            {
                Vector3 d = world - player.position;
                float mx = area.center.x + d.x / range * (size / 2);
                float my = area.center.y + d.z / range * (size / 2);
                if (!area.Contains(new Vector2(mx, my))) return;
                var prev = GUI.color; GUI.color = c;
                GUI.DrawTexture(new Rect(mx - r, my - r, r * 2, r * 2), white);
                GUI.color = prev;
            }

            foreach (var v in FindObjectsOfType<DriveableVehicle>()) Blip(v.transform.position, new Color(0.7f, 0.7f, 0.75f), 2);
            foreach (var n in FindObjectsOfType<CityNPC>())
                Blip(n.transform.position, n.role == CityNPC.Role.Cop ? new Color(0.4f, 0.5f, 1f) : new Color(0.6f, 0.9f, 0.6f), 2);
            foreach (var r in FindObjectsOfType<RobberyDesk>()) Blip(r.transform.position, new Color(1f, 0.85f, 0.3f), 3);

            // mission objective marker + giver beacon
            var mm = MissionManager.Instance;
            if (mm != null && mm.Marker.HasValue) Blip(mm.Marker.Value, new Color(1f, 0.85f, 0.2f), 4);
            foreach (var g in FindObjectsOfType<MissionGiver>()) Blip(g.transform.position, new Color(0.8f, 0.5f, 1f), 4);

            Blip(player.position, Color.white, 3);
        }
    }
}
