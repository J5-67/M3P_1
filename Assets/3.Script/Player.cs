using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceSoundProfile
    {
        public string surfaceTag; // 바닥의 태그 이름 (예: "Wood", "Stone")
        public List<AudioClip> walkSounds;       // 걷기
        public List<AudioClip> sprintSounds;     // 달리기
        public List<AudioClip> jumpStartSounds;  // 점프 시작
        public List<AudioClip> landingSounds;    // 착지
    }

    [Header("--- Components ---")]
    [SerializeField] private Transform cameraRoot;

    [Header("--- Movement Settings ---")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

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
    [SerializeField] private float footstepVolume = 0.5f;

    [Header("--- Surface Sounds ---")]
    [Tooltip("태그가 없는 바닥에서 날 기본 소리")]
    [SerializeField] private SurfaceSoundProfile defaultSurface;

    [Tooltip("태그별 바닥 소리 목록")]
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

    private void Start()
    {
        if (!TryGetComponent(out characterController)) Debug.LogError("CharacterController 없음!");
        if (footstepSource == null) TryGetComponent(out footstepSource);
        if (cameraRoot != null) defaultPosY = cameraRoot.localPosition.y; else Debug.LogError("Camera Root 없음!");

        flashLight = GetComponentInChildren<FlashLight>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!characterController.isGrounded)
        {
            airTime += Time.deltaTime;
        }
        else
        {
            // 땅에 있을 때 airTime 초기화는 HandleLanding에서 처리
        }

        wasGrounded = characterController.isGrounded;

        HandleMovement();
        HandleRotation();
        HandleHeadBob();
        HandleLanding();
    }

    private void HandleMovement()
    {
        float currentSpeed = (isSprinting && characterController.isGrounded) ? sprintSpeed : walkSpeed;

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
            float speedMultiplier = isSprinting ? sprintBobMultiplier : 1f;
            bobTimer += Time.deltaTime * (bobSpeed * speedMultiplier);

            float sinValue = Mathf.Sin(bobTimer);
            float currentBobAmount = isSprinting ? bobAmount * sprintBobMultiplier : bobAmount;
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
                // [유니] 현재 바닥에 맞는 착지 소리 재생!
                SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
                PlayRandomClip(currentProfile.landingSounds, footstepVolume * 1.2f);
            }
            airTime = 0f;
        }
    }

    // [유니] 발소리 재생 (재질 확인 포함)
    private void PlayFootstepSound()
    {
        SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
        List<AudioClip> clipsToPlay = isSprinting ? currentProfile.sprintSounds : currentProfile.walkSounds;

        PlayRandomClip(clipsToPlay, footstepVolume);
    }

    private SurfaceSoundProfile GetCurrentSurfaceProfile()
    {
        // 발밑으로 레이저를 쏴서 뭐가 있는지 확인해
        RaycastHit hit;
        // 캐릭터 중심에서 아래로 1.5m 정도 쏴봄 (발바닥 위치 확인)
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 2.0f))
        {
            // 부딪힌 물체의 태그를 확인!
            foreach (var profile in surfaceProfiles)
            {
                if (hit.collider.CompareTag(profile.surfaceTag))
                {
                    return profile; // 태그가 일치하는 프로필 발견!
                }
            }
        }

        // 아무것도 안 걸리거나 태그가 없으면 기본 소리 반환
        return defaultSurface;
    }

    private void PlayRandomClip(List<AudioClip> clips, float volume)
    {
        if (clips == null || clips.Count == 0) return;

        int index = Random.Range(0, clips.Count);
        footstepSource.pitch = Random.Range(0.9f, 1.1f);
        footstepSource.PlayOneShot(clips[index], volume);
    }

    // --- Input System Messages ---
    private void OnMove(InputValue value) { moveInput = value.Get<Vector2>(); }
    private void OnLook(InputValue value) { lookInput = value.Get<Vector2>(); }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && characterController.isGrounded)
        {
            // [유니] 점프 시작 소리도 재질에 맞게!
            SurfaceSoundProfile currentProfile = GetCurrentSurfaceProfile();
            PlayRandomClip(currentProfile.jumpStartSounds, footstepVolume);

            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void OnInteraction(InputValue value) { if (value.isPressed) Debug.Log("상호작용"); }
    private void OnSprint(InputValue value) { isSprinting = value.isPressed; }

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