using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("--- Interaction Settings ---")]
    [SerializeField] private float interactionDistance = 3.0f; // 상호작용 가능 거리
    [SerializeField] private LayerMask interactionLayer; // 아이템 레이어만 체크 (최적화)
    [SerializeField] private TMP_Text interactionText; // 화면 중앙 안내 텍스트 (예: "E 열쇠 획득")

    [Header("--- Health Settings ---")]
    public float maxHealth = 100f;
    public float currentHealth;
    public Image damageOverlay; // 맞으면 빨갛게 번쩍일 이미지
    public AudioClip damageSound; // 맞을 때 "윽!" 소리
    public float flashSpeed = 2f; // 화면 빨개진 거 사라지는 속도

    [Header("--- Inventory ---")]
    public int keyCount = 0;

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

    private Camera playerCamera;

    private void Start()
    {
        if (!TryGetComponent(out characterController)) Debug.LogError("CharacterController 없음!");
        if (footstepSource == null) Debug.LogWarning("Footstep AudioSource가 연결되지 않았습니다.");
        if (breathingSource == null) Debug.LogWarning("Breathing AudioSource가 연결되지 않았습니다.");

        if (cameraRoot != null) defaultPosY = cameraRoot.localPosition.y; else Debug.LogError("Camera Root 없음!");

        playerCamera = Camera.main;
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }

        flashLight = GetComponentInChildren<FlashLight>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHealth = maxHealth;
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

        HandleInteractionUI();
        HandleStamina();
        HandleMovement();
        HandleRotation();
        HandleHeadBob();
        HandleLanding();
        HandleDamageOverlay();
    }

    private void HandleInteractionUI()
    {
        // 혹시 카메라가 없으면 아무것도 안 함 (에러 방지)
        if (playerCamera == null) return;

        // [수정] cameraRoot 대신 playerCamera의 위치와 방향을 사용!
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        // 디버그용 초록색 선 (이제 눈에서 나가는 걸 볼 수 있음!)
        Debug.DrawRay(rayOrigin, rayDirection * interactionDistance, Color.green);

        RaycastHit hit;
        // [수정] 발사 위치와 방향을 변경된 변수로 교체
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance, interactionLayer))
        {
            // ... (나머지 코드는 그대로!) ...
            if (hit.collider.CompareTag("Key"))
            {
                interactionText.text = "[E] 열쇠 획득";
                interactionText.gameObject.SetActive(true);
            }
            else if (hit.collider.CompareTag("Battery"))
            {
                interactionText.text = "[E] 배터리 교체";
                interactionText.gameObject.SetActive(true);
            }
            else
            {
                interactionText.gameObject.SetActive(false);
            }
        }
        else
        {
            interactionText.gameObject.SetActive(false);
        }
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

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        // 1. 소리 재생
        if (footstepSource != null && damageSound != null)
        {
            footstepSource.PlayOneShot(damageSound, 1.0f);
        }

        // 2. 화면 빨갛게 만들기 (알파값 0.8로 확 올림)
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = 0.1f;
            damageOverlay.color = c;
        }

        // 3. 게임 오버 체크
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void HandleDamageOverlay()
    {
        if (damageOverlay != null)
        {
            // 색깔을 서서히 투명하게(Alpha -> 0) 만듦
            if (damageOverlay.color.a > 0)
            {
                Color c = damageOverlay.color;
                c.a = Mathf.Lerp(c.a, 0f, flashSpeed * Time.deltaTime);
                damageOverlay.color = c;
            }
        }
    }

    void Die()
    {
        Debug.Log("으악! 사망했습니다.");
        // 나중엔 여기에 'Game Over' 화면 띄우는 로직 넣으면 돼!
        // 일단은 움직임 멈추기
        walkSpeed = 0f;
        sprintSpeed = 0f;
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

    private void OnInteraction(InputValue value)
    {
        if (value.isPressed && playerCamera != null) // playerCamera 체크 추가
        {
            Vector3 rayOrigin = playerCamera.transform.position;
            Vector3 rayDirection = playerCamera.transform.forward;

            RaycastHit hit;
            // [수정] 여기도 cameraRoot -> rayOrigin, rayDirection으로 변경
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionDistance, interactionLayer))
            {
                // ... (아이템 먹는 로직 그대로) ...
                if (hit.collider.CompareTag("Key"))
                {
                    keyCount++;

                    GhostEnemy[] enemies = FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None);

                    foreach (GhostEnemy ghost in enemies)
                    {
                        ghost.IncreaseSpeed(1.0f); // 속도 1 증가
                    }

                    Destroy(hit.collider.gameObject);

                    if (interactionText != null)
                    {
                        interactionText.text = $"열쇠 획득! ({keyCount}/3)\n적이 빨라집니다...";
                        interactionText.gameObject.SetActive(true);
                    }
                }
                else if (hit.collider.CompareTag("Battery"))
                {
                    if (flashLight != null)
                    {
                        flashLight.RestoreBattery();
                        Destroy(hit.collider.gameObject);
                        // 텍스트 바로 꺼주기
                        if (interactionText != null) interactionText.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

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