using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace PostgresMigrate
{
    public static class MgoDatabase
    {
        private const string ConnectionStringFormat = "mongodb://{2}:{3}@{0}/{1}?safe=true";

        private static MongoClient _client;
        private static MongoServer _server;
        private static MongoDatabase _database;

        static MgoDatabase()
        {
            var connection = string.Format(ConnectionStringFormat,
                "127.0.0.1", "rohbot", "server", "");

            _client = new MongoClient(connection);
            _server = _client.GetServer();
            _database = _server.GetDatabase("rohbot");

            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Descending(r => r.Date));
            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Hashed(r => r.Chat));

            Accounts.EnsureIndex(IndexKeys<Account>.Descending(r => r.NameLower), IndexOptions.SetUnique(true));
            Accounts.EnsureIndex(IndexKeys<Account>.Hashed(r => r.Address));

            RoomBans.EnsureIndex(IndexKeys<RoomOptions>.Hashed(r => r.Room));
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

        public static MongoCollection<RoomOptions> RoomBans
        {
            get { return GetCollection<RoomOptions>("RoomBans"); }
        }
    }
}
