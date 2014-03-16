using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using SteamMobile.Packets;
using SteamMobile.Rooms;
using SuperWebSocket;

namespace SteamMobile
{
    public class Connection : WebSocketSession<Connection>
    {
        public string Address { get; private set; }
        public Session Session;

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
                    message = String.Format("Logged in as {0}.", account.Name);
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
                            Created = Util.GetCurrentUnixTimestamp()
                        };

                        newToken.Insert();
                        existingTokens.Add(newToken);
                    }

                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = String.Format("Logged in as {0}.", account.Name);
                }
            } while (false);

            if (account != null)
            {
                var session = Program.SessionManager.GetOrCreate(account);
                session.Add(this);

                Send(new AuthenticateResponse
                {
                    Name = account.Name,
                    Tokens = string.Join(",", tokens),
                    Success = true
                });
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
            Send(new SysMessage { Content = Util.HtmlEncode(message), Date = Util.GetCurrentUnixTimestamp() });
        }

        public void Send(Packet packet)
        {
            Send(Packet.WriteToMessage(packet));
        }

        protected override void OnSessionStarted()
        {
            Session = null;

            try
            {
                var isLocal = RemoteEndPoint.Address.ToString() == "127.0.0.1";
                Address = isLocal ? Items["X-Real-IP"].ToString() : RemoteEndPoint.Address.ToString();
            }
            catch
            {
                Address = "127.0.0.1";
            }
        }
    }
}
