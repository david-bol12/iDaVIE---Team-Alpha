// Sub-team 6 — LengthPrefixFraming (Gateway Contract v1 §"Framing").
//
// Pure byte-level encode / decode of the wire format:
//   <ascii-decimal length><LF><utf-8 json payload>
//
// Extracted from JsonRpcPipeGateway so framing correctness can be tested
// against the spec text without standing up a real pipe. Every clause below
// quotes the corresponding sentence in Gateway Contract v1.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Encode / decode the length-prefixed wire frames described in Gateway Contract v1
    /// §"Framing". The format intentionally omits the LSP <c>Content-Length:</c>
    /// header — cheaper to parse and still tail-debuggable.
    /// </summary>
    public static class LengthPrefixFraming
    {
        // Gateway Contract v1: "Separator is a single \n (0x0A). No \r."
        private const byte Lf = 0x0A;
        private const byte Cr = 0x0D;

        /// <summary>
        /// Build a single frame from a UTF-8 JSON payload.
        /// </summary>
        public static byte[] Encode(ReadOnlySpan<byte> payload)
        {
            // Gateway Contract v1: "length = byte length of the UTF-8 payload, ASCII decimal,
            // no leading zeros."
            var header = Encoding.ASCII.GetBytes(payload.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var frame = new byte[header.Length + 1 + payload.Length];
            Buffer.BlockCopy(header, 0, frame, 0, header.Length);
            frame[header.Length] = Lf;
            payload.CopyTo(frame.AsSpan(header.Length + 1));
            return frame;
        }

        /// <summary>
        /// Read exactly one frame from <paramref name="stream"/>. Reads the
        /// ASCII-decimal length, the LF separator, then the payload bytes.
        /// Throws <see cref="FormatException"/> on a malformed header.
        /// Throws <see cref="EndOfStreamException"/> if the stream closes mid-frame.
        /// </summary>
        public static async Task<byte[]> ReadOneAsync(Stream stream, CancellationToken ct = default)
        {
            var headerBuilder = new StringBuilder(16);
            var one = new byte[1];

            while (true)
            {
                var read = await stream.ReadAsync(one.AsMemory(0, 1), ct).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        headerBuilder.Length == 0
                            ? "Pipe closed before frame header."
                            : $"Pipe closed mid-header after '{headerBuilder}'.");
                }

                if (one[0] == Lf) break;

                // Gateway Contract v1: "No \r."
                if (one[0] == Cr)
                    throw new FormatException("Frame header contained CR (0x0D); Gateway Contract v1 forbids \\r.");

                // Gateway Contract v1: "ASCII decimal" — reject anything else early.
                if (one[0] < (byte)'0' || one[0] > (byte)'9')
                    throw new FormatException($"Frame header byte 0x{one[0]:X2} is not an ASCII decimal digit.");

                headerBuilder.Append((char)one[0]);

                if (headerBuilder.Length > 12)
                    throw new FormatException("Frame header exceeded 12 digits — almost certainly a desync.");
            }

            if (headerBuilder.Length == 0)
                throw new FormatException("Frame header was empty (LF received with no preceding digits).");

            // Gateway Contract v1: "no leading zeros."
            if (headerBuilder.Length > 1 && headerBuilder[0] == '0')
                throw new FormatException($"Frame header had a leading zero: '{headerBuilder}'.");

            if (!int.TryParse(headerBuilder.ToString(), System.Globalization.NumberStyles.None,
                               System.Globalization.CultureInfo.InvariantCulture, out var length))
            {
                throw new FormatException($"Frame header '{headerBuilder}' did not parse as a non-negative int.");
            }

            var payload = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = await stream.ReadAsync(payload.AsMemory(offset, length - offset), ct).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"Pipe closed mid-payload at byte {offset}/{length}.");
                offset += read;
            }

            return payload;
        }
    }
}
