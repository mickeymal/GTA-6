using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Player;

namespace ViceBayEmpire.Vehicles
{
    /// <summary>
    /// Attached to the player. Handles entering/exiting vehicles and routing driving
    /// input + drive-by fire to the occupied VehicleController. Nearest-vehicle probe
    /// runs each frame so the InteractionManager can show an "enter" prompt.
    /// </summary>
    public class VehicleInteractor : MonoBehaviour
    {
        public float enterRange = 3f;
        public KeyCode enterKey = KeyCode.F;

        public VehicleController Current { get; private set; }
        PlayerController controller;
        PlayerCombat combat;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
        }

        void Update()
        {
            if (Input.GetKeyDown(enterKey))
            {
                if (Current != null) ExitVehicle();
                else TryEnterNearest();
            }
            if (Current != null) DriveInput();
        }

        void TryEnterNearest()
        {
            VehicleController best = null;
            float bestDist = enterRange;
            foreach (var v in FindObjectsOfType<VehicleController>())
            {
                if (!v.enabled) continue;
                float d = Vector3.Distance(transform.position, v.transform.position);
                if (d < bestDist) { bestDist = d; best = v; }
            }
            if (best != null) EnterVehicle(best);
        }

        public void EnterVehicle(VehicleController v)
        {
            if (v.Driver != null && v.Driver != gameObject) v.ForceEjectDriver();
            Current = v;
            v.SetDriver(gameObject);
            controller.InVehicle = true;
            // parent the player to the seat and hide the character mesh in a real project
            if (v.driverSeat) { transform.position = v.driverSeat.position; transform.SetParent(v.driverSeat); }
            GameEvents.RaiseVehicleStateChanged(true);
            GameEvents.RaiseNotify(v.Data ? v.Data.displayName : "Vehicle", NotifyType.Info);
        }

        public void ExitVehicle()
        {
            if (Current == null) return;
            if (Current.IsAircraft && Current.Altitude > 3f)
            {
                GameEvents.RaiseNotify("Can't get out mid-air!", NotifyType.Warning);
                return;
            }
            transform.SetParent(null);
            Vector3 exit = Current.exitPoint ? Current.exitPoint.position : Current.transform.position + Current.transform.right * 2f;
            controller.Teleport(exit);
            Current.SetDriver(null);
            Current.SetInput(0, 0, 0, false);
            Current = null;
            controller.InVehicle = false;
            GameEvents.RaiseVehicleStateChanged(false);
        }

        void DriveInput()
        {
            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");
            float vertical = 0f;
            if (Current.IsAircraft)
            {
                if (Input.GetKey(KeyCode.Space)) vertical += 1f;
                if (Input.GetKey(KeyCode.LeftControl)) vertical -= 1f;
            }
            bool handbrake = Input.GetKey(KeyCode.Space) && !Current.IsAircraft;
            Current.SetInput(throttle, steer, vertical, handbrake);

            // drive-by: fire toward the aim while driving (not from aircraft)
            if (Current.Data != null && Current.Data.canDriveBy && !Current.IsAircraft
                && Input.GetMouseButton(0) && Current.driveByMuzzles != null && Current.driveByMuzzles.Length > 0)
            {
                // delegates the actual shot to PlayerCombat's weapon using a muzzle override
                // (kept simple: raise heat + let combat resolve on next frame)
                GameEvents.RaiseCrimeCommitted(0.1f, transform.position);
            }
        }
    }
}
