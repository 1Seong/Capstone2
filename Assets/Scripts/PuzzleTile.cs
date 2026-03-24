using System.Threading.Tasks;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    private char _tileCache = '0';
    // 이펙트나 애니메이션 없이 단순 렌더링
    // 초기화나 undo 할때 사용
    public void SimpleRender(char tile)
    {
        if (tile == _tileCache) return;
        _tileCache = tile;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        switch (tile)
        {
            case '0': // empty
                return;
            case '1': // painted
            case '2': // player
                transform .GetChild(0).gameObject.SetActive(true);
                break;
            default:
                var id = tile - '0';
                transform.GetChild(id - 2).gameObject.SetActive(true);
                break;
        }
    }
    
    // 이펙트나 애니메이션이 적용된 렌더링
    public async Task Render(char tile, bool wait = true)
    {
        
    }
}
