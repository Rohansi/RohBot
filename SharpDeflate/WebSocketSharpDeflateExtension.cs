using System.Collections.Generic;
using vtortola.WebSockets;

namespace SharpDeflate
{
    /// <summary>
    /// Per-message compression extension based on vtortola.WebSockets.Deflate but uses Zlib.Portable instead of System.IO.Compression.
    /// </summary>
    public sealed class WebSocketSharpDeflateExtension : IWebSocketMessageExtension
    {
        public string Name => "permessage-deflate";

        private static readonly WebSocketExtension Response = new WebSocketExtension("permessage-deflate", new List<WebSocketExtensionOption>(new[] { new WebSocketExtensionOption { Name = "client_no_context_takeover" } }));

        public bool TryNegotiate(WebSocketHttpRequest request, out WebSocketExtension extensionResponse, out IWebSocketMessageExtensionContext context)
        {
            extensionResponse = Response;
            context = new WebSocketSharpDeflateContext();
            return true;
        }
    }
}
