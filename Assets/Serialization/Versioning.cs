
namespace Serialization
{
    /// <summary>
    /// Packs and unpacks the version byte. Bit layout (LSB first): Format, Compatibility, Protocol, DataKind.
    /// Each field is 2 bits (0-3). Policy (supported values) is defined by the consumer.
    /// </summary>
    public static class Versioning
    {
        private const int FieldMask = 0b11;
        private const int FormatShift = 0;
        private const int CompatibilityShift = 2;
        private const int ProtocolShift = 4;
        private const int DataKindShift = 6;

        public static byte Pack(byte dataKind, byte protocol, byte compatibility, byte format)
        {
            return (byte)(
                ((format & FieldMask)        << FormatShift) |
                ((compatibility & FieldMask) << CompatibilityShift) |
                ((protocol & FieldMask)      << ProtocolShift) |
                ((dataKind & FieldMask)      << DataKindShift));
        }

        public static (byte dataKind, byte protocol, byte compatibility, byte format) Unpack(byte version)
        {
            return (
                (byte)((version >> DataKindShift)      & FieldMask),
                (byte)((version >> ProtocolShift)      & FieldMask),
                (byte)((version >> CompatibilityShift) & FieldMask),
                (byte)((version >> FormatShift)        & FieldMask)
            );
        }
    }
}