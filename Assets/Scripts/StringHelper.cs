using UnityEngine;

public class StringHelper
{
    public static string Encode(char[,,] cube)
    {
        int size = cube.GetLength(0);
        var sb = new System.Text.StringBuilder(size * size * size);
        for (int x = 0; x < size; ++x)
            for (int y = 0; y < size; ++y)
                for (int z = 0; z < size; ++z)
                    sb.Append(cube[x, y, z]);
        return sb.ToString();
    }
    
    public static char[,,] DecodeCube(string data)
    {
        int size = Mathf.RoundToInt(Mathf.Pow(data.Length, 1f / 3f));
        var cube = new char[size, size, size];
        int i = 0;
        for (int x = 0; x < size; ++x)
            for (int y = 0; y < size; ++y)
                for (int z = 0; z < size; ++z)
                    cube[x, y, z] = data[i++];
        return cube;
    }
}
