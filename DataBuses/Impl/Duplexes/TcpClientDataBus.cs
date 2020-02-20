using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    /// <summary>
    /// Duplex data bus that connects to a listening TCP socket server. Assumes 
    /// exchange is message based and uses a provided serializer and deserializer
    /// to pop and push messages off the network stream.
    /// </summary>
    /// <typeparam name="T1">Message type sent by the socket server</typeparam>
    /// <typeparam name="T2">Message type socket server expects</typeparam>
    internal class TcpClientDataBus<T1, T2> : BaseDataBus<T1, T2>
    {

        private readonly int _tcpServerPort;
        private readonly string _tcpServerHostname;
        private volatile bool _isDisposed;
        private readonly TcpClient _tcpClient;
        private readonly ISerializer<T1> _serializer;
        private readonly IDeserializer<T2> _deserializer;

        /// <summary>
        /// TCP Data Bus Constructor
        /// </summary>
        /// <param name="dataBusOptions">Generic shared options</param>
        /// <param name="serializer">serializer that converts outgoing objects to bytes</param>
        /// <param name="deserializer">deserializer the converts incoming bytes to desired type</param>
        /// <param name="loggerFactory">logger factory that provides ILogger for instance</param>
        /// <returns></returns>
        public TcpClientDataBus(
            DataBusOptions dataBusOptions,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer,
            ILoggerFactory loggerFactory) : base(dataBusOptions, loggerFactory)
        {
            _serializer = serializer;
            _deserializer = deserializer;
            _tcpServerHostname = dataBusOptions.SupplementalSettings["hostname"];
            _tcpServerPort = int.Parse(dataBusOptions.SupplementalSettings["port"]);
            _tcpClient = new TcpClient(_tcpServerHostname, _tcpServerPort);
        }

        /// <summary>
        /// Send data to the socket server
        /// </summary>
        /// <param name="data">Data to send to the socket server</param>
        /// <param name="token">Cancellation token to abort send</param>
        /// <returns>Task that completes with send result</returns>
        protected override async Task SendData(T1 data, CancellationToken token)
        {
            ReadOnlyMemory<byte> outData = _serializer.Serialize(data);
            await _tcpClient.GetStream().WriteAsync(outData, token);
            await _tcpClient.GetStream().FlushAsync(token);
        }

        /// <summary>
        /// Extracts a single object from the stream using the deserializer in a streaming manner
        /// </summary>
        /// <param name="pObjTimeout">amount of time to wait to retrieve the messages from the stream</param>
        /// <param name="token">cancellation to cancel call</param>
        /// <returns>Task that completes with a new data object from the stream or is cancelled due to timeout</returns>
        protected override Task<T2> GetData(TimeSpan pObjTimeout, CancellationToken token)
        {
            return TakeFromQueue(pObjTimeout, token);
        }

        /// <summary>
        /// Create a read task that extracts data from the TCP network stream and maintains the socket connection, 
        /// converts the data into the desired type and inserts it into the message buffer.
        /// </summary>
        /// <param name="token">CancellationToken to cancel the read task</param>
        /// <returns>Task that doesn't complete until the databus is closed/disposed</returns>
        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && !this._readStopEvent.WaitOne(0, false))
                {
                    if (!_tcpClient.Connected)
                    {
                        await _tcpClient.ConnectAsync(_tcpServerHostname, _tcpServerPort);
                    }
                    
                    if (_tcpClient.Connected)
                    {
                       var data = await  _deserializer.Deserialize(_tcpClient.GetStream(), token);
                       AddToQueue(data);
                    }

                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }
            }, token);
        }


        /// <summary>
        /// Clean up connections and other resources that implement IDispoable that were
        /// allocated for this instance
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            base.Dispose(disposing);

            if (disposing) {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _deserializer.Dispose();
            }
            _isDisposed = true;
        }
        
    }
}