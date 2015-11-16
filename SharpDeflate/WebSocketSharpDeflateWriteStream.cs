using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
using vtortola.WebSockets;

namespace SharpDeflate
{
    public sealed class WebSocketSharpDeflateWriteStream : WebSocketMessageWriteStream
    {
        private readonly static byte[] FinalByte = { 0 };
        private readonly WebSocketMessageWriteStream _inner;
        private readonly DeflateStream _deflate;
        private bool _isClosed, _isDisposed;

        public WebSocketSharpDeflateWriteStream(WebSocketMessageWriteStream inner)
        {
            _inner = inner;
            _deflate = new DeflateStream(_inner, CompressionMode.Compress, true);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            RemoveUTF8BOM(buffer, ref offset, ref count);
            if (count == 0)
                return;

            _deflate.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            RemoveUTF8BOM(buffer, ref offset, ref count);
            if (count == 0)
                return;

            await _deflate.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        }

        public override async Task CloseAsync(CancellationToken cancellation)
        {
            if (_isClosed)
                return;

            _isClosed = true;
            _deflate.Close();
            _inner.Write(FinalByte, 0, 1);
            await _inner.CloseAsync(cancellation).ConfigureAwait(false);
        }

        public override void Close()
        {
            if (_isClosed)
                return;

            _isClosed = true;
            _deflate.Close();
            _inner.Write(FinalByte, 0, 1);
            _inner.Close();
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
