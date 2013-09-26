using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Web;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace SteamMobile
{
    public class LoginToken
    {
        public ObjectId Id;
        public string Token;
        public string Address;
        public long Created;
        public long LastUsed;
        public string SteamId;
    }

    public class LoginServer
    {
        private const string SteamOpenId = "https://steamcommunity.com/openid/login";

        private Thread _thread;
        private Random _random;

        public LoginServer()
        {
            _random = new Random();
            _thread = new Thread(LoginServerThread);
            _thread.Start();
        }

        public void LoginServerThread()
        {
            var server = new HttpListener();
            server.Prefixes.Add("http://localhost:12001/");
            server.Start();

            while (true)
            {
                try
                {
                    var context = server.GetContext();
                    var request = context.Request;
                    var response = context.Response;

                    var requestedUrl = request.Url.LocalPath;
                    var requestQuery = HttpUtility.ParseQueryString(request.Url.Query);

                    switch (requestedUrl)
                    {
                        // generates the login url and redirects the user to it
                        case "/":
                        {
                            var openidArgs = HttpUtility.ParseQueryString("");
                            openidArgs.Add("openid.ns", "http://specs.openid.net/auth/2.0");
                            openidArgs.Add("openid.mode", "checkid_setup");
                            openidArgs.Add("openid.return_to", Program.Settings.Host + "login/validate/");
                            openidArgs.Add("openid.realm", Program.Settings.Host);
                            openidArgs.Add("openid.identity", "http://specs.openid.net/auth/2.0/identifier_select");
                            openidArgs.Add("openid.claimed_id", "http://specs.openid.net/auth/2.0/identifier_select");

                            response.RedirectLocation = SteamOpenId + "?" + openidArgs;
                            break;
                        }

                        // openid provider redirects to this page where we confirm the login
                        case "/validate/":
                        {
                            var validateArgs = HttpUtility.ParseQueryString("");
                            validateArgs.Add("openid.ns", "http://specs.openid.net/auth/2.0");
                            validateArgs.Add("openid.assoc_handle", requestQuery["openid.assoc_handle"]);
                            validateArgs.Add("openid.signed", requestQuery["openid.signed"]);
                            validateArgs.Add("openid.sig", requestQuery["openid.sig"]);
                            validateArgs.Add("openid.mode", "check_authentication");

                            var signedArgs = requestQuery["openid.signed"].Split(',');
                            foreach (var signed in signedArgs)
                            {
                                validateArgs["openid." + signed] = requestQuery["openid." + signed];
                            }

                            bool valid = false;
                            string steamId = Regex.Match(validateArgs["openid.claimed_id"], @"http://steamcommunity\.com/openid/id/(\d+)").Groups[1].Value;

                            try
                            {
                                var validateData = Encoding.UTF8.GetBytes(validateArgs.ToString());
                                var webRequest = WebRequest.Create(SteamOpenId);
                                webRequest.Method = "POST";
                                webRequest.ContentType = "application/x-www-form-urlencoded";
                                webRequest.ContentLength = validateData.Length;
                                using (var requestStream = webRequest.GetRequestStream())
                                    requestStream.Write(validateData, 0, validateData.Length);

                                string webResponseText;
                                using (var webResponse = webRequest.GetResponse())
                                using (var webResponseStream = webResponse.GetResponseStream())
                                using (var reader = new StreamReader(webResponseStream, Encoding.UTF8))
                                    webResponseText = reader.ReadToEnd();

                                valid = webResponseText.Contains("is_valid:true");

                                if (!valid)
                                {
                                    Program.Logger.WarnFormat("Validate Failed from {0}: {1}", request.Headers["X-Real-IP"], webResponseText);
                                }
                            }
                            catch (Exception e)
                            {
                                Program.Logger.Error("Steam OpenID Error", e);
                            }

                            if (valid)
                            {
                                var existingCookies = request.Cookies.Cast<Cookie>().ToList();
                                string tokenName;
                                do
                                {
                                    tokenName = "session" + _random.Next();
                                } while (existingCookies.Any(i => i.Name == tokenName));

                                var token = Database.LoginTokens.AsQueryable().FirstOrDefault(r => r.Address == request.Headers["X-Real-IP"] && r.SteamId == steamId);
                                if (token == null)
                                {
                                    var tokenBuffer = new byte[32];
                                    _random.NextBytes(tokenBuffer);

                                    token = new LoginToken
                                    {
                                        Token = Convert.ToBase64String(tokenBuffer),
                                        Address = request.Headers["X-Real-IP"],
                                        Created = Util.GetCurrentUnixTimestamp(),
                                        LastUsed = Util.GetCurrentUnixTimestamp(),
                                        SteamId = steamId
                                    };

                                    Database.LoginTokens.Insert(token);
                                }

                                var cookie = new Cookie(tokenName, token.Token)
                                {
                                    Path = "/",
                                    Expires = DateTime.UtcNow.AddDays(14)
                                };
                                response.Cookies.Add(cookie);
                            }

                            response.RedirectLocation = Program.Settings.Host;
                            break;
                        }

                        default:
                            response.RedirectLocation = Program.Settings.Host;
                            break;
                    }

                    var responseBuffer = Encoding.UTF8.GetBytes("Redirected");
                    response.StatusCode = 302;
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentLength64 = responseBuffer.Length;
                    response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                    response.OutputStream.Close();
                }
                catch (Exception e)
                {
                    Program.Logger.Error("LoginServer Error", e);
                }
            }
        }
    }
}
