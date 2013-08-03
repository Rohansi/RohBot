using System;
using System.Collections.Generic;

namespace SteamMobile
{
    public static class Ticker
    {
        class TickerItem
        {
            public readonly string Text;
            public readonly DateTime Created;

            public TickerItem(string text)
            {
                Text = text;
                Created = DateTime.Now;
            }
        }

        private static readonly List<TickerItem> Items;

        static Ticker()
        {
            Items = new List<TickerItem>();
        }

        public static void Add(string text)
        {
            lock (Items)
            {
                Items.RemoveAll(i => i.Text == text);
                Items.Add(new TickerItem(text));
            }
        }

        public static void Update()
        {
            lock (Items)
            {
                Items.RemoveAll(i => DateTime.Now - i.Created > TimeSpan.FromSeconds(10));

                var text = "";

                if (Items.Count > 0)
                {
                    for (var i = 0; i < Items.Count; i++)
                    {
                        text += Items[i].Text;
                        if (i == Items.Count - 1)
                            break; // last item, no suffix
                        if (i == Items.Count - 2)
                            text += " and "; // second last
                        else
                            text += ", ";
                    }

                    text += " logged in.";
                }

                if (Steam.Bot != null && Steam.Bot.Playing != text)
                {
                    Steam.Bot.Playing = text;
                }
            }
        }
    }
}
