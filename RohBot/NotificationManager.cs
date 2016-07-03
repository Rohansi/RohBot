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

        public string ios_badgeType => "Increase";
        public int ios_badgeCount => 1;
        public string ios_sound = "pop.wav";
    }

    public class NotificationManager
    {
        public List<Notification> Notifications => notifications;

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
            if (message.Line notis ChatLine)
                return;

            var chatLine = (ChatLine)message.Line;
            var content = String.Format("{0} - {1}", chatLine.Sender, chatLine.Content);
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

            Console.WriteLine(await response.Content.ReadAsStringAsync());
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
