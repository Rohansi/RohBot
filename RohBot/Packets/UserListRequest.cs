namespace RohBot.Packets
{
    // C -> S
    public class UserListRequest : Packet
    {
        public override string Type => "userListRequest";

        public string Target;

        public override void Handle(Connection connection)
        {
            // TODO: move implementation here later
            var commandTarget = new CommandTarget(connection, Target);
            var command = new Commands.Users();
            command.Handle(commandTarget, command.Type, new string[0]);
        }
    }
}
