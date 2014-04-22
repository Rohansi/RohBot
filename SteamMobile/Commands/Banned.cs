using System.Linq;

namespace SteamMobile.Commands
{
    public class Banned : Command
    {
        public override string Type { get { return "banned"; } }

        public override string Format(CommandTarget target, string type) { return ""; }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !Util.IsMod(target))
                return;

            var banned = target.Room.Banned;
            banned = banned.OrderBy(s => s).ToList();

            if (banned.Count == 0)
            {
                if (!target.Room.IsWhitelisted)
                    target.Send("Nobody is banned.");
                else
                    target.Send("Everybody is banned.");
            }
            else
            {
                if (!target.Room.IsWhitelisted)
                    target.Send(string.Format("Banned: {0}", string.Join(", ", banned)));
                else
                    target.Send(string.Format("Whitelisted: {0}", string.Join(", ", banned)));
            }
        }
    }
}
