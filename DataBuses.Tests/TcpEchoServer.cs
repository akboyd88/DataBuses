using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Boyd.DataBuses.Tests
{
    public class TcpEchoServer : IDisposable
    {
        private int _listenPort;
        private TcpListener _listener;
        private Task _listenTask;
        private CancellationTokenSource _cancellationTokenSource;
        private IList<TcpClient> _clients;
        private volatile bool _serve;
        private volatile bool _isDisposed;

        public TcpEchoServer(int listenPort)
        {
            _listenPort = listenPort;
            _listener = new TcpListener(IPAddress.Any, _listenPort);
            _cancellationTokenSource = new CancellationTokenSource();
            _clients = new List<TcpClient>();
            _serve = true;
            _listener.Start();
            _listenTask = Listen();

        }

        private void EchoClient(TcpClient client)
        {
            Task.Run(async () =>
            {
                while (client.Connected && _serve)
                {
                    if (client.Available > 0)
                    {
                        var rentedBuffer = ArrayPool<byte>.Shared.Rent(client.Available);
                        var dataRead = await client.GetStream().ReadAsync(rentedBuffer, 0, client.Available,
                            _cancellationTokenSource.Token);
                        await client.GetStream().WriteAsync(rentedBuffer, 0, dataRead, _cancellationTokenSource.Token);
                        await client.GetStream().FlushAsync(_cancellationTokenSource.Token);
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }

                    _cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(50));
                }
            }, _cancellationTokenSource.Token);

        }

        private Task Listen()
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested && _serve)
                    {
                        if (_listener.Pending() && _serve)
                        {
                            var client = await _listener.AcceptTcpClientAsync();
                            EchoClient(client);
                            _clients.Add(client);
                        }

                        _cancellationTokenSource.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(50));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }, _cancellationTokenSource.Token);
        }

        public void Close()
        {
            _serve = false;
            foreach (var client in _clients)
            {
                client.Close();
                client.Dispose();
            }
            _listener.Stop();

            var result =_listenTask.Wait(TimeSpan.FromMilliseconds(250));
            if (!result)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource.Dispose();
        }

        public void Dispose()
        {
            if(!_isDisposed)
            {
                _isDisposed = true;
                Close();
            }
        }
    }
}