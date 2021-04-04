using Boyd.DataBuses.Models;
using System;
using System.IO;
using System.IO.Ports;
using Boyd.DataBuses.Interfaces.Hardware;

namespace Boyd.DataBuses.Impl.Internal
{
    class BoydSerialPort : ISerialPort
    {
        private SerialPort _serialPort;
        public BoydSerialPort(DataBusOptions pOptions) 
        {


            _serialPort = new SerialPort(
                pOptions.SupplementalSettings["port"], 
                int.Parse(pOptions.SupplementalSettings["baudRate"]), 
                Enum.Parse<Parity>(pOptions.SupplementalSettings["parity"]), 
                int.Parse(pOptions.SupplementalSettings["dataBits"]), 
                Enum.Parse<StopBits>(pOptions.SupplementalSettings["stopBits"]));
        }

        public Stream BaseStream => _serialPort.BaseStream;

        public bool IsOpen => _serialPort.IsOpen;

        public long BytesToRead => _serialPort.BytesToRead;

        public void Close()
        {
            _serialPort.Close();
        }

        public void Open()
        {
            _serialPort.Open();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _serialPort.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
