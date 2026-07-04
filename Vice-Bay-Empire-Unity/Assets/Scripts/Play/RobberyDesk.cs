using System.Collections;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Crime;   // IInteractable

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Interactive store/bank robbery on a teller counter. Flow:
    ///   aim a weapon + [E]  -> hold-up begins, teller is scared
    ///   [E] again           -> demand money; teller either complies or resists
    ///   fire near the teller -> raises fear so they comply (warning shots)
    ///   comply              -> register cash; banks then open a vault to reach in time
    ///   resist              -> silent alarm, cops called, grab what you can
    /// Drives the reused wanted heat via CrimeCommitted and shows state through
    /// notifications + the dialogue system for teller lines.
    /// </summary>
    public class RobberyDesk : MonoBehaviour, IInteractable
    {
        public bool isBank = false;
        public string locationName = "Store";
        public long registerCash = 1500;
        public long vaultCash = 40000;
        public Transform vaultPoint;
        [Range(0f, 1f)] public float baseBravery = 0.4f;

        public enum State { Idle, HeldUp, VaultRun, Done }
        public State state = State.Idle;
        float fear;
        float cooldownUntil;
        Transform player;

        public string Prompt => state switch
        {
            State.Idle => $"[E] Rob {locationName} (aim a weapon)",
            State.HeldUp => "[E] Demand the money  ·  fire near the teller to scare them",
            _ => null
        };

        void Update()
        {
            if (state == State.HeldUp) fear = Mathf.Max(0f, fear - 0.2f * Time.deltaTime);
            if (state == State.VaultRun && player != null)
            {
                Vector3 grab = vaultPoint ? vaultPoint.position : transform.position;
                if (Vector3.Distance(player.position, grab) < 3f)
                {
                    GameManager.Instance.economy.AddCash(isBank ? vaultCash : registerCash, dirty: true);
                    Finish();
                }
            }
        }

        public void Interact(GameObject interactor)
        {
            if (Time.time < cooldownUntil || state == State.Done) return;
            player = interactor.transform;
            bool aimed = PlayRefs.Shooter != null && PlayRefs.Shooter.IsAiming;

            if (state == State.Idle)
            {
                if (!aimed) { GameEvents.RaiseNotify("Draw a weapon first.", NotifyType.Warning); return; }
                BeginHoldup();
            }
            else if (state == State.HeldUp) Demand();
        }

        public void OnWarningShot()
        {
            if (state != State.HeldUp) return;
            fear = Mathf.Clamp01(fear + 0.5f);
            DialogueSystem.Instance?.Say("Teller", "Okay! Okay! Please don't shoot!", 1.2f);
        }

        void BeginHoldup()
        {
            state = State.HeldUp; fear = 0.35f;
            GameEvents.RaiseCrimeCommitted(0.2f, transform.position);
            DialogueSystem.Instance?.Say("Teller", "Please, I have a family, don't hurt me!", 1.2f);
        }

        void Demand()
        {
            float effectiveBravery = baseBravery * (1f - fear);
            if (Random.value < effectiveBravery)
            {
                DialogueSystem.Instance?.Say("Teller", "You don't have the nerve! (ALARM!)", 0.9f);
                GameEvents.RaiseNotify("SILENT ALARM TRIPPED!", NotifyType.Danger);
                GameEvents.RaiseCrimeCommitted(0.8f, transform.position);
                state = State.VaultRun;   // you can still force the register/vault, but cops are coming
                if (!isBank) StartCoroutine(GrabRegisterThenFinish(0.5f));
            }
            else
            {
                DialogueSystem.Instance?.Say("Teller", "Take it! Take everything, just go!", 1.1f);
                GameEvents.RaiseCrimeCommitted(0.5f, transform.position);
                if (isBank)
                {
                    state = State.VaultRun;
                    GameEvents.RaiseNotify("Vault open! Reach the cash before the cops box you in.", NotifyType.Warning);
                    GameEvents.RaiseCrimeCommitted(1.0f, transform.position);
                }
                else StartCoroutine(GrabRegisterThenFinish(1.2f));
            }
        }

        IEnumerator GrabRegisterThenFinish(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance.economy.AddCash(registerCash, dirty: true);
            Finish();
        }

        void Finish()
        {
            state = State.Done;
            cooldownUntil = Time.time + 600f;   // cleaned out for a while
            GameEvents.RaiseNotify($"{locationName} robbed — now lose the cops!", NotifyType.Success);
        }
    }
}
