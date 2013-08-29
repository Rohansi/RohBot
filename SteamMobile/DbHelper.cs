using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile
{
    public static class DbHelper
    {
        public static HistoryLine RowToChatLine(dynamic result)
        {
            switch ((string)result.Type)
            {
                case "chat":
                    return new ChatLine(result.Date, result.Chat, result.UserType, result.Sender, result.SenderId, result.Content, result.InGame);
                case "state":
                    return new StateLine(result.Date, result.Chat, result.State, result.For, result.ForId, result.Bt, result.ById, result.Content);
                default:
                    throw new NotImplementedException();
            }
        }

        public static void Log(HistoryLine line)
        {
            SqlCommand cmd;
            
            switch (line.Type)
            {
                case "chat":
                break;
                case "state":
                break;
                default:
                    throw new NotImplementedException();
            }= new SqlCommand("INSERT INTO chathistory (Type,Date,Chat,Content,UserType,Sender,SenderId,InGame) VALUES (@Type,@Date,@Chat,@Content,@UserType,@Sender,@SenderId,@InGame)");
            
        }
    }
}
