using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RedHttpServerCore.Request;
using RedHttpServerCore.Response;

namespace StockManager
{
    /// <summary>
    ///     Session manager for cookie-based authentication.
    /// </summary>
    /// <typeparam name="TSess"></typeparam>
    public class SessionManager<TSess>
    {
        /// <summary>
        ///     The settings available for SameSite
        /// </summary>
        public enum SameSiteSetting
        {
            None,
            Lax,
            Strict
        }

        private readonly string _cookie;
        private readonly TimeSpan _sessionLength;
        private readonly ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();

        /// <summary>
        ///     The name of the session token cookie. Defaults to 'token'
        /// </summary>
        public string TokenName = "token";

        /// <summary>
        ///     Constructor for SessionManager
        /// </summary>
        /// <param name="sessionLength">The default length of a session</param>
        /// <param name="domain">The domain specified for the cookie</param>
        /// <param name="path">The path specified for the cookie</param>
        /// <param name="httpOnly">Whether the cookie should be unavailable to javascript</param>
        /// <param name="secure">Whether the session cookie only should be send with secure (https) requests</param>
        /// <param name="sameSite">The same-site policy specified for the cookie</param>
        public SessionManager(TimeSpan sessionLength, string domain = "", string path = "", bool httpOnly = true,
            bool secure = true, SameSiteSetting sameSite = SameSiteSetting.Lax)
        {
            _sessionLength = sessionLength;
            var d = domain == "" ? "" : $" Domain={domain};";
            var p = path == "" ? "" : $" Path={path};";
            var h = httpOnly ? " HttpOnly;" : "";
            var s = secure ? " Secure;" : "";
            var ss = sameSite == SameSiteSetting.None ? "" : $" SameSite={sameSite};";
            _cookie = $"{d}{p}{h}{s}{ss}";
            Maintain();
        }

        private async void Maintain()
        {
            var delay = TimeSpan.FromMinutes(_sessionLength.TotalMinutes * 0.26);
            while (true)
            {
                await Task.Delay(delay);
                var now = DateTime.UtcNow;
                var expired = _sessions.Where(kvp => kvp.Value.Expires < now).ToList();
                foreach (var kvp in expired)
                    _sessions.TryRemove(kvp.Key, out var s);
            }
        }

        /// <summary>
        /// Tries to authenticate a request by checking for cookie token and the verify the token
        /// </summary>
        /// <param name="req"></param>
        /// <param name="res"></param>
        /// <param name="redirect"></param>
        /// <param name="redirectUrl"></param>
        /// <param name="data"></param>
        /// <returns>The session data if the token is valid, default(TSess) otherwise</returns>
        public bool TryAuthenticateRequest(RRequest req, RResponse res, out TSess data, bool redirect = true, string redirectUrl = "/login.html")
        {
            if (req.Cookies.ContainsKey(TokenName) && req.Cookies[TokenName] != "" && _sessions.TryGetValue(req.Cookies[TokenName], out var s))
            {
                data = s.SessionData;
                return true;
            }
            data = default(TSess);
            if (redirect)
            {
                if (req.Cookies.ContainsKey(TokenName))
                    res.AddHeader("Set-Cookie", $"{TokenName}=;{_cookie} Expires=Thu, 01 Jan 1970 00:00:00 GMT");
                res.SendString("{\"redirect\":\"" + redirectUrl + "\"}", "text/json", status: 401);
            }
            return false;
        }
        

        /// <summary>
        ///     Creates a new session and returns the cookie to send the client with 'Set-Header'.
        /// </summary>
        /// <param name="sessionData"></param>
        /// <returns></returns>
        public string OpenSession(TSess sessionData)
        {
            var id = Guid.NewGuid().ToString("N").Substring(8);
            var exp = DateTime.UtcNow.Add(_sessionLength);
            _sessions.TryAdd(id, new Session(sessionData, exp));
            return $"{TokenName}={id};{_cookie} Expires={exp:R}";
        }

        /// <summary>
        ///     Renews the expiration of a active session and returns the cookie to send the client with 'Set-Cookie'. Returns
        ///     empty string if token invalid
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string RenewSession(string token)
        {
            if (_sessions.TryGetValue(token, out var sess))
            {
                sess.Expires = DateTime.UtcNow.Add(_sessionLength);
                return $"{TokenName}={token};{_cookie} Expires={sess.Expires:R}";
            }
            return "";
        }

        /// <summary>
        ///     Closes an active session so the token becomes invalid. Returns true if an active session was found
        /// </summary>
        /// <param name="token"></param>
        /// <param name="cookie">The cookie to return, to invalidate the existing cookie</param>
        /// <returns></returns>
        public bool CloseSession(string token, out string cookie)
        {
            cookie = $"{TokenName}=;{_cookie} Expires=Thu, 01 Jan 1970 00:00:00 GMT";
            return _sessions.TryRemove(token, out var s);
        }

        private class Session
        {
            internal Session(TSess tsess, DateTime exp)
            {
                SessionData = tsess;
                Expires = exp;
            }

            public TSess SessionData { get; }
            public DateTime Expires { get; set; }
        }
    }

    class SessionData
    {
        public string Key { get; set; }

        public SessionData(string key)
        {
            Key = key;
        }

        public override string ToString()
        {
            return Key;
        }

        public override int GetHashCode()
        {
            return Key.Length;
        }
    }
}