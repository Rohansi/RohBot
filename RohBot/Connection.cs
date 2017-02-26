using System;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;
using RohBot.Packets;
using RohBot.Rooms;

namespace RohBot
{
    public class Connection : WebSocketClient
    {
        public string Address { get; private set; }
        public string UserAgent { get; private set; }
        public bool IsMobile { get; private set; }
        public bool IsTokenLogin { get; private set; }
        public Session Session { get; set; }

        public void SendJoinRoom(Room room)
        {
            Send(new Chat
            {
                Method = "join",
                Name = room.RoomInfo.Name,
                ShortName = room.RoomInfo.ShortName
            });

            room.SendHistory(this);
        }

        public void SendLeaveRoom(Room room)
        {
            Send(new Chat
            {
                Method = "leave",
                Name = room.RoomInfo.Name,
                ShortName = room.RoomInfo.ShortName
            });
        }

        public void Login(string username, string password, string token)
        {
            bool loggedIn = false;
            Account account;
            string message;

            do
            {
                account = Account.Get(username);
                if (account == null)
                {
                    message = "Invalid username or password.";
                    break;
                }

                if (Session != null)
                {
                    message = "You are already logged in.";
                    break;
                }

                if (!Util.IsValidUsername(username))
                {
                    message = Util.InvalidUsernameMessage;
                    break;
                }

                if (string.IsNullOrEmpty(password))
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        message = "Missing password.";
                        break;
                    }

                    var loginToken = LoginToken.Find(account.Id, token);
                    if (loginToken == null)
                    {
                        message = "Automatic login failed. Login with your username and password.";
                        break;
                    }

                    loginToken.UpdateAccessed(UserAgent, Address);

                    IsTokenLogin = true;
                    loggedIn = true;
                    message = $"Logged in as {account.Name}.";
                }
                else
                {
                    if (!Util.IsValidPassword(password))
                    {
                        message = Util.InvalidPasswordMessage;
                        break;
                    }

                    var givenPassword = Convert.ToBase64String(Util.HashPassword(password, Convert.FromBase64String(account.Salt)));
                    if (givenPassword != account.Password)
                    {
                        account = null;
                        message = "Invalid username or password.";
                        break;
                    }

                    var newToken = new LoginToken
                    {
                        UserId = account.Id,
                        Created = Util.GetCurrentTimestamp(),
                        Accessed = Util.GetCurrentTimestamp(),
                        UserAgent = UserAgent,
                        Address = Address,
                        Token = Util.GenerateLoginToken(),
                    };

                    newToken.Insert();

                    IsTokenLogin = false;
                    token = newToken.Token;
                    loggedIn = true;
                    message = $"Logged in as {account.Name}.";
                }
            } while (false);

            if (loggedIn)
            {
                Send(new AuthenticateResponse
                {
                    Name = account.Name,
                    Tokens = token,
                    Success = true
                });

                var session = Program.SessionManager.GetOrCreate(account);
                session.Add(this);
            }

            SendSysMessage(message);
        }

        public void Register(string username, string password)
        {
            string message;

            username = username.Trim();

            do
            {
                if (Session != null)
                {
                    message = "You can not register while logged in.";
                    break;
                }

                if (!Util.IsValidUsername(username))
                {
                    message = Util.InvalidUsernameMessage;
                    break;
                }

                if (!Util.IsValidPassword(password))
                {
                    message = Util.InvalidPasswordMessage;
                    break;
                }

                var accountsFromAddress = Account.FindWithAddress(Address).Count();
                if (accountsFromAddress >= 3)
                {
                    message = "Too many accounts were created from this location.";
                    break;
                }

                var salt = Util.GenerateSalt();
                var account = new Account
                {
                    Address = Address,
                    Name = username,
                    Password = Convert.ToBase64String(Util.HashPassword(password, salt)),
                    Salt = Convert.ToBase64String(salt),
                    EnabledStyle = "",
                    Rooms = new string[0]
                };

                try
                {
                    account.Insert();
                }
                catch (NpgsqlException)
                {
                    message = "An account with that name already exists.";
                    break;
                }

                message = "Account created. You can now login.";
            } while (false);

            SendSysMessage(message);
        }

        public void SendSysMessage(string message)
        {
            Send(new SysMessage { Content = Util.HtmlEncode(message), Date = Util.GetCurrentTimestamp() });
        }

        public void Send(Packet packet)
        {
            Send(Packet.WriteToMessage(packet));
        }

        public void Send(string data)
        {
            SendAsync(data).Wait();
        }

        private static readonly Regex MobileUserAgent = new Regex(@"Android|iPhone|iPad|iPod|Windows Phone", RegexOptions.Compiled);

        protected override void OnOpen()
        {
            Session = null;

            try
            {
                if (IsLocal)
                    Address = Headers["X-Real-IP"] ?? "127.0.0.1";
                else
                    Address = EndPoint.Address.ToString();
            }
            catch
            {
                Address = "127.0.0.1";
            }
            
            try
            {
                UserAgent = Headers["User-Agent"] ?? "";
                IsMobile = MobileUserAgent.IsMatch(UserAgent);
            }
            catch
            {
                IsMobile = false;
            }
        }

        protected override void OnMessage(string message)
        {
            try
            {
                Packet.ReadFromMessage(message).Handle(this);
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Bad packet from {Address}: {message}", ex);
            }
        }

        protected override void OnError(Exception exception)
        {
            Program.Logger.Error($"Socket error from {Address}:", exception);
        }
    }
}
