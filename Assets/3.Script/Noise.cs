using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Noise : MonoBehaviour
{
    [Header("--- Targets ---")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemy;

    [Header("--- Settings ---")]
    [Tooltip("노이즈가 시작될 최대 거리")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("노이즈가 최대가 될 최소 거리")]
    [SerializeField] private float minDistance = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float maxIntensity = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 1.0f;

    [Header("--- UI ---")]
    [SerializeField] private Image noiseImage;

    private Material noiseMat;
    private AudioSource noiseAudio;

    void Start()
    {
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
        if (player == null || enemy == null || noiseMat == null) return;

        float distance = Vector3.Distance(player.position, enemy.position);

        float distanceFactor = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);
        float finalIntensity = distanceFactor * maxIntensity;

        noiseMat.SetFloat("_NoiseIntensity", finalIntensity);

        if (noiseAudio != null)
        {
            noiseAudio.volume = distanceFactor * maxVolume;
        }
    }
}