using UnityEngine;

/// <summary>
/// GimmickCube 셰이더를 MaterialPropertyBlock으로 제어.
/// 600~1000개 인스턴싱 환경에서 드로우콜 최소화.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class GimmickCubeEffect : MonoBehaviour
{
    public enum GimmickType { PortalIn, PortalOut, Ghost, Laser, Inverter }

    [SerializeField] private GimmickType gimmickType = GimmickType.PortalIn;
    [SerializeField] private Color       colorA      = new Color(0.2f, 0.8f, 1.0f, 1f);
    [SerializeField] private Color       colorB      = new Color(0.5f, 0.1f, 0.9f, 1f);
    [SerializeField] private float       speed       = 1.0f;
    [SerializeField] private float       intensity   = 1.0f;

    [Header("포탈 전용")]
    [SerializeField] private float swirlTightness = 4.0f;
    
    private Renderer             _renderer;
    private MaterialPropertyBlock _mpb;

    // 셰이더 프로퍼티 ID 캐싱 (string 룩업 방지)
    private static readonly int ID_GimmickType    = Shader.PropertyToID("_GimmickType");
    private static readonly int ID_ColorA         = Shader.PropertyToID("_ColorA");
    private static readonly int ID_ColorB         = Shader.PropertyToID("_ColorB");
    private static readonly int ID_Speed          = Shader.PropertyToID("_Speed");
    private static readonly int ID_Intensity      = Shader.PropertyToID("_Intensity");
    private static readonly int ID_SwirlTightness = Shader.PropertyToID("_SwirlTightness");
    private static readonly int ID_LaserAxis      = Shader.PropertyToID("_LaserAxis");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
        Apply();
    }

    /// <summary>파라미터 변경 후 수동으로 호출하거나, Inspector 변경 시 자동 적용.</summary>
    public void Apply()
    {
        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetFloat(ID_GimmickType,    (float)gimmickType);
        _mpb.SetColor(ID_ColorA,         colorA);
        _mpb.SetColor(ID_ColorB,         colorB);
        _mpb.SetFloat(ID_Speed,          speed);
        _mpb.SetFloat(ID_Intensity,      intensity);
        _mpb.SetFloat(ID_SwirlTightness, swirlTightness);
        //_mpb.SetFloat(ID_LaserAxis,      (float)laserAxis);

        _renderer.SetPropertyBlock(_mpb);
    }

    // 런타임에서 기믹 타입을 바꿀 때 사용
    public void SetGimmickType(GimmickType type)
    {
        gimmickType = type;
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_mpb == null)      _mpb      = new MaterialPropertyBlock();
        Apply();
    }
#endif
}
