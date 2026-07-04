using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;
using ViceBayEmpire.Player;

namespace ViceBayEmpire.UI
{
    /// <summary>
    /// Radial weapon wheel. Hold the wheel key to open (slows time), point with the
    /// mouse to highlight a category, release to equip. Reads the player's owned
    /// weapons from PlayerLoadout. Bind slot visuals in a real UI; this drives logic.
    /// </summary>
    public class WeaponWheelUI : MonoBehaviour
    {
        public KeyCode wheelKey = KeyCode.Tab;
        public float openTimeScale = 0.25f;
        public CanvasGroup canvasGroup;
        public PlayerLoadout loadout;

        static readonly WeaponCategory[] Order =
        {
            WeaponCategory.Melee, WeaponCategory.Pistol, WeaponCategory.SMG, WeaponCategory.Shotgun,
            WeaponCategory.Rifle, WeaponCategory.Sniper, WeaponCategory.Heavy, WeaponCategory.Thrown
        };

        bool open;

        void Update()
        {
            if (Input.GetKeyDown(wheelKey)) Open();
            else if (Input.GetKeyUp(wheelKey)) Close();

            if (open)
            {
                // scroll also cycles for quick swaps
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0f) loadout.Cycle(scroll > 0 ? 1 : -1);
            }
        }

        void Open()
        {
            open = true;
            if (canvasGroup) { canvasGroup.alpha = 1f; canvasGroup.blocksRaycasts = true; }
            Time.timeScale = openTimeScale;
        }

        void Close()
        {
            open = false;
            if (canvasGroup) { canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = false; }
            Time.timeScale = 1f;

            // equip the category the mouse points at
            Vector2 dir = (Vector2)Input.mousePosition - new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (dir.magnitude > 40f)
            {
                float ang = Mathf.Repeat(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f + 360f, 360f);
                int index = Mathf.RoundToInt(ang / (360f / Order.Length)) % Order.Length;
                loadout.SelectByCategory(Order[index]);
            }
        }
    }
}
