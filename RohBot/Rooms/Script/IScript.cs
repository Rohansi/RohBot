
namespace RohBot.Rooms.Script
{
    public interface IScript
    {
        void Initialize(ScriptHost host);
        void Update(float deltaTime);

        bool OnSendHistory(Connection connection);
        bool OnSendLine(HistoryLine line);
        bool OnSendMessage(Connection connection, string message);
    }
}
