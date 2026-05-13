using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RippleEffectController : MonoBehaviour
{
    public static RippleEffectController Instance { get; private set; }

    [SerializeField] private Material rippleMaterial;
    [SerializeField] private float    duration     = 0.8f;
    [SerializeField] private bool     distortionOn = true;

    private static readonly int ID_Progress     = Shader.PropertyToID("_RippleProgress");
    private static readonly int ID_OriginUV     = Shader.PropertyToID("_OriginUV");
    private static readonly int ID_DistortionOn = Shader.PropertyToID("_DistortionOn");
    private static readonly int ID_AspectRatio  = Shader.PropertyToID("_AspectRatio");

    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rippleMaterial.SetFloat(ID_Progress,     0f);
        rippleMaterial.SetFloat(ID_DistortionOn, distortionOn ? 1f : 0f);
    }

    /// <summary>월드 포지션에서 링이 퍼져나가는 효과 실행</summary>
    public void Play(Vector3 worldPosition)
    {
        // 월드 → 스크린 → UV 변환
        Vector3 screenPos = Camera.main.WorldToViewportPoint(worldPosition);
        Vector2 originUV  = new Vector2(screenPos.x, screenPos.y);

        PlayAsync(originUV).Forget();
    }

    private async UniTaskVoid PlayAsync(Vector2 originUV)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        float aspectRatio = (float)Screen.width / Screen.height;
        rippleMaterial.SetVector(ID_OriginUV,    new Vector4(originUV.x, originUV.y, 0, 0));
        rippleMaterial.SetFloat(ID_AspectRatio,  aspectRatio);
        rippleMaterial.SetFloat(ID_DistortionOn, distortionOn ? 1f : 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rippleMaterial.SetFloat(ID_Progress, progress);

            await UniTask.Yield(_cts.Token);
        }

        rippleMaterial.SetFloat(ID_Progress, 0f);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
