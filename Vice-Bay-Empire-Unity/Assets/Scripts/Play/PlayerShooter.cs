using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Audio;
using ViceBayEmpire.Player;   // PlayerLoadout, WeaponSlot, IDamageable
using ViceBayEmpire.Data;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Aiming and shooting from the third-person camera. Hitscan with spread/recoil,
    /// shotgun pellets, reload, tracer + impact spark, muzzle audio, and warning-shot
    /// detection for robberies. Reads the shared PlayerLoadout so Dark-Web / shop
    /// weapon purchases appear here automatically. Works on foot and for drive-bys.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        public Camera cam;
        public LayerMask hitMask = ~0;
        public LineRenderer tracer;      // optional; created at runtime by bootstrap

        PlayerLoadout loadout;
        float nextFire, reloadDone;
        public bool IsAiming { get; private set; }

        void Awake()
        {
            loadout = GetComponent<PlayerLoadout>();
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (PlayRefs.UIBlocking || (PlayRefs.Status && PlayRefs.Status.IsDead)) return;
            var slot = loadout != null ? loadout.Current : null;

            if (Input.GetKeyDown(KeyCode.R)) StartReload(slot);
            if (Input.mouseScrollDelta.y != 0 && Input.GetKey(KeyCode.LeftAlt))
                loadout?.Cycle(Input.mouseScrollDelta.y > 0 ? 1 : -1);

            IsAiming = Input.GetMouseButton(1);
            if (slot == null || slot.data == null) return;
            if (slot.data.category == WeaponCategory.Melee) { if (Input.GetMouseButtonDown(0)) Melee(); return; }
            if (Time.time < reloadDone) return;

            bool wantFire = slot.data.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (wantFire && Time.time >= nextFire) Fire(slot);
        }

        void Fire(WeaponSlot slot)
        {
            if (slot.magAmmo <= 0) { StartReload(slot); return; }
            nextFire = Time.time + 1f / Mathf.Max(0.1f, slot.data.fireRate);
            slot.magAmmo--;

            Vector3 origin = cam.transform.position;
            Vector3 baseDir = cam.transform.forward;
            int pellets = Mathf.Max(1, slot.data.pellets);
            for (int i = 0; i < pellets; i++)
            {
                float spread = slot.Spread * (IsAiming ? 0.4f : 1f);
                Vector3 dir = Quaternion.Euler(Random.Range(-spread, spread), Random.Range(-spread, spread), 0) * baseDir;
                ResolveShot(origin, dir, slot);
            }

            AudioManager.Instance?.PlayGunshot(transform.position, slot.suppressor);
            GameEvents.RaiseCrimeCommitted(slot.suppressor ? 0.03f : 0.12f, transform.position);
            if (slot.magAmmo == 0) StartReload(slot);
        }

        void ResolveShot(Vector3 origin, Vector3 dir, WeaponSlot slot)
        {
            Vector3 end = origin + dir * slot.data.range;
            if (Physics.Raycast(origin, dir, out var hit, slot.data.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;

                // warning shot near a teller boosts robbery fear instead of doing damage
                var rob = hit.collider.GetComponentInParent<RobberyDesk>();
                if (rob != null) rob.OnWarningShot();

                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && (Object)dmg != PlayRefs.Status)
                {
                    float falloff = 1f - Mathf.Clamp01(hit.distance / slot.data.range) * 0.3f;
                    dmg.TakeDamage(slot.data.damage * falloff, gameObject);
                    SpawnSpark(hit.point, hit.normal);
                }
                else SpawnSpark(hit.point, hit.normal);

                if (slot.data.isExplosive) Explosions.Detonate(hit.point, 8f, slot.data.damage, gameObject);
            }
            DrawTracer(origin, end);
        }

        void Melee()
        {
            if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, transform.forward, out var hit, 2.2f))
            {
                hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(20f, gameObject);
                GameEvents.RaiseCrimeCommitted(0.2f, hit.point);
            }
        }

        void StartReload(WeaponSlot slot)
        {
            if (slot == null || slot.reserveAmmo <= 0 || slot.magAmmo >= slot.MagSize) return;
            reloadDone = Time.time + slot.data.reloadTime;
            Invoke(nameof(FinishReload), slot.data.reloadTime);
            GameEvents.RaiseNotify("Reloading...", NotifyType.Info);
        }
        void FinishReload() => loadout.Current?.Reload();

        void DrawTracer(Vector3 a, Vector3 b)
        {
            if (tracer == null) return;
            tracer.SetPosition(0, a); tracer.SetPosition(1, b);
            tracer.enabled = true;
            CancelInvoke(nameof(HideTracer));
            Invoke(nameof(HideTracer), 0.05f);
        }
        void HideTracer() { if (tracer) tracer.enabled = false; }

        void SpawnSpark(Vector3 pos, Vector3 normal)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.transform.position = pos;
            spark.transform.localScale = Vector3.one * 0.12f;
            Destroy(spark.GetComponent<Collider>());
            Bootstrap.MaterialFactory.Paint(spark, new Color(1f, 0.85f, 0.3f), 0.5f, emissive: true);
            Destroy(spark, 0.15f);
        }
    }
}
