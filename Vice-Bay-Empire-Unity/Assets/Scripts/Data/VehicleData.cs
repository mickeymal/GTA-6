using UnityEngine;

namespace ViceBayEmpire.Data
{
    public enum VehicleClass { Sports, Muscle, Sedan, SUV, Truck, Motorcycle, Bicycle,
                               Speedboat, Yacht, JetSki, FishingBoat, Plane, Jet, Helicopter, Emergency }

    [CreateAssetMenu(menuName = "ViceBay/Vehicle", fileName = "Vehicle_")]
    public class VehicleData : ScriptableObject
    {
        public string id;
        public string displayName;
        public VehicleClass vehicleClass;
        public GameObject prefab;
        public Sprite icon;

        [Header("Performance")]
        public float topSpeed = 55f;           // m/s
        public float acceleration = 12f;       // m/s^2
        public float braking = 20f;
        public float handling = 3f;            // steering responsiveness
        public float mass = 1400f;
        public float grip = 1f;                // 1 = full, lower = drifty

        [Header("Air/Sea")]
        public float liftSpeed = 40f;          // takeoff speed for planes
        public float stallSpeed = 28f;
        public float verticalRate = 8f;        // heli climb / plane pitch

        [Header("Durability")]
        public float maxHealth = 1000f;
        public bool canDriveBy = true;

        [Header("Economy")]
        public long price = 20000;
        public bool availableAtDealer = true;
    }
}
