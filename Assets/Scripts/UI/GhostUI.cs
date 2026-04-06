using System;
using UnityEngine;

public class GhostUI : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform player;
    [SerializeField] private float offset;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        // 플레이어 월드 좌표 → 화면 좌표 → UI 위치
        Vector3 screenPos = playerCamera.WorldToScreenPoint(player.position + Vector3.up * offset);
        
        // 스크린 좌표 → Canvas 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,  // 부모 RectTransform
            screenPos,
            playerCamera,  // Screen Space - Camera면 카메라 필요, Overlay면 null
            out Vector2 localPos
        );

        _rectTransform.anchoredPosition = localPos;
    }
}
