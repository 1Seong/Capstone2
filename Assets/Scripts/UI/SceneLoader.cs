using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string name)
    {
        SceneChange.Instance.LoadScene(name);
    }
    
    public void LoadSceneWithLoginCheck(string name)
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        SceneChange.Instance.LoadScene(name);
    }
}
