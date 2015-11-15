
namespace RohBot.Rooms.Script.Commands
{
    public class Default : Command
    {
        public override string Type => "script_";

        public override string Format(CommandTarget target, string type)
        {
            if (!target.IsRoom || !(target.Room is ScriptRoom))
                return "";

            var scriptRoom = (ScriptRoom)target.Room;
            ScriptRoom.CommandHandler command;

            return scriptRoom.Commands.TryGetValue(type, out command) ? command.Format : null;
        }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !(target.Room is ScriptRoom))
                return;

            var scriptRoom = (ScriptRoom)target.Room;
            ScriptRoom.CommandHandler command;

            if (scriptRoom.Commands.TryGetValue(type, out command))
            {
                scriptRoom.SafeInvoke(() => command.Handler(target, parameters));
                return;
            }

            target.Send("Unknown command.");
        }
    }
}
