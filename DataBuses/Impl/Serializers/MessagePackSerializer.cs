using System;
using Boyd.DataBuses.Interfaces;
using MessagePack;

namespace Boyd.DataBuses.Impl.Serializers
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MessagePackSerializer<T> : ISerializer<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Obj"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ReadOnlyMemory<byte> Serialize(T Obj)
        {
           return new ReadOnlyMemory<byte>(MessagePackSerializer.Serialize(Obj));
        }
    }
}