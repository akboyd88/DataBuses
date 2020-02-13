using System;
using System.Threading;
using System.Threading.Tasks;

namespace Boyd.DataBuses.Interfaces
{
    /// <summary>
    /// A delegate for the data ingested event 
    /// </summary>
    /// <param name="pObjData">The data that was just ingested</param>
    /// <typeparam name="TData">Type of data</typeparam>
    public delegate void DataIngestedEvt<TData>(TData pObjData);

    /// <summary>
    /// Delegate for data commit to underlying bus event
    /// </summary>
    /// <param name="pObjCommittedData">The data sent to the underlying bus, this can be in any form as data can be
    /// transformed by processors</param>
    public delegate void DataIngestCommitted(dynamic pObjCommittedData);

    /// <summary>
    /// Data in interface
    /// </summary>
    /// <typeparam name="TData">The type of the data that will be ingested by the implementing object</typeparam>
    public interface IDataIngress<TData> : IDisposable
    {
        /// <summary>
        /// Ingest the data and forward it to the underlying transport
        /// </summary>
        /// <param name="pObjDataToIngest">Data object to ingest into the data bus</param>
        /// <param name="pCancelToken">Cancellation token to stop the write/ingest</param>
        /// <returns>A Task that can be awaited, if it fails to ingest the data it throws an exception, if the task
        /// completes without exception the data has been handed off to the next layer.</returns>
        Task PutData(TData pObjDataToIngest, CancellationToken pCancelToken);

        /// <summary>
        /// Fired when Ingest is called
        /// </summary>
        event DataIngestedEvt<TData> DataIngestedEvt;

        /// <summary>
        /// Fired when data has been ingested and fully committed to the underlying data buss.
        /// </summary>
        event DataIngestCommitted DataIngestCommittedEvt;

    }
}