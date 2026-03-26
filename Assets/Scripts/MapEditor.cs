using System;
using System.Collections.Generic;
using UnityEngine;

public class MapEditor : MonoBehaviour
{
    [SerializeField] private int cubeSize = 10;
    private char[,,] _map;
    public static MapEditor Instance;

    [SerializeField] private char currentTile = (char)TileType.Road;
    public char CurrentTile() => currentTile;
    
    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform  cubeParent;
    
    private PuzzleTile[,,] _tiles;

    private bool _isValidated = false;
    
    private EditTile _hoveredTile;
    
    // TODO: 아이템별 material 지정
    
    private struct MapState
    {
        public char[,,] Map;
        public Vector3 PlayerPos;
        public bool PlayerActive;
        public bool IsValidated;
    }

    private Stack<MapState> _undoStack = new Stack<MapState>();
    
    private void SaveUndoState()
    {
        // 얕은 복사가 아닌 깊은 복사 필수
        var snapshot = (char[,,])_map.Clone();
        _undoStack.Push(new MapState()
            { Map = snapshot, PlayerPos = playerModel.position, PlayerActive = playerModel.gameObject.activeSelf, IsValidated = _isValidated});
    }

    public void Undo(bool doRender = true)
    {
        if (_undoStack.Count == 0) return;

        var s = _undoStack.Pop();
        _map = s.Map;
        playerModel.position = s.PlayerPos;
        playerModel.gameObject.SetActive(s.PlayerActive);
        _isValidated = s.IsValidated;

        if (doRender)
            Render();
    }
    
    private void Render()
    {
        for (var i = 0; i < cubeSize; ++i)
        {
            for (var j = 0; j < cubeSize; ++j)
            {
                for (var k = 0; k < cubeSize; ++k)
                {
                    var c = _map[i, j, k];
                    _tiles[i, j, k].SimpleRender(c);
                }
            }
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
        
        InitEditor();
    }

    private void InitEditor()
    {
        _isValidated = false;
        _map = new char[cubeSize, cubeSize, cubeSize];
        
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
        
        _isValidated = false;
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
        gameObject.SetActive(false);
        // 로딩 오래 걸리면 씬 전환 효과 넣기
        GameManager.Instance.PlayGame(_map);
    }

    public void AutoTest()
    {
        
    }

    public void Export()
    {
        if (!_isValidated) return;
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
    /*
    private void Update()
    {
        HandleTileInteraction();
    }

    private void HandleTileInteraction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        EditTile hitTile = Physics.Raycast(ray, out RaycastHit hit)
            ? hit.collider.GetComponent<EditTile>()
            : null;

        // 호버 하이라이트 갱신
        if (hitTile != _hoveredTile)
        {
            _hoveredTile?.SetHighlight(false);
            hitTile?.SetHighlight(true);
            _hoveredTile = hitTile;
        }

        // 클릭 또는 드래그 중 타일 설정
        if (Input.GetMouseButton(0) && hitTile != null)
        {
            var pos = hitTile.transform.position;
            SetTile((int)pos.x, (int)pos.y, (int)pos.z);
        }
    }
    */
}
