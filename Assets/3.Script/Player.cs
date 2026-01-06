using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceSoundProfile
    {
        public string surfaceTag;
        public List<AudioClip> walkSounds;
        public List<AudioClip> sprintSounds;
        public List<AudioClip> jumpStartSounds;
        public List<AudioClip> landingSounds;
    }

    [Header("--- Components ---")]
    [SerializeField] private Transform cameraRoot;

    [Header("--- Movement Settings ---")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("--- Stamina Settings ---")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("초당 스테미나 소모량")]
    [SerializeField] private float staminaDrainRate = 15f;
    [Tooltip("초당 스테미나 회복량")]
    [SerializeField] private float staminaRegenRate = 10f;
    [Tooltip("완전히 지쳤을 때 회복 대기 시간")]
    [SerializeField] private float exhaustionPenaltyTime = 3f;
    [SerializeField] private Slider staminaSlider;

    [Header("--- Look Settings ---")]
    [SerializeField] private float mouseSensitivity = 15.0f;
    [SerializeField] private float lookXLimit = 80.0f;

    [Header("--- Head Bobbing Settings ---")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobSpeed = 14f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;
    private float defaultPosY = 0;
    private float bobTimer = 0;

    [Header("--- FlashLight Settings ---")]
    [SerializeField] private float flashCooldown = 0.5f;
    private float lastFlashTime = -1f;

    [Header("--- Landing Settings ---")]
    [SerializeField] private float minAirTime = 0.25f;
    private float airTime = 0f;

    [Header("--- Audio Settings ---")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private float footstepVolume = 0.5f;

    [Header("--- Breathing Clips ---")]
    [SerializeField] private AudioClip runBreathSound;
    [SerializeField] private AudioClip exhaustedBreathSound;

    [Header("--- Surface Sounds ---")]
    [SerializeField] private SurfaceSoundProfile defaultSurface;
    [SerializeField] private List<SurfaceSoundProfile> surfaceProfiles;

    private CharacterController characterController;
    private FlashLight flashLight;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 playerVelocity;
    private float xRotation = 0f;

    private bool isSprinting = false;
    private bool wasGrounded = true;
    private bool hasStepped = false;

    private float currentStamina;
    private bool isExhausted = false;
    private float exhaustedTimer = 0f;

    private void Start()
    {
        if (!TryGetComponent(out characterController)) Debug.LogError("CharacterController 없음!");
        if (footstepSource == null) Debug.LogWarning("Footstep AudioSource가 연결되지 않았습니다.");
        if (breathingSource == null) Debug.LogWarning("Breathing AudioSource가 연결되지 않았습니다.");

        if (cameraRoot != null) defaultPosY = cameraRoot.localPosition.y; else Debug.LogError("Camera Root 없음!");

        flashLight = GetComponentInChildren<FlashLight>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        staminaSlider.value = maxStamina;
    }

    private void Update()
    {
        if (!characterController.isGrounded)
        {
            airTime += Time.deltaTime;
        }

        wasGrounded = characterController.isGrounded;
        staminaSlider.value = currentStamina;

        HandleStamina();
        HandleMovement();
        HandleRotation();
        HandleHeadBob();
        HandleLanding();
    }

    private void HandleStamina()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (isSprinting && isMoving && !isExhausted)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (breathingSource != null && runBreathSound != null)
            {
                if (breathingSource.clip != runBreathSound || !breathingSource.isPlaying)
                {
                    breathingSource.clip = runBreathSound;
                    breathingSource.loop = true;
                    breathingSource.Play();
                }
            }

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
                exhaustedTimer = exhaustionPenaltyTime;
                isSprinting = false;

                if (breathingSource != null && exhaustedBreathSound != null)
                {
                    breathingSource.clip = exhaustedBreathSound;
                    breathingSource.loop = false;
                    breathingSource.Play();
                }
            }
        }
        else
        {
            if (isExhausted)
            {
                exhaustedTimer -= Time.deltaTime;
                if (exhaustedTimer <= 0f)
                {
                    isExhausted = false;
                }
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;

                if (breathingSource != null && breathingSource.isPlaying && breathingSource.clip == runBreathSound)
                {
                    breathingSource.Stop();
                }
            }

            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = (isSprinting && !isExhausted && characterController.isGrounded) ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (characterController.isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity * Time.deltaTime);
        xRotation -= lookInput.y * mouseSensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -lookXLimit, lookXLimit);
        if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraRoot == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.1f;

        if (isMoving && characterController.isGrounded)
        {
            bool actuallySprinting = isSprinting && !isExhausted;
            float speedMultiplier = actuallySprinting ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * (bobSpeed * speedMultiplier);

            float sinValue = Mathf.Sin(bobTimer);
            float currentBobAmount = actuallySprinting ? bobAmount * sprintBobMultiplier : bobAmount;
            float newY = defaultPosY + sinValue * currentBobAmount;

            cameraRoot.localPosition = new Vector3(cameraRoot.localPosition.x, newY, cameraRoot.localPosition.z);

            if (sinValue <= -0.95f && !hasStepped)
            {
                PlayFootstepSound();
                hasStepped = true;
            }
            else if (sinValue > -0.8f && hasStepped)
            {
                hasStepped = false;
            }
        }
        else
        {
            bobTimer = 0;
            hasStepped = false;
            float newY = Mathf.Lerp(cameraRoot.localPosition.y, defaultPosY, Time.deltaTime * bobSpeed);
            cameraRoot.localPosition = new Vector3(cameraRoot.localPosition.x, newY, cameraRoot.localPosition.z);
        }
    }

    private void HandleLanding()
    {
        if (!wasGrounded && characterController.isGrounded)
        {
            if (airTime > minAirTime)
            {
                SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
                PlayRandomClip(currentProfile.landingSounds, footstepVolume * 1.2f);
            }
            airTime = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
        bool actuallySprinting = isSprinting && !isExhausted;
        List<AudioClip> clipsToPlay = actuallySprinting ? currentProfile.sprintSounds : currentProfile.walkSounds;

        PlayRandomClip(clipsToPlay, footstepVolume);
    }

    private SurfaceSoundProfile GetCurrentSurfaceProfile()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2.0f))
        {
            foreach (var profile in surfaceProfiles)
            {
                if (hit.collider.CompareTag(profile.surfaceTag))
                {
                    return profile;
                }
            }
        }
        return defaultSurface;
    }

    private void PlayRandomClip(List<AudioClip> clips, float volume)
    {
        if (clips == null || clips.Count == 0) return;

        int index = Random.Range(0, clips.Count);
        footstepSource.pitch = Random.Range(0.9f, 1.1f);
        footstepSource.PlayOneShot(clips[index], volume);
    }

    private void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    private void OnLook(InputValue value) { lookInput = value.Get<Vector2>(); }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && characterController.isGrounded && !isExhausted)
        {
            SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
            PlayRandomClip(currentProfile.jumpStartSounds, footstepVolume);

            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void OnInteraction(InputValue value) { if (value.isPressed) Debug.Log("상호작용"); }

    private void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    private void OnFlashLight(InputValue value)
    {
        if (value.isPressed && flashLight != null)
        {
            if (Time.time >= lastFlashTime + flashCooldown)
            {
                flashLight.ToggleFlashlight();
                lastFlashTime = Time.time;
            }
        }
    }

    public Vector2 GetLookInput() { return lookInput; }
}