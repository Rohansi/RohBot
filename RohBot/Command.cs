using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EzSteam;
using RohBot.Rooms;

namespace RohBot
{
    public abstract class Command
    {
        /// <summary>
        /// The command type. This is the text that identifies a specific command
        /// and followed the Command Header (such as '/'). An empty string signifies
        /// a default command handler. Should ALWAYS be lowercase.
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Return a parameter format string. '-' is a short parameter and ']' uses remaining space.
        /// Returns null if it can not handle type.
        /// Target and type can be ignored if this is not a default handler.
        /// Examples:
        ///   ""        = No parameters
        ///   "-"       = One short parameter (word or text enclosed in double quotes)
        ///   "]"       = One parameter containing all text after Type
        ///   "--]"     = Two short parameters and one parameter containing the leftovers
        /// </summary>
        public abstract string Format(CommandTarget target, string type);

        /// <summary>
        /// Called when somebody uses this command.
        /// Type can be ignored if this is not a default handler.
        /// </summary>
        public abstract void Handle(CommandTarget target, string type, string[] parameters);

        private static readonly Dictionary<string, Command> Commands;

        static Command()
        {
            Commands = new Dictionary<string, Command>();

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetExportedTypes().Where(type => type.IsSubclassOf(typeof(Command)));

            foreach (var type in types)
            {
                var instance = (Command)Activator.CreateInstance(type);
                Commands.Add(instance.Type, instance);
            }
        }

        public static bool Handle(CommandTarget target, string message, string commandHeader)
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
                Command command = null;
                string format = null;

                Func<string, bool> tryResolve = s =>
                {
                    if (!Commands.TryGetValue(s, out command))
                        return false;

                    format = command.Format(target, type);
                    if (format == null)
                        return false;

                    return true;
                };

                // TODO: is there a better way to do this?
                if (!target.IsRoom || !tryResolve(target.Room.CommandPrefix + type))
                    if (!target.IsRoom || !tryResolve(target.Room.CommandPrefix))
                        if (!tryResolve(type))
                            if (!tryResolve(""))
                                return true;

                var parameters = new List<string>();

                foreach (var p in format)
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

                command.Handle(target, type, parameters.ToArray());
                return true;
            }
            catch (Exception e)
            {
                target?.Send("Command failed.");

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
        public readonly SteamChat PrivateChat;
        public readonly SteamPersona Persona;
        public readonly Connection Connection;

        public bool IsSteam => Persona != null;
        public bool IsRoom => Room != null;
        public bool IsPrivateChat => PrivateChat != null;
        public bool IsWeb => Connection != null;

        // For Steam rooms
        public CommandTarget(Room room, SteamPersona sender)
        {
            Room = room;
            Persona = sender;
        }

        // For Steam private messages
        public CommandTarget(SteamChat steamChat, SteamPersona sender)
        {
            PrivateChat = steamChat;
            Persona = sender;
        }

        // For RohBot rooms
        public CommandTarget(Connection connection, string room)
        {
            Room = Program.RoomManager.Get(room);
            Connection = connection;
        }

        public void Send(string message)
        {
            if (IsWeb)
                Connection.SendSysMessage(message);
            else if (IsRoom)
                Room.Send(message);
            else if (IsPrivateChat)
                PrivateChat.Send(message);
        }
    }
}
