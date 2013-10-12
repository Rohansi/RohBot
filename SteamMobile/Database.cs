using MongoDB.Driver;
using MongoDB.Driver.Builders;
using SteamMobile.Rooms;

namespace SteamMobile
{
    public static class Database
    {
        private const string ConnectionStringFormat = "mongodb://{2}:{3}@{0}/{1}?safe=true";

        private static MongoClient _client;
        private static MongoServer _server;
        private static MongoDatabase _database;

        static Database()
        {
            var settings = Program.Settings;
            var connection = string.Format(ConnectionStringFormat, settings.DbServer, settings.DbName, settings.DbUser, settings.DbPass);

            _client = new MongoClient(connection);
            _server = _client.GetServer();
            _database = _server.GetDatabase(settings.DbName);

            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Descending(r => r.Date));
            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Hashed(r => r.Chat));

            Accounts.EnsureIndex(IndexKeys<Account>.Descending(r => r.NameLower), IndexOptions.SetUnique(true));
            Accounts.EnsureIndex(IndexKeys<Account>.Hashed(r => r.Address));

            LoginTokens.EnsureIndex(IndexKeys<LoginToken>.Hashed(r => r.Name));
            LoginTokens.EnsureIndex(IndexKeys<LoginToken>.Descending(r => r.Created));

            RoomBans.EnsureIndex(IndexKeys<RoomBans>.Hashed(r => r.Room));
        }

        private static MongoCollection<T> GetCollection<T>(string name, WriteConcern concern = null)
        {
            if (concern == null)
                concern = WriteConcern.Acknowledged;

            return _database.GetCollection<T>(name, concern);
        }

        public static MongoCollection<HistoryLine> ChatHistory
        {
            get { return GetCollection<HistoryLine>("ChatHistory"); }
        }

        public static MongoCollection<Account> Accounts
        {
            get { return GetCollection<Account>("Accounts"); }
        }

        public static MongoCollection<LoginToken> LoginTokens
        {
            get { return GetCollection<LoginToken>("LoginTokens"); }
        }

        public static MongoCollection<RoomBans> RoomBans
        {
            get { return GetCollection<RoomBans>("RoomBans"); }
        }
    }
}
