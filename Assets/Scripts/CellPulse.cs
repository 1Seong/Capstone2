using UnityEngine;

public class CellPulse : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.3f, 1f);
    [SerializeField] private float pulseIntensity = 1.5f;
    [SerializeField] private float pulseDuration = 1f;

    private Material _pulseMaterial;
    private Color _originalEmissionColor;
    private float _originalEmissionStrength;

    static readonly int ID_EmissionColor    = Shader.PropertyToID("_EmissionColor");
    static readonly int ID_EmissionStrength = Shader.PropertyToID("_EmissionStrength");
    static readonly int ID_BaseColor = Shader.PropertyToID("_BaseColor");
    private Color _originalBaseColor;

    float _phaseOffset;
    bool  _isPulsing;

    public void StartPulse(float delay = 0f)
    {
        _pulseMaterial = GetComponent<MeshRenderer>().material;
        _originalEmissionColor    = _pulseMaterial.GetColor(ID_EmissionColor);
        _originalEmissionStrength = _pulseMaterial.GetFloat(ID_EmissionStrength);
        _originalBaseColor        = _pulseMaterial.GetColor(ID_BaseColor);
        _phaseOffset = delay;
        _isPulsing   = true;
    }

    void Update()
    {
        if (!_isPulsing) return;
        float phase = Mathf.PingPong((Time.time - _phaseOffset) / pulseDuration, 1f);
        _pulseMaterial.SetColor(ID_EmissionColor,    Color.Lerp(_originalEmissionColor, highlightColor, phase));
        _pulseMaterial.SetFloat(ID_EmissionStrength, Mathf.Lerp(_originalEmissionStrength, pulseIntensity, phase));
        _pulseMaterial.SetColor(ID_BaseColor,        Color.Lerp(_originalBaseColor, highlightColor, phase));
    }

    public void StopPulse()
    {
        _isPulsing = false;
        _pulseMaterial.SetColor(ID_EmissionColor,    _originalEmissionColor);
        _pulseMaterial.SetFloat(ID_EmissionStrength, _originalEmissionStrength);
        _pulseMaterial.SetColor(ID_BaseColor,        _originalBaseColor);
    }
}
