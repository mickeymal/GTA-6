using UnityEngine;

namespace ViceBayEmpire.Data
{
    public enum PropertyType { Safehouse, Apartment, Penthouse, Mansion, Yacht, Garage }

    /// <summary>
    /// Purchasable real-estate / yacht. Properties act as save points, garages,
    /// wardrobe and (for yachts/mansions) weapon storage and party hosting.
    /// </summary>
    [CreateAssetMenu(menuName = "ViceBay/Property", fileName = "Prop_")]
    public class PropertyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public PropertyType type;
        public Sprite photo;
        public Vector3 entrancePosition;
        public Vector3 spawnInsidePosition;

        [Header("Cost & upkeep")]
        public long purchaseCost = 150000;
        public long dailyUpkeep = 200;

        [Header("Amenities")]
        public int garageSlots = 2;
        public bool hasWardrobe = true;
        public bool isSavePoint = true;
        public bool hasWeaponStorage = false;
        public bool hasHelipad = false;
        public bool canHostParties = false;      // yachts / mansions -> rep + minigames
        public long passiveIncome = 0;           // yachts rented out, etc.
    }
}
