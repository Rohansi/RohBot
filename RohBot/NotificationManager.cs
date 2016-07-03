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
        [JsonProperty("app_id")]
        public string AppID => Program.Settings.NotificationAppID;
        [JsonProperty("contents")]
        public Dictionary<string, string> Contents { get; set; }
        [JsonProperty("include_player_ids")]
        public List<String> DeviceTokens { get; set; }

        [JsonIgnore]
        public string Sound { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, string> Data { get; set; }

        // iOS
        [JsonProperty("ios_badgeType")]
        public string IOSBadgeType => "Increase";
        [JsonProperty("ios_badgeCount")]
        public int IOSBadgeCount => 1;
        [JsonProperty("ios_sound")]
        public string IOSSound => Sound;

        // Android
        [JsonProperty("android_sound")]
        public string AndroidSound
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Sound);
            }
        }
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
            if (message.Line is ChatLine == false)
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
            notificationPacket.Sound = "pop.wav";
            notificationPacket.DeviceTokens = deviceTokens;
            notificationPacket.Contents = new Dictionary<string, string>(){
                { "en", message }
            };

            PostNotificationRequest(notificationPacket);
        }
    }
}
