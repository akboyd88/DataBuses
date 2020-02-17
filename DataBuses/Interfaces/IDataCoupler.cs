using System;
using System.Threading;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// provides a mechanism to couple duplex data pipelines
    /// </summary>
    public interface IDataCoupler<TData>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pObjEgress"></param>
        /// <param name="pObjIngress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IDisposable CoupleEgressToIngress(IDataEgress<TData> pObjEgress, IDataIngress<TData> pObjIngress, CancellationToken cancellationToken);

    }
}