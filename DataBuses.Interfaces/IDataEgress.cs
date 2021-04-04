using System;
using System.Threading;
using System.Threading.Tasks;

namespace Boyd.DataBuses.Interfaces
{

    /// <summary>
    /// Delegate for signaling that data is available
    /// </summary>
    public delegate void EgressDataAvailableEvt();

    /// <summary>
    /// Data out interface
    /// </summary>
    public interface IDataEgress<TData> : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        void StartReading();

        /// <summary>
        /// Take the next data from the underlying data bus, provide a timeout and cancellation token
        /// </summary>
        /// <param name="pObjTimeout">How long to wait to take the data from the underlying bus</param>
        /// <param name="pCancelToken">CancellationToken</param>
        /// <returns></returns>
        Task<TData> TakeData(TimeSpan pObjTimeout, CancellationToken pCancelToken);

        /// <summary>
        /// Fired when data is available to be taken out of the egress
        /// </summary>
        event EgressDataAvailableEvt OnEgressDataAvailableEvt;

        /// <summary>
        /// Gets a wait handle that can be awaited for the next time data is available to be taken from the
        /// underlying data bus
        /// </summary>
        EventWaitHandle EgressDataAvailableWaitHandle { get; }
    }
}