using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordThreadMap
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Nr;
        public readonly PerfRecordThreadMapEntry[] Entries;

        public PerfRecordThreadMap(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Nr = stream.Read<ulong>();
            Entries = new PerfRecordThreadMapEntry[Nr];

            for (ulong i = 0; i < Nr; i++)
            {
                Entries[i] = new PerfRecordThreadMapEntry(stream);
            }
        }
    }
}