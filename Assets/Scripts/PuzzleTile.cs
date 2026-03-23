using System.Threading.Tasks;
using UnityEngine;

public class PuzzleTile : MonoBehaviour
{
    // 이펙트나 애니메이션 없이 단순 렌더링
    // 초기화나 undo 할때 사용
    public void SimpleRender(char tile)
    {
        var id = tile - '0';
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
        
        transform.GetChild(id).gameObject.SetActive(true);
    }
    
    // 이펙트나 애니메이션이 적용된 렌더링
    public async Task Render(char tile, bool wait = true)
    {
        
    }
}
