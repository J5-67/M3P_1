using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
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

    [Header("--- Audio Settings ---")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private float footstepVolume = 0.5f;

    // [유니] 사운드 리스트 세분화!
    [Header("--- Audio Clips ---")]
    [SerializeField] private List<AudioClip> walkSounds;
    [SerializeField] private List<AudioClip> sprintSounds;
    [SerializeField] private List<AudioClip> jumpStartSounds;
    [SerializeField] private List<AudioClip> landingSounds;

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
        if (cameraRoot != null) defaultPosY = cameraRoot.localPosition.y; else Debug.LogError("Camera Root 없음!");
        if (!TryGetComponent(out characterController)) Debug.LogError("CharacterController 없음!");
        if (footstepSource == null) TryGetComponent(out footstepSource);

        flashLight = GetComponentInChildren<FlashLight>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
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
        if (!wasGrounded && characterController.isGrounded && playerVelocity.y < -0.1f)
        {
            PlayRandomClip(landingSounds, footstepVolume * 1.2f);
        }
    }

    private void PlayFootstepSound()
    {
        List<AudioClip> clipsToPlay = isSprinting ? sprintSounds : walkSounds;
        PlayRandomClip(clipsToPlay, footstepVolume);
    }

    private void PlayRandomClip(List<AudioClip> clips, float volume)
    {
        if (clips == null || clips.Count == 0) return;

        int index = Random.Range(0, clips.Count);

        footstepSource.pitch = Random.Range(0.9f, 1.1f);
        footstepSource.PlayOneShot(clips[index], volume);
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed && characterController.isGrounded)
        {
            PlayRandomClip(jumpStartSounds, footstepVolume);
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void OnInteraction(InputValue value)
    {
        if (value.isPressed) Debug.Log("상호작용");
    }

    // [유니] 달리기 입력 추가
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

    public Vector2 GetLookInput()
    {
        return lookInput;
    }
}