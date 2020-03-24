using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Interfaces.Internal;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    internal class SerialDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        private readonly ISerialPort _serialPort;
        private readonly ISerializer<T1> _serializer;
        private readonly IDeserializer<T2> _deserializer;
        private volatile bool _isDisposed;
        
        public SerialDataBus(
            ILoggerFactory loggerFactory, 
            ISerializer<T1> pSerializer,
            IDeserializer<T2> pDeserializer,
            ISerialPort pSerialPort,
            DataBusOptions options) : base(options, loggerFactory)
        {
            _deserializer = pDeserializer;
            _serializer = pSerializer;
            _serialPort = pSerialPort;

        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            
            base.Dispose(disposing);

            if (disposing) {
                _isDisposed = true;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort.Dispose();
                _deserializer.Dispose();
            }

            _isDisposed = true;
        }

        protected override Task SendData(T1 data, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var raw = _serializer.Serialize(data);
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }
                await _serialPort.BaseStream.WriteAsync(raw, token);
                await _serialPort.BaseStream.FlushAsync(token);
            }, token);

        }

        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
        }

        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }

                while (!token.IsCancellationRequested && !_readStopEvent.WaitOne(0, false))
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        var data = await  _deserializer.Deserialize(_serialPort.BaseStream, token);
                        AddToQueue(data);
                    }
                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }

            }, token);
        }
    }
}