using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    /// <summary>
    /// Base class for shared functionality between implementations
    /// </summary>
    /// <typeparam name="T1">Data In type</typeparam>
    /// <typeparam name="T2">Data Out Type</typeparam>
    internal abstract class BaseDataBus<T1, T2> : IDataDuplex<T1, T2>
    {
        private Task _readTask;
        
        private CancellationTokenSource _readTaskCancelSource;
        protected readonly EventWaitHandle _readStopEvent;
        private readonly EventWaitHandle _readDataAvailableEvent;
        private volatile bool _isDisposed;
        private ILogger _logger;

        /// <summary>
        /// Fired when data is available to be taken out of the egress
        /// </summary>
        public event EgressDataAvailableEvt OnEgressDataAvailableEvt;

        /// <summary>
        /// Gets a wait handle that can be awaited for the next time data is available to be taken from the
        /// underlying data bus
        /// </summary>
        public EventWaitHandle EgressDataAvailableWaitHandle
        {
            get { return _readDataAvailableEvent; }
        }

        protected void FireEgressDataAvailableEvt()
        {
            this.OnEgressDataAvailableEvt?.Invoke();
        }
        
        internal BaseDataBus(
            ILoggerFactory loggerFactory)
        {
            if(loggerFactory != null) 
                _logger = loggerFactory.CreateLogger<BaseDataBus<T1, T2>>();
            _readTaskCancelSource = new CancellationTokenSource();
            _readStopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _readDataAvailableEvent = new EventWaitHandle(false, EventResetMode.ManualReset);
        }
        
        
        /// <summary>
        /// Dispose/cleanup of resources
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        protected void BaseDispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                CleanUpReadTask();
                
            }
        }

        abstract public void Dispose();

        private void CleanUpReadTask()
        {
            if (_readTask != null)
            {
                _readStopEvent.Set();
                //TODO: move to configurable value or const defined value
                var result = _readTask?.Wait(TimeSpan.FromMilliseconds(500));
                if (result == false)
                {
                    _readTaskCancelSource.Cancel();
                    _readTaskCancelSource.Dispose();
                    
                    _readTaskCancelSource = new CancellationTokenSource();
                }

                _readTask = null;
            }
        }
        
        /// <summary>
        /// Starts the reading task responsible for offloading data from the underlying bus
        /// </summary>
        public void StartReading()
        {
            CleanUpReadTask();
            _readTask = CreateReadTask(_readTaskCancelSource.Token);
        }
        
        /// <summary>
        /// Ingest the data and forward it to the underlying transport
        /// </summary>
        /// <param name="pObjDataToIngest">Data object to ingest into the data bus</param>
        /// <param name="pCancelToken">Cancellation token to stop the write/ingest</param>
        /// <returns>A Task that can be awaited, if it fails to ingest the data it throws an exception, if the task
        /// completes without exception the data has been handed off to the next layer.</returns>
        public async Task PutData(T1 pObjDataToIngest, CancellationToken pCancelToken)
        {
            DataIngestedEvt?.Invoke(pObjDataToIngest);
            await SendData(pObjDataToIngest, pCancelToken);
            DataIngestCommittedEvt?.Invoke(pObjDataToIngest);

            
        }
        
        /// <summary>
        /// Fired when Ingest is called
        /// </summary>
        public event DataIngestedEvt<T1> DataIngestedEvt;

        /// <summary>
        /// Fired when data has been ingested and fully committed to the underlying data buss.
        /// </summary>
        public event DataIngestCommitted DataIngestCommittedEvt;
        

        /// <summary>
        /// Take the next data from the underlying data bus, provide a timeout and cancellation token
        /// </summary>
        /// <param name="pObjTimeout">How long to wait to take the data from the underlying bus</param>
        /// <param name="pCancelToken">CancellationToken</param>
        /// <returns></returns>
        public async Task<T2> TakeData(TimeSpan pObjTimeout, CancellationToken pCancelToken)
        {
            return  await GetData(pObjTimeout, pCancelToken);
        }



        protected abstract Task SendData(T1 data, CancellationToken token);
        protected abstract Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token);
        protected abstract Task CreateReadTask(CancellationToken token);
    }
}