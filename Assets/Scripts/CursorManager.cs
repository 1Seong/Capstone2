using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [SerializeField] private RectTransform _cursorTransform;
    [SerializeField] private Image _cursorImage;

    [Header("무지개 설정")]
    [SerializeField] private float _speed = 0.4f;
    [SerializeField] private float _saturation = 0.45f;
    [SerializeField] private float _brightness = 0.95f;
    [SerializeField] private bool _rainbowEnabled = true;

    private void Awake()
    {
        Instance = this;

        Cursor.visible = false;
    }

    private void Update()
    {
        // 무지개 색상만 Update에서
        if (_rainbowEnabled)
        {
            float hue = Mathf.Repeat(Time.time * _speed, 1f);
            _cursorImage.color = Color.HSVToRGB(hue, _saturation, _brightness);
        }
    }

    private void LateUpdate()
    {
        // 위치는 LateUpdate에서
        _cursorTransform.position = Input.mousePosition;
    }

    /// <summary>커서 이미지 교체</summary>
    public void SetCursor(Sprite sprite)
    {
        _cursorImage.sprite = sprite;
    }

    /// <summary>무지개 효과 켜기/끄기 (끄면 흰색으로 복구)</summary>
    public void SetRainbow(bool enabled)
    {
        _rainbowEnabled = enabled;
        if (!enabled)
            _cursorImage.color = Color.white;
    }

    private void OnDestroy()
    {
        if (this != Instance) return;
        Cursor.visible = true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.visible = !hasFocus; // 포커스 잃으면 시스템 커서 복구
    }
}
