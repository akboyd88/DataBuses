using System;
using System.Threading.Tasks;
using Boyd.DataBuses.Impl.Egresses;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class EgressFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="pOutProcessor"></param>
        /// <returns></returns>
        public static IDataEgress<T> Build(
            DataBusOptions options, 
            ILoggerFactory loggerFactory = null,
            Func<dynamic, Task<T>> pOutProcessor = null
        )
        {
            switch (options.DatabusType)
            {
                case DataBusType.Udp:
                    return new UdpEgress<T>(
                        options,
                        DeserializerFactory<T>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings), 
                        loggerFactory);
                case DataBusType.TcpClient:
                case DataBusType.Serial:
                case DataBusType.SignalR:
                case DataBusType.Redis:
                default:
                    return null;
            }
        }
    }
}