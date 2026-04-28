using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public async void LoadScene(string name)
    {
        await SceneChange.Instance.LoadScene(name);
    }
    
    public async void LoadSceneWithLoginCheck(string name)
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        await SceneChange.Instance.LoadScene(name, false);
    }
}
