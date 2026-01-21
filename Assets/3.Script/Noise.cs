using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Noise : MonoBehaviour
{
    [Header("--- Targets ---")]
    [SerializeField] private Transform player;
    // [수정] enemy 변수 삭제! 이제 태그로 찾음
    [Tooltip("적들이 사용 중인 태그 이름 (정확히 적어야 함)")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("--- Settings ---")]
    [Tooltip("노이즈가 시작될 최대 거리")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("노이즈가 최대가 될 최소 거리")]
    [SerializeField] private float minDistance = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float maxIntensity = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.5f;

    [Header("--- UI ---")]
    [SerializeField] private Image noiseImage;

    private Material noiseMat;
    private AudioSource noiseAudio;

    void Start()
    {
        if (player == null)
        {
            // 플레이어 안 넣었으면 자동으로 찾기 (편의성)
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (noiseImage != null)
        {
            noiseMat = noiseImage.material;
            noiseMat.SetFloat("_NoiseIntensity", 0f);
        }

        if (!TryGetComponent(out noiseAudio))
        {
            Debug.LogError("Noise 오브젝트에 AudioSource가 없습니다!");
        }
        else
        {
            noiseAudio.loop = true;
            noiseAudio.volume = 0f;
            if (!noiseAudio.isPlaying) noiseAudio.Play();
        }
    }

    void Update()
    {
        if (player == null || noiseMat == null) return;

        // 1. 가장 가까운 적 찾기
        float closestDistance = maxDistance; // 일단 최대 거리로 설정 (노이즈 없음)
        bool enemyFound = false;

        // "Enemy" 태그를 가진 모든 오브젝트를 가져옴
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        foreach (GameObject enemyObj in enemies)
        {
            if (enemyObj == null) continue;

            // 플레이어와 적 사이의 거리 계산
            float dist = Vector3.Distance(player.position, enemyObj.transform.position);

            // 더 가까운 적이 나타나면 거리 갱신
            if (dist < closestDistance)
            {
                closestDistance = dist;
                enemyFound = true;
            }
        }

        // 2. 노이즈 강도 계산 (가장 가까운 적 기준)
        // 적이 감지 범위(maxDistance) 안으로 들어왔을 때만 계산
        if (enemyFound && closestDistance < maxDistance)
        {
            float distanceFactor = 1f - Mathf.InverseLerp(minDistance, maxDistance, closestDistance);
            float finalIntensity = distanceFactor * maxIntensity;

            noiseMat.SetFloat("_NoiseIntensity", finalIntensity);

            if (noiseAudio != null)
            {
                noiseAudio.volume = distanceFactor * maxVolume;
            }
        }
        else
        {
            // 주변에 적이 없거나 멀리 있으면 노이즈 끔
            noiseMat.SetFloat("_NoiseIntensity", 0f);
            if (noiseAudio != null) noiseAudio.volume = 0f;
        }
    }
}