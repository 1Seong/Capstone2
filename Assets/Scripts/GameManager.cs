
using System;
using System.Collections.Generic;
using com.example;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool isPlaying = true;
    public event Action UserClearEvent;
    public event Action UserEnterEvent;
    public event Action SingleClearEvent;
    public event Action SingleEnterEvent;
    
    [SerializeField] private GameObject testClearPanel;
    [SerializeField] private GameObject userClearPanel;
    [SerializeField] private GameObject singleClearPanel;
    [SerializeField] private TMP_Text testResultTMP;
    [SerializeField] private TMP_Text userResultTMP;
    [SerializeField] private TMP_Text singleResultTMP;
    [SerializeField] private GameObject userBestText;
    [SerializeField] private GameObject singleBestText;
    private MapCreating _currentMapCreating;
    private long _currentUserMapId;
    private short? _currentUserBestMoves;
    private Guid _currentUserMapUserId;
    private int _currentSingleMapId;
    private short? _currentSingleBestMoves;
    public bool blockIndicators;
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

    public async UniTask EnterGameSingle(int id, short? best, char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, int rotateAxis = 0, bool[] canRotate = null)
    {
        await SceneChange.Instance.LoadSceneAddition("PuzzlePlayScene", false);
        SingleEnterEvent?.Invoke();
        SingleEnterEvent = null;
        _currentSingleMapId = id;
        _currentSingleBestMoves = best;
        PlayGame(data, portalPairDic, rotateAxis, canRotate);
        await SceneChange.Instance.LoadSceneAddition("PuzzlePlayScene", false);
    }
    
    public async UniTask EnterGameUser(long id, Guid userId, short? best, char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, int rotateAxis = 0, bool[] canRotate = null)
    {
        await SceneChange.Instance.LoadSceneAddition("PuzzlePlayScene", false);
        UserEnterEvent?.Invoke();
        UserEnterEvent = null;
        _currentUserMapId = id;
        _currentUserMapUserId =  userId;
        _currentUserBestMoves = best;
        PlayGame(data, portalPairDic, rotateAxis, canRotate);
        await SceneChange.Instance.ManualEndFade();
    }

    public void PlayGame(char[,,] data, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, int rotateAxis = 0, bool[] canRotate = null)
    {
        isPlaying = true;
        var o = FindAnyObjectByType<PuzzlePlayer>(FindObjectsInactive.Include);
        
        o.SetMapData(data, portalPairDic, rotateAxis, canRotate);
        o.gameObject.SetActive(true);
    }

    public async void EnterEditor(MapCreating mapCreating)
    {
        _currentMapCreating = mapCreating;
        await SceneChange.Instance.LoadScene("PuzzleEdit");
        //MapEditor.Instance.Initialize(mapCreating);
    }

    public MapCreating GetMapCreating()
    {
        var m = _currentMapCreating;
        _currentMapCreating = null;
        return m;
    }

    public void GameClearedSingle(int moves)
    {
        isPlaying = false;
        // 싱글이냐 유저맵이냐에 따라 다름
    }
    
    public async void ReturnToHubMap()
    {
        await UniTask.Yield();
    }
    
    public async UniTaskVoid GameClearedUser(short moves)
    {
        isPlaying = false;
        userResultTMP.text = $"움직임 수: {moves.ToString()}";
        if(_currentUserBestMoves == null || moves < _currentUserBestMoves)
            userBestText.SetActive(true);
        userClearPanel.SetActive(true);
        
        //SceneChange.Instance.LightLoading(true);
        try
        {
            await DBManager.Instance.UpsertMapClearsAsync(new MapClears()
            {
                UserId = Guid.Parse(SupabaseManager.Instance.Supabase().Auth.CurrentUser.Id),
                MapId = _currentUserMapId,
                MapUserId = _currentUserMapUserId,
                Moves = moves
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            PopUpManager.Instance.Show("기록 업로드 실패");
        }
        //SceneChange.Instance.LightLoading(false);
    }

    public async void ReturnToUserMapList()
    {
        userClearPanel.SetActive(false);
        await SceneChange.Instance.UnloadScene("PuzzlePlayScene");
    }

    public void GameClearedEventInvoke()
    {
        UserClearEvent?.Invoke();
        UserClearEvent = null;
    }

    public void GameClearedTest(int moves)
    {
        isPlaying = false;
        testResultTMP.text = $"움직임 수: {moves.ToString()}";
        testClearPanel.SetActive(true);
    }

    public void ReturnToEditor()
    {
        testClearPanel.SetActive(false);
        var player = FindAnyObjectByType<PuzzlePlayer>();
        
        MapEditor.Instance.gameObject.SetActive(true);
        MapEditor.Instance.IsTesting = false;
        player.gameObject.SetActive(false);
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
