using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SteamMobile
{
    public abstract class Command
    {
        /// <summary>
        /// The command type. This is the text that identifies a specific command
        /// and followed the Command Header (such as '/').
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Parameter format string. '-' is a short parameter and ']' uses remaining space.
        /// Examples:
        ///   ""        = No parameters
        ///   "-"       = One short parameter (word or text enclosed in double quotes)
        ///   "]"       = One parameter containing all text after Type
        ///   "--]"     = Two short parameters and one parameter containing the leftovers
        /// </summary>
        public abstract string Format { get; }

        /// <summary>
        /// Called when somebody attempts to use this command.
        /// </summary>
        public abstract void Handle(CommandTarget target, string[] parameters);


        private static readonly Dictionary<string, Command> Commands;

        static Command()
        {
            Commands = new Dictionary<string, Command>();

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetExportedTypes().Where(type => type.IsSubclassOf(typeof(Command)));

            foreach (var type in types)
            {
                var instance = (Command)Activator.CreateInstance(type);
                Commands[instance.Type] = instance;
            }
        }

        public static bool Handle(CommandTarget target, string message, string commandHeader = "/")
        {
            if (!message.StartsWith(commandHeader))
                return false;

            var commandStr = message.Substring(commandHeader.Length);

            if (string.IsNullOrWhiteSpace(commandStr))
                return false;

            var reader = new StringReader(commandStr);
            var type = ReadWord(reader).ToLower();

            // default handler for non-existing commands
            if (!Commands.ContainsKey(type))
            {
                if (target.IsSession)
                    Program.SendMessage(target.Session, "*", "Unknown command.");

                return true;
            }

            var command = Commands[type];
            var parameters = new List<string>();

            foreach (var p in command.Format)
            {
                var param = "";

                switch (p)
                {
                    case '-':
                        param = ReadWord(reader);
                        break;
                    case ']':
                        param = ReadRemaining(reader);
                        break;
                }

                if (param == null)
                    break;

                parameters.Add(param);
            }

            command.Handle(target, parameters.ToArray());
            return true;
        }

        private static string ReadRemaining(StringReader reader)
        {
            var word = "";

            while (reader.Peek() != -1)
            {
                word += (char)reader.Read();
            }

            return string.IsNullOrWhiteSpace(word) ? null : word;
        }

        private static string ReadWord(StringReader reader)
        {
            var word = "";

            if (reader.Peek() == '"')
            {
                reader.Read(); // skip open
                while (reader.Peek() != '"')
                {
                    if (reader.Peek() == -1)
                        break;
                    word += (char)reader.Read();
                }
                reader.Read(); // skip close
            }
            else
            {
                while (reader.Peek() != ' ')
                {
                    if (reader.Peek() == -1)
                        break;
                    word += (char)reader.Read();
                }

                if (string.IsNullOrWhiteSpace(word))
                    word = null;
            }

            SkipWhiteSpace(reader);
            return word;
        }

        private static void SkipWhiteSpace(StringReader reader)
        {
            while (char.IsWhiteSpace((char)reader.Peek()))
                reader.Read();
        }
    }

    public class CommandTarget
    {
        public readonly SteamChat Chat;
        public readonly Session Session;

        public bool IsChat { get { return Chat != null; } }
        public bool IsSession { get { return Session != null; } }

        private CommandTarget() { }

        private CommandTarget(SteamChat steamChat)
        {
            Chat = steamChat;
        }

        private CommandTarget(Session session)
        {
            Session = session;
        }

        public static CommandTarget FromSteam(SteamChat steamChat)
        {
            return new CommandTarget(steamChat);
        }

        public static CommandTarget FromSession(Session session)
        {
            return new CommandTarget(session);
        }
    }
}
