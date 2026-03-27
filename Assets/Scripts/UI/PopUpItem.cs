using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PopUpItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _rectTransform;

    private const float FadeInDuration = 0.25f;
    private const float FadeOutDuration = 0.35f;
    private const float SlideInDistance = 30f;

    public async UniTask ShowAsync(string message, float displayDuration)
    {
        _messageText.text = message;

        // 초기 상태
        _canvasGroup.alpha = 0f;
        var startY = _rectTransform.anchoredPosition.y - SlideInDistance;
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, startY);

        // 슬라이드 인 + 페이드 인
        var targetY = _rectTransform.anchoredPosition.y + SlideInDistance;
        _rectTransform.DOAnchorPosY(targetY, FadeInDuration).SetEase(Ease.OutCubic);
        await _canvasGroup.DOFade(1f, FadeInDuration).SetEase(Ease.OutCubic)
            .AsyncWaitForCompletion().AsUniTask();

        await UniTask.Delay(TimeSpan.FromSeconds(displayDuration));

        // 페이드 아웃
        await _canvasGroup.DOFade(0f, FadeOutDuration).SetEase(Ease.InCubic)
            .AsyncWaitForCompletion().AsUniTask();
    }
}
