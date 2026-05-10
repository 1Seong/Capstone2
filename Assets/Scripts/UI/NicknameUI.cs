using System;
using TMPro;
using UnityEngine;

public class NicknameUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject signoutPanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI errorTMP;
    
    /*
    private async void OnEnable()
    {
        bool b;
        try
        {
            b = await DBManager.Instance.HasNicknameAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            return;
        }

        if (!b)
        {
            panel.SetActive(true);
        }
    }
    */

    public async void OnClick()
    {
        if (!GameManager.Instance.CheckNetworkAndLogIn())
            return;
        
        string s = inputField.text.Trim();
        if (string.IsNullOrEmpty(s))
        {
            errorTMP.text = "닉네임은 1자 이상 20자 이하여야 합니다.";
            return;
        }

        bool b;
        SceneChange.Instance.LightLoading(true);
        try
        {
            b = await DBManager.Instance.IsNicknameAvailableAsync(s);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            errorTMP.text = "나중에 다시 시도해주세요.";
            SceneChange.Instance.LightLoading(false);
            return;
        }
        if (!b)
        {
            errorTMP.text = "이미 존재하는 닉네임입니다.";
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        try
        {
            await DBManager.Instance.UpsertNicknameAsync(s);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            errorTMP.text = "나중에 다시 시도해주세요.";
            SceneChange.Instance.LightLoading(false);
            return;
        }
        
        panel.SetActive(false);
        signoutPanel.SetActive(true);
        PopUpManager.Instance.Show("닉네임 설정 완료.");
        SceneChange.Instance.LightLoading(false);
    }
}
