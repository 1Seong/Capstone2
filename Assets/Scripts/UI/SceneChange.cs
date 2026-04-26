using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public static SceneChange Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void LoadScene(string sceneName)
    {
        // TODO : Scene Effect
        
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneAddition(string sceneName)
    {
        // TODO : Scene Effect
        SceneManager.LoadScene(sceneName,  LoadSceneMode.Additive);
    }
}
