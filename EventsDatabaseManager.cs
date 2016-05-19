using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSharpTest
{
    class EventsDatabaseManager
    {
        private SQLiteConnection _dbConnection { get; set; }

        void ConnectToSQLDatabase()
        {
            throw new NotImplementedException();

#if DEBUG
            //SQLiteConnection.CreateFile("WarframeAlerts.sqlite");
#endif
            _dbConnection = new SQLiteConnection("Data Source=WarframeAlerts.sqlite;Version=3;");
            _dbConnection.Open();
            //if (_dbConnection != null) CreateDatabase();
        }

        private int ExecuteSQLNonQuery(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            return command.ExecuteNonQuery();
        }

        /*private void ReadDatabase(string table, )
        {
            throw new NotImplementedException();

            if (_dbConnection != null)
            {
                SQLiteCommand query = new SQLiteCommand("SELECT * FROM alerts", _dbConnection);
                SQLiteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    TimeSpan timeToExpire = (DateTime.Parse(reader["expirationTime"].ToString()).Subtract(DateTime.Now));
                    WarframeAlert alertData = new WarframeAlert(
                        reader["giud"].ToString(),
                        reader["destinationName"].ToString(),
                        reader["factionName"].ToString(),
                        reader["missionName"].ToString(),
                        int.Parse(reader["credits"].ToString()),
                        reader["lootName"].ToString(),
                        timeToExpire.Minutes);

                    List<DiscordMessage> messageHistory = new List<DiscordMessage>();

                    DiscordMessage associatedMessage = null;
                    String lastDiscordMessage = String.Empty;

                    int messageBatch = 0;

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

                    _activeAlerts.Add(new Tuple<WarframeEvent, DiscordMessage>(alertData, associatedMessage));
                }

                _activeAlerts.Sort((Tuple<WarframeEvent, DiscordMessage> a, Tuple<WarframeEvent, DiscordMessage> b) =>
                {
                    return a.Item1.ExpirationTime.CompareTo(b.Item1.ExpirationTime);
                }
               );
            }
        }*/
    }
}
