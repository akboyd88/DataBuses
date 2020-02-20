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
    /// Generic exception for anything thrown by the SignalRDataBus
    /// </summary>
    public class SignalRDataBusException : Exception
    {

        /// <summary>
        /// Create a new SignalRDataBusException with a custom message
        /// </summary>
        /// <param name="message">custom exception message</param>
        public SignalRDataBusException(string message) : base(message)
        {

        }
    }
    

    /// <summary>
    /// SignalR Data Bus that takes a configurable invoke receipient to receive messages and invoke target to send messages
    /// to the signal r hub 
    /// </summary>
    /// <typeparam name="T1">Outoing data type</typeparam>
    /// <typeparam name="T2">Incoming data type</typeparam>
    internal class SignalRDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        
        private readonly HubConnection _hubConnection;
        private readonly string _hubInvokeTarget;
        private volatile bool _isDisposed;


        /// <summary>
        /// Create a new SignalRDataBus instance
        /// </summary>
        /// <param name="options">DataBus options specifying what signal r server to connect to, what remote method to invoke and what client event 
        /// to list for</param>
        /// <param name="loggerFactory">Logger factory that provides a ILogger instance to this instance for logging informatoin</param>
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
        

        
        /// <summary>
        /// Dispose resources that implement IDisposable or need to be cleaned up such as connections
        /// </summary>
        /// <param name="disposing">if resources in this object should be cleaned up</param>
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
        /// Incoming data handler that pushes messages into a message queue
        /// </summary>
        /// <param name="data">Data sent from remote SignalR server</param>
        private void RecvData(T2 data)
        {
            AddToQueue(data);
        }
        
        /// <summary>
        /// Send data to the remote SignalR server
        /// </summary>
        /// <param name="data">data to send to server</param>
        /// <param name="token">cancellation token to abort request</param>
        /// <returns>Task that completes when invocation is complete</returns>
        protected override async Task SendData(T1 data, CancellationToken token)
        {
            await _hubConnection.InvokeAsync<T1>(_hubInvokeTarget, data, token);
        }

        /// <summary>
        /// Get data from the message queue, waiting if no data is available yet
        /// </summary>
        /// <param name="pObjTimeout">How long to wait to get data out of the message queue</param>
        /// <param name="token">cancellation token to abort the process of getting the data from the queue</param>
        /// <returns>A task that completes with the next data from the message queue</returns>
        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
        }

        
        /// <summary>
        /// Required by the base class but not currently needed for this SignalR DataBus
        /// </summary>
        /// <param name="token">cancellation token to abort the read task</param>
        /// <returns>Completed Task</returns>
        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}