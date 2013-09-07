using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Driver.Builders;

namespace SteamMobile.Commands
{
    public class Search : Command
    {
        private static Stopwatch cooldown = Stopwatch.StartNew();

        public override string Type
        {
            get { return "search"; }
        }

        public override string Format
        {
            get { return "]"; }
        }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.Account == null || target.IsGroupChat || parameters.Length < 1)
                return;

            if (cooldown.Elapsed.TotalSeconds < 2.5)
            {
                target.Send("Search is on cooldown, wait a few seconds and try again.");
                return;
            }

            ThreadPool.QueueUserWorkItem(a =>
            {
                try
                {
                    var results = Database.ChatHistory.Find(Query.And(Query.EQ("_t", "ChatLine"), Query.LT("Date", Util.GetCurrentUnixTimestamp()), Query.Matches("Content", parameters[0]))).SetSortOrder(SortBy.Descending("Date")).SetLimit(20).ToList();

                    if (results.Count == 0)
                    {
                        target.Send("No matches.");
                        return;
                    }

                    var s = new StringBuilder();
                    s.AppendFormat("Found {0} matches:\n", results.Count);

                    foreach (var line in results.OfType<ChatLine>())
                    {
                        s.AppendFormat("{0}: {1}\n", WebUtility.HtmlDecode(line.Sender), WebUtility.HtmlDecode(line.Content));
                    }

                    var msg = s.ToString();
                    if (msg.Length > 2000)
                        msg = msg.Substring(0, 2000) + "...";

                    target.Send(msg);
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Search", e);
                    target.Send("Error, check logs.");
                }

                cooldown.Restart();
            });
        }
    }
}
