using UnityEngine;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// GTA-style third-person orbit camera. Mouse orbits, scroll zooms, collides with
    /// world geometry so it doesn't clip through buildings. Follows the player on foot
    /// and the vehicle while driving (pulling back for a wider view).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 6f;
        public float vehicleDistance = 9f;
        public float height = 2.2f;
        public float sensitivity = 3f;
        public float minPitch = -20f, maxPitch = 70f;
        public LayerMask collisionMask = ~0;

        float yaw, pitch = 15f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        public float Yaw => yaw;

        void LateUpdate()
        {
            if (target == null) return;

            if (!PlayRefs.UIBlocking)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, 3f, 14f);
                Cursor.lockState = CursorLockMode.Locked;
            }
            else Cursor.lockState = CursorLockMode.None;

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            float dist = PlayRefs.InVehicle ? vehicleDistance : distance;
            Vector3 focus = target.position + Vector3.up * height;
            Vector3 wanted = focus - rot * Vector3.forward * dist;

            // don't clip through geometry
            if (Physics.Linecast(focus, wanted, out var hit, collisionMask,
                                 QueryTriggerInteraction.Ignore))
                wanted = hit.point + hit.normal * 0.3f;

            transform.position = Vector3.Lerp(transform.position, wanted, 12f * Time.deltaTime);
            transform.rotation = rot;
        }

        /// <summary>Flat forward vector for camera-relative movement.</summary>
        public Vector3 FlatForward()
        {
            Vector3 f = transform.forward; f.y = 0; return f.normalized;
        }
        public Vector3 FlatRight()
        {
            Vector3 r = transform.right; r.y = 0; return r.normalized;
        }
    }
}
