using UnityEngine;
using System.Collections; // 코루틴 쓰려면 필수!
using TMPro;

public class EscapeZone : MonoBehaviour
{
    [Header("--- Settings ---")]
    public int requiredKeys = 3; // 필요한 열쇠 개수
    public float endDelay = 3.0f; // 엔딩 후 게임 꺼질 때까지 대기 시간

    [Header("--- UI ---")]
    public GameObject winUIPanel; // 클리어 화면
    public TMP_Text messageText;  // 안내 메시지

    private bool isEnding = false; // 중복 실행 방지용

    private void OnTriggerEnter(Collider other)
    {
        if (isEnding) return; // 이미 엔딩 진행 중이면 무시

        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                if (player.keyCount >= requiredKeys)
                {
                    StartCoroutine(ProcessEnding()); // 엔딩 시작!
                }
                else
                {
                    ShowMessage($"열쇠가 부족해! ({player.keyCount}/{requiredKeys})");
                }
            }
        }
    }

    // 엔딩 연출을 처리하는 코루틴
    IEnumerator ProcessEnding()
    {
        isEnding = true;
        Debug.Log("탈출 성공! 엔딩 시퀀스 시작...");

        // 1. 맵에 있는 모든 적(GhostEnemy) 찾아서 비활성화 (사라지게 하기)
        GhostEnemy[] enemies = FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None);
        foreach (GhostEnemy ghost in enemies)
        {
            ghost.gameObject.SetActive(false);
        }

        // 2. 클리어 UI 띄우기
        if (winUIPanel != null) winUIPanel.SetActive(true);
        if (messageText != null) messageText.gameObject.SetActive(false); // 기존 메시지는 끄기

        // 3. 몇 초 기다리기 (여운)
        yield return new WaitForSeconds(endDelay);

        // 4. 게임 종료!
        Debug.Log("게임 종료 (Application.Quit)");
        Application.Quit();

#if UNITY_EDITOR
        // 에디터에서는 게임이 안 꺼지니까, 강제로 플레이 모드 끄기 (테스트용)
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(true);
            CancelInvoke("HideMessage"); // 기존에 예약된 끄기 취소
            Invoke("HideMessage", 2f);
        }
    }

    void HideMessage()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
    }
}