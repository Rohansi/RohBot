using System.Linq;

namespace RohBot.Commands
{
    public class Modded : Command
    {
        public override string Type { get { return "modded"; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsMod(target))
                return;

            var modded = target.Room.Modded;
            modded.Add(target.Room.RoomInfo.Admin.ToLower());
            modded = modded.OrderBy(n => n).ToList();

            target.Send(string.Format("Mods for this room: {0}", string.Join(", ", modded)));
        }
    }
}
