using System;
using ProtoBuf;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Data.Sqlite;

namespace BridgeChat.Core
{
    public class MainClass
    {
        public static void Main(string[] args)
        {
            using (var datastore = new SystemDatastore(new SqliteConnection("URI=file:core.sqlite").EnableForeignKeys())) {
                //datastore.CreateSchema();
                var server = new ChatServer(new IPEndPoint(IPAddress.Loopback, 31337), datastore);
                var runTask = server.Run();
                Console.ReadKey(true);
                server.StopListening();
                runTask.Wait();
            }
        }
    }
}
