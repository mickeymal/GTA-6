using System;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Endgame
{
    public enum SportType { Golf, Tennis, Basketball, Triathlon, JetSkiRace, Parachute, Hunting, Yoga }

    /// <summary>
    /// Base class for the leisure/sports minigames. Each concrete sport implements
    /// scoring; this shell handles start/stop, timing, reward payout and a personal
    /// best. Attach a subclass to the sport's activity trigger in the world.
    ///
    /// Provided as an extensible framework — Golf/JetSkiRace/Parachute have sample
    /// scoring; the rest follow the same pattern (override Begin/Tick/Score).
    /// </summary>
    public abstract class SportsChallenge : MonoBehaviour, Crime.IInteractable
    {
        public SportType sport;
        public string title = "Sport";
        public long baseReward = 2000;
        public float timeLimit = 120f;

        public bool Active { get; private set; }
        protected float elapsed;
        protected int score;
        public int PersonalBest { get; private set; }

        public string Prompt => Active ? $"[E] Quit {title}" : $"[E] Play {title}";

        public void Interact(GameObject interactor)
        {
            if (Active) End(false);
            else Begin();
        }

        protected virtual void Begin()
        {
            Active = true; elapsed = 0f; score = 0;
            GameEvents.RaiseNotify($"{title} started!", NotifyType.Info);
        }

        protected virtual void Update()
        {
            if (!Active) return;
            elapsed += Time.deltaTime;
            Tick();
            if (timeLimit > 0f && elapsed >= timeLimit) End(true);
        }

        /// <summary>Per-frame gameplay for the sport (movement checks, target hits...).</summary>
        protected abstract void Tick();

        /// <summary>Final reward for the achieved score.</summary>
        protected virtual long ComputeReward() => baseReward + score * 50;

        protected void End(bool finished)
        {
            Active = false;
            if (finished)
            {
                long reward = ComputeReward();
                GameManager.Instance.economy.AddCash(reward);
                if (score > PersonalBest) { PersonalBest = score; GameEvents.RaiseNotify("New personal best!", NotifyType.Success); }
                GameEvents.RaiseNotify($"{title}: score {score}, +${reward:N0}", NotifyType.Money);
            }
            else GameEvents.RaiseNotify($"{title} abandoned.", NotifyType.Info);
        }
    }

    // ------------------------------------------------------------------ samples
    public class GolfChallenge : SportsChallenge
    {
        public int holes = 9;
        int currentHole;
        protected override void Begin() { base.Begin(); currentHole = 0; sport = SportType.Golf; title = "Golf"; }
        protected override void Tick()
        {
            // stroke input: LMB to putt; lower strokes = higher score (par scoring)
            if (Input.GetMouseButtonDown(0))
            {
                int strokes = UnityEngine.Random.Range(2, 6);   // replace with aim/power meter
                score += Mathf.Max(1, 6 - strokes);
                if (++currentHole >= holes) End(true);
            }
        }
    }

    public class JetSkiRace : SportsChallenge
    {
        public Transform[] buoys;
        int next;
        public Vehicles.VehicleController requiredCraft;
        protected override void Begin() { base.Begin(); next = 0; title = "Jet Ski Race"; timeLimit = 180f; }
        protected override void Tick()
        {
            if (buoys == null || next >= buoys.Length) return;
            var player = GameObject.FindWithTag("Player");
            if (player && Vector3.Distance(player.transform.position, buoys[next].position) < 6f)
            {
                next++; score += 10;
                GameEvents.RaiseNotify($"Buoy {next}/{buoys.Length}", NotifyType.Info);
                if (next >= buoys.Length) End(true);
            }
        }
    }

    public class ParachuteJump : SportsChallenge
    {
        public Transform landingZone;
        protected override void Begin() { base.Begin(); title = "Parachute Jump"; timeLimit = 0f; }
        protected override void Tick()
        {
            var player = GameObject.FindWithTag("Player");
            if (player && player.transform.position.y < 2f)
            {
                float dist = landingZone ? Vector3.Distance(player.transform.position, landingZone.position) : 99f;
                score = Mathf.Max(0, 100 - Mathf.RoundToInt(dist));
                End(true);
            }
        }
    }
}
