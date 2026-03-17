using System;
using com.example;
using TMPro;
using UnityEngine;


public class LogInPanel : MonoBehaviour
{
    [SerializeField] TMP_Text tmp_text;
    
    private void OnEnable()
    {
        var email = SupabaseManager.Instance.Supabase().Auth.CurrentSession.User.Email;

        tmp_text.text = $"{email} 로 로그인 되었습니다.";
    }
}
