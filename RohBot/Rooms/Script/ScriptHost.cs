using System;

namespace RohBot.Rooms.Script
{
    public class ScriptHost
    {
        private readonly ScriptRoom _room;

        internal ScriptHost(ScriptRoom room)
        {
            _room = room;
        }

        internal void Reset()
        {
            
        }

        public void RegisterCommand(string type, string format, Action<CommandTarget, string[]> handler)
        {
            var cmdHandler = new ScriptRoom.CommandHandler(type, format, handler);
            _room.Commands.Add(type, cmdHandler);
        }

        public void Send(string message)
        {
            _room.Send(message);
        }

        public void SendLine(HistoryLine line)
        {
            _room.SendLine(line);
        }

        public bool IsBanned(string name)
        {
            return _room.IsBanned(name);
        }

        public bool IsMod(string name)
        {
            return _room.IsMod(name);
        }
    }
}
