using System;
using MySql.Data.MySqlClient;

namespace AQShuttle
{
    public static class DatabaseHelper
    {
        // This is the master key to your entire system!
        // We will update this string once your Host Machine is ready.
        private static string connectionString = "Server=192.168.1.196;Database=AQShuttleDB;Uid=aqadmin;Pwd=aq123";

        // A simple method that all your screens can use to get a fresh, open connection to the database
        public static MySqlConnection GetConnection()
        {
            MySqlConnection conn = new MySqlConnection(connectionString);
            return conn;
        }
    }
}