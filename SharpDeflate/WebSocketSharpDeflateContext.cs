using vtortola.WebSockets;

namespace SharpDeflate
{
    public sealed class WebSocketSharpDeflateContext : IWebSocketMessageExtensionContext
    {
        public WebSocketMessageReadStream ExtendReader(WebSocketMessageReadStream message)
        {
            return message.Flags.Rsv1 ? new WebSocketSharpDeflateReadStream(message) : message;
        }

        public WebSocketMessageWriteStream ExtendWriter(WebSocketMessageWriteStream message)
        {
            message.ExtensionFlags.Rsv1 = true;
            return new WebSocketSharpDeflateWriteStream(message);
        }
    }
}
