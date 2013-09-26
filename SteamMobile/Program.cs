using System;
using System.Threading;
using log4net;

namespace SteamMobile
{
    public class Program
    {
        public static readonly ILog Logger = LogManager.GetLogger("Steam");
        public static Settings Settings;
        public static SessionManager SessionManager;
        public static RoomManager RoomManager;
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
            Steam = new Steam();

            _taskScheduler = new TaskScheduler();
            _taskScheduler.Add(TimeSpan.FromSeconds(0.1), () =>
            {
                SessionManager.Update();
                RoomManager.Update();
                Steam.Update();
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
