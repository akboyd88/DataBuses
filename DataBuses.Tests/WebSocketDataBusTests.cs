using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using WebSocketSharp;
using WebSocketSharp.Server;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class WebSocketDataBusTests
    {
        [Fact]
        public async Task VerifyWsEchoServer()
        {
            var wssv = new WebSocketServer ("ws://127.0.0.1:30001");
            wssv.AddWebSocketService<WebSocketEchoServer> ("/echo");
            wssv.Start ();

            TaskCompletionSource<string> result = new TaskCompletionSource<string>();
            
            
            using (var ws = new WebSocket ("ws://127.0.0.1:30001/echo"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    if(e.IsText && !e.IsPing)
                        result.SetResult(e.Data);
                    else if (e.IsBinary)
                        result.SetResult(Encoding.UTF8.GetString(e.RawData));
                };

                ws.Connect ();
                ws.OnError += (sender, err) => throw (err.Exception);
                ws.Send ("test");

                var mess = await result.Task;
                Assert.Equal("test", mess);
            }
            wssv.Stop();
            

        }

        [Fact]
        public async Task DuplexE2ENoTransformTest()
        {
            var dOptions = new DataBusOptions();
            dOptions.DataExchangeFormat = SerDerType.MessagePack;
            dOptions.DatabusType = DataBusType.WebSocketClient;
            dOptions.SupplementalSettings = new Dictionary<string, string>();
            dOptions.SupplementalSettings["url"] = "ws://127.0.0.1:30000/echo";
            dOptions.SupplementalSettings["subProtocol"] = "binary";
            dOptions.SupplementalSettings["maxBufferedMessages"] = "10";
            
            
            var wssv = new WebSocketServer ("ws://127.0.0.1:30000");
            wssv.AddWebSocketService<WebSocketEchoServer> ("/echo");
            wssv.Start ();

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
            wssv.Stop ();

        }
    }
}