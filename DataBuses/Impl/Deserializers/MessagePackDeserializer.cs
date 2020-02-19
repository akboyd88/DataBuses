using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;
using MessagePack;

namespace Boyd.DataBuses.Impl.Deserializers
{
    /// <summary>
    /// Exception when deserializing a message pack message
    /// </summary>
    public class MPackDeserializeException : Exception
    {
        /// <summary>
        ///
        /// </summary>
        public MPackDeserializeException(string message) : base(message)
        {

        }
    }
    /// <summary>
    /// A message pack deserializer that uses the MessagePack-CSharp library to deserialize raw data
    /// to C# generic types
    /// </summary>
    /// <typeparam name="T">Generic type to message pack messages to</typeparam>
    public class MessagePackDeserializer<T> : IDeserializer<T>
    {
        /// <summary>
        /// Collection for tracking readers associated with streams, this is needed because the message pack
        /// deserializer/stream reader is greedy and reads all data available from stream
        /// </summary>
        private readonly ConcurrentDictionary<Stream, MessagePackStreamReader> _readerDictionary;
        
        /// <summary>
        /// Constructor for MessagePack deserializer
        /// </summary>
        public MessagePackDeserializer()
        {
            _readerDictionary = new ConcurrentDictionary<Stream, MessagePackStreamReader>();
        }
        
        /// <summary>
        /// Deserialize a complete message pack object in read only memory byte form
        /// </summary>
        /// <param name="rawData">complete message pack object</param>
        /// <returns>deserialize generic result</returns>
        public T Deserialize(ReadOnlyMemory<byte> rawData)
        {
            return MessagePackSerializer.Deserialize<T>(rawData, MessagePackSerializerOptions.Standard);
        }

        /// <summary>
        /// Deserialize a complete message pack object in byte array form to a generic type
        /// </summary>
        /// <param name="rawData">Complete message pack object</param>
        /// <returns>Deserialized generic type result</returns>
        public T Deserialize(byte[] rawData)
        {
            return MessagePackSerializer.Deserialize<T>(rawData, MessagePackSerializerOptions.Standard);
        }

        /// <summary>
        /// Deserialize the next message pack object from the stream using a MessagePackStreamReader to pop out the object
        /// </summary>
        /// <param name="stream">stream where MessagePack objects will/are written to that will be consumed by this deserializer</param>
        /// <param name="token">cancellation token to stop the deserialization operation</param>
        /// <returns>A task that can be awaited with the resultant deserialized object</returns>
        public async Task<T> Deserialize(Stream stream, CancellationToken token)
        {
            var reader = GetReader(stream);
            var streamReadResult = await reader.ReadAsync(token);
            if (streamReadResult != null)
            {
                return MessagePackSerializer.Deserialize<T>((ReadOnlySequence<byte>) streamReadResult,
                    MessagePackSerializerOptions.Standard);
            }
            

            throw new MPackDeserializeException("Failed to deserialize message pack object from stream");
        }

        /// <summary>
        /// Deserialize a sequence of bytes from message pack to a generic type
        /// </summary>
        /// <param name="bytes">byte sequence containing complete and well formed message pack object</param>
        /// <returns>Deserialized generic result</returns>
        public T Deserialize(ReadOnlySequence<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, MessagePackSerializerOptions.Standard);

        }
        
        /// <summary>
        /// Fetches a reader associated with the provided stream, if the stream doesn't have a
        /// reader already a new reader is created and tracked in the collection
        /// </summary>
        /// <param name="str">Stream to fetch the associated reader for</param>
        /// <returns>message pack stream reader linked to the specified stream</returns>
        private MessagePackStreamReader GetReader(Stream str)
        {
            if (!_readerDictionary.TryGetValue(str, out var reader))
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


        /// <summary>
        /// Used to clean up any resources allocated that persist across deserialize calls.
        /// </summary>
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
