using RohBot.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace RohBot
{
    class OneSignalNotificationPacket
    {
        public string app_id => Program.Settings.NotificationAppID;
        public Dictionary<string, string> contents { get; set; }
        public List<String> include_player_ids { get; set; } 
    }

    public class NotificationManager
    {
        private string apiKey;
        private string appID;
        private List<Notification> notifications;

        public NotificationManager()
        {
            apiKey = Program.Settings.NotificationAPIKey;
            appID = Program.Settings.NotificationAppID;
            notifications = LoadNotifications().ToList<Notification>(); 
        }

        public void HandleMessage(Message message) 
        {
            var content = message.Line.Content;
            var recipientDevices = notifications
                                        .Where(n => n.Regex.IsMatch(content))
                                        .Select(n => n.DeviceToken);

            if (recipientDevices.Count() > 0)
                Notify(recipientDevices, content);
        }

        private IEnumerable<Notification> LoadNotifications()
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.notifications");

            return cmd.Execute().Select(row => new Notification(row));
        }

        public void InvalidateNotificationCache()
        {
            notifications = LoadNotifications().ToList<Notification>();
        }

        private static void PostNotificationRequest(OneSignalNotificationPacket notificationPacket)
        {
            var request = WebRequest.Create("https://onesignal.com/api/v1/notifications") as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentType = "application/json";

            request.Headers.Add("authorization", "Basic " + Program.Settings.NotificationAPIKey);

            byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notificationPacket));

            string responseContent = null;

            try
            {
                using (var writer = request.GetRequestStream())
                {
                    writer.Write(byteArray, 0, byteArray.Length);
                }

                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        responseContent = reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(new StreamReader(ex.Response.GetResponseStream()).ReadToEnd());
            }
        }

        public static void Notify(IEnumerable<string> deviceTokens, string message)
        {
            var notificationPacket = new OneSignalNotificationPacket();
            notificationPacket.include_player_ids = deviceTokens.ToList<String>();
            notificationPacket.contents = new Dictionary<string, string>(){
                { "en", message }
            };

            PostNotificationRequest(notificationPacket);
        }
    }
}
