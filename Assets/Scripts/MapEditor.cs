using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.example;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MapEditor : MonoBehaviour
{
    public int cubeSize = 10;
    private char[,,] _map;
    private MapCreating _currentMapCreating;
    public static MapEditor Instance;
    [SerializeField] private char currentTile = (char)TileType.Road;
    public char CurrentTile
    {
        get => currentTile;
        set
        {
            if (currentTile == (char)TileType.PortalOut && value !=  (char)TileType.PortalIn)
            {
                _tiles[_previousPortalInPos.x, _previousPortalInPos.y, _previousPortalInPos.z].SimpleRender((char)TileType.Empty);
                PortalEditing = false;
            }
            currentTile = value;
        }
    }

    private Vector3Int _previousPortalInPos;
    private bool _portalEditing = false;
    public bool PortalEditing
    {
        set
        {
            if (value)
                camera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            else
                camera.cullingMask |= (1 << LayerMask.NameToLayer("UI"));
            _portalEditing = value;
        }
    }
    private Dictionary<Vector3Int, Vector3Int> _portalPairDict = new();

    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform  cubeParent;
    [SerializeField] private GameObject tileIndicatorParent;
    [SerializeField] private GameObject[] indicators;
    [SerializeField] private GameObject rightSideButtonsParent;
    [SerializeField] private Button exportButton;
    [SerializeField] private GameObject linePrefab;
    public LineRenderer currentLine;
    [SerializeField] private Camera camera;
    [SerializeField] private Camera thumbnailCamera;
    private PuzzleTile[,,] _tiles;
    private bool _isValidated;
    
    [SerializeField] private Button showAnswerButton;
    [SerializeField] private GameObject stopShowAnswerButton;
    [SerializeField] private Transform ghostPlayer;
    [SerializeField] private float showAnswerTimePerPos = 0.5f;
    private CancellationTokenSource _showAnswerCts;
    private Stack<Vector3Int> _answer;

    private int _selectedAxis = 0;
    public int SelectedAxis // 툴에서 선택된 축
    {
        get => _selectedAxis;
        set
        {
            foreach(var i in indicators)
                i.SetActive(false);
            if (value == 0) // 타일 모드이면 다시 툴셋 패널 활성화
                leftPanel.SetActive(true);
            else
                leftPanel.SetActive(false);
            indicators[value].SetActive(true);
            _selectedAxis = value; 
        }
    }
    private int _rotAxis = 0;
    private int RotAxis
    {
        set
        {
            if (_rotAxis != value) // 다른 축으로 전환
            {
                if (_rotAxis != 0) // 이전에 있던 축이 빈 축이 아니면 정리
                {
                    for (int i = 0; i != cubeSize; ++i)
                        _canRotate[i] = false;
                    LayerRender();
                }
                foreach (var i in innerCubes)
                    i.gameObject.SetActive(false);
                innerCubes[value].gameObject.SetActive(true);
            }
            _rotAxis = value;
        }
    }
    private bool[] _canRotate;
    [SerializeField] private GameObject leftPanel;
    [SerializeField] private Transform[] innerCubes;
    
    private struct MapState
    {
        public char[,,] Map;
        public Vector3 PlayerPos;
        public bool PlayerActive;
    }

    private readonly Stack<MapState> _undoStack = new();
    
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (char[,,])_map.Clone();
        _undoStack.Push(new MapState()
            { Map = snapshot, PlayerPos = playerModel.position, PlayerActive = playerModel.gameObject.activeSelf});
    }

    public void Undo(bool doRender = true)
    {
        if (_undoStack.Count == 0) return;
        
        SetValidated(false);
        var s = _undoStack.Pop();
        _map = s.Map;
        playerModel.position = s.PlayerPos;
        playerModel.gameObject.SetActive(s.PlayerActive);
        PortalEditing = false;
        
        if(CurrentTile == (char)TileType.PortalOut)
            CurrentTile = (char)TileType.PortalIn;

        if (doRender)
            Render();
    }
    
    private void Render()
    {
        for (var i = 0; i != cubeSize; ++i)
        for (var j = 0; j != cubeSize; ++j)
        for (var k = 0; k != cubeSize; ++k)
        {
            var c = _map[i, j, k];
            _tiles[i, j, k].SimpleRender(c);
        }
    }

    private void LayerRender()
    {
        for (int i = 1; i != cubeSize-1; ++i)
                innerCubes[_rotAxis].GetChild(i).GetChild(0).gameObject.SetActive(_canRotate[i]);
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
        }

        showAnswerButton.onClick.AddListener(() => ShowAnswer().Forget());
        
        _tiles = new PuzzleTile[cubeSize, cubeSize, cubeSize];
        int x = 0;
        foreach (Transform plane in cubeParent) // 평면 (10개)
        {
            int y = 0;
            foreach (Transform row in plane) // 줄 (10개)
            {
                int z = 0;
                foreach (Transform cell in row) // 칸 (10개)
                {
                    _tiles[x, y, z] = cell.GetComponent<PuzzleTile>();
                    z++;
                }
                y++;
            }
            x++;
        }

        _canRotate = new bool[cubeSize];
        
        SetValidated(false);

        if (_map is null)
        {
            _map = new char[cubeSize, cubeSize, cubeSize];
            for (int i = 0; i != cubeSize; ++i)
            for (int j = 0; j != cubeSize; ++j)
            for (int k = 0; k != cubeSize; ++k)
                _map[i, j, k] = (char)TileType.Empty;
        }
    }

    private void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
    }

    public void ResetEditor()
    {
        SetValidated(false);
        
        playerModel.gameObject.SetActive(false);
        
        for (int i = 0; i != cubeSize; ++i)
        for (int j = 0; j != cubeSize; ++j)
        for (int k = 0; k != cubeSize; ++k)
            _map[i, j, k] = (char)TileType.Empty;

        _portalPairDict = new();
        RotAxis = 0;
        Render();
    }

    public void Initialize(MapCreating mapCreating) // 외부에서 데이터 넣어주고 초기화
    {
        GameManager.Instance.OnScreenExitEvent += ExitEditor;
        _currentMapCreating = mapCreating;
        if(_currentMapCreating.Data != null)
            _map = StringHelper.DecodeCube(_currentMapCreating.Data);
        if (_currentMapCreating.PortalPairs != null)
        {
            var portalPairList = PortalPairHelper.Decode(_currentMapCreating.PortalPairs);
            _portalPairDict = PortalPairHelper.ToDict(portalPairList);

            foreach (var i in portalPairList)
            {
                var l = Instantiate(linePrefab, _tiles[i.InPos.x, i.InPos.y, i.InPos.z].transform)
                    .GetComponent<LineRenderer>();
                l.SetPosition(0, i.InPos);
                l.SetPosition(1, i.OutPos);
            }
        }
        if (_currentMapCreating.RotInfo != null)
        {
            var rotInfo = RotateHelper.Decode(_currentMapCreating.RotInfo);
            RotAxis = rotInfo.Axis;
            _canRotate = rotInfo.Layers;
        }
        LayerRender();
        Render();
    }

    public void SetTile(int layer, int row, int col)
    {
        if (_portalEditing && _previousPortalInPos == new Vector3Int(layer, row, col))
        {
            PopUpManager.Instance.Show("다른 위치를 지정해주세요");
            return;
        }
        
        if(!_portalEditing)
            SaveUndoState();
        //Debug.Log(layer + " " + row + " " + col);
        SetValidated(false);
        
        if(playerModel.position == new Vector3(layer, row, col))
        {
            playerModel.gameObject.SetActive(false);
        }
        else if (_portalPairDict.ContainsKey(new Vector3Int(layer, row, col)))
        {
            var key = new Vector3Int(layer, row, col);
            var val = _portalPairDict[key];
            _portalPairDict.Remove(key);
            _portalPairDict.Remove(val);
            _map[val.x, val.y, val.z] = (char)TileType.Empty;
            _tiles[val.x, val.y, val.z].SimpleRender((char)TileType.Empty);
        }

        switch (currentTile)
        {
            case (char)TileType.Player:
                if (!playerModel.gameObject.activeSelf)
                {
                    playerModel.gameObject.SetActive(true);
                }
                else // 이전 위치 받아와서 지움, 같은 위치 지정할 경우 주의
                {
                    var pos = playerModel.position;
                    _map[(int)pos.x, (int)pos.y, (int)pos.z] = (char)TileType.Empty;
                }
            
                playerModel.position = new Vector3(layer, row, col);
                _tiles[layer, row, col].SimpleRender((char)TileType.Empty);
                break;
   
            case (char)TileType.PortalIn:
                _previousPortalInPos = new Vector3Int(layer, row, col);
                currentLine = _tiles[layer, row, col].GetComponentInChildren<LineRenderer>(true); // 점선 초기화
                if (currentLine == null)
                {
                    currentLine = Instantiate(linePrefab, _tiles[layer, row, col].transform).GetComponent<LineRenderer>();
                }
                currentLine.gameObject.SetActive(true);
                currentLine.SetPosition(0, new Vector3(layer, row, col));
                currentLine.SetPosition(1,  new Vector3(layer, row, col));
                PortalEditing = true;
                break;
            
            case (char)TileType.PortalOut:
                var outPos = new Vector3Int(layer, row, col);
                _portalPairDict[_previousPortalInPos] = outPos;
                _portalPairDict[outPos] = _previousPortalInPos; // 딕셔너리에 쌍 등록
                currentLine.SetPosition(1,  outPos); // 점선 잇기
                PortalEditing = false;
                break;
        }
        _map[layer, row, col] = currentTile;
        _tiles[layer, row, col].SimpleRender(currentTile);
        
        if(currentTile == (char)TileType.PortalIn)
            CurrentTile = (char)TileType.PortalOut;
        else if(currentTile == (char)TileType.PortalOut)
            CurrentTile = (char)TileType.PortalIn;
    }

    public void SetLayer(int index)
    {
        RotAxis = _selectedAxis;
        _canRotate[index] = !_canRotate[index];
        innerCubes[_rotAxis].GetChild(index).GetChild(0).gameObject.SetActive(_canRotate[index]);
        if (_canRotate.Any(i => i))
            return;
        RotAxis = 0;
    }
    
    public void PlayTest()
    {
        if (!playerModel.gameObject.activeSelf)
        {
            PopUpManager.Instance.Show("시작 위치를 찾을 수 없습니다.");
            return;
        }

        GameManager.Instance.OnScreenExitEvent -= ExitEditor;
        GameManager.Instance.OnScreenExitEvent += GameManager.Instance.ReturnToEditor;
        
        gameObject.SetActive(false);
        // 로딩 오래 걸리면 씬 전환 효과 넣기
        GameManager.Instance.PlayGame((char[,,])_map.Clone(), _portalPairDict, _rotAxis, _canRotate, true);
    }

    public void AutoTest()
    {
        var res = PuzzleSolver.Solve(_map, _portalPairDict);

        if (!res.IsSolvable)
        {
            //Debug.Log(res.ErrorMsg);
            PopUpManager.Instance.Show(res.ErrorMsg);
            return;
        }
        
        SetValidated(true, res.SolutionPath);
        PopUpManager.Instance.Show("자동 테스트를 통과했습니다!");
    }

    public void Export()
    {
        if (!_isValidated) return;
        
        ExportToFileDownloads();
    }

    #region ToolSet
    public void OnPortalLineToggleChanged(bool b)
    {
        if(b)
            ShowPortalLines();
        else
        {
            HidePortalLines();
        }
    }
    
    public void ShowPortalLines()
    {
        camera.cullingMask |= (1 << LayerMask.NameToLayer("PortalLine"));
    }

    public void HidePortalLines()
    {
        camera.cullingMask &= ~(1 << LayerMask.NameToLayer("PortalLine"));
    }

    public void SetCurrentTileToRoad()
    {
        CurrentTile = (char)TileType.Road;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToEraser()
    {
        CurrentTile = (char)TileType.Empty;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToPlayer()
    {
        CurrentTile = (char)TileType.Player;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToPortal()
    {
        CurrentTile = (char)TileType.PortalIn;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToGhost()
    {
        CurrentTile = (char)TileType.Ghost;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToInv()
    {
        CurrentTile = (char)TileType.Inv;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToLaser()
    {
        CurrentTile = (char)TileType.Laser;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToDashXp()
    {
        CurrentTile = (char)TileType.DashXp;
        // 여기에서 현재 mat 지정
    }
    public void SetCurrentTileToDashXm()
    {
        CurrentTile = (char)TileType.DashXm;
        // 여기에서 현재 mat 지정
    }
    public void SetCurrentTileToDashYp()
    {
        CurrentTile = (char)TileType.DashYp;
        // 여기에서 현재 mat 지정
    }
    public void SetCurrentTileToDashYm()
    {
        CurrentTile = (char)TileType.DashYm;
        // 여기에서 현재 mat 지정
    }
    public void SetCurrentTileToDashZp()
    {
        CurrentTile = (char)TileType.DashZp;
        // 여기에서 현재 mat 지정
    }
    public void SetCurrentTileToDashZm()
    {
        CurrentTile = (char)TileType.DashZm;
        // 여기에서 현재 mat 지정
    }

    public void SetCurrentAxis(int i)
    {
        SelectedAxis = i;
    }
    
    #endregion
    public void ExitEditor()
    {
        if(!SupabaseManager.Instance.IsNetworkAvailable() || !SupabaseManager.Instance.IsLoggedIn())
            PopUpManager.Instance.Show("네트워크에 연결되어 있지 않습니다!");
        
        SaveToDB();
        _currentMapCreating = null;
        // 제작 중 맵 목록 씬 로드
    }
    
    public async void SaveToDB()
    {
        /*
        string url;
        // 스크린샷 + DB에 update
        try
        {
            url = await CaptureThumbnailAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            PopUpManager.Instance.Show("썸네일 캡쳐 실패");
        }
        // url 넣기
        */
        // 이름이랑 설명도 수정?
        _currentMapCreating.Data = StringHelper.Encode(_map);
        _currentMapCreating.PortalPairs = PortalPairHelper.Encode(PortalPairHelper.ToList(_portalPairDict));
        _currentMapCreating.RotInfo = RotateHelper.Encode(new RotateInfo{Axis =  _rotAxis, Layers = _canRotate});

        try
        {
            await DBManager.Instance.UpdateMapCreatingAsync(_currentMapCreating);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            PopUpManager.Instance.Show("저장 실패");
        }
    }
    
    public void EndShowAnswer()
    {
        GameManager.Instance.OnScreenExitEvent += ExitEditor;
        GameManager.Instance.OnScreenExitEvent -= EndShowAnswer;
        
        _showAnswerCts?.Cancel();
        _showAnswerCts?.Dispose();
        _showAnswerCts = null;
    
        ghostPlayer.gameObject.SetActive(false);
        tileIndicatorParent.SetActive(true);
        rightSideButtonsParent.SetActive(true);
        stopShowAnswerButton.SetActive(false);
    }

    public async UniTaskVoid ShowAnswer()
    {
        GameManager.Instance.OnScreenExitEvent -= ExitEditor;
        GameManager.Instance.OnScreenExitEvent += EndShowAnswer;
        
        _showAnswerCts?.Cancel();
        _showAnswerCts?.Dispose();
        _showAnswerCts = new CancellationTokenSource();
        
        rightSideButtonsParent.SetActive(false);
        tileIndicatorParent.SetActive(false);
        stopShowAnswerButton.SetActive(true);
        
        ghostPlayer.gameObject.SetActive(true);

        await ShowAnswerAnim(_showAnswerCts.Token);
    
        // Cancel이 아닌 정상 완료일 때만 EndShowAnswer 호출
        if (!_showAnswerCts.IsCancellationRequested)
            EndShowAnswer();
    }

    private async UniTask ShowAnswerAnim(CancellationToken ct)
    {
        foreach(var i in _answer)
        {
            ghostPlayer.position = i;
            await UniTask.WaitForSeconds(showAnswerTimePerPos, cancellationToken: ct);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
        }
        
        if (_portalEditing) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetEditor();
        }
    }
    
    private void ExportToFileDownloads(string fileName = "puzzle_export.txt")
    {
        var content = StringHelper.Encode(_map);
        
        string downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            "Downloads", 
            fileName
        );
    
        File.WriteAllText(downloadsPath, content, System.Text.Encoding.UTF8);
        PopUpManager.Instance.Show($"파일 저장됨: {downloadsPath}");
        Debug.Log($"파일 저장됨: {downloadsPath}");
    }

    public void SetValidated(bool validated, Stack<Vector3Int> answer = null)
    {
        _isValidated = validated;
        showAnswerButton.interactable = validated;
        exportButton.interactable = validated;
        
        if(answer is not null)
            _answer = new Stack<Vector3Int>(answer);
    }
    
    public async Task<string> CaptureThumbnailAsync()
    {
        // 특정 카메라 시점에서 RenderTexture로 캡처
        var renderTexture = new RenderTexture(512, 512, 24);
        thumbnailCamera.targetTexture = renderTexture;
        thumbnailCamera.gameObject.SetActive(true);
        thumbnailCamera.Render();
        thumbnailCamera.gameObject.SetActive(false);

        RenderTexture.active = renderTexture;
        var texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        texture.Apply();

        RenderTexture.active = null;
        thumbnailCamera.targetTexture = null;

        var bytes = texture.EncodeToJPG(quality: 80);
        Destroy(texture);
        Destroy(renderTexture);

        // Storage에 업로드
        var path = $"{_currentMapCreating.MapId}.jpg";
        await SupabaseManager.Instance.Supabase().Storage
            .From("map-thumbnails")
            .Upload(bytes, path, new Supabase.Storage.FileOptions{Upsert = true});

        return SupabaseManager.Instance.Supabase().Storage
            .From("map-thumbnails")
            .GetPublicUrl(path);
    }
}
