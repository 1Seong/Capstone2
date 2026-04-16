using System;
using System.Collections.Generic;
using com.example;
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

    public void EnterGame(char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, int rotateAxis = 0, bool[] canRotate = null)
    {
        // 플레이 씬 로드
        // 이전 씬 로드하는 함수 등록
        PlayGame(data, portalPairDic, rotateAxis, canRotate);
    }

    public void PlayGame(char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, int rotateAxis = 0, bool[] canRotate = null,
        bool isTest = false)
    {
        var o = FindAnyObjectByType<PuzzlePlayer>(FindObjectsInactive.Include);
        
        o.SetMapData(data, portalPairDic, rotateAxis, canRotate, isTest);
        o.gameObject.SetActive(true);
    }

    public void EnterEditor(MapCreating mapCreating)
    {
        // 에디터 씬 로드
        MapEditor.Instance.Initialize(mapCreating);
    }

    public void GameCleared(int moves)
    {
        // 싱글이냐 유저맵이냐에 따라 다름
    }

    public void GameClearedTest(int moves)
    {
        testResultTMP.text = $"움직임 수: {moves.ToString()}";
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

    public void Stop()
    {
        isPlaying = false;
    }

    public void Play()
    {
        isPlaying = true;
    }

    public bool CheckNetworkAndLogIn()
    {
        if (!SupabaseManager.Instance.IsNetworkAvailable())
        {
            Debug.LogWarning("오프라인");
            PopUpManager.Instance.Show("네트워크에 연결 상태를 확인해주세요.");
            return false;
        }
        if (!SupabaseManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("로그아웃 상태");
            PopUpManager.Instance.Show("로그인 되어 있지 않습니다.");
            return false;
        }

        return true;
    }
    
    public bool CheckNetwork()
    {
        if (!SupabaseManager.Instance.IsNetworkAvailable())
        {
            Debug.LogWarning("오프라인");
            PopUpManager.Instance.Show("네트워크에 연결 상태를 확인해주세요.");
            return false;
        }

        return true;
    }
}
