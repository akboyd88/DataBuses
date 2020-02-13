using System;
using System.Text.Json;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Serializers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonSerializer<T> : ISerializer<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Obj"></param>
        /// <returns></returns>
        public ReadOnlyMemory<byte> Serialize(T Obj)
        {
            return new ReadOnlyMemory<byte>(JsonSerializer.SerializeToUtf8Bytes(Obj));
        }
    }
}