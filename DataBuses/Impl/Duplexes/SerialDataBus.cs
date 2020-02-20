using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    internal class SerialDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        private readonly string _serialPortName;
        private readonly SerialPort _serialPort;
        private readonly int _baudRate;
        private readonly int _dataBits;
        private readonly Parity _parity;
        private readonly StopBits _stopBit;
        private readonly ISerializer<T1> _serializer;
        private readonly IDeserializer<T2> _deserializer;
        private volatile bool _isDisposed;
        
        public SerialDataBus(
            ILoggerFactory loggerFactory, 
            ISerializer<T1> pSerializer,
            IDeserializer<T2> pDeserializer,
            DataBusOptions options) : base(options, loggerFactory)
        {
            _deserializer = pDeserializer;
            _serializer = pSerializer;
            _serialPortName = options.SupplementalSettings["port"];
            _baudRate = int.Parse(options.SupplementalSettings["baudRate"]);
            _dataBits = int.Parse(options.SupplementalSettings["dataBits"]);
            
            Enum.TryParse(options.SupplementalSettings["parity"], out _parity);
            Enum.TryParse(options.SupplementalSettings["stopBits"], out _stopBit);
            
            _serialPort = new SerialPort(
                _serialPortName, 
                _baudRate, 
                _parity, 
                _dataBits, 
                _stopBit);
            
            
            
            
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing) {
                _isDisposed = true;
                _serialPort.Close();
                _serialPort.Dispose();
                _deserializer.Dispose();
            }

            _isDisposed = true;
            base.Dispose(disposing);
        }

        protected override Task SendData(T1 data, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var raw = _serializer.Serialize(data);
                await _serialPort.BaseStream.WriteAsync(raw, token);
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