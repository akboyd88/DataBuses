using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class TcpDataBusTest
    {
        [Fact]
        public async Task EchoServerVerification()
        {
            var echoServer = new TcpEchoServer(25003);
            var tcpClient = new TcpClient("localhost", 25003);
            Assert.True(tcpClient.Connected);

            tcpClient.GetStream();

            var bytes = Encoding.UTF8.GetBytes("test");
            var copy = new byte[bytes.Length];
            
            tcpClient.GetStream().Write(bytes);
            tcpClient.GetStream().Flush();
            tcpClient.GetStream().Read(copy);
            Assert.Equal(bytes, copy);

        }
        
        [Fact(Skip = "Todo")]
        public async Task DuplexE2ENoTransformTest()
        {
            var dOptions = new DataBusOptions();
            dOptions.DataExchangeFormat = SerDerType.MessagePack;
            dOptions.DatabusType = DataBusType.TcpClient;
            dOptions.SupplementalSettings = new Dictionary<string, string>();
            dOptions.SupplementalSettings["port"] = "25002";
            dOptions.SupplementalSettings["hostname"] = "localhost";
            
            var echoServer = new TcpEchoServer(25002);

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
            echoServer.Close();

        }
    }
}