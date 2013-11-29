using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Npgsql;

namespace PostgresMigrate
{
    static class Database
    {
        private static List<NpgsqlConnection> _connections;

        static Database()
        {
            _connections = new List<NpgsqlConnection>();
        }

        public static NpgsqlConnection CreateConnection()
        {
            var conn = _connections.FirstOrDefault(c => c.State == ConnectionState.Open);

            if (conn == null)
            {
                var connectionStr = string.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};Encoding=UNICODE;",
                    "127.0.0.1", "5432", "server", "", "rohbot");

                conn = new NpgsqlConnection(connectionStr);
                conn.Open();
                return conn;
            }

            lock (_connections)
                _connections.Remove(conn);

            return conn;
        }

        public static void RecycleConnection(NpgsqlConnection conn)
        {
            lock (_connections)
                _connections.Add(conn);
        }
    }

    class Command
    {
        private NpgsqlConnection _connection;
        private readonly NpgsqlCommand _command;

        public Command(string sql)
        {
            _connection = Database.CreateConnection();
            _command = new NpgsqlCommand(sql, _connection);
        }

        public object this[string name]
        {
            set
            {
                var idx = _command.Parameters.IndexOf(name);
                if (idx != -1)
                    _command.Parameters[idx].Value = value;
                _command.Parameters.AddWithValue(name, value);
            }
        }

        public IEnumerable<dynamic> Execute()
        {
            NpgsqlDataReader reader = null;

            try
            {
                reader = _command.ExecuteReader();

                var names = new string[reader.FieldCount];
                var values = new object[reader.FieldCount];

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    names[i] = reader.GetName(i);
                }

                while (reader.Read())
                {
                    var no = reader.GetValues(values);
                    yield return new Result(names, values);
                }
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();

                Database.RecycleConnection(_connection);
                _connection = null;
            }
        }

        public void ExecuteNonQuery()
        {
            try
            {
                _command.ExecuteNonQuery();
            }
            finally
            {
                Database.RecycleConnection(_connection);
                _connection = null;
            }
        }

        public object ExecuteScalar()
        {
            try
            {
                return _command.ExecuteScalar();
            }
            finally
            {
                Database.RecycleConnection(_connection);
                _connection = null;
            }
        }
    }

    class Result : DynamicObject
    {
        private readonly Dictionary<string, object> _columns;

        public Result(string[] names, object[] values)
        {
            _columns = new Dictionary<string, object>();

            for (var i = 0; i < names.Length; i++)
            {
                _columns.Add(names[i], values[i]);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return _columns.TryGetValue(binder.Name, out result);
        }
    }
}
