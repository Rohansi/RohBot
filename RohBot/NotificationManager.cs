using System;
using RohBot.Packets;
using RohBot.Rooms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RohBot
{
    public class NotificationManager
    {
        private class OneSignalNotificationPacket
        {
            [JsonProperty("app_id")]
            public string AppId => Program.Settings.NotificationAppID;

            [JsonProperty("headings")]
            public Dictionary<string, string> Headings { get; set; }

            [JsonProperty("contents")]
            public Dictionary<string, string> Contents { get; set; }

            [JsonProperty("include_player_ids")]
            public List<string> DeviceTokens { get; set; }

            [JsonIgnore]
            public string Sound { get; set; }

            [JsonIgnore]
            public string Group { get; set; }

            [JsonIgnore]
            public Dictionary<string, string> GroupMessage { get; set; }

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

            [JsonProperty("android_group")]
            public string AndroidGroup => Group;

            [JsonProperty("android_group_message")]
            public Dictionary<string, string> AndroidGroupMessage => GroupMessage;
        }

        private readonly object _sync = new object();
        private List<Notification> _notifications;

        public NotificationManager()
        {
            InvalidateNotificationCache();
        }

        // TODO: optimize these getters?
        public bool Exists(string deviceToken, out Notification subscription)
        {
            lock (_sync)
            {
                subscription = _notifications.FirstOrDefault(n => n.DeviceToken == deviceToken);
                return subscription != null;
            }
        }

        public IEnumerable<Notification> FindWithId(long userId)
        {
            lock (_sync)
            {
                return _notifications.Where(n => n.UserId == userId);
            }
        }

        public Notification Get(string deviceToken)
        {
            lock (_sync)
            {
                return _notifications.FirstOrDefault(n => n.DeviceToken == deviceToken);
            }
        }

        public async void HandleMessage(Room room, Message message)
        {
            var chatLine = message.Line as ChatLine;
            if (chatLine == null || chatLine.SenderId == "0")
                return;

            await Task.Delay(1); // force into threadpool

            var senderId = long.Parse(chatLine.SenderId);

            List<string> recipientDevices;

            lock (_sync)
            {
                recipientDevices = _notifications
                    .Where(n => n.UserId != senderId && !room.IsBanned(n.Name) && n.Rooms.Contains(chatLine.Chat))
                    .Where(n => IsMatch(n.Regex, chatLine.Content))
                    .Select(n => n.DeviceToken)
                    .ToList();
            }

            if (recipientDevices.Count > 0)
            {
                var chat = chatLine.Chat;
                var title = Program.RoomManager.Get(chat)?.RoomInfo.Name ?? chat;
                var content = $"{WebUtility.HtmlDecode(chatLine.Sender)}: {WebUtility.HtmlDecode(chatLine.Content)}";
                await Notify(recipientDevices, title, content, chat, chatLine.Date);
            }
        }
        
        public void InvalidateNotificationCache()
        {
            var cmd = new SqlCommand(@"
                SELECT notifications.*, accounts.name, accounts.rooms
                FROM rohbot.accounts
                INNER JOIN rohbot.notifications ON (rohbot.accounts.id = rohbot.notifications.userid)");

            var newNotifications = cmd.Execute().Select(row => new Notification(row)).ToList();

            lock (_sync)
            {
                _notifications = newNotifications;
            }
        }

        private static async Task Notify(List<string> deviceTokens, string title, string content, string chat, long date)
        {
            var notificationPacket = new OneSignalNotificationPacket();
            notificationPacket.Sound = "rohbotNotification.wav";
            notificationPacket.DeviceTokens = deviceTokens;

            notificationPacket.Headings = new Dictionary<string, string>
            {
                { "en", title }
            };

            notificationPacket.Contents = new Dictionary<string, string>
            {
                { "en", content }
            };

            notificationPacket.Group = chat;
            notificationPacket.GroupMessage = new Dictionary<string, string>
            {
                { "en", "$[notif_count] mentions" }
            };

            notificationPacket.Data = new Dictionary<string, string>
            {
                { "chat", chat },
                { "date", date.ToString("D") }
            };

            try
            {
                await PostNotificationRequest(notificationPacket);
            }
            catch (Exception e)
            {
                Program.Logger.Error("Failed to send push notification", e);
            }
        }

        private static async Task PostNotificationRequest(OneSignalNotificationPacket notificationPacket)
        {
            var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(notificationPacket), Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Program.Settings.NotificationAPIKey);

            var responseMessage = await httpClient.PostAsync("https://onesignal.com/api/v1/notifications", content);
            var responseBody = await responseMessage.Content.ReadAsStringAsync();
            var response = JObject.Parse(responseBody);

            JToken errors;
            if (response.TryGetValue("errors", out errors))
            {
                if (errors.Type == JTokenType.Object)
                {
                    var invalidIds = errors.Value<JArray>("invalid_player_ids");
                    UnsubscribeDeviceTokens(invalidIds.ToObject<List<string>>());
                }
                else
                {
                    var errorMessage = $"Notification server returned following error(s): {errors}";
                    Program.Logger.Warn(errorMessage);
                }
            }
        }

        private static void UnsubscribeDeviceTokens(List<string> deviceTokens)
        {
            if (deviceTokens.Count == 0)
                return;
            
            using (var connection = Database.CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var token in deviceTokens)
                {
                    var cmd = new SqlCommand("DELETE FROM rohbot.notifications WHERE devicetoken=:devicetoken", connection, transaction);
                    cmd["devicetoken"] = token;
                    cmd.ExecuteNonQueryNoDispose();
                }

                transaction.Commit();
            }

            Program.NotificationsDirty = true;
        }

        private static bool IsMatch(Regex regex, string input)
        {
            try
            {
                return regex.IsMatch(input);
            }
            catch
            {
                return false;
            }
        }
    }
}
