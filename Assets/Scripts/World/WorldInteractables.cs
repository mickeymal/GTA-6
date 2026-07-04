using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Crime;
using ViceBayEmpire.DarkWeb;

namespace ViceBayEmpire.World
{
    /// <summary>ATM: withdraw/deposit, and — if you own a skimmer kit — install a skimmer.</summary>
    public class ATMInteractable : MonoBehaviour, IInteractable
    {
        public string Prompt => "[E] Use ATM";
        public void Interact(GameObject interactor)
        {
            // A real build opens an ATM UI. If a skimmer kit is owned, offer to skim.
            if (FraudCenter.Instance != null && FraudCenter.Instance.HasTool("skimmer_kit"))
            {
                FraudCenter.Instance.InstallSkimmer(transform.position);
            }
            else
            {
                GameEvents.RaiseNotify("ATM: balance shown. (Buy a skimmer kit on the Dark Web to exploit it.)", NotifyType.Info);
            }
        }
    }

    /// <summary>A laptop / phone terminal that opens the Dark Web marketplace UI.</summary>
    public class DarkWebTerminal : MonoBehaviour, IInteractable
    {
        public string Prompt => "[E] Open laptop (Dark Web)";
        public void Interact(GameObject interactor)
        {
            if (DarkWebMarketplace.Instance != null) DarkWebMarketplace.Instance.Open();
            else GameEvents.RaiseNotify("No connection.", NotifyType.Warning);
        }
    }

    /// <summary>Legit shop vendor (gun store, clothing, car dealer). Opens the matching shop UI.</summary>
    public class ShopVendor : MonoBehaviour, IInteractable
    {
        public enum ShopKind { GunStore, Clothing, CarDealer, Ammunation, Barber }
        public ShopKind kind;
        public string Prompt => $"[E] Browse {kind}";
        public void Interact(GameObject interactor)
        {
            GameEvents.RaiseNotify($"{kind} opened.", NotifyType.Info);
            // hook to ShopUI.Open(kind) in a full build
        }
    }

    /// <summary>Business management desk inside an owned business.</summary>
    public class BusinessDesk : MonoBehaviour, IInteractable
    {
        public Data.BusinessData business;
        public string Prompt => business != null ? $"[E] Manage {business.displayName}" : "[E] Manage business";
        public void Interact(GameObject interactor)
        {
            if (business == null) return;
            if (!Business.BusinessManager.Instance.Owns(business.id))
            {
                if (Business.BusinessManager.Instance.Purchase(business)) { }
            }
            else GameEvents.RaiseNotify($"Managing {business.displayName}. (opens management UI)", NotifyType.Info);
        }
    }

    /// <summary>Property front door: buy it, or enter/save if owned.</summary>
    public class PropertyDoor : MonoBehaviour, IInteractable
    {
        public Data.PropertyData property;
        public string Prompt => property != null
            ? (Property.PropertyManager.Instance != null && Property.PropertyManager.Instance.Owns(property.id)
                ? $"[E] Enter {property.displayName}"
                : $"[E] Buy {property.displayName} (${property.purchaseCost:N0})")
            : "[E] Property";

        public void Interact(GameObject interactor)
        {
            var pm = Property.PropertyManager.Instance;
            if (pm == null || property == null) return;
            if (!pm.Owns(property.id)) { pm.Purchase(property); return; }
            // enter + autosave
            interactor.GetComponent<Player.PlayerController>()?.Teleport(property.spawnInsidePosition);
            SaveCoordinator.Instance?.Save();
            GameEvents.RaiseNotify("Entered home. Game saved.", NotifyType.Success);
        }
    }
}
