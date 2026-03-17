using System;
using TMPro;
using UnityEngine;

public class UIClear : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] inputFields;
    [SerializeField] private TMP_Text statusText;

    private void OnEnable()
    {
        foreach (var i in inputFields)
        {
            i.text = string.Empty;
        }
        statusText.text = string.Empty;
    }
}
