
namespace RohBot.Rooms.Script.Commands
{
    public class Compile : Command
    {
        public override string Type { get { return "script_compile"; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !(target.Room is ScriptRoom) || !Util.IsSuperAdmin(target))
                return;

            var scriptRoom = (ScriptRoom)target.Room;
            scriptRoom.Recompile();
        }
    }
}
