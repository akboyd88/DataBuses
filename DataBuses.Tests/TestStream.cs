using System.IO;
using System.IO.Pipelines;
using System.Threading;

namespace Boyd.DataBuses.Tests
{
    public class TestStream : Stream
    {
        private Stream _writeStream;
        private Stream _readStream;
        private CancellationTokenSource _cancellationTokenSource;
        private PipeWriter _pWriter;
        
        public TestStream()
        {
            _readStream = new MemoryStream();
            _writeStream = new MemoryStream();
            _cancellationTokenSource = new CancellationTokenSource();
            _pWriter = PipeWriter.Create(_readStream, new StreamPipeWriterOptions());
            _writeStream.CopyToAsync(_pWriter, _cancellationTokenSource.Token);
        }

        public override void Flush()
        {
            //Do not flush
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _readStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _writeStream.Write(buffer, offset, count);
        }

        public override bool CanRead => _readStream.CanRead;
        public override bool CanSeek => _readStream.CanSeek && _writeStream.CanSeek;
        public override bool CanWrite => _writeStream.CanWrite;
        public override long Length => _readStream.Length;

        public override long Position
        {
            get => _readStream.Position;
            set => _readStream.Position = value;
        }

        public override void Close()
        {
            base.Close();
            _writeStream.Close();
            _readStream.Close();
            _pWriter.Complete();
        }
    }
}