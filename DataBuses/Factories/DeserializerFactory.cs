using System;
using System.Collections.Generic;
using Boyd.DataBuses.Impl.Deserializers;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class DeserializerFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IDeserializer<T> Build(SerDerType type, IDictionary<string, string> options)
        {
            switch (type)
            {
                case SerDerType.MessagePack:
                    return BuildMessagePackDeSerializer(options);
                case SerDerType.Json:
                    return BuildJsonDeSerializer(options);
                default:
                    throw new NotImplementedException("SerDerType not implemented");
            }
        }

        private static IDeserializer<T> BuildMessagePackDeSerializer(IDictionary<string, string> options)
        {
            return new MessagePackDeserializer<T>();
        }
        
        private static IDeserializer<T> BuildJsonDeSerializer(IDictionary<string, string> options)
        {
            return new JsonDeserializer<T>();
        }
    }
}