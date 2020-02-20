using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public sealed class IgnoreWithoutVSPFact : FactAttribute
    {
        public IgnoreWithoutVSPFact() {
            if(!HasVSPConfigured()) {
                Skip = "Ignore when not linux and VSP not configured";
            }
        }
    
        private static bool HasVSPConfigured()
            => Environment.GetEnvironmentVariable("HAS_VSP_CONFIGURED") != null;
    }
}