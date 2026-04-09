
using UnityEngine;

public class EditTile : MonoBehaviour
{
    [SerializeField] private MeshRenderer cellIndicateMeshRenderer;
    [SerializeField] private GameObject[] arrows;
    private static readonly Color GrayColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    private static readonly Color RedColor = new Color(1f, 0f, 0f, 0.3f);

    //TODO : 블럭 별 material 다르게 설정
    private int x, y, z;
    private void Awake()
    {
        x = (int)transform.position.x;
        y = (int)transform.position.y;
        z = (int)transform.position.z;
    }

    private void OnMouseEnter()
    {
        cellIndicateMeshRenderer.material.color = GrayColor;
        var tile = MapEditor.Instance.CurrentTile;
        var max = MapEditor.Instance.cubeSize - 1;
        switch (tile)
        {
            case (char)TileType.DashXp:
                if (x == max || x == 0 && y > 0 && y < max && z > 0 && z < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
            case (char)TileType.DashXm:
                if (x == 0 || x == 9 && y > 0 && y < max && z > 0 && z < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
            case (char)TileType.DashYp:
                if (y == max || y == 0 && x > 0 && x < max && z > 0 && z < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
            case (char)TileType.DashYm:
                if (y == 0 || y == max && x > 0 && x < max && z > 0 && z < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
            case (char)TileType.DashZp:
                if (z == max || z == 0 && x > 0 && x < max && y > 0 && y < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
            case (char)TileType.DashZm:
                if (z == 0 || z == max && x > 0 && x < max && y > 0 && y < max)
                    cellIndicateMeshRenderer.material.color = RedColor;
                break;
        }
        cellIndicateMeshRenderer.enabled = true;
        
        if(tile == (char)TileType.PortalOut)
            MapEditor.Instance.currentLine.SetPosition(1, new Vector3(transform.position.x, transform.position.y, transform.position.z));

        if (Input.GetMouseButton(0))
        {
            OnMouseDown();
        }
    }
    
    private void OnMouseDown()
    {
        var editor = MapEditor.Instance;
        var tile = editor.CurrentTile;
        var max = editor.cubeSize - 1;
        switch (tile)
        {
            case (char)TileType.DashXp when x == max || x == 0 && y > 0 && y < max && z > 0 && z < max:
            case (char)TileType.DashXm when x == 0 || x == max && y > 0 && y < max && z > 0 && z < max:
            case (char)TileType.DashYp when y == max || y == 0 && x > 0 && x < max && z > 0 && z < max:
            case (char)TileType.DashYm when y == 0 || y == max && x > 0 && x < max && z > 0 && z < max:
            case (char)TileType.DashZp when z == max || z == 0 && x > 0 && x < max && y > 0 && y < max:
            case (char)TileType.DashZm when z == 0 || z == max && x > 0 && x < max && y > 0 && y < max:
                PopUpManager.Instance.Show("해당 위치에 설치할 수 없습니다!");
                return;
        }
        editor.SetTile(x, y, z);
    }

    private void OnMouseExit()
    {
        cellIndicateMeshRenderer.enabled = false;
        foreach(var arrow in arrows)
            arrow.SetActive(false);
    }
}
