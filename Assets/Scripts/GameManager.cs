using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isPlaying = true;

    [SerializeField] private GameObject testClearPanel;

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

    public void PlayGame(char[,,] data, bool isTest = false)
    {
        var o = FindAnyObjectByType<PuzzlePlayer>(FindObjectsInactive.Include);
        
        o.SetMapData(data, isTest);
        o.gameObject.SetActive(true);
    }

    public void GameCleared()
    {
        
    }

    public void GameClearedTest()
    {
        testClearPanel.SetActive(true);
    }

    public void ReturnToEditor()
    {
        testClearPanel.SetActive(false);
        var editor = FindAnyObjectByType<MapEditor>(FindObjectsInactive.Include);
        var player = FindAnyObjectByType<PuzzlePlayer>();
        
        editor.gameObject.SetActive(true);
        player.gameObject.SetActive(false);
    }
}
