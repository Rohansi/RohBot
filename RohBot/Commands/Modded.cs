using System.Linq;

namespace RohBot.Commands
{
    public class Modded : Command
    {
        public override string Type => "modded";

        public override string Format(CommandTarget target, string type) => "";

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsMod(target))
                return;

            var modded = target.Room.Modded;
            modded.Add(target.Room.RoomInfo.Admin.ToLower());
            modded = modded.OrderBy(n => n).ToList();

            target.Send($"Mods for this room: {string.Join(", ", modded)}");
        }
    }
}
