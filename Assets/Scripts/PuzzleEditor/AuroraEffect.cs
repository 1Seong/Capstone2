using UnityEngine;

public class AuroraEffect : MonoBehaviour
{
    [Header("축 색상")]
    public Color colorX = new Color(0.9f, 0.4f, 0.4f);
    public Color colorY = new Color(0.3f, 0.85f, 0.6f);
    public Color colorZ = new Color(0.85f, 0.65f, 0.2f);

    [Header("오로라 설정")]
    public int layersPerAxis = 6;       // 축마다 쿼드 장수
    public float quadSize = 80f;         // 쿼드 크기
    public float spreadRange = 40f;      // 축 수직 방향 분포 범위

    [Header("쉐이더 파라미터")]
    public Shader auroraShader;
    public float opacity = 0.28f;
    public float speed = 0.18f;
    public float noiseScale = 1.5f;
    public float curtainSharpness = 3.0f;
    public float fadeDistance = 2.0f;

    void Start()
    {
        SpawnAurora(Vector3.right,   colorX);
        SpawnAurora(Vector3.up,      colorY);
        SpawnAurora(Vector3.forward, colorZ);
    }

    void SpawnAurora(Vector3 axis, Color color)
    {
        Material mat = new Material(auroraShader);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Opacity", opacity);
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_NoiseScale", noiseScale);
        mat.SetFloat("_CurtainSharpness", curtainSharpness);
        mat.SetFloat("_FadeDistance", fadeDistance);

        // 축에 수직인 두 방향
        Vector3 perp1 = Vector3.Cross(axis, Vector3.up).normalized;
        if (perp1.sqrMagnitude < 0.01f)
            perp1 = Vector3.Cross(axis, Vector3.forward).normalized;
        Vector3 perp2 = Vector3.Cross(axis, perp1).normalized;

        for (int i = 0; i < layersPerAxis; i++)
        {
            // 축 수직 평면에서 랜덤 오프셋
            float r = Random.Range(-spreadRange, spreadRange);
            Vector3 offset = perp1 * r + perp2 * Random.Range(-spreadRange, spreadRange);

            GameObject go = new GameObject($"Aurora_{axis}_{i}");
            go.transform.SetParent(transform);
            go.transform.position = transform.position + offset;

            // 쿼드를 축 방향으로 정렬
            go.transform.rotation = Quaternion.LookRotation(perp1, axis);

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            mf.mesh = CreateQuad(quadSize);
        }
    }

    Mesh CreateQuad(float size)
    {
        float h = size * 0.5f;
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-h, -h, 0),
            new Vector3(-h,  h, 0),
            new Vector3( h,  h, 0),
            new Vector3( h, -h, 0)
        };
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
