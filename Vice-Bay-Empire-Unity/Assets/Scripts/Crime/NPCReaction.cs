using UnityEngine;
using UnityEngine.AI;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Crime
{
    /// <summary>
    /// Lightweight reactive state machine for ambient NPCs. Uses NavMeshAgent when
    /// present. Reacts to threats: flee, panic (flee + call police), or cower.
    /// Witnessing a serious crime can trigger a police call that adds heat.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCReaction : MonoBehaviour
    {
        public enum State { Idle, Wander, Flee, Cower, Dead }
        public State state = State.Wander;

        public float fleeSpeed = 6f;
        public float wanderSpeed = 1.6f;
        public float callPoliceChance = 0.4f;

        NavMeshAgent agent;
        Transform threat;
        float wanderTimer;
        bool hasCalledPolice;

        void Awake() => agent = GetComponent<NavMeshAgent>();

        void Update()
        {
            switch (state)
            {
                case State.Wander: Wander(); break;
                case State.Flee: Flee(); break;
            }
        }

        void Wander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f && agent.isOnNavMesh)
            {
                wanderTimer = Random.Range(3f, 7f);
                agent.speed = wanderSpeed;
                Vector3 target = transform.position + Random.insideUnitSphere * 12f;
                if (NavMesh.SamplePosition(target, out var hit, 6f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        void Flee()
        {
            if (threat == null || !agent.isOnNavMesh) { state = State.Wander; return; }
            agent.speed = fleeSpeed;
            Vector3 away = (transform.position - threat.position).normalized * 20f;
            Vector3 target = transform.position + away;
            if (NavMesh.SamplePosition(target, out var hit, 10f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        public void Flee(Transform from)
        {
            threat = from;
            state = State.Flee;
        }

        public void Panic(Transform from)
        {
            Flee(from);
            if (!hasCalledPolice && Random.value < callPoliceChance)
            {
                hasCalledPolice = true;
                GameEvents.RaiseCrimeCommitted(0.3f, transform.position);   // witness call
            }
        }

        public void OnKilled()
        {
            state = State.Dead;
            if (agent) agent.enabled = false;
            GameEvents.RaiseCrimeCommitted(0.6f, transform.position);
            // nearby NPCs panic
            foreach (var r in FindObjectsOfType<NPCReaction>())
                if (r != this && (r.transform.position - transform.position).sqrMagnitude < 900f)
                    r.Panic(transform);
        }
    }
}
