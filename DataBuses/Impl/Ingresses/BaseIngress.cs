﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Ingresses
{
    internal abstract class BaseIngress<T> : IDataIngress<T>
    {
        public event DataIngestedEvt<T> DataIngestedEvt;
        public event DataIngestCommitted DataIngestCommittedEvt;
        private readonly ILogger _logger;
        private volatile bool _isDisposed;

        protected BaseIngress(ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
            {
                _logger = loggerFactory.CreateLogger<BaseIngress<T>>();
            }
        }
        
        protected void BaseDispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        public async Task PutData(T pObjDataToIngest, CancellationToken pCancelToken)
        {
            DataIngestedEvt?.Invoke(pObjDataToIngest);

            await SendData(pObjDataToIngest, pCancelToken).ConfigureAwait(false);
            DataIngestCommittedEvt?.Invoke(pObjDataToIngest);
            
        }

        public void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }

        public abstract void Dispose();

        protected abstract Task SendData(T data, CancellationToken token);

    }
}