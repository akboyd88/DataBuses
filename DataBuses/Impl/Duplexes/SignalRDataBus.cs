using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Models;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    internal class SignalRDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        /// <summary>
        /// 
        /// </summary>
        private HubConnection _hubConnection;
        /// <summary>
        /// 
        /// </summary>
        private string _hubUrl;
        /// <summary>
        /// 
        /// </summary>
        private IHubProxy _hubProxy;
        /// <summary>
        /// 
        /// </summary>
        private string _hubInvokeRecipient;
        /// <summary>
        /// 
        /// </summary>
        private string _hubInvokeTarget;
        /// <summary>
        /// 
        /// </summary>
        private string _hubName;
        /// <summary>
        /// 
        /// </summary>
        private int _messageBufferMaxSize;
        /// <summary>
        /// 
        /// </summary>
        private BlockingCollection<T2> _messageQueue;
        /// <summary>
        /// 
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public SignalRDataBus(DataBusOptions options, ILoggerFactory loggerFactory) : base(loggerFactory)
        {

            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<SignalRDataBus<T1, T2>>();
        
            _hubUrl = options.SupplementalSettings["hubUrl"];
            _hubName = options.SupplementalSettings["hubName"];
            _hubInvokeRecipient = options.SupplementalSettings["hubInvokeRecipient"];
            _hubInvokeTarget = options.SupplementalSettings["hubInvokeTarget"];
            _messageBufferMaxSize = int.Parse(options.SupplementalSettings["maxBufferedMessages"]);
            _messageQueue = new BlockingCollection<T2>(_messageBufferMaxSize);
            
            _hubConnection = new HubConnection(_hubUrl);
            _hubProxy = _hubConnection.CreateHubProxy(_hubName);
            _hubProxy.On<T2>(_hubInvokeRecipient, RecvData);
            _hubConnection.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            _hubConnection.Stop();
            _hubConnection.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        private void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void RecvData(T2 data)
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task SendData(T1 data, CancellationToken token)
        {
            await _hubProxy.Invoke(_hubInvokeTarget, data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pObjTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var extraSource = new CancellationTokenSource(pObjTimeout);
                var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, extraSource.Token);
                var item = _messageQueue.Take(linkedSource.Token);
                if (_messageQueue.Count == 0)
                {
                    EgressDataAvailableWaitHandle.Reset();
                }
                return item;
            }, token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}