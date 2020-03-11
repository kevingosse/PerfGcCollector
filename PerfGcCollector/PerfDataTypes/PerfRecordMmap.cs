using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordMmap
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly ulong Addr;
        public readonly ulong Len;
        public readonly ulong Pgoff;
        public readonly byte[] Filename;

        public PerfRecordMmap(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Addr = stream.Read<ulong>();
            Len = stream.Read<ulong>();
            Pgoff = stream.Read<ulong>();
            Filename = stream.ReadArray<byte>(header.GetRemainingBytes() - sizeof(uint) * 2 - sizeof(ulong) * 3);
        }
    }
}