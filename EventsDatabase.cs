using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordSharp.Objects;

namespace DiscordSharpTest
{
    class EventsDatabase
    {
        private SQLiteConnection _dbConnection { get; set; }

        public EventsDatabase()
        {
            Connect();
        }

        private void CreateDatabase()
        {
            //ExecuteSQLNonQuery(sql, _dbConnection);
            string sql = "CREATE TABLE alerts (GUID VARCHAR(20), destinationName VARCHAR(40), factionName VARCHAR(30), missionName VARCHAR(20), credits INT, lootName VARCHAR(30), lootQuantity INT, minLevel INT, maxLevel INT, startTime DATETIME, expireTime DATETIME)";
            ExecuteSQLNonQuery(sql);
            //Table to hold message/alert associations, as well as the time which a message was originally posted.
            sql = "CREATE TABLE alertMessageHistory (alertID VARCHAR(20), messageID VARCHAR(30), postTime DATETIME)";
            ExecuteSQLNonQuery(sql);
        }

        void Connect()
        {
#if DEBUG
            //SQLiteConnection.CreateFile("WarframeEvents.sqlite");
#endif
            _dbConnection = new SQLiteConnection("Data Source=WarframeEvents.sqlite;Version=3;");
            _dbConnection.Open();
            //if (_dbConnection != null) CreateDatabase();
        }

        private int ExecuteSQLNonQuery(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            return command.ExecuteNonQuery();
        }

        public void AddAlert(WarframeAlert alert)
        {
            if (_dbConnection != null)
            {
                ExecuteSQLNonQuery(
                    $"INSERT INTO alerts (GUID, destinationName, factionName, missionName, credits, lootName, lootQuantity, minLevel, maxLevel, startTime, expireTime)" +
                    $" VALUES ('{alert.GUID}', '{alert.DestinationName}', '{alert.MissionDetails.Faction}', '{alert.MissionDetails.MissionType}', {alert.MissionDetails.Credits}, '{alert.MissionDetails.Reward}', {alert.MissionDetails.RewardQuantity}, {alert.MissionDetails.MinimumLevel}, {alert.MissionDetails.MaximumLevel}, '{alert.StartTime:yyyy-MM-dd HH:mm:ss}', '{alert.ExpireTime:yyyy-MM-dd HH:mm:ss}')");
            }
        }

        [Obsolete]
        public void AddAlert(WarframeAlert alert, string messageID)
        {
            if (_dbConnection != null)
            {
                ExecuteSQLNonQuery(
                    $"INSERT INTO alerts (GUID, destinationName, factionName, missionName, credits, lootName, lootQuantity, minLevel, maxLevel, startTime, expireTime)" +
                    $" VALUES ('{alert.GUID}', '{alert.DestinationName}', '{alert.MissionDetails.Faction}', '{alert.MissionDetails.MissionType}', {alert.MissionDetails.Credits}, '{alert.MissionDetails.Reward}', {alert.MissionDetails.RewardQuantity}, {alert.MissionDetails.MinimumLevel}, {alert.MissionDetails.MaximumLevel}, '{alert.StartTime:yyyy-MM-dd HH:mm:ss}', '{alert.ExpireTime:yyyy-MM-dd HH:mm:ss}')");
            }
        }
        [Obsolete]
        public void DeleteAlert(WarframeAlert alert)
        {
            if (_dbConnection != null)
            {
                ExecuteSQLNonQuery($"DELETE FROM alerts WHERE GUID = '{alert.GUID}'");
            }
        }
        [Obsolete]
        public Dictionary<WarframeAlert, string> ReadDatabase()
        {
            Console.WriteLine("Reading database!");

            Dictionary<WarframeAlert, string> resultDictionary = new Dictionary<WarframeAlert, string>();

            if (_dbConnection != null)
            {
                Console.WriteLine("_dbConnection != null");
                SQLiteCommand query = new SQLiteCommand("SELECT * FROM alerts", _dbConnection);
                SQLiteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    MissionInfo mInfo = new MissionInfo(
                        reader["factionName"].ToString(), reader["missionName"].ToString(),
                        int.Parse(reader["credits"].ToString()), reader["lootName"].ToString(),
                        int.Parse(reader["lootQuantity"].ToString()), int.Parse(reader["minLevel"].ToString()),
                        int.Parse(reader["maxLevel"].ToString()),
                        false);

                    DateTime startTime = DateTime.Parse(reader["startTime"].ToString());
                    DateTime expireTime = DateTime.Parse(reader["expireTime"].ToString());

                    WarframeAlert alert = new WarframeAlert(mInfo, reader["GUID"].ToString(), reader["destinationName"].ToString(), startTime, expireTime);
                    string messageID = reader["alertID"].ToString();
                    #region comment
                    //List<DiscordMessage> messageHistory = new List<DiscordMessage>();

                    //DiscordMessage associatedMessage = null;
                    //String lastDiscordMessage = String.Empty;

                    /*int messageBatch = 0;

                    do
                    {

                        messageHistory = Client.GetMessageHistory(Client.GetChannelByName(ALERTS_CHANNEL), 20, lastDiscordMessage);

                        Log($"Looping for message ({reader["alertID"].ToString()}) in batch {messageBatch * 20}-{((messageBatch * 20) + 19)}");

                        associatedMessage = messageHistory.Find(x => x.ID == reader["alertID"].ToString());
                        if (messageHistory.Count > 0)
                            lastDiscordMessage = messageHistory.Last().ID;

                        ++messageBatch;
                    } while ((associatedMessage == null) && (messageHistory.Count > 0));
                    //string id = Client.GetMessageHistory(Client.GetChannelByName(ALERTS_CHANNEL), 1, reader["alertID"].ToString()).First().ID;

                    if ((associatedMessage == null) && (messageHistory.Count == 0))
                    {
                        Log($"Message {reader["alertID"].ToString()} could not be found and will subsequently be deleted from database.");
                        ExecuteSQLNonQuery($"DELETE FROM alerts WHERE alertID = '{reader["alertID"].ToString()}'", _dbConnection);
                    }
#if DEBUG
                    else
                        Log($"Message {reader["alertID"].ToString()} was found.");
#endif


                    alertData.AssociatedMessageID = associatedMessage.ID;
                    */
                    #endregion

                    Console.WriteLine($"Reading {alert.GUID}, {messageID}");

                    resultDictionary.Add(alert, messageID);
                }
                Console.WriteLine("Read Complete");
            }
            else
                Console.WriteLine("_dbConnection = null;");
            return resultDictionary;
        }
    }
}
