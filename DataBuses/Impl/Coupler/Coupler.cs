using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Coupler
{
    internal class Coupling<T> : IDisposable
    {
        private Task _copyTask;
        private IDataEgress<T> _egress;
        private IDataIngress<T> _ingress;
        private EventWaitHandle _stopEvent;
        private volatile bool _done = false;
        
        private CancellationToken _cancel;
        private CancellationTokenSource _taskCancel;

        public Coupling(IDataEgress<T> pObjEgress, IDataIngress<T> pObjIngress, CancellationToken cancellationToken)
        {
            _egress = pObjEgress;
            _ingress = pObjIngress;
            _stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _cancel = cancellationToken;
            _taskCancel = new CancellationTokenSource();
            _copyTask = CreateCopyTask();
        }

        private Task CreateCopyTask()
        {
            return Task.Run(async () =>
            {
                while (!_done)
                {
                    try
                    {
                        var result = WaitHandle.WaitAny(new[] {_stopEvent, _egress.EgressDataAvailableWaitHandle});
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
                        Console.WriteLine(e);
                    }

                }
            });
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
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Coupler<T> : IDataCoupler<T>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pObjEgress"></param>
        /// <param name="pObjIngress"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public  IDisposable CoupleEgressToIngress(IDataEgress<T> pObjEgress, IDataIngress<T> pObjIngress, CancellationToken cancelToken)
        {
            return new Coupling<T>(pObjEgress, pObjIngress, cancelToken);
        }
    }
}