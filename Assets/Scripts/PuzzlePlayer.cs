
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TileType
{
    Empty = 'a', Painted = 'b', Player = 'c', Road = 'd',
    PortalIn = 'e', PortalOut = 'f', PortalInPainted = 'g', PortalOutPainted = 'h',
    Count
}

public static class TileHelper
{
    public static bool IsPainted(char tile)
    {
        return tile is (char)TileType.Painted or (char)TileType.PortalInPainted or (char)TileType.PortalOutPainted;
    }

    public static readonly Dictionary<char, char> PaintTable = new()
    {
        {(char)TileType.Player, (char)TileType.Painted}, // 초기화에 사용
        {(char)TileType.Painted, (char)TileType.Road},
        {(char)TileType.Road, (char)TileType.Painted},
        {(char)TileType.PortalInPainted, (char)TileType.PortalIn},
        {(char)TileType.PortalIn, (char)TileType.PortalInPainted},
        {(char)TileType.PortalOutPainted, (char)TileType.PortalOut},
        {(char)TileType.PortalOut, (char)TileType.PortalOutPainted}
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
    private int _roadLeftCount; // 캐시데이터 - 연동 주의
    private Dictionary<Vector3Int, Vector3Int> _portalPairDic;

    [SerializeField] private Transform playerModel;
    [SerializeField] private float playerMoveDuration = 0.5f;
    [SerializeField] private Ease playerMoveEase = Ease.OutExpo;

    private bool _isMoving;

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
    private Stopwatch _stopwatch = new();
    private int _moves = 0;


    private struct MapState
    {
        public char[,,] Map;
        public Vector3 PlayerPos;
        public int RoadLeftCount;
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
        _stopwatch.Restart();
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
        _stopwatch.Stop();
        
        if (_isTesting)
        {
            MapEditor.Instance.SetValidated(true, _answer);
            GameManager.Instance.GameClearedTest(_stopwatch.Elapsed, _moves);
        }
        else
            GameManager.Instance.GameCleared(_stopwatch.Elapsed, _moves);
    }

    #endregion

    #region Player

    // 인덱스 계산
    private async UniTask MovePlayer(Vector3 dir, bool doRender = true)
    {
        var pos = playerModel.position;
        var nPos = pos + dir;
        var nLayer = (int)nPos.x;
        var nRow = (int)nPos.y;
        var nCol = (int)nPos.z;
        
        if ((int)nPos.x == -1 || (int)nPos.x == CubeSize || (int)nPos.y == -1 || (int)nPos.y == CubeSize || (int)nPos.z == -1 ||
            (int)nPos.z == CubeSize
            || _map[nLayer, nRow, nCol] == (char)TileType.Empty ||TileHelper.IsPainted(_map[nLayer, nRow, nCol]))// TODO : 통과 아이템 효과
        {
            // 이동 불가 애니메이션
            if (doRender)
            {
                await playerModel.DOShakePosition(0.2f, 0.2f, 20).AsyncWaitForCompletion().AsUniTask();
            }
            return;
        }
        var before = _map[nLayer, nRow, nCol];

        SaveUndoState();
        _answer.Push(new Vector3Int(nLayer, nRow, nCol));
        ++_moves;

        if (!TileHelper.IsPainted(before))
            --_roadLeftCount;

        if (doRender)
        {
            // 플레이어 움직임 애니메이션 기다리고 - 회전 시 수행 x
            await playerModel.DOMove(nPos, playerMoveDuration).SetEase(playerMoveEase).AsyncWaitForCompletion().AsUniTask();
            // 새 타일로 이동 및 아이템 사용
            await PaintWithRender(nLayer, nRow, nCol, true); // 일단 simpleRender 사용
        }
        else
        {
            if (before == (char)TileType.Empty || TileHelper.IsPainted(before)) return;
        
            var after = TileHelper.PaintTable[before];
            _map[nLayer, nRow, nCol] = after;
        }
        
        await ApplyItem(nLayer, nRow, nCol, before); // 아이템 사용
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
                return;
        }
    }

    // 움직임이 확정된 뒤 저장
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (char[,,])_map.Clone();
        _undoStack.Push(new MapState()
            { Map = snapshot, PlayerPos = playerModel.position, RoadLeftCount = _roadLeftCount });
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
        _answer.Pop();
        if (_undoStack.Count < _answer.Count) 
            _answer.Pop();
        --_moves;

        if (doRender)
            Render();
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