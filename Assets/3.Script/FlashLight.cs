using UnityEngine;
using UnityEngine.UI; // UI 기능을 위해 필수!

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

    [Header("--- UI Settings ---")]
    [Tooltip("화면에 보여질 배터리 아이콘 (Image)")]
    public Image batteryUI;

    [Tooltip("배터리 상태별 이미지 6장 (100% -> 0% 순서)")]
    public Sprite[] batterySprites; // 0:100%, 1:80%, 2:60%, 3:40%, 4:20%, 5:0%

    [Header("--- Sway Settings ---")]
    [SerializeField] private float smooth = 8f;
    [SerializeField] private float swayMultiplier = 2f;

    [Header("--- Enemy Detection ---")]
    public float lightRange = 15f;      // 빛이 닿는 거리
    public float lightAngle = 45f;      // 빛의 각도 (Spot Light 각도의 절반 정도)
    public LayerMask enemyLayer;        // 적만 골라낼 레이어

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
        UpdateBatteryUI(); // 시작하자마자 UI 갱신
    }

    private void Update()
    {
        HandleSway();
        HandleBattery();
        DetectEnemies();
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

        // 매 프레임 UI 업데이트 (이미지 교체)
        UpdateBatteryUI();
    }

    private void UpdateBatteryUI()
    {
        // 연결 안 되어 있으면 무시 (에러 방지)
        if (batteryUI == null || batterySprites == null || batterySprites.Length == 0) return;

        // 현재 배터리 비율 계산 (0.0 ~ 1.0)
        float ratio = currentBattery / maxBattery;
        int index = 0;

        // 비율에 따라 보여줄 이미지 번호 결정 (6단계)
        if (ratio > 0.8f) index = 0; // 100% ~ 81%
        else if (ratio > 0.6f) index = 1; // 80% ~ 61%
        else if (ratio > 0.4f) index = 2; // 60% ~ 41%
        else if (ratio > 0.2f) index = 3; // 40% ~ 21%
        else if (ratio > 0.0f) index = 4; // 20% ~ 1%
        else index = 5; // 0% (완전 방전)

        // 인덱스가 배열 범위를 넘지 않게 안전장치
        index = Mathf.Clamp(index, 0, batterySprites.Length - 1);

        // 이미지 교체!
        batteryUI.sprite = batterySprites[index];
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

    // 아이템 먹었을 때 호출할 함수
    public void RestoreBattery()
    {
        currentBattery = maxBattery;

        // 소리 재생
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);

        // 배터리 찼으니까 즉시 UI 업데이트
        UpdateBatteryUI();
    }

    private void DetectEnemies()
    {
        // 1. 배터리가 없거나 꺼져있으면 감지 안 함
        if (!isFlashLightOn || currentBattery <= 0) return;

        // 2. 내 주변 lightRange 반경 내의 모든 적(Collider)을 찾음
        Collider[] hits = Physics.OverlapSphere(transform.position, lightRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            Transform target = hit.transform;

            // 3. 적이 내 앞쪽(각도)에 있는지 계산
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // 내 정면(forward)과 적 방향 사이의 각도
            if (Vector3.Angle(transform.forward, directionToTarget) < lightAngle)
            {
                // 4. 벽에 가려졌는지 확인 (Raycast)
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                // 나~적 사이에 장애물이 없다면? (Enemy 레이어만 맞아야 함, 벽에 막히면 안 됨)
                // *중요: 벽 레이어도 포함해서 검사해야 벽 뒤의 적을 못 봄!
                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, ~enemyLayer)) // ~enemyLayer는 적 빼고 다 검사한다는 뜻이지만, 여기선 벽(Default) 체크용
                {
                    // 5. 조건 통과! 적에게 "빛 받음" 알리기
                    GhostEnemy ghost = target.GetComponent<GhostEnemy>();
                    if (ghost != null)
                    {
                        ghost.OnLightHit();
                    }
                }
                // 벽 체크가 복잡하면 일단 위 if문 빼고 ghost.OnLightHit()만 호출해도 됨!
            }
        }
    }

}