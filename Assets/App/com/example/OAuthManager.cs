
using System;
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
            _redirectUrl = Application.platform == RuntimePlatform.WindowsPlayer
                                 || Application.platform == RuntimePlatform.OSXPlayer
                                 || Application.platform == RuntimePlatform.LinuxPlayer
                ? $"http://localhost:{port}/callback"
                : DeepLinkScheme;

            // OAuth URL 요청 (PKCE 자동 처리됨)
            var signInOptions = new SignInOptions
            {
                RedirectTo = _redirectUrl,
                FlowType = Constants.OAuthFlowType.PKCE,
            };

            _oauthState = await client.Auth.SignIn(provider, signInOptions);

            if (_oauthState?.Uri == null)
                throw new Exception("OAuth URL 생성 실패");

            // 브라우저 열기
            Application.OpenURL(_oauthState.Uri.ToString());

            // 콜백 대기
            _callbackTcs = new TaskCompletionSource<string>();

            string callbackUrl;
            if (_redirectUrl.StartsWith("http://localhost"))
                callbackUrl = await WaitForLocalCallback();
            else
                callbackUrl = await _callbackTcs.Task;

            // URL에서 code 추출 후 세션 교환
            var session = await ExchangeCodeForSession(client, callbackUrl);
            return session;
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
        private async Task<string> WaitForLocalCallback()
        {
            using var listener = new System.Net.HttpListener();
            listener.Prefixes.Add(_redirectUrl);
            listener.Start();

            Debug.Log($"[OAuth] 로컬 콜백 대기 중 (port {_redirectUrl})...");

            var context = await listener.GetContextAsync();
            string rawUrl = "http://localhost" + context.Request.RawUrl;

            // 브라우저에 완료 메시지 표시
            var response = context.Response;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(
                "<html><body><h2>로그인 완료! 게임으로 돌아가세요.</h2></body></html>"
            );
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            listener.Stop();
            return rawUrl;
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