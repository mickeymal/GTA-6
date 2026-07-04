using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Player
{
    /// <summary>
    /// Third-person character controller: walk/sprint, jump/vault, swim, climb, and a
    /// ragdoll hand-off on death. Uses Unity's CharacterController for kinematic ground
    /// movement and switches to buoyancy when submerged. Camera is a separate follow
    /// rig (PlayerCamera) reading the aim state from PlayerCombat.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.2f;
        public float sprintSpeed = 6.5f;
        public float swimSpeed = 2.4f;
        public float acceleration = 12f;
        public float rotationSpeed = 12f;
        public float jumpHeight = 1.3f;
        public float gravity = -20f;

        [Header("Vault / Climb")]
        public float vaultHeight = 1.2f;
        public float climbCheckDistance = 0.6f;
        public LayerMask climbMask = ~0;

        [Header("Water")]
        public float waterLevelY = 0.5f;
        public float buoyancy = 2f;

        [Header("References")]
        public Transform cameraRig;
        public Animator animator;

        public bool InVehicle { get; set; }
        public bool IsSwimming { get; private set; }
        public bool IsSprinting { get; private set; }

        CharacterController cc;
        Vector3 velocity;
        Vector3 planarVel;
        PlayerCombat combat;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            combat = GetComponent<PlayerCombat>();
            if (cameraRig == null && Camera.main) cameraRig = Camera.main.transform;
        }

        void Update()
        {
            if (InVehicle || GameManager.Instance == null || GameManager.Instance.State == GameState.Paused)
                return;

            IsSwimming = transform.position.y < waterLevelY;
            if (IsSwimming) SwimMove();
            else GroundMove();

            UpdateAnimator();
        }

        void GroundMove()
        {
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && input.y > 0.1f;
            float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

            // camera-relative movement
            Vector3 fwd = cameraRig ? Vector3.ProjectOnPlane(cameraRig.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cameraRig ? cameraRig.right : Vector3.right;
            Vector3 wish = (fwd * input.y + right * input.x).normalized;

            Vector3 desired = wish * targetSpeed;
            planarVel = Vector3.MoveTowards(planarVel, desired, acceleration * Time.deltaTime);

            // face movement (or camera forward when aiming)
            if (combat != null && combat.IsAiming)
                RotateTowards(fwd);
            else if (wish.sqrMagnitude > 0.01f)
                RotateTowards(wish);

            if (cc.isGrounded)
            {
                velocity.y = -1f;
                if (Input.GetButtonDown("Jump"))
                {
                    if (TryVault()) { /* vault handled */ }
                    else velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else velocity.y += gravity * Time.deltaTime;

            Vector3 move = planarVel + Vector3.up * velocity.y;
            cc.Move(move * Time.deltaTime);
        }

        void SwimMove()
        {
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 fwd = cameraRig ? cameraRig.forward : Vector3.forward;
            Vector3 right = cameraRig ? cameraRig.right : Vector3.right;
            Vector3 wish = (fwd * input.y + right * input.x);
            float vertical = 0f;
            if (Input.GetKey(KeyCode.Space)) vertical += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) vertical -= 1f;

            planarVel = Vector3.MoveTowards(planarVel, wish.normalized * swimSpeed, acceleration * Time.deltaTime);
            // gentle buoyancy toward the surface
            float toSurface = (waterLevelY - transform.position.y);
            velocity.y = Mathf.Lerp(velocity.y, vertical * swimSpeed + toSurface * buoyancy, 2f * Time.deltaTime);

            if (wish.sqrMagnitude > 0.01f) RotateTowards(Vector3.ProjectOnPlane(wish, Vector3.up));
            cc.Move((planarVel + Vector3.up * velocity.y) * Time.deltaTime);
        }

        bool TryVault()
        {
            // simple forward obstacle probe: if a low ledge is ahead, hop over it
            Vector3 origin = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origin, transform.forward, out var hit, climbCheckDistance, climbMask))
            {
                Vector3 top = hit.point + Vector3.up * vaultHeight + transform.forward * 0.5f;
                if (!Physics.CheckSphere(top, 0.3f, climbMask))
                {
                    StartCoroutine(VaultTo(top));
                    return true;
                }
            }
            return false;
        }

        System.Collections.IEnumerator VaultTo(Vector3 target)
        {
            animator?.SetTrigger("Vault");
            Vector3 start = transform.position;
            float t = 0f;
            cc.enabled = false;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                transform.position = Vector3.Lerp(start, target, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.4f;
                yield return null;
            }
            cc.enabled = true;
        }

        void RotateTowards(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", planarVel.magnitude);
            animator.SetBool("Sprinting", IsSprinting);
            animator.SetBool("Swimming", IsSwimming);
            animator.SetBool("Grounded", cc.isGrounded);
        }

        /// <summary>Teleport helper used by property respawn / vehicle exit.</summary>
        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
        }
    }
}
