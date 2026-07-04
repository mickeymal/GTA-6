using UnityEngine;
using ViceBayEmpire.Bootstrap;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Simple traffic AI: drives a DriveableVehicle from intersection to intersection
    /// along the shared road lattice (CityLayout), preferring to continue straight and
    /// turning at random. Brakes for obstacles ahead (player, other cars) so it doesn't
    /// plow through. Relinquishes control the instant the player hops in (carjack/enter).
    /// </summary>
    [RequireComponent(typeof(DriveableVehicle))]
    public class TrafficDriver : MonoBehaviour
    {
        DriveableVehicle vehicle;
        Vector2Int node;         // current target node on the lattice
        Vector2Int dir;          // travel direction in node steps
        bool active = true;
        DriveableVehicle[] nearbyCars = System.Array.Empty<DriveableVehicle>();
        float refresh;

        void Awake()
        {
            vehicle = GetComponent<DriveableVehicle>();
            node = CityLayout.NearestNode(transform.position);
            dir = Random.value < 0.5f ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
            if (Random.value < 0.5f) dir = -dir;
        }

        public void Relinquish() { active = false; enabled = false; }

        void FixedUpdate()
        {
            if (!active) return;
            refresh -= Time.fixedDeltaTime;
            if (refresh <= 0f) { refresh = 1.5f; nearbyCars = FindObjectsOfType<DriveableVehicle>(); }

            Vector3 target = CityLayout.NodePos(node);
            Vector3 to = target - transform.position; to.y = 0;

            if (to.magnitude < 5f) PickNextNode();

            // steer toward the target node
            float ang = Vector3.SignedAngle(transform.forward, to.normalized, Vector3.up);
            float steer = Mathf.Clamp(ang / 30f, -1f, 1f);

            // cruise; brake only for the player or another vehicle close ahead
            float throttle = 0.4f;
            Vector3 probe = transform.position + transform.forward * 6f;
            if (PlayRefs.Player && Vector3.Distance(probe, PlayRefs.Player.position) < 4f) throttle = -0.3f;
            foreach (var other in nearbyCars)
                if (other && other != vehicle && Vector3.Distance(probe, other.transform.position) < 4f) { throttle = -0.2f; break; }

            vehicle.SetAI(throttle, steer);
        }

        void PickNextNode()
        {
            // continue straight if possible, else turn onto a valid neighbor
            var straight = node + dir;
            if (CityLayout.ValidNode(straight) && Random.value < 0.7f) { node = straight; return; }

            var options = new System.Collections.Generic.List<Vector2Int>();
            foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, 1) * -1 })
            {
                var n = node + d;
                if (CityLayout.ValidNode(n) && d != -dir) options.Add(d);   // no U-turns
            }
            if (options.Count == 0) { dir = -dir; return; }
            dir = options[Random.Range(0, options.Count)];
            node += dir;
        }
    }
}
