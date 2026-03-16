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
		public TMP_InputField EmailInput = null!;
		public TMP_InputField PasswordInput = null!;
		public TMP_Text ErrorText = null!;
        public TMP_Text SignUpText = null!;
        public TMP_Text SignOutText = null!;
        public TMP_Text DeleteText;
		public SupabaseManager SupabaseManager = null!;

        [Header("Panels")]
        public GameObject SigninPanel;
        public GameObject SignoutPanel;
        public GameObject SignupPanel;
        public GameObject AccountDeletePanel;

		// Private implementation
		private bool _doSignIn;
		private bool _doSignOut;
		private bool _doSignUp;
        private bool _doDelete;
		private bool _isSigningIn, _isSigningOut, _isSigningUp, _isDeleting;

		// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
		public void SignIn()
		{
			_doSignIn = true;
		}

		// Unity does not allow async UI events, so we set a flag and use Update() to do the async work
		public void SignOut()
		{
			_doSignOut = true;
		}

		public void SignUp()
		{
			_doSignUp = true;
		}

        public void DeleteAccount()
        {
            _doDelete = true;
        }

		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async void Update()
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
				try
				{
					await PerformSignIn();
				}
				finally
				{
					_isSigningIn = false;
				}
				
			}

            if (_doSignUp)
            {
                _doSignUp = false;
                if (_isSigningUp) return;

                _isSigningUp = true;
                try
                {
                    await PerformSignUp();
                }
                finally
                {
                    _isSigningUp = false;
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

		// This is where we do the async work and handle exceptions
		[SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
		private async Task PerformSignIn()
		{
			try
			{
                if (!EmailInput.text.Contains('@') || string.IsNullOrWhiteSpace(EmailInput.text))
                {
                    ErrorText.text = "유효하지 않은 이메일 형식입니다.";
                    return;
                }

                Session session = (await SupabaseManager.Supabase()!.Auth.SignIn(EmailInput.text, PasswordInput.text))!;
                //ErrorText.text = $"Success! Signed In as {session.User?.Email}";
                SignoutPanel.SetActive(false);
                SigninPanel.SetActive(true);
			}
            catch (GotrueException goTrueException)
            {
                ErrorText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.Offline => "오프라인입니다.",
                    _ => goTrueException.Message.Contains("invalid format")
                    ? "유효하지 않은 이메일 형식입니다."
                    : "이메일과 비밀번호를 다시 확인해주세요."
                };
                Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                ErrorText.text = "오류가 발생했습니다. 다시 시도해주세요.";
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
                SigninPanel.SetActive(false);
                SignoutPanel.SetActive(true);
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
                SignOutText.text = "오류가 발생했습니다. 다시 시도해주세요.";
                Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }

        [SuppressMessage("ReSharper", "Unity.PerformanceCriticalCodeInvocation")]
        private async Task PerformSignUp()
        {
            try
            {
                if (!EmailInput.text.Contains('@') || string.IsNullOrWhiteSpace(EmailInput.text))
                {
                    SignUpText.text = "유효하지 않은 이메일 형식입니다.";
                    return;
                }

                var response = await SupabaseManager.Supabase()!.Auth.SignUp(EmailInput.text, PasswordInput.text, new SignUpOptions { 
                    RedirectTo = "https://1seong.github.io/3dcubepainting.github.io/" });

                if (response?.User != null && response == null)
                {
                    // 정상 케이스: 이메일 인증 대기 중
                    SignUpText.text = $"인증 이메일을 {response.User.Email}로 보냈습니다. 이메일을 확인해주세요.";
                }
                else if (response != null)
                {
                    // Email confirmation이 꺼져 있을 때 (혹은 나중에 끌 경우 대비)
                    SignUpText.text = $"가입 완료! {response.User?.Email}로 로그인되었습니다.";
                }
                else
                {
                    SignUpText.text = "알 수 없는 응답입니다. 다시 시도해주세요.";
                }
            }
            catch (GotrueException goTrueException)
            {
                SignUpText.text = goTrueException.Reason switch
                {
                    FailureHint.Reason.UserAlreadyRegistered => "이미 등록된 이메일입니다.", // Email confirmation이 꺼져 있을 때
					FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
                    _ => goTrueException.Message.Contains("invalid format")
					? "유효하지 않은 이메일 형식입니다."
					: $"오류: {goTrueException.Message}"
                };
				Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                SignUpText.text = "오류가 발생했습니다. 다시 시도해주세요.";
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
                    FailureHint.Reason.Offline => "네트워크 연결을 확인해주세요.",
                    _ => $"오류: {goTrueException.Message}"
                };
                Debug.Log(goTrueException.Message, gameObject);
                Debug.LogException(goTrueException, gameObject);
            }
            catch (Exception e)
            {
                SignOutText.text = "오류가 발생했습니다. 다시 시도해주세요.";
                Debug.Log(e.Message, gameObject);
                Debug.LogException(e, gameObject);
            }
        }
        */
    }
}
