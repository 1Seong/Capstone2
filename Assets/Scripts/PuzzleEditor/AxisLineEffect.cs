using UnityEngine;

public class AxisLineEffect : MonoBehaviour
{
    [Header("축 색상")]
    public Color colorX = new Color(0.9f, 0.3f, 0.3f);
    public Color colorY = new Color(0.2f, 0.8f, 0.5f);
    public Color colorZ = new Color(0.8f, 0.6f, 0.1f);

    [Header("라인 설정")]
    public int countPerAxis = 120;
    public float lineLength = 80f;
    public float lineWidth = 0.04f;
    public float spawnRadius = 30f;  // 큐브 중심 기준 분포 반경

    [Header("쉐이더")]
    public Shader lineShader;
    public float opacity = 0.35f;
    public float speed = 0.25f;
    public float fadeDistance = 1.5f;

    void Start()
    {
        SpawnAxis(Vector3.right,   colorX);
        SpawnAxis(Vector3.up,      colorY);
        SpawnAxis(Vector3.forward, colorZ);
    }

    void SpawnAxis(Vector3 dir, Color color)
    {
        // 축에 수직인 두 벡터
        Vector3 perp1 = Vector3.Cross(dir, Vector3.up);
        if (perp1.sqrMagnitude < 0.01f)
            perp1 = Vector3.Cross(dir, Vector3.right);
        perp1.Normalize();
        Vector3 perp2 = Vector3.Cross(dir, perp1).normalized;

        Material mat = new Material(lineShader);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Opacity", opacity);
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_FadeDistance", fadeDistance);
        mat.enableInstancing = true;

        for (int i = 0; i < countPerAxis; i++)
        {
            // 축에 수직 평면에서 랜덤 위치
            float r = Random.Range(0f, spawnRadius);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = (perp1 * Mathf.Cos(angle) + perp2 * Mathf.Sin(angle)) * r;
            Vector3 center = transform.position + offset;

            // 라인 쿼드 생성
            GameObject go = new GameObject($"Line_{dir}_{i}");
            go.transform.SetParent(transform);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            mf.mesh = CreateLineMesh(center, dir, lineLength, lineWidth);
        }
    }

    Mesh CreateLineMesh(Vector3 center, Vector3 dir, float length, float width)
    {
        // 카메라를 향하는 빌보드 대신 고정 쿼드 (축 방향 + up)
        Vector3 up = Vector3.Cross(dir, Vector3.right).normalized;
        if (up.sqrMagnitude < 0.01f)
            up = Vector3.Cross(dir, Vector3.forward).normalized;

        Vector3 half = dir * (length * 0.5f);
        Vector3 side = up * (width * 0.5f);

        Vector3 p0 = center - half - side;
        Vector3 p1 = center - half + side;
        Vector3 p2 = center + half + side;
        Vector3 p3 = center + half - side;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0, p1, p2, p3 };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
