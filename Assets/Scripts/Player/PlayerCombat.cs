using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Player
{
    /// <summary>
    /// Handles aiming (ADS), firing (hitscan with spread/recoil, shotgun pellets,
    /// sniper bullet-drop via a short ballistic step), reloading, and drive-by fire
    /// while in a vehicle. Raises CrimeCommitted on gunfire near NPCs.
    /// Robbery warning-shots are detected and forwarded to the aimed RobberyTarget.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        public Camera aimCamera;
        public Transform muzzlePoint;
        public LayerMask hitMask = ~0;
        public GameObject muzzleFlashPrefab;
        public GameObject impactPrefab;

        public bool IsAiming { get; private set; }
        float nextFireTime;
        float reloadFinishTime;

        PlayerLoadout loadout;
        PlayerController controller;

        void Awake()
        {
            loadout = GetComponent<PlayerLoadout>();
            controller = GetComponent<PlayerController>();
            if (aimCamera == null) aimCamera = Camera.main;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Paused) return;

            IsAiming = Input.GetMouseButton(1);
            var slot = loadout.Current;
            if (slot == null || slot.data.category == WeaponCategory.Melee)
            {
                if (Input.GetMouseButtonDown(0)) Melee();
                return;
            }

            if (Time.time < reloadFinishTime) return;
            if (Input.GetKeyDown(KeyCode.R)) StartReload(slot);

            bool wantFire = slot.data.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (wantFire && Time.time >= nextFireTime) Fire(slot);
        }

        void Fire(WeaponSlot slot)
        {
            if (slot.magAmmo <= 0) { StartReload(slot); return; }
            nextFireTime = Time.time + 1f / slot.data.fireRate;
            slot.magAmmo--;

            Vector3 origin = muzzlePoint ? muzzlePoint.position : aimCamera.transform.position;
            for (int i = 0; i < Mathf.Max(1, slot.data.pellets); i++)
            {
                Vector3 dir = SpreadDir(aimCamera.transform.forward, slot.Spread * (IsAiming ? 0.4f : 1f));
                ResolveShot(origin, dir, slot);
            }

            if (muzzleFlashPrefab && muzzlePoint) Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            // gunfire heat (suppressed = far quieter)
            GameEvents.RaiseCrimeCommitted(slot.suppressor ? 0.03f : 0.12f, transform.position);

            if (slot.magAmmo == 0) StartReload(slot);
        }

        void ResolveShot(Vector3 origin, Vector3 dir, WeaponSlot slot)
        {
            float range = slot.data.range;
            // sniper bullet drop: march the ray in a few gravity-affected steps
            if (slot.data.bulletDrop)
            {
                Vector3 pos = origin, vel = dir * slot.data.muzzleVelocity;
                float step = 0.02f, traveled = 0f;
                while (traveled < range)
                {
                    Vector3 next = pos + vel * step;
                    if (Physics.Linecast(pos, next, out var dh, hitMask))
                    {
                        ApplyHit(dh, slot); return;
                    }
                    vel += Vector3.down * 9.81f * step;
                    traveled += (next - pos).magnitude;
                    pos = next;
                }
                return;
            }

            if (Physics.Raycast(origin, dir, out var hit, range, hitMask))
                ApplyHit(hit, slot);
        }

        void ApplyHit(RaycastHit hit, WeaponSlot slot)
        {
            if (impactPrefab) Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            // robbery: shot near the teller counts as a warning shot
            var robbery = hit.collider.GetComponentInParent<Crime.RobberyTarget>();
            if (robbery != null) { robbery.OnWarningShotFired(); return; }

            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                float falloff = 1f - Mathf.Clamp01(hit.distance / slot.data.range) * 0.3f;
                dmg.TakeDamage(slot.data.damage * falloff, gameObject);
                var npc = hit.collider.GetComponentInParent<Crime.NPCReaction>();
                if (npc) GameEvents.RaiseCrimeCommitted(0.4f, hit.point);
            }
            if (slot.data.isExplosive)
                Explosions.Detonate(hit.point, 8f, slot.data.damage, gameObject);
        }

        void Melee()
        {
            Vector3 origin = transform.position + Vector3.up;
            if (Physics.SphereCast(origin, 0.5f, transform.forward, out var hit, 2f, hitMask))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(18f, gameObject);
                GameEvents.RaiseCrimeCommitted(0.2f, hit.point);
            }
        }

        void StartReload(WeaponSlot slot)
        {
            if (slot.reserveAmmo <= 0 || slot.magAmmo >= slot.MagSize) return;
            reloadFinishTime = Time.time + slot.data.reloadTime;
            Invoke(nameof(FinishReload), slot.data.reloadTime);
        }

        void FinishReload() => loadout.Current?.Reload();

        Vector3 SpreadDir(Vector3 forward, float degrees)
        {
            if (degrees <= 0f) return forward;
            return Quaternion.Euler(Random.Range(-degrees, degrees), Random.Range(-degrees, degrees), 0f) * forward;
        }
    }
}
