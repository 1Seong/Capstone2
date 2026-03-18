using System;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using TMPro;
using UnityEngine;
using Client = Supabase.Client;

namespace com.example
{
	public class SupabaseManager : MonoBehaviour
	{

		// Public Unity references
		public SessionListener SessionListener = null!;
		public SupabaseSettings SupabaseSettings = null!;

		//public TMP_Text ErrorText = null!;

		// Public in case other components are interested in network status
		private readonly NetworkStatus _networkStatus = new();

		// Internals
		private Client? _client;

        [SerializeField]private bool _isLoggedIn = false;
        public bool IsLoggedIn() => _isLoggedIn;

        public Client? Supabase() => _client;

		public static SupabaseManager Instance;
        private void Awake()
        {
            if(Instance != null && Instance != this)
			{
				Destroy(gameObject);
			}
			else
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
        }

        private async void Start()
		{
			SupabaseOptions options = new();
			// We set an option to refresh the token automatically using a background thread.
			options.AutoRefreshToken = true;

			// We start setting up the client here
			Client client = new(SupabaseSettings.SupabaseURL, SupabaseSettings.SupabaseAnonKey, options);

			// The first thing we do is attach the debug listener
			client.Auth.AddDebugListener(DebugListener!);

			// Next we set up the network status listener and tell it to turn the client online/offline
			_networkStatus.Client = (Supabase.Gotrue.Client)client.Auth;

			// Next we set up the session persistence - without this the client will forget the session
			// each time the app is restarted
			client.Auth.SetPersistence(new UnitySession());

			// This will be called whenever the session changes
			client.Auth.AddStateChangedListener(SessionListener.UnityAuthListener);
			client.Auth.AddStateChangedListener(OnAuthStateChanged);

			// Fetch the session from the persistence layer
			// If there is a valid/unexpired session available this counts as a user log in
			// and will send an event to the UnityAuthListener above.
			client.Auth.LoadSession();

			// Allow unconfirmed user sessions. If you turn this on you will have to complete the
			// email verification flow before you can use the session.
			client.Auth.Options.AllowUnconfirmedUserSessions = false;
			client.Auth.Options.AutoRefreshToken = true;

			// We check the network status to see if we are online or offline using a request to fetch
			// the server settings from our project. Here's how we build that URL.
			string url = $"{SupabaseSettings.SupabaseURL}/auth/v1/settings?apikey={SupabaseSettings.SupabaseAnonKey}";
			try
			{
				// This will get the current network status
				client.Auth.Online = await _networkStatus.StartAsync(url);
			}
			catch (NotSupportedException)
			{
				// Some platforms don't support network status checks, so we just assume we are online
				client.Auth.Online = true;
			}
			catch (Exception e)
			{
				// Something else went wrong, so we assume we are offline
				//ErrorText.text = e.Message;
				Debug.Log(e.Message, gameObject);
				Debug.LogException(e, gameObject);

				client.Auth.Online = false;
			}
			if (client.Auth.Online)
			{
				/*
				Debug.Log($"CurrentSession: {client.Auth.CurrentSession != null}");
				Debug.Log($"CurrentSession Expired: {client.Auth.CurrentSession?.Expired()}");
				Debug.Log($"RefreshToken: {client.Auth.CurrentSession?.RefreshToken}");
				*/
				if (client.Auth.CurrentSession != null)
				{
					try
					{
						await client.Auth.RefreshSession();
						Debug.Log("세션 갱신 성공");
						_isLoggedIn = true;
					}
					catch (Exception e)
					{
						Debug.LogWarning($"세션 갱신 실패: {e.Message}");
						_isLoggedIn = false;
					}
				}
				
				// Now we start up the client, which will in turn start up the background thread.
				// This will attempt to refresh the session token, which in turn may send a second
				// user login event to the UnityAuthListener.
				await client.InitializeAsync();
				/*
				if (client.Auth.CurrentSession == null)
				{
					try
					{
						await client.Auth.RefreshSession();
						Debug.Log("세션 갱신 성공");
						_isLoggedIn = true;
					}
					catch (Exception e)
					{
						Debug.LogWarning($"세션 갱신 실패: {e.Message}");
						_isLoggedIn = false;
					}
				}
				*/
				Debug.Log($"Online: {client.Auth.Online}");
				Debug.Log($"CurrentSession: {client.Auth.CurrentSession != null}");
				Debug.Log($"CurrentSession Expired: {client.Auth.CurrentSession?.Expired()}");
				Debug.Log($"Created at: {client.Auth.CurrentSession?.CreatedAt}");
				Debug.Log($"Expired at: {client.Auth.CurrentSession?.ExpiresAt()}");
				Debug.Log($"RefreshToken: {client.Auth.CurrentSession?.RefreshToken}");
				Debug.Log($"CurrentUser: {client.Auth.CurrentUser?.Email}");

				// Here we fetch the server settings and log them to the console
				Settings serverConfiguration = (await client.Auth.Settings())!;
				Debug.Log($"Auto-confirm emails on this server: {serverConfiguration.MailerAutoConfirm}");
			}
			_client = client;
		}

		private void DebugListener(string message, Exception e)
		{
			//ErrorText.text = message;
			Debug.Log(message, gameObject);
			// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
			if (e != null)
				Debug.LogException(e, gameObject);
		}

        // This is called when Unity shuts down. You want to be sure to include this so that the
        // background thread is terminated cleanly. Keep in mind that if you are running the app
        // in the Unity Editor, if you don't call this method you will leak the background thread!
        private void OnApplicationQuit()
        {
            Cleanup();
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            //싱글턴이 파괴될 때만 정리
            if (this != Instance) return;
            Cleanup();
        }
#endif

        private void Cleanup()
        {
	        if (_client == null) return;
	        _client?.Auth.Shutdown();
	        _client = null;
        }

        public async Task<bool> IsNetworkAvailableAsync() // 비동기 - 맵 업로드 직전같은 상황에서 정확한 진단을 위해 사용
        {
            if (!_networkStatus.Ready) return false;

            string url = $"{SupabaseSettings.SupabaseURL}/auth/v1/settings?apikey={SupabaseSettings.SupabaseAnonKey}";
            return await _networkStatus.PingCheck(url);
        }

		public bool IsNetworkAvailable() => _client!.Auth.Online;// 동기 - UI 관련에 사용

		public bool IsNetworkAvailableAndLoggedIn() => IsNetworkAvailable() && _isLoggedIn;

		public async Task<bool> IsNetworkAvailableAsyncAndLoggedIn() => 
			await IsNetworkAvailableAsync() && _isLoggedIn;

        private void OnAuthStateChanged(IGotrueClient<User, Session> sender, Constants.AuthState state)
        {
	        if (state == Constants.AuthState.SignedIn && sender.CurrentSession != null)
	        {
		        /*
		        Debug.Log("Signed In");
		        Debug.Log($"CurrentSession Expired: {sender.CurrentSession?.Expired()}");
		        Debug.Log($"CurrentSession Expired at: {sender.CurrentSession?.ExpiresAt()}");
		        Debug.Log($"CurrentSession Created at: {sender.CurrentSession?.CreatedAt}");
		        */
		        // SignedIn 시점에 명시적으로 저장
		        new UnitySession().SaveSession(sender.CurrentSession);
		        Debug.Log("SignedIn 시점 세션 강제 저장");
	        }
            _isLoggedIn = state switch
            {
                Constants.AuthState.SignedIn => sender.CurrentSession?.AccessToken != null && !sender.CurrentSession.Expired(),
                Constants.AuthState.TokenRefreshed => sender.CurrentSession?.AccessToken != null && !sender.CurrentSession.Expired(),
                Constants.AuthState.UserUpdated => sender.CurrentSession?.AccessToken != null && !sender.CurrentSession.Expired(),
                Constants.AuthState.SignedOut => false,
                Constants.AuthState.Shutdown => false,
                Constants.AuthState.PasswordRecovery => false,
                _ => _isLoggedIn
            };
        }

    }
}
