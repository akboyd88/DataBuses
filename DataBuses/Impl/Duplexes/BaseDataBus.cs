using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
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
        private volatile bool _isDisposed;
        private readonly ILogger _logger;
        
        /// <summary>
        /// 
        /// </summary>
        protected BlockingCollection<T2> _messageQueue;

        /// <summary>
        /// Fired when data is available to be taken out of the egress
        /// </summary>
        public event EgressDataAvailableEvt OnEgressDataAvailableEvt;

        /// <summary>
        /// Gets a wait handle that can be awaited for the next time data is available to be taken from the
        /// underlying data bus
        /// </summary>
        public EventWaitHandle EgressDataAvailableWaitHandle { get; }

        protected void FireEgressDataAvailableEvt()
        {
            this.OnEgressDataAvailableEvt?.Invoke();
        }

        public int MessagesInQueueCount
        {
            get { return _messageQueue.Count; }
        }
        
        internal BaseDataBus(
            DataBusOptions options,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory != null)
            {
                _logger = loggerFactory.CreateLogger<BaseDataBus<T1, T2>>();
            }

            _messageQueue = options.MaxBufferedMessages != null ? new BlockingCollection<T2>(options.MaxBufferedMessages.Value) : new BlockingCollection<T2>();
            _readTaskCancelSource = new CancellationTokenSource();
            _readStopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            EgressDataAvailableWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);           
        }
   
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing) {
                CleanUpReadTask();
            }
      
            _isDisposed = true;
        }
        
        protected void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }
        
        protected void AddToQueue(T2 data)
        {
            var newMsg = true;
            if (!_messageQueue.TryAdd(data))
            {
                //take something out to make room
                if (!_messageQueue.TryTake(out var item))
                {
                    //lost incoming due to queue size limit, warn
                    Log(LogLevel.Warning, "Failed to free up space in the message queue buffer, incoming message lost!");
                    newMsg = false;
                }
                else
                {
                    if (!_messageQueue.TryAdd(data))
                    {
                        //lost incoming and lost oldest message
                        Log(LogLevel.Critical, "Failed to add a message after freeing up space in message buffer, oldest and newest message lost!");
                        newMsg = false;
                    }

                }
                
            }

            if (newMsg)
            {
                EgressDataAvailableWaitHandle.Set();
                FireEgressDataAvailableEvt();
            }
        }
        
        protected Task<T2> TakeFromQueue(TimeSpan pObjTimeout, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var extraSource = new CancellationTokenSource();
                extraSource.CancelAfter(pObjTimeout);
                var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, extraSource.Token);
                var item = _messageQueue.Take(linkedSource.Token);
                if (_messageQueue.Count == 0)
                {
                    EgressDataAvailableWaitHandle.Reset();
                }
                return item;
            }, token);
        }


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
            await SendData(pObjDataToIngest, pCancelToken).ConfigureAwait(false);
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
            return  await GetData(pObjTimeout, pCancelToken).ConfigureAwait(false);
        }

        protected abstract Task SendData(T1 data, CancellationToken token);
        protected abstract Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token);
        protected abstract Task CreateReadTask(CancellationToken token);
    }
}