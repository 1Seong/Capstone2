using System;
using System.Threading;
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

    private CancellationTokenSource _cts;

    public async UniTask ShowAsync(string message, float displayDuration)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _messageText.text = message;
        _canvasGroup.alpha = 0f;
        var startY = _rectTransform.anchoredPosition.y - SlideInDistance;
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, startY);

        var targetY = _rectTransform.anchoredPosition.y + SlideInDistance;

        // DOTween에 token 직접 연결 → 취소 시 트윈도 자동 Kill
        _rectTransform.DOAnchorPosY(targetY, FadeInDuration)
            .SetEase(Ease.OutCubic)
            .WithCancellation(token);

        await _canvasGroup.DOFade(1f, FadeInDuration)
            .SetEase(Ease.OutCubic)
            .WithCancellation(token);

        await UniTask.Delay(TimeSpan.FromSeconds(displayDuration), cancellationToken: token);

        await _canvasGroup.DOFade(0f, FadeOutDuration)
            .SetEase(Ease.InCubic)
            .WithCancellation(token);
    }

    public void ForceClose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnDestroy()
    {
        ForceClose();
    }
}
