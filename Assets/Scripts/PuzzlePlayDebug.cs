using System;
using UnityEngine;

public class PuzzlePlayDebug : MonoBehaviour
{
    [Serializable]
    public class MapPlane
    {
        [Tooltip("각 행을 문자열로 입력 (길이 10)")]
        public string[] rows = {
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
            "0000000000",
        };
    }
    
    [SerializeField] private MapPlane[] planes = new MapPlane[10];

    public char[,,] ToArray()
    {
        var cube = new char[10, 10, 10];
        for (int x = 0; x < 10; x++)
        for (int y = 0; y < 10; y++)
        for (int z = 0; z < 10; z++)
            cube[x, y, z] = planes[x].rows[y][z];
        return cube;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.PlayGame(ToArray());
    }
}
