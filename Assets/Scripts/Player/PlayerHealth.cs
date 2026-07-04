using System.Collections;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Player
{
    /// <summary>
    /// Player health + armor with regen, damage handling, ragdoll on death, and a
    /// respawn at the active property (or the hospital) after a "wasted" delay.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        public float maxArmor = 100f;
        public float health = 100f;
        public float armor = 0f;

        [Header("Regen")]
        public float regenDelay = 6f;
        public float regenPerSecond = 8f;
        public float regenCap = 50f;       // only regen up to half (GTA-style)

        [Header("Ragdoll")]
        public Rigidbody[] ragdollBodies;
        public Collider mainCollider;
        public MonoBehaviour[] disableOnDeath;   // controller, combat, etc.

        public bool IsDead { get; private set; }
        float lastDamageTime;

        void Start()
        {
            SetRagdoll(false);
            GameEvents.RaiseHealthChanged(health, maxHealth);
        }

        void Update()
        {
            if (IsDead) return;
            if (Time.time - lastDamageTime > regenDelay && health < regenCap)
            {
                health = Mathf.Min(regenCap, health + regenPerSecond * Time.deltaTime);
                GameEvents.RaiseHealthChanged(health, maxHealth);
            }
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead) return;
            lastDamageTime = Time.time;
            if (armor > 0f)
            {
                float absorbed = Mathf.Min(armor, amount * 0.65f);
                armor -= absorbed;
                amount -= absorbed;
            }
            health -= amount;
            GameEvents.RaiseHealthChanged(health, maxHealth);
            if (health <= 0f) Die();
        }

        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
            GameEvents.RaiseHealthChanged(health, maxHealth);
        }

        public void AddArmor(float amount) => armor = Mathf.Min(maxArmor, armor + amount);

        void Die()
        {
            IsDead = true;
            health = 0f;
            SetRagdoll(true);
            foreach (var m in disableOnDeath) if (m) m.enabled = false;
            GameEvents.RaisePlayerDied();
            StartCoroutine(RespawnAfter(3.5f));
        }

        IEnumerator RespawnAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SetRagdoll(false);
            foreach (var m in disableOnDeath) if (m) m.enabled = true;

            var pm = Property.PropertyManager.Instance;
            Vector3 respawn = pm != null && !string.IsNullOrEmpty(pm.activeHomeId)
                ? pm.GetRespawnPoint() : transform.position + Vector3.up;
            GetComponent<PlayerController>()?.Teleport(respawn);

            IsDead = false;
            health = maxHealth * 0.6f;
            armor = 0f;
            GameEvents.RaiseHealthChanged(health, maxHealth);
            FindObjectOfType<WantedSystem>()?.Clear();
            GameManager.Instance.SetState(GameManager.Instance.storyComplete ? GameState.Endgame : GameState.Playing);
            GameEvents.RaiseNotify("Wasted. You wake up at home.", NotifyType.Warning);
        }

        void SetRagdoll(bool on)
        {
            if (ragdollBodies != null)
                foreach (var rb in ragdollBodies) if (rb) rb.isKinematic = !on;
            if (mainCollider) mainCollider.enabled = !on;
        }
    }

    /// <summary>Common damage interface for player, NPCs and vehicles.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject source = null);
    }
}
