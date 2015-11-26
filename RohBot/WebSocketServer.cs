using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using SharpDeflate;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace RohBot
{
    public sealed class WebSocketServer<TClient> : IDisposable
        where TClient : WebSocketClient, new()
    {
        private class ClientHandle : IDisposable
        {
            private Action _dispose;

            public ClientHandle(Action dispose)
            {
                if (dispose == null)
                    throw new ArgumentNullException(nameof(dispose));

                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }

        private CancellationTokenSource _cts;
        private WebSocketListener _listener;
        private object _clientsSync;
        private List<TClient> _clients;
        private IReadOnlyList<TClient> _clientsCache;

        public WebSocketServer(IPEndPoint endpoint)
        {
            var options = new WebSocketListenerOptions
            {
                PingTimeout = TimeSpan.FromSeconds(30)
            };

            _cts = new CancellationTokenSource();
            _listener = new WebSocketListener(endpoint, options);
            _clientsSync = new object();
            _clients = new List<TClient>();

            var rfc6455 = new WebSocketFactoryRfc6455(_listener);
            rfc6455.MessageExtensions.RegisterExtension(new WebSocketSharpDeflateExtension());
            _listener.Standards.RegisterStandard(rfc6455);

            _listener.Start();
            ListenAsync();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Dispose();
        }

        public IReadOnlyList<TClient> Clients
        {
            get
            {
                lock (_clientsSync)
                {
                    return _clientsCache ?? (_clientsCache = _clients.ToList().AsReadOnly());
                }
            }
        }

        private async void ListenAsync()
        {
            while (_listener.IsStarted)
            {
                WebSocket websocket;

                try
                {
                    websocket = await _listener.AcceptWebSocketAsync(_cts.Token).ConfigureAwait(false);
                    if (websocket == null)
                        continue;
                }
                catch (Exception e)
                {
                    Program.Logger.Error("Failed to accept websocket connection", e);
                    continue;
                }
                
                var client = new TClient();
                var clientHandle = new ClientHandle(() =>
                {
                    lock (_clientsSync)
                    {
                        _clients.Remove(client);
                        _clientsCache = null;
                    }
                });

                client.Open(clientHandle, websocket, _cts.Token);

                lock (_clientsSync)
                {
                    _clients.Add(client);
                    _clientsCache = null;
                }
            }
        }
    }
}
