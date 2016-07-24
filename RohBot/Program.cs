using System;
using System.Threading;
using RohBot.Rooms.Steam;
using SteamKit2;
using log4net;

namespace RohBot
{
    public class Program
    {
        public static readonly DateTime StartTime = DateTime.Now;
        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        public static Settings Settings;
        public static SessionManager SessionManager;
        public static RoomManager RoomManager;
        public static DelayManager DelayManager;
        public static NotificationManager NotificationManager;
        public static bool NotificationsDirty = false;
        public static Steam Steam;

        private static TaskScheduler _taskScheduler;

        static void Main()
        {
            Logger.Info("Process starting");

            /*DebugLog.AddListener((a, b) =>
            {
                Console.WriteLine("{0} {1} {2}", DateTime.Now, a, b);
            });

            DebugLog.Enabled = true;*/

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Fatal("Unhandled exception: " + e.ExceptionObject);
                Logger.Info("Process exiting");
            };

            ThreadPool.SetMaxThreads(10, 1);

            LoadSettings();

            if (Settings == null)
            {
                Logger.Fatal("Failed to load settings!");
                return;
            }

            SessionManager = new SessionManager();
            RoomManager = new RoomManager();
            DelayManager = new DelayManager();
            NotificationManager = new NotificationManager();
            Steam = new Steam();

            RoomManager.Update();
            SessionManager.Start();

            _taskScheduler = new TaskScheduler();
            _taskScheduler.Add(TimeSpan.FromSeconds(0.5), () =>
            {
                SessionManager.Update();
                RoomManager.Update();
                DelayManager.Update();
                Steam.Update();
            });

            //_taskScheduler.Add(TimeSpan.FromSeconds(5), () => SessionManager.Ping());

            //_taskScheduler.Add(TimeSpan.FromMinutes(1), GC.Collect);

            _taskScheduler.Add(TimeSpan.FromHours(1), () =>
            {
                var t = Util.GetTimestamp(DateTime.UtcNow - TimeSpan.FromDays(30));
                LoginToken.RemoveOlderThan(t);
            });

            _taskScheduler.Add(TimeSpan.FromSeconds(10), () =>
            {
                if (!NotificationsDirty)
                    return;

                NotificationManager.InvalidateNotificationCache();
                NotificationsDirty = false;
            });

            _taskScheduler.Add(TimeSpan.FromMinutes(2.5), () =>
            {
                if (Steam.Status == Steam.ConnectionStatus.Connected)
                {
                    Steam.Bot.PersonaState = EPersonaState.Online;
                }
            });

            while (true)
            {
                _taskScheduler.Run();
                Thread.Sleep(10);
            }
        }

        public static void LoadSettings()
        {
            try
            {
                var newSettings = Settings.Load("settings.json");
                Settings = newSettings;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load settings", e);
            }
        }
    }
}
