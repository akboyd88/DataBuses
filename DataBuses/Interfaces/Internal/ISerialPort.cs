using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Boyd.DataBuses.Interfaces.Internal
{
    internal interface ISerialPort : IDisposable
    {
        void Open();
        void Close();
        Stream BaseStream { get;  }
        bool IsOpen { get; }
        int BytesToRead { get; }
    }
}
