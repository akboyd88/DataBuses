using System;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// Generic wrapper for a class that takes a type and converts it into a
    /// data exchange format for communication with other services and processes
    /// </summary>
    public interface ISerializer<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Obj"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> Serialize(T Obj);
    }
}