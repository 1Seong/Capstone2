using DG.Tweening;
using UnityEngine;

public class GhostEffectController : MonoBehaviour
{
    public static GhostEffectController Instance { get; private set; }

    [SerializeField] private Material ghostMaterial;
    [SerializeField] private float fadeDuration = 0.5f;

    private static readonly int PropIntensity    = Shader.PropertyToID("_Intensity");
    private static readonly int PropSaturation   = Shader.PropertyToID("_Saturation");
    private static readonly int PropAspectRatio  = Shader.PropertyToID("_AspectRatio");

    private Tween _tween;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 화면 비율 초기 설정
        UpdateAspectRatio();
        ghostMaterial.SetFloat(PropIntensity, 0f);
    }

    private void UpdateAspectRatio()
    {
        float aspect = (float)Screen.width / Screen.height;
        ghostMaterial.SetFloat(PropAspectRatio, aspect);
    }

    /// <summary>유령 상태 진입</summary>
    public void Enter()
    {
        UpdateAspectRatio();
        _tween?.Kill();
        _tween = DOTween.To(
            () => ghostMaterial.GetFloat(PropIntensity),
            v  => ghostMaterial.SetFloat(PropIntensity, v),
            1f, fadeDuration
        ).SetEase(Ease.OutCubic);
    }

    /// <summary>유령 상태 해제</summary>
    public void Exit()
    {
        _tween?.Kill();
        _tween = DOTween.To(
            () => ghostMaterial.GetFloat(PropIntensity),
            v  => ghostMaterial.SetFloat(PropIntensity, v),
            0f, fadeDuration
        ).SetEase(Ease.InCubic);
    }

    private void OnDestroy()
    {
        if (this != Instance) return;
        _tween?.Kill();
        ghostMaterial.SetFloat(PropIntensity, 0f);
    }
}
