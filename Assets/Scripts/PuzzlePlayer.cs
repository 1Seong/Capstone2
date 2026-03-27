using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public enum TileType
{
    Empty = '0',
    Painted = '1',
    Player = '2',
    Road = '3'
}

public class PuzzlePlayer : MonoBehaviour
{
    // 게임판
    // ReSharper disable once InconsistentNaming
    private int CubeSize;

    [SerializeField] private Transform cubeParent;
    private PuzzleTile[,,] _tiles;

    private char[,,] _map;
    private int _roadLeftCount; // 캐시데이터 - 연동 주의

    [SerializeField] private Transform playerModel;
    [SerializeField] private float playerMoveDuration = 0.5f;
    [SerializeField] private Ease playerMoveEase = Ease.OutExpo;

    private bool _isMoving;

    // 카메라
    [SerializeField] private Vector3[] cameraPos;
    [SerializeField] private Vector3[] cameraRot;

    [SerializeField] private Camera cam;
    [SerializeField] private int camIndex;
    [SerializeField] private float camMoveDuration = 0.5f;
    [SerializeField] private Ease camMoveEase = Ease.OutExpo;

    // 시스템
    private Action _onClearAction; // TODO : 각 상황마다 적절한 함수 전달
    private bool _isCleared;

    private struct MapState
    {
        public char[,,] Map;
        public Vector3 PlayerPos;
        public int RoadLeftCount;
    }

    private readonly Stack<MapState> _undoStack = new();
    
    private void OnEnable()
    {
        InitGame();
        CheckGameCleared();
    }

    #region GameSystem

    private void InitGame()
    {
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
        
        // 렌더링을 하면서
        // 플레이어 시작점 파악
        // 남은 블럭 개수 파악
        _isCleared = false;
        _roadLeftCount = 0;

        for (var i = 0; i < CubeSize; ++i)
        for (var j = 0; j < CubeSize; ++j)
        for (var k = 0; k < CubeSize; ++k)
        {
            var c = _map[i, j, k];

            switch (c)
            {
                case (char)TileType.Empty:
                    continue;
                case (char)TileType.Road:
                    ++_roadLeftCount;
                    break;
                case (char)TileType.Player:
                    playerModel.position = new Vector3(i, j, k);
                    break;
            }

            _tiles[i, j, k].SimpleRender(c);
        }
    }

    public void SetMapData(char[,,] map)
    {
        _map = map;
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
        _onClearAction?.Invoke();
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

        if ((int)nPos.x == -1 || (int)nPos.x == 10 || (int)nPos.y == -1 || (int)nPos.y == 10 || (int)nPos.z == -1 ||
            (int)nPos.z == 10
            || _map[nLayer, nRow, nCol] == (char)TileType.Empty || _map[nLayer, nRow, nCol] == (char)TileType.Painted)
        {
            // TODO : 통과 아이템 효과
            // 이동 불가 애니메이션
            if (doRender)
            {
                await playerModel.DOShakePosition(playerMoveDuration).AsyncWaitForCompletion().AsUniTask();
            }

            return;
        }

        SaveUndoState();

        _map[(int)pos.x, (int)pos.y, (int)pos.z] = (char)TileType.Painted; // 현재 위치 페인트, 단 페인트 렌더링은 플레이어가 도달할 때 해주기때문에 이미 함

        if (_map[nLayer, nRow, nCol] is not (char)TileType.Painted)
            --_roadLeftCount;

        if (doRender)
        {
            // 플레이어 움직임 애니메이션 기다리고
            var t = playerModel.DOMove(nPos, playerMoveDuration, true).SetEase(playerMoveEase).AsyncWaitForCompletion().AsUniTask();
            // 새 타일 칠하기 및 아이템 발동
            await SetTileWithRender(nLayer, nRow, nCol, (char)TileType.Player, true, true); // 일단 simpleRender 사용
            await t;
        }
        else
        {
            SetTile(nLayer, nRow, nCol, (char)TileType.Player);
        }

        CheckGameCleared();
    }

    // 움직임이 확정된 뒤 저장
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (char[,,])_map.Clone();
        _undoStack.Push(new MapState()
            { Map = snapshot, PlayerPos = playerModel.position, RoadLeftCount = _roadLeftCount });
    }

    public void Undo(bool doRender = true)
    {
        if (_undoStack.Count == 0) return;

        var s = _undoStack.Pop();
        _map = s.Map;
        playerModel.position = s.PlayerPos;
        _roadLeftCount = s.RoadLeftCount;

        if (doRender)
            Render();
    }

    // 랜더링 없는 타일 설정
    public void SetTile(int layer, int row, int col, char tile, bool activateItem = true)
    {
        _map[layer, row, col] = tile;

        // 템 발동
        if (activateItem)
        {

        }
    }

    // 이동 및 아이템 사용할때 사용 -> 즉 타일이 '1'(페인트) 또는 '2'(플레이어)로만 바뀜
    public async UniTask SetTileWithRender(int layer, int row, int col, char tile, bool activateItem = true,
        bool useSimpleRender = false, bool wait = true)
    {
        var c = _map[layer, row, col];
        var alreadyPainted = false;
        if (c == (char)TileType.Painted) alreadyPainted = true;

        _map[layer, row, col] = tile;

        if (alreadyPainted) return;

        if (useSimpleRender)
        {
            _tiles[layer, row, col].SimpleRender(tile);
            return;
        }

        if (wait)
            await _tiles[layer, row, col].Render(tile);
        else
            _ = _tiles[layer, row, col].Render(tile, false);

        if (activateItem)
        {

        }
        //템 발동
    }

    #endregion
    
    #region PlayerControl

    public async UniTaskVoid MovePlayerControl(Vector3 dir)
    {
        if (_isMoving) return;
        _isMoving = true;
        try
        {
            await MovePlayer(dir);
        }
        finally
        {
            _isMoving = false;
        }
    }
    #endregion

    #region CameraMove
    public async UniTask MoveToCornerArc(int index, CancellationToken ct = default)
    {
        var targetPos = cameraPos[index];
        var targetRot = Quaternion.Euler(cameraRot[index]);
        var centerElement = (CubeSize - 1) / 2.0f;
        var center = new Vector3(centerElement, centerElement, centerElement);
        var distance = CubeSize * 2;

        // 구면 보간으로 중심 기준 호 경로 생성
        var fromDir = (cam.transform.position - center).normalized;
        var toDir   = (targetPos - center).normalized;

        var posSeq = DOTween.Sequence();
        posSeq.Append(
            DOTween.To(t =>
            {
                // Slerp으로 구면 위를 따라 이동
                var dir = Vector3.Slerp(fromDir, toDir, t);
                cam.transform.position = center + dir * distance;
            }, 0f, 1f, camMoveDuration).SetEase(camMoveEase)
        );

        var rotateTween = cam.transform
            .DORotateQuaternion(targetRot, camMoveDuration)
            .SetEase(camMoveEase);

        await UniTask.WhenAll(
            posSeq.ToUniTask(cancellationToken: ct),
            rotateTween.ToUniTask(cancellationToken: ct)
        );
    }
    
    public async UniTaskVoid OnCameraButtonClicked(int i)
    {
        if (_isMoving) return;
        _isMoving = true;
        try
        {
            await MoveToCornerArc(i);
        }
        finally
        {
            _isMoving = false;
        }
    }

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
}