using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class SignalRDataBusTest
    {
        [Fact]
        public async Task VerifySignalREchoServer()
        {
            var echoServer = new SignalREchoServer("http://127.0.0.1:30002");
            
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(5000));
            
            HubConnection connection = new HubConnectionBuilder()
                .WithUrl(new Uri("http://127.0.0.1:30002/echo"))
                .Build();


            var taskCompletionSource = new TaskCompletionSource<byte[]>();
            var cancelCancelSource = new CancellationTokenSource();
            var timeoutCancelTask = Task.Delay(TimeSpan.FromMilliseconds(2000),cancelCancelSource.Token).ContinueWith(
                (a) =>
                {
                    taskCompletionSource.SetCanceled();
                },cancelCancelSource.Token);
            
            connection.On<byte[]>("echo", s =>
            {
                taskCompletionSource.SetResult(s);
            });
            
            await connection.StartAsync(cts.Token);

            
            await connection.InvokeAsync<string>("echo", Encoding.UTF8.GetBytes("echo"),cts.Token);
            var result = await taskCompletionSource.Task.ConfigureAwait(false);
            
            Assert.Equal(Encoding.UTF8.GetBytes("echo"), result);
            if (!cancelCancelSource.IsCancellationRequested)
            {
                cancelCancelSource.Cancel();
            }

            try
            {
                await timeoutCancelTask.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }

            await connection.StopAsync(cts.Token);

            await echoServer.Stop();
        }
        
         [Fact]
        public async Task DuplexE2ENoTransformTest()
        {
            var dOptions = new DataBusOptions();
            dOptions.DataExchangeFormat = SerDerType.Json;
            dOptions.DatabusType = DataBusType.SignalR;
            dOptions.SupplementalSettings = new Dictionary<string, string>();
            dOptions.SupplementalSettings["hubUrl"] = "http://127.0.0.1:30003/echo";
            dOptions.SupplementalSettings["hubInvokeTarget"] = "echoObject";
            dOptions.SupplementalSettings["hubInvokeRecipient"] = "echoObject";
            dOptions.SupplementalSettings["maxBufferedMessages"] = "10";
            
            var echoServer = new SignalREchoServer("http://127.0.0.1:30003");


            var duplexDatabus = DuplexFactory<TestMPackMessage,TestMPackMessage>.Build(dOptions);
            
            var sourceMessage = new TestMPackMessage();
            sourceMessage.test1 = 5;
            sourceMessage.test2 = "test";
            sourceMessage.test3 = 5.0;
            
            var sourceMessage2 = new TestMPackMessage();
            sourceMessage2.test1 = 10;
            sourceMessage2.test2 = "test2";
            sourceMessage2.test3 = 10.0;
            duplexDatabus.StartReading();
            
            await duplexDatabus.PutData(sourceMessage, CancellationToken.None);
            await duplexDatabus.PutData(sourceMessage2, CancellationToken.None);
            
            var result = duplexDatabus.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(250), false);
            Assert.True(result);
            var recvMessage = await duplexDatabus.TakeData(TimeSpan.FromMilliseconds(1000), CancellationToken.None);
            Assert.Equal(sourceMessage.test1, recvMessage.test1);
            Assert.Equal(sourceMessage.test2, recvMessage.test2);
            Assert.Equal(sourceMessage.test3, recvMessage.test3);
            
            
            var result2 = duplexDatabus.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(250), false);
            Assert.True(result2);
            var recvMessage2 = await duplexDatabus.TakeData(TimeSpan.FromMilliseconds(1000), CancellationToken.None);
            Assert.Equal(sourceMessage2.test1, recvMessage2.test1);
            Assert.Equal(sourceMessage2.test2, recvMessage2.test2);
            Assert.Equal(sourceMessage2.test3, recvMessage2.test3);
            duplexDatabus.Dispose();
            await echoServer.Stop();

        }
    }
}