using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class PuzzlePlayer : MonoBehaviour
{
    // 게임판
    private const int Layers = 6, Rows = 10, Cols = 10;
    private readonly int[] Up = new[] { 0, 1 };
    private readonly int[] Down = new[] { 0, -1 };
    private readonly int[] Left = new[] { -1, 0 };
    private readonly int[] Right = new[] { 1, 0 };
    
    private enum TileType{ Empty, Painted, Player, Road }
    
    [SerializeField] private GameObject plane0; // 8 * 9
    [SerializeField] private GameObject plane1;
    [SerializeField] private GameObject plane2;
    [SerializeField] private GameObject plane3;
    [SerializeField] private GameObject plane4; // 10 * 10
    [SerializeField] private GameObject plane5;
    private PuzzleTile[][] _planeTiles = new PuzzleTile[6][];

    private string[,] _map = new string[Layers, Rows]; // 6 10 10

    private int[] _playerPos = new int[3];

    private int _roadLeftCount; // 캐시데이터 - 연동 주의

    [SerializeField] private Transform playerModel;
    [SerializeField] private float playerMoveDuration = 0.5f;
    [SerializeField] private Ease  playerMoveEase = Ease.OutExpo;

    private bool _isMoving = false;
    // 카메라
    private const int Eyes = 8;
    
    [SerializeField] private Camera _camera;
    private int[,,] _cameraPos = new int[Layers, Rows, Cols];
    
    // 시스템
    private Action _onClearAction;
    private bool _isCleared = false;
    
    private struct MapState
    {
        public string[,] Map;
        public int[] PlayerPos;
        public int RoadLeftCount;
    }
    
    private Stack<MapState> _undoStack = new Stack<MapState>();
    
    
    private void Awake()
    {
        _planeTiles[0] = plane0.GetComponentsInChildren<PuzzleTile>();
        _planeTiles[1] = plane1.GetComponentsInChildren<PuzzleTile>();
        _planeTiles[2] = plane2.GetComponentsInChildren<PuzzleTile>();
        _planeTiles[3] = plane3.GetComponentsInChildren<PuzzleTile>();
        _planeTiles[4] = plane4.GetComponentsInChildren<PuzzleTile>();
        _planeTiles[5] = plane5.GetComponentsInChildren<PuzzleTile>();
    }

    private void OnEnable()
    {
        InitGame();
    }

    private void OnDisable()
    {
        
    }

    #region GameSystem
    private void InitGame()
    {
        // 렌더링을 하면서
        // 플레이어 시작점 파악
        // 남은 블럭 개수 파악
        _isCleared = false;
        _roadLeftCount = 0;
        
        for (var i = 0; i < Layers; ++i)
        {
            for (var j = 0; j < _planeTiles[i].Length; ++j)
            {
                var cols = _planeTiles[i].Length == 72 ? 9 : 10;
                var tile = _map[i, j / cols][j % cols];

                if (tile is '3') ++_roadLeftCount;
                else if (tile is '2')
                {
                    _playerPos = new[] { i, j / cols, j % cols };
                    playerModel.position = _planeTiles[i][j].transform.position;
                }

                _planeTiles[i][j].SimpleRender(tile);
            }
        }
    }
    
    public void SetMapData(string[,] map)
    {
        _map = map;
    }

    public void SetMapData(string map)
    {
        _map = StringHelper.Decode(map);
    }

    // 맵 전체 렌더링
    // Undo 할때 사용
    private void Render()
    {
        for (var i = 0; i < Layers; ++i)
        {
            for (var j = 0; j < _planeTiles[i].Length; ++j)
            {
                var cols = _planeTiles[i].Length == 72 ? 9 : 10;
                _planeTiles[i][j].SimpleRender(_map[i, j / cols][j % cols]);
            }
        }
    }

    private void CheckGameCleared()
    {
        if (_roadLeftCount != 0) return;

        _isCleared = true;
        _onClearAction?.Invoke();
    }
    
    #endregion
    
    #region Player
    // 인덱스 계산
    private async Task MovePlayer(int[] dir)
    {
        var tasks = new List<Task>();
        var itemExist = false;
        char itemId;
        
        var nPos = GetNextPos(_playerPos[0],  _playerPos[1], _playerPos[2], dir);
        var nLayer = nPos[0];
        var nRow = nPos[1];
        var nCol = nPos[2];

        switch (_map[nLayer, nRow][nCol])
        {
            // TODO: 벽 통과 아이템 케이스 해줘야됨
            case '0' or '1':
                // 막힘 애니메이션

                return;
            case '4' or '5':
                itemExist = true;
                itemId = _map[nLayer, nRow][nCol];
                break;
        }
        
        tasks.Add(SetTileWithRender(_playerPos[0],  _playerPos[1], _playerPos[2], '1')); // paint

        if (_map[nLayer, nRow][nCol] is not '1')
            --_roadLeftCount;
        tasks.Add(SetTileWithRender(nLayer, nRow, nCol, '2')); // set player position
        _playerPos = nPos; // update cache
        SaveUndoState();

        // 플레이어 움직임 애니메이션 기다리고
        var cols = _planeTiles[nLayer].Length == 72 ? 9 : 10;
        var targetPos = _planeTiles[nLayer][nRow * cols + nCol].transform.position;
        var t = playerModel.DOMove(targetPos, playerMoveDuration).SetEase(playerMoveEase); // 얘를 tasks 목록에 집어넣을 수 없어서 따로 대기
        await Task.WhenAll(tasks);
        await t;
        
        // 이 자리에 아이템이 있었으면 발동
        // 아이템 애니메이션까지 끝나면
        if (itemExist)
        {
            
        }

        CheckGameCleared();
    }
    
    // 움직임이 확정된 뒤 저장
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (string[,])_map.Clone();
        _undoStack.Push(new MapState(){Map = snapshot,  PlayerPos = _playerPos, RoadLeftCount = _roadLeftCount});
    }
    
    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var s = _undoStack.Pop();
        _map = s.Map;
        _playerPos = s.PlayerPos;
        _roadLeftCount = s.RoadLeftCount;
        
        Render();
    }
    
    public void SetTile(int layer, int row, int col, char tile)
    {
        var rowChars = _map[layer, row].ToCharArray();
        rowChars[col] = tile;
        _map[layer, row] = new string(rowChars);
    }
    
    // 이동 및 아이템 사용할때 사용 -> 즉 타일이 '1'(페인트) 또는 '2'(플레이어)로만 바뀜
    public async Task SetTileWithRender(int layer, int row, int col, char tile, bool useSimpleRender = false, bool wait = true)
    {
        var alreadyPainted = false;
        if (_map[layer, row][col] is '1') alreadyPainted = true;
        
        var rowChars = _map[layer, row].ToCharArray();
        rowChars[col] = tile;
        _map[layer, row] = new string(rowChars);

        if (alreadyPainted) return;
        
        var cols = _planeTiles[layer].Length == 72 ? 9 : 10;

        if (useSimpleRender)
        {
            _planeTiles[layer][row * cols + col].SimpleRender(tile);
            return;
        }
        
        if(wait)
            await _planeTiles[layer][row * cols + col].Render(tile);
        else
            _ = _planeTiles[layer][row * cols + col].Render(tile, false);
    }
    
    private void ApplyItem(int id)
    {
        
    }
    
    #endregion
      
    #region CameraMove
    
    #endregion

    private void Update()
    {

        if (_isCleared) return;
        
        // TODO : 행동이 완료될 때까지 조작 막기 (애니메이션 때문), New Input System으로 분리?
        // 플레이어 이동 또는 Undo 가능
        // 키 입력을 적절한 방향으로 변환
        // 방향으로 이동 시도
        // 렌더링
        // 아이템이 있으면 아이템 사용
        //렌더링
        //undo 스택 저장
        // 클리어 검사

        // 카메라 조작
    }
    
    // Helpers
    private int[] GetNextPos(int layer, int row, int col, int[] dir)
    {
        var nLayer = layer;
        var nRow = row + dir[0];
        var nCol = col + dir[1];
        
        if(nRow is >= 0 and < 7 && nCol is >= 0 and < 8)
            return new []{nRow, nCol, nLayer};
        
        switch (nLayer)
        {
            case 0:
                if (nCol == -1)
                {
                    nLayer = 3;
                    nCol = 8;
                }
                else if (nCol == 9)
                {
                    nLayer = 1;
                    nCol = 0;
                }
                else if (nRow == -1)
                {
                    nLayer = 4;
                    nRow = 9;
                    ++nCol;
                }
                else if (nRow == 8)
                {
                    nLayer = 5;
                    nRow = 0;
                    ++nCol;
                }
                break;
            case 1:
                if (nCol == -1)
                {
                    nLayer = 0;
                    nCol = 8;
                }
                else if (nCol == 9)
                {
                    nLayer = 2;
                    nCol = 0;
                }
                else if (nRow == -1)
                {
                    nLayer = 4;
                    nRow = 8 - nCol;
                    nCol = 9;
                }
                else if (nRow == 8)
                {
                    nLayer = 5;
                    nRow = nCol + 1;
                    nCol = 9;
                }
                break;
            case 2:
                if (nCol == -1)
                {
                    nLayer = 1;
                    nCol = 8;
                }
                else if (nCol == 9)
                {
                    nLayer = 3;
                    nCol = 0;
                }
                else if (nRow == -1)
                {
                    nLayer = 4;
                    nRow = 0;
                    nCol = 8 - nCol;
                }
                else if (nRow == 8)
                {
                    nLayer = 4;
                    nRow = 9;
                    nCol = 8 -  nCol;
                }
                break;
            case 3:
                if (nCol == -1)
                {
                    nLayer = 2;
                    nCol = 8;
                }
                else if (nCol == 9)
                {
                    nLayer = 0;
                    nCol = 0;
                }
                else if (nRow == -1)
                {
                    nLayer = 4;
                    nRow = nCol + 1;
                    nCol = 0;
                }
                else if (nRow == 8)
                {
                    nLayer = 5;
                    nRow = 8 - nCol;
                    nCol = 0;
                }
                break;
            case 4:
                if (nCol == -1)
                {
                    if (nRow == 0)
                    {
                        nLayer = 2;
                        nRow = 0;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 3;
                        nCol = nRow - 1;
                        nRow = 0;
                    }
                }
                else if (nCol == 10)
                {
                    if (nRow == 9)
                    {
                        nLayer = 0;
                        nRow = 0;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 1;
                        nCol = 8 - nRow;
                        nRow = 0;
                    }
                }
                else if (nRow == -1)
                {
                    if (nCol == 9)
                    {
                        nLayer = 1;
                        nRow = 0;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 2;
                        nRow = 0;
                        nCol = 8 - nCol;
                    }
                }
                else if (nRow == 10)
                {
                    if (nCol == 0)
                    {
                        nLayer = 3;
                        nRow = 0;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 0;
                        nRow = 0;
                        --nCol;
                    }
                }
                break;
            case 5:
                if (nCol == -1)
                {
                    if (nRow == 9)
                    {
                        nLayer = 2;
                        nRow = 7;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 3;
                        nCol = 8 - nRow;
                        nRow = 7;
                    }
                }
                else if (nCol == 10)
                {
                    if (nRow == 0)
                    {
                        nLayer = 0;
                        nRow = 7;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 1;
                        nCol = nRow - 1;
                        nRow = 7;
                    }
                }
                else if (nRow == -1)
                {
                    if (nCol == 0)
                    {
                        nLayer = 3;
                        nRow = 7;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 0;
                        --nCol;
                        nRow = 7;
                    }
                }
                else if (nRow == 10)
                {
                    if (nCol == 9)
                    {
                        nLayer = 1;
                        nRow = 7;
                        nCol = 8;
                    }
                    else
                    {
                        nLayer = 2;
                        nCol = 8 - nCol;
                        nRow = 7;
                    }
                }
                break;
            default:
                break;
        }
        
        return new []{nRow, nCol, nLayer};
    }
}
