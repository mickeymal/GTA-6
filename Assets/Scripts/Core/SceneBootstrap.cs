using UnityEngine;
using ViceBayEmpire.UI;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Place in the city scene. On load it either starts a new game or restores a save
    /// depending on how the player entered from the menu (flag on GameManager). Also
    /// gives the player a starting pistol on a new game and wires the pause toggle.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        public static bool LoadFromSave;      // set by MainMenu before loading the scene

        public Player.PlayerLoadout playerLoadout;
        public Data.WeaponData startingPistol;
        public GameObject explosionVfx;

        void Start()
        {
            if (explosionVfx) Explosions.ExplosionVfxPrefab = explosionVfx;

            if (LoadFromSave && SaveCoordinator.Instance != null && SaveCoordinator.Instance.Load())
            {
                // restored
            }
            else
            {
                GameManager.Instance.StartNewGame();
                if (playerLoadout && startingPistol)
                    playerLoadout.GiveWeapon(startingPistol, startingPistol.magazineSize * 4);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) GameManager.Instance.TogglePause();
        }
    }
}
