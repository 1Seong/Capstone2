
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    private char _tileCache = 'A';
    
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer arrowRenderer;
    [SerializeField] private MeshRenderer quadRenderer;
    [SerializeField] private Material[] mats;
    [SerializeField] private Material[] arrowMats;
    [SerializeField] private Material[] quadMats;

    // 이펙트나 애니메이션 없이 단순 렌더링
    // 초기화나 undo 할때 사용
    public void SimpleRender(char tile)
    {
        if (tile == _tileCache) return;
        _tileCache = tile;

        meshRenderer.enabled = false;
        arrowRenderer.gameObject.SetActive(false);
        quadRenderer.gameObject.SetActive(false);
        if (tile != (char)TileType.PortalIn && transform.childCount == 3)
        {
            transform.GetChild(2).gameObject.SetActive(false);
        }

        int id;
        switch (tile)
        {
            case (char)TileType.Empty:
            case (char)TileType.Player:
                return;
            case (char)TileType.Road:
            case (char)TileType.Painted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                break;
            case (char)TileType.DashXp:
            case (char)TileType.DashXpPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case (char)TileType.DashXm:
            case (char)TileType.DashXmPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
            case (char)TileType.DashYp:
            case (char)TileType.DashYpPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, 0, -90);
                break;
            case (char)TileType.DashYm:
            case (char)TileType.DashYmPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case (char)TileType.DashZp:
            case (char)TileType.DashZpPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            case (char)TileType.DashZm:
            case (char)TileType.DashZmPainted:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                arrowRenderer.gameObject.SetActive(true);
                arrowRenderer.material = arrowMats[tile-(int)TileType.DashXp];
                arrowRenderer.transform.localRotation = Quaternion.Euler(0, -90, 0);
                break;
            default:
                id = tile - (int)TileType.Empty;
                meshRenderer.enabled = true;
                meshRenderer.material = mats[id];
                quadRenderer.gameObject.SetActive(true);
                quadRenderer.material = quadMats[id];
                break;
        }
    }
    
    // 이펙트나 애니메이션이 적용된 렌더링
    // TODO: 이펙트 있는 렌더링 구현
    public async UniTask Render(char tile, bool wait = true)
    {
        await UniTask.Yield();
    }
}
