using System;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamMobile
{
    class ClanNameCallback : CallbackMsg
    {
        public SteamID ClanID { get; private set; }
        public string Name { get; private set; }
        public byte[] Avatar { get; private set; }

        public ClanNameCallback(CMsgClientClanState body)
        {
            ClanID = body.steamid_clan;
            Name = body.name_info.clan_name;
            Avatar = body.name_info.sha_avatar;
        }
    }

    class SteamHandlers : ClientMsgHandler
    {
        public override void HandleMsg(IPacketMsg packetMsg)
        {
            switch (packetMsg.MsgType)
            {
                case EMsg.ClientClanState:
                    {
                        var clanMsg = new ClientMsgProtobuf<CMsgClientClanState>(packetMsg);

                        if (clanMsg.Body.name_info == null)
                            return; // we only care about the name

                        var callback = new ClanNameCallback(clanMsg.Body);
                        Client.PostCallback(callback);
                        break;
                    }
            }
        }
    }
}
