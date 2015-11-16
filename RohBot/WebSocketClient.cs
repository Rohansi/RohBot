using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using vtortola.WebSockets;

namespace RohBot
{
    public abstract class WebSocketClient
    {
        private IDisposable _websocketHandle;
        private WebSocket _websocket;
        private BufferBlock<string> _sendBuffer;

        protected WebSocketClient()
        {
            _sendBuffer = new BufferBlock<string>(new DataflowBlockOptions
            {
                BoundedCapacity = 8
            });
        }

        public bool IsConnected => _websocket != null && _websocket.IsConnected;
        public IPEndPoint EndPoint => _websocket?.RemoteEndpoint;
        public bool IsLocal => _websocket != null && EndPoint.Address.Equals(IPAddress.Any) || IPAddress.IsLoopback(EndPoint.Address);
        public HttpHeadersCollection Headers => _websocket?.HttpRequest.Headers;

        public async Task SendAsync(string message)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The WebSocketClient is not connected.");

            await _sendBuffer.SendAsync(message).ConfigureAwait(false);
        }

        public void Close()
        {
            try
            {
                OnClose();
            }
            finally
            {
                Disconnect();
            }
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void OnError(Exception exception) { }
        protected virtual void OnMessage(string message) { }

        internal void Open(IDisposable websocketHandle, WebSocket websocket, CancellationToken cancellationToken)
        {
            if (websocketHandle == null)
                throw new ArgumentNullException(nameof(websocketHandle));

            if (websocket == null)
                throw new ArgumentNullException(nameof(websocket));

            _websocketHandle = websocketHandle;
            _websocket = websocket;

            ReadWebSocketAsync(cancellationToken);
            WriteWebSocketAsync(cancellationToken);
        }
        
        private void Error(Exception exception)
        {
            try
            {
                OnError(exception);
            }
            finally
            {
                Disconnect();
            }
        }

        private void Disconnect()
        {
            _websocket?.Dispose();
            _websocket = null;

            _websocketHandle?.Dispose();
            _websocketHandle = null;
        }

        private async void ReadWebSocketAsync(CancellationToken ct)
        {
            try
            {
                OnOpen();

                while (!ct.IsCancellationRequested && _websocket.IsConnected)
                {
                    var readStream = await _websocket.ReadMessageAsync(ct).ConfigureAwait(false);
                    if (readStream == null || readStream.MessageType != WebSocketMessageType.Text)
                        continue; // TODO: do i need to read the message?

                    using (var reader = new StreamReader(readStream, Encoding.UTF8))
                        OnMessage(await reader.ReadToEndAsync().ConfigureAwait(false));
                }

                Close();
            }
            catch (OperationCanceledException)
            {
                Close();
            }
            catch (Exception e)
            {
                Error(e);
            }
        }

        private async void WriteWebSocketAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _websocket.IsConnected)
                {
                    var data = await _sendBuffer.ReceiveAsync(ct).ConfigureAwait(false);

                    using (var message = _websocket.CreateMessageWriter(WebSocketMessageType.Text))
                    using (var writer = new StreamWriter(message, Encoding.UTF8))
                    {
                        await writer.WriteAsync(data).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Error(e);
            }
        }
    }
}
