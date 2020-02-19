using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
        private string _hubInvokeRecipient;
        /// <summary>
        /// 
        /// </summary>
        private string _hubInvokeTarget;

        /// <summary>
        /// 
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public SignalRDataBus(DataBusOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {

            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<SignalRDataBus<T1, T2>>();
        
            _hubUrl = options.SupplementalSettings["hubUrl"];
            _hubInvokeRecipient = options.SupplementalSettings["hubInvokeRecipient"];
            _hubInvokeTarget = options.SupplementalSettings["hubInvokeTarget"];

            if (options.DataExchangeFormat == SerDerType.MessagePack)
            {
                throw new Exception("Message Pack Not Yet Supported for SignalR DataBus");
            }
            
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(_hubUrl))
                .AddJsonProtocol().Build();

            _hubConnection.On<T2>(_hubInvokeRecipient, RecvData);
            _hubConnection.StartAsync().Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            _hubConnection.StopAsync().Wait();
            _hubConnection.DisposeAsync().Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void RecvData(T2 data)
        {
            AddToQueue(data);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override async Task SendData(T1 data, CancellationToken token)
        {
            await _hubConnection.InvokeAsync<T1>(_hubInvokeTarget, data, token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pObjTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
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