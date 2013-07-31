using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SteamMobile
{
    public static class Database
    {
        private const string ConnectionStringFormat = "mongodb://{2}:{3}@{0}/{1}?safe=true";

        private static MongoClient client;
        private static MongoServer server;
        private static MongoDatabase database;

        static Database()
        {
            client = new MongoClient(string.Format(ConnectionStringFormat, Settings.DbServer, Settings.DbName, Settings.DbUser, Settings.DbPass));
            server = client.GetServer();
            database = server.GetDatabase(Settings.DbName);

            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Descending(r => r.Date));
            ChatHistory.EnsureIndex(IndexKeys<HistoryLine>.Hashed(r => r.Chat));
        }

        public static MongoCollection<HistoryLine> ChatHistory
        {
            get { return GetCollection<HistoryLine>("ChatHistory"); }
        }

        private static MongoCollection<T> GetCollection<T>(string name, WriteConcern concern = null)
        {
            if (concern == null)
                concern = WriteConcern.Acknowledged;

            return database.GetCollection<T>(name, concern);
        }
    }
}
