// Sub-team 6 — LengthPrefixFraming tests (ADR-0002 §"Framing").
//
// Every test references the exact spec clause it pins down. The whole point
// of having a pure byte-level framing helper is that these tests can quote
// the spec verbatim without spinning up a pipe.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace iDaVIE.Client.Gateway.Tests
{
    [TestFixture]
    [Category("Gateway")]
    public class LengthPrefixFramingTests
    {
        [Test]
        public async Task RoundTrip_PreservesPayloadBytes_Exactly()
        {
            // ADR-0002: "Payload is exactly `length` bytes; no trailing newline
            // inside the frame." Round-trip a non-trivial payload (including a
            // multi-byte UTF-8 character) and assert byte equality.
            var payload = Encoding.UTF8.GetBytes(@"{""jsonrpc"":""2.0"",""id"":17,""method"":""file.open"",""params"":{""path"":""/Volumes/cubes/résumé.fits""}}");

            var frame = LengthPrefixFraming.Encode(payload);

            using var stream = new MemoryStream(frame);
            var decoded = await LengthPrefixFraming.ReadOneAsync(stream);

            Assert.That(decoded, Is.EqualTo(payload));
        }

        [Test]
        public async Task EmptyPayload_ProducesZeroLengthFrame()
        {
            // ADR-0002: "length = byte length of the UTF-8 payload, ASCII
            // decimal, no leading zeros." Edge case: zero is one digit; the
            // frame is "0\n" with no payload bytes.
            var frame = LengthPrefixFraming.Encode(ReadOnlySpan<byte>.Empty);
            Assert.That(frame, Is.EqualTo(new byte[] { (byte)'0', 0x0A }));

            using var stream = new MemoryStream(frame);
            var decoded = await LengthPrefixFraming.ReadOneAsync(stream);
            Assert.That(decoded, Is.Empty);
        }

        [Test]
        public void ReadOneAsync_LeadingZeroInHeader_ThrowsFormatException()
        {
            // ADR-0002: "no leading zeros." "012\n..." must reject.
            var bad = new byte[] { (byte)'0', (byte)'1', (byte)'2', 0x0A,
                                   (byte)'a', (byte)'b', (byte)'c' };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<FormatException>()
                              .With.Message.Contains("leading zero"));
        }

        [Test]
        public void ReadOneAsync_CarriageReturnInHeader_ThrowsFormatException()
        {
            // ADR-0002: "Separator is a single \n (0x0A). No \r." Reject as
            // soon as the CR byte is observed — do not consume it silently.
            var bad = new byte[] { (byte)'5', 0x0D, 0x0A, (byte)'a', (byte)'b' };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<FormatException>()
                              .With.Message.Contains("CR"));
        }

        [Test]
        public void ReadOneAsync_StreamClosedMidPayload_ThrowsEndOfStreamException()
        {
            // Header says 10 bytes follow, stream only delivers 4. Real pipes
            // closing mid-frame is the most common failure we need to surface
            // cleanly so the gateway's read loop can clean up pending callers.
            var bad = new byte[] { (byte)'1', (byte)'0', 0x0A,
                                   (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<EndOfStreamException>()
                              .With.Message.Contains("mid-payload"));
        }

        [Test]
        public void ReadOneAsync_NonDigitInHeader_ThrowsFormatException()
        {
            // ADR-0002: "ASCII decimal." A letter in the header is a desync —
            // catch it before allocating a huge payload buffer.
            var bad = new byte[] { (byte)'1', (byte)'2', (byte)'x', 0x0A };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<FormatException>()
                              .With.Message.Contains("ASCII decimal"));
        }
    }
}
