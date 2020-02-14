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
        private string _serialPortName;
        private SerialPort _serialPort;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private StopBits _stopBit;
        private ISerializer<T1> _serializer;
        private IDeserializer<T2> _deserializer;
        private volatile bool _isDisposed;
        
        public SerialDataBus(
            ILoggerFactory loggerFactory, 
            DataBusOptions options) : base(loggerFactory)
        {
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

        public override void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _serialPort.Close();
                _serialPort.Dispose();
                BaseDispose();
            }

        }

        protected override Task SendData(T1 data, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var raw = _serializer.Serialize(data);
                _serialPort.Write(raw.ToArray(), 0, raw.Length);
            });

        }

        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(() =>
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                }

                while (!token.IsCancellationRequested && !_readStopEvent.WaitOne(0, false))
                {
                    if (_serialPort.BytesToRead > 0)
                    {

                    }
                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }

            });
        }
    }
}