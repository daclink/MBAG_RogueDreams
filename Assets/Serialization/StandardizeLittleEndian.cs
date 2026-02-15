using System;
using System.Buffers.Binary;
using System.IO;

namespace Serialization
{
    /// <summary>
    /// Helpers for reading/writing multi-byte primitives in little-endian order,
    /// regardless of host endianness. Uses BinaryPrimitives for cross-platform binary serialization.
    /// </summary>
    public static class StandardizeLittleEndian
    {
        /// <summary>
        /// Validates passed data stream. Throws exception if null.
        /// </summary>
        /// <param name="stream">Target data stream.</param>
        private static void ValidateStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// Allocate a 4-byte span (32-bits) on the stack.
        /// Then encode the passed value in little-endian order onto the span.
        /// Finally, write to the stream.
        /// </summary>
        /// <param name="stream">The destination data stream.</param>
        /// <param name="value">Value to be written to the stream.</param>
        public static void WriteInt32(Stream stream, int value)
        {
            ValidateStream(stream);

            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Allocate an 8-byte span (64-bits) on the stack.
        /// Then encode the passed value in little-endian order onto the span.
        /// Finally, write to the stream.
        /// </summary>
        /// <param name="stream">The destination data stream.</param>
        /// <param name="value">Value to be written to the stream.</param>
        public static void WriteUInt64(Stream stream, ulong value)
        {
            ValidateStream(stream);

            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            stream.Write(buffer);
        }

        /// <summary>
        /// Allocate a 4-byte span (32-bits) on the stack.
        /// Then read exactly 4 bytes from the stream to the span and throw if less than 4 bytes are read.
        /// Finally, interpret the data as a little-endian integer and return the value.
        /// </summary>
        /// <param name="stream">The target data stream.</param>
        /// <returns>The 32-bit signed integer read from the stream.</returns>
        public static int ReadInt32(Stream stream)
        {
            ValidateStream(stream);

            Span<byte> buffer = stackalloc byte[4];
            _ = stream.Read(buffer) == 4 ? 0 : throw new EndOfStreamException("Expected 4 bytes for Int32.");
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        /// <summary>
        /// Allocate an 8-byte span (64-bits) on the stack.
        /// Then read exactly 8 bytes from the stream to the span and throw if less than 8 bytes are read.
        /// Finally, interpret the data as a little-endian integer and return the value.
        /// </summary>
        /// <param name="stream">The target data stream.</param>
        /// <returns>The 64-bit unsigned integer read from the stream.</returns>
        public static ulong ReadUInt64(Stream stream)
        {
            ValidateStream(stream);

            Span<byte> buffer = stackalloc byte[8];
            _ = stream.Read(buffer) == 8 ? 0 : throw new EndOfStreamException("Expected 8 bytes for UInt64.");
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }
    }
}
