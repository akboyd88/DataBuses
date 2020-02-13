using System;
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
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<T> Deserialize(Stream stream, CancellationToken token)
        {
            return await MessagePackSerializer.DeserializeAsync<T>(stream, MessagePackSerializerOptions.Standard, token);
        }
    }
}