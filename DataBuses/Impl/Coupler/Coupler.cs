using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Coupler
{
    internal class Coupling<T> : IDisposable
    {
        private readonly Task _copyTask;
        private readonly IDataEgress<T> _egress;
        private readonly IDataIngress<T> _ingress;
        private readonly EventWaitHandle _stopEvent;
        private volatile bool _done;
        
        private readonly CancellationToken _cancel;
        private readonly CancellationTokenSource _taskCancel;
        private readonly ILogger _logger;

        public Coupling(
            IDataEgress<T> pObjEgress, 
            IDataIngress<T> pObjIngress,
            ILogger pLogger,
            CancellationToken cancellationToken)
        {
            _logger = pLogger;
            _egress = pObjEgress;
            _ingress = pObjIngress;
            _stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _cancel = cancellationToken;
            _taskCancel = new CancellationTokenSource();
            _copyTask = CreateCopyTask();
        }

        private void Log(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }

        private Task CreateCopyTask()
        {
            return Task.Run(async () =>
            {
                while (!_done)
                {
                    try
                    {
                        var result = WaitHandle.WaitAny(new WaitHandle[] {_stopEvent, _egress.EgressDataAvailableWaitHandle});
                        if (result == 1)
                        {
                            var data = await _egress.TakeData(TimeSpan.FromMilliseconds(250), _cancel);
                            await _ingress.PutData(data, _cancel);
                        }

                        if (result == 0)
                        {
                            _done = true;
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Log(LogLevel.Error, "Error in coupler copy task: " + e.Message);
                    }

                }
            }, _cancel);
        }
        
        public void Dispose()
        {
            _done = true;
            _stopEvent.Set();
            if (!_copyTask.Wait(TimeSpan.FromMilliseconds(500)))
            {
                _taskCancel.Cancel();
            }
            _stopEvent.Dispose();
            _copyTask.Dispose();
        }
    }
    
    /// <summary>
    /// Couples an Egress to an Ingress so that data is forwarded from one data bus to the other, data busses
    /// must use the same message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Coupler<T> : IDataCoupler<T>
    {

        /// <summary>
        /// Creates an object that forwards messages from an egress to an ingress, returns an object that implements
        /// IDisposable which when disposed stops the coupling but doesn't stop the ingres and egress
        /// </summary>
        /// <param name="pObjEgress">Egress object</param>
        /// <param name="pObjIngress">Ingress object</param>
        /// <param name="pLogger">Logger</param>
        /// <param name="cancelToken">cancellation token</param>
        /// <returns></returns>
        public  IDisposable CoupleEgressToIngress(
            IDataEgress<T> pObjEgress, 
            IDataIngress<T> pObjIngress, 
            ILogger pLogger,
            CancellationToken cancelToken)
        {
            return new Coupling<T>(pObjEgress, pObjIngress, pLogger, cancelToken);
        }
    }
}