using Boyd.DataBuses.Impl.Duplexes;
using Boyd.DataBuses.Interfaces;
using Boyd.DataBuses.Models;
using Microsoft.Extensions.Logging;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public static class DuplexFactory<T1, T2>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="pInProcessor"></param>
        /// <param name="pOutProcessor"></param>
        /// <returns></returns>
        public static IDataDuplex<T1, T2> Build(
            DataBusOptions options, 
            ILoggerFactory loggerFactory = null
        )
        {
            switch (options.DatabusType)
            {
                case DataBusType.Udp:
                    return new UdpDataBus<T1, T2>(
                        options,
                        SerializerFactory<T1>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings),
                        DeserializerFactory<T2>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings),
                        loggerFactory);
                case DataBusType.TcpClient:
                    return new TcpClientDataBus<T1, T2>(
                        options,
                        SerializerFactory<T1>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings),
                        DeserializerFactory<T2>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings),
                        loggerFactory);
                case DataBusType.WebSocketClient:
                    return new WebSocketDataBus<T1, T2>(
                        loggerFactory, 
                        options, 
                        SerializerFactory<T1>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings),
                        DeserializerFactory<T2>.Build(
                            options.DataExchangeFormat, 
                            options.SupplementalSettings));
                case DataBusType.SignalR:
                    return new SignalRDataBus<T1, T2>(
                        options,
                        loggerFactory);
                case DataBusType.Redis:
                case DataBusType.Serial:
                default:
                    return null;
            }
        }
        
    }
}