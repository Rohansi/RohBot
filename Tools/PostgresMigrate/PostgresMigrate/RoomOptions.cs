using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace PostgresMigrate
{
    public class RoomOptions
    {
        public ObjectId Id;
        public long NewId;
        public string Room;
        public HashSet<string> Bans;
        public HashSet<string> Mods;
    }
}
