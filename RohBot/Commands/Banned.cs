using System.Linq;

namespace RohBot.Commands
{
    public class Banned : Command
    {
        public override string Type => "banned";

        public override string Format(CommandTarget target, string type) => "";

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
                    target.Send($"Banned: {string.Join(", ", banned)}");
                else
                    target.Send($"Whitelisted: {string.Join(", ", banned)}");
            }
        }
    }
}
