using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Net;

using MySql.Data.MySqlClient;

namespace AntiMoneyHackEmailer
{
    public class Program
    {
        static void Main(string[] args)
        {
            PlayerInfo infos = new PlayerInfo();
            MySqlConnection mySqlConn = new MySqlConnection();
            MySqlCommand command;
            MySqlDataReader reader;

            // An Un-nessasary large amount of App-config calls inside of Main.. Will have to fix later..
            string dbServerIp = ConfigurationSettings.AppSettings.Get("ServerIP");
            string dbPort = ConfigurationSettings.AppSettings.Get("DbPort");
            string dbUserId = ConfigurationSettings.AppSettings.Get("DbUserID");
            string dbPassword = ConfigurationSettings.AppSettings.Get("DbPassword");
            string dbName = ConfigurationSettings.AppSettings.Get("DbName");

            // Setting up Connection string.. Unfortunatly it's the only way I know how to access the db right now until I figure out a better method to do such a thing.
            mySqlConn.ConnectionString = "Server=" + dbServerIp + "userid=" + dbUserId +
                                         "password=" + dbPassword + "port=" + dbPort + "database=" + dbName;

            string emailSubject = ConfigurationSettings.AppSettings.Get("EmailSubject");
            // For a customizable body add inside of App.config a key name and value and map it here based on examples below

            string toEmailAddress = ConfigurationSettings.AppSettings.Get("ToEmail");

            string fromEmailAddress = ConfigurationSettings.AppSettings.Get("FromEmail");
            string fromEmailPassword = ConfigurationSettings.AppSettings.Get("FromPassword");

            string smtpHost = ConfigurationSettings.AppSettings.Get("smtpHost");
            string smtpPort = ConfigurationSettings.AppSettings.Get("smtpPort");

            // Grabbing App.Config values for money values
            string maxAllowedCashOnHand = ConfigurationSettings.AppSettings.Get("CashOnHand");
            string maxAllowedBankCash = ConfigurationSettings.AppSettings.Get("BankCash");

            int counter = 0;

            try
            {
                mySqlConn.Open();
                // May have to modify the query to use the proper table names, I have an old db version that I was using that may be incorrect naming conventions.
                string query = "SELECT * " +
                               "FROM " + dbName + ".players " +
                               "WHERE cash >= " + Int32.Parse(maxAllowedCashOnHand) +
                               "OR bankacc >= " + Int32.Parse(maxAllowedBankCash);

                command = new MySqlCommand(query, mySqlConn);
                reader = command.ExecuteReader();

                List<PlayerInfo> playersList = new List<PlayerInfo>();
                while (reader.Read())
                {
                    PlayerInfo info = new PlayerInfo()
                    {
                        suspectedPlayerName = reader.GetString("name"),
                        playerId = reader.GetString("playerid"),
                        databaseId = reader.GetString("id"), // Not sure if this is the actual title of the column..?
                        cashOnHand = reader.GetString("cash"),
                        moneyInBank = reader.GetString("bankacc"),
                    };
                    
                    counter++;
                }

                mySqlConn.Close();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            finally
            {
                mySqlConn.Dispose();
            }

            for (int i = 0; i < counter; i++)
            {
                var fromMessage = new MailAddress(fromEmailAddress, "Possible Hacker Alert System");
                string fromPassword = fromEmailPassword;

                // Can Convert this from a string to a List<string> to have multiple people notified instead of just one.
                var toMessage = new MailAddress(toEmailAddress);

                string subject = emailSubject + infos.suspectedPlayerName[i];
                // Setting the email subject from app.config and concatenating the players name here
                string body = "Player name: " + infos.suspectedPlayerName[i] +
                              "\n" + "Player ID: " + infos.playerId[i] +
                              "\n" + "Database ID: (UID)" + infos.databaseId[i] +
                              "\n" + "Player cash on hand: " + infos.cashOnHand[i] +
                              "\n" + "Player bank amount: " + infos.moneyInBank[i];

                Console.WriteLine(body);

                var smtp = new SmtpClient
                {
                    Host = smtpHost,
                    Port = Int32.Parse(smtpPort),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(fromMessage.Address, fromPassword),
                    Timeout = 20000
                };

                using (var message = new MailMessage(fromMessage, toMessage)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }

            }
            Console.WriteLine("Message Sent\nPlease Press any button to exit...");
            Console.ReadLine();
        }
    }
}