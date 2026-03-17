using System;
using com.example;
using UnityEngine;
using UnityEngine.UI;

public class OnlineButton : MonoBehaviour
{
    [SerializeField] private bool doesRequireLogin;
    private Button _button;
    private Toggle _toggle;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (!SupabaseManager.Instance.IsNetworkAvailable() || doesRequireLogin && !SupabaseManager.Instance.IsLoggedIn())
        {
            if (_button != null)
                _button.interactable = false;
            if (_toggle != null)
                _toggle.interactable = false;
        }
        else
        {
            if (_button != null)
                _button.interactable = true;
            if (_toggle != null)
                _toggle.interactable = true;
        }
    }
}
