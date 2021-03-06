﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Egresses
{
    internal class UdpEgress<T> : BaseEgress<T>
    {
        private readonly IDeserializer<T> _deserializer;
        private readonly UdpClient _udpClient;
        private volatile bool _isDisposed;
        
        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(() =>
            {
                while (!token.IsCancellationRequested && !this._readStopEvent.WaitOne(0, false))
                {
                    if (_udpClient.Available > 0 && !this.EgressDataAvailableWaitHandle.WaitOne(0, false))
                    {
                        this.EgressDataAvailableWaitHandle.Set();
                        this.FireEgressDataAvailableEvt();
                    }
                }
            },token);
        }

        protected override async Task<T> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            var datagram = await _udpClient.ReceiveAsync();
            var data = new ReadOnlyMemory<byte>(datagram.Buffer);
            var deserializedResult = _deserializer.Deserialize(data);
            EgressDataAvailableWaitHandle.Reset();
            return deserializedResult;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataBusOptions"></param>
        /// <param name="deserializer"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public UdpEgress(
            DataBusOptions dataBusOptions,
            IDeserializer<T> deserializer,
            ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            _deserializer = deserializer;
            _udpClient = new UdpClient(int.Parse(dataBusOptions.SupplementalSettings["receivePort"]));
        }

        /// <summary>
        /// 
        /// </summary>
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

        ~UdpEgress()
        {
            Dispose();
        }
    }
}