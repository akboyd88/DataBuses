using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Ingresses
{
    internal class UdpIngress<T> : BaseIngress<T>
    {
        private int _remotePort;
        private string _remoteHost;
        private UdpClient _udpClient;
        private ISerializer<dynamic> _serializer;
        private volatile bool _isDisposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBusOptions"></param>
        /// <param name="serializer"></param>
        /// <param name="loggerFactory"></param>
        internal UdpIngress(
            DataBusOptions dataBusOptions,
            ISerializer<dynamic> serializer,
            ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _serializer = serializer;
            _remoteHost = dataBusOptions.SupplementalSettings["remoteHost"];
            _remotePort = int.Parse(dataBusOptions.SupplementalSettings["remotePort"]);
            _udpClient = new UdpClient();
        }
        
        
        protected override async Task SendData(T data, CancellationToken token)
        {
            ReadOnlyMemory<byte> serData = _serializer.Serialize(data);
            await _udpClient.SendAsync(serData.ToArray(), serData.Length, _remoteHost, _remotePort);
        }

        public override void Dispose()
        {
            if (!_isDisposed)
            {               
                BaseDispose();
                _isDisposed = true;
                _udpClient.Close();
                _udpClient.Dispose();
 
            }
        }
        
        ~UdpIngress()
        {
            Dispose();
        }
    }
}