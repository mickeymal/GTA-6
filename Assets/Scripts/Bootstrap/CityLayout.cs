using UnityEngine;

namespace ViceBayEmpire.Bootstrap
{
    /// <summary>
    /// Shared, single-source-of-truth layout constants for the Vice Bay map, so the
    /// city geometry (CityBuilder), the traffic AI (TrafficDriver) and swim/spawn logic
    /// all agree on where the roads, land and water are. Everything is in world units.
    /// </summary>
    public static class CityLayout
    {
        // grid of two-way roads; intersections are every (RoadsX[i], RoadsZ[j])
        public static readonly float[] RoadsX = { -100, -60, -20, 20, 60 };
        public static readonly float[] RoadsZ = { -80, -40, 0, 40 };
        public const float RoadWidth = 7f;

        // land occupies this rectangle (top surface at y = 0)
        public const float LandXMin = -130, LandXMax = 95, LandZMin = -100, LandZMax = 80;

        // water: ocean to the east (x > OceanX) and the bay to the north (z > BayZ)
        public const float OceanX = 95f;
        public const float BayZ = 80f;
        public const float WaterLevelY = 0.4f;

        // district anchors (used for building placement + ambient audio)
        public static readonly Vector3 Downtown = new(0, 0, 45);
        public static readonly Vector3 Slums = new(-95, 0, -20);
        public static readonly Vector3 Beach = new(85, 0, 0);
        public static readonly Vector3 Harbor = new(-20, 0, 90);
        public static readonly Vector3 Airport = new(-40, 0, -90);

        public static bool IsWater(Vector3 p) => p.x > OceanX || p.z > BayZ;

        /// <summary>Grid node nearest to a world position (snaps to the road lattice).</summary>
        public static Vector2Int NearestNode(Vector3 p)
        {
            int ix = 0; float bx = float.MaxValue;
            for (int i = 0; i < RoadsX.Length; i++) { float d = Mathf.Abs(RoadsX[i] - p.x); if (d < bx) { bx = d; ix = i; } }
            int iz = 0; float bz = float.MaxValue;
            for (int j = 0; j < RoadsZ.Length; j++) { float d = Mathf.Abs(RoadsZ[j] - p.z); if (d < bz) { bz = d; iz = j; } }
            return new Vector2Int(ix, iz);
        }

        public static Vector3 NodePos(Vector2Int n) =>
            new(RoadsX[Mathf.Clamp(n.x, 0, RoadsX.Length - 1)], 0.5f, RoadsZ[Mathf.Clamp(n.y, 0, RoadsZ.Length - 1)]);

        public static bool ValidNode(Vector2Int n) =>
            n.x >= 0 && n.x < RoadsX.Length && n.y >= 0 && n.y < RoadsZ.Length;
    }
}
