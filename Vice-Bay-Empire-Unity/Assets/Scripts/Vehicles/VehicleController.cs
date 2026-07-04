using UnityEngine;
using ViceBayEmpire.Core;
using ViceBayEmpire.Data;
using ViceBayEmpire.Player;

namespace ViceBayEmpire.Vehicles
{
    /// <summary>
    /// Rigidbody-based vehicle with class-specific handling: ground cars/bikes drive
    /// with grip + drift, boats float and steer on water, planes need takeoff speed and
    /// can stall, helicopters spin up rotors then hover/translate. Damage degrades
    /// performance; at zero health the vehicle catches fire and explodes.
    ///
    /// Input is fed by the driver (player or AI) via <see cref="SetInput"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour, IDamageable
    {
        public VehicleData Data;
        [SerializeField] float health;
        public Transform[] driveByMuzzles;
        public Transform driverSeat, exitPoint;

        // input
        float throttle, steer, vertical;
        bool handbrake;
        public GameObject Driver { get; private set; }

        Rigidbody rb;
        bool exploded;
        float smokeTimer;

        public float HealthPct => Data != null ? health / Data.maxHealth : 0f;
        public bool IsAircraft => Data != null && (Data.vehicleClass == VehicleClass.Plane
                                  || Data.vehicleClass == VehicleClass.Jet
                                  || Data.vehicleClass == VehicleClass.Helicopter);
        public bool IsBoat => Data != null && (Data.vehicleClass == VehicleClass.Speedboat
                              || Data.vehicleClass == VehicleClass.Yacht
                              || Data.vehicleClass == VehicleClass.JetSki
                              || Data.vehicleClass == VehicleClass.FishingBoat);
        public float Altitude { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (Data != null) { rb.mass = Data.mass; health = Data.maxHealth; }
        }

        public void SetInput(float throttle, float steer, float vertical, bool handbrake)
        {
            this.throttle = Mathf.Clamp(throttle, -1f, 1f);
            this.steer = Mathf.Clamp(steer, -1f, 1f);
            this.vertical = Mathf.Clamp(vertical, -1f, 1f);
            this.handbrake = handbrake;
        }

        public void SetDriver(GameObject driver)
        {
            Driver = driver;
            GameEvents.RaiseVehicleStateChanged(driver != null);
        }

        public void ForceEjectDriver()
        {
            if (Driver != null)
            {
                var reaction = Driver.GetComponent<Crime.NPCReaction>();
                reaction?.Flee(transform);
            }
            Driver = null;
        }

        void FixedUpdate()
        {
            if (Data == null || exploded) return;
            float perf = Mathf.Lerp(0.4f, 1f, HealthPct);   // damage cuts performance

            if (IsAircraft) AircraftPhysics(perf);
            else if (IsBoat) BoatPhysics(perf);
            else GroundPhysics(perf);
        }

        // ------------------------------------------------------------- ground
        void GroundPhysics(float perf)
        {
            Vector3 fwd = transform.forward;
            float forwardSpeed = Vector3.Dot(rb.velocity, fwd);

            float accel = Data.acceleration * perf;
            if (throttle > 0f) rb.AddForce(fwd * accel * throttle, ForceMode.Acceleration);
            else if (throttle < 0f)
            {
                if (forwardSpeed > 1f) rb.AddForce(-fwd * Data.braking, ForceMode.Acceleration);
                else rb.AddForce(fwd * accel * throttle * 0.5f, ForceMode.Acceleration);
            }

            // clamp top speed
            if (rb.velocity.magnitude > Data.topSpeed * perf)
                rb.velocity = rb.velocity.normalized * Data.topSpeed * perf;

            // steering scales with speed
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 8f);
            float turn = Data.handling * steer * speedFactor * (forwardSpeed >= 0 ? 1 : -1);
            transform.Rotate(Vector3.up, turn * (handbrake ? 1.6f : 1f), Space.World);

            // kill lateral velocity for grip (less when handbraking -> drift)
            Vector3 lateral = Vector3.Project(rb.velocity, transform.right);
            float grip = handbrake ? Data.grip * 0.25f : Data.grip;
            rb.velocity -= lateral * grip * Time.fixedDeltaTime * 6f;
        }

        // ------------------------------------------------------------- boat
        void BoatPhysics(float perf)
        {
            Altitude = 0f;
            Vector3 fwd = transform.forward;
            rb.AddForce(fwd * Data.acceleration * perf * throttle, ForceMode.Acceleration);
            if (rb.velocity.magnitude > Data.topSpeed * perf)
                rb.velocity = rb.velocity.normalized * Data.topSpeed * perf;
            float speedFactor = Mathf.Clamp01(rb.velocity.magnitude / 6f);
            transform.Rotate(Vector3.up, Data.handling * steer * speedFactor, Space.World);
            // heavy lateral wallow
            Vector3 lateral = Vector3.Project(rb.velocity, transform.right);
            rb.velocity -= lateral * 0.9f * Time.fixedDeltaTime * 4f;
        }

        // ------------------------------------------------------------- aircraft
        void AircraftPhysics(float perf)
        {
            Vector3 fwd = transform.forward;
            float speed = Vector3.Dot(rb.velocity, fwd);

            if (Data.vehicleClass == VehicleClass.Helicopter)
            {
                // vertical thrust to climb/hover; tilt to translate
                Altitude = transform.position.y;
                float lift = (vertical) * Data.verticalRate;
                rb.AddForce(Vector3.up * (9.81f + lift), ForceMode.Acceleration);
                rb.AddForce(fwd * Data.acceleration * throttle * perf, ForceMode.Acceleration);
                transform.Rotate(Vector3.up, Data.handling * steer, Space.World);
                if (rb.velocity.magnitude > Data.topSpeed * perf)
                    rb.velocity = rb.velocity.normalized * Data.topSpeed * perf;
            }
            else
            {
                // fixed wing: need lift speed to climb, stall if too slow
                rb.AddForce(fwd * Data.acceleration * throttle * perf, ForceMode.Acceleration);
                Altitude = transform.position.y;
                bool hasLift = speed >= Data.liftSpeed;
                if (hasLift)
                {
                    rb.AddForce(Vector3.up * 9.81f, ForceMode.Acceleration);  // counter gravity
                    transform.Rotate(transform.right, -vertical * Data.verticalRate * Time.fixedDeltaTime, Space.World);
                }
                else if (Altitude > 2f && speed < Data.stallSpeed)
                {
                    // stalling: nose drops
                    transform.Rotate(transform.right, 30f * Time.fixedDeltaTime, Space.World);
                }
                transform.Rotate(Vector3.up, Data.handling * steer * Mathf.Clamp01(speed / 20f), Space.World);
                if (rb.velocity.magnitude > Data.topSpeed * perf)
                    rb.velocity = rb.velocity.normalized * Data.topSpeed * perf;
            }
        }

        // ------------------------------------------------------------- damage
        public void TakeDamage(float amount, GameObject source = null)
        {
            if (exploded) return;
            health -= amount;
            if (health <= 0f) Explode(source);
        }

        void OnCollisionEnter(Collision col)
        {
            float impact = col.relativeVelocity.magnitude;
            if (impact > 8f)
            {
                TakeDamage(impact * 2f);
                var other = col.collider.GetComponentInParent<IDamageable>();
                if (other != null && other != (IDamageable)this) other.TakeDamage(impact * 1.5f, Driver);
            }
        }

        void Update()
        {
            if (exploded || Data == null) return;
            if (HealthPct < 0.3f)
            {
                smokeTimer -= Time.deltaTime;
                if (smokeTimer <= 0f) { smokeTimer = 0.2f; /* spawn smoke VFX here */ }
            }
        }

        void Explode(GameObject source)
        {
            exploded = true;
            Explosions.Detonate(transform.position, 10f, 200f, source);
            if (Driver != null)
            {
                Driver.GetComponent<IDamageable>()?.TakeDamage(120f, source);
                var pc = Driver.GetComponent<PlayerController>();
                if (pc) pc.InVehicle = false;
            }
            // leave a burnt-out husk; disable control
            enabled = false;
        }
    }
}
