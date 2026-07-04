using UnityEngine;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Tiny static registry so runtime-spawned systems (NPCs, police, camera, menus)
    /// can find the player without inspector wiring. Set once by GameBootstrap.
    /// </summary>
    public static class PlayRefs
    {
        public static Transform Player;
        public static PlayerStatus Status;
        public static PlayerMovement3D Movement;
        public static PlayerShooter Shooter;
        public static DriveableVehicle CurrentVehicle;
        public static Camera Cam;

        /// <summary>True while any full-screen menu/dialogue owns the cursor and pauses play input.</summary>
        public static bool UIBlocking;

        public static bool InVehicle => CurrentVehicle != null;
    }
}
