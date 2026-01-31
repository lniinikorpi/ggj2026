using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerMesh;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator boardAnimator;

    [Header("Ragdoll")]
    [SerializeField] private Rigidbody controllerPlayerRigidbody;
    [SerializeField] private Collider controllerPlayerCollider;

    [SerializeField] private Rigidbody ragdollPlayerRigidbody;
    [SerializeField] private Collider ragdollPlayerCollider;
    [SerializeField] private Rigidbody ragdollBoardRigidbody;
    [SerializeField] private Collider ragdollBoardCollider;

    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float friction = 0.95f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float raycastDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Air Control Settings")]
    [SerializeField, Range(0f, 1f)] private float airAccelerationMultiplier = 0.35f;
    [SerializeField, Range(0f, 10f)] private float airTurnSpeedMultiplier = 0.6f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpImpulseMax = 7f;
    [SerializeField] private float jumpImpulseMin = 3.5f;
    [SerializeField] private float jumpImpulseHoldMax = .5f;
    [SerializeField] private float jumpCooldown = 0.1f;
    private float currentJumpImpulse = 0f;
    private bool isJumpHold = false;

    [Header("Turn Tilt Settings")]
    [SerializeField, Range(0f, 45f)] private float maxTurnTiltDegrees = 12f;
    [SerializeField, Range(0.1f, 30f)] private float turnTiltSmoothing = 10f;
    [SerializeField, Range(0f, 0.5f)] private float turnInputDeadzone = 0.02f;

    [Header("Camera Target Settings")]
    [SerializeField] private float cameraYawOffset = 0f;
    [SerializeField, Range(-89f, 89f)] private float cameraPitch = 10f;
    [SerializeField, Range(0.01f, 2f)] private float cameraYawSmoothTime = 0.12f;
    [SerializeField, Range(0.01f, 2f)] private float cameraPitchSmoothTime = 0.12f;
    [SerializeField, Range(0.01f, 2f)] private float cameraUpSmoothTime = 0.08f;
    
    [Header("Tricks")]
    [SerializeField] private List<TrickData> tricks;

    [Header("Trick Movement Helpers")]
    [SerializeField, Range(0f, 0.5f)] private float airTurnSuppressDuration = 0.12f;

    [Header("Landing Boost")]
    [SerializeField] private float landingTrickBoostImpulse = 3.0f;
    [SerializeField] private float landingTrickBoostMinHorizontalSpeed = 0.25f;
    [SerializeField, Range(1f, 3f)] private float landingTrickBoostMaxSpeedMultiplier = 1.5f;
    [SerializeField, Range(0f, 3f)] private float landingTrickBoostOverspeedDuration = 0.75f;

    [SerializeField, Range(0.1f, 1f)] private float trickDirectionDeadzone = 0.5f;
    [SerializeField] private int maxBufferedDirections = 8;

    private float isTricking;
    private float wasTricking;

    private bool wasGrounded;
    private bool trickLocked;
    private readonly List<Direction> bufferedDirections = new List<Direction>();
    private Direction? lastBufferedDirection;

    private float suppressAirTurnUntil;
    private bool landingBoostPending;
    private float allowOverspeedUntil;

    private Rigidbody rb;
    private Vector2 playerInput;
    private float throttle;
    private float currentYaw;
    private float lastJumpTime;
    private float currentTurnTilt;

    private bool isRagdoll;
    private Vector3 lastAirVelocity;

    private float cameraYaw;
    private float cameraPitchCurrent;
    private float cameraYawVelocity;
    private float cameraPitchVelocity;
    private Vector3 cameraUpCurrent;
    private Vector3 cameraUpVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Default controller references to the main rigidbody/collider if not explicitly wired.
        if (controllerPlayerRigidbody == null) controllerPlayerRigidbody = rb;
        if (controllerPlayerCollider == null) controllerPlayerCollider = GetComponent<Collider>();
        
        // Ensure Rigidbody is configured for a skating feel
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Initialize yaw based on current rotation
        currentYaw = transform.eulerAngles.y;

        // Initialize camera target smoothing to current values to avoid a startup snap.
        cameraYaw = currentYaw + cameraYawOffset;
        cameraPitchCurrent = cameraPitch;
        cameraUpCurrent = Vector3.up;

        // Ensure ragdoll physics are disabled on start (they can be enabled on fall).
        SetRagdollEnabled(false);
    }

    void Update()
    {
        if (isRagdoll) return;

        UpdateMeshTransform();
        UpdateCameraTargetTransform();
        if (isJumpHold) HoldJump(Time.deltaTime);

        bool grounded = IsGrounded();
        if (grounded && !wasGrounded)
        {
            // Simplified falling rule:
            // If we land while a trick is still in progress, we fall.
            // (This replaces the old "bad landing" speed/angle based fall mechanic.)
            if (trickLocked)
            {
                EnterRagdoll();
                wasGrounded = grounded;
                return;
            }

            if (landingBoostPending)
            {
                ApplyLandingTrickBoost();
                landingBoostPending = false;
            }

            bufferedDirections.Clear();
            lastBufferedDirection = null;
        }
        else if (!grounded && wasGrounded)
        {
            bufferedDirections.Clear();
            lastBufferedDirection = null;
        }

        wasGrounded = grounded;
    }

    void FixedUpdate()
    {
        if (isRagdoll) return;

        bool grounded = IsGrounded();

        if (!grounded)
        {
            lastAirVelocity = rb.linearVelocity;
        }

        MovePlayer(grounded);
    }

    public void OnMove(InputValue value)
    {
        playerInput = value.Get<Vector2>();
        if (TryMapInputToDirection(playerInput, out Direction direction))
        {
            bool isNewDirection = !lastBufferedDirection.HasValue || lastBufferedDirection.Value != direction;

            if (isNewDirection && !IsGrounded())
            {
                BufferDirection(direction);

                // Quick trick-direction taps (especially left/right) shouldn't accidentally rotate us mid-air.
                if (airTurnSuppressDuration > 0f && (direction == Direction.Left || direction == Direction.Right))
                {
                    suppressAirTurnUntil = Time.time + airTurnSuppressDuration;
                }
            }

            if (isNewDirection && isTricking > 0.5f)
            {
                TryExecuteSingleDirectionTrick(direction);
            }

            if (isNewDirection)
            {
                lastBufferedDirection = direction;
            }
        }
        else
        {
            lastBufferedDirection = null;
        }
    }

    public void OnThrottle(InputValue value)
    {
        float val = value.Get<float>();
        throttle = val;
    }

    public void OnJump(InputValue value)
    {
        if (Time.time < lastJumpTime + jumpCooldown) return;
        if (!IsGrounded()) return;
        
        float val = value.Get<float>();
        if (val == 1.0f)
        {
            animator.SetBool("Jump", false);
            animator.SetBool("JumpStart", true);
            isJumpHold = true;
            currentJumpImpulse = 0f;
        }
        else
        {
            // Make jumps consistent regardless of current vertical motion.
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            float jumpMultiplier = currentJumpImpulse / jumpImpulseHoldMax;
            float jumpImpulse = jumpImpulseMin + (jumpImpulseMax - jumpImpulseMin) * jumpMultiplier;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            lastJumpTime = Time.time;
            animator.SetBool("Jump", true);
            animator.SetBool("JumpStart", false);
            isJumpHold = false;
        }
    }

    private void HoldJump(float delta)
    {
        currentJumpImpulse = Mathf.Clamp(currentJumpImpulse + delta, 0, jumpImpulseHoldMax);
    }

    private bool IsGrounded()
    {
        return TryGetGroundHit(out _);
    }

    private bool TryGetGroundHit(out RaycastHit hit)
    {
        return Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayer);
    }

    private void EnterRagdoll()
    {
        if (isRagdoll) return;
        isRagdoll = true;

        if (animator != null) animator.enabled = false;
        if (boardAnimator != null) boardAnimator.enabled = false;

        SetControllerEnabled(false);
        SetRagdollEnabled(true);

        // Give the ragdoll the incoming motion so the transition feels continuous.
        if (ragdollPlayerRigidbody != null)
        {
            ragdollPlayerRigidbody.linearVelocity = lastAirVelocity;
        }

        if (ragdollBoardRigidbody != null)
        {
            ragdollBoardRigidbody.linearVelocity = lastAirVelocity;
        }
    }

    private void SetControllerEnabled(bool enabled)
    {
        if (controllerPlayerCollider != null) controllerPlayerCollider.enabled = enabled;
        if (controllerPlayerRigidbody != null)
        {
            controllerPlayerRigidbody.isKinematic = !enabled;
            controllerPlayerRigidbody.detectCollisions = enabled;
            if (!enabled)
            {
                controllerPlayerRigidbody.linearVelocity = Vector3.zero;
                controllerPlayerRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void SetRagdollEnabled(bool enabled)
    {
        if (ragdollPlayerCollider != null) ragdollPlayerCollider.enabled = enabled;
        if (ragdollPlayerRigidbody != null)
        {
            ragdollPlayerRigidbody.isKinematic = !enabled;
            ragdollPlayerRigidbody.detectCollisions = enabled;
            ragdollPlayerRigidbody.useGravity = enabled;
        }

        if (ragdollBoardCollider != null) ragdollBoardCollider.enabled = enabled;
        if (ragdollBoardRigidbody != null)
        {
            ragdollBoardRigidbody.isKinematic = !enabled;
            ragdollBoardRigidbody.detectCollisions = enabled;
            ragdollBoardRigidbody.useGravity = enabled;
        }
    }

    private void MovePlayer(bool grounded)
    {
        float appliedTurnSpeed = grounded ? turnSpeed : (turnSpeed * airTurnSpeedMultiplier);
        float appliedAcceleration = grounded ? acceleration : (acceleration * airAccelerationMultiplier);

        // Update rotation (yaw) based on horizontal input
        bool canTurnInAir = grounded || Time.time >= suppressAirTurnUntil;
        if (Mathf.Abs(playerInput.x) > 0.01f && (grounded || canTurnInAir))
        {
            currentYaw += playerInput.x * appliedTurnSpeed * Time.fixedDeltaTime;
        }

        // Calculate forward direction based on current yaw
        Vector3 forwardDirection = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        
        if (Mathf.Abs(throttle) > 0.1f)
        {
            // Apply acceleration in the forward direction based on vertical input
            rb.AddForce(forwardDirection * (throttle * appliedAcceleration), ForceMode.Acceleration);
        }
        else if (grounded)
        {
            // Apply friction when no forward/backward input
            Vector3 velocity = rb.linearVelocity;
            velocity.x *= friction;
            velocity.z *= friction;
            rb.linearVelocity = velocity;
        }

        // Clamp speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        float allowedMaxSpeed = maxSpeed;
        if (Time.time < allowOverspeedUntil)
        {
            allowedMaxSpeed = maxSpeed * Mathf.Max(1f, landingTrickBoostMaxSpeedMultiplier);
        }

        if (allowedMaxSpeed > 0.0001f && horizontalVelocity.magnitude > allowedMaxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * allowedMaxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    public void OnTrick(InputValue value)
    {
        isTricking = value.Get<float>();

        bool pressed = isTricking > 0.5f && wasTricking <= 0.5f;
        wasTricking = isTricking;

        if (!pressed) return;
        if (trickLocked) return;
        if (IsGrounded()) return;

        TrickData trick = FindBestTrickFromBuffer();
        if (trick != null)
        {
            ExecuteTrick(trick);
        }
    }

    private void BufferDirection(Direction direction)
    {
        bufferedDirections.Add(direction);
        if (bufferedDirections.Count > maxBufferedDirections && maxBufferedDirections > 0)
        {
            bufferedDirections.RemoveAt(0);
        }
    }

    private bool TryMapInputToDirection(Vector2 input, out Direction direction)
    {
        direction = Direction.Up;

        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);

        if (absX < trickDirectionDeadzone && absY < trickDirectionDeadzone)
        {
            return false;
        }

        if (absX >= absY)
        {
            direction = input.x < 0f ? Direction.Left : Direction.Right;
            return true;
        }

        direction = input.y < 0f ? Direction.Down : Direction.Up;
        return true;
    }

    private TrickData FindBestTrickFromBuffer()
    {
        if (tricks == null || tricks.Count == 0) return null;
        if (bufferedDirections.Count == 0) return null;

        TrickData best = null;
        int bestLen = 0;

        foreach (TrickData trick in tricks)
        {
            if (trick == null || trick.directions == null) continue;
            int len = trick.directions.Count;
            if (len <= 0) continue;
            if (len > bufferedDirections.Count) continue;
            if (len <= bestLen) continue;

            if (MatchesSuffix(bufferedDirections, trick.directions))
            {
                best = trick;
                bestLen = len;
            }
        }

        return best;
    }

    private static bool MatchesSuffix(List<Direction> buffer, List<Direction> pattern)
    {
        int len = pattern.Count;
        int start = buffer.Count - len;
        for (int i = 0; i < len; i++)
        {
            if (buffer[start + i] != pattern[i]) return false;
        }

        return true;
    }

    private void TryExecuteSingleDirectionTrick(Direction direction)
    {
        if (trickLocked) return;
        if (IsGrounded()) return;

        if (tricks == null) return;
        foreach (TrickData trick in tricks)
        {
            if (trick == null || trick.directions == null) continue;
            if (trick.directions.Count != 1) continue;
            if (trick.directions[0] != direction) continue;

            ExecuteTrick(trick);
            return;
        }
    }

    private void ExecuteTrick(TrickData trick)
    {
        if (trick == null) return;

        trickLocked = true;

        if (!IsGrounded())
        {
            landingBoostPending = true;
        }

        if (animator != null && trick.clip != null)
        {
            animator.Play(trick.clip.name);
        }
        else
        {
            //Debug.LogWarning($"Trick '{(trick != null ? trick.trickName : "<null>")}' could not be played (missing Animator or trickName).", this);
        }

        if (boardAnimator != null && trick.boardClip != null)
        {
            boardAnimator.Play(trick.boardClip.name);
        }

        float duration = Mathf.Max(0.01f, trick.trickTime);
        StartCoroutine(UnlockTrickAfter(duration));
    }

    private System.Collections.IEnumerator UnlockTrickAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        trickLocked = false;
    }

    private void ApplyLandingTrickBoost()
    {
        if (rb == null) return;
        if (landingTrickBoostImpulse <= 0f) return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 dir;

        if (horizontalVelocity.magnitude >= landingTrickBoostMinHorizontalSpeed)
        {
            dir = horizontalVelocity.normalized;
        }
        else
        {
            dir = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;
        }

        rb.AddForce(dir * landingTrickBoostImpulse, ForceMode.Impulse);

        if (landingTrickBoostOverspeedDuration > 0f && landingTrickBoostMaxSpeedMultiplier > 1f)
        {
            allowOverspeedUntil = Time.time + landingTrickBoostOverspeedDuration;
        }
    }

    private void UpdateMeshTransform()
    {
        if (playerMesh == null) return;

        // Position follows exactly
        playerMesh.transform.position = transform.position;

        // Base rotation from yaw
        Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        Vector3 up = Vector3.up;

        // Adjust for slope
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            up = hit.normal;
        }

        // Calculate target rotation: 
        // We want to look 'forward' but align our 'up' with the surface normal.
        // Vector3.Cross(up, forward) gives us the 'right' vector.
        // Vector3.Cross(right, up) gives us a 'forward' vector that is orthogonal to 'up'.
        Vector3 right = Vector3.Cross(up, forward);
        Vector3 alignedForward = Vector3.Cross(right, up);
        
        Quaternion targetRotation = Quaternion.LookRotation(alignedForward, up);

        float turnInput = Mathf.Abs(playerInput.x) > turnInputDeadzone ? playerInput.x : 0f;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speedNormalized = maxSpeed > 0.0001f ? Mathf.Clamp01(horizontalVelocity.magnitude / maxSpeed) : 0f;

        float targetTurnTilt = -turnInput * maxTurnTiltDegrees * speedNormalized;
        currentTurnTilt = Mathf.Lerp(currentTurnTilt, targetTurnTilt, Time.fixedDeltaTime * turnTiltSmoothing);

        Quaternion bankRotation = Quaternion.AngleAxis(currentTurnTilt, targetRotation * Vector3.forward);
        Quaternion finalRotation = bankRotation * targetRotation;

        playerMesh.transform.rotation = Quaternion.Slerp(playerMesh.transform.rotation, finalRotation, Time.fixedDeltaTime * rotationSpeed);
    }

    private void UpdateCameraTargetTransform()
    {
        if (cameraTarget == null) return;

        // Position follows exactly
        cameraTarget.position = transform.position;

        // Smooth yaw/pitch while still aligning the target to ramps (ground normal).
        // We intentionally do NOT apply the mesh's turn-bank tilt here.

        float desiredYaw = currentYaw + cameraYawOffset;
        cameraYaw = Mathf.SmoothDampAngle(cameraYaw, desiredYaw, ref cameraYawVelocity, cameraYawSmoothTime);

        float desiredPitch = cameraPitch;
        cameraPitchCurrent = Mathf.SmoothDampAngle(cameraPitchCurrent, desiredPitch, ref cameraPitchVelocity, cameraPitchSmoothTime);

        // Determine and smooth the "up" direction from the ground when available.
        Vector3 desiredUp = Vector3.up;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            desiredUp = hit.normal;
        }

        cameraUpCurrent = Vector3.SmoothDamp(cameraUpCurrent, desiredUp, ref cameraUpVelocity, cameraUpSmoothTime);
        Vector3 up = cameraUpCurrent.sqrMagnitude > 0.0001f ? cameraUpCurrent.normalized : Vector3.up;

        // Build a slope-aligned base rotation from the smoothed yaw.
        Vector3 forward = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
        Vector3 alignedForward = Vector3.ProjectOnPlane(forward, up);
        if (alignedForward.sqrMagnitude < 0.0001f)
        {
            alignedForward = Vector3.ProjectOnPlane(Vector3.forward, up);
        }
        alignedForward.Normalize();
        Quaternion baseRotation = Quaternion.LookRotation(alignedForward, up);

        // Apply pitch around the base rotation's local right axis.
        Quaternion pitchRotation = Quaternion.AngleAxis(cameraPitchCurrent, baseRotation * Vector3.right);
        cameraTarget.rotation = pitchRotation * baseRotation;
    }
}
