using System.Collections.Generic;
using System.Linq;
using Fleck;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace SteamMobile
{
    public class Session
    {
        public Account Account;
        public string Room;
        
        public readonly IWebSocketConnection Socket;
        public readonly string Address;

        public Session(IWebSocketConnection socket)
        {
            Account = null;

            Socket = socket;
            Address = socket.ConnectionInfo.ClientIpAddress;
        }

        public void Login(string username, string password, List<string> tokens)
        {
            if (username.ToLower() == "guest")
            {
                Room = Program.Settings.DefaultRoom;
                var room = Program.RoomManager.Get(Room);
                if (room != null)
                    room.SendHistory(this);
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

                var usernameLower = username.ToLower();
                var existingTokens = Database.LoginTokens.AsQueryable().Where(r => r.Name == usernameLower).ToList();

                if (string.IsNullOrEmpty(password))
                {
                    if (tokens.Count == 0)
                    {
                        message = "Missing password.";
                        break;
                    }
                    
                    if (!existingTokens.Any(t => t.Address == Address && tokens.Contains(t.Token)))
                    {
                        message = "Automatic login failed.";
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

                    var givenPassword = Util.HashPassword(password, account.Salt);
                    if (!givenPassword.SequenceEqual(account.Password))
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
                        Database.LoginTokens.Insert(newToken);
                        existingTokens.Add(newToken);
                    }

                    Account = account;
                    tokens = existingTokens.Select(t => t.Token).ToList();
                    message = string.Format("Logged in as {0}.", Account.Name);
                }
            } while (false);

            if (Account != null && Account.DefaultRoom != Room)
            {
                Room = Account.DefaultRoom;

                var room = Program.RoomManager.Get(Room);
                if (room != null)
                    room.SendHistory(this);
            }

            Send(new Packets.SysMessage
            {
                Date = Util.GetCurrentUnixTimestamp(),
                Content = message
            });

            Send(new Packets.AuthenticateResponse
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

                var accountsFromAddress = Database.Accounts.AsQueryable().Count(a => a.Address == Address);
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
                    NameLower = username.ToLower(),
                    Password = Util.HashPassword(password, salt),
                    Salt = salt,
                    DefaultRoom = Program.Settings.DefaultRoom
                };

                try
                {
                    Database.Accounts.Insert(account);
                }
                catch (WriteConcernException)
                {
                    message = "An account with that name already exists.";
                    break;
                }

                message = "Account created. You can now login.";
            } while (false);

            Send(new Packets.SysMessage
            {
                Date = Util.GetCurrentUnixTimestamp(),
                Content = message
            });
        }

        public void Send(Packet packet)
        {
            Socket.Send(Packet.WriteToMessage(packet));
        }

        public void SendRaw(string str)
        {
            Socket.Send(str);
        }
    }
}
