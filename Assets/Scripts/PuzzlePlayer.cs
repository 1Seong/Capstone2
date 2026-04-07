
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TileType
{
    Empty = 'a', Painted, Player, Road,
    PortalIn, PortalOut, PortalInPainted, PortalOutPainted,
    Ghost, GhostPainted,
    Inv, InvPainted,
    Laser, LaserPainted,
    Count
}

public static class TileHelper
{
    public static bool IsPainted(char tile)
    {
        return tile is (char)TileType.Painted or (char)TileType.PortalInPainted or (char)TileType.PortalOutPainted
            or (char)TileType.GhostPainted or (char)TileType.InvPainted or (char)TileType.LaserPainted;
    }

    public static readonly Dictionary<char, char> PaintTable = new()
    {
        {(char)TileType.Player, (char)TileType.Painted}, // 초기화에 사용
        {(char)TileType.Road, (char)TileType.Painted},
        {(char)TileType.PortalIn, (char)TileType.PortalInPainted},
        {(char)TileType.PortalOut, (char)TileType.PortalOutPainted},
        {(char)TileType.Ghost, (char)TileType.Painted},
        {(char)TileType.Inv, (char)TileType.Painted},
        {(char)TileType.Laser, (char)TileType.Painted}
    };
    
    public static readonly Dictionary<char, char> InvTable = new()
    {
        {(char)TileType.Road, (char)TileType.Painted},
        {(char)TileType.Painted, (char)TileType.Road},
        {(char)TileType.PortalIn, (char)TileType.PortalInPainted},
        {(char)TileType.PortalOut, (char)TileType.PortalOutPainted},
        {(char)TileType.PortalInPainted, (char)TileType.PortalIn},
        {(char)TileType.PortalOutPainted, (char)TileType.PortalOut},
        {(char)TileType.Ghost, (char)TileType.GhostPainted},
        {(char)TileType.Inv, (char)TileType.InvPainted},
        {(char)TileType.GhostPainted, (char)TileType.Ghost},
        {(char)TileType.InvPainted, (char)TileType.Inv},
        {(char)TileType.Laser, (char)TileType.LaserPainted},
        {(char)TileType.LaserPainted, (char)TileType.Laser}
    };

    public const int GhostCount = 5;

    public static readonly Vector3Int[] Dirs = {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0),
        new(0, 0, 1),
        new(0, 0, -1)
    };
}

public class PuzzlePlayer : MonoBehaviour
{
    [Header("GamePlay")]
    // ReSharper disable once InconsistentNaming
    private int CubeSize;
    
    [SerializeField] private Transform cubeParent;
    private PuzzleTile[,,] _tiles;

    private char[,,] _map;
    private char[,,] _initialMap;
    private int _roadLeftCount;
    private Dictionary<Vector3Int, Vector3Int> _portalPairDic;

    [SerializeField] private Transform playerModel;
    [SerializeField] private float playerMoveDuration = 0.5f;
    [SerializeField] private Ease playerMoveEase = Ease.OutExpo;
    private bool _isMoving;
    [SerializeField] private TMP_Text ghostText;
    private List<CellPulse> _highlightCells = new();
    private int _currentGhostCount;
    private int CurrentGhostCount
    {
        get => _currentGhostCount;
        set
        {
            ghostText.text = value.ToString();
            if (_currentGhostCount > 0 && value == 0) // 카운트가 꺼지는 경우
            {
                playerModel.GetComponent<MeshRenderer>().materials[0].DOFade(1f, 0.5f)
                    .SetEase(Ease.InOutSine).OnComplete(()=>ghostText.gameObject.SetActive(false));
            }
            else if (_currentGhostCount == 0 && value > 0) // 켜지는 경우
            {
                ghostText.gameObject.SetActive(true);
                playerModel.GetComponent<MeshRenderer>().materials[0].DOFade(0.45f, 0.5f)
                    .SetEase(Ease.InOutSine);
            }
            _currentGhostCount = value;
        }
    }
    private bool _hasLaser;

    private bool HasLaser
    {
        get => _hasLaser;
        set
        {
            if (!_hasLaser && value) // 켜짐
            {
                _highlightCells.Clear();
                var p = playerModel.position;
                int x = (int)p.x, y = (int)p.y, z = (int)p.z;
                bool d1 = true, d2 = true, d3 = true, d4 = true, d5 = true, d6 = true;
                var depth = 1;
                var offset = 0.06f;
                while (d1 || d2 || d3 || d4 || d5 || d6)
                {
                    if (d1)
                    {
                        var t = x + depth;
                        if (CannotMove(t, y, z))
                            d1 = false;
                        else
                        {
                            var cell = _tiles[t, y, z].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    if (d2)
                    {
                        var t = x - depth;
                        if (CannotMove(t, y, z))
                            d2 = false;
                        else
                        {
                            var cell = _tiles[t, y, z].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    if (d3)
                    {
                        var t = y + depth;
                        if (CannotMove(x, t, z))
                            d3 = false;
                        else
                        {
                            var cell = _tiles[x, t, z].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    if (d4)
                    {
                        var t = y - depth;
                        if (CannotMove(x, t, z))
                            d4 = false;
                        else
                        {
                            var cell = _tiles[x, t, z].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    if (d5)
                    {
                        var t = z + depth;
                        if (CannotMove(x, y, t))
                            d5 = false;
                        else
                        {
                            var cell = _tiles[x, y, t].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    if (d6)
                    {
                        var t = z - depth;
                        if (CannotMove(x, y, t))
                            d6 = false;
                        else
                        {
                            var cell = _tiles[x, y, t].GetComponent<CellPulse>();
                            _highlightCells.Add(cell);
                            cell.StartPulse((depth-1) * offset);
                        }
                    }
                    ++depth;
                }
            }
            else if (_hasLaser && !value) // 꺼짐
            {
                foreach (var cell in _highlightCells)
                    cell.StopPulse();
            }
            _hasLaser = value;
        }
    }

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform pivot;
    [SerializeField] private float camMoveDuration = 0.5f;
    [SerializeField] private Ease camMoveEase = Ease.OutExpo;
    [SerializeField] private Vector3 currentUp = Vector3.up;
    [SerializeField] private Vector3 currentRight = Vector3.right;
    [SerializeField] private Vector3 currentLeft = Vector3.back;
    private Vector3 _initialCameraPosition;
    private Quaternion _initialCameraRotation;

    [Header("System")] 
    [SerializeField] private InputActionReference camera1Action;
    [SerializeField] private InputActionReference camera2Action;
    [SerializeField] private InputActionReference camera3Action;
    [SerializeField] private InputActionReference camera4Action;
    [SerializeField] private InputActionReference camera5Action;
    [SerializeField] private InputActionReference camera6Action;
    [SerializeField] private InputActionReference qAction;
    [SerializeField] private InputActionReference dAction;
    [SerializeField] private InputActionReference aAction;
    [SerializeField] private InputActionReference eAction;
    [SerializeField] private InputActionReference wAction;
    [SerializeField] private InputActionReference sAction;
    [SerializeField] private InputActionReference undoAction;
    [SerializeField] private InputActionReference resetAction;
    private bool _isTesting;
    private bool _isCleared;
    private Stack<Vector3Int> _answer;
    private int _moves = 0;


    private struct MapState
    {
        public char[,,] Map;
        public Vector3 PlayerPos;
        public int RoadLeftCount;
        public int CurrentGhostCount;
        public bool HasLaser;
    }

    private Stack<MapState> _undoStack;

    private void Awake()
    {
        _initialCameraPosition = pivot.position;
        _initialCameraRotation = pivot.rotation;
        
        _tiles = new PuzzleTile[CubeSize, CubeSize, CubeSize];

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

    private void OnEnable()
    {
        camera1Action.action.performed += CameraRotation1InputWrapper;
        camera2Action.action.performed += CameraRotation2InputWrapper;
        camera3Action.action.performed += CameraRotation3InputWrapper;
        camera4Action.action.performed += CameraRotation4InputWrapper;
        camera5Action.action.performed += CameraRotation5InputWrapper;
        camera6Action.action.performed += CameraRotation6InputWrapper;
        undoAction.action.performed += UndoInputWrapper;
        resetAction.action.performed += RestartInputWrapper;
        camera1Action.action.Enable();
        camera2Action.action.Enable();
        camera3Action.action.Enable();
        camera4Action.action.Enable();
        camera5Action.action.Enable();
        camera6Action.action.Enable();
        qAction.action.Enable();
        dAction.action.Enable();
        aAction.action.Enable();
        eAction.action.Enable();
        wAction.action.Enable();
        sAction.action.Enable();
        undoAction.action.Enable();
        resetAction.action.Enable();
        
        InitGame();
        CheckGameCleared();
    }

    private void OnDisable()
    {
        camera1Action.action.performed -= CameraRotation1InputWrapper;
        camera2Action.action.performed -= CameraRotation2InputWrapper;
        camera3Action.action.performed -= CameraRotation3InputWrapper;
        camera4Action.action.performed -= CameraRotation4InputWrapper;
        camera5Action.action.performed -= CameraRotation5InputWrapper;
        camera6Action.action.performed -= CameraRotation6InputWrapper;
        undoAction.action.performed -= UndoInputWrapper;
        resetAction.action.performed -= RestartInputWrapper;
        camera1Action.action.Disable();
        camera2Action.action.Disable();
        camera3Action.action.Disable();
        camera4Action.action.Disable();
        camera5Action.action.Disable();
        camera6Action.action.Disable();
        qAction.action.Disable();
        dAction.action.Disable();
        aAction.action.Disable();
        eAction.action.Disable();
        wAction.action.Disable();
        sAction.action.Disable();
        undoAction.action.Disable();
        resetAction.action.Disable();
    }

    #region GameSystem

    private void InitGame()
    {
        // 렌더링을 하면서
        // 플레이어 시작점 파악
        // 남은 블럭 개수 파악
        _isCleared = false;
        _roadLeftCount = 0;
        _map = (char[,,])_initialMap.Clone();

        for (var i = 0; i < CubeSize; ++i)
        for (var j = 0; j < CubeSize; ++j)
        for (var k = 0; k < CubeSize; ++k)
        {
            var c = _map[i, j, k];
            _tiles[i, j, k].SimpleRender(c);
            
            switch (c)
            {
                case (char)TileType.Empty:
                    continue;
                case (char)TileType.Player:
                    playerModel.position = new Vector3(i, j, k);
                    PaintWithRender(i, j, k, true);
                    break;
                case (char)TileType.Road:
                case (char) TileType.PortalIn:
                case (char) TileType.PortalOut:
                case (char) TileType.Ghost:
                case (char) TileType.Inv:
                case (char) TileType.Laser:
                    ++_roadLeftCount;
                    break;
            }
        }
        
        if(_answer is not null)
            _answer.Clear();
        else
            _answer = new();
        if(_undoStack is not null)
            _undoStack.Clear();
        else
            _undoStack = new();
        pivot.position = _initialCameraPosition;
        pivot.rotation = _initialCameraRotation;
        currentUp = Vector3.up;
        currentRight = Vector3.right;
        currentLeft = Vector3.back;
        _moves = 0;
        CurrentGhostCount = 0;
        HasLaser = false;
    }

    public void SetMapData(char[,,] map, Dictionary<Vector3Int, Vector3Int> portalPairDic = null, bool isTest = false)
    {
        _map = map;
        _portalPairDic =  portalPairDic;
        _initialMap = (char[,,])map.Clone();
        _isTesting = isTest;
        CubeSize = map.GetLength(0);
    }

    // 맵 전체 렌더링
    // Undo 할때 사용
    private void Render()
    {
        for (var i = 0; i < CubeSize; ++i)
        for (var j = 0; j < CubeSize; ++j)
        for (var k = 0; k < CubeSize; ++k)
        {
            var c = _map[i, j, k];
            _tiles[i, j, k].SimpleRender(c);
        }
    }

    private void CheckGameCleared()
    {
        if (_roadLeftCount != 0) return;

        _isCleared = true;
        
        if (_isTesting)
        {
            MapEditor.Instance.SetValidated(true, _answer);
            GameManager.Instance.GameClearedTest(_moves);
        }
        else
            GameManager.Instance.GameCleared(_moves);
    }

    #endregion

    #region Player

    private bool CannotMove(int x, int y, int z)
    {
        return x == -1 || x == CubeSize || y == -1 || y == CubeSize || z == -1 || z == CubeSize
               || _map[x, y, z] == (char)TileType.Empty
               || _currentGhostCount == 0 && TileHelper.IsPainted(_map[x, y, z]);
    }

    // 인덱스 계산
    private async UniTask MovePlayer(Vector3 dir)
    {
        var pos = playerModel.position;
        var nPos = pos + dir;
        var nLayer = (int)nPos.x;
        var nRow = (int)nPos.y;
        var nCol = (int)nPos.z;
        
        if (CannotMove(nLayer, nRow, nCol))
        {
            // 이동 불가 애니메이션
            await playerModel.DOShakePosition(0.2f, 0.2f, 20).AsyncWaitForCompletion().AsUniTask();
            return;
        }
        // ---- 전진 ----
        var before = _map[nLayer, nRow, nCol];
        SaveUndoState();
        _answer.Push(new Vector3Int(nLayer, nRow, nCol));
        ++_moves;
        if (_currentGhostCount > 0)
            --CurrentGhostCount;

        if (_hasLaser)
        {
            HasLaser = false;
            do
            {
                --_roadLeftCount;
                await PaintWithRender((int)nPos.x, (int)nPos.y, (int)nPos.z, true);
                await UniTask.WaitForSeconds(0.11f);
                nPos += dir;
            } while (!CannotMove((int)nPos.x, (int)nPos.y, (int)nPos.z));
        }
        else
        {
            // 플레이어 움직임 애니메이션 기다리고 - 회전 시 수행 x
            await playerModel.DOMove(nPos, playerMoveDuration).SetEase(playerMoveEase).AsyncWaitForCompletion()
                .AsUniTask();
            // 색칠 및 아이템 사용
            if (_currentGhostCount == 0)
            {
                if (!TileHelper.IsPainted(before))
                    --_roadLeftCount;
                await PaintWithRender(nLayer, nRow, nCol, true); // 일단 simpleRender 사용
                await ApplyItem(nLayer, nRow, nCol, before); // 아이템 사용
            }
        }

        CheckGameCleared();
    }

    private async UniTask ApplyItem(int x, int y, int z, char before)
    {
        switch (before)
        {
            case (char)TileType.PortalIn:
                await playerModel.DOScale(Vector3.zero, playerMoveDuration).SetEase(Ease.InOutSine)
                    .AsyncWaitForCompletion().AsUniTask();
                playerModel.position = _portalPairDic[new Vector3Int(x, y, z)];
                _answer.Push(_portalPairDic[new Vector3Int(x, y, z)]);
                await playerModel.DOScale(Vector3.one, playerMoveDuration).SetEase(Ease.InOutSine)
                    .AsyncWaitForCompletion().AsUniTask();
                await PaintWithRender(x, y, z, true);
                --_roadLeftCount;
                break;
            
            case (char)TileType.Ghost:
                CurrentGhostCount = TileHelper.GhostCount;
                break;
            
            case (char)TileType.Inv:
                _roadLeftCount = 0;
                for (var i = 0; i != CubeSize; ++i)
                for (var j = 0; j != CubeSize; ++j)
                for (var k = 0; k != CubeSize; ++k)
                {
                    var beforeInv = _map[i, j, k];
                    if(beforeInv == (char)TileType.Empty || i == x && j == y && k == z)
                        continue;
                    var afterInv = TileHelper.InvTable[beforeInv];
                    _map[i, j, k] = afterInv;
                    if (!TileHelper.IsPainted(afterInv))
                        ++_roadLeftCount;
                    _tiles[i, j, k].SimpleRender(afterInv); // TODO: 나중에 Render로 변경
                }
                break;
            
            case (char)TileType.Laser:
                HasLaser = true;
                break;
        }
    }

    // 움직임이 확정된 뒤 저장
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (char[,,])_map.Clone();
        _undoStack.Push(new MapState()
        {
            Map = snapshot, PlayerPos = playerModel.position, RoadLeftCount = _roadLeftCount, CurrentGhostCount = _currentGhostCount,
            HasLaser = _hasLaser
        });
        var pos = playerModel.position;
    }

    public void UndoInputWrapper(InputAction.CallbackContext _) => Undo();
    public void Undo(bool doRender = true)
    {
        if (_undoStack.Count == 0 || _isMoving || _isCleared) return;
        if (!GameManager.Instance.isPlaying) return;

        var s = _undoStack.Pop();
        _map = s.Map;
        playerModel.position = s.PlayerPos;
        _roadLeftCount = s.RoadLeftCount;
        CurrentGhostCount = s.CurrentGhostCount;

        _answer.Pop();
        if (_undoStack.Count < _answer.Count) 
            _answer.Pop();
        --_moves;

        if (doRender)
            Render();
        
        HasLaser = s.HasLaser;
    }

    // 이동 및 아이템 사용할때 사용 -> 즉 타일이 색칠
    public async UniTask PaintWithRender(int layer, int row, int col,
        bool useSimpleRender = false, bool wait = true)
    {
        var before = _map[layer, row, col];
        if (before == (char)TileType.Empty || TileHelper.IsPainted(before)) return;
        
        var tile = TileHelper.PaintTable[before];
        _map[layer, row, col] = tile;
        
        if (useSimpleRender)
        {
            _tiles[layer, row, col].SimpleRender(tile);
            return;
        }

        if (wait)
            await _tiles[layer, row, col].Render(tile);
        else
            _ = _tiles[layer, row, col].Render(tile, false);
    }

    #endregion
    
    #region PlayerControl

    private async void Update()
    {
        if (qAction.action.IsPressed())
        {
            await MovePlayerQ();
        }
        if (dAction.action.IsPressed())
        {
            await MovePlayerD();
        }
        if (aAction.action.IsPressed())
        {
            await MovePlayerA();
        }
        if (eAction.action.IsPressed())
        {
            await MovePlayerE();
        }
        if (wAction.action.IsPressed())
        {
            await MovePlayerW();
        }
        if (sAction.action.IsPressed())
        {
            await MovePlayerS();
        }
    }

    public async UniTask MovePlayerQ()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(-currentRight);
        }
        finally
        {
            _isMoving = false;
        }
    }
    
    public async UniTask MovePlayerD()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(currentRight);
        }
        finally
        {
            _isMoving = false;
        }
    }
    
    public async UniTask MovePlayerA()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(currentLeft);
        }
        finally
        {
            _isMoving = false;
        }
    }
    
    public async UniTask MovePlayerE()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(-currentLeft);
        }
        finally
        {
            _isMoving = false;
        }
    }
    
    public async UniTask MovePlayerW()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(currentUp);
        }
        finally
        {
            _isMoving = false;
        }
    }
    
    public async UniTask MovePlayerS()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        try
        {
            await MovePlayer(-currentUp);
        }
        finally
        {
            _isMoving = false;
        }
    }

    public void RestartInputWrapper(InputAction.CallbackContext _) => InitGame();
    
    #endregion

    #region CameraMove

    private void TransitionTo(Vector3 corner, Vector3 up)
    {
        var target = Quaternion.LookRotation(-corner, up);
        // Pivot 회전만 바꾸면 카메라는 자동으로 따라옴
        pivot.DORotateQuaternion(target, camMoveDuration).SetEase(camMoveEase).OnComplete(() => _isMoving = false);
    }

    public void CameraRotation1InputWrapper(InputAction.CallbackContext _) => CameraRotation1();
    public void CameraRotation2InputWrapper(InputAction.CallbackContext _) => CameraRotation2();
    public void CameraRotation3InputWrapper(InputAction.CallbackContext _) => CameraRotation3();
    public void CameraRotation4InputWrapper(InputAction.CallbackContext _) => CameraRotation4();
    public void CameraRotation5InputWrapper(InputAction.CallbackContext _) => CameraRotation5();
    public void CameraRotation6InputWrapper(InputAction.CallbackContext _) => CameraRotation6();
    public void CameraRotation1()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;

        var left = currentLeft;
        var right = currentRight;

        currentLeft = -left;
        currentRight = -right;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    public void CameraRotation2()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        
        var left = currentLeft;
        var right = currentRight;

        currentLeft = right;
        currentRight = -left;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    public void CameraRotation3()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        
        var left = currentLeft;
        var right = currentRight;
        var up = currentUp;

        currentLeft = -up;
        currentRight = -left;
        currentUp = right;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    public void CameraRotation4()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        
        var left = currentLeft;
        var right = currentRight;
        var up = currentUp;

        currentLeft = right;
        currentRight = left;
        currentUp = -up;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    public void CameraRotation5()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        
        var left = currentLeft;
        var right = currentRight;
        var up = currentUp;

        currentLeft = -right;
        currentRight = -up;
        currentUp = left;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    public void CameraRotation6()
    {
        if (_isMoving || _isCleared || !GameManager.Instance.isPlaying) return;
        _isMoving = true;
        
        var left = currentLeft;
        var right = currentRight;

        currentLeft = -right;
        currentRight = left;
        
        TransitionTo(currentLeft + currentRight + currentUp, currentUp);
    }

    #endregion
}