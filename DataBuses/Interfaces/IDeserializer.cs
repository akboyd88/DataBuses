using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// Generic Deserializer 
    /// </summary>
    public interface IDeserializer<T> : IDisposable
    {
        /// <summary>
        /// Deserialize an object in ReadOnlyMemoryform 
        /// </summary>
        /// <param name="rawData">raw data in ReadOnlyMemory form</param>
        /// <returns>Deserialized object of specified type</returns>
        T Deserialize(ReadOnlyMemory<byte> rawData);
        
        /// <summary>
        /// Deserialize an object in ReadOnlyMemory form 
        /// </summary>
        /// <param name="rawData">raw data in ReadOnlyMemory form</param>
        /// <returns>Deserialized object of specified type</returns>
        T Deserialize(byte[] rawData);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancelToken">cancel token for cancelling the streaming read for deserialization</param>
        /// <returns></returns>
        Task<T> Deserialize(Stream stream, CancellationToken cancelToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<ReadOnlySequence<byte>> GetAsyncEnumerable(Stream stream, CancellationToken cancelToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        int RemainingBytes(Stream stream);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        bool HasMore(Stream stream);

        T Deserialize(ReadOnlySequence<byte> bytes);
    }
}