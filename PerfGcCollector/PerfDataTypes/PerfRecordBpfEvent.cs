using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordBpfEvent
    {
        public readonly PerfEventHeader Header;
        public readonly PerfBpfEventType Type;
        public readonly ushort Flags;
        public readonly uint Id;
        public readonly byte[] Tag;

        public unsafe PerfRecordBpfEvent(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Type = stream.Read<PerfBpfEventType>();
            Flags = stream.Read<ushort>();
            Id = stream.Read<uint>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(PerfBpfEventType) - sizeof(ushort) - sizeof(uint);
            Tag = stream.ReadArray<byte>(remainingBytes);
        }
    }
}