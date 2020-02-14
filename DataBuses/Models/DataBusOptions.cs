using System.Collections.Generic;

namespace Boyd.DataBuses.Models
{
    /// <summary>
    /// 
    /// </summary>
    public enum SerDerType
    {
        /// <summary>
        /// 
        /// </summary>
        MessagePack,
        /// <summary>
        /// 
        /// </summary>
        Json
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DataBusType
    {
        /// <summary>
        /// 
        /// </summary>
        Udp,
        /// <summary>
        /// 
        /// </summary>
        TcpClient,
        /// <summary>
        /// 
        /// </summary>
        WebSocketClient,
        /// <summary>
        /// 
        /// </summary>
        Serial,
        /// <summary>
        /// 
        /// </summary>
        SignalR,
        /// <summary>
        /// 
        /// </summary>
        Redis

    }
    
    /// <summary>
    /// 
    /// </summary>
    public class DataBusOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public SerDerType DataExchangeFormat;
        /// <summary>
        /// 
        /// </summary>
        public DataBusType DatabusType;
        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, string> SupplementalSettings;
    }
}