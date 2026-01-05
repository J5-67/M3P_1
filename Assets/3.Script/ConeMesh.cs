using UnityEngine;

[ExecuteAlways] // 에디터에서도 Update가 돌아가게 함 (가장 안전!)
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ConeMesh : MonoBehaviour
{
    public float radius = 2f;   // 빛의 넓이
    public float length = 20f;   // 빛의 길이
    public int segments = 32;     // 둥근 정도

    private Mesh mesh;
    private bool needsUpdate = false; // [핵심] 변경사항 체크용 깃발 

    void OnEnable()
    {
        needsUpdate = true;
    }

    // [핵심 수정] 여기서 직접 메쉬를 건드리지 않고, "업데이트 필요함!" 표시만 남김
    void OnValidate()
    {
        needsUpdate = true;
    }

    // [핵심 수정] 가장 안전한 타이밍인 Update에서 메쉬를 변경함
    void Update()
    {
        if (needsUpdate)
        {
            CreateCone();
            needsUpdate = false; // 업데이트 완료했으니 깃발 내림
        }
    }

    void CreateCone()
    {
        // 혹시라도 오브젝트가 파괴되는 중이면 중단
        if (this == null || gameObject == null) return;

        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "ProceduralCone";
        }
        else
        {
            mesh.Clear();
        }

        // --- 여기서부터 메쉬 계산 (이전과 동일) ---
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        vertices[segments + 1] = new Vector3(0, 0, length);

        float angleStep = 360.0f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, length);

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i == segments - 1) ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        mf.mesh = mesh;
    }
}