using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    private char _tileCache = 'A';

    private readonly int _initialChildNum = (int)TileType.Count - (int)TileType.Empty;
    // 이펙트나 애니메이션 없이 단순 렌더링
    // 초기화나 undo 할때 사용
    public void SimpleRender(char tile)
    {
        if (tile == _tileCache) return;
        _tileCache = tile;

        if (tile == (char)TileType.PortalIn)
        {
            for (var i = 0; i != _initialChildNum; ++i)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);

        switch (tile)
        {
            case (char)TileType.Empty:
            case (char)TileType.Player:
                return;
            default:
                var id = tile - (int)TileType.Empty;
                //Debug.Log(tile.ToString() + " " + id.ToString());
                transform.GetChild(id).gameObject.SetActive(true);
                break;
        }
    }
    
    // 이펙트나 애니메이션이 적용된 렌더링
    public async UniTask Render(char tile, bool wait = true)
    {
        
    }
}
