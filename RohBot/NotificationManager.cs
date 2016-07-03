using RohBot.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
                                        .Select(n => n.DeviceToken)
                                        .ToList<string>();

            if (recipientDevices.Count > 0)
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

        private static async void PostNotificationRequest(OneSignalNotificationPacket notificationPacket)
        {
            var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(notificationPacket), Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Program.Settings.NotificationAPIKey);

            var response = await httpClient.PostAsync("https://onesignal.com/api/v1/notifications", content);

            Console.WriteLine(response);
        }

        public static void Notify(List<string> deviceTokens, string message)
        {
            var notificationPacket = new OneSignalNotificationPacket();
            notificationPacket.include_player_ids = deviceTokens;
            notificationPacket.contents = new Dictionary<string, string>(){
                { "en", message }
            };

            PostNotificationRequest(notificationPacket);
        }
    }
}
