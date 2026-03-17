using System;
using TMPro;
using UnityEngine;

public class UIClear : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] inputFields;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text SucessText;

    private void OnEnable()
    {
        if(inputFields != null || inputFields.Length > 0)
            foreach (var i in inputFields)
            {
                i.text = string.Empty;
            }
        statusText.text = string.Empty;
        if(SucessText != null) 
        {
            SucessText.text = string.Empty;
        }
    }
}
