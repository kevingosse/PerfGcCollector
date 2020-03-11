using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerfRecordThreadMapEntry
    {
        public readonly ulong Pid;
        public readonly byte[] Comm;

        public PerfRecordThreadMapEntry(Stream stream)
        {
            Pid = stream.Read<ulong>();
            Comm = stream.ReadArray<byte>(16);
        }
    }
}