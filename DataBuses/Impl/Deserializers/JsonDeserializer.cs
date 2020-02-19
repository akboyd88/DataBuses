using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Deserializers
{
    /// <summary>
    /// A JSON deserializer that uses the System.Text.Json library to deserialize JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonDeserializer<T> : IDeserializer<T>
    {
        /// <summary>
        /// Deserializes a ReadOnlyMemory object of bytes to a generic type
        /// </summary>
        /// <param name="rawData">Complete json object in binary form</param>
        /// <returns>Deserialized generic result</returns>
        public T Deserialize(ReadOnlyMemory<byte> rawData)
        {
            return JsonSerializer.Deserialize<T>(rawData.Span);
        }
        
        /// <summary>
        /// Deserialize a byte array into the specified type
        /// </summary>
        /// <param name="rawData">raw byte array containing a complete json object in binary form</param>
        /// <returns>Deserialized result object from byte array</returns>
        public T Deserialize(byte[] rawData)
        {
            return JsonSerializer.Deserialize<T>(rawData);
        }

        /// <summary>
        /// Pops the next object matching the specified generic type out of the specified stream,
        /// if the data isn't already present in the stream this will read the stream further to
        /// extract the object
        /// </summary>
        /// <param name="stream">Stream object to extract the object from </param>
        /// <param name="cancelToken">cancellation token to cancel the task responsible for reading and deserializing
        /// the object from the stream</param>
        /// <returns>Task that can be awaited for the resultant deserialized object</returns>
        public async Task<T> Deserialize(Stream stream, CancellationToken cancelToken)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, null, cancelToken);
        }

        /// <summary>
        /// Takes the sequence of bytes and converts to the specified generic strongly typed object
        /// </summary>
        /// <param name="bytes">sequence of bytes representing complete json object string</param>
        /// <returns>Deserialized object from the raw json</returns>
        public T Deserialize(ReadOnlySequence<byte> bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes.ToArray());
        }

        /// <summary>
        /// Clean up any resources that stick around past serialize and deserialize calls
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            //No resources are held onto past calls currently
        }
    }
}