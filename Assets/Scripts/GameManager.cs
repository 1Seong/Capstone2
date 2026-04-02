using System;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isPlaying = true;

    [SerializeField] private GameObject testClearPanel;
    [SerializeField] private TMP_Text testResultTMP;

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

    public void EnterGame(char[,,] data)
    {
        // 플레이 씬 로드
        PlayGame(data);
    }

    public void PlayGame(char[,,] data, bool isTest = false)
    {
        var o = FindAnyObjectByType<PuzzlePlayer>(FindObjectsInactive.Include);
        
        o.SetMapData(data, isTest);
        o.gameObject.SetActive(true);
    }

    public void EnterEditor(char[,,] data)
    {
        //에디터 씬 로드
        MapEditor.Instance.SetMapData(data);
    }

    public void GameCleared(TimeSpan ts, int moves)
    {
        // 싱글이냐 유저맵이냐에 따라 다름
    }

    public void GameClearedTest(TimeSpan ts, int moves)
    {
        testResultTMP.text = $"클리어 시간: {ts.Minutes:D2}:{ts.Seconds:D2}\n움직임 수: {moves.ToString()}";
        testClearPanel.SetActive(true);
    }

    public void ReturnToEditor()
    {
        testClearPanel.SetActive(false);
        var player = FindAnyObjectByType<PuzzlePlayer>();
        
        MapEditor.Instance.gameObject.SetActive(true);
        player.gameObject.SetActive(false);
    }
}
