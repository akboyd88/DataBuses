using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Moq;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class WebSocketDataBusTests
    {
        [Fact]
        public async Task VerifyWsEchoServer()
        {
            var wssv = new WebSocketEchoServer("http://127.0.0.1:30001");
            
            var client = new ClientWebSocket();
            await client.ConnectAsync(new Uri("ws://127.0.0.1:30001/echo"), CancellationToken.None);

            await client.SendAsync(Encoding.UTF8.GetBytes("test"), WebSocketMessageType.Binary, true,
                CancellationToken.None);

            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            var result = await client.ReceiveAsync(buffer, CancellationToken.None);
            
            var memory = new Memory<byte>(buffer, 0, result.Count);
            var text = Encoding.UTF8.GetString(memory.Span);
            Assert.Equal("test", text);

            await wssv.Stop();
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
            
            var wssv = new WebSocketEchoServer("http://127.0.0.1:30000");
            
            var mockedSerialPortfactory = new Mock<ISerialPortFactory>();
            var duplexFactory = new DuplexFactory<TestMPackMessage, TestMPackMessage>(mockedSerialPortfactory.Object);
            var duplexDatabus = duplexFactory.Build(dOptions);
            
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
            await wssv.Stop();

        }
    }
}