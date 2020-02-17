using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Factories;
using Boyd.DataBuses.Models;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    
    public class SerDerTests
    {

        [Fact]
        public async Task StreamMessagePackTest()
        {
            var serializer =
                SerializerFactory<TestMPackMessage>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMPackMessage>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMPackMessage();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;
            
            var testSourceObj2 = new TestMPackMessage();
            testSourceObj2.test1 = 5;
            testSourceObj2.test2 = "test";
            testSourceObj2.test3 = 5.0;

            var memoryStream = new MemoryStream();
            var exchangeFormat = serializer.Serialize(testSourceObj);
            var exchangeFormat2 = serializer.Serialize(testSourceObj2);
            memoryStream.Write(exchangeFormat.Span);
            memoryStream.Write(exchangeFormat2.Span);
            memoryStream.Position = 0;

            var deserObj = await deserializer.Deserialize(memoryStream, CancellationToken.None);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
            Assert.True(memoryStream.CanRead);
            var deserObj2 = await deserializer.Deserialize(memoryStream, CancellationToken.None);
            
            Assert.NotNull(deserObj2);
            Assert.Equal(testSourceObj2.test1, deserObj2.test1);
            Assert.Equal(testSourceObj2.test2, deserObj2.test2);
            Assert.Equal(testSourceObj2.test3, deserObj2.test3);
            
        }
        
        [Fact]
        public void SimpleMessagePackTest()
        {
            var serializer =
                SerializerFactory<TestMPackMessage>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMPackMessage>.Build(SerDerType.MessagePack,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMPackMessage();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var exchangeFormat = serializer.Serialize(testSourceObj);
            var deserObj = deserializer.Deserialize(exchangeFormat);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj.test1, deserObj.test1);
            Assert.Equal(testSourceObj.test2, deserObj.test2);
            Assert.Equal(testSourceObj.test3, deserObj.test3);
            
            
            var testSourceObj2 = new TestMPackMessage();
            testSourceObj.test1 = 5;
            testSourceObj.test2 = "test";
            testSourceObj.test3 = 5.0;

            var exchangeFormat2 = serializer.Serialize(testSourceObj2);
            var sequence = new ReadOnlySequence<byte>(exchangeFormat2);
            var deserObj2 = deserializer.Deserialize(sequence);
            
            Assert.NotNull(deserObj);
            Assert.Equal(testSourceObj2.test1, deserObj2.test1);
            Assert.Equal(testSourceObj2.test2, deserObj2.test2);
            Assert.Equal(testSourceObj2.test3, deserObj2.test3);
        }

        [Fact]
        public async Task StreamJsonTest()
        {
            var serializer =
                SerializerFactory<TestMPackMessage>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMPackMessage>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMPackMessage();
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
                SerializerFactory<TestMPackMessage>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(serializer);

            var deserializer =
                DeserializerFactory<TestMPackMessage>.Build(SerDerType.Json,
                    new Dictionary<string, string>());
            Assert.NotNull(deserializer);
            
            var testSourceObj = new TestMPackMessage();
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