using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EzSteam;
using SteamKit2;
using SteamMobile.Packets;

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
            if (string.IsNullOrWhiteSpace(message) || !message.StartsWith(commandHeader))
                return false;

            try
            {
                var commandStr = message.Substring(commandHeader.Length);

                if (string.IsNullOrWhiteSpace(commandStr))
                    return false;

                var reader = new StringReader(commandStr);
                var type = (ReadWord(reader) ?? "").ToLower();

                // default handler for non-existing commands
                if (!Commands.ContainsKey(type))
                {
                    if (target.IsSession || target.IsPrivateChat)
                    {
                        target.Send("Unknown command.");
                        return true;
                    }

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
            catch (Exception e)
            {
                if (target != null)
                    target.Send("Command failed.");

                Program.Logger.Error("Command failed: ", e);
                return true;
            }
        }

        private static string ReadRemaining(StringReader reader)
        {
            var word = "";

            SkipWhiteSpace(reader);

            while (reader.Peek() != -1)
            {
                word += (char)reader.Read();
            }

            return string.IsNullOrWhiteSpace(word) ? null : word;
        }

        private static string ReadWord(StringReader reader)
        {
            var word = "";

            SkipWhiteSpace(reader);

            if (reader.Peek() == '"')
            {
                reader.Read(); // skip open
                while (reader.Peek() != '"')
                {
                    if (reader.Peek() == -1)
                        break;

                    if (reader.Peek() == '\\')
                    {
                        reader.Read(); // skip \
                        var ch = reader.Read();
                        switch (ch)
                        {
                            case -1:
                                break; // eof, do nothing
                            case '\\':
                                word += '\\';
                                break;
                            case '"':
                                word += '"';
                                break;
                            default:
                                word += (char)ch;
                                break;
                        }
                    }
                    else
                    {
                        word += (char)reader.Read();
                    }
                }
                reader.Read(); // skip close
            }
            else
            {
                while (!char.IsWhiteSpace((char)reader.Peek()))
                {
                    if (reader.Peek() == -1)
                        break;
                    word += (char)reader.Read();
                }

                if (string.IsNullOrEmpty(word))
                    word = null;
            }

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
        public readonly Room Room;
        public readonly Chat PrivateChat;
        public readonly SteamID SteamId;
        public readonly Session Session;

        public bool IsSteam { get { return Session == null; } }
        public bool IsGroupChat { get { return Room != null; } }
        public bool IsPrivateChat { get { return PrivateChat != null; } }
        public bool IsSession { get { return Session != null; } }

        public CommandTarget(Room room, SteamID sender)
        {
            Room = room;
            SteamId = sender;
        }

        public CommandTarget(Chat steamChat, SteamID sender)
        {
            PrivateChat = steamChat;
            SteamId = sender;
        }

        public CommandTarget(Session session)
        {
            Room = Program.RoomManager.Get(session.Room);
            Session = session;
        }

        public void Send(string message)
        {
            if (IsSession)
                Session.Send(new SysMessage { Content = message, Date = Util.GetCurrentUnixTimestamp() });
            else if (IsGroupChat)
                Room.Send(message);
            else if (IsPrivateChat)
                PrivateChat.Send(message);
        }
    }
}
