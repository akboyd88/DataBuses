using System;
using System.IO;

namespace Boyd.DataBuses.Interfaces.Hardware
{

    /// <summary>
    /// 
    /// </summary>
    public interface ISerialPort : IDisposable
    {

        /// <summary>
        /// 
        /// </summary>
        void Open();

        /// <summary>
        /// 
        /// </summary>
        void Close();

        /// <summary>
        /// 
        /// </summary>
        Stream BaseStream { get;  }
  
        /// <summary>
        /// 
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 
        /// </summary>
        long BytesToRead { get; }
    }
}
