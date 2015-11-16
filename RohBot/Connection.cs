using System;
using System.Collections.Generic;
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
        public bool IsMobile { get; private set; }
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

        public void Login(string username, string password, List<string> tokens)
        {
            Account account = null;
            string message;

            do
            {
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

                var existingTokens = LoginToken.FindAll(username).ToList();

                if (String.IsNullOrEmpty(password))
                {
                    if (tokens.Count == 0)
                    {
                        message = "Missing password.";
                        break;
                    }

                    if (!existingTokens.Any(t => t.Address == Address && tokens.Contains(t.Token)))
                    {
                        message = "Automatic login failed. Login with your username and password.";
                        break;
                    }

                    account = Account.Get(username);
                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = $"Logged in as {account.Name}.";
                }
                else
                {
                    if (!Util.IsValidPassword(password))
                    {
                        message = Util.InvalidPasswordMessage;
                        break;
                    }

                    account = Account.Get(username);
                    if (account == null)
                    {
                        message = "Invalid username or password.";
                        break;
                    }

                    var givenPassword = Convert.ToBase64String(Util.HashPassword(password, Convert.FromBase64String(account.Salt)));
                    if (givenPassword != account.Password)
                    {
                        account = null;
                        message = "Invalid username or password.";
                        break;
                    }

                    LoginToken newToken = existingTokens.FirstOrDefault(t => t.Address == Address);
                    if (newToken == null)
                    {
                        newToken = new LoginToken
                        {
                            Name = account.Name.ToLower(),
                            Address = Address,
                            Token = Util.GenerateLoginToken(),
                            Created = Util.GetCurrentTimestamp()
                        };

                        newToken.Insert();
                        existingTokens.Add(newToken);
                    }

                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = $"Logged in as {account.Name}.";
                }
            } while (false);

            if (account != null)
            {
                Send(new AuthenticateResponse
                {
                    Name = account.Name,
                    Tokens = string.Join(",", tokens),
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
                IsMobile = MobileUserAgent.IsMatch(Headers["User-Agent"]);
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
