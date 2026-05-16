
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
    [SerializeField] private float popDis = 0.8f;
    [SerializeField] private float popDur = 0.3f;

    private const int CubeSize = 10;

    // 이펙트나 애니메이션 없이 단순 렌더링
    // 초기화나 undo 할때 사용
    public void SimpleRender(char tile)
    {
        //Debug.Log(tile);
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

    public async UniTask Pop()
    {
        var target = Vector3.zero;
        if (transform.position.x == 0) 
            --target.x;
        else if (transform.position.x == CubeSize - 1)
            ++target.x;
        if (transform.position.y == 0)
            --target.y;
        else if (transform.position.y == CubeSize - 1)
            ++target.y;
        if (transform.position.z == 0)
            --target.z;
        else if (transform.position.z == CubeSize - 1)
            ++target.z;

        target = transform.position + target.normalized * popDis;

        await transform.DOMove(target, popDur).SetLoops(2, LoopType.Yoyo).AsyncWaitForCompletion().AsUniTask();
    }
    
    // 이펙트나 애니메이션이 적용된 렌더링
    // TODO: 이펙트 있는 렌더링 구현
    public async UniTask Render(char tile, bool wait = true)
    {
        await UniTask.Yield();
    }
}
