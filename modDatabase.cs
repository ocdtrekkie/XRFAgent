﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace XRFAgent
{
    internal class modDatabase
    {
        private static SQLiteConnection conn;

        /// <summary>
        /// Loads the database module: Creates database connection and creates tables
        /// </summary>
        public static void Load()
        {
            conn = new SQLiteConnection(Properties.Settings.Default.Database_FileURI);
            conn.ExecuteScalar<int>("PRAGMA journal_mode = WAL;");
            conn.CreateTable<Config>();
            conn.CreateTable<InstalledSoftware>();
            conn.CreateTable<LocalQueue>();
        }

        /// <summary>
        /// Unloads the database module: Closes database connection
        /// </summary>
        public static void Unload()
        {
            conn.Close();
            conn.Dispose();
        }

        /// <summary>
        /// Class defining the CONFIG table
        /// </summary>
        [Table("CONFIG")]
        public class Config
        {
            [PrimaryKey, MaxLength(100)]
            public string Key { get; set; }

            public string Value { get; set; }
        }

        /// <summary>
        /// Adds a new setting to the CONFIG table
        /// </summary>
        /// <param name="config">(Config) Setting to add</param>
        /// <returns>(int) Number of rows added</returns>
        public static int AddConfig(Config config)
        {
            int result = conn.Insert(config);
            return result;
        }

        /// <summary>
        /// Updates an existing setting in the CONFIG table
        /// </summary>
        /// <param name="config">(Config) Setting to update</param>
        /// <returns>(int) Number of rows updated</returns>
        public static int UpdateConfig(Config config)
        {
            int result = 0;
            result = conn.Update(config);
            return result;
        }

        /// <summary>
        /// Updates an existing setting in the CONFIG table or adds it if it does not exist
        /// </summary>
        /// <param name="config">(Config) Setting to add or update</param>
        /// <returns>(int) Number of rows updated</returns>
        public static int AddOrUpdateConfig(Config config)
        {
            int result = UpdateConfig(config);
            if (result == 0)
            {
                result = AddConfig(config);
            }
            return result;
        }

        /// <summary>
        /// Gets the value of a setting in the CONFIG table
        /// </summary>
        /// <param name="key">(string) Name of setting</param>
        /// <returns>(string) Value of setting</returns>
        public static string GetConfig(string key)
        {
            var Value = from c in conn.Table<Config>()
                        where c.Key == key
                        select c.Value;
            return Value.FirstOrDefault();
        }

        /// <summary>
        /// Class defining the LOCALQUEUE table
        /// </summary>
        [Table("LOCALQUEUE")]
        public class LocalQueue
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Src { get; set; }

            public string Auth { get; set; }

            public string Dest { get; set; }

            public string Mesg { get; set; }

            public bool Recv { get; set; }
        }

        public static int EnqueueLocalMessage(LocalQueue localQueue)
        {
            int result = conn.Insert(localQueue);
            return result;
        }

        /// <summary>
        /// Class defining the INSTALLEDSOFTWARE table
        /// </summary>
        [Table("INSTALLEDSOFTWARE")]
        public class InstalledSoftware
        {
            [PrimaryKey]
            public string Name { get; set; }

            public string Version { get; set; }

            public string Publisher { get; set; }

            public string InstallDate { get; set; }
        }

        /// <summary>
        /// Adds a new application to the INSTALLEDSOFTWARE table
        /// </summary>
        /// <param name="software">(InstalledSoftware) Application to add</param>
        /// <returns>(int) Number of rows added</returns>
        public static int AddSoftware(InstalledSoftware software)
        {
            int result = conn.Insert(software);
            return result;
        }

        /// <summary>
        /// Updates an existing application in the INSTALLEDSOFTWARE table
        /// </summary>
        /// <param name="software">(InstalledSoftware) Application to update</param>
        /// <returns>(int) Number of rows updated</returns>
        public static int UpdateSoftware(InstalledSoftware software)
        {
            int result = 0;
            result = conn.Update(software);
            return result;
        }

        /// <summary>
        /// Dumps the current contents of the INSTALLEDSOFTWARE table
        /// </summary>
        public static void TruncateSoftware()
        {
            conn.DeleteAll<InstalledSoftware>();
        }
    }
}
