using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// Generic Deserializer 
    /// </summary>
    public interface IDeserializer<T>
    {
        /// <summary>
        /// Deserialize an object in ReadOnlyMemory<byte> form 
        /// </summary>
        /// <param name="rawData">raw data in ReadOnlyMemory<byte> form</param>
        /// <returns>Deserialized object of specified type</returns>
        T Deserialize(ReadOnlyMemory<byte> rawData);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Task<T> Deserialize(Stream stream, CancellationToken cancelToken);
    }
}