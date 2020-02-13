using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using MessagePack;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    [MessagePackObject]
    public class TestMessagePackObject
    {
        [Key(0)]
        public int test1 { get; set; }
        [Key(1)]
        public string test2 { get; set;  }
        [Key(2)]
        public double test3 { get; set; }
    }
    public class SerDerTests
    {

        [Fact]
        public async Task StreamMessagePackTest()
        {
            var serializer =
                SerializerFactory<TestMessagePackObject>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMessagePackObject>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMessagePackObject();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var memoryStream = new MemoryStream();
            var exchangeFormat = serializer.Serialize(testSourceObj);
            memoryStream.Write(exchangeFormat.Span);
            memoryStream.Position = 0;

            var deserObj = await deserializer.Deserialize(memoryStream, CancellationToken.None);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
        }
        
        [Fact]
        public void SimpleMessagePackTest()
        {
            var serializer =
                SerializerFactory<TestMessagePackObject>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMessagePackObject>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMessagePackObject();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var exchangeFormat = serializer.Serialize(testSourceObj);
            var deserObj = deserializer.Deserialize(exchangeFormat);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
        }

        [Fact]
        public async Task StreamJsonTest()
        {
            var serializer =
                SerializerFactory<TestMessagePackObject>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMessagePackObject>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMessagePackObject();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var memoryStream = new MemoryStream();
            var exchangeFormat = serializer.Serialize(testSourceObj);
            memoryStream.Write(exchangeFormat.Span);
            memoryStream.Position = 0;
            
            var deserObj = await deserializer.Deserialize(memoryStream, CancellationToken.None);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
        }
        
        [Fact]
        public void SimpleJsonTest()
        {
            var serializer =
                SerializerFactory<TestMessagePackObject>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMessagePackObject>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMessagePackObject();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var exchangeFormat = serializer.Serialize(testSourceObj);
            var deserObj = deserializer.Deserialize(exchangeFormat);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
        }
    }
}