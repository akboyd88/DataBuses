using Boyd.DataBuses.Impl.Internal;
using Boyd.DataBuses.Interfaces.Internal;
using Boyd.DataBuses.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boyd.DataBuses.Factories
{
    /// <summary>
    /// Create wrapped serial port
    /// </summary>
    internal static class SerialPortFactory
    {
        /// <summary>
        /// Create a serial port that implements ISerialPort
        /// </summary>
        /// <param name="pOptions">serial port options</param>
        /// <returns>ISerialPort instance</returns>
        internal static ISerialPort Create(DataBusOptions pOptions)
        {
            return new BoydSerialPort(pOptions);
        }
    }
}
