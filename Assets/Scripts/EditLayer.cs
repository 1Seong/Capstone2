using UnityEngine;

public class EditLayer : MonoBehaviour
{
    [SerializeField] private MeshRenderer cellIndicateMeshRenderer;
    
    private void OnMouseEnter()
    {
        cellIndicateMeshRenderer.enabled = true;
        
        if (Input.GetMouseButton(0))
        {
            OnMouseDown();
        }
    }
    
    private void OnMouseDown()
    {
        var axis = MapEditor.Instance.SelectedAxis;
        int targetIndex = 1;
        switch (axis)
        {
            case 1: // x
                targetIndex = (int)transform.position.x;
                break;
            case 2: // y
                targetIndex = (int)transform.position.y;
                break;
            case 3: // z
                targetIndex = (int)transform.position.z;
                break;
        }
        MapEditor.Instance.SetLayer(targetIndex);
    }

    private void OnMouseExit()
    {
        cellIndicateMeshRenderer.enabled = false;
    }
}
