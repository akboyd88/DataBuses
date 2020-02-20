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
    /// </summary>
    public class SignalRDataBusException : Exception
    {
        /// <summary>
        /// </summary>
        public SignalRDataBusException(string message) : base(message)
        {

        }
    }
    
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
        private readonly HubConnection _hubConnection;

        /// <summary>
        /// 
        /// </summary>
        private readonly string _hubInvokeTarget;

        private volatile bool _isDisposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public SignalRDataBus(DataBusOptions options, ILoggerFactory loggerFactory) : base(options, loggerFactory)
        {

            _hubInvokeTarget = options.SupplementalSettings["hubInvokeTarget"];

            if (options.DataExchangeFormat == SerDerType.MessagePack)
            {
                throw new SignalRDataBusException("Message Pack Not Yet Supported for SignalR DataBus");
            }
            
            
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(options.SupplementalSettings["hubUrl"]))
                .AddJsonProtocol().Build();

            _hubConnection.On<T2>( options.SupplementalSettings["hubInvokeRecipient"], RecvData);
            _hubConnection.StartAsync().Wait();
        }
        
        
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing) {
                _hubConnection.StopAsync().Wait();
                _hubConnection.DisposeAsync().Wait();
            }
            
            _isDisposed = true;
            base.Dispose(disposing);
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