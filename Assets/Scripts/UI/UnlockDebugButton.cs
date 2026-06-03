using UnityEngine;

public class UnlockDebugButton : MonoBehaviour
{
    public void UnLock()
    {
        SaveManager.Instance.UnlockAllMaps();
    }
    
    public void Lock()
    {
        SaveManager.Instance.LockAllMaps();
    }
}
