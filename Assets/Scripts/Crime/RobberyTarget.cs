using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Crime
{
    public enum RobberyKind { Store, Bank }

    /// <summary>
    /// A robbable location (convenience store, gas station, or full bank). Attach to
    /// the register/teller trigger volume. When the player aims a weapon at the teller
    /// and issues the "hands up" command, this runs the full interaction:
    /// intimidation → compliance (cash handed over) or resistance (alarm) → optional
    /// vault (banks) → escalating wanted level → getaway.
    ///
    /// Compliance depends on how forcefully the player intimidates (fire warning shots
    /// to raise fear). A brave teller may hit the silent alarm instead of complying.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RobberyTarget : MonoBehaviour
    {
        [Header("Config")]
        public RobberyKind kind = RobberyKind.Store;
        public string locationName = "Rob's Liquor";
        public Transform teller;
        public Transform vaultPoint;              // banks only
        public float registerCash = 1500f;
        public float vaultCash = 45000f;
        public float grabRadius = 3f;

        [Header("Teller behaviour")]
        [Range(0f, 1f)] public float baseBravery = 0.4f;   // chance to resist / hit alarm
        public float intimidationDecay = 0.2f;             // fear fades per second

        [Header("Runtime state (read-only)")]
        public RobberyState state = RobberyState.Idle;
        public float fear;                                 // 0..1, raised by aiming/warning shots

        public enum RobberyState { Idle, HeldUp, Complying, VaultOpen, AlarmTriggered, Complete, Failed }

        bool playerInRange;
        Transform player;
        float cooldownUntil;

        void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            player = other.transform;
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
            if (state == RobberyState.HeldUp || state == RobberyState.Complying)
            {
                // player walked off mid-holdup -> teller recovers, minor heat
                Fail("The teller called the cops after you left.");
            }
        }

        void Update()
        {
            if (!playerInRange || Time.time < cooldownUntil) return;
            if (state == RobberyState.Idle)
                GameEvents.RaiseInteractionPrompt($"Aim at the teller and press [E] to hold up {locationName}");

            if (state == RobberyState.HeldUp)
                fear = Mathf.Max(0f, fear - intimidationDecay * Time.deltaTime);
        }

        /// <summary>Call from InteractionManager when the player presses interact while aiming here.</summary>
        public void OnInteract(bool weaponAimed)
        {
            if (Time.time < cooldownUntil || state >= RobberyState.Complete) return;

            if (!weaponAimed)
            {
                GameEvents.RaiseNotify("Draw a weapon to rob this place.", NotifyType.Warning);
                return;
            }

            switch (state)
            {
                case RobberyState.Idle:
                    BeginHoldup();
                    break;
                case RobberyState.HeldUp:
                    DemandMoney();
                    break;
                case RobberyState.VaultOpen:
                    // handled by grab trigger; nothing here
                    break;
            }
        }

        /// <summary>A warning shot near the teller ratchets up fear/compliance.</summary>
        public void OnWarningShotFired()
        {
            if (state != RobberyState.HeldUp) return;
            fear = Mathf.Clamp01(fear + 0.5f);
            GameEvents.RaiseNotify("The teller flinches, terrified.", NotifyType.Info);
        }

        void BeginHoldup()
        {
            state = RobberyState.HeldUp;
            fear = 0.35f;
            GameEvents.RaiseInteractionPrompt("[E] Demand the money  ·  fire near them to intimidate");
            GameEvents.RaiseNotify($"\"{TellerLine("scared")}\"", NotifyType.Info);
            GameEvents.RaiseCrimeCommitted(0.2f, transform.position);   // brandishing raises minor heat
        }

        void DemandMoney()
        {
            float effectiveBravery = baseBravery * (1f - fear);
            if (Random.value < effectiveBravery)
            {
                // teller resists — silent alarm
                state = RobberyState.AlarmTriggered;
                GameEvents.RaiseNotify($"\"{TellerLine("resist")}\" — SILENT ALARM!", NotifyType.Danger);
                GameEvents.RaiseCrimeCommitted(0.7f, transform.position);
                StartCoroutine(AlarmThenAllowGrab());
            }
            else
            {
                state = RobberyState.Complying;
                GameEvents.RaiseNotify($"\"{TellerLine("comply")}\"", NotifyType.Info);
                StartCoroutine(HandOverCash());
            }
        }

        IEnumerator HandOverCash()
        {
            yield return new WaitForSeconds(1.5f);
            GameManager.Instance.economy.AddCash((long)registerCash, dirty: true);
            GameEvents.RaiseCrimeCommitted(0.5f, transform.position);

            if (kind == RobberyKind.Bank && vaultPoint != null)
            {
                state = RobberyState.VaultOpen;
                GameEvents.RaiseNotify("Vault unlocked. Grab the cash — the clock is ticking!", NotifyType.Warning);
                GameEvents.RaiseInteractionPrompt("Reach the vault before the cops arrive");
                GameEvents.RaiseCrimeCommitted(1.0f, transform.position);   // bank job = big heat
            }
            else
            {
                Complete();
            }
        }

        IEnumerator AlarmThenAllowGrab()
        {
            // even on resistance the player can still force the register open, but cops are coming
            state = RobberyState.VaultOpen;
            GameEvents.RaiseInteractionPrompt("Grab what you can and run!");
            yield return null;
        }

        void LateUpdate()
        {
            if (state == RobberyState.VaultOpen && player != null)
            {
                Vector3 grabPoint = vaultPoint != null ? vaultPoint.position : transform.position;
                if ((player.position - grabPoint).sqrMagnitude < grabRadius * grabRadius)
                {
                    float take = kind == RobberyKind.Bank ? vaultCash : registerCash;
                    GameManager.Instance.economy.AddCash((long)take, dirty: true);
                    Complete();
                }
            }
        }

        void Complete()
        {
            state = RobberyState.Complete;
            cooldownUntil = Time.time + 3600f;   // this location is cleaned out for a long while
            GameEvents.RaiseInteractionPrompt(null);
            GameEvents.RaiseNotify($"Robbery complete — now lose the cops!", NotifyType.Success);
        }

        void Fail(string reason)
        {
            state = RobberyState.Failed;
            cooldownUntil = Time.time + 60f;
            GameEvents.RaiseInteractionPrompt(null);
            GameEvents.RaiseNotify(reason, NotifyType.Danger);
            GameEvents.RaiseCrimeCommitted(0.6f, transform.position);
        }

        string TellerLine(string mood) => mood switch
        {
            "scared"  => "Please, I don't want any trouble!",
            "comply"  => "Okay okay! Take it, just don't shoot!",
            "resist"  => "You don't have the guts, kid.",
            _ => "..."
        };
    }
}
