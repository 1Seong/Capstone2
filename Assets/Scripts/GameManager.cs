using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    //[SerializeField] private GameObject playInstance;
    
    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayGame(char[,,] data)
    {
        var o = FindAnyObjectByType<PuzzlePlayer>();
        
        o.SetMapData(data);
        o.gameObject.SetActive(true);
    }
}
