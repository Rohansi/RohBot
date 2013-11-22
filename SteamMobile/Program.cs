using System;
using System.Threading;
using MongoDB.Driver.Builders;
using SteamKit2;
using log4net;

namespace SteamMobile
{
    public class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        public static Settings Settings;
        public static SessionManager SessionManager;
        public static RoomManager RoomManager;
        public static DelayManager DelayManager;
        public static Steam Steam;

        private static TaskScheduler _taskScheduler;

        static void Main()
        {
            Logger.Info("Process starting");

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Fatal("Unhandled exception: " + e.ExceptionObject);
                Logger.Info("Process exiting");
            };

            ThreadPool.SetMaxThreads(10, 1);

            LoadSettings();

            SessionManager = new SessionManager();
            RoomManager = new RoomManager();
            DelayManager = new DelayManager();
            Steam = new Steam();

            _taskScheduler = new TaskScheduler();
            _taskScheduler.Add(TimeSpan.FromSeconds(0.5), () =>
            {
                SessionManager.Update();
                RoomManager.Update();
                DelayManager.Update();
                Steam.Update();
            });

            _taskScheduler.Add(TimeSpan.FromHours(1), () =>
            {
                var t = Util.GetUnixTimestamp(DateTime.UtcNow - TimeSpan.FromDays(30));
                Database.LoginTokens.Remove(Query.LT("Created", t));
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
            Settings = Settings.Load("settings.json");
        }
    }
}
