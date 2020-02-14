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

        public Coupling(IDataEgress<T> pObjEgress, IDataIngress<T> pObjIngress)
        {
            _egress = pObjEgress;
            _ingress = pObjIngress;
            _stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _copyTask = CreateCopyTask();
        }

        private Task CreateCopyTask()
        {
            return Task.Run(async () =>
            {
                while (!_stopEvent.WaitOne(0, false))
                {
                    try
                    {
                        var result = WaitHandle.WaitAny(new[] {_stopEvent, _egress.EgressDataAvailableWaitHandle});
                        if (result == 1)
                        {
                            await _ingress.PutData(await _egress.TakeData(TimeSpan.FromMilliseconds(250), CancellationToken.None),
                                CancellationToken.None);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _stopEvent.WaitOne(TimeSpan.FromMilliseconds(250));
                    }

                }
            });
        }
        
        public void Dispose()
        {
            _stopEvent.Set();
            _copyTask.Wait();
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
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public  IDisposable CoupleEgressToIngress(IDataEgress<T> pObjEgress, IDataIngress<T> pObjIngress)
        {
            return new Coupling<T>(pObjEgress, pObjIngress);
        }
    }
}