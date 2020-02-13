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

        private int _tcpServerPort;
        private string _tcpServerHostname;
        private ILogger _logger;
        private volatile bool _isDisposed;
        private TcpClient _tcpClient;
        private ISerializer<T1> _serializer;
        private IDeserializer<T2> _deserializer;

        public TcpClientDataBus(
            DataBusOptions dataBusOptions,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer,
            ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            _tcpServerHostname = dataBusOptions.SupplementalSettings["hostname"];
            _tcpServerPort = int.Parse(dataBusOptions.SupplementalSettings["port"]);
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger<UdpDataBus<T1, T2>>();
            _tcpClient = new TcpClient(_tcpServerHostname, _tcpServerPort);
        }

        protected override async Task SendData(T1 data, CancellationToken token)
        {
            ReadOnlyMemory<byte> outData = _serializer.Serialize(data);
            _tcpClient.GetStream().Write(outData.Span);
            _tcpClient.GetStream().Flush();
        }

        /// <summary>
        /// Extracts a single object from the stream using the deserializer in a streaming manner
        /// </summary>
        /// <param name="pObjTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            var stream = _tcpClient.GetStream();
            var localTimeOutCancel = new CancellationTokenSource();
            var joinedToken = CancellationTokenSource.CreateLinkedTokenSource(localTimeOutCancel.Token, token);
            //localTimeOutCancel.CancelAfter(pObjTimeout);
            var deserializedResult = await _deserializer.Deserialize(stream, joinedToken.Token);
            localTimeOutCancel.Dispose();
            EgressDataAvailableWaitHandle.Reset();
            return deserializedResult;
            
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
                    
                    if (_tcpClient.Connected && 
                        _tcpClient.Available > 0 && 
                        !EgressDataAvailableWaitHandle.WaitOne(0, false))
                    {
                        EgressDataAvailableWaitHandle.Set();
                        FireEgressDataAvailableEvt();
                    }

                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }
            });
        }

        public override void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _tcpClient.Close();
                _tcpClient.Dispose();
                BaseDispose();
            }
        }
        
    }
}