using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Crime;    // IInteractable
using ViceBayEmpire.Player;   // IDamageable

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// A capsule pedestrian that wanders, reacts to threats (flee + scream + call cops),
    /// and can be mugged or — if flagged a driver — carjacked. In "Cop" role it chases
    /// the player and shoots. Implements IInteractable (mug/carjack) and IDamageable.
    /// No NavMesh required: simple steering so it runs in a bare scene.
    /// </summary>
    public class CityNPC : MonoBehaviour, IInteractable, IDamageable
    {
        public enum Role { Civilian, Gang, Cop }
        public Role role = Role.Civilian;

        public float wanderSpeed = 1.8f, fleeSpeed = 5f, chaseSpeed = 4.5f;
        public float health = 60f;
        public long carriedCash = 120;
        public DriveableVehicle car;          // set if this NPC is a driver (carjack target)

        CharacterController cc;
        Vector3 wanderDir;
        float retargetTimer, shootTimer, chatTimer;
        bool fleeing, dead, calledCops;

        static readonly string[] CivilianLines =
        {
            "Nice weather for the beach, huh?", "You seen the prices downtown lately?",
            "Watch where you're going!", "Vice Bay never sleeps.",
            "I swear the cops are everywhere today.", "Spare some change?"
        };
        static readonly string[] GangLines =
        {
            "This is our block, keep moving.", "You lost, tourist?",
            "Don't start nothing you can't finish.", "We run these streets."
        };

        public string Prompt => car != null ? "[E] Carjack" : (role == Role.Cop ? null : "[E] Mug");

        void Awake()
        {
            // the primitive comes with a CapsuleCollider; swap it for a CharacterController
            var existing = GetComponent<Collider>();
            if (existing) Destroy(existing);
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 2f; cc.center = new Vector3(0, 1, 0); cc.radius = 0.4f;

            wanderDir = Random.insideUnitSphere; wanderDir.y = 0; wanderDir.Normalize();
            if (role == Role.Cop) health = 90f;
            else if (role == Role.Gang) health = 80f;
        }

        void Update()
        {
            if (dead || PlayRefs.Player == null) return;
            if (role == Role.Cop) CopUpdate();
            else CivilianUpdate();
        }

        void CivilianUpdate()
        {
            AmbientChatter();
            Vector3 move;
            if (fleeing)
            {
                Vector3 away = transform.position - PlayRefs.Player.position; away.y = 0;
                move = away.normalized * fleeSpeed;
                if (away.magnitude > 40f) fleeing = false;
            }
            else
            {
                retargetTimer -= Time.deltaTime;
                if (retargetTimer <= 0f)
                {
                    retargetTimer = Random.Range(2f, 5f);
                    wanderDir = Quaternion.Euler(0, Random.Range(-90f, 90f), 0) * wanderDir;
                }
                move = wanderDir * wanderSpeed;
            }
            Face(move);
            cc.SimpleMove(move);
        }

        void CopUpdate()
        {
            Vector3 to = PlayRefs.Player.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            if (dist > 3f) cc.SimpleMove(to.normalized * chaseSpeed);
            Face(to);
            shootTimer -= Time.deltaTime;
            if (dist < 30f && shootTimer <= 0f)
            {
                shootTimer = 0.9f;
                if (Random.value < 0.7f && PlayRefs.Status != null)
                {
                    PlayRefs.Status.TakeDamage(6f, gameObject);
                    Audio.AudioManager.Instance?.PlayGunshot(transform.position, false);
                }
            }
        }

        void AmbientChatter()
        {
            if (fleeing || PlayRefs.Player == null) return;
            chatTimer -= Time.deltaTime;
            if (chatTimer > 0f) return;
            chatTimer = Random.Range(8f, 16f);
            if (Vector3.Distance(transform.position, PlayRefs.Player.position) < 8f && Random.value < 0.5f)
            {
                var lines = role == Role.Gang ? GangLines : CivilianLines;
                DialogueSystem.Instance?.Say(role == Role.Gang ? "Gangster" : "Local",
                    lines[Random.Range(0, lines.Length)], role == Role.Gang ? 0.85f : 1.05f);
            }
        }

        void Face(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        }

        // ---- IInteractable: mug / carjack ----
        public void Interact(GameObject interactor)
        {
            bool aimed = PlayRefs.Shooter != null && PlayRefs.Shooter.IsAiming;
            if (car != null) Carjack(aimed);
            else Mug(aimed);
        }

        void Mug(bool aimed)
        {
            float bravery = aimed ? 0.2f : 0.5f;
            if (Random.value < bravery) { Panic(); GameEvents.RaiseCrimeCommitted(0.3f, transform.position); }
            else
            {
                GameManager.Instance.economy.AddCash(carriedCash, dirty: true);
                GameEvents.RaiseNotify($"Mugged ${carriedCash:N0}", NotifyType.Money);
                GameEvents.RaiseCrimeCommitted(0.25f, transform.position);
                fleeing = true;
            }
        }

        void Carjack(bool aimed)
        {
            if (!aimed && Random.value < 0.4f) { GameEvents.RaiseNotify("Driver speeds off!", NotifyType.Warning); return; }
            fleeing = true;
            var interactor = PlayRefs.Player.GetComponent<PlayerInteractor3D>();
            if (interactor) car.Enter(interactor);
            car = null;
            GameEvents.RaiseCrimeCommitted(0.3f, transform.position);
        }

        public void Panic()
        {
            fleeing = true;
            if (!calledCops && Random.value < 0.4f) { calledCops = true; GameEvents.RaiseCrimeCommitted(0.3f, transform.position); }
        }

        // ---- IDamageable ----
        public void TakeDamage(float amount, GameObject source = null)
        {
            if (dead) return;
            health -= amount;
            if (role == Role.Civilian) Panic();
            if (health <= 0f) Die();
        }

        void Die()
        {
            dead = true;
            GameEvents.RaiseCrimeCommitted(role == Role.Cop ? 1.0f : 0.6f, transform.position);
            GameManager.Instance.economy.AddCash(Random.Range(15, 80), dirty: true);   // loose cash drop
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);   // topple
            if (cc) cc.enabled = false;
            Destroy(gameObject, 12f);
        }
    }
}
