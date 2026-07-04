using UnityEngine;
using ViceBayEmpire.Audio;

namespace ViceBayEmpire.Play
{
    /// <summary>
    /// Third-person character controller with walk, sprint, jump, auto-vault over low
    /// obstacles, and swimming when below the water line. Camera-relative movement.
    /// Disabled while driving; PlayerStatus handles the ragdoll/death state.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement3D : MonoBehaviour
    {
        public float walkSpeed = 4f;
        public float sprintSpeed = 8f;
        public float swimSpeed = 3f;
        public float jumpHeight = 1.4f;
        public float gravity = -22f;
        public float waterLevelY = 0.4f;
        public float rotationLerp = 14f;

        public ThirdPersonCamera cam;

        CharacterController cc;
        Vector3 velocity;
        float footTimer;
        public bool IsSwimming { get; private set; }
        public bool IsSprinting { get; private set; }

        void Awake() => cc = GetComponent<CharacterController>();

        void Update()
        {
            if (PlayRefs.InVehicle) return;               // driving handled by DriveableVehicle
            if (PlayRefs.Status != null && PlayRefs.Status.IsDead) return;

            IsSwimming = transform.position.y < waterLevelY;
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 wish = cam ? (cam.FlatForward() * input.y + cam.FlatRight() * input.x)
                               : new Vector3(input.x, 0, input.y);
            wish = Vector3.ClampMagnitude(wish, 1f);

            if (IsSwimming) SwimMove(wish);
            else GroundMove(wish);

            if (wish.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(wish.x, 0, wish.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationLerp * Time.deltaTime);
            }
        }

        void GroundMove(Vector3 wish)
        {
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && wish.sqrMagnitude > 0.1f;
            float speed = IsSprinting ? sprintSpeed : walkSpeed;

            if (cc.isGrounded)
            {
                velocity.y = -2f;
                if (Input.GetButtonDown("Jump") && !TryVault())
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else velocity.y += gravity * Time.deltaTime;

            Vector3 move = wish * speed + Vector3.up * velocity.y;
            cc.Move(move * Time.deltaTime);

            // footstep audio while moving on the ground
            if (cc.isGrounded && wish.sqrMagnitude > 0.1f)
            {
                footTimer -= Time.deltaTime * (IsSprinting ? 1.6f : 1f);
                if (footTimer <= 0f) { footTimer = 0.45f; AudioManager.Instance?.PlayFootstep(transform.position); }
            }
        }

        void SwimMove(Vector3 wish)
        {
            float vertical = 0f;
            if (Input.GetKey(KeyCode.Space)) vertical += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) vertical -= 1f;
            // float toward the surface
            float toSurface = Mathf.Clamp(waterLevelY - transform.position.y, -1f, 1f);
            Vector3 move = wish * swimSpeed + Vector3.up * (vertical * swimSpeed + toSurface * 2f);
            velocity = Vector3.zero;
            cc.Move(move * Time.deltaTime);
        }

        bool TryVault()
        {
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origin, transform.forward, out var hit, 0.8f))
            {
                Vector3 top = hit.point + Vector3.up * 1.3f + transform.forward * 0.6f;
                if (!Physics.CheckSphere(top, 0.3f))
                {
                    StartCoroutine(VaultTo(top));
                    return true;
                }
            }
            return false;
        }

        System.Collections.IEnumerator VaultTo(Vector3 target)
        {
            cc.enabled = false;
            Vector3 start = transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime * 3.5f)
            {
                transform.position = Vector3.Lerp(start, target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.3f;
                yield return null;
            }
            cc.enabled = true;
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false; transform.position = pos; cc.enabled = true;
        }
    }
}
