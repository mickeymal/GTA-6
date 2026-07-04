using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;
using ViceBayEmpire.Audio;
using ViceBayEmpire.Property;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Story + tutorial mission engine. Runs an ordered chain of MissionData, driving
    /// one objective at a time, showing markers/HUD text (via GameHUD polling), firing
    /// dialogue briefings and choices, handling timers/fail/retry, and paying rewards
    /// (cash + reputation). Tutorial missions auto-chain; story missions are started at
    /// a giver beacon. Progress persists through SaveCoordinator.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public List<MissionData> missions = new();
        public int Reputation { get; private set; }
        public readonly HashSet<string> completed = new();

        MissionData active;
        int nextIndex;              // index of the next mission to offer/start
        int objIndex;
        float objTimer = -1f;       // fail countdown (>0 active)
        float holdAccum;            // for HoldPosition
        int propBaseline;
        bool dialogueDone;

        CityNPC killTarget;
        DriveableVehicle stealTarget;
        GameObject beacon;          // world objective beacon
        MissionGiver activeGiver;

        WantedSystem Wanted => FindObjectOfType<WantedSystem>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnEnable() => GameEvents.PlayerDied += OnPlayerDied;
        void OnDisable() => GameEvents.PlayerDied -= OnPlayerDied;
        void OnPlayerDied() { if (active != null) FailMission("You were wasted"); }

        // ---- public HUD accessors -------------------------------------------
        public bool Active => active != null;
        public string MissionTitle => active != null ? active.title : null;
        public string ObjectiveText
        {
            get
            {
                if (active == null) return null;
                var o = active.objectives[objIndex];
                string t = string.IsNullOrEmpty(o.text) ? o.type.ToString() : o.text;
                if (objTimer > 0f) t += $"   ⏱ {Mathf.CeilToInt(objTimer)}s";
                if (o.type == ObjectiveType.HoldPosition)
                    t += $"   {Mathf.CeilToInt(Mathf.Max(0, HoldGoal(o) - holdAccum))}s";
                return t;
            }
        }
        public Vector3? Marker { get; private set; }

        // ---- lifecycle -------------------------------------------------------
        public void Init(List<MissionData> list)
        {
            missions = list;
            nextIndex = 0;
        }

        public void StartGame()
        {
            // resume at the first not-completed mission
            nextIndex = 0;
            while (nextIndex < missions.Count && completed.Contains(missions[nextIndex].id)) nextIndex++;
            if (nextIndex < missions.Count)
            {
                if (missions[nextIndex].isTutorial) StartMission(nextIndex);
                else SpawnGiverForNext();
            }
        }

        public void StartMission(int index)
        {
            if (index < 0 || index >= missions.Count) return;
            ClearGiver();
            active = missions[index];
            nextIndex = index;
            objIndex = 0;
            AudioManager.Instance?.PlayCue("mission_start");
            RunBriefing(active);
            GameEvents.RaiseNotify($"MISSION: {active.title}", NotifyType.Success);
            BeginObjective();
        }

        void RunBriefing(MissionData m)
        {
            // barks: "Speaker|Line"
            foreach (var b in m.briefing)
            {
                int bar = b.IndexOf('|');
                if (bar > 0) DialogueSystem.Instance?.Say(b.Substring(0, bar), b.Substring(bar + 1), 1f);
            }
        }

        void BeginObjective()
        {
            var o = active.objectives[objIndex];
            objTimer = (o.timeLimit > 0f && o.type != ObjectiveType.HoldPosition) ? o.timeLimit : -1f;
            holdAccum = 0f;
            dialogueDone = false;
            killTarget = null;
            stealTarget = null;
            Marker = Markerless(o.type) ? (Vector3?)null : o.position;

            switch (o.type)
            {
                case ObjectiveType.Kill when o.spawnTargetGang:
                    killTarget = SpawnTarget(o.position, o.targetHealth);
                    break;
                case ObjectiveType.StealVehicle:
                    stealTarget = Bootstrap.VehicleFactory.Build(ParseMode(o.vehicleClass), o.position,
                        new Color(0.9f, 0.85f, 0.2f), "Target " + o.vehicleClass);
                    break;
                case ObjectiveType.BuyAnyProperty:
                    propBaseline = PropertyManager.Instance != null ? PropertyManager.Instance.owned.Count : 0;
                    break;
                case ObjectiveType.OpenDarkWeb: MenuSystem.OpenedDarkWeb = false; break;
                case ObjectiveType.OpenBusiness: MenuSystem.OpenedBusiness = false; break;
                case ObjectiveType.DialogueChoice: RunChoice(o); break;
            }
            UpdateBeacon();
        }

        void Update()
        {
            if (active == null) return;
            var o = active.objectives[objIndex];

            if (objTimer > 0f)
            {
                objTimer -= Time.deltaTime;
                if (objTimer <= 0f) { FailMission("Out of time"); return; }
            }

            // marker follows moving targets
            if (killTarget != null && !killTarget.IsDead) Marker = killTarget.transform.position;
            else if (stealTarget != null) Marker = stealTarget.transform.position;
            else Marker = Markerless(o.type) ? (Vector3?)null : o.position;
            UpdateBeacon();

            if (CheckObjective(o)) AdvanceObjective();
        }

        bool CheckObjective(ObjectiveSpec o)
        {
            Vector3 p = PlayRefs.Player ? PlayRefs.Player.position : Vector3.zero;
            float dist = Vector3.Distance(p, o.position);
            switch (o.type)
            {
                case ObjectiveType.GoTo: return dist <= o.radius;
                case ObjectiveType.ReachOnFoot: return dist <= o.radius && !PlayRefs.InVehicle;
                case ObjectiveType.DriveToLocation:
                case ObjectiveType.Deliver: return dist <= o.radius && PlayRefs.InVehicle;
                case ObjectiveType.FlyThrough:
                    return dist <= o.radius + 6f && PlayRefs.InVehicle && PlayRefs.CurrentVehicle.Altitude > 6f;
                case ObjectiveType.EnterVehicleClass:
                    return PlayRefs.InVehicle && PlayRefs.CurrentVehicle.mode == ParseMode(o.vehicleClass);
                case ObjectiveType.StealVehicle:
                    return PlayRefs.CurrentVehicle == stealTarget && stealTarget != null;
                case ObjectiveType.Kill:
                    return killTarget == null || killTarget.IsDead;
                case ObjectiveType.Rob:
                    return NearestRobberyDone(o.position, 20f);
                case ObjectiveType.ReachWanted:
                    return Wanted != null && Wanted.stars >= o.wantedStars;
                case ObjectiveType.LoseWanted:
                    return Wanted == null || Wanted.stars == 0;
                case ObjectiveType.HoldPosition:
                    if (dist <= o.radius) holdAccum += Time.deltaTime; else holdAccum = 0f;
                    return holdAccum >= HoldGoal(o);
                case ObjectiveType.DialogueChoice: return dialogueDone;
                case ObjectiveType.OpenDarkWeb: return MenuSystem.OpenedDarkWeb;
                case ObjectiveType.OpenBusiness: return MenuSystem.OpenedBusiness;
                case ObjectiveType.BuyAnyProperty:
                    return PropertyManager.Instance != null && PropertyManager.Instance.owned.Count > propBaseline;
            }
            return false;
        }

        void AdvanceObjective()
        {
            objIndex++;
            if (objIndex >= active.objectives.Count) CompleteMission();
            else { GameEvents.RaiseNotify("Objective complete", NotifyType.Success); BeginObjective(); }
        }

        void CompleteMission()
        {
            var m = active;
            completed.Add(m.id);
            GameManager.Instance.economy.AddCash(m.rewardMoney);
            Reputation += m.rewardReputation;
            AudioManager.Instance?.PlayCue("mission_pass");
            GameEvents.RaiseNotify($"MISSION PASSED: {m.title}  +${m.rewardMoney:N0} · +{m.rewardReputation} rep", NotifyType.Money);
            if (!string.IsNullOrEmpty(m.unlockNote)) GameEvents.RaiseNotify(m.unlockNote, NotifyType.Success);

            active = null;
            ClearBeacon();
            nextIndex++;
            if (nextIndex < missions.Count)
            {
                if (m.autoStartNext) StartMission(nextIndex);
                else SpawnGiverForNext();
            }
            else GameEvents.RaiseNotify("You run Vice Bay now. Story complete!", NotifyType.Success);
        }

        void FailMission(string reason)
        {
            if (active == null) return;
            AudioManager.Instance?.PlayCue("mission_fail");
            GameEvents.RaiseNotify($"MISSION FAILED: {reason}", NotifyType.Danger);
            if (killTarget) Destroy(killTarget.gameObject);
            objIndex = 0;
            objTimer = -1f;
            // retry the same mission from the start after a beat
            var m = active; active = null; ClearBeacon();
            Invoke(nameof(RetryPending), 2f);
            pendingRetry = System.Array.IndexOf(missions.ToArray(), m);
        }
        int pendingRetry = -1;
        void RetryPending() { if (pendingRetry >= 0) StartMission(pendingRetry); }

        // ---- helpers ---------------------------------------------------------
        static bool Markerless(ObjectiveType t) =>
            t is ObjectiveType.LoseWanted or ObjectiveType.ReachWanted or ObjectiveType.OpenDarkWeb
              or ObjectiveType.OpenBusiness or ObjectiveType.BuyAnyProperty or ObjectiveType.DialogueChoice;

        float HoldGoal(ObjectiveSpec o) => o.timeLimit > 0 ? o.timeLimit : 12f;

        bool NearestRobberyDone(Vector3 pos, float maxDist)
        {
            RobberyDesk best = null; float bd = maxDist;
            foreach (var r in FindObjectsOfType<RobberyDesk>())
            {
                float d = Vector3.Distance(pos, r.transform.position);
                if (d < bd) { bd = d; best = r; }
            }
            return best != null && best.state == RobberyDesk.State.Done;
        }

        CityNPC SpawnTarget(Vector3 pos, float hp)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Mission Target";
            go.transform.position = pos + Vector3.up;
            Bootstrap.MaterialFactory.Paint(go, new Color(0.9f, 0.15f, 0.2f), 0.3f, emissive: true);
            var npc = go.AddComponent<CityNPC>();
            npc.role = CityNPC.Role.Gang;
            npc.health = hp;
            return npc;
        }

        void RunChoice(ObjectiveSpec o)
        {
            var node = new DialogueSystem.Node { speaker = o.speaker, text = o.line };
            node.choices.Add(new DialogueSystem.Choice { label = o.choiceA, nextNode = null,
                onPick = () => { Reputation += o.repA; dialogueDone = true; } });
            node.choices.Add(new DialogueSystem.Choice { label = o.choiceB, nextNode = null,
                onPick = () => { Reputation += o.repB; dialogueDone = true; } });
            var dict = new Dictionary<string, DialogueSystem.Node> { { "q", node } };
            DialogueSystem.Instance?.StartTree(dict, "q", () => dialogueDone = true);
        }

        public static DriveableVehicle.Mode ParseMode(string s) => s switch
        {
            "Boat" => DriveableVehicle.Mode.Boat,
            "Plane" => DriveableVehicle.Mode.Plane,
            "Helicopter" => DriveableVehicle.Mode.Helicopter,
            _ => DriveableVehicle.Mode.Car
        };

        // ---- beacons / givers ------------------------------------------------
        void UpdateBeacon()
        {
            if (Marker == null) { ClearBeacon(); return; }
            if (beacon == null)
            {
                beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beacon.name = "ObjectiveBeacon";
                Destroy(beacon.GetComponent<Collider>());
                beacon.transform.localScale = new Vector3(1.5f, 12f, 1.5f);
                Bootstrap.MaterialFactory.Paint(beacon, new Color(1f, 0.85f, 0.2f, 1f), 0.6f, emissive: true);
            }
            beacon.transform.position = Marker.Value + Vector3.up * 12f;
        }
        void ClearBeacon() { if (beacon) Destroy(beacon); Marker = null; }

        void SpawnGiverForNext()
        {
            if (nextIndex >= missions.Count) return;
            ClearGiver();
            var m = missions[nextIndex];
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MissionGiver_" + m.id;
            go.transform.position = m.giverPosition + Vector3.up;
            go.transform.localScale = new Vector3(1.6f, 2.6f, 1.6f);
            Bootstrap.MaterialFactory.Paint(go, new Color(0.9f, 0.7f, 1f), 0.6f, emissive: true);
            activeGiver = go.AddComponent<MissionGiver>();
            activeGiver.index = nextIndex;
            activeGiver.title = m.title;
            GameEvents.RaiseNotify($"New mission available: {m.title} (purple marker)", NotifyType.Info);
        }
        void ClearGiver() { if (activeGiver) Destroy(activeGiver.gameObject); activeGiver = null; }

        // ---- save/load -------------------------------------------------------
        public List<string> CompletedIds() => new(completed);
        public void Restore(IEnumerable<string> done, int reputation)
        {
            completed.Clear();
            foreach (var id in done) completed.Add(id);
            Reputation = reputation;
        }
    }

    /// <summary>Purple beacon you press E on to begin the next story mission.</summary>
    public class MissionGiver : MonoBehaviour, Crime.IInteractable
    {
        public int index;
        public string title;
        public string Prompt => $"[E] Start mission: {title}";
        public void Interact(GameObject interactor) => MissionManager.Instance?.StartMission(index);
    }
}
