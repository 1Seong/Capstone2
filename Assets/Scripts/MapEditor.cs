using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MapEditor : MonoBehaviour
{
    [SerializeField] private int cubeSize = 10;
    private char[,,] _map;
    public static MapEditor Instance;

    [SerializeField] private char currentTile = (char)TileType.Road;
    public char CurrentTile() => currentTile;
    
    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform  cubeParent;
    [SerializeField] private GameObject tileIndicatorParent;
    [SerializeField] private GameObject rightSideButtonsParent;
    [SerializeField] private Button exportButton;
    
    private PuzzleTile[,,] _tiles;

    private bool _isValidated;
    
    [SerializeField] private Button showAnswerButton;
    [SerializeField] private GameObject stopShowAnswerButton;
    [SerializeField] private Transform ghostPlayer;
    [SerializeField] private float showAnswerTimePerPos = 0.5f;
    private CancellationTokenSource _showAnswerCts;
    
    private EditTile _hoveredTile;

    private Stack<Vector3Int> _answer;
    
    // TODO: 아이템별 material 지정
    
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

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(gameObject);
        else
        {
            Instance = this;
        }

        showAnswerButton.onClick.AddListener(() => ShowAnswer().Forget());
        
        InitEditor();
    }

    private void InitEditor()
    {
        SetValidated(false);
        _map = new char[cubeSize, cubeSize, cubeSize];
        
        for (int i = 0; i != cubeSize; ++i)
        for (int j = 0; j != cubeSize; ++j)
        for (int k = 0; k != cubeSize; ++k)
            _map[i, j, k] = (char)TileType.Empty;
        
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
    }

    public void SetTile(int layer, int row, int col)
    {
        SaveUndoState();
        //Debug.Log(layer + " " + row + " " + col);
        SetValidated(false);
        _map[layer, row, col] = currentTile;

        if (currentTile == (char)TileType.Player)
        {
            if (!playerModel.gameObject.activeSelf)
            {
                playerModel.gameObject.SetActive(true);
            }
            else
            {
                var pos = playerModel.position;
                _map[(int)pos.x, (int)pos.y, (int)pos.z] = (char)TileType.Empty;
            }
            
            playerModel.position = new Vector3(layer, row, col);
            _tiles[layer, row, col].SimpleRender((char)TileType.Empty);
            return;
        }
        if (currentTile == (char)TileType.Empty)
        {
            if(playerModel.position == new Vector3(layer, row, col))
            {
                playerModel.gameObject.SetActive(false);
            }
        }
        _tiles[layer, row, col].SimpleRender(currentTile);
    }
    
    #region Button

    public void PlayTest()
    {
        if (!playerModel.gameObject.activeSelf)
        {
            PopUpManager.Instance.Show("시작 위치를 찾을 수 없습니다.");
            return;
        }
        
        gameObject.SetActive(false);
        // 로딩 오래 걸리면 씬 전환 효과 넣기
        GameManager.Instance.PlayGame((char[,,])_map.Clone(), true);
    }

    public void AutoTest()
    {
        var res = PuzzleSolver.Solve(_map);

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

    public void SetCurrentTileToRoad()
    {
        currentTile = (char)TileType.Road;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToEraser()
    {
        currentTile = (char)TileType.Empty;
        // 여기에서 현재 mat 지정
    }
    
    public void SetCurrentTileToPlayer()
    {
        currentTile = (char)TileType.Player;
        // 여기에서 현재 mat 지정
    }
    
    #endregion

    public async UniTaskVoid ShowAnswer()
    {
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
        foreach(var i in _answer.Reverse())
        {
            ghostPlayer.position = i;
            await UniTask.WaitForSeconds(showAnswerTimePerPos, cancellationToken: ct);
        }
    }

    public void EndShowAnswer()
    {
        _showAnswerCts?.Cancel();
        _showAnswerCts?.Dispose();
        _showAnswerCts = null;
    
        ghostPlayer.gameObject.SetActive(false);
        tileIndicatorParent.SetActive(true);
        rightSideButtonsParent.SetActive(true);
        stopShowAnswerButton.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Undo();
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
        _answer = answer;
    }
}
