// Tests for LengthPrefixFraming (the contract's "Framing" section).
//
// Each test calls out the exact spec clause it's checking. That's really the
// whole reason for pulling framing out into its own helper: the tests can quote
// the spec word-for-word without ever spinning up a pipe.

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
            // Contract: "Payload is exactly `length` bytes; no trailing newline
            // inside the frame." Round-trip a non-trivial payload (with a
            // multi-byte UTF-8 character in it) and check the bytes match exactly.
            var payload = Encoding.UTF8.GetBytes(@"{""jsonrpc"":""2.0"",""id"":17,""method"":""file.open"",""params"":{""path"":""/Volumes/cubes/résumé.fits""}}");

            var frame = LengthPrefixFraming.Encode(payload);

            using var stream = new MemoryStream(frame);
            var decoded = await LengthPrefixFraming.ReadOneAsync(stream);

            Assert.That(decoded, Is.EqualTo(payload));
        }

        [Test]
        public async Task EmptyPayload_ProducesZeroLengthFrame()
        {
            // Contract: "length = byte length of the UTF-8 payload, ASCII decimal,
            // no leading zeros." The edge case is zero: it's a single digit, so the
            // frame is just "0\n" with no payload after it.
            var frame = LengthPrefixFraming.Encode(ReadOnlySpan<byte>.Empty);
            Assert.That(frame, Is.EqualTo(new byte[] { (byte)'0', 0x0A }));

            using var stream = new MemoryStream(frame);
            var decoded = await LengthPrefixFraming.ReadOneAsync(stream);
            Assert.That(decoded, Is.Empty);
        }

        [Test]
        public void ReadOneAsync_LeadingZeroInHeader_ThrowsFormatException()
        {
            // Contract: "no leading zeros", so "012\n..." has to be rejected.
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
            // Contract: "Separator is a single \n (0x0A). No \r." Reject the moment
            // we see the CR byte rather than quietly swallowing it.
            var bad = new byte[] { (byte)'5', 0x0D, 0x0A, (byte)'a', (byte)'b' };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<FormatException>()
                              .With.Message.Contains("CR"));
        }

        [Test]
        public void ReadOneAsync_StreamClosedMidPayload_ThrowsEndOfStreamException()
        {
            // Header claims 10 bytes follow but the stream only has 4. A pipe
            // closing mid-frame is the failure we hit most often, so we want it
            // surfaced cleanly enough for the read loop to clean up pending callers.
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
            // Contract: "ASCII decimal." A letter in the header means we've lost
            // sync, so catch it before we go allocating a huge payload buffer.
            var bad = new byte[] { (byte)'1', (byte)'2', (byte)'x', 0x0A };
            using var stream = new MemoryStream(bad);
            Assert.That(async () => await LengthPrefixFraming.ReadOneAsync(stream),
                        Throws.TypeOf<FormatException>()
                              .With.Message.Contains("ASCII decimal"));
        }
    }
}
