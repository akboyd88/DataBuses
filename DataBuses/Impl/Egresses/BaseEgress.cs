﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Egresses
{

    internal abstract class BaseEgress<T> : IDataEgress<T>
    {
        private Task _readTask;
        private CancellationTokenSource _readTaskCancelSource;
        protected readonly EventWaitHandle _readStopEvent;
        private readonly EventWaitHandle _readDataAvailableEvent;
        private volatile bool _isDisposed;
        private readonly ILogger _logger;


        protected BaseEgress(
            ILoggerFactory loggerFactory)
        {
            if(loggerFactory != null) 
                _logger = loggerFactory.CreateLogger<BaseEgress<T>>();
            _readTaskCancelSource = new CancellationTokenSource();
            _readStopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _readDataAvailableEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }
        protected void FireEgressDataAvailableEvt()
        {
            this.OnEgressDataAvailableEvt?.Invoke();
        }
        
        /// <summary>
        /// Dispose/cleanup of resources
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void BaseDispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                CleanUpReadTask();
                _readDataAvailableEvent.Dispose();
                _readStopEvent.Dispose();
                _readTaskCancelSource.Dispose();
                
            }
        }

        private void CleanUpReadTask()
        {
            if (_readTask != null)
            {
                _readStopEvent.Set();
                //TODO: move to configurable value or const defined value
                var result = _readTask.Wait(TimeSpan.FromMilliseconds(500));
                if (!result)
                {
                    _readTaskCancelSource.Cancel();
                    _readTaskCancelSource.Dispose();
                    
                    _readTaskCancelSource = new CancellationTokenSource();
                }

                _readTask = null;
            }
        }
        
        protected void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }
        
        /// <summary>
        /// Starts the reading task responsible for offloading data from the underlying bus
        /// </summary>
        public void StartReading()
        {
            CleanUpReadTask();
            _readTask = CreateReadTask(_readTaskCancelSource.Token);
        }

        public async Task<T> TakeData(TimeSpan pObjTimeout, CancellationToken pCancelToken)
        {
            return await GetData(pObjTimeout, pCancelToken).ConfigureAwait(false);
        }

        public event EgressDataAvailableEvt OnEgressDataAvailableEvt;

        public EventWaitHandle EgressDataAvailableWaitHandle => _readDataAvailableEvent;
        public abstract void Dispose();

        protected abstract Task CreateReadTask(CancellationToken token);
        protected abstract Task<T> GetData(TimeSpan pObjTimeout, CancellationToken token);

    }
}