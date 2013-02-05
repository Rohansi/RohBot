using System;
using SteamKit2;

namespace SteamMobile
{
    class SteamUtil
    {
        public static SteamID ChatFromClan(SteamID clanId)
        {
            if (!clanId.IsClanAccount)
                throw new ArgumentException("clanId is not a clan account");

            SteamID chatId = clanId.ConvertToUInt64();

            chatId.AccountInstance = (uint)SteamID.ChatInstanceFlags.Clan;
            chatId.AccountType = EAccountType.Chat;

            return chatId;
        }

        public static SteamID ClanFromChat(SteamID chatId)
        {
            if (!chatId.IsChatAccount)
                throw new ArgumentException("chatId is not a chat account");

            SteamID clanId = chatId.ConvertToUInt64();

            clanId.AccountInstance = 0;
            clanId.AccountType = EAccountType.Clan;

            return clanId;
        }
    }
}
