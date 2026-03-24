using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject playInstance;
    
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

    private void PlayGame(char[,,] data)
    {
        var o = Instantiate(playInstance);
        
        o.GetComponent<PuzzlePlayer>().SetMapData(data);
        o.SetActive(true);
    }
}
