using System;
using UnityEngine;

public class EditorCameraController : MonoBehaviour
{
    [Header("Rotation")] 
    [SerializeField] private bool useRotate = true;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minSize = 2f;
    [SerializeField] private float maxSize = 20f;

    [SerializeField] private Transform pivot; // 큐브(맵) 중심
    private float _currentDistance;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if(useRotate)
            HandleRotation();
        HandleZoom();
    }

    /// <summary>
    /// 마우스 우클릭 드래그로 피벗(큐브 중심) 기준 카메라 회전
    /// </summary>
    private void HandleRotation()
    {
        if (!Input.GetMouseButton(1)) return;

        pivot.Rotate(Vector3.up,    Input.GetAxis("Mouse X") * rotationSpeed, Space.World);
        pivot.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * rotationSpeed, Space.Self);
    }

    /// <summary>
    /// 마우스 휠로 카메라 줌 인/아웃
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        _camera.orthographicSize = Mathf.Clamp(
            _camera.orthographicSize - scroll * zoomSpeed,
            minSize,
            maxSize
        );
    }
}
