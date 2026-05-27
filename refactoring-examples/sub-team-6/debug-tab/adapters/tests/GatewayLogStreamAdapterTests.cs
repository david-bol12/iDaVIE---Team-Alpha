// Sub-team 6 — GatewayLogStreamAdapter wire-level tests.
//
// Companion to FitsServiceAdapterTests for the push-stream half of the
// transport (audit F10). Asserts that a server-pushed log.emit notification
// on the gateway becomes a structured LogEntry on the inner ILogStream,
// per ADR-0002 §"Message shape".
//
// What each test pins:
//   1. LogEmitNotification_PublishesToSubscribers_WithLevelMsgAndTimestamp
//      Happy path: WARN level, message body, ISO-8601 ts → preserved end-to-end.
//   2. LogEmitNotification_UnknownLevel_FallsBackToInfo
//      Lenient level parsing — unknown strings do not drop the entry.
//   3. NotificationForOtherMethod_IsIgnored
//      log.emit filter: progress.update and others do not leak into ILogStream.
//   4. Dispose_DetachesFromGatewayNotifications
//      Lifetime hygiene: post-dispose notifications must not reach observers.

using System;
using System.Collections.Generic;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.DebugTab;
using NUnit.Framework;

namespace iDaVIE.Desktop.Adapters.DebugTab.Tests
{
    [TestFixture]
    [Category("Adapter")]
    public class GatewayLogStreamAdapterTests
    {
        private sealed class CapturingObserver : ILogObserver
        {
            public List<LogEntry> Received { get; } = new();
            public void OnNext(LogEntry entry) => Received.Add(entry);
        }

        [Test]
        public async System.Threading.Tasks.Task LogEmitNotification_PublishesToSubscribers_WithLevelMsgAndTimestamp()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            using var adapter = new GatewayLogStreamAdapter(gateway);

            var observer = new CapturingObserver();
            adapter.Subscribe(observer);

            gateway.EmitNotification("log.emit", new
            {
                level = "WARN",
                msg   = "VR init slow",
                ts    = "2026-05-21T09:14:02Z",
            });

            Assert.That(observer.Received, Has.Count.EqualTo(1));
            var entry = observer.Received[0];
            Assert.That(entry.Level,     Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Message,   Is.EqualTo("VR init slow"));
            Assert.That(entry.Timestamp, Is.EqualTo(new DateTime(2026, 5, 21, 9, 14, 2, DateTimeKind.Utc)));
        }

        [Test]
        public async System.Threading.Tasks.Task LogEmitNotification_UnknownLevel_FallsBackToInfo()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            using var adapter = new GatewayLogStreamAdapter(gateway);

            var observer = new CapturingObserver();
            adapter.Subscribe(observer);

            // "TRACE" is not in our LogLevel enum but the server might emit
            // it. The adapter must keep the entry and map sensibly rather
            // than throwing or silently dropping.
            gateway.EmitNotification("log.emit", new { level = "TRACE", msg = "x", ts = (string?)null });

            Assert.That(observer.Received, Has.Count.EqualTo(1));
            Assert.That(observer.Received[0].Level, Is.EqualTo(LogLevel.Info));
        }

        [Test]
        public async System.Threading.Tasks.Task NotificationForOtherMethod_IsIgnored()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            using var adapter = new GatewayLogStreamAdapter(gateway);

            var observer = new CapturingObserver();
            adapter.Subscribe(observer);

            // progress.update is in the catalogue for the File tab — it
            // arrives on the same OnNotification fan-out and must not be
            // mistaken for a log entry.
            gateway.EmitNotification("progress.update", new { fraction = 0.5 });

            Assert.That(observer.Received, Is.Empty);
        }

        [Test]
        public async System.Threading.Tasks.Task Dispose_DetachesFromGatewayNotifications()
        {
            var gateway = new FakeGateway();
            await gateway.ConnectAsync();
            var adapter = new GatewayLogStreamAdapter(gateway);

            var observer = new CapturingObserver();
            adapter.Subscribe(observer);

            adapter.Dispose();
            gateway.EmitNotification("log.emit", new { level = "INFO", msg = "post-dispose", ts = (string?)null });

            Assert.That(observer.Received, Is.Empty,
                "Adapter must not deliver entries after Dispose — the gateway " +
                "subscription should already be removed.");
        }
    }
}
