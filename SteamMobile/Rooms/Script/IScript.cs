
namespace SteamMobile.Rooms.Script
{
    public interface IScript
    {
        void Initialize(ScriptHost host);
        void Update(float deltaTime);

        bool OnSendHistory(Session session);
        bool OnSendLine(HistoryLine line);
    }
}
