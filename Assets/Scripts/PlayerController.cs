using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerMesh;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Animator animator;
    
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

    private Rigidbody rb;
    private Vector2 playerInput;
    private float currentYaw;
    private float lastJumpTime;
    private float currentTurnTilt;

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
        
        // Ensure Rigidbody is configured for a skating feel
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        // Initialize yaw based on current rotation
        currentYaw = transform.eulerAngles.y;

        // Initialize camera target smoothing to current values to avoid a startup snap.
        cameraYaw = currentYaw + cameraYawOffset;
        cameraPitchCurrent = cameraPitch;
        cameraUpCurrent = Vector3.up;
    }

    void Update()
    {
        UpdateMeshTransform();
        UpdateCameraTargetTransform();
        if (isJumpHold) HoldJump(Time.deltaTime);
    }

    void FixedUpdate()
    {
        bool grounded = IsGrounded();
        MovePlayer(grounded);
    }

    public void OnMove(InputValue value)
    {
        playerInput = value.Get<Vector2>();
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
        return Physics.Raycast(transform.position, Vector3.down, raycastDistance, groundLayer);
    }

    private void MovePlayer(bool grounded)
    {
        float appliedTurnSpeed = grounded ? turnSpeed : (turnSpeed * airTurnSpeedMultiplier);
        float appliedAcceleration = grounded ? acceleration : (acceleration * airAccelerationMultiplier);

        // Update rotation (yaw) based on horizontal input
        if (Mathf.Abs(playerInput.x) > 0.01f)
        {
            currentYaw += playerInput.x * appliedTurnSpeed * Time.fixedDeltaTime;
        }

        // Calculate forward direction based on current yaw
        Vector3 forwardDirection = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        
        if (Mathf.Abs(playerInput.y) > 0.1f)
        {
            // Apply acceleration in the forward direction based on vertical input
            rb.AddForce(forwardDirection * (playerInput.y * appliedAcceleration), ForceMode.Acceleration);
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
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
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
