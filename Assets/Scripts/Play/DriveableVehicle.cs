using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Audio;
using ViceBayEmpire.Player;   // IDamageable

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// One self-contained, runtime-friendly vehicle that covers all four classes with a
    /// mode switch: Car (grip + drift + handbrake), Boat (floats on water, wallows),
    /// Plane (needs takeoff speed to climb, stalls when slow), Helicopter (vertical
    /// thrust + tilt). Rigidbody-based, damageable, explodes at zero health. Enter/exit
    /// is driven by PlayerInteractor3D. Engine audio pitch tracks speed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DriveableVehicle : MonoBehaviour, IDamageable
    {
        public enum Mode { Car, Boat, Plane, Helicopter }
        public Mode mode = Mode.Car;
        public string displayName = "Vehicle";

        [Header("Performance")]
        public float topSpeed = 30f;
        public float accel = 14f;
        public float braking = 22f;
        public float turn = 90f;
        public float grip = 6f;
        public float liftSpeed = 16f, stallSpeed = 10f, verticalRate = 8f;
        public float maxHealth = 400f;
        public float enterRadius = 1.5f;
        public float waterLevelY = 0.4f;

        public Transform seat, exitPoint;

        Rigidbody rb;
        float health;
        AudioSource engine;
        PlayerInteractor3D driver;
        bool exploded;

        // AI (traffic) control when there is no player driver
        bool aiControlled;
        float aiThrottle, aiSteer;
        public void SetAI(float throttle, float steer) { aiControlled = true; aiThrottle = throttle; aiSteer = steer; }
        public void ClearAI() => aiControlled = false;

        public bool Occupied => driver != null;
        public float Speed => rb ? rb.velocity.magnitude : 0f;
        public float Altitude => transform.position.y;
        public float HealthPct => health / maxHealth;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = maxHealth;
            rb.mass = mode == Mode.Boat ? 800 : mode == Mode.Car ? 1200 : 600;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (mode == Mode.Plane || mode == Mode.Helicopter) rb.useGravity = true;
        }

        // ---------------------------------------------------------------- enter/exit
        public void Enter(PlayerInteractor3D who)
        {
            GetComponent<TrafficDriver>()?.Relinquish();   // hop into moving traffic
            aiControlled = false;
            driver = who;
            PlayRefs.CurrentVehicle = this;
            var p = who.transform;
            p.SetParent(seat != null ? seat : transform);
            p.localPosition = Vector3.zero;
            var cc = p.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            if (engine == null) engine = AudioManager.Instance?.AttachEngine(transform);
            GameEvents.RaiseVehicleStateChanged(true);
            GameEvents.RaiseNotify(displayName, NotifyType.Info);
        }

        public void Exit(bool force = false)
        {
            if (driver == null) return;
            if (!force && (mode == Mode.Plane || mode == Mode.Helicopter) && Altitude > 3f && Speed > 3f)
            { GameEvents.RaiseNotify("Too high to get out!", NotifyType.Warning); return; }

            var p = driver.transform;
            p.SetParent(null);
            Vector3 outPos = exitPoint ? exitPoint.position : transform.position + transform.right * 2.5f + Vector3.up;
            var cc = p.GetComponent<CharacterController>();
            var move = p.GetComponent<PlayerMovement3D>();
            if (move) move.Teleport(outPos); else p.position = outPos;
            if (cc) cc.enabled = true;
            driver = null;
            PlayRefs.CurrentVehicle = null;
            GameEvents.RaiseVehicleStateChanged(false);
        }

        // ---------------------------------------------------------------- physics
        void FixedUpdate()
        {
            if (exploded) return;
            float perf = Mathf.Lerp(0.4f, 1f, HealthPct);
            float throttle = 0, steer = 0, vertical = 0; bool handbrake = false;
            if (Occupied && !PlayRefs.UIBlocking)
            {
                throttle = Input.GetAxis("Vertical");
                steer = Input.GetAxis("Horizontal");
                handbrake = Input.GetKey(KeyCode.Space) && mode == Mode.Car;
                if (mode == Mode.Plane || mode == Mode.Helicopter)
                {
                    if (Input.GetKey(KeyCode.Space)) vertical += 1f;
                    if (Input.GetKey(KeyCode.LeftControl)) vertical -= 1f;
                }
            }
            else if (aiControlled)   // AI traffic driver
            {
                throttle = aiThrottle;
                steer = aiSteer;
            }

            switch (mode)
            {
                case Mode.Car: CarPhysics(perf, throttle, steer, handbrake); break;
                case Mode.Boat: BoatPhysics(perf, throttle, steer); break;
                case Mode.Plane: PlanePhysics(perf, throttle, steer, vertical); break;
                case Mode.Helicopter: HeliPhysics(perf, throttle, steer, vertical); break;
            }

            if (engine)
            {
                engine.pitch = Mathf.Lerp(engine.pitch, 0.7f + Speed / topSpeed * 1.3f, 4f * Time.fixedDeltaTime);
                engine.volume = Occupied ? 0.6f : 0.3f;
            }
        }

        void CarPhysics(float perf, float throttle, float steer, bool handbrake)
        {
            Vector3 fwd = transform.forward;
            float fSpeed = Vector3.Dot(rb.velocity, fwd);
            if (throttle > 0) rb.AddForce(fwd * accel * perf * throttle, ForceMode.Acceleration);
            else if (throttle < 0)
                rb.AddForce((fSpeed > 1 ? -fwd * braking : fwd * accel * 0.5f * throttle), ForceMode.Acceleration);

            if (rb.velocity.magnitude > topSpeed * perf) rb.velocity = rb.velocity.normalized * topSpeed * perf;

            float speedFactor = Mathf.Clamp01(Mathf.Abs(fSpeed) / 6f);
            transform.Rotate(Vector3.up, turn * steer * speedFactor * (fSpeed >= 0 ? 1 : -1) * Time.fixedDeltaTime
                             * (handbrake ? 1.5f : 1f), Space.World);
            // grip: kill lateral velocity (less on handbrake -> drift)
            Vector3 lateral = Vector3.Project(rb.velocity, transform.right);
            rb.velocity -= lateral * (handbrake ? grip * 0.2f : grip) * Time.fixedDeltaTime;
        }

        void BoatPhysics(float perf, float throttle, float steer)
        {
            bool onWater = transform.position.y < waterLevelY + 1.5f;
            if (onWater)
            {
                // buoyancy toward the water line
                float depth = waterLevelY - transform.position.y;
                rb.AddForce(Vector3.up * (depth * 12f + 9.81f), ForceMode.Acceleration);
                rb.AddForce(transform.forward * accel * perf * throttle, ForceMode.Acceleration);
                transform.Rotate(Vector3.up, turn * steer * Mathf.Clamp01(Speed / 5f) * Time.fixedDeltaTime, Space.World);
                Vector3 lateral = Vector3.Project(rb.velocity, transform.right);
                rb.velocity -= lateral * 0.9f * Time.fixedDeltaTime;
            }
            if (rb.velocity.magnitude > topSpeed * perf) rb.velocity = rb.velocity.normalized * topSpeed * perf;
        }

        void PlanePhysics(float perf, float throttle, float steer, float vertical)
        {
            rb.AddForce(transform.forward * accel * perf * Mathf.Max(0, throttle), ForceMode.Acceleration);
            float speed = Vector3.Dot(rb.velocity, transform.forward);
            bool lift = speed >= liftSpeed;
            if (lift)
            {
                rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);   // counter gravity
                transform.Rotate(transform.right, -vertical * verticalRate * Time.fixedDeltaTime * 6f, Space.World);
            }
            else if (Altitude > 2f && speed < stallSpeed)
                transform.Rotate(transform.right, 24f * Time.fixedDeltaTime, Space.World);   // stall nose-drop
            transform.Rotate(Vector3.up, turn * steer * Mathf.Clamp01(speed / 12f) * Time.fixedDeltaTime, Space.World);
            if (rb.velocity.magnitude > topSpeed * perf) rb.velocity = rb.velocity.normalized * topSpeed * perf;
        }

        void HeliPhysics(float perf, float throttle, float steer, float vertical)
        {
            rb.AddForce(Vector3.up * (9.81f + vertical * verticalRate), ForceMode.Acceleration);
            rb.AddForce(transform.forward * accel * perf * throttle, ForceMode.Acceleration);
            transform.Rotate(Vector3.up, turn * steer * Time.fixedDeltaTime, Space.World);
            rb.velocity *= (1f - 0.6f * Time.fixedDeltaTime);   // air drag
            if (rb.velocity.magnitude > topSpeed * perf) rb.velocity = rb.velocity.normalized * topSpeed * perf;
        }

        // ---------------------------------------------------------------- damage
        public void TakeDamage(float amount, GameObject source = null)
        {
            if (exploded) return;
            health -= amount;
            if (health <= 0) Explode(source);
        }

        void OnCollisionEnter(Collision col)
        {
            float impact = col.relativeVelocity.magnitude;
            if (impact > 9f)
            {
                TakeDamage(impact * 1.5f);
                col.collider.GetComponentInParent<IDamageable>()?.TakeDamage(impact, gameObject);
            }
        }

        void Explode(GameObject source)
        {
            exploded = true;
            AudioManager.Instance?.PlayExplosion(transform.position);
            Explosions.Detonate(transform.position, 10f, 200f, source);
            if (driver != null) { PlayRefs.Status?.TakeDamage(140f, source); Exit(force: true); }
            MaterialFactoryTint();
            enabled = false;
        }

        void MaterialFactoryTint()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.sharedMaterial = Bootstrap.MaterialFactory.Solid(new Color(0.1f, 0.1f, 0.1f));
        }
    }
}
