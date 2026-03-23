using UnityEngine;

public class StringHelper
{
    // 상수 정의
    const int Layers = 6, Rows = 10, Cols = 10;
    
    // 직렬화 헬퍼
    public static string[][] ToJagged(string[,] map)
    {
        int layers = map.GetLength(0);
        int rows   = map.GetLength(1);
        var jagged = new string[layers][];
        for (int l = 0; l < layers; l++)
        {
            jagged[l] = new string[rows];
            for (int r = 0; r < rows; r++)
                jagged[l][r] = map[l, r];
        }
        return jagged;
    }

    // 인코딩: string[,] → text
    public static string Encode(string[,] map)
    {
        var sb = new System.Text.StringBuilder(Layers * Rows * Cols);
        for (int l = 0; l < Layers; l++)
            for (int r = 0; r < Rows; r++)
                sb.Append(map[l, r]); // 각 행 string을 순서대로 이어붙임
        return sb.ToString();
    }

    // 디코딩: text → string[,]
    public static string[,] Decode(string text)
    {
        var map = new string[Layers, Rows];
        for (int l = 0; l < Layers; l++)
            for (int r = 0; r < Rows; r++)
                map[l, r] = text.Substring((l * Rows + r) * Cols, Cols);
        return map;
    }
}
