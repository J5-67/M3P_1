using UnityEngine;

public class GhostEnemy : MonoBehaviour
{
    [Header("--- Settings ---")]
    public float moveSpeed = 2.0f;      // 이동 속도 (느리게!)
    public float rotationSpeed = 5.0f;  // 회전 속도
    public float respawnDist = 20f;     // 빛 맞으면 도망가는 거리
    public float disappearTime = 1.0f;  // 빛을 견디는 시간

    [Header("--- Debug ---")]
    public float currentLightTime = 0f;

    private Transform player;
    private bool isRunningAway = false;

    void Start()
    {
        // 태그로 플레이어 찾기 (없으면 에러 방지)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없어!");
        }
    }

    void Update()
    {
        if (player == null || isRunningAway) return;

        // 1. 플레이어 쪽 바라보기 (Y축 회전만! 고개는 안 숙임)
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // 높이 차이는 무시 (유령이 기울어지지 않게)

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 2. 플레이어 쪽으로 이동 (Vector3.MoveTowards 사용)
        // transform.position을 직접 건드리기 때문에 벽을 뚫고 지나옴 (유령 컨셉!)
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        // 빛 게이지 자연 회복
        if (currentLightTime > 0)
        {
            currentLightTime -= Time.deltaTime;
        }
    }

    // 손전등에 맞았을 때 호출됨
    public void OnLightHit()
    {
        currentLightTime += Time.deltaTime * 2f; // 게이지 차오름

        if (currentLightTime >= disappearTime)
        {
            Respawn();
        }
    }

    // 도망가는 로직 (NavMesh 없이 좌표 이동)
    void Respawn()
    {
        // 플레이어 뒤쪽 어딘가로 텔레포트
        // (플레이어가 보는 방향의 반대편 + 랜덤 약간 섞기)
        Vector3 randomPos = Random.insideUnitSphere * 5f; // 랜덤 오차
        Vector3 spawnPos = player.position - (player.forward * respawnDist) + randomPos;

        // 높이는 현재 높이 유지 (땅속에 박히지 않게)
        spawnPos.y = transform.position.y;

        transform.position = spawnPos;
        currentLightTime = 0f;

        Debug.Log("유령이 뒤로 도망갔어! ");
    }

    public void IncreaseSpeed(float amount)
    {
        moveSpeed += amount;
        Debug.Log($"유령이 화났다! 현재 속도: {moveSpeed}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player p = other.GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(20f);

                Respawn();
            }
        }
    }
}