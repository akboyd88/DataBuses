using System;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// provides a mechanism to couple duplex data pipelines
    /// </summary>
    public interface IDataCoupler<TData>
    {
        /// <summary>
        /// Map the output of an egress into an ingress
        /// </summary>
        /// <param name="pObjEgress">Data emitter/egress data source to post data into ingress</param>
        /// <param name="pObjIngress">Destination for the data source</param>
        /// <returns>Disposable object that can be used to break the coupling on dispose</returns>
        IDisposable CoupleEgressToIngress(IDataEgress<TData> pObjEgress, IDataIngress<TData> pObjIngress);

    }
}