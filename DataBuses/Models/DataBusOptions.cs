using System.Collections.Generic;

namespace Boyd.DataBuses.Models
{
    /// <summary>
    /// Specify what serialization and deserialization should be used in the bus
    /// </summary>
    public enum SerDerType
    {
        /// <summary>
        ///  MessagePack  https://msgpack.org/
        /// </summary>
        MessagePack,
        /// <summary>
        /// JSON https://www.json.org/json-en.html
        /// </summary>
        Json
    }

    /// <summary>
    /// The type of data bus to create
    /// </summary>
    public enum DataBusType
    {
        /// <summary>
        /// A UDP data bus where messages are sent or received as UDP data grams,
        /// Assumes messages can be contained in a data gram and that NAT traversal is not needed
        /// </summary>
        Udp,
        /// <summary>
        /// A TcpClient data bus where the data bus connects to a listening socket server. Deserializatoin is done in a
        /// streaming manner and the deserializer used most support deserializing from a stream.
        /// </summary>
        TcpClient,
        /// <summary>
        /// Connects to a web socket server and assumes binary sub protocol where messages are complete messages for serialization
        /// and deserialization
        /// </summary>
        WebSocketClient,
        /// <summary>
        /// Opens a serial port on the system as the data bus.
        /// </summary>
        Serial,
        /// <summary>
        /// Connects to a SignalR server and handles the specified on handler for receipt of messages
        /// and invokes the specified target method for sending messages
        /// </summary>
        SignalR,
        /// <summary>
        /// Not yet supported but uses Redis Publish Subscribe functionality to use a topic as a data bus
        /// </summary>
        Redis

    }
    
    /// <summary>
    /// The options used to configure and create data buses.
    /// </summary>
    public class DataBusOptions
    {
        /// <summary>
        /// The data exchange format this data bus should use, this is used for the serializer and deserializer
        /// creation step
        /// </summary>
        public SerDerType DataExchangeFormat;
        /// <summary>
        /// The data bus type to use, this is used in the factory to determine the underlying type/implementation
        /// that should be instantiated
        /// </summary>
        public DataBusType DatabusType;

        /// <summary>
        /// Set the maximum number of messages that should be allowed to be buffered by the
        /// data bus. This is used to set a bound on buffered messages and prevent unbounded memory growth for
        /// buffered messages. Eventually if this is full it will result in back pressure an messages will be left in the underlying transports
        /// Currently if the use of the databus doesn't pop items out of the buffer fast enough and a bound is set for the max bufferred messages,
        /// messages will be lost but if this occurs it will be logged.
        /// </summary>
        public int? MaxBufferedMessages;
        /// <summary>
        /// Supplemental setting dictionary that is used for custom settings that are specific to a certain data bus type and not common/shared
        /// across data busses.
        /// </summary>
        public IDictionary<string, string> SupplementalSettings;
    }
}