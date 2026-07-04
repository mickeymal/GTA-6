using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Crime;   // IInteractable

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Runtime interaction: raycasts from the camera / probes nearby for anything the
    /// player can act on (robbery desks, shops, ATMs, dark-web laptops, NPCs to mug or
    /// carjack, business desks) and shows a prompt. E interacts; F enters/exits the
    /// nearest vehicle. Passes the aiming state to interactables that care (robbery).
    /// </summary>
    public class PlayerInteractor3D : MonoBehaviour
    {
        public Camera cam;
        public float reach = 3.5f;
        public float vehicleReach = 4f;
        public LayerMask mask = ~0;

        IInteractable current;

        void Awake() { if (cam == null) cam = Camera.main; }

        void Update()
        {
            if (PlayRefs.UIBlocking) { GameEvents.RaiseInteractionPrompt(null); return; }

            if (Input.GetKeyDown(KeyCode.F)) ToggleVehicle();

            current = FindInteractable(out string prompt);
            if (PlayRefs.InVehicle) prompt = "[F] Exit vehicle";
            GameEvents.RaiseInteractionPrompt(prompt);

            if (Input.GetKeyDown(KeyCode.E) && current != null && !PlayRefs.InVehicle)
                current.Interact(gameObject);
        }

        IInteractable FindInteractable(out string prompt)
        {
            prompt = null;
            // 1) look at
            Ray ray = new(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, reach + 30f, mask, QueryTriggerInteraction.Collide))
            {
                var it = hit.collider.GetComponentInParent<IInteractable>();
                if (it != null && hit.distance <= reach + 6f) { prompt = it.Prompt; return it; }
            }
            // 2) proximity fallback (so you don't have to aim precisely at NPCs)
            IInteractable best = null; float bestD = reach + 2f;
            foreach (var col in Physics.OverlapSphere(transform.position, reach + 2f, mask, QueryTriggerInteraction.Collide))
            {
                var it = col.GetComponentInParent<IInteractable>();
                if (it == null) continue;
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < bestD) { bestD = d; best = it; prompt = it.Prompt; }
            }
            return best;
        }

        void ToggleVehicle()
        {
            if (PlayRefs.InVehicle) { PlayRefs.CurrentVehicle.Exit(); return; }
            DriveableVehicle best = null; float bestD = vehicleReach;
            foreach (var v in FindObjectsOfType<DriveableVehicle>())
            {
                float d = Vector3.Distance(transform.position, v.transform.position);
                if (d < bestD + v.enterRadius) { bestD = d; best = v; }
            }
            best?.Enter(this);
        }
    }
}
