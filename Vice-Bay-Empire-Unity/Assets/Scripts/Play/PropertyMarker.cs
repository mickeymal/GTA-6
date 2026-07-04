using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Crime;    // IInteractable
using ViceBayEmpire.Property;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// A buyable property's front door in the world. If unowned, [E] buys it (via the
    /// reused PropertyManager). If owned, [E] enters (teleports inside), sets it as the
    /// active home/save point, and saves the game. Works with the runtime player.
    /// </summary>
    public class PropertyMarker : MonoBehaviour, IInteractable
    {
        public PropertyData property;

        public string Prompt
        {
            get
            {
                var pm = PropertyManager.Instance;
                if (property == null || pm == null) return null;
                return pm.Owns(property.id)
                    ? $"[E] Enter {property.displayName} (sets home + saves)"
                    : $"[E] Buy {property.displayName} — ${property.purchaseCost:N0}";
            }
        }

        public void Interact(GameObject interactor)
        {
            var pm = PropertyManager.Instance;
            if (pm == null || property == null) return;

            if (!pm.Owns(property.id)) { pm.Purchase(property); return; }

            pm.SetActiveHome(property.id);
            if (PlayRefs.Status != null) PlayRefs.Status.respawnPoint = property.spawnInsidePosition;
            PlayRefs.Movement?.Teleport(property.spawnInsidePosition + Vector3.up);
            SaveCoordinator.Instance?.Save();
            GameEvents.RaiseNotify($"Home set: {property.displayName}. Game saved.", NotifyType.Success);
        }
    }
}
