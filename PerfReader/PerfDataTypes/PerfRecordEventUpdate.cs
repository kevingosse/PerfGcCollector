using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordEventUpdate
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Type;
        public readonly ulong Id;
        public readonly byte[] Data;

        public PerfRecordEventUpdate(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Type = stream.Read<ulong>();
            Id = stream.Read<ulong>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong) * 2;
            Data = stream.ReadArray<byte>(remainingBytes);
        }
    }
}