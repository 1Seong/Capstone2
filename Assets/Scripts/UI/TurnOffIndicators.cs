using System;
using UnityEngine;

public class TurnOffIndicators : MonoBehaviour
{
    private void OnEnable()
    {
        GameManager.Instance.blockIndicators = true;
    }

    private void OnDisable()
    {
        GameManager.Instance.blockIndicators = false;
    }
}
