
using System;
using System.Threading;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using UnityEngine;
using Client = Supabase.Client;

namespace com.example
{
    public class OAuthManager : MonoBehaviour
    {
        // PC 전용: 로컬 HTTP 리스너 포트
        private const string DeepLinkScheme = "mygame://callback";
        private string _redirectUrl;

        private TaskCompletionSource<string> _callbackTcs;
        private ProviderAuthState _oauthState;

        /*
        void OnEnable()
        {
            // 모바일 Deep Link 수신
            Application.deepLinkActivated += OnDeepLinkActivated;

            // 앱 시작 시 이미 Deep Link가 있는 경우 처리
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeepLinkActivated(Application.absoluteURL);
        }


        void OnDisable()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
        }
        */

        // ──────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────

        public async Task<Session> SignInWithGoogle()
            => await SignInWithProvider(Constants.Provider.Google);

        public async Task<Session> SignInWithDiscord()
            => await SignInWithProvider(Constants.Provider.Discord);

        // ──────────────────────────────────────────
        // 핵심 OAuth 플로우
        // ──────────────────────────────────────────

        private async Task<Session> SignInWithProvider(Constants.Provider provider)
        {
            var client = SupabaseManager.Instance.Supabase();

            var port = GetAvailablePort();

            // PC와 모바일 콜백 URL 분기
            var isMobile = Application.platform == RuntimePlatform.Android
                            || Application.platform == RuntimePlatform.IPhonePlayer;

            _redirectUrl = isMobile
                ? DeepLinkScheme
                : $"http://localhost:{port}/callback/";
            
            // 리스너 먼저 시작
            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add(_redirectUrl);
            listener.Start();

            // OAuth URL 요청 (PKCE 자동 처리됨)
            var signInOptions = new SignInOptions
            {
                RedirectTo = _redirectUrl,
                FlowType = Constants.OAuthFlowType.PKCE,
            };

            try
            {
                _oauthState = await client!.Auth.SignIn(provider, signInOptions);

                if (_oauthState?.Uri == null)
                    throw new Exception("OAuth URL 생성 실패");

                // 브라우저 열기
                Application.OpenURL(_oauthState.Uri.ToString());

                // 콜백 대기
                _callbackTcs = new TaskCompletionSource<string>();

                string callbackUrl;
                if (_redirectUrl.StartsWith("http://localhost"))
                    callbackUrl = await WaitForLocalCallback(listener);
                else
                    callbackUrl = await _callbackTcs.Task;

                // URL에서 code 추출 후 세션 교환
                return await ExchangeCodeForSession(client, callbackUrl);
            }
            catch
            {
                listener.Close(); // 예외 경로: 여기서 닫기
                throw;
            }
        }

        private async Task<Session> ExchangeCodeForSession(Client client, string callbackUrl)
        {
            var uri = new Uri(callbackUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var authCode = query["code"];

            if (string.IsNullOrEmpty(authCode))
                throw new Exception($"콜백 URL에 code가 없음: {callbackUrl}");

            // Supabase C# SDK가 PKCE 교환을 처리
            // callbackUrl 예: mygame://callback?code=xxxx&state=yyyy
            var session = await client.Auth.ExchangeCodeForSession(_oauthState.PKCEVerifier, authCode);

            if (session == null)
                throw new Exception("세션 교환 실패");

            Debug.Log($"[OAuth] 로그인 성공: {session.User?.Email ?? session.User?.Id}");
            return session;
        }

        // ──────────────────────────────────────────
        // 콜백 수신
        // ──────────────────────────────────────────

        /*
        // 모바일: Deep Link로 수신
        private void OnDeepLinkActivated(string url)
        {
            if (!url.StartsWith(DeepLinkScheme)) return;
            Debug.Log($"[OAuth] Deep Link 수신: {url}");
            _callbackTcs?.TrySetResult(url);
        }
        */

        // PC: 로컬 HTTP 리스너
        private async Task<string> WaitForLocalCallback(System.Net.HttpListener listener)
        {
            using (listener)
            using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(3)))
            {
                try
                {
                    var contextTask = listener.GetContextAsync();
                    await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, cts.Token));

                    if (!contextTask.IsCompleted)
                        throw new TimeoutException("로그인 대기 시간 초과 (3분)");

                    var context = contextTask.Result;
                    var rawUrl = "http://localhost" + context.Request.RawUrl;
                    
                    // 브라우저를 GitHub Pages로 보내기
                    var response = context.Response;
                    response.StatusCode = 302;
                    response.RedirectLocation = "https://1seong.github.io/3dcubepainting.github.io/login-complete.html";
                    response.Close();
                    
                    // ... 응답 처리
                    return rawUrl;
                }
                catch (TaskCanceledException)
                {
                    throw new TimeoutException("로그인 대기 시간 초과 (3분)");
                }
                // using 블록 종료 시 listener.Dispose() → Stop() + Close() 자동 호출
            }
        }

        private static int GetAvailablePort()
        {
            // TcpListener port 0 → OS가 배정 → 번호 읽고 즉시 닫기
            var tcp = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcp.Start();
            int port = ((System.Net.IPEndPoint)tcp.LocalEndpoint).Port;
            tcp.Stop();
            return port;
        }
    }
}