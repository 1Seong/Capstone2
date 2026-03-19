using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace com.example
{
	public class SignInWithEmailPassword : MonoBehaviour
	{
		// Public Unity References
		public SupabaseManager SupabaseManager = null!;

        [Header("SignIn")]
        public GameObject SigninPanel;
        public TMP_InputField EmailInput = null!;
        public TMP_InputField PasswordInput = null!;
        public TMP_Text ErrorText = null!;
        public Button SignInCloseButton = null!;
        
        [Header("SignUp")]
        public GameObject SignupPanel;
        public TMP_InputField SignUpEmailInput = null!;
        public TMP_InputField SignUpPasswordInput = null!;
        public TMP_InputField SecondaryPasswordInput = null!;
        public TMP_Text SignUpText = null!;
        public TMP_Text SignUpSucessText = null!;
        public Button SignUpCloseButton = null!;
        
        [Header("SignOut")]
        public GameObject SignoutPanel;
        public TMP_Text SignOutText = null!;
        
        [Header("Recovery")]
        public TMP_InputField RecoveryEmailInput = null!;
        public TMP_Text RecoveryText = null!;
        public TMP_Text RecoverySucessText = null!;
        public Button RecoveryCloseButton = null!;

        [Header("ChangePw")] 
        public TMP_InputField ChangePwPasswordInput = null!;
        public TMP_InputField ChangePwSecondaryPasswordInput = null!;
        public TMP_Text ChangePwText = null!;
        public TMP_Text ChangePwSucessText = null!;
        public Button ChangePwCloseButton = null!;
        
        [Header("Delete")]
        public GameObject AccountDeletePanel;
        public TMP_Text DeleteText;

        [Header("OAuth")] 
        [SerializeField] private OAuthManager _oAuthManager;
        
        

		// Private implementation
		private bool _doSignIn;
		private bool _doSignOut;
		private bool _doSignUp;
		private bool _doRecovery;
		private bool _doChangePw;
        private bool _doDelete;
        private bool _doGoogleSignIn;
        private bool _doDiscordSignIn;
		private bool _isSigningIn, _isSigningOut, _isSigningUp, _isDeleting, _isRecoverying, _isChangingPw, _isSigningInGoogle, _isSigningInDiscord;

		#region ButtonCallback
		// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
		public void SignIn()
		{
			if (!SupabaseManager.IsNetworkAvailable())
			{
				ErrorText.text = "네트워크 연결을 확인해주세요.";
				return;
			}
			if (!EmailInput.text.Contains('@') || string.IsNullOrWhiteSpace(EmailInput.text))
			{
				ErrorText.text = "유효하지 않은 이메일 형식입니다.";
				return;
			}

			if (PasswordInput.text == string.Empty || string.IsNullOrWhiteSpace(PasswordInput.text))
			{
				ErrorText.text = "비밀번호를 입력해주세요.";
				return;
			}

			_doSignIn = true;
		}

		// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
		public void SignOut()
		{
			if (!SupabaseManager.IsNetworkAvailable())
			{
				SignOutText.text = "네트워크 연결을 확인해주세요.";
				return;
			}
			
			_doSignOut = true;
		}

		public void SignUp()
		{
			if (!SupabaseManager.IsNetworkAvailable())
			{
				SignOutText.text = "네트워크 연결을 확인해주세요.";
				return;
			}
			if (!SignUpEmailInput.text.Contains('@') || string.IsNullOrWhiteSpace(SignUpEmailInput.text))
			{
				SignUpText.text = "유효하지 않은 이메일 형식입니다.";
				return;
			}

			if (SignUpPasswordInput.text == string.Empty || string.IsNullOrWhiteSpace(SignUpPasswordInput.text))
			{
				SignUpText.text = "비밀번호를 입력해주세요.";
				return;
			}

			if (SignUpPasswordInput.text.Length < 6)
			{
				SignUpText.text = "비밀번호는 최소 6자리 이상이어야 합니다.";
				return;
			}

			if (SignUpPasswordInput.text != SecondaryPasswordInput.text)
			{
				SignUpText.text = "비밀번호가 일치하지 않습니다.";
				return;
			}
			
			_doSignUp = true;
		}
		
		public void Recovery()
		{
			if (!SupabaseManager.IsNetworkAvailable())
			{
				RecoveryText.text = "네트워크 연결을 확인해주세요.";
				return;
			}
			if (!RecoveryEmailInput.text.Contains('@') || string.IsNullOrWhiteSpace(RecoveryEmailInput.text))
			{
				RecoveryText.text = "유효하지 않은 이메일 형식입니다.";
				return;
			}
			
			_doRecovery = true;
		}
		
		public void ChangePw()
		{
			if (!SupabaseManager.IsNetworkAvailable())
			{
				ChangePwText.text = "네트워크 연결을 확인해주세요.";
				return;
			}

			if (ChangePwPasswordInput.text == string.Empty || string.IsNullOrWhiteSpace(ChangePwPasswordInput.text))
			{
				ChangePwText.text = "새 비밀번호를 입력해주세요.";
				return;
			}

			if (ChangePwPasswordInput.text.Length < 6)
			{
				ChangePwText.text = "비밀번호는 최소 6자리 이상이어야 합니다.";
				return;
			}

			if (ChangePwPasswordInput.text != ChangePwSecondaryPasswordInput.text)
			{
				ChangePwText.text = "비밀번호가 일치하지 않습니다.";
				return;
			}
			
			_doChangePw = true;
		}

        public void DeleteAccount()
        {
            _doDelete = true;
        }

        public void GoogleSignIn()
        {
	        if (!SupabaseManager.IsNetworkAvailable())
	        {
		        ErrorText.text = "네트워크 연결을 확인해주세요.";
		        return;
	        }
	        
	        _doGoogleSignIn = true;
        }
        
        public void DiscordSignIn()
        {
	        if (!SupabaseManager.IsNetworkAvailable())
	        {
		        ErrorText.text = "네트워크 연결을 확인해주세요.";
		        return;
	        }
	        
	        _doDiscordSignIn = true;
        }
        #endregion

        public void OpenAccount()
        {
	        if(SupabaseManager.IsLoggedIn())
		        SignoutPanel.SetActive(true);
	        else
		        SigninPanel.SetActive(true);
        }

		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async void Update()
		{
			try
			{
				// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
				if (_doSignOut)
				{
					_doSignOut = false;
					if (_isSigningOut) return;

					_isSigningOut = true;
					try
					{
						await PerformSignOut();
					}
					finally
					{
						_isSigningOut = false;
					}
				}

				// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
				if (_doSignIn)
				{
					_doSignIn = false;
					if(_isSigningIn) return;

					_isSigningIn = true;
					SignInCloseButton.interactable = false;
					try
					{
						await PerformSignIn();
					}
					finally
					{
						_isSigningIn = false;
						SignInCloseButton.interactable = true;
					}
				
				}

				if (_doSignUp)
				{
					_doSignUp = false;
					if (_isSigningUp) return;

					_isSigningUp = true;
					SignUpCloseButton.interactable = false;
					SignUpSucessText.text = "처리 중...";
					try
					{
						await PerformSignUp();
					}
					finally
					{
						_isSigningUp = false;
						SignUpCloseButton.interactable = true;
					}

				}
				
				if (_doRecovery)
				{
					_doRecovery = false;
					if (_isRecoverying) return;

					_isRecoverying = true;
					RecoveryCloseButton.interactable = false;
					RecoverySucessText.text = "처리 중...";
					try
					{
						await PerformRecovery();
					}
					finally
					{
						_isRecoverying = false;
						RecoveryCloseButton.interactable = true;
					}

				}
				
				if (_doChangePw)
				{
					_doChangePw = false;
					if (_isChangingPw) return;

					_isChangingPw = true;
					ChangePwCloseButton.interactable = false;
					ChangePwSucessText.text = "처리 중...";
					try
					{
						await PerformChangePw();
					}
					finally
					{
						_isChangingPw = false;
						ChangePwCloseButton.interactable = true;
					}

				}

				if (_doGoogleSignIn)
				{
					_doGoogleSignIn = false;
					if (_isSigningInGoogle) return;

					_isSigningInGoogle = true;
					SignInCloseButton.interactable = false;
					try
					{
						await PerformGoogleLogin();
					}
					finally
					{
						_isSigningInGoogle = false;
						SignInCloseButton.interactable = true;
					}
				}
				
				if (_doDiscordSignIn)
				{
					_doDiscordSignIn = false;
					if (_isSigningInDiscord) return;

					_isSigningInDiscord = true;
					SignInCloseButton.interactable = false;
					try
					{
						await PerformDiscordLogin();
					}
					finally
					{
						_isSigningInDiscord = false;
						SignInCloseButton.interactable = true;
					}
				}
				/*
            if(_doDelete && !_isDeleting)
            {
                _doDelete = false;
                _isDeleting = true;
                try
                {
                    await PerformDelete();
                }
                finally
                {
                    _isDeleting = false;
                }
            }
            */
			}
			catch (Exception e)
			{
				Debug.Log(e.Message, gameObject);
				Debug.LogException(e, gameObject);
			}
		}

		// This is where we do the async work and handle exceptions
		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async Task PerformSignIn()
		{
			try
			{
                var session = (await SupabaseManager.Supabase()!.Auth.SignIn(EmailInput.text, PasswordInput.text))!;
                //ErrorText.text = $"Success! Signed In as {session.User?.Email}";

                if (session?.AccessToken != null)
                {
	                SigninPanel.SetActive(false);
	                SignoutPanel.SetActive(true);
                }
			}
            catch (GotrueException goTrueException)
            {
                ErrorText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
                    _ => goTrueException.Message.Contains("invalid format")
                    ? "유효하지 않은 형식입니다."
                    : "이메일과 비밀번호를 다시 확인해주세요."
                };
                Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                ErrorText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
                Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }
		
		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async Task PerformDiscordLogin()
		{
			try
			{
				var session = await _oAuthManager.SignInWithDiscord();
				//ErrorText.text = $"Success! Signed In as {session.User?.Email}";

				if (session?.AccessToken != null)
				{
					SigninPanel.SetActive(false);
					SignoutPanel.SetActive(true);
				}
			}
			catch (GotrueException goTrueException)
			{
				ErrorText.text = goTrueException.Reason switch
				{
					FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
					_ => "문제가 발생했습니다. 다시 시도해주세요."
				};
				Debug.Log(goTrueException.Message, gameObject);
				Debug.LogException(goTrueException, gameObject);
			}
			catch (Exception e)
			{
				ErrorText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
				Debug.Log(e.Message, gameObject);
				Debug.LogException(e, gameObject);
			}
		}
		
		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async Task PerformGoogleLogin()
		{
			try
			{
				var session = await _oAuthManager.SignInWithGoogle();
				//ErrorText.text = $"Success! Signed In as {session.User?.Email}";

				if (session?.AccessToken != null)
				{
					SigninPanel.SetActive(false);
					SignoutPanel.SetActive(true);
				}
			}
			catch (GotrueException goTrueException)
			{
				ErrorText.text = goTrueException.Reason switch
				{
					FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
					_ => "문제가 발생했습니다. 다시 시도해주세요."
				};
				Debug.Log(goTrueException.Message, gameObject);
				Debug.LogException(goTrueException, gameObject);
			}
			catch (Exception e)
			{
				ErrorText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
				Debug.Log(e.Message, gameObject);
				Debug.LogException(e, gameObject);
			}
		}

        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformSignOut()
        {
            try
            {
                await SupabaseManager.Supabase()!.Auth.SignOut();
                //SignOutText.text = $"Signed out";
                SignoutPanel.SetActive(false);
                SigninPanel.SetActive(true);
            }
            catch (GotrueException goTrueException)
            {
                SignOutText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
                    _ => $"오류: {goTrueException.Message}"
                };
                Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                SignOutText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요";
                Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }

        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformSignUp()
        {
            try
            {
                var response = await SupabaseManager.Supabase()!.Auth.SignUp(SignUpEmailInput.text, SignUpPasswordInput.text, new SignUpOptions { 
                    RedirectTo = "https://1seong.github.io/3dcubepainting.github.io/" });

                if (response?.AccessToken == null)
                {
	                // 이메일 인증이 켜진 경우 — Session이 null로 반환됨
	                // User 정보를 response에서 꺼낼 수 없으므로 입력값 사용
	                SignUpSucessText.text = $"{SignUpEmailInput.text}으로 인증 메일을 전송했습니다.";
                }
                else
                {
	                // 이메일 인증이 꺼진 경우 — 즉시 로그인됨
	                SignUpSucessText.text = $"{response.User?.Email}으로 가입이 완료되었습니다.";
                }
            }
            catch (GotrueException goTrueException)
            {
                SignUpText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.UserAlreadyRegistered => "이미 사용 중인 이메일입니다.", // Email confirmation이 꺼진 경우 대비
					FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
                    _ => goTrueException.Message.Contains("invalid format")
					? "유효하지 않은 형식입니다."
					: $"오류: {goTrueException.Message}"
                };
				Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                SignUpText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
				Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }
        
        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformRecovery()
        {
	        try
	        {
		        var response = await SupabaseManager.Supabase()!.Auth.ResetPasswordForEmail(RecoveryEmailInput.text);

		        if (response)
		        {
			        // 이메일 인증이 켜진 경우
			        // User 정보를 response에서 꺼낼 수 없으므로 입력값 사용
			        RecoverySucessText.text = $"{RecoveryEmailInput.text}으로 비밀번호 재설정 메일을 전송했습니다.";
		        }
		        else
		        {
			        RecoveryText.text = $"이메일 전송에 실패했습니다. 다시 확인해주세요.";
		        }
	        }
	        catch (GotrueException goTrueException)
	        {
		        RecoveryText.text = goTrueException.Reason switch
		        {
			        FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
			        _ => goTrueException.Message.Contains("invalid format")
				        ? "유효하지 않은 형식입니다."
				        : $"오류: {goTrueException.Message}"
		        };
		        Debug.Log(goTrueException.Message, gameObject);
		        Debug.LogException(goTrueException, gameObject);
	        }
	        catch (Exception e)
	        {
		        RecoveryText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
		        Debug.Log(e.Message, gameObject);
		        Debug.LogException(e, gameObject);
	        }
        }
        
        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformChangePw()
        {
	        try
	        {
		        var response = await SupabaseManager.Supabase()!.Auth.Update(new UserAttributes()
			        { Password = ChangePwPasswordInput.text });

		        if (response != null)
		        {
			        ChangePwSucessText.text = $"비밀번호 변경이 완료되었습니다.";
		        }
		        else
		        {
			        ChangePwText.text = $"변경 실패, 나중에 다시 시도해주세요.";
		        }
	        }
	        catch (GotrueException goTrueException)
	        {
		        ChangePwText.text = goTrueException.Reason switch
		        {
			        FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
			        _ => goTrueException.Message.Contains("invalid format")
				        ? "유효하지 않은 형식입니다."
				        : $"오류: {goTrueException.Message}"
		        };
		        Debug.Log(goTrueException.Message, gameObject);
		        Debug.LogException(goTrueException, gameObject);
	        }
	        catch (Exception e)
	        {
		        ChangePwText.text = "오류가 발생했습니다. 나중에 다시 시도해주세요.";
		        Debug.Log(e.Message, gameObject);
		        Debug.LogException(e, gameObject);
	        }
        }

        /*
        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformDelete()
        {
            try
            {
                await SupabaseManager.Supabase()!.Auth.
                //SignOutText.text = $"Signed out";
                SigninPanel.SetActive(false);
                SignoutPanel.SetActive(true);
            }
            catch (GotrueException goTrueException)
            {
                SignOutText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.Offline => "?????? ?????? ??????????.",
                    _ => $"????: {goTrueException.Message}"
                };
                Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                SignOutText.text = "?????? ?????????. ??? ?�????????.";
                Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }
        */
    }
}
