// brief §6.6 | Debug tab AFTER — unit tests for LogStream and DebugTabViewModel (NUnit 3)
// No Unity dependency. Satisfies NFR-TST-1 and Section 9.2 testability evidence.
// Run with: dotnet test debug-tab/tests/DebugTabTests.csproj
using System;
using System.Collections.Generic;
using iDaVIE.Desktop.DebugTab;
using NUnit.Framework;

namespace iDaVIE.Desktop.DebugTab.Tests
{
    // ── Shared test double ─────────────────────────────────────────────────────

    internal sealed class LambdaObserver : ILogObserver
    {
        private readonly Action<LogEntry> _onNext;
        public LambdaObserver(Action<LogEntry> onNext) => _onNext = onNext;
        public void OnNext(LogEntry entry) => _onNext(entry);
    }

    /// <summary>
    /// Fake ILogStream used by DebugTabViewModel tests so they don't depend on the
    /// production LogStream implementation. Verifies the ViewModel subscribes and
    /// receives entries correctly regardless of the underlying stream.
    /// </summary>
    internal sealed class FakeLogStream : ILogStream
    {
        private readonly List<ILogObserver> _observers = new();

        public void Publish(LogLevel level, string message)
            => Publish(level, message, DateTime.UtcNow);

        public void Publish(LogLevel level, string message, DateTime timestamp)
        {
            var entry = new LogEntry(level, message, timestamp);
            foreach (var o in _observers) o.OnNext(entry);
        }

        public void Subscribe(ILogObserver observer)
        {
            if (!_observers.Contains(observer)) _observers.Add(observer);
        }

        public void Unsubscribe(ILogObserver observer)
            => _observers.Remove(observer);

        public int ObserverCount => _observers.Count;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LogStream tests (production implementation)
    // ═══════════════════════════════════════════════════════════════════════════

    [TestFixture]
    public sealed class LogStreamTests
    {
        [Test]
        public void Publish_ToSubscriber_DeliversEntry()
        {
            var stream   = new LogStream();
            LogEntry? got = null;
            stream.Subscribe(new LambdaObserver(e => got = e));

            stream.Publish(LogLevel.Info, "hello");

            Assert.IsNotNull(got);
            Assert.AreEqual(LogLevel.Info, got!.Level);
            Assert.AreEqual("hello", got.Message);
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            var stream = new LogStream();
            Assert.DoesNotThrow(() => stream.Publish(LogLevel.Warning, "nobody listening"));
        }

        [Test]
        public void Publish_SetsTimestamp_WithinReasonableBounds()
        {
            var stream = new LogStream();
            var before = DateTime.UtcNow;
            LogEntry? got = null;
            stream.Subscribe(new LambdaObserver(e => got = e));

            stream.Publish(LogLevel.Info, "t");

            var after = DateTime.UtcNow;
            Assert.GreaterOrEqual(got!.Timestamp, before);
            Assert.LessOrEqual(got.Timestamp, after);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var stream = new LogStream();
            int count  = 0;
            var obs    = new LambdaObserver(_ => count++);
            stream.Subscribe(obs);

            stream.Publish(LogLevel.Info, "first");
            stream.Unsubscribe(obs);
            stream.Publish(LogLevel.Info, "second");

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Subscribe_DuplicateObserver_DeliverOnlyOnce()
        {
            var stream = new LogStream();
            int count  = 0;
            var obs    = new LambdaObserver(_ => count++);
            stream.Subscribe(obs);
            stream.Subscribe(obs);   // duplicate — should be ignored

            stream.Publish(LogLevel.Info, "msg");

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllReceive()
        {
            var stream = new LogStream();
            var r1     = new List<LogEntry>();
            var r2     = new List<LogEntry>();
            stream.Subscribe(new LambdaObserver(r1.Add));
            stream.Subscribe(new LambdaObserver(r2.Add));

            stream.Publish(LogLevel.Error, "boom");

            Assert.AreEqual(1, r1.Count);
            Assert.AreEqual(1, r2.Count);
        }

        [Test]
        public void Subscribe_NullObserver_ThrowsArgumentNullException()
        {
            var stream = new LogStream();
            Assert.Throws<ArgumentNullException>(() => stream.Subscribe(null!));
        }

        [Test]
        public void Unsubscribe_NullObserver_ThrowsArgumentNullException()
        {
            var stream = new LogStream();
            Assert.Throws<ArgumentNullException>(() => stream.Unsubscribe(null!));
        }

        [Test]
        public void Publish_DeliversCopyOfSnapshotSoLateUnsubscribeIsHandledSafely()
        {
            // An observer that unsubscribes itself during delivery must not throw
            var stream      = new LogStream();
            ILogObserver? selfUnsubscriber = null;
            selfUnsubscriber = new LambdaObserver(_ => stream.Unsubscribe(selfUnsubscriber!));
            stream.Subscribe(selfUnsubscriber);

            Assert.DoesNotThrow(() => stream.Publish(LogLevel.Info, "self-remove"));
        }

        [Test]
        public void AllLogLevels_AreForwarded_Correctly()
        {
            foreach (var level in new[] { LogLevel.Info, LogLevel.Warning, LogLevel.Error })
            {
                var stream = new LogStream();
                LogEntry? got = null;
                stream.Subscribe(new LambdaObserver(e => got = e));
                stream.Publish(level, "test");
                Assert.AreEqual(level, got!.Level, $"Level {level} was not forwarded");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DebugTabViewModel tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestFixture]
    public sealed class DebugTabViewModelTests
    {
        private static (DebugTabViewModel vm, FakeLogStream stream) Build()
        {
            var stream = new FakeLogStream();
            var vm     = new DebugTabViewModel(stream);
            return (vm, stream);
        }

        // ── Construction ──────────────────────────────────────────────────────

        [Test]
        public void Constructor_SubscribesToProvidedStream()
        {
            var (vm, stream) = Build();
            stream.Publish(LogLevel.Info, "after subscribe");

            Assert.AreEqual(1, vm.LogEntries.Count);
        }

        [Test]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new DebugTabViewModel(null!));
        }

        [Test]
        public void Constructor_LogEntriesStartsEmpty()
        {
            var (vm, _) = Build();
            Assert.AreEqual(0, vm.LogEntries.Count);
        }

        // ── AppendEntry ────────────────────────────────────────────────────────

        [Test]
        public void AppendEntry_AddsEntry()
        {
            var (vm, _) = Build();
            var entry   = new LogEntry(LogLevel.Info, "msg", DateTime.UtcNow);

            vm.AppendEntry(entry);

            Assert.AreEqual(1, vm.LogEntries.Count);
            Assert.AreSame(entry, vm.LogEntries[0]);
        }

        [Test]
        public void AppendEntry_RaisesEntriesChanged()
        {
            var (vm, _) = Build();
            bool raised = false;
            vm.EntriesChanged += () => raised = true;

            vm.AppendEntry(new LogEntry(LogLevel.Info, "x", DateTime.UtcNow));

            Assert.IsTrue(raised);
        }

        [Test]
        public void AppendEntry_NullEntry_ThrowsArgumentNullException()
        {
            var (vm, _) = Build();
            Assert.Throws<ArgumentNullException>(() => vm.AppendEntry(null!));
        }

        [Test]
        public void AppendEntry_MultipleEntries_PreservesOrder()
        {
            var (vm, _) = Build();
            vm.AppendEntry(new LogEntry(LogLevel.Info,    "first",  DateTime.UtcNow));
            vm.AppendEntry(new LogEntry(LogLevel.Warning, "second", DateTime.UtcNow));
            vm.AppendEntry(new LogEntry(LogLevel.Error,   "third",  DateTime.UtcNow));

            Assert.AreEqual("first",  vm.LogEntries[0].Message);
            Assert.AreEqual("second", vm.LogEntries[1].Message);
            Assert.AreEqual("third",  vm.LogEntries[2].Message);
        }

        // ── ClearEntries ───────────────────────────────────────────────────────

        [Test]
        public void ClearEntries_RemovesAllEntries()
        {
            var (vm, _) = Build();
            vm.AppendEntry(new LogEntry(LogLevel.Info, "a", DateTime.UtcNow));
            vm.AppendEntry(new LogEntry(LogLevel.Info, "b", DateTime.UtcNow));

            vm.ClearEntries();

            Assert.AreEqual(0, vm.LogEntries.Count);
        }

        [Test]
        public void ClearEntries_RaisesEntriesChanged()
        {
            var (vm, _) = Build();
            vm.AppendEntry(new LogEntry(LogLevel.Info, "x", DateTime.UtcNow));
            bool raised = false;
            vm.EntriesChanged += () => raised = true;

            vm.ClearEntries();

            Assert.IsTrue(raised);
        }

        [Test]
        public void ClearEntries_OnEmptyList_DoesNotThrow()
        {
            var (vm, _) = Build();
            Assert.DoesNotThrow(() => vm.ClearEntries());
        }

        // ── Observer integration ───────────────────────────────────────────────

        [Test]
        public void StreamPublish_ReceiveEntry_CorrectLevel()
        {
            var (vm, stream) = Build();
            stream.Publish(LogLevel.Error, "something broke");

            Assert.AreEqual(1, vm.LogEntries.Count);
            Assert.AreEqual(LogLevel.Error, vm.LogEntries[0].Level);
        }

        [Test]
        public void StreamPublish_ReceiveEntry_CorrectMessage()
        {
            var (vm, stream) = Build();
            stream.Publish(LogLevel.Warning, "watch out");

            Assert.AreEqual("watch out", vm.LogEntries[0].Message);
        }

        [Test]
        public void StreamPublish_MultipleMessages_AllAccumulate()
        {
            var (vm, stream) = Build();
            stream.Publish(LogLevel.Info,    "first");
            stream.Publish(LogLevel.Warning, "second");
            stream.Publish(LogLevel.Error,   "third");

            Assert.AreEqual(3, vm.LogEntries.Count);
        }

        [Test]
        public void StreamPublish_RaisesEntriesChanged()
        {
            var (vm, stream) = Build();
            bool raised      = false;
            vm.EntriesChanged += () => raised = true;

            stream.Publish(LogLevel.Info, "ping");

            Assert.IsTrue(raised);
        }

        // ── LogEntries is read-only ────────────────────────────────────────────

        [Test]
        public void LogEntries_IsReadOnlyCollection()
        {
            var (vm, _) = Build();
            // IReadOnlyList<T> — ensure the concrete type does not expose mutation
            Assert.IsInstanceOf<System.Collections.Generic.IReadOnlyList<LogEntry>>(vm.LogEntries);
        }

        // ── Entry cap ──────────────────────────────────────────────────────────

        [Test]
        public void AppendEntry_OverCap_TrimsOldestEntry()
        {
            // Fill past the internal MaxEntries cap (2000) and verify the oldest
            // entries are dropped rather than the list growing unbounded.
            var (vm, stream) = Build();

            for (int i = 0; i < 2001; i++)
                stream.Publish(LogLevel.Info, $"msg{i}");

            // List must not exceed the cap
            Assert.LessOrEqual(vm.LogEntries.Count, 2000);

            // Most-recent entry must be the last one published
            Assert.AreEqual("msg2000", vm.LogEntries[vm.LogEntries.Count - 1].Message);
        }

        // ── Dispose ────────────────────────────────────────────────────────────

        [Test]
        public void Dispose_UnsubscribesFromStream()
        {
            var (vm, stream) = Build();

            vm.Dispose();
            stream.Publish(LogLevel.Info, "after dispose");

            // ViewModel must not receive entries after unsubscribing
            Assert.AreEqual(0, vm.LogEntries.Count);
        }

        [Test]
        public void Dispose_Twice_DoesNotThrow()
        {
            var (vm, _) = Build();
            vm.Dispose();
            Assert.DoesNotThrow(() => vm.Dispose());
        }

        [Test]
        public void Dispose_LeavesExistingEntriesIntact()
        {
            var (vm, stream) = Build();
            stream.Publish(LogLevel.Info, "before dispose");

            vm.Dispose();

            Assert.AreEqual(1, vm.LogEntries.Count);
        }

        // ── AutoScrollEnabled ──────────────────────────────────────────────────

        [Test]
        public void AutoScrollEnabled_DefaultsToTrue()
        {
            var (vm, _) = Build();
            Assert.IsTrue(vm.AutoScrollEnabled);
        }

        [Test]
        public void AutoScrollEnabled_CanBeToggledOff()
        {
            var (vm, _) = Build();
            vm.AutoScrollEnabled = false;
            Assert.IsFalse(vm.AutoScrollEnabled);
        }
    }
}
