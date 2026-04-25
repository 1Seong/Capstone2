using System.Collections.Generic;
using UnityEngine;

public class EdgeCubeInitializer : MonoBehaviour
{
    [Header("큐브 설정")]
    public float originOffset = .5f;  // 큐브 전체 시작 위치 (중심 기준)
    public GameObject edgeCubePrefab;

    // 연속된 false 구간을 반환
    private struct Segment
    {
        public int StartIndex;
        public int Length;
    }

    List<Segment> GetSegments(bool[] state)
    {
        var segments = new List<Segment>();
        int i = 1;

        while (i < state.Length - 1)
        {
            if (!state[i]) // false 구간 시작
            {
                int start = i;
                while (i < state.Length - 1 && !state[i])
                    i++;
                segments.Add(new Segment { StartIndex = start, Length = i - start });
            }
            else
            {
                i++;
            }
        }

        return segments;
    }

    public void Initialize(bool[] canRotate, int axis, int cubeSize)
    {
        if (axis == 0)
        {
            Instantiate(edgeCubePrefab, transform);
            return;
        }
        
        var segments = GetSegments(canRotate);
        
        foreach (var seg in segments)
        {
            float size = seg.Length;
            float center   = originOffset + (seg.StartIndex + seg.Length * 0.5f);

            GameObject edge = Instantiate(edgeCubePrefab, transform);
            float x = (cubeSize - 1) / 2.0f;
            switch (axis)
            {
                case 1: // x
                    edge.transform.localPosition = new Vector3(center, x, x);
                    edge.transform.localScale    = new Vector3(size, cubeSize-2, cubeSize-2);
                    break;
                case 2: // y
                    edge.transform.localPosition = new Vector3(x, center, x);
                    edge.transform.localScale    = new Vector3(cubeSize-2, size, cubeSize-2);
                    break;
                case 3: // z
                    edge.transform.localPosition = new Vector3(x, x, center);
                    edge.transform.localScale    = new Vector3(cubeSize-2, cubeSize-2, size);
                    break;
            }
        }
    }

    public void Clear()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}
