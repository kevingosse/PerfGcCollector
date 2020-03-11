using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordHeaderAttr
    {
        public readonly PerfEventHeader Header;
        public readonly PerfEventAttr Attr;
        public readonly ulong[] Id;

        public unsafe PerfRecordHeaderAttr(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Attr = stream.Read<PerfEventAttr>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(PerfEventAttr);
            Id = stream.ReadArray<ulong>(remainingBytes);
        }
    }
}