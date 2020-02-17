using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Impl.Serializers;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Deserializers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonDeserializer<T> : IDeserializer<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public T Deserialize(ReadOnlyMemory<byte> rawData)
        {
            return JsonSerializer.Deserialize<T>(rawData.Span);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public T Deserialize(byte[] rawData)
        {
            return JsonSerializer.Deserialize<T>(rawData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<T> Deserialize(Stream stream, CancellationToken cancelToken)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, null, cancelToken);
        }

        public IAsyncEnumerable<ReadOnlySequence<byte>> GetAsyncEnumerable(Stream stream, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public int RemainingBytes(Stream stream)
        {
            throw new NotImplementedException();
        }

        public bool HasMore(Stream stream)
        {
            throw new NotImplementedException();
        }

        public T Deserialize(ReadOnlySequence<byte> bytes)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}