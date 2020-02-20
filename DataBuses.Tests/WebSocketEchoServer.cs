

using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boyd.DataBuses.Tests
{
    public class WebSocketEchoServer
    {
        private readonly IWebHost host;
        public WebSocketEchoServer(string bindUrl)
        {
            host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(bindUrl)
                .UseStartup<WebSocketEchoServerApp>()
                .Build(); 
            host.Start();
        }

        public async Task Stop()
        {
            
            await host.StopAsync();
        }
    }

    class WebSocketEchoServerApp : IDisposable
    {
        private readonly CancellationTokenSource stopCts;
        private  volatile bool _isDisposed;
        
        public WebSocketEchoServerApp(IConfiguration pConfig)
        {
            stopCts = new CancellationTokenSource();
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
        }
        
        

        private async Task Echo(HttpContext context, WebSocket ws)
        {
            while (!stopCts.IsCancellationRequested)
            {
                var backingArray = ArrayPool<byte>.Shared.Rent(4096);
                var buffer = new Memory<byte>(backingArray);
                var result = await ws.ReceiveAsync(buffer, stopCts.Token);
                if ( result.Count > 0)
                {
                    var sendBuffer = new ReadOnlyMemory<byte>(backingArray, 0, result.Count);
                    await ws.SendAsync(sendBuffer, WebSocketMessageType.Binary, result.EndOfMessage, stopCts.Token);
                }

                ArrayPool<byte>.Shared.Return(backingArray);
            }

        }

        public Task Stop()
        {
            stopCts.Cancel();
            return Task.CompletedTask;
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var webSocketOptions = new WebSocketOptions() 
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };

            app.UseWebSockets(webSocketOptions);
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/echo")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, webSocket).ConfigureAwait(false);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
        }

        public void Dispose()
        {
            if(!_isDisposed)
            {
                _isDisposed = true;
                stopCts.Dispose();
            }
        }
    }
}