using System;
using UnityEngine;

public class EditTile : MonoBehaviour
{
    [SerializeField] private MeshRenderer cellIndicateMeshRenderer;

    //TODO : 블럭 별 material 다르게 설정
    
    private void OnMouseEnter()
    {
        cellIndicateMeshRenderer.enabled = true;
        
        if(MapEditor.Instance.CurrentTile == (char)TileType.PortalOut)
            MapEditor.Instance.currentLine.SetPosition(1, new Vector3(transform.position.x, transform.position.y, transform.position.z));

        if (Input.GetMouseButton(0))
        {
            OnMouseDown();
        }
    }
    
    private void OnMouseDown()
    {
        var editor = MapEditor.Instance;
        var pos = transform.position;
        editor.SetTile((int)pos.x, (int)pos.y, (int)pos.z);
    }

    private void OnMouseExit()
    {
        cellIndicateMeshRenderer.enabled = false;
    }
    
    public void SetHighlight(bool matEnabled)
    {
        cellIndicateMeshRenderer.enabled = matEnabled;
    }
}
