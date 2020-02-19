using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    internal class TcpClientDataBus<T1, T2> : BaseDataBus<T1, T2>
    {

        private readonly int _tcpServerPort;
        private readonly string _tcpServerHostname;
        private ILogger _logger;
        private volatile bool _isDisposed;
        private readonly TcpClient _tcpClient;
        private readonly ISerializer<T1> _serializer;
        private readonly IDeserializer<T2> _deserializer;

        public TcpClientDataBus(
            DataBusOptions dataBusOptions,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer,
            ILoggerFactory loggerFactory) : base(dataBusOptions, loggerFactory)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            _tcpServerHostname = dataBusOptions.SupplementalSettings["hostname"];
            _tcpServerPort = int.Parse(dataBusOptions.SupplementalSettings["port"]);
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger<UdpDataBus<T1, T2>>();
            _tcpClient = new TcpClient(_tcpServerHostname, _tcpServerPort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task SendData(T1 data, CancellationToken token)
        {
            ReadOnlyMemory<byte> outData = _serializer.Serialize(data);
            await _tcpClient.GetStream().WriteAsync(outData, token);
            await _tcpClient.GetStream().FlushAsync(token);
        }

        /// <summary>
        /// Extracts a single object from the stream using the deserializer in a streaming manner
        /// </summary>
        /// <param name="pObjTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
        }

        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && !this._readStopEvent.WaitOne(0, false))
                {
                    if (!_tcpClient.Connected)
                    {
                        await _tcpClient.ConnectAsync(_tcpServerHostname, _tcpServerPort);
                    }
                    
                    if (_tcpClient.Connected)
                    {
                       var data = await  _deserializer.Deserialize(_tcpClient.GetStream(), token);
                       AddToQueue(data);
                    }

                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }
            }, token);
        }

        public override void Dispose()
        {
            if (!_isDisposed)
            {
                BaseDispose();
                _isDisposed = true;
                _tcpClient.Close();
                _tcpClient.Dispose();
                _deserializer.Dispose();
            }
        }
        
    }
}