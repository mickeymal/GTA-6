using UnityEngine;

namespace ViceBayEmpire.Data
{
    public enum WeaponCategory { Melee, Pistol, SMG, Rifle, Shotgun, Sniper, Heavy, Thrown }

    /// <summary>Data-driven weapon definition. Create assets via the Create menu.</summary>
    [CreateAssetMenu(menuName = "ViceBay/Weapon", fileName = "Weapon_")]
    public class WeaponData : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponCategory category;
        public Sprite icon;
        public GameObject worldPrefab;         // model held in hand

        [Header("Ballistics")]
        public float damage = 20f;
        public float fireRate = 6f;            // shots per second
        public float range = 120f;
        public int magazineSize = 12;
        public float reloadTime = 1.6f;
        public float spreadDegrees = 1.5f;
        public float recoil = 1.5f;
        public int pellets = 1;                // >1 for shotguns
        public float muzzleVelocity = 400f;    // used for bullet drop on snipers
        public bool bulletDrop = false;
        public bool isExplosive = false;
        public bool automatic = false;

        [Header("Economy")]
        public long price = 500;
        public long ammoPrice = 40;

        [Header("Attachments allowed")]
        public bool allowSuppressor = true;
        public bool allowScope = false;
        public bool allowExtendedMag = true;
    }
}
