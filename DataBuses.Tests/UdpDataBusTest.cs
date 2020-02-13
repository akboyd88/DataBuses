using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using MessagePack;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    [MessagePackObject()]
    public class TestMPackMessage
    {
        [Key(0)]
        public int test1 { get; set; }
        [Key(1)]
        public string test2 { get; set; }
        [Key(2)]
        public double test3 { get; set; }
    }
    public class UdpDataBusTest
    {
        [Fact]
        public async Task DuplexE2ENoTransformTest()
        {
            var dOptions = new DataBusOptions();
            dOptions.DataExchangeFormat = SerDerType.MessagePack;
            dOptions.DatabusType = DataBusType.Udp;
            dOptions.SupplementalSettings = new Dictionary<string, string>();
            dOptions.SupplementalSettings["receivePort"] = "25000";
            dOptions.SupplementalSettings["remotePort"] = "25000";
            dOptions.SupplementalSettings["remoteHost"] = "localhost";

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
            var recvMessage = await duplexDatabus.TakeData(TimeSpan.FromMilliseconds(250), CancellationToken.None);
            Assert.Equal(sourceMessage.test1, recvMessage.test1);
            Assert.Equal(sourceMessage.test2, recvMessage.test2);
            Assert.Equal(sourceMessage.test3, recvMessage.test3);
            
            
            var result2 = duplexDatabus.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(250), false);
            Assert.True(result2);
            var recvMessage2 = await duplexDatabus.TakeData(TimeSpan.FromMilliseconds(250), CancellationToken.None);
            Assert.Equal(sourceMessage2.test1, recvMessage2.test1);
            Assert.Equal(sourceMessage2.test2, recvMessage2.test2);
            Assert.Equal(sourceMessage2.test3, recvMessage2.test3);
            duplexDatabus.Dispose();

        }

        [Fact]
        public async Task IngressAndEgressE2ETest()
        {
            var dOptions = new DataBusOptions();
            dOptions.DataExchangeFormat = SerDerType.MessagePack;
            dOptions.DatabusType = DataBusType.Udp;
            dOptions.SupplementalSettings = new Dictionary<string, string>();
            dOptions.SupplementalSettings["receivePort"] = "25001";
            dOptions.SupplementalSettings["remotePort"] = "25001";
            dOptions.SupplementalSettings["remoteHost"] = "localhost";
            
            var ingress = IngressFactory<TestMPackMessage>.Build(dOptions);
            var egress = EgressFactory<TestMPackMessage>.Build(dOptions);
            
            Assert.NotNull(ingress);
            Assert.NotNull(egress);
            
            egress.StartReading();
            
            var sourceMessage = new TestMPackMessage();
            sourceMessage.test1 = 5;
            sourceMessage.test2 = "test";
            sourceMessage.test3 = 5.0;
            
            var sourceMessage2 = new TestMPackMessage();
            sourceMessage2.test1 = 10;
            sourceMessage2.test2 = "test2";
            sourceMessage2.test3 = 10.0;

            await ingress.PutData(sourceMessage, CancellationToken.None);
            await ingress.PutData(sourceMessage2, CancellationToken.None);
            
            var result = egress.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(250), false);
            Assert.True(result);
            var recvMessage = await egress.TakeData(TimeSpan.FromMilliseconds(250), CancellationToken.None);
            Assert.Equal(sourceMessage.test1, recvMessage.test1);
            Assert.Equal(sourceMessage.test2, recvMessage.test2);
            Assert.Equal(sourceMessage.test3, recvMessage.test3);
            
            
            var result2 = egress.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(250), false);
            Assert.True(result2);
            var recvMessage2 = await egress.TakeData(TimeSpan.FromMilliseconds(250), CancellationToken.None);
            Assert.Equal(sourceMessage2.test1, recvMessage2.test1);
            Assert.Equal(sourceMessage2.test2, recvMessage2.test2);
            Assert.Equal(sourceMessage2.test3, recvMessage2.test3);
            

            ingress.Dispose();
            egress.Dispose();
        }
        
    }
}