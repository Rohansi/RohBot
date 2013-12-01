using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using SteamMobile.Packets;
using SuperWebSocket;

namespace SteamMobile
{
    public class Session : WebSocketSession<Session>
    {
        public Account Account;
        public string Room;

        public string Address { get; private set; }

        protected override void OnSessionStarted()
        {
            Account = null;

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

        public void Login(string username, string password, List<string> tokens, string roomOverride)
        {
            if (username.ToLower() == "guest")
            {
                Account = null;
                Room = string.IsNullOrWhiteSpace(roomOverride) ? Program.Settings.DefaultRoom : roomOverride;
                SwitchRoom(Room);

                Send(new AuthenticateResponse
                {
                    Name = "Guest",
                    Success = false,
                    Tokens = ""
                });

                return;
            }

            string message;

            do
            {
                if (!Util.IsValidUsername(username))
                {
                    message = Util.InvalidUsernameMessage;
                    break;
                }

                var existingTokens = LoginToken.FindAll(username).ToList();

                if (string.IsNullOrEmpty(password))
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
                    
                    Account = Account.Get(username);
                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = string.Format("Logged in as {0}.", Account.Name);
                }
                else
                {
                    if (!Util.IsValidPassword(password))
                    {
                        message = Util.InvalidPasswordMessage;
                        break;
                    }

                    var account = Account.Get(username);
                    if (account == null)
                    {
                        message = "Invalid username or password.";
                        break;
                    }

                    var givenPassword = Convert.ToBase64String(Util.HashPassword(password, Convert.FromBase64String(account.Salt)));
                    if (givenPassword != account.Password)
                    {
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

                    Account = account;
                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = string.Format("Logged in as {0}.", Account.Name);
                }
            } while (false);

            if (Account != null)
            {
                Room = string.IsNullOrWhiteSpace(roomOverride) ? Account.DefaultRoom : roomOverride;
                SwitchRoom(Room);
            }

            SendSysMessage(message);

            Send(new AuthenticateResponse
            {
                Name = Account == null ? "Guest" : Account.Name,
                Success = Account != null,
                Tokens = string.Join(",", tokens)
            });
        }

        public void Register(string username, string password)
        {
            string message;

            do
            {
                if (Account != null)
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
                    DefaultRoom = Program.Settings.DefaultRoom,
                    EnabledStyle = ""
                };

                try
                {
                    account.Insert();
                }
                catch (NpgsqlException e)
                {
                    Console.WriteLine(e);
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

        public bool SwitchRoom(string newRoom)
        {
            if (string.IsNullOrEmpty(newRoom))
                newRoom = Account != null ? Account.DefaultRoom : Program.Settings.DefaultRoom;
            newRoom = newRoom.ToLower();

            if (Room == newRoom)
                return true;

            var room = Program.RoomManager.Get(newRoom);
            if (room == null)
            {
                SendSysMessage("Room does not exist.");
                return false;
            }

            Room = newRoom;
            room.SendHistory(this);
            return true;
        }
    }
}
