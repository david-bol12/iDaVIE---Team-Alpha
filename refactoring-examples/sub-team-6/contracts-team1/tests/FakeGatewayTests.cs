// Behavioural tests for FakeGateway.
//
// These nail down the behaviour the adapter test projects rely on. Each test
// also doubles as a little worked example of how a downstream test would set up
// the gateway double.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace iDaVIE.Client.Gateway.Tests
{
    [TestFixture]
    [Category("Gateway")]
    public class FakeGatewayTests
    {
        [Test]
        public void SendAsync_BeforeConnect_Throws()
        {
            var gateway = new FakeGateway();
            // Same as JsonRpcPipeGateway: sending without connecting first is a
            // programmer mistake, not a transport failure.
            Assert.That(async () => await gateway.SendAsync<int>("anything", null),
                        Throws.TypeOf<InvalidOperationException>()
                              .With.Message.Contains("not connected"));
        }

        [Test]
        public async Task SendAsync_NoHandler_Throws_WithMethodNameInMessage()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            // The "you forgot to stub it" trap: the message has to name the method
            // so a failing test points you straight at the missing SetResponse call.
            Assert.That(async () => await gateway.SendAsync<int>("file.open", null),
                        Throws.TypeOf<InvalidOperationException>()
                              .With.Message.Contains("file.open"));
        }

        [Test]
        public async Task SetResponse_DispatchesValueAndRecordsCall()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            gateway.SetResponse("dataset.getAxes", new { nAxis = 3 });

            var result = await gateway.SendAsync<NAxisOnly>("dataset.getAxes", new { datasetId = "ds-1" });

            Assert.That(result.NAxis, Is.EqualTo(3));
            Assert.That(gateway.Sent, Has.Count.EqualTo(1));
            var sent = System.Linq.Enumerable.First(gateway.Sent);
            Assert.That(sent.Method, Is.EqualTo("dataset.getAxes"));
        }

        [Test]
        public async Task SetError_RaisesJsonRpcException_WithCodeAndMessage()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            // -32011 is "FITS header invalid" in the contract's Error model section.
            gateway.SetError("file.open", code: -32011, message: "FITS header invalid");

            var ex = Assert.ThrowsAsync<JsonRpcException>(
                async () => await gateway.SendAsync<object>("file.open", new { path = "x" }));

            Assert.That(ex!.Code, Is.EqualTo(-32011));
            Assert.That(ex.Message, Is.EqualTo("FITS header invalid"));
        }

        [Test]
        public async Task EmitNotification_FiresSubscribersSynchronously()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();

            string? receivedMethod = null;
            gateway.OnNotification += n => receivedMethod = n.Method;

            gateway.EmitNotification("log.emit", new { level = "WARN", msg = "x" });

            Assert.That(receivedMethod, Is.EqualTo("log.emit"));
        }

        private sealed record NAxisOnly(int NAxis);
    }
}
