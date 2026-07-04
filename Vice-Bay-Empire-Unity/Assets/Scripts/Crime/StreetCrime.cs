using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Crime
{
    /// <summary>
    /// Spontaneous street crime: mug a pedestrian (aim at them → they drop cash or
    /// resist), or carjack (yank a driver out). NPCs react with a shared fear model
    /// broadcast so bystanders flee/scream and may call the police.
    /// Attach to pedestrian NPC root; a driver NPC additionally references its vehicle.
    /// </summary>
    public class StreetCrime : MonoBehaviour, IInteractable
    {
        public enum Kind { Pedestrian, Driver }
        public Kind kind = Kind.Pedestrian;

        [Range(0f, 1f)] public float bravery = 0.3f;
        public long carriedCash = 150;
        public Vehicles.VehicleController drivenVehicle;   // set if this NPC is a driver

        public string Prompt => kind == Kind.Driver
            ? "[E] Carjack  (aim a weapon to intimidate)"
            : "[E] Mug  (aim a weapon to intimidate)";

        NPCReaction reaction;
        void Awake() => reaction = GetComponent<NPCReaction>();

        public void Interact(GameObject interactor)
        {
            var combat = interactor.GetComponent<Player.PlayerCombat>();
            bool aimed = combat != null && combat.IsAiming;

            if (kind == Kind.Driver) Carjack(interactor, aimed);
            else Mug(interactor, aimed);
        }

        void Mug(GameObject interactor, bool aimed)
        {
            if (!aimed)
            {
                // unarmed shove mugging — lower yield, higher resist chance
                bravery += 0.3f;
            }

            if (Random.value < bravery)
            {
                GameEvents.RaiseNotify("\"Help! Robbery!\" — they run and scream.", NotifyType.Warning);
                GameEvents.RaiseCrimeCommitted(0.35f, transform.position);
                reaction?.Flee(interactor.transform);
                BroadcastPanic(interactor.transform);
            }
            else
            {
                GameManager.Instance.economy.AddCash(carriedCash, dirty: true);
                GameEvents.RaiseNotify($"Mugged for ${carriedCash:N0}.", NotifyType.Money);
                GameEvents.RaiseCrimeCommitted(0.25f, transform.position);
                reaction?.Flee(interactor.transform);
            }
        }

        void Carjack(GameObject interactor, bool aimed)
        {
            if (drivenVehicle == null) return;

            if (!aimed && Random.value < bravery)
            {
                GameEvents.RaiseNotify("The driver floors it and speeds away!", NotifyType.Warning);
                return;
            }

            // eject NPC driver and hand the vehicle to the player
            reaction?.Flee(interactor.transform);
            var enter = interactor.GetComponent<Vehicles.VehicleInteractor>();
            drivenVehicle.ForceEjectDriver();
            enter?.EnterVehicle(drivenVehicle);
            GameEvents.RaiseNotify($"Carjacked a {drivenVehicle.Data.displayName}.", NotifyType.Info);
            GameEvents.RaiseCrimeCommitted(0.3f, transform.position);
            BroadcastPanic(interactor.transform);
        }

        void BroadcastPanic(Transform threat)
        {
            foreach (var r in FindObjectsOfType<NPCReaction>())
                if ((r.transform.position - transform.position).sqrMagnitude < 400f)
                    r.Panic(threat);
        }
    }
}
