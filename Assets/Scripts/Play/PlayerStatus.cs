using System.Collections;
using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Player;   // IDamageable

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Player health + armor with regen, a simple physics "ragdoll" topple on death
    /// (the capsule is released to a Rigidbody and knocked over), and respawn at the
    /// active property/hospital. Implements the shared IDamageable so bullets, cars and
    /// explosions all hurt the player through one path.
    /// </summary>
    public class PlayerStatus : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f, maxArmor = 100f;
        public float health = 100f, armor = 0f;
        public float regenDelay = 6f, regenPerSecond = 10f, regenCap = 55f;
        public Vector3 respawnPoint;

        public bool IsDead { get; private set; }
        float lastHit;

        CharacterController cc;
        PlayerMovement3D movement;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement3D>();
            respawnPoint = transform.position;
        }

        void Start() => GameEvents.RaiseHealthChanged(health, maxHealth);

        void Update()
        {
            if (IsDead) return;
            if (Time.time - lastHit > regenDelay && health < regenCap)
            {
                health = Mathf.Min(regenCap, health + regenPerSecond * Time.deltaTime);
                GameEvents.RaiseHealthChanged(health, maxHealth);
            }
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead) return;
            lastHit = Time.time;
            if (armor > 0f)
            {
                float absorb = Mathf.Min(armor, amount * 0.6f);
                armor -= absorb; amount -= absorb;
            }
            health -= amount;
            GameEvents.RaiseHealthChanged(health, maxHealth);
            if (health <= 0f) StartCoroutine(Die());
        }

        public void Heal(float a) { health = Mathf.Min(maxHealth, health + a); GameEvents.RaiseHealthChanged(health, maxHealth); }
        public void AddArmor(float a) => armor = Mathf.Min(maxArmor, armor + a);

        IEnumerator Die()
        {
            IsDead = true;
            health = 0f;
            GameEvents.RaisePlayerDied();
            GameEvents.RaiseNotify("WASTED", NotifyType.Danger);

            // release to a temporary ragdoll: disable the controller, add a rigidbody, topple
            if (PlayRefs.InVehicle) PlayRefs.CurrentVehicle.Exit(force: true);
            if (movement) movement.enabled = false;
            if (cc) cc.enabled = false;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.AddForce((Random.insideUnitSphere + Vector3.up) * 4f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);

            yield return new WaitForSeconds(3.5f);

            Destroy(rb);
            transform.rotation = Quaternion.identity;
            transform.position = respawnPoint + Vector3.up;
            if (cc) cc.enabled = true;
            if (movement) movement.enabled = true;

            IsDead = false;
            health = maxHealth * 0.6f; armor = 0f;
            GameEvents.RaiseHealthChanged(health, maxHealth);
            FindObjectOfType<WantedSystem>()?.Clear();
            // small cash penalty
            GameManager.Instance.economy.SpendUpTo((long)(GameManager.Instance.economy.Cash * 0.1f));
            GameEvents.RaiseNotify("You wake up at the hospital.", NotifyType.Warning);
        }
    }
}
