using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using vtortola.WebSockets;

namespace SharpDeflate
{
    public sealed class WebSocketSharpDeflateReadStream : WebSocketMessageReadStream
    {
        private readonly WebSocketMessageReadStream _inner;
        private readonly DeflateStream _deflate;
        private bool _isDisposed;

        public WebSocketSharpDeflateReadStream(WebSocketMessageReadStream inner)
        {
            _inner = inner;
            _deflate = new DeflateStream(_inner, CompressionMode.Decompress, true);
        }

        public override WebSocketMessageType MessageType => _inner.MessageType;

        public override WebSocketExtensionFlags Flags => _inner.Flags;

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _deflate.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _deflate.ReadAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _deflate.Dispose();
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
