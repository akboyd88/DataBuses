using System;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public sealed class IgnoreWithoutSerialPorts : FactAttribute
    {
        public IgnoreWithoutSerialPorts() {
            if(!HasTestSerialPorts()) {
                Skip = "Ignore when not linux and test ports not configured";
            }
        }
    
        private static bool HasTestSerialPorts()
            => Environment.GetEnvironmentVariable("HAS_TEST_SERIAL_PORTS") != null;
    }
}