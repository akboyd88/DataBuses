using System;
using Microsoft.Extensions.Logging;
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
        /// <param name="pLogger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IDisposable CoupleEgressToIngress(IDataEgress<TData> pObjEgress, IDataIngress<TData> pObjIngress, ILogger pLogger, CancellationToken cancellationToken);

    }
}