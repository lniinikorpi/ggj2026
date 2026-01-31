using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject playerMesh;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<Animator> animators;
    [SerializeField] private Animator boardAnimator;
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private RandomBailText randomBailText;
    [SerializeField] private AudioSource boardAudioSource;
    [SerializeField] private AudioSource jumpAudioSource;
    [SerializeField] private AudioSource landAudioSource;
    [SerializeField] private AudioSource airWheelAudioSource;

    [Header("Board Audio")]
    [SerializeField, Range(0f, 1f)] private float boardAudioMaxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float boardAudioMinPitch = 0.85f;
    [SerializeField, Range(0.1f, 3f)] private float boardAudioMaxPitch = 1.25f;

    [Header("Customization")]
    [SerializeField] private List<SkinnedMeshRenderer> maskRenderers;
    [SerializeField] private List<Material> maskMaterials;

    [Header("Ragdoll")]
    [SerializeField] private Rigidbody controllerPlayerRigidbody;
    [SerializeField] private Collider controllerPlayerCollider;

    [SerializeField] private Rigidbody ragdollPlayerRigidbody;
    [SerializeField] private Collider ragdollPlayerCollider;
    [SerializeField] private Rigidbody ragdollBoardRigidbody;
    [SerializeField] private Collider ragdollBoardCollider;
    [SerializeField] private Rigidbody ragdollMask1Rigidbody;
    [SerializeField] private Collider ragdollMask1Collider;
    [SerializeField] private Rigidbody ragdollMask2Rigidbody;
    [SerializeField] private Collider ragdollMask2Collider;

    [Header("Respawn")]
    [SerializeField] private float ragdollRespawnDelay = 2f;

    [SerializeField, Range(0.01f, 1f)] private float respawnFadeInDuration = 0.08f;
    [SerializeField, Range(0.01f, 2f)] private float respawnFadeOutDuration = 0.25f;

    [SerializeField] private GameDataSO gameData;
    
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
    [SerializeField, Range(0.1f, 1f)] private float trickDirectionDeadzone = 0.5f;
    
    [SerializeField] private float trickClearDuration = 2f;
    
    [Header("Trick Movement Helpers")]
    [SerializeField, Range(0f, 0.5f)] private float airTurnSuppressDuration = 0.12f;

    [Header("Landing Boost")]
    [SerializeField] private float landingTrickBoostImpulse = 3.0f;
    [SerializeField] private float landingTrickBoostMinHorizontalSpeed = 0.25f;
    [SerializeField, Range(1f, 3f)] private float landingTrickBoostMaxSpeedMultiplier = 1.5f;
    [SerializeField, Range(0f, 3f)] private float landingTrickBoostOverspeedDuration = 0.75f;

    
 
    [SerializeField] private int maxBufferedDirections = 8;

    
    
    
    private float isTricking;
    private float wasTricking;
    private float lastTrickTime;
    private bool isTrickScorePending = false;
    
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

    private Vector3 startSpawnPosition;
    private Quaternion startSpawnRotation;
    private Vector3 currentRespawnPosition;
    private Quaternion currentRespawnRotation;

    private Vector3 ragdollPlayerOffset;
    private Quaternion ragdollPlayerRotOffset;
    private Vector3 ragdollBoardOffset;
    private Quaternion ragdollBoardRotOffset;
    private Vector3 ragdollMask1Offset;
    private Quaternion ragdollMask1RotOffset;
    private Vector3 ragdollMask2Offset;
    private Quaternion ragdollMask2RotOffset;

    private Coroutine respawnRoutine;
    private Coroutine fadeRoutine;

    private float cameraYaw;
    private float cameraPitchCurrent;
    private float cameraYawVelocity;
    private float cameraPitchVelocity;
    private Vector3 cameraUpCurrent;
    private Vector3 cameraUpVelocity;

    private Tracker tracker;

   
    
    
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

        // Cache initial spawn and default respawn point.
        startSpawnPosition = transform.position;
        startSpawnRotation = transform.rotation;
        currentRespawnPosition = startSpawnPosition;
        currentRespawnRotation = startSpawnRotation;

        tracker = GetComponent<Tracker>();

        CacheRagdollOffsets();

        // Initialize camera target smoothing to current values to avoid a startup snap.
        cameraYaw = currentYaw + cameraYawOffset;
        cameraPitchCurrent = cameraPitch;
        cameraUpCurrent = Vector3.up;

        // Ensure ragdoll physics are disabled on start (they can be enabled on fall).
        SetRagdollEnabled(false);
    }

    private void CacheRagdollOffsets()
    {
        if (ragdollPlayerRigidbody != null)
        {
            // Cache in controller-local space so we can reapply correctly after respawn rotations.
            ragdollPlayerOffset = ragdollPlayerRigidbody.transform.localPosition;
            ragdollPlayerRotOffset = ragdollPlayerRigidbody.transform.localRotation;
        }

        if (ragdollBoardRigidbody != null)
        {
            ragdollBoardOffset = ragdollBoardRigidbody.transform.localPosition;
            ragdollBoardRotOffset = ragdollBoardRigidbody.transform.localRotation;
        }

        if (ragdollMask1Rigidbody != null)
        {
            ragdollMask1Offset = ragdollMask1Rigidbody.transform.localPosition;
            ragdollMask1RotOffset = ragdollMask1Rigidbody.transform.localRotation;
        }

        if (ragdollMask2Rigidbody != null)
        {
            ragdollMask2Offset = ragdollMask2Rigidbody.transform.localPosition;
            ragdollMask2RotOffset = ragdollMask2Rigidbody.transform.localRotation;
        }
    }

    private void Start()
    {
        ApplySavedCustomization();
    }

    private void ApplySavedCustomization()
    {
        if (maskRenderers == null || maskRenderers.Count == 0) return;

        SaveData save = SaveSystem.LoadGame();

        int selectedMask = Mathf.Clamp(save.selectedMaskIndex, 0, maskRenderers.Count - 1);
        for (int i = 0; i < maskRenderers.Count; i++)
        {
            if (maskRenderers[i] != null)
                maskRenderers[i].gameObject.SetActive(i == selectedMask);
        }

        if (maskMaterials == null || maskMaterials.Count == 0) return;

        int selectedMaterial = Mathf.Clamp(save.selectedMaskMaterialIndex, 0, maskMaterials.Count - 1);
        Material mat = maskMaterials[selectedMaterial];
        if (mat != null && maskRenderers[selectedMask] != null)
        {
            maskRenderers[selectedMask].material = mat;
        }
    }

    void Update()
    {
        if (isRagdoll)
        {
            ApplyBoardAudio(0f, boardAudioMinPitch);
            return;
        }

        UpdateMeshTransform();
        UpdateCameraTargetTransform();
        if (isJumpHold) HoldJump(Time.deltaTime);

        bool grounded = IsGrounded();
        if (grounded && !wasGrounded)
        {
            if (landAudioSource != null)
            {
                landAudioSource.Play();
            }

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

        if (Time.time - lastTrickTime > trickClearDuration && isTrickScorePending)
        {
            gameData.CalculateTrickScore();
            isTrickScorePending = false;
        }
    }

    void FixedUpdate()
    {
        if (isRagdoll)
        {
            ApplyBoardAudio(0f, boardAudioMinPitch);
            ApplyAirWheelAudio(0f, boardAudioMinPitch);
            return;
        }

        bool grounded = IsGrounded();

        if (!grounded)
        {
            lastAirVelocity = rb.linearVelocity;
        }

        MovePlayer(grounded);

        UpdateBoardAudio(grounded);
        UpdateAirWheelAudio(grounded);
        foreach (var anim in animators)
        {
            anim.SetFloat("Speed", rb.linearVelocity.magnitude);
        }
    }

    private void UpdateBoardAudio(bool grounded)
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        float t = maxSpeed <= 0f ? 0f : Mathf.Clamp01(speed / maxSpeed);

        float volume = grounded ? t * boardAudioMaxVolume : 0f;
        float pitch = Mathf.Lerp(boardAudioMinPitch, boardAudioMaxPitch, t);

        ApplyBoardAudio(volume, pitch);
    }

    private void ApplyBoardAudio(float volume, float pitch)
    {
        if (boardAudioSource == null) return;
        boardAudioSource.volume = volume;
        boardAudioSource.pitch = pitch;
    }

    private void UpdateAirWheelAudio(bool grounded)
    {
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        float t = maxSpeed <= 0f ? 0f : Mathf.Clamp01(speed / maxSpeed);

        // While airborne, use the wheel-in-air sound instead of the ground rolling sound.
        float volume = grounded ? 0f : t * boardAudioMaxVolume;
        float pitch = Mathf.Lerp(boardAudioMinPitch, boardAudioMaxPitch, t);

        ApplyAirWheelAudio(volume, pitch);
    }

    private void ApplyAirWheelAudio(float volume, float pitch)
    {
        if (airWheelAudioSource == null) return;

        airWheelAudioSource.volume = volume;
        airWheelAudioSource.pitch = pitch;

        // Avoid keeping the source running silently (helps if it isn't set to "Play On Awake").
        if (volume > 0.001f)
        {
            if (!airWheelAudioSource.isPlaying)
            {
                airWheelAudioSource.Play();
            }
        }
        else
        {
            if (airWheelAudioSource.isPlaying)
            {
                airWheelAudioSource.Stop();
            }
        }
    }

    private static bool CanWriteVelocity(Rigidbody body)
    {
        return body != null && !body.isKinematic;
    }

    private static readonly int JumpParam = Animator.StringToHash("Jump");
    private static readonly int JumpStartParam = Animator.StringToHash("JumpStart");

    private static bool HasBoolParameter(Animator animator, int parameterHash)
    {
        if (animator == null) return false;

        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool && param.nameHash == parameterHash)
            {
                return true;
            }
        }

        return false;
    }

    private static void SetBoolIfExists(Animator animator, int parameterHash, bool value)
    {
        if (!HasBoolParameter(animator, parameterHash)) return;
        animator.SetBool(parameterHash, value);
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
        if (!CanWriteVelocity(rb)) return;
        
        float val = value.Get<float>();
        if (val == 1.0f)
        {
            foreach (var anim in animators)
            {
                SetBoolIfExists(anim, JumpParam, false);
                SetBoolIfExists(anim, JumpStartParam, true);
            }
            isJumpHold = true;
            currentJumpImpulse = 0f;
        }
        else
        {
            // Make jumps consistent regardless of current vertical motion.
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            if (jumpAudioSource != null)
            {
                jumpAudioSource.Play();
            }
            rb.linearVelocity = velocity;

            float jumpMultiplier = currentJumpImpulse / jumpImpulseHoldMax;
            float jumpImpulse = jumpImpulseMin + (jumpImpulseMax - jumpImpulseMin) * jumpMultiplier;
            rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            lastJumpTime = Time.time;
            foreach (var anim in animators)
            {
                SetBoolIfExists(anim, JumpParam, true);
                SetBoolIfExists(anim, JumpStartParam, false);
            }
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

    private void EnterRagdoll(bool isWater = false)
    {
        if (isRagdoll) return;
        isRagdoll = true;

        if (animators != null)
        {
            foreach (var anim in animators)
            { 
                anim.enabled = false;
            }
        }

        if (boardAnimator != null) boardAnimator.enabled = false;

        SetControllerEnabled(false);
        SetRagdollEnabled(true);
        randomBailText.TriggerBail(isWater);

        // Give the ragdoll the incoming motion so the transition feels continuous.
        if (CanWriteVelocity(ragdollPlayerRigidbody))
        {
            ragdollPlayerRigidbody.linearVelocity = lastAirVelocity;
        }

        if (CanWriteVelocity(ragdollBoardRigidbody))
        {
            ragdollBoardRigidbody.linearVelocity = lastAirVelocity;
        }
        if (CanWriteVelocity(ragdollMask1Rigidbody))
        {
            ragdollMask1Rigidbody.linearVelocity = lastAirVelocity;
        }
        if (CanWriteVelocity(ragdollMask2Rigidbody))
        {
            ragdollMask2Rigidbody.linearVelocity = lastAirVelocity;
        }

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
        respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(ragdollRespawnDelay);

        // Fade only if a CanvasGroup has been assigned.
        if (fadeCanvas != null)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }
            fadeRoutine = StartCoroutine(RespawnFadeSequence());
        }
        else
        {
            Respawn();
        }

        respawnRoutine = null;
    }

    private System.Collections.IEnumerator RespawnFadeSequence()
    {
        yield return FadeCanvasAlphaTo(1f, respawnFadeInDuration);
        Respawn();
        yield return new WaitForSeconds(.5f);
        yield return FadeCanvasAlphaTo(0f, respawnFadeOutDuration);
        fadeRoutine = null;
    }

    private System.Collections.IEnumerator FadeCanvasAlphaTo(float targetAlpha, float duration)
    {
        if (fadeCanvas == null) yield break;

        float startAlpha = fadeCanvas.alpha;
        if (duration <= 0f)
        {
            fadeCanvas.alpha = targetAlpha;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerpT = Mathf.Clamp01(t / duration);
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, lerpT);
            yield return null;
        }
        fadeCanvas.alpha = targetAlpha;
    }

    public void Respawn()
    {
        // If we are not ragdolled, still allow a hard reset back to the last respawn point.
        // (Useful for debugging or external calls.)

        // Disable ragdoll first so we can safely teleport without physics fighting us.
        SetRagdollEnabled(false);
        SetControllerEnabled(true);

        isRagdoll = false;

        // Reset core motion/state so controller behaves consistently after teleport.
        if (CanWriteVelocity(rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        lastAirVelocity = Vector3.zero;
        isJumpHold = false;
        currentJumpImpulse = 0f;
        trickLocked = false;
        isTricking = 0f;
        wasTricking = 0f;
        lastTrickTime = 0f;
        isTrickScorePending = false;
        bufferedDirections.Clear();
        lastBufferedDirection = null;
        landingBoostPending = false;
        allowOverspeedUntil = 0f;
        suppressAirTurnUntil = 0f;

        // Pull the respawn point from the Tracker (single source of truth).
        if (tracker != null && tracker.TryGetLastCheckpointPose(out Vector3 checkpointPos, out Quaternion checkpointRot))
        {
            currentRespawnPosition = checkpointPos;
            currentRespawnRotation = checkpointRot;
        }
        else
        {
            currentRespawnPosition = startSpawnPosition;
            currentRespawnRotation = startSpawnRotation;
        }

        // Teleport controller.
        // IMPORTANT: this object is driven by a non-kinematic Rigidbody, so setting only the Transform
        // can be overwritten by the physics step (snapping back to the pre-teleport Rigidbody pose).
        // Set the Rigidbody pose directly to make the teleport stick.
        Rigidbody controllerBody = controllerPlayerRigidbody != null ? controllerPlayerRigidbody : rb;
        if (controllerBody != null)
        {
            if (CanWriteVelocity(controllerBody))
            {
                controllerBody.linearVelocity = Vector3.zero;
                controllerBody.angularVelocity = Vector3.zero;
            }
            controllerBody.position = currentRespawnPosition;
            controllerBody.rotation = currentRespawnRotation;
            controllerBody.Sleep();
        }
        transform.SetPositionAndRotation(currentRespawnPosition, currentRespawnRotation);
        Physics.SyncTransforms();
        currentYaw = currentRespawnRotation.eulerAngles.y;

        // Reset camera smoothing so we don't snap/spin after teleport.
        cameraYaw = currentYaw + cameraYawOffset;
        cameraPitchCurrent = cameraPitch;
        cameraYawVelocity = 0f;
        cameraPitchVelocity = 0f;
        cameraUpCurrent = Vector3.up;
        cameraUpVelocity = Vector3.zero;

        // Re-enable animators.
        if (animators != null)
        {
            foreach (var anim in animators)
            {
                if (anim == null) continue;
                anim.enabled = true;
                SetBoolIfExists(anim, JumpParam, false);
                SetBoolIfExists(anim, JumpStartParam, false);
            }
        }

        if (boardAnimator != null)
        {
            boardAnimator.enabled = true;
            SetBoolIfExists(boardAnimator, JumpParam, false);
            SetBoolIfExists(boardAnimator, JumpStartParam, false);
        }

        // Keep ragdoll parts with the player so we don't leave pieces behind in the scene.
        ResetRagdollTransformsToRespawn();
    }

    private void ResetRagdollTransformsToRespawn()
    {
        if (ragdollPlayerRigidbody != null)
        {
            ragdollPlayerRigidbody.transform.SetLocalPositionAndRotation(ragdollPlayerOffset, ragdollPlayerRotOffset);
            if (CanWriteVelocity(ragdollPlayerRigidbody))
            {
                ragdollPlayerRigidbody.linearVelocity = Vector3.zero;
                ragdollPlayerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (ragdollBoardRigidbody != null)
        {
            ragdollBoardRigidbody.transform.SetLocalPositionAndRotation(ragdollBoardOffset, ragdollBoardRotOffset);
            if (CanWriteVelocity(ragdollBoardRigidbody))
            {
                ragdollBoardRigidbody.linearVelocity = Vector3.zero;
                ragdollBoardRigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (ragdollMask1Rigidbody != null)
        {
            ragdollMask1Rigidbody.transform.SetLocalPositionAndRotation(ragdollMask1Offset, ragdollMask1RotOffset);
            if (CanWriteVelocity(ragdollMask1Rigidbody))
            {
                ragdollMask1Rigidbody.linearVelocity = Vector3.zero;
                ragdollMask1Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (ragdollMask2Rigidbody != null)
        {
            ragdollMask2Rigidbody.transform.SetLocalPositionAndRotation(ragdollMask2Offset, ragdollMask2RotOffset);
            if (CanWriteVelocity(ragdollMask2Rigidbody))
            {
                ragdollMask2Rigidbody.linearVelocity = Vector3.zero;
                ragdollMask2Rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void SetControllerEnabled(bool enabled)
    {
        if (controllerPlayerCollider != null) controllerPlayerCollider.enabled = enabled;
        if (controllerPlayerRigidbody != null)
        {
            if (!enabled)
            {
                if (CanWriteVelocity(controllerPlayerRigidbody))
                {
                    controllerPlayerRigidbody.linearVelocity = Vector3.zero;
                    controllerPlayerRigidbody.angularVelocity = Vector3.zero;
                }
            }

            controllerPlayerRigidbody.isKinematic = !enabled;
            controllerPlayerRigidbody.detectCollisions = enabled;
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
        if (ragdollMask1Collider != null) ragdollMask1Collider.enabled = enabled;
        if (ragdollMask1Rigidbody != null)
        {
            ragdollMask1Rigidbody.isKinematic = !enabled;
            ragdollMask1Rigidbody.detectCollisions = enabled;
            ragdollMask1Rigidbody.useGravity = enabled;
        }
        if (ragdollMask2Collider != null) ragdollMask2Collider.enabled = enabled;
        if (ragdollMask2Rigidbody != null)
        {
            ragdollMask2Rigidbody.isKinematic = !enabled;
            ragdollMask2Rigidbody.detectCollisions = enabled;
            ragdollMask2Rigidbody.useGravity = enabled;
        }
    }

    private void MovePlayer(bool grounded)
    {
        if (!CanWriteVelocity(rb)) return;

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

    public void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        
        lastTrickTime = Time.time;
        gameData.DoTrick(trick);
        isTrickScorePending = true;

        trickLocked = true;

        if (!IsGrounded())
        {
            landingBoostPending = true;
        }

        if (animators != null && trick.clip != null)
        {
            foreach (var anim in animators)
            {
                anim.Play(trick.clip.name);
            }
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

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Water")) {
            EnterRagdoll(true);
        }
    }
}
