using UnityEngine;

public class StarDetector : MonoBehaviour
{
    [SerializeField] private StarNode starNode;

    private void OnMouseEnter()
    {
        starNode.Attach();
    }

    private void OnMouseExit()
    {
        starNode.Detach();
    }
}
