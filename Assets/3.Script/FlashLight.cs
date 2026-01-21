using UnityEngine;
using UnityEngine.UI; // 배터리 UI가 있다면 필요

public class FlashLight : MonoBehaviour
{
    [Header("--- Components ---")]
    [SerializeField] private Light flashlight;
    [SerializeField] private GameObject volumetricBeam;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("--- Battery Settings ---")]
    public float maxBattery = 100f;
    public float currentBattery;
    [Tooltip("초당 배터리 소모량")]
    public float batteryDrainRate = 2f;
    public Slider batterySlider; // (선택) 배터리 잔량 UI

    [Header("--- Sway Settings ---")]
    [SerializeField] private float smooth = 8f;
    [SerializeField] private float swayMultiplier = 2f;

    private Player playerScript;
    private bool isFlashLightOn = true;

    private void Start()
    {
        if (!transform.root.TryGetComponent(out playerScript))
        {
            Debug.LogError("Player 스크립트를 찾을 수 없습니다.");
        }

        // 시작할 때 배터리 꽉 채우기
        currentBattery = maxBattery;
        UpdateFlashlightState();
    }

    private void Update()
    {
        HandleSway();
        HandleBattery();
    }

    private void HandleSway()
    {
        if (playerScript == null) return;
        Vector2 lookInput = playerScript.GetLookInput();
        float mouseX = lookInput.x * swayMultiplier;
        float mouseY = lookInput.y * swayMultiplier;
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);
        Quaternion targetRotation = rotationX * rotationY;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
    }

    private void HandleBattery()
    {
        // 켜져 있을 때만 배터리 소모
        if (isFlashLightOn && currentBattery > 0)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;

            // 배터리가 다 닳으면 꺼짐
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                isFlashLightOn = false;
                UpdateFlashlightState();
            }
        }

        // UI 업데이트
        if (batterySlider != null)
        {
            batterySlider.value = currentBattery / maxBattery;
        }
    }

    public void ToggleFlashlight()
    {
        // 배터리가 없으면 못 킴
        if (currentBattery <= 0) return;

        isFlashLightOn = !isFlashLightOn;
        UpdateFlashlightState();

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void UpdateFlashlightState()
    {
        if (flashlight != null) flashlight.enabled = isFlashLightOn;
        if (volumetricBeam != null) volumetricBeam.SetActive(isFlashLightOn);
    }

    // [핵심] 배터리 아이템 먹었을 때 호출할 함수
    public void RestoreBattery()
    {
        currentBattery = maxBattery;
        // 충전되면 자동으로 켜지게 할지, 아니면 그냥 충전만 할지 선택 (여기선 충전만)
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound); // 충전 소리 재생 (선택)
    }
}