using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void ShowOption()
    {
        GameManager.Instance.ShowOption();
    }
    
    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
