using UnityEngine;

public class BilboardBeam : MonoBehaviour
{
    public Vector3 axis; // 축 방향 (부모가 설정)

    void LateUpdate()
    {
        Transform cam = Camera.main.transform;
    
        // 쿼드의 Z(forward)는 축 방향으로 고정
        // 쿼드의 Y는 카메라에서 빔을 바라볼 때 수직이 되는 방향
        Vector3 toCamera = (cam.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(axis, toCamera).normalized;

        // right와 axis로 rotation 확정
        if (right.sqrMagnitude > 0.01f)
        {
            Vector3 up = Vector3.Cross(right, axis).normalized;
            transform.rotation = Quaternion.LookRotation(axis, up);
        }
    }
}
