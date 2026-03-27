using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PopUpManager : MonoBehaviour
{
    public static PopUpManager Instance { get; private set; }

    [SerializeField] private PopUpItem _popupPrefab;

    // 팝업이 쌓일 부모 Transform (Canvas 하위의 상단 앵커 오브젝트)
    [SerializeField] private Transform _popupContainer;

    [SerializeField] private float _displayDuration = 2.5f;

    // 현재 표시 중인 팝업 목록 (위→아래 순서로 관리)
    private readonly List<PopUpItem> _activePopups = new();

    private const float ItemHeight = 60f;   // 팝업 하나의 높이
    private const float ItemSpacing = 8f;   // 팝업 사이 간격
    private const float ShiftDuration = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 팝업 메시지를 표시합니다.
    /// </summary>
    public void Show(string message)
    {
        _ = ShowAsync(message);
    }

    private async UniTask ShowAsync(string message)
    {
        // 기존 팝업들을 아래로 밀어냄
        ShiftExistingPopups();

        var popup = Instantiate(_popupPrefab, _popupContainer);
        var rt = popup.GetComponent<RectTransform>();

        // 새 팝업은 항상 맨 위(y=0 기준)에서 시작
        rt.anchoredPosition = new Vector2(0f, 0f);

        _activePopups.Insert(0, popup);

        await popup.ShowAsync(message, _displayDuration);

        _activePopups.Remove(popup);
        Destroy(popup.gameObject);
    }

    private void ShiftExistingPopups()
    {
        float shift = ItemHeight + ItemSpacing;
        foreach (var popup in _activePopups)
        {
            var rt = popup.GetComponent<RectTransform>();
            rt.DOAnchorPosY(rt.anchoredPosition.y - shift, ShiftDuration)
                .SetEase(Ease.OutCubic);
        }
    }
}
