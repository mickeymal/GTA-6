using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Player
{
    /// <summary>Runtime weapon slot: ammo, attachments, and reload state.</summary>
    [System.Serializable]
    public class WeaponSlot
    {
        public WeaponData data;
        public int magAmmo;
        public int reserveAmmo;
        public bool suppressor, scope, extendedMag;

        public int MagSize => extendedMag ? Mathf.RoundToInt(data.magazineSize * 1.5f) : data.magazineSize;
        public float Spread => scope ? data.spreadDegrees * 0.6f : data.spreadDegrees;

        public WeaponSlot(WeaponData d, int reserve)
        {
            data = d; reserveAmmo = reserve; magAmmo = d.magazineSize;
        }

        public void Reload()
        {
            int need = MagSize - magAmmo;
            int take = Mathf.Min(need, reserveAmmo);
            magAmmo += take; reserveAmmo -= take;
        }
    }

    /// <summary>
    /// The player's owned weapons, organized by category for the weapon wheel.
    /// PlayerCombat reads the selected slot; shops and Dark Web add weapons here.
    /// </summary>
    public class PlayerLoadout : MonoBehaviour
    {
        public List<WeaponSlot> weapons = new();
        public int currentIndex;

        public WeaponSlot Current => weapons.Count > 0 ? weapons[Mathf.Clamp(currentIndex, 0, weapons.Count - 1)] : null;

        public void GiveWeapon(WeaponData data, int ammo)
        {
            var existing = weapons.Find(w => w.data == data);
            if (existing != null) { existing.reserveAmmo += ammo; }
            else { weapons.Add(new WeaponSlot(data, ammo)); }
            GameEvents.RaiseNotify($"Acquired {data.displayName}.", NotifyType.Success);
        }

        public void AddAmmo(WeaponData data, int amount)
        {
            var s = weapons.Find(w => w.data == data);
            if (s != null) s.reserveAmmo += amount;
        }

        public void Select(int index)
        {
            if (index >= 0 && index < weapons.Count) currentIndex = index;
        }

        public void SelectByCategory(WeaponCategory cat)
        {
            int idx = weapons.FindIndex(w => w.data.category == cat);
            if (idx >= 0) currentIndex = idx;
        }

        public void Cycle(int dir)
        {
            if (weapons.Count == 0) return;
            currentIndex = (currentIndex + dir + weapons.Count) % weapons.Count;
        }

        public bool ApplyAttachment(string type)
        {
            var s = Current; if (s == null) return false;
            switch (type)
            {
                case "suppressor": if (!s.data.allowSuppressor) return false; s.suppressor = true; break;
                case "scope": if (!s.data.allowScope) return false; s.scope = true; break;
                case "extendedMag": if (!s.data.allowExtendedMag) return false; s.extendedMag = true; break;
                default: return false;
            }
            GameEvents.RaiseNotify($"{type} fitted to {s.data.displayName}.", NotifyType.Success);
            return true;
        }
    }
}
