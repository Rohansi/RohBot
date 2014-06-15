namespace RohBot.Rooms.Remote.Commands
{
    public class Default : Command
    {
        public override string Type { get { return "remote_"; } }

        public override string Format(CommandTarget target, string type)
        {
            if (!target.IsRoom || !(target.Room is RemoteRoom))
                return "";

            var remoteRoom = (RemoteRoom)target.Room;

            if (remoteRoom.Commands == null)
                return null;

            string format;
            return remoteRoom.Commands.TryGetValue(type, out format) ? format : null;
        }

        public override void Handle(CommandTarget target, string type, string[] parameters)
        {
            if (!target.IsRoom || !(target.Room is RemoteRoom))
                return;

            var remoteRoom = (RemoteRoom)target.Room;
            remoteRoom.CallCommand(target, type, parameters);
        }
    }
}
