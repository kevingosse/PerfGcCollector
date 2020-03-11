using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordKSymbol
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Addr;
        public readonly uint Len;
        public readonly ushort KsymType;
        public readonly ushort Flags;
        public readonly byte[] Name;

        public PerfRecordKSymbol(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Addr = stream.Read<ulong>();
            Len = stream.Read<uint>();
            KsymType = stream.Read<ushort>();
            Flags = stream.Read<ushort>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong) - sizeof(uint) - (sizeof(ushort) * 2);

            Name = stream.ReadArray<byte>(remainingBytes);
        }
    }
}