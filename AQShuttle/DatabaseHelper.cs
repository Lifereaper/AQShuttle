using System;
using MySql.Data.MySqlClient;

namespace AQShuttle
{
    public static class DatabaseHelper
    {
        // Added AllowPublicKeyRetrieval=True; to bypass the strict network security block
        private static string connectionString = "Server=192.168.1.196;Database=AQShuttleDB;Uid=aqadmin;Pwd=AQ2026AQ!;AllowPublicKeyRetrieval=True;";

        public static MySqlConnection GetConnection()
        {
            MySqlConnection conn = new MySqlConnection(connectionString);
            return conn;
        }
    }
}
