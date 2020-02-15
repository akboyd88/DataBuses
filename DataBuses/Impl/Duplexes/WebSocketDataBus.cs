using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Impl.Duplexes
{
    internal class WebSocketDataBus<T1, T2> : BaseDataBus<T1, T2>
    {
        private ClientWebSocket _clientWebSocket;
        private string _wsSocketServerUrl;
        private string _wsSocketSubProtocol;
        private IDictionary<string, string> _wsCustomHeaders;
        private ISerializer<T1> _serializer;
        private IDeserializer<T2> _deserializer;
        private int _bufferSize = 4096;
        private int _messageBufferMaxSize;
        private ILogger _logger;
        private BlockingCollection<T2> _messageQueue;
        private EventWaitHandle _openEvent;

        private volatile bool _isDisposed;

        public WebSocketDataBus(
            ILoggerFactory loggerFactory,
            DataBusOptions options,
            ISerializer<T1> serializer,
            IDeserializer<T2> deserializer) : base(loggerFactory)
        {
            if (loggerFactory != null)
                _logger = loggerFactory.CreateLogger<WebSocketDataBus<T1, T2>>();
            _wsSocketServerUrl = options.SupplementalSettings["url"];
            _wsSocketSubProtocol = options.SupplementalSettings["subProtocol"];
            _messageBufferMaxSize = int.Parse(options.SupplementalSettings["maxBufferedMessages"]);
            _openEvent =new EventWaitHandle(false, EventResetMode.AutoReset);
            _messageQueue = new BlockingCollection<T2>(_messageBufferMaxSize);


            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.Options.AddSubProtocol(_wsSocketSubProtocol);
            _wsCustomHeaders = new Dictionary<string, string>();
            _serializer = serializer;
            _deserializer = deserializer;
            ExtractCustomHeaders(options.SupplementalSettings);
            SetWebSocketCustomHeaders();
        }

        private void SetWebSocketCustomHeaders()
        {
            foreach (var kvp in _wsCustomHeaders)
            {
                _clientWebSocket.Options.SetRequestHeader(kvp.Key, kvp.Value);
            }
        }
        
        private void ExtractCustomHeaders(IDictionary<string, string> dict)
        {
            foreach (KeyValuePair<string, string> kvp in dict)
            {
               var result =  kvp.Key.Split('.');
               if (result.Length > 0 && result[0].ToLower() == "header")
               {
                   _wsCustomHeaders[result[1]] = kvp.Key;
               }
            }
        }

        public override void Dispose()
        {
            if (!_isDisposed)
            {
                BaseDispose();
                _isDisposed = true;
                CancellationTokenSource cancelSource = new CancellationTokenSource();
                cancelSource.CancelAfter(250);
                if (_clientWebSocket.State == WebSocketState.Open ||
                    _clientWebSocket.State == WebSocketState.CloseReceived ||
                    _clientWebSocket.State == WebSocketState.CloseSent)
                {
                    _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Graceful Close", cancelSource.Token).Wait();
                }
                cancelSource.Dispose();
                _clientWebSocket.Dispose();
                _messageQueue.Dispose();
                _openEvent.Dispose();

            }
        }

        protected override Task SendData(T1 data, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var raw = _serializer.Serialize(data);
                if (_clientWebSocket.State != WebSocketState.Open)
                {
                    _openEvent.WaitOne();
                }
                await _clientWebSocket.SendAsync(raw, WebSocketMessageType.Binary, true, token);
            }, token);

        }

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

        private void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }

        private void AddToQueue(T2 data)
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

        protected override Task CreateReadTask(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                if (_clientWebSocket.State != WebSocketState.Connecting &&
                    _clientWebSocket.State != WebSocketState.Open)
                {
                    await _clientWebSocket.ConnectAsync(new Uri(_wsSocketServerUrl), token);
                    _openEvent.Set();
                }

                var bufferList = new List<Tuple<byte[], int>>();
                
                while (!token.IsCancellationRequested && !_readStopEvent.WaitOne(0, false))
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                    try
                    {
                        var result = await _clientWebSocket.ReceiveAsync(buffer, token);
                        if (result.CloseStatus == null)
                        {
                            if (result.EndOfMessage)
                            {
                                byte[] totalMessageBuffer;
                                if (bufferList.Count == 0)
                                {
                                    totalMessageBuffer = buffer;
                                }
                                else
                                {
                                    int totalSize = result.Count;
                                    foreach (var t in bufferList)
                                    {
                                        totalSize += t.Item2;
                                    }

                                    totalMessageBuffer = ArrayPool<byte>.Shared.Rent(totalSize);
                                    int copyOffset = 0;

                                    foreach (var t in bufferList)
                                    {
                                        Buffer.BlockCopy(t.Item1, 0, totalMessageBuffer, copyOffset, t.Item2);
                                        ArrayPool<byte>.Shared.Return(t.Item1);
                                        copyOffset += t.Item2;
                                    }
                                    bufferList.Clear();
                                    Buffer.BlockCopy(buffer, 0, totalMessageBuffer, copyOffset, buffer.Length);
                                    ArrayPool<byte>.Shared.Return(buffer);

                                }

                                var serResult = _deserializer.Deserialize(buffer);
                                AddToQueue(serResult);
                                ArrayPool<byte>.Shared.Return(totalMessageBuffer);
                            }
                            else
                            {
                                bufferList.Add(new Tuple<byte[], int>(buffer, result.Count));
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        Log(LogLevel.Error, e.Message);
                    }
                    _readStopEvent.WaitOne(TimeSpan.FromMilliseconds(50));
                }
            });
        }
    }
}