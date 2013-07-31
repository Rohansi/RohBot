using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Commands
{
    public class Uptime : Command
    {
        public override string Type { get { return "uptime"; }  }

        public override string Format { get { return ""; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            var uptime = DateTime.Now - Program.StartTime;
            target.Send("Current uptime: " + string.Format("{0} days {1} hours {2} minutes {3} seconds", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds));
        }
    }
}
