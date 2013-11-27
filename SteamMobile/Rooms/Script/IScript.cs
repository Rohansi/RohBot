
namespace SteamMobile.Rooms.Script
{
    public interface IScript
    {
        void Initialize(ScriptHost host);
        void Update(float deltaTime);

        bool OnSendMessage(Session session, string message);
        bool OnSendLine(HistoryLine line);
    }
}
