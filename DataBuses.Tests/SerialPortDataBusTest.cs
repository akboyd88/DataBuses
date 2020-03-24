
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class SerialPortDataBus
    {
        [IgnoreWithoutSerialPorts]
        public async Task DuplexE2ENoTransformTest()
        {
            var serialPort1 = Environment.GetEnvironmentVariable("TEST_SERIAL_PORT_1") != null ? 
                Environment.GetEnvironmentVariable("TEST_SERIAL_PORT_1") : "/dev/ttyUSB0";

            var serialPort2 = Environment.GetEnvironmentVariable("TEST_SERIAL_PORT_2") != null ? 
                Environment.GetEnvironmentVariable("TEST_SERIAL_PORT_2") : "/dev/ttyUSB1";

            var dOptions1 = new DataBusOptions();
            dOptions1.DataExchangeFormat = SerDerType.MessagePack;
            dOptions1.DatabusType = DataBusType.Serial;
            dOptions1.SupplementalSettings = new Dictionary<string, string>();
            dOptions1.SupplementalSettings["port"] = serialPort1;
            dOptions1.SupplementalSettings["baudRate"] = "9600";
            dOptions1.SupplementalSettings["parity"] = Parity.None.ToString();
            dOptions1.SupplementalSettings["stopBits"] = StopBits.Two.ToString();
            dOptions1.SupplementalSettings["dataBits"] = "8";

            var duplexDatabus1 = DuplexFactory<TestMPackMessage,TestMPackMessage>.Build(dOptions1);
            
            var dOptions2 = new DataBusOptions();
            dOptions2.DataExchangeFormat = SerDerType.MessagePack;
            dOptions2.DatabusType = DataBusType.Serial;
            dOptions2.SupplementalSettings = new Dictionary<string, string>();
            dOptions2.SupplementalSettings["port"] =serialPort2;
            dOptions2.SupplementalSettings["baudRate"] = "9600";
            dOptions2.SupplementalSettings["parity"] = Parity.None.ToString();
            dOptions2.SupplementalSettings["stopBits"] = StopBits.Two.ToString();
            dOptions2.SupplementalSettings["dataBits"] = "8";

            var duplexDatabus2 = DuplexFactory<TestMPackMessage,TestMPackMessage>.Build(dOptions2);
            duplexDatabus2.StartReading();
            
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(1000));

            var sourceMessage = new TestMPackMessage {test1 = 5, test2 = "test", test3 = 5.0};

            var sourceMessage2 = new TestMPackMessage {test1 = 10, test2 = "test2", test3 = 10.0};
            duplexDatabus1.StartReading();
            
            await duplexDatabus1.PutData(sourceMessage, cts.Token);
            await duplexDatabus1.PutData(sourceMessage2, cts.Token);


            var result = duplexDatabus2.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000), false);
            Assert.True(result);
            var recvMessage = await duplexDatabus2.TakeData(TimeSpan.FromMilliseconds(1000), cts.Token);
            Assert.Equal(sourceMessage.test1, recvMessage.test1);
            Assert.Equal(sourceMessage.test2, recvMessage.test2);
            Assert.Equal(sourceMessage.test3, recvMessage.test3);
            
            var result2 = duplexDatabus2.EgressDataAvailableWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000), false);
            Assert.True(result2);
            var recvMessage2 = await duplexDatabus2.TakeData(TimeSpan.FromMilliseconds(1000), cts.Token);
            Assert.Equal(sourceMessage2.test1, recvMessage2.test1);
            Assert.Equal(sourceMessage2.test2, recvMessage2.test2);
            Assert.Equal(sourceMessage2.test3, recvMessage2.test3);
            
            duplexDatabus1.Dispose();
            duplexDatabus2.Dispose();
        }
    }
}