using System.Collections.Generic;
using System.Dynamic;
using Npgsql;

namespace RohBot
{
    public static class Database
    {
        private static NpgsqlConnectionStringBuilder _connectionStr;

        static Database()
        {
            _connectionStr = new NpgsqlConnectionStringBuilder
            {
                Host = Program.Settings.DbAddress,
                Port = Program.Settings.DbPort,
                Database = Program.Settings.DbName,
                UserName = Program.Settings.DbUser,
                Password = Program.Settings.DbPass,

                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 20
            };
        }

        public static NpgsqlConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionStr);
            connection.Open();
            return connection;
        }
    }

    public class SqlCommand
    {
        private readonly NpgsqlCommand _command;

        public SqlCommand(string sql)
        {
            _command = new NpgsqlCommand(sql, Database.CreateConnection());
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
            using (_command.Connection)
            using (var reader = _command.ExecuteReader())
            {
                var names = new string[reader.FieldCount];
                var values = new object[reader.FieldCount];

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    names[i] = reader.GetName(i);
                }

                while (reader.Read())
                {
                    reader.GetValues(values);
                    yield return new SqlResult(names, values);
                }
            }
        }

        public void ExecuteNonQuery()
        {
            using (_command.Connection)
                _command.ExecuteNonQuery();
        }

        public object ExecuteScalar()
        {
            using (_command.Connection)
                return _command.ExecuteScalar();
        }
    }

    public class SqlResult : DynamicObject
    {
        private readonly Dictionary<string, object> _columns;

        public SqlResult(IList<string> names, IList<object> values)
        {
            _columns = new Dictionary<string, object>();

            for (var i = 0; i < names.Count; i++)
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
