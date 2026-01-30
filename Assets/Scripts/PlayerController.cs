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
    [SerializeField] private float jumpImpulse = 7f;
    [SerializeField] private float jumpCooldown = 0.1f;

    [Header("Turn Tilt Settings")]
    [SerializeField, Range(0f, 45f)] private float maxTurnTiltDegrees = 12f;
    [SerializeField, Range(0.1f, 30f)] private float turnTiltSmoothing = 10f;
    [SerializeField, Range(0f, 0.5f)] private float turnInputDeadzone = 0.02f;

    private Rigidbody rb;
    private Vector2 playerInput;
    private float currentYaw;
    private float lastJumpTime;
    private float currentTurnTilt;

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
    }

    void Update()
    {
        UpdateMeshTransform();
        UpdateCameraTargetTransform();
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
        Debug.Log(value.Get<float>());
        if (Time.time < lastJumpTime + jumpCooldown) return;
        if (!IsGrounded()) return;

        // Make jumps consistent regardless of current vertical motion.
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
        lastJumpTime = Time.time;
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
        float targetTurnTilt = -turnInput * maxTurnTiltDegrees;
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

        // Base rotation from yaw
        Vector3 forward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        Vector3 up = Vector3.up;

        // Adjust for slope (same as mesh), but do NOT apply turn banking tilt.
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            up = hit.normal;
        }

        Vector3 right = Vector3.Cross(up, forward);
        Vector3 alignedForward = Vector3.Cross(right, up);

        Quaternion targetRotation = Quaternion.LookRotation(alignedForward, up);

        cameraTarget.rotation = Quaternion.Slerp(cameraTarget.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
    }
}
