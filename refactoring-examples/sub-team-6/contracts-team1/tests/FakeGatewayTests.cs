// Sub-team 6 — FakeGateway behavioural tests.
//
// Pins down the FakeGateway contract that the adapter test projects depend on.
// Each test is also a worked example of how a downstream test sets up the
// gateway double.

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
            // Mirrors JsonRpcPipeGateway: a Send without ConnectAsync first is
            // a programmer error, not a transport error.
            Assert.That(async () => await gateway.SendAsync<int>("anything", null),
                        Throws.TypeOf<InvalidOperationException>()
                              .With.Message.Contains("not connected"));
        }

        [Test]
        public async Task SendAsync_NoHandler_Throws_WithMethodNameInMessage()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            // The "test forgot to stub" trap: assertion message must name the
            // method so the failing test points the reader at the missing
            // SetResponse call.
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
            // Code -32011 = "FITS header invalid" per ADR-0002 §"Error model".
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
