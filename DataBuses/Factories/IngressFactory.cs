using System;
using System.Threading.Tasks;
using Boyd.DataBuses.Impl.Ingresses;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class IngressFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="pInProcessor"></param>
        /// <returns></returns>
        public static IDataIngress<T> Build(
            DataBusOptions options, 
            ILoggerFactory loggerFactory = null,
            Func<T, Task<dynamic>> pInProcessor = null
        )
        {
            switch (options.DatabusType)
            {
                case DataBusType.Udp:
                case DataBusType.TcpClient:
                    return new UdpIngress<T>(
                        options,
                        SerializerFactory<dynamic>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings), 
                        loggerFactory);
                case DataBusType.Serial:
                case DataBusType.SignalR:
                case DataBusType.Redis:
                default:
                    return null;
            }
        }
    }
}