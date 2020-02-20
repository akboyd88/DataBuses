using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    /// <summary>
    /// Current assumptions, messages can be delivered in one data gram, ser/deser uses message pack.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    internal class UdpDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        private ILogger _logger;
        private UdpClient _udpClient;
        private int _receivePort;
        private string _remoteHost;
        private int _remotePort;
        private ISerializer<T1> _serializer;
        private IDeserializer<T2> _deserializer;
        private volatile bool _isDisposed;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBusOptions"></param>
        /// <param name="serializer"></param>
        /// <param name="deserializer"></param>
        /// <param name="loggerFactory"></param>
        internal UdpDataBus(
            DataBusOptions dataBusOptions,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer,
            ILoggerFactory loggerFactory) : base(dataBusOptions, loggerFactory)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            _remoteHost = dataBusOptions.SupplementalSettings["remoteHost"];
            _remotePort = int.Parse(dataBusOptions.SupplementalSettings["remotePort"]);
            _receivePort = int.Parse(dataBusOptions.SupplementalSettings["receivePort"]);
            if(loggerFactory != null)
                _logger = loggerFactory.CreateLogger<UdpDataBus<T1, T2>>();
            _udpClient = new UdpClient(_receivePort);
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
            await _udpClient.SendAsync(outData.ToArray(), outData.Length, _remoteHost, _remotePort);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pObjTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            _udpClient.Client.ReceiveTimeout = pObjTimeout.Milliseconds;
            var datagram = await _udpClient.ReceiveAsync();
            var data = new ReadOnlyMemory<byte>(datagram.Buffer);
            var deserializedResult = _deserializer.Deserialize(data);
            EgressDataAvailableWaitHandle.Reset();
            return deserializedResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!token.IsCancellationRequested && !this._readStopEvent.WaitOne(0, false))
                    {
                        if (_udpClient.Available > 0 && !this.EgressDataAvailableWaitHandle.WaitOne(0, false))
                        {
                            this.EgressDataAvailableWaitHandle.Set();
                            this.FireEgressDataAvailableEvt();
                        }
                        _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                    }
                }
                catch (Exception e)
                {
                    if (_logger != null)
                    {
                        _logger.LogError("Exception occured in UDP Data Bus Read Task {0}", e);
                    }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing) {
                _udpClient.Close();
                _udpClient.Dispose();
                _deserializer.Dispose();
            }
            _isDisposed = true;
            base.Dispose(disposing);
        }

        ~UdpDataBus()
        {
            Dispose();
        }
    }
}