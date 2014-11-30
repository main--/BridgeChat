using System;
using System.Data;
using Mono.Data.Sqlite;

namespace BridgeChat.Core
{
    public static class DatastoreUtils
    {
        public static void SetParameter(this IDbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }

        // This is off by default.
        // WTF, sqlite!?
        public static SqliteConnection EnableForeignKeys(this SqliteConnection sc)
        {
            sc.Open();
            var cmd = sc.CreateCommand();
            cmd.CommandText = "pragma foreign_keys=on;";
            cmd.ExecuteNonQuery();
            return sc;
        }
    }
}

