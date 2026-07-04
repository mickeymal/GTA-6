using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Player;

namespace ViceBayEmpire
{
    /// <summary>Static helper for radial explosion damage + force + VFX/heat.</summary>
    public static class Explosions
    {
        public static GameObject ExplosionVfxPrefab;   // assign once at bootstrap

        public static void Detonate(Vector3 center, float radius, float damage, GameObject source = null)
        {
            if (ExplosionVfxPrefab != null)
                Object.Instantiate(ExplosionVfxPrefab, center, Quaternion.identity);

            foreach (var col in Physics.OverlapSphere(center, radius))
            {
                float dist = Vector3.Distance(center, col.transform.position);
                float falloff = 1f - Mathf.Clamp01(dist / radius);

                var dmg = col.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(damage * falloff, source);

                var rb = col.attachedRigidbody;
                if (rb != null) rb.AddExplosionForce(damage * 12f, center, radius, 1.5f, ForceMode.Impulse);
            }

            GameEvents.RaiseCrimeCommitted(1.0f, center);
        }
    }
}
