using UnityEngine;

public class AxisBeam : MonoBehaviour
{
    [Header("큐브 설정")]
    public float cubeSize = 10f;
    public float spawnMultiplier = 1.3f;  // 큐브 크기 대비 분포 범위

    [Header("축 색상")]
    public Color colorX = new Color(0.9f, 0.35f, 0.35f);
    public Color colorY = new Color(0.25f, 0.85f, 0.55f);
    public Color colorZ = new Color(0.85f, 0.65f, 0.15f);

    [Header("빔 설정")]
    public int beamsPerAxis = 20;
    public float beamLength = 60f;      // 큐브보다 충분히 길게
    public float beamWidth = 0.15f;

    [Header("쉐이더 파라미터")]
    public Shader beamShader;
    [Range(0, 1)]  public float opacity = 0.5f;
    [Range(0, 5)]  public float speed = 1.0f;
    [Range(1, 10)] public float pulseCount = 3f;
    [Range(1, 16)] public float pulseSharpness = 4f;
    [Range(0.1f, 2f)] public float beamSoftness = 0.8f;
    [Range(0.1f, 5f)] public float fadeDistance = 1.5f;
    [Range(0.1f, 40f)] public float cubeFadeMargin = 3.0f;

    void Start()
    {
        SpawnAxis(Vector3.right,   colorX);
        SpawnAxis(Vector3.up,      colorY);
        SpawnAxis(Vector3.forward, colorZ);
    }

    void SpawnAxis(Vector3 axis, Color color)
    {
        Material mat = new Material(beamShader);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Opacity", opacity);
        mat.SetFloat("_Speed", speed);
        mat.SetFloat("_PulseCount", pulseCount);
        mat.SetFloat("_PulseSharpness", pulseSharpness);
        mat.SetFloat("_BeamWidth", beamSoftness);
        mat.SetFloat("_FadeDistance", fadeDistance);
        mat.SetVector("_CubeCenter", transform.position);
        mat.SetFloat("_CubeSize", cubeSize);
        mat.SetFloat("_CubeFadeMargin", cubeFadeMargin);

        // 축에 수직인 두 방향
        Vector3 perp1 = Vector3.Cross(axis, Vector3.up).normalized;
        if (perp1.sqrMagnitude < 0.01f)
            perp1 = Vector3.Cross(axis, Vector3.forward).normalized;
        Vector3 perp2 = Vector3.Cross(axis, perp1).normalized;

        float spawnRange = cubeSize * spawnMultiplier * 0.5f;

        for (int i = 0; i < beamsPerAxis; i++)
        {
            // 큐브 범위 내외 랜덤 분포
            float u = Random.Range(-spawnRange, spawnRange);
            float v = Random.Range(-spawnRange, spawnRange);
            Vector3 offset = perp1 * u + perp2 * v;

            GameObject go = new GameObject($"Beam_{axis}_{i}");
            go.transform.SetParent(transform);

            // 빔 중심은 큐브 중심, 축 방향으로 정렬
            go.transform.position = transform.position + offset;
            go.transform.rotation = Quaternion.identity;
            
            BilboardBeam bb = go.AddComponent<BilboardBeam>();
            bb.axis = axis;

            MeshFilter   mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;

            mf.mesh = CreateBeamMesh(beamLength, beamWidth);
        }
    }

    Mesh CreateBeamMesh(float length, float width)
    {
        float halfLen = length * 0.5f;
        float halfWidth = width * 0.5f;

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-halfWidth, 0, -halfLen),
            new Vector3(halfWidth, 0, -halfLen),
            new Vector3(halfWidth, 0, halfLen),
            new Vector3(-halfWidth, 0, halfLen)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
