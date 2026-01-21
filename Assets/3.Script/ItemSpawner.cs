using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("--- Spawn Settings ---")]
    [Tooltip("아이템이 생성될 수 있는 모든 위치들 (빈 게임 오브젝트들)")]
    public List<Transform> allSpawnPoints;

    [Header("--- Item Prefabs ---")]
    [Tooltip("탈출 아이템 프리팹")]
    public GameObject escapeItemPrefab;
    [Tooltip("탈출 아이템 생성 개수 (이만큼 먼저 배치하고 나머지는 배터리가 됨)")]
    public int escapeItemCount = 3;

    [Tooltip("배터리 아이템 프리팹")]
    public GameObject batteryItemPrefab;

    void Start()
    {
        SpawnAllItems();
    }

    void SpawnAllItems()
    {
        if (allSpawnPoints == null || allSpawnPoints.Count == 0)
        {
            Debug.LogWarning("스폰 포인트가 하나도 없습니다! 리스트를 채워주세요.");
            return;
        }

        // 1. 스폰 포인트 리스트 복사 (원본 보존 & 중복 방지용)
        List<Transform> availablePoints = new List<Transform>(allSpawnPoints);

        // 2. 탈출 아이템 먼저 랜덤 생성 (VIP 대우)
        int spawnCount = Mathf.Min(escapeItemCount, availablePoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            // 랜덤 뽑기
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[randomIndex];

            if (escapeItemPrefab != null)
            {
                GameObject item = Instantiate(escapeItemPrefab, spawnPoint.position, spawnPoint.rotation);
                item.name = $"EscapeItem_{i + 1}";
            }

            // 이 자리는 썼으니까 목록에서 삭제!
            availablePoints.RemoveAt(randomIndex);
        }

        Debug.Log($"탈출 아이템 {spawnCount}개 배치 완료!");

        // 3. 남은 자리(availablePoints)에 전부 배터리 깔기 (떨이 처리)
        int batteryCount = 0;
        foreach (Transform point in availablePoints)
        {
            if (batteryItemPrefab != null)
            {
                GameObject item = Instantiate(batteryItemPrefab, point.position, point.rotation);
                item.name = $"BatteryItem_{batteryCount + 1}";
                batteryCount++;
            }
        }

        Debug.Log($"남은 자리에 배터리 {batteryCount}개 배치 완료!");
    }
}