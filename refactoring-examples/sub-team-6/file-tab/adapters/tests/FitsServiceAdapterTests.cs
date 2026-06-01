// Sub-team 6 — FitsServiceAdapter wire-level round-trip tests.
//
// These tests are the answer to the audit's F9 ("transport contract has no
// consumer"). They prove that calling IFitsService through the rewired
// adapter produces exactly the JSON-RPC methods and params shape mandated
// by Gateway Contract v1 §"Method catalogue (v1)" — using FakeGateway in place of a
// real named pipe, so the suite runs in 10s of ms under standard CI.
//
// What each test pins:
//   1. OpenImageAsync_HappyPath_DispatchesFileOpenThenDatasetGetAxes
//      Asserts the two-call protocol (file.open → dataset.getAxes), correct
//      params on each, and the resulting FitsFileInfo carries the metadata
//      from dataset.getAxes.
//   2. OpenImageAsync_GetAxesFails_FiresBestEffortFileClose
//      Asserts the "we just allocated a server-side dataset id we will
//      never use" cleanup path — adapter must not leak it.
//   3. GetHeaderTextAsync_DispatchesDatasetGetHeader_WithDatasetIdAndHdu
//      Pins the third method name and its params shape.
//   4. FitsHandleDispose_FiresFileClose
//      Pins handle disposal → file.close (best-effort, fire-and-forget).

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.FileTab;
using NUnit.Framework;

namespace iDaVIE.Desktop.Adapters.FileTab.Tests
{
    [TestFixture]
    [Category("Adapter")]
    public class FitsServiceAdapterTests
    {
        [Test]
        public async Task OpenImageAsync_HappyPath_DispatchesFileOpenThenDatasetGetAxes()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();

            // Stub the two calls the adapter is expected to make, in order.
            gateway.SetResponse("file.open", new { datasetId = "ds-3f1c" });
            gateway.SetResponse("dataset.getAxes", new
            {
                hduList   = new[] { new { index = 1, name = "PRIMARY", hduType = "IMAGE" } },
                nAxis     = 3,
                axisSizes = new System.Collections.Generic.Dictionary<int, long>
                {
                    [1] = 256, [2] = 256, [3] = 64,
                },
                headerText     = "SIMPLE  =  T\n",
                estimatedBytes = 16_777_216L,
            });

            var adapter = new FitsServiceAdapter(gateway);
            var info = await adapter.OpenImageAsync("/data/cube.fits");

            // Two calls, in the documented order.
            var sent = gateway.Sent.ToArray();
            Assert.That(sent.Length, Is.EqualTo(2));
            Assert.That(sent[0].Method, Is.EqualTo("file.open"));
            Assert.That(sent[1].Method, Is.EqualTo("dataset.getAxes"));

            // file.open params: { path, isMask }. Property names must match
            // the camelCase wire shape the real pipe will see.
            var openParams = sent[0].Params!.Value;
            Assert.That(openParams.GetProperty("path").GetString(), Is.EqualTo("/data/cube.fits"));
            Assert.That(openParams.GetProperty("isMask").GetBoolean(), Is.False);

            // dataset.getAxes params: { datasetId }.
            var axesParams = sent[1].Params!.Value;
            Assert.That(axesParams.GetProperty("datasetId").GetString(), Is.EqualTo("ds-3f1c"));

            // Returned DTO carries metadata from the second response.
            Assert.That(info.NAxis, Is.EqualTo(3));
            Assert.That(info.AxisSizes[3], Is.EqualTo(64));
            Assert.That(info.HeaderText, Does.Contain("SIMPLE"));
            Assert.That(info.EstimatedBytes, Is.EqualTo(16_777_216L));
            Assert.That(info.FilePath, Is.EqualTo("/data/cube.fits"));
        }

        [Test]
        public async Task OpenImageAsync_GetAxesFails_FiresBestEffortFileClose()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();

            gateway.SetResponse("file.open", new { datasetId = "ds-orphan" });
            // dataset.getAxes raises a JSON-RPC error — code -32030 = native
            // plug-in failure per Gateway Contract v1 §"Error model".
            gateway.SetError("dataset.getAxes", code: -32030, message: "plug-in panicked");
            gateway.SetResponse<object?>("file.close", null);

            var adapter = new FitsServiceAdapter(gateway);

            var ex = Assert.ThrowsAsync<JsonRpcException>(
                async () => await adapter.OpenImageAsync("/data/bad.fits"));
            Assert.That(ex!.Code, Is.EqualTo(-32030));

            // file.close is fire-and-forget — give the continuation a tick to
            // run before asserting on the sent log.
            for (var i = 0; i < 10 && gateway.Sent.Count < 3; i++)
                await Task.Delay(10);

            var sent = gateway.Sent.ToArray();
            Assert.That(sent.Length, Is.EqualTo(3));
            Assert.That(sent[2].Method, Is.EqualTo("file.close"));
            Assert.That(sent[2].Params!.Value.GetProperty("datasetId").GetString(),
                        Is.EqualTo("ds-orphan"));
        }

        [Test]
        public async Task GetHeaderTextAsync_DispatchesDatasetGetHeader_WithDatasetIdAndHdu()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            gateway.SetResponse("file.open", new { datasetId = "ds-1" });
            gateway.SetResponse("dataset.getAxes", new
            {
                hduList   = System.Array.Empty<HduInfo>(),
                nAxis     = 0,
                axisSizes = new System.Collections.Generic.Dictionary<int, long>(),
                headerText     = "",
                estimatedBytes = 0L,
            });
            gateway.SetResponse("dataset.getHeader", "EXTNAME = SCI\n");

            var adapter = new FitsServiceAdapter(gateway);
            var info = await adapter.OpenImageAsync("/x.fits");

            var header = await adapter.GetHeaderTextAsync(info.Handle, hduIndex: 2);

            Assert.That(header, Is.EqualTo("EXTNAME = SCI\n"));

            var headerCall = gateway.Sent.Last();
            Assert.That(headerCall.Method, Is.EqualTo("dataset.getHeader"));
            var hp = headerCall.Params!.Value;
            Assert.That(hp.GetProperty("datasetId").GetString(), Is.EqualTo("ds-1"));
            Assert.That(hp.GetProperty("hduIndex").GetInt32(), Is.EqualTo(2));
        }

        [Test]
        public async Task FitsHandleDispose_FiresFileClose()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            gateway.SetResponse("file.open", new { datasetId = "ds-7" });
            gateway.SetResponse("dataset.getAxes", new
            {
                hduList   = System.Array.Empty<HduInfo>(),
                nAxis     = 0,
                axisSizes = new System.Collections.Generic.Dictionary<int, long>(),
                headerText     = "",
                estimatedBytes = 0L,
            });
            gateway.SetResponse<object?>("file.close", null);

            var adapter = new FitsServiceAdapter(gateway);
            var info = await adapter.OpenImageAsync("/x.fits");

            info.Handle.Dispose();

            for (var i = 0; i < 10 && gateway.Sent.Count < 3; i++)
                await Task.Delay(10);

            var closeCall = gateway.Sent.Last();
            Assert.That(closeCall.Method, Is.EqualTo("file.close"));
            Assert.That(closeCall.Params!.Value.GetProperty("datasetId").GetString(),
                        Is.EqualTo("ds-7"));
        }
    }
}
