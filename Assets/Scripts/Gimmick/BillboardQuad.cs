using UnityEngine;

public class BillboardQuad : MonoBehaviour
{
    public enum BillboardMode { Free, AxisLocked }
    public enum DashDirection { XPositive, XNegative, YPositive, YNegative, ZPositive, ZNegative }

    [SerializeField] private BillboardMode mode = BillboardMode.Free;

    [Header("AxisLocked 모드")]
    [SerializeField] private DashDirection dashDirection = DashDirection.XPositive;

    public BillboardMode Mode      { get => mode;          set => mode          = value; }
    public DashDirection Direction { get => dashDirection; set => dashDirection = value; }

    private Transform _cam;
    private Transform _parent;

    private void Awake()
    {
        _cam    = Camera.main.transform;
        _parent = transform.parent;
    }

    private void LateUpdate()
    {
        if (_parent == null) return;
        if (mode == BillboardMode.Free) Free();
        else                            AxisLocked();
    }

    private void Free()
    {
        transform.rotation = _cam.rotation;
    }

    private void AxisLocked()
    {
        // 매 프레임 dashDirection에서 직접 axis 계산 → 런타임 변경 즉시 반영
        Vector3 axis = dashDirection switch
        {
            DashDirection.XPositive => Vector3.right,
            DashDirection.XNegative => Vector3.left,
            DashDirection.YPositive => Vector3.up,
            DashDirection.YNegative => Vector3.down,
            DashDirection.ZPositive => Vector3.forward,
            _                       => Vector3.back,
        };

        Vector3 toCamera = (_cam.position - transform.position).normalized;

        // axis와 카메라가 거의 평행하면 스킵
        if (Mathf.Abs(Vector3.Dot(axis, toCamera)) > 0.99f) return;

        // Quad UV상 uv.x가 axis 방향, uv.y가 폭 방향
        // → Quad의 로컬 right(X축)가 axis와 일치해야 화살표가 axis 방향으로 흐름
        // → Quad의 로컬 up(Y축)이 axis × toCamera 수직면 위에 놓임
        // → Quad의 로컬 forward(Z축, 법선)가 카메라를 향함

        // Quad 로컬 X = axis
        // Quad 로컬 Z = axis × (axis × toCamera) 방향 (카메라를 향하는 법선)
        Vector3 quadRight   = axis;
        Vector3 quadUp      = Vector3.Cross(toCamera, axis).normalized;
        Vector3 quadForward = Vector3.Cross(quadRight, quadUp).normalized;

        if (quadUp.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(quadForward, quadUp);
    }
}
