using UnityEngine;
using UnityEngine.UI; // UI를 다루기 위해 필요

public class Noise : MonoBehaviour
{
    [Header("--- Targets ---")]
    [SerializeField] private Transform player; // 플레이어
    [SerializeField] private Transform enemy;  // 적

    [Header("--- Settings ---")]
    [Tooltip("노이즈가 시작될 최대 거리 (이보다 멀면 노이즈 없음)")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("노이즈가 최대가 될 최소 거리 (이보다 가까우면 노이즈 최대)")]
    [SerializeField] private float minDistance = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float maxIntensity = 0.8f; // 노이즈가 가장 심할 때의 투명도 (1이면 완전 안 보임)

    [Header("--- UI ---")]
    [SerializeField] private Image noiseImage; // 노이즈 재질이 적용된 UI 이미지

    private Material noiseMat; // 실제 조절할 재질 인스턴스

    void Start()
    {
        // 이미지에 적용된 재질(Material)의 복사본을 가져와서 조절 준비
        if (noiseImage != null)
        {
            // .material을 쓰면 원본을 건드리지 않고 이 게임 오브젝트만의 인스턴스를 만듦
            noiseMat = noiseImage.material;
            noiseMat.SetFloat("_NoiseIntensity", 0f); // 시작할 땐 노이즈 끄기
        }
    }

    void Update()
    {
        // 필요한 요소들이 없으면 실행 안 함
        if (player == null || enemy == null || noiseMat == null) return;

        // 1. 거리 계산
        float distance = Vector3.Distance(player.position, enemy.position);

        // 2. 거리를 0~1 사이 값으로 변환 (가까울수록 1, 멀수록 0)
        // Mathf.InverseLerp(min, max, value)는 value가 min에 가까우면 0, max에 가까우면 1을 반환함.
        // 우리는 반대로 가까울수록 커야 하니까 1에서 뺐음.
        float distanceFactor = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);

        // 3. 최종 강도 계산 (최대 강도 적용)
        float finalIntensity = distanceFactor * maxIntensity;

        // 4. 쉐이더에 값 전달 (우리가 쉐이더 그래프에서 만든 변수 이름!)
        noiseMat.SetFloat("_NoiseIntensity", finalIntensity);
    }
}