using System;
using System.Collections.Generic;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// Provides a mechanism to forward a single data ingress to multiple receiving data ingresses 
    /// </summary>
    public interface IDataMultiplexer<TData>
    {
        /// <summary>
        /// Take a data source emitter IDataEgress, and map it's data to a collection
        /// of IDataIngresses that can accept the same incoming data type
        /// </summary>
        /// <param name="pObjDataSource">data source/emitter</param>
        /// <param name="pObjDestinations">collection of receivers of data</param>
        /// <returns>IDisposable object that when disposed stops the multiplexing</returns>
        IDisposable Multiplex(IDataEgress<TData> pObjDataSource, IList<IDataIngress<TData>> pObjDestinations);
    }
}