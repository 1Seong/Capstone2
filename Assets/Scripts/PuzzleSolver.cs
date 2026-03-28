using System.Collections.Generic;
using UnityEngine;

public static class PuzzleSolver
{
    // 6방향: +X, -X, +Y, -Y, +Z, -Z
    private static readonly Vector3Int[] Directions = new Vector3Int[]
    {
        new Vector3Int( 1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int( 0, 1, 0),
        new Vector3Int( 0,-1, 0),
        new Vector3Int( 0, 0, 1),
        new Vector3Int( 0, 0,-1),
    };
 
    public struct SolveResult
    {
        public bool IsSolvable;
        public string ErrorMsg;
        /// <summary>클리어 가능한 경우 정답 경로, 불가능하면 null</summary>
        public Stack<Vector3Int> SolutionPath;
    }
 
    /// <summary>
    /// 퍼즐이 클리어 가능한지 검사하고, 가능하다면 정답 경로를 함께 반환합니다.
    /// </summary>
    /// <param name="map">원본 맵 데이터 (변경되지 않습니다)</param>
    public static SolveResult Solve(char[,,] map)
    {
        var cubeSize = map.GetLength(0);
 
        // 맵 복사 (원본 보호)
        var workMap = (char[,,])map.Clone();
 
        Vector3Int startPos = default;
        var totalPaintable = 0;
        var foundStart = false;
 
        // 시작 위치 탐색 & 색칠 가능 칸 카운트
        for (var x = 0; x != cubeSize; ++x)
        for (var y = 0; y != cubeSize; ++y)
        for (var z = 0; z != cubeSize; ++z)
        {
            var cell = workMap[x, y, z];
            switch (cell)
            {
                case (char)TileType.Player:
                    if (foundStart)
                    {
                        var msg = "시작 위치가 2개 이상입니다.";
                        //Debug.LogWarning(msg);
                        //PopUpManager.Instance.Show(msg);
                        return new SolveResult { IsSolvable = false, ErrorMsg = msg, SolutionPath = null };
                    }

                    startPos = new Vector3Int(x, y, z);
                    //Debug.Log(startPos);
                    foundStart = true;
                    break;
                case (char)TileType.Road:
                    ++totalPaintable;
                    //Debug.Log(x + " " + y + " " + z);
                    break;
            }
        }
 
        if (!foundStart)
        {
            var msg = "시작 위치를 찾을 수 없습니다.";
            //Debug.LogWarning(msg);
            //PopUpManager.Instance.Show(msg);
            return new SolveResult { IsSolvable = false, ErrorMsg = msg, SolutionPath = null };
        }
 
        // 시작 위치를 '3'으로 칠하고 DFS 시작
        workMap[startPos.x, startPos.y, startPos.z] = (char)TileType.Painted;

        var paintedCount = 0;

        var path = new Stack<Vector3Int>();
 
        var solved = DFS(
            workMap, startPos,
            cubeSize,
            totalPaintable, ref paintedCount,
            path
        );
 
        return new SolveResult
        {
            IsSolvable = solved,
            ErrorMsg = "해답이 존재하지 않습니다!",
            SolutionPath = solved ? path : null
        };
    }
 
    // ───────────────────────────────────────────────────────────────
 
    // ReSharper disable once InconsistentNaming
    private static bool DFS(
        char[,,] map,
        Vector3Int current,
        int size,
        int totalPaintable, ref int paintedCount,
        Stack<Vector3Int> path)
    {
        // 모든 색칠 가능 공간을 칠했으면 클리어
        if (paintedCount == totalPaintable)
            return true;
        
        // TODO: 회전 Layer 처리
        foreach (var dir in Directions)
        {
            int nx = current.x + dir.x;
            int ny = current.y + dir.y;
            int nz = current.z + dir.z;
 
            // 범위 체크
            if ((uint)nx >= (uint)size ||
                (uint)ny >= (uint)size ||
                (uint)nz >= (uint)size)
                continue;
            
            // '3'(이미 칠해진 칸)은 재통과 불가
            // TODO: 아이템 효과로 지나갈 수 있음 처리
            if (map[nx, ny, nz] is (char)TileType.Empty or (char)TileType.Painted)
                continue;
 
            var next = new Vector3Int(nx, ny, nz);
 
            // ── 전진 ──
            var before = map[nx, ny, nz];
            if (before != (char)TileType.Painted)
            {
                map[nx, ny, nz] = (char)TileType.Painted;
                ++paintedCount;
            }

            path.Push(next);
            
            // TODO: 아이템 처리
 
            if (DFS(map, next, size, totalPaintable, ref paintedCount, path))
                return true;
 
            // ── 백트래킹 ──
            path.Pop();
            if(before != (char)TileType.Painted)
                --paintedCount;
            map[nx, ny, nz] = before;
        }
 
        return false;
    }
 
    // ReSharper disable once UnusedMember.Local
    private static int CountCell(char[,,] map, int size, char target)
    {
        var count = 0;
        for (var x = 0; x != size; ++x)
        for (var y = 0; y != size; ++y)
        for (var z = 0; z != size; ++z)
            if (map[x, y, z] == target)
                ++count;
        return count;
    }
}
