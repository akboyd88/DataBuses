using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using MessagePack;
using MessagePack.Resolvers;

namespace Boyd.DataBuses.Impl.Deserializers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessagePackDeserializer<T> : IDeserializer<T>
    {
        private ConcurrentDictionary<Stream, MessagePackStreamReader> _readerDictionary;
        
        public MessagePackDeserializer()
        {
            _readerDictionary = new ConcurrentDictionary<Stream, MessagePackStreamReader>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T Deserialize(ReadOnlyMemory<byte> rawData)
        {
            return MessagePackSerializer.Deserialize<T>(rawData, MessagePackSerializerOptions.Standard);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public T Deserialize(byte[] rawData)
        {
            return MessagePackSerializer.Deserialize<T>(rawData, MessagePackSerializerOptions.Standard);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<T> Deserialize(Stream stream, CancellationToken token)
        {
            var reader = GetReader(stream);
            var streamReadResult = await reader.ReadAsync(token);
            if (streamReadResult != null)
            {
                return MessagePackSerializer.Deserialize<T>((ReadOnlySequence<byte>) streamReadResult,
                    MessagePackSerializerOptions.Standard);
            }

            throw new Exception("Failed to deserialize message pack object from stream");
        }

        public IAsyncEnumerable<ReadOnlySequence<byte>> GetAsyncEnumerable(Stream stream, CancellationToken cancelToken)
        {
            return GetReader(stream).ReadArrayAsync(cancelToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int RemainingBytes(Stream stream)
        {
            if (_readerDictionary.TryGetValue(stream, out var reader))
            {
                return (int)reader.RemainingBytes.Length;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool HasMore(Stream stream)
        {
            if (_readerDictionary.TryGetValue(stream, out var reader))
            {
                return !reader.RemainingBytes.IsEmpty;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public T Deserialize(ReadOnlySequence<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, MessagePackSerializerOptions.Standard);

        }

        private MessagePackStreamReader GetReader(Stream str)
        {
            MessagePackStreamReader reader;
            if (!_readerDictionary.TryGetValue(str, out  reader))
            {
                reader = new MessagePackStreamReader(str, true);
                _readerDictionary.AddOrUpdate(str, reader, (stream, streamReader) =>
                {
                    reader.Dispose();
                    reader = streamReader;
                    return reader;
                });
            }
            return reader;
        }

        public void Dispose()
        {
            foreach(var kvp in _readerDictionary)
            {
                kvp.Value.Dispose();
            }
            _readerDictionary.Clear();
        }
    }
}
