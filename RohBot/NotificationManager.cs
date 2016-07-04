using RohBot.Packets;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RohBot
{
    class OneSignalNotificationPacket
    {
        [JsonProperty("app_id")]
        public string AppId => Program.Settings.NotificationAppID;

        [JsonProperty("contents")]
        public Dictionary<string, string> Contents { get; set; }

        [JsonProperty("include_player_ids")]
        public List<string> DeviceTokens { get; set; }

        [JsonIgnore]
        public string Sound { get; set; }

        [JsonProperty("data")]
        public Dictionary<string, string> Data { get; set; }

        // iOS
        [JsonProperty("ios_badgeType")]
        public string IosBadgeType => "Increase";

        [JsonProperty("ios_badgeCount")]
        public int IosBadgeCount => 1;

        [JsonProperty("ios_sound")]
        public string IosSound => Sound;

        // Android
        [JsonProperty("android_sound")]
        public string AndroidSound => Path.GetFileNameWithoutExtension(Sound);
    }

    public class NotificationManager
    {
        public List<Notification> Notifications { get; private set; }

        public NotificationManager()
        {
            InvalidateNotificationCache();
        }

        // TODO: optimize these getters?
        public bool Exists(string deviceToken)
        {
            return Notifications.Any(n => n.DeviceToken == deviceToken);
        }

        public IEnumerable<Notification> FindWithId(long userId)
        {
            return Notifications.Where(n => n.UserId == userId);
        }

        public Notification Get(string deviceToken)
        {
            return Notifications.FirstOrDefault(n => n.DeviceToken == deviceToken);
        }

        public void HandleMessage(Message message)
        {
            if (message.Line is ChatLine == false)
                return;

            var chatLine = (ChatLine)message.Line;
            var content = $"[{chatLine.Chat}] {chatLine.Sender}: {chatLine.Content}";
            var recipientDevices = Notifications
                .Where(n => n.Regex.IsMatch(chatLine.Content))
                .Select(n => n.DeviceToken)
                .ToList();

            if (recipientDevices.Count > 0)
                Notify(recipientDevices, content);
        }

        // TODO: don't use this as often
        public void InvalidateNotificationCache()
        {
            var cmd = new SqlCommand("SELECT * FROM rohbot.notifications");
            Notifications = cmd.Execute().Select(row => new Notification(row)).ToList();
        }

        private static void Notify(List<string> deviceTokens, string message)
        {
            var notificationPacket = new OneSignalNotificationPacket();
            notificationPacket.Sound = "rohbotNotification.wav";
            notificationPacket.DeviceTokens = deviceTokens;
            notificationPacket.Contents = new Dictionary<string, string>()
            {
                { "en", message }
            };

            PostNotificationRequest(notificationPacket);
        }

        private static async void PostNotificationRequest(OneSignalNotificationPacket notificationPacket)
        {
            var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(notificationPacket), Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Program.Settings.NotificationAPIKey);

            var responseMessage = await httpClient.PostAsync("https://onesignal.com/api/v1/notifications", content);
            var responseBody = await responseMessage.Content.ReadAsStringAsync();
            dynamic response = JsonConvert.DeserializeObject(responseBody);

            if (response.errors != null)
            {
                var errors = ((JArray)response.errors).ToObject<List<string>>();
                var errorMessage = $"Notification server returned following error(s): {string.Join(", ", errors)}";

                Program.Logger.Warn(errorMessage);
            }
        }
    }
}
