using UnityEngine;

/// <summary>
/// 카메라를 항상 바라보는 빌보드 Quad.
/// 기믹 큐브의 자식으로 배치.
/// </summary>
public class BillboardQuad : MonoBehaviour
{
    private Transform _camTransform;

    private void Awake()
    {
        _camTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        transform.rotation = _camTransform.rotation;
    }
}
