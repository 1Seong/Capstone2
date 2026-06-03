using UnityEngine;

public class StarEdge : MonoBehaviour
{
    [SerializeField] private Transform from;
    [SerializeField] private Transform to;
    [SerializeField] private LineRenderer line;

    private void Awake()
    {
        line.positionCount = 2;
    }

    private void LateUpdate()
    {
        line.SetPosition(0, from.position - transform.position);
        line.SetPosition(1, to.position- transform.position);
    }
}
