using TMPro;
using UnityEngine;

/// <summary>
/// TMP 버텍스 컬러를 매 프레임 조작해 무지개 효과 구현.
/// 셰이더 불필요 — 외부 폰트 에셋 완전 호환.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class RainbowText : MonoBehaviour
{
    [Header("무지개 설정")]
    [SerializeField] private float speed      = 0.4f;
    [SerializeField] private float scale      = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float saturation = 0.45f;
    [Range(0f, 1f)]
    [SerializeField] private float brightness = 0.95f;
    [Range(0f, 1f)]
    [SerializeField] private float opacity    = 1.0f;

    private TMP_Text _tmp;
    private bool     _isUpdating; // 재귀 진입 방지 플래그

    private void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        ApplyColors();
    }

private void ApplyColors()
    {
        if (_isUpdating) return;
        _isUpdating = true;

        _tmp.ForceMeshUpdate(true, false);

        TMP_TextInfo textInfo = _tmp.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0)
        {
            _isUpdating = false;
            return;
        }

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex  = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            float hue   = Mathf.Repeat((float)i * scale - Time.time * speed, 1f);
            Color color = HsvToRgb(hue, saturation, brightness);
            color.a     = opacity;

            Color32 c = color;
            Color32[] colors = textInfo.meshInfo[matIndex].colors32;
            colors[vertIndex + 0] = c;
            colors[vertIndex + 1] = c;
            colors[vertIndex + 2] = c;
            colors[vertIndex + 3] = c;
        }

        for (int i = 0; i < textInfo.materialCount; i++)
        {
            if (textInfo.meshInfo[i].mesh == null) continue;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            _tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        _isUpdating = false;
    }

    private static Color HsvToRgb(float h, float s, float v)
    {
        if (s <= 0f) return new Color(v, v, v);

        float hh     = Mathf.Repeat(h, 1f) * 6f;
        int   region = (int)hh;
        float frac   = hh - region;
        float p = v * (1f - s);
        float q = v * (1f - s * frac);
        float t = v * (1f - s * (1f - frac));

        return region switch
        {
            0 => new Color(v, t, p),
            1 => new Color(q, v, p),
            2 => new Color(p, v, t),
            3 => new Color(p, q, v),
            4 => new Color(t, p, v),
            _ => new Color(v, p, q),
        };
    }
}
