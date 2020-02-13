using System;
using Boyd.DataBuses.Interfaces;

namespace Boyd.DataBuses.Impl.Coupler
{
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
            throw new NotImplementedException();
        }
    }
}