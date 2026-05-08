using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GalaxyA 셰이더를 절반 해상도 RenderTexture에 Blit한 뒤
/// RawImage에 업스케일해서 표시. 풀 해상도 fragment 실행을 1/4로 줄임.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class GalaxyBackground : MonoBehaviour
{
    [SerializeField] private Material galaxyMaterial;

    [Range(0.1f, 1.0f)]
    [SerializeField] private float resolutionScale = 0.5f;

    private RenderTexture _rt;
    [SerializeField] private RawImage      _rawImage;
    private int           _cachedWidth;
    private int           _cachedHeight;

    private void Awake()
    {
        if(_rawImage == null)
            _rawImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        CreateRT();
    }

    private void OnDisable()
    {
        ReleaseRT();
    }

    private void Update()
    {
        // 해상도 변경 감지 (에디터 리사이즈, 런타임 해상도 변경 대응)
        int w = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

        if (w != _cachedWidth || h != _cachedHeight)
            CreateRT();

        if (_rt != null && galaxyMaterial != null)
            Graphics.Blit(null, _rt, galaxyMaterial);
    }

    private void CreateRT()
    {
        ReleaseRT();

        _cachedWidth  = Mathf.Max(1, Mathf.RoundToInt(Screen.width  * resolutionScale));
        _cachedHeight = Mathf.Max(1, Mathf.RoundToInt(Screen.height * resolutionScale));

        _rt = new RenderTexture(_cachedWidth, _cachedHeight, 0, RenderTextureFormat.Default)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };
        _rt.Create();

        _rawImage.texture = _rt;
    }

    private void ReleaseRT()
    {
        if (_rt == null) return;
        
        // active RenderTexture가 _rt이면 해제
        if (RenderTexture.active == _rt)
            RenderTexture.active = null;
        _rawImage.texture = null;
        _rt.Release();
        Destroy(_rt);
        _rt = null;
    }

    private void OnDestroy()
    {
        ReleaseRT();
    }
}
