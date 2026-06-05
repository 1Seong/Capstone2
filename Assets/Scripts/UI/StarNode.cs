using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class StarNode : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualStar;
    [SerializeField] private Camera targetCamera;

    [Header("Follow")]
    [SerializeField] private float followDistance = 0f;
    [SerializeField] private float followSmooth = 12f;

    [Header("Return")]
    [SerializeField] private float returnDuration = 0.45f;
    [SerializeField] private float overshootPower = 1.35f;
    
    [Header("Scale")]
    [SerializeField] private float attachScaleMultiplier = 1.2f;
    [SerializeField] private float scaleUpDuration = 0.18f;
    [SerializeField] private float scaleDownDuration = 0.25f;
    [SerializeField] private Ease scaleUpEase = Ease.OutBack;
    [SerializeField] private Ease scaleDownEase = Ease.OutBack;

    private Vector3 originLocalPosition;
    private Vector3 originLocalScale;
    private CancellationTokenSource followCts;
    private Tween returnTween;
    private Tween scaleTween;

    private bool isAttached;

    private void Awake()
    {
        originLocalPosition = visualStar.localPosition;
        originLocalScale = visualStar.localScale;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void Attach()
    {
        if (isAttached)
            return;

        isAttached = true;
        AudioManager.Instance.PlaySFX(AudioManager.SFXType.Click);

        returnTween?.Kill();
        scaleTween?.Kill();
        
        ScaleUp();

        followCts?.Cancel();
        followCts?.Dispose();

        followCts = new CancellationTokenSource();
        FollowCursorAsync(followCts.Token).Forget();
    }

    public void Detach()
    {
        if (!isAttached)
            return;

        isAttached = false;

        followCts?.Cancel();
        followCts?.Dispose();
        followCts = null;

        ReturnToOrigin();
        ScaleDown();
    }

    private async UniTaskVoid FollowCursorAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Vector3 mouseWorld = GetMouseWorldPosition();

            Vector3 dir = visualStar.position - mouseWorld;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.up;
            else
                dir.Normalize();

            Vector3 targetWorldPosition = mouseWorld + dir * followDistance;
            targetWorldPosition.z = visualStar.position.z;

            visualStar.position = Vector3.Lerp(
                visualStar.position,
                targetWorldPosition,
                Time.deltaTime * followSmooth
            );

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private void ReturnToOrigin()
    {
        returnTween?.Kill();

        returnTween = visualStar
            .DOLocalMove(originLocalPosition, returnDuration)
            .SetEase(Ease.OutBack, overshootPower);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;

        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z - visualStar.position.z);
        mouse.z = distanceFromCamera;

        Vector3 world = targetCamera.ScreenToWorldPoint(mouse);
        world.z = visualStar.position.z;

        return world;
    }
    
    private void ScaleUp()
    {
        scaleTween?.Kill();

        Vector3 targetScale = originLocalScale * attachScaleMultiplier;

        scaleTween = visualStar
            .DOScale(targetScale, scaleUpDuration)
            .SetEase(scaleUpEase);
    }

    private void ScaleDown()
    {
        scaleTween?.Kill();

        scaleTween = visualStar
            .DOScale(originLocalScale, scaleDownDuration)
            .SetEase(scaleDownEase);
    }

    private void OnDestroy()
    {
        followCts?.Cancel();
        followCts?.Dispose();
        returnTween?.Kill();
    }

    public Transform VisualTransform => visualStar;
}
