using UnityEngine;

public class CellPulse : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.3f, 1f);
    [SerializeField] private float pulseIntensity = 1.5f;  // Bloom용 HDR 배수
    [SerializeField] private float pulseDuration = 1f;

    private Material _pulseMaterial;
    private Color _originalColor;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    // 외부에서 호출 — 딜레이로 물결 효과
    public void StartPulse(float delay = 0f)
    {
        var activeRenderer = GetActiveChildRenderer();
        _pulseMaterial = activeRenderer.material;
        _pulseMaterial.EnableKeyword("_EMISSION");
        _originalColor = _pulseMaterial.GetColor(EmissionColor);

        // delay를 위상 오프셋으로 — Update에서 시간 기반으로 직접 계산
        _phaseOffset = delay;
        _isPulsing = true;
    }

    float _phaseOffset;
    bool _isPulsing;

    void Update()
    {
        if (!_isPulsing) return;

        // 현재 위상 (0~1), 셀마다 phaseOffset만큼 어긋남
        float phase = Mathf.PingPong((Time.time - _phaseOffset) / pulseDuration, 1f);
        Color current = Color.Lerp(_originalColor, highlightColor * pulseIntensity, phase);
        _pulseMaterial.SetColor(EmissionColor, current);
    }

    public void StopPulse()
    {
        _isPulsing = false;
        _pulseMaterial.SetColor(EmissionColor, _originalColor);
        _pulseMaterial.DisableKeyword("_EMISSION");
    }

    private MeshRenderer GetActiveChildRenderer()
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
                return child.GetComponent<MeshRenderer>();
        }
        return null;
    }
}
