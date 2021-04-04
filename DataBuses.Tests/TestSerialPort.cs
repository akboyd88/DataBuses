using System.IO;
using Boyd.DataBuses.Interfaces.Hardware;

namespace Boyd.DataBuses.Tests
{
    public class TestSerialPort : ISerialPort
    {
        private readonly Stream _stream;
        private volatile bool _open;

        public TestSerialPort()
        {
            _stream = new TestStream();

        }

        public void Dispose()
        {
            _stream.Close();
        }

        public void Open()
        {
            //Mark open 
            _open = true;
        }

        public void Close()
        {
            //Mark close
            _open = false;
        }

        public Stream BaseStream => _stream;
        public bool IsOpen => _open;

        public long BytesToRead => _stream.Length;
    }
}