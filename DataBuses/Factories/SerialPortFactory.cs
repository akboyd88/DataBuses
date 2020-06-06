using Boyd.DataBuses.Impl.Internal;
using Boyd.DataBuses.Interfaces.Hardware;
using Boyd.DataBuses.Models;

namespace Boyd.DataBuses.Factories
{
    
    /// <summary>
    /// Serial port factory
    /// </summary>
    public interface ISerialPortFactory
    {

        /// <summary>
        /// Create a new serial port interface
        /// </summary>
        /// <param name="options">options to use while creating serial port</param>
        /// <returns>Serial port interface</returns>
        ISerialPort Create(DataBusOptions options);
    }
    
    /// <summary>
    /// Create wrapped serial port
    /// </summary>
    internal class SerialPortFactory : ISerialPortFactory
    {
        /// <summary>
        /// Create a serial port that implements ISerialPort
        /// </summary>
        /// <param name="pOptions">serial port options</param>
        /// <returns>ISerialPort instance</returns>
        public ISerialPort Create(DataBusOptions pOptions)
        {
            return new BoydSerialPort(pOptions);
        }
    }
}
