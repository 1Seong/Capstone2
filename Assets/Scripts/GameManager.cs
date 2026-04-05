using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isPlaying = true;

    [SerializeField] private GameObject testClearPanel;
    [SerializeField] private TMP_Text testResultTMP;

    public event Action OnScreenExitEvent;
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

    public void EnterGame(char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null)
    {
        // 플레이 씬 로드
        // 이전 씬 로드하는 함수 등록
        PlayGame(data);
    }

    public void PlayGame(char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, bool isTest = false)
    {
        var o = FindAnyObjectByType<PuzzlePlayer>(FindObjectsInactive.Include);
        
        o.SetMapData(data, portalPairDic, isTest);
        o.gameObject.SetActive(true);
    }

    public void EnterEditor(MapCreating mapCreating)
    {
        // 에디터 씬 로드
        MapEditor.Instance.Initialize(mapCreating);
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
        OnScreenExitEvent += MapEditor.Instance.ExitEditor;
        OnScreenExitEvent -= ReturnToEditor;
        
        testClearPanel.SetActive(false);
        var player = FindAnyObjectByType<PuzzlePlayer>();
        
        MapEditor.Instance.gameObject.SetActive(true);
        player.gameObject.SetActive(false);
    }

    public void ExitToMenuButton()
    {
        OnScreenExitEvent?.Invoke();
    }
}
