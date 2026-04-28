using System;
using UnityEngine;

public class TurnOffIndicators : MonoBehaviour
{
    private void OnEnable()
    {
        MapEditor.Instance.blockIndicator = true;
    }

    private void OnDisable()
    {
        MapEditor.Instance.blockIndicator = false;
    }
}
