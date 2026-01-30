using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerMesh;
    
    [Header("Movement Settings")]
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float friction = 0.95f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float raycastDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private Vector2 playerInput;
    private float currentYaw;

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
    }

    void FixedUpdate()
    {
        MovePlayer();
        UpdateMeshTransform();
    }

    public void OnMove(InputValue value)
    {
        playerInput = value.Get<Vector2>();
    }

    private void MovePlayer()
    {
        // Update rotation (yaw) based on horizontal input
        if (Mathf.Abs(playerInput.x) > 0.01f)
        {
            currentYaw += playerInput.x * turnSpeed * Time.fixedDeltaTime;
        }

        // Calculate forward direction based on current yaw
        Vector3 forwardDirection = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
        
        if (Mathf.Abs(playerInput.y) > 0.1f)
        {
            // Apply acceleration in the forward direction based on vertical input
            rb.AddForce(forwardDirection * (playerInput.y * acceleration), ForceMode.Acceleration);
        }
        else
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
        playerMesh.transform.rotation = Quaternion.Slerp(playerMesh.transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
    }
}
