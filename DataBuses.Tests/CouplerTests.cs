using System;
using System.Threading;
using System.Threading.Tasks;
using Boyd.DataBuses.Impl.Coupler;
using Boyd.DataBuses.Interfaces;
using Moq;
using Xunit;

namespace Boyd.DataBuses.Tests
{
    public class CouplerTests
    {
        [Fact]
        public async Task CouplerForwardsMessages()
        {
            var coupler = new Coupler<string>();
            var mockIngress = new Mock<IDataIngress<string>>();
            var mockEgress = new Mock<IDataEgress<string>>();
            
            EventWaitHandle dataAvailable = new EventWaitHandle(false, EventResetMode.AutoReset);
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(250));

            mockEgress.Setup(s => s.EgressDataAvailableWaitHandle).Returns(dataAvailable);
            mockEgress.Setup(s => s.TakeData(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult("test"));
            mockIngress.Setup(s => s.PutData(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask).Verifiable();

            var coupledObj = coupler.CoupleEgressToIngress(mockEgress.Object, mockIngress.Object, null, cts.Token);
            dataAvailable.Set();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            mockIngress.Verify(s => s.PutData(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            coupledObj.Dispose();
        }
    }
}