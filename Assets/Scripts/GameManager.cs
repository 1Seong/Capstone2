
using System;
using System.Collections.Generic;
using com.example;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private static readonly int PulseEnabled = Shader.PropertyToID("_PulseEnabled");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public bool isPlaying = true;
    public event Action UserClearEvent;
    public event Action UserEnterEvent;
    public event Action SingleClearEvent;
    public event Action SingleEnterEvent;
    public event Action ParticleOptionEvent;
    public event Action GridOptionEvent;
    
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
    private string _currentSingleMapId;
    private short? _currentSingleBestMoves;
    public bool blockIndicators;

    private bool _showGrid;
    public bool ShowGrid {get => _showGrid;}
    private bool _showParticle;
    public bool  ShowParticle {get => _showParticle;}
    [SerializeField] private Toggle gridToggle;
    [SerializeField] private Toggle particleToggle;

    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject color1SelectImage;
    [SerializeField] private GameObject color2SelectImage;
    [SerializeField] private Material paintedMat;
    [SerializeField] private Toggle pulseToggle;
    [SerializeField] private Material roadMat;
    [SerializeField] private Color color1;
    [SerializeField] private Color color2;
    
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
        QualitySettings.vSyncCount = 1;     // vSync 활성화 (모니터 주사율에 동기화)
        //Application.targetFrameRate = 60;
        _showGrid = PlayerPrefs.GetInt("showGrid", 1) == 1;
        _showParticle = PlayerPrefs.GetInt("showParticle", 1) == 1;
        gridToggle.isOn = _showGrid;
        particleToggle.isOn = _showParticle;
        paintedMat.SetFloat(PulseEnabled, PlayerPrefs.GetInt("pulse", 1));
        pulseToggle.isOn = PlayerPrefs.GetInt("pulse", 1) == 1;
        color1SelectImage.SetActive(false);
        color2SelectImage.SetActive(false);
        if (PlayerPrefs.GetInt("roadColor", 0) == 0)
        {
            color1SelectImage.SetActive(true);
            roadMat.SetColor(BaseColor, color1);
        }
        else
        {
            color2SelectImage.SetActive(true);
            roadMat.SetColor(BaseColor, color2);
        }
    }

    public void GridToggle(bool b)
    {
        _showGrid = b;
        GridOptionEvent?.Invoke();
        PlayerPrefs.SetInt("showGrid", _showGrid ? 1 : 0);
    }

    public void ParticleToggle(bool b)
    {
        _showParticle = b;
        ParticleOptionEvent?.Invoke();
        PlayerPrefs.SetInt("showParticle", _showParticle ? 1 : 0);
    }

    public void PulseToggle(bool b)
    {
        paintedMat.SetFloat(PulseEnabled, b ? 1 : 0);
        PlayerPrefs.SetInt("pulse", b  ? 1 : 0);
    }

    public void SetColor1()
    {
        roadMat.SetColor(BaseColor, color1);
        color1SelectImage.SetActive(true);
        color2SelectImage.SetActive(false);
        PlayerPrefs.SetInt("roadColor", 0);
    }
    
    public void SetColor2()
    {
        roadMat.SetColor(BaseColor, color2);
        color2SelectImage.SetActive(true);
        color1SelectImage.SetActive(false);
        PlayerPrefs.SetInt("roadColor", 1);
    }

    public void ShowOption()
    {
        optionPanel.SetActive(true);
    }

    public void HideOption()
    {
        optionPanel.SetActive(false);
    }

    public async UniTask EnterGameSingle(long mapId, string mapData, string portalData, string rotData)
    {
        await SceneChange.Instance.LoadSceneAddition("SinglePuzzlePlayScene", false);
        SingleEnterEvent?.Invoke();
        SingleEnterEvent = null;

        _currentSingleMapId = mapId.ToString();
        _currentSingleBestMoves = (short)SaveManager.Instance.LoadClear(_currentSingleMapId);
        if (_currentSingleBestMoves.Value == -1)
            _currentSingleBestMoves = null;

        var data = StringHelper.DecodeCube(mapData);
        var portalPairDic = PortalPairHelper.ToDict(PortalPairHelper.Decode(portalData));
        var rotInfo = RotateHelper.Decode(rotData);
        
        PlayGame(data, portalPairDic, rotInfo.Axis, rotInfo.Layers);
        await SceneChange.Instance.UnloadScene("SingleHub");
        //await SceneChange.Instance.ManualEndFade();
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
        singleResultTMP.text = $"이동 횟수: {moves.ToString()}";
        if (_currentSingleBestMoves == null || moves < _currentSingleBestMoves)
        {
            SaveManager.Instance.SaveClear(_currentSingleMapId, moves);
            singleBestText.SetActive(true);
        }

        singleClearPanel.SetActive(true);
    }
    
    public async void ReturnToHubMap()
    {
        singleClearPanel.SetActive(false);
        await SceneChange.Instance.LoadScene("SingleHub");
    }
    
    public async UniTaskVoid GameClearedUser(short moves)
    {
        isPlaying = false;
        userResultTMP.text = $"이동 횟수: {moves.ToString()}";
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
        testResultTMP.text = $"이동 횟수: {moves.ToString()}";
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
