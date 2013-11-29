using System;
using System.Linq;

namespace PostgresMigrate
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("THIS WILL DELETE ALL DATA IN THE SQL DATABASE. TO CONTINUE, TYPE 'YES'.");
            if ((Console.ReadLine() ?? "").ToLower() != "yes")
            {
                Console.WriteLine("Aborted.");
                return;
            }

            var cmd = new Command("TRUNCATE rohbot.chathistory; TRUNCATE rohbot.accounts; TRUNCATE rohbot.roomsettings;");
            cmd.ExecuteNonQuery();

            TransferAccounts();
            TransferRoomOptions();
            TransferChatHistory();
        }

        private static void TransferChatHistory()
        {
            long total = MgoDatabase.ChatHistory.Count();
            long count = 0;
            var timer = 0;

            Console.WriteLine("Transferring ChatHistory:");
            foreach (var line in MgoDatabase.ChatHistory.FindAll())
            {
                Command cmd;

                if (line is ChatLine)
                {
                    var chatLine = (ChatLine)line;
                    cmd = new Command("INSERT INTO rohbot.chathistory (type,date,chat,content,usertype,sender,senderid,senderstyle,ingame) VALUES(:type,:date,:chat,:content,:usertype,:sender,:senderid,:senderstyle,:ingame);");
                    cmd["usertype"] = chatLine.UserType;
                    cmd["sender"] = chatLine.Sender;
                    cmd["senderid"] = chatLine.SenderId;
                    cmd["senderstyle"] = chatLine.SenderStyle;
                    cmd["ingame"] = chatLine.InGame;
                }
                else if (line is StateLine)
                {
                    var stateLine = (StateLine)line;
                    cmd = new Command("INSERT INTO rohbot.chathistory (type,date,chat,content,state,\"for\",forid,by,byid) VALUES(:type,:date,:chat,:content,:state,:for,:forid,:by,:byid);");
                    cmd["state"] = stateLine.State;
                    cmd["for"] = stateLine.For;
                    cmd["forid"] = stateLine.ForId;
                    cmd["by"] = stateLine.By;
                    cmd["byid"] = stateLine.ById;
                }
                else
                {
                    throw new NotSupportedException(line.GetType().ToString());
                }

                cmd["type"] = line.Type;
                cmd["date"] = line.Date;
                cmd["chat"] = line.Chat;
                cmd["content"] = line.Content;
                cmd.ExecuteNonQuery();

                count++;
                timer++;
                if (timer == 1000)
                {
                    timer = 0;

                    var percent = (count / (double)total) * 100;
                    Console.WriteLine("{0} / {1} [{2:0.00}%]", count, total, percent);
                }
            }
            Console.WriteLine("Done!");
        }

        private static void TransferAccounts()
        {
            Console.Write("Transferring Accounts: ");
            foreach (var account in MgoDatabase.Accounts.FindAll())
            {
                var cmd = new Command("INSERT INTO rohbot.accounts (name,address,password,salt,defaultroom,enabledstyle) VALUES(:name,:address,:pass,:salt,:room,:style);");
                cmd["name"] = account.Name;
                cmd["address"] = account.Address ?? "127.0.0.1";
                cmd["pass"] = Convert.ToBase64String(account.Password);
                cmd["salt"] = Convert.ToBase64String(account.Salt);
                cmd["room"] = account.DefaultRoom;
                cmd["style"] = account.EnabledStyle;
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("Done!");
        }

        private static void TransferRoomOptions()
        {
            Console.Write("Transferring RoomSettings: ");
            foreach (var options in MgoDatabase.RoomBans.FindAll())
            {
                var cmd = new Command("INSERT INTO rohbot.roomsettings (room,bans,mods) VALUES(:room,:bans,:mods);");
                cmd["room"] = options.Room;
                cmd["bans"] = options.Bans.ToArray();
                cmd["mods"] = options.Mods.ToArray();
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("Done!");
        }
    }
}
