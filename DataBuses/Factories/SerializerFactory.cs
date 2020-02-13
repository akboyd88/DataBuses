using System;
using System.Collections.Generic;
using Boyd.DataBuses.Impl.Serializers;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class SerializerFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ISerializer<T> Build(SerDerType type, IDictionary<string, string> options)
        {
            switch (type)
            {
                case SerDerType.MessagePack:
                    return BuildMessagePackSerializer(options);
                case SerDerType.Json:
                    return BuildJsonSerializer(options);
                default:
                    throw new NotImplementedException("SerDerType not implemented");
            }
        }

        private static ISerializer<T> BuildMessagePackSerializer(IDictionary<string, string> options)
        {
            return new MessagePackSerializer<T>();

        }
        
        private static ISerializer<T> BuildJsonSerializer(IDictionary<string, string> options)
        {
            return new JsonSerializer<T>();
        }
        
    }
}