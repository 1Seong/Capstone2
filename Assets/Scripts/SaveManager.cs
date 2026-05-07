using System;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void SaveClear(string mapId, int moves)
    {
        PlayerPrefs.SetInt(mapId, moves);
        PlayerPrefs.Save();
    }

    public int LoadClear(string mapId)
    {
        return PlayerPrefs.GetInt(mapId, -1);
    }
}
