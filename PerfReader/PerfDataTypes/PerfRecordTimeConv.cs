using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordTimeConv
    {
        public readonly PerfEventHeader Header;
        public readonly ulong TimeShift;
        public readonly ulong TimeMult;
        public readonly ulong TimeZero;

        public PerfRecordTimeConv(Stream stream, PerfEventHeader header)
        {
            Header = header;
            TimeShift = stream.Read<ulong>();
            TimeMult = stream.Read<ulong>();
            TimeZero = stream.Read<ulong>();
        }
    }
}