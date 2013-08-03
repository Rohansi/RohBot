using System;
using System.Text;

namespace SteamMobile.Commands
{
    public class Help : Command
    {
        public override string Type { get { return "help"; } }

        public override string Format { get { return "-"; } }

        public override void Handle(CommandTarget target, string[] parameters)
        {
            if (target.IsGroupChat)
            {
                target.Send("Use that command in a private message with me, it can get spammy.");
                return;
            }

            var response = new StringBuilder();

            // command list
            if (parameters.Length == 0)
            {
                response.AppendLine("Available commands: chat, w, r, users, sessions, ban, unban, rejoin, uptime, refresh, reboot");
                response.AppendLine("For more information about a command, provide the command name to the help command:");
                response.AppendLine(" help <command>");
                target.Send(response.ToString());
                return;
            }
            
            switch (parameters[0].ToLower())
            {
                case "chat":
                    response.AppendLine("Switches the active chat of a client session:");
                    response.AppendLine(" chat <chat> - Switch to <chat>");
                    response.AppendLine(" chat default - Switch to the default chat");
                    response.AppendLine(" chat default <chat> - Switch and set account default to <chat>");
                    response.AppendLine(" chat list - Print a list of available chats");
                    break;

                case "w":
                    response.AppendLine("Whisper to another RohBot user:");
                    response.AppendLine(" w <name> <message>");
                    break;

                case "r":
                    response.AppendLine("Reply to a whisper:");
                    response.AppendLine(" r <message>");
                    break;

                case "users":
                    response.AppendLine("Print a list of the people in the chat.");
                    break;
                
                case "sessions":
                    response.AppendLine("Print a list of RohBot sessions.");
                    break;

                case "ban":
                    response.AppendLine("Ban a user from using RohBot:");
                    response.AppendLine(" ban <name>");
                    break;

                case "unban":
                    response.AppendLine("Unban a user from RohBot:");
                    response.AppendLine(" unban <name>");
                    break;

                case "rejoin":
                    response.AppendLine("Rejoin a chat, useful when Steam breaks:");
                    response.AppendLine(" rejoin <chat>");
                    break;

                case "uptime":
                    response.AppendLine("Print the process uptime of RohBot.");
                    break;

                case "refresh":
                    response.AppendLine("Reload settings and accounts.");
                    break;

                case "reboot":
                    response.AppendLine("Restarts RohBot.");
                    break;

                default:
                    response.AppendLine("There is no help text for that.");
                    break;
            }

            target.Send(response.ToString());
        }
    }
}
