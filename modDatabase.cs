using System;
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
        public static SQLiteConnection conn;

        public static void Close_DB()
        {
            conn.Close();
            conn.Dispose();
        }

        public static void Connect_DB()
        {
            conn = new SQLiteConnection(Properties.Settings.Default.Database_FileURI);
            conn.CreateTable<Config>();
        }

        [Table("CONFIG")]
        public class Config
        {
            [PrimaryKey, MaxLength(100)]
            public string Key { get; set; }

            public string Value { get; set; }
        }

        public int AddConfig(Config config)
        {
            int result = conn.Insert(config);
            return result;
        }

        public int UpdateConfig(Config config)
        {
            int result = 0;
            result = conn.Update(config);
            return result;
        }

        public int AddOrUpdateConfig(Config config)
        {
            int result = UpdateConfig(config);
            if (result == 0)
            {
                result = AddConfig(config);
            }
            return result;
        }

        public string GetConfig(string key)
        {
            var Value = from c in conn.Table<Config>()
                        where c.Key == key
                        select c.Value;
            return Value.FirstOrDefault();
        }
    }
}
