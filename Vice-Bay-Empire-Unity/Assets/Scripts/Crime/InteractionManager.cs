using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Crime
{
    /// <summary>
    /// Central raycast-based interaction driver. Each frame it probes what the player
    /// is looking at / standing near and shows a contextual prompt. Pressing the
    /// interact key routes to the right system: robbery, carjack, mugging, shop,
    /// property door, business desk, ATM (skimming), dark-web laptop, etc.
    ///
    /// Any interactable implements <see cref="IInteractable"/>; robbery targets and
    /// crime-specific objects are handled explicitly for the "aim a weapon" nuance.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public Transform player;
        public Camera cam;
        public float interactDistance = 4f;
        public LayerMask interactMask = ~0;
        public KeyCode interactKey = KeyCode.E;

        Player.PlayerCombat combat;      // to know if a weapon is aimed
        RobberyTarget nearbyRobbery;
        IInteractable nearbyInteractable;

        void Awake()
        {
            if (player) combat = player.GetComponent<Player.PlayerCombat>();
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            Probe();
            if (Input.GetKeyDown(interactKey)) TryInteract();
        }

        void Probe()
        {
            nearbyRobbery = null;
            nearbyInteractable = null;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance + 40f, interactMask))
            {
                nearbyRobbery = hit.collider.GetComponentInParent<RobberyTarget>();
                nearbyInteractable = hit.collider.GetComponentInParent<IInteractable>();
                if (nearbyInteractable != null && hit.distance <= interactDistance)
                    GameEvents.RaiseInteractionPrompt(nearbyInteractable.Prompt);
            }
        }

        void TryInteract()
        {
            bool aimed = combat != null && combat.IsAiming;

            if (nearbyRobbery != null)
            {
                nearbyRobbery.OnInteract(aimed);
                return;
            }
            if (nearbyInteractable != null)
            {
                nearbyInteractable.Interact(player.gameObject);
            }
        }
    }

    /// <summary>Anything the player can press E on.</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(GameObject interactor);
    }
}
