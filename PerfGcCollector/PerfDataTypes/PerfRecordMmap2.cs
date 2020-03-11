using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordMmap2
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly ulong Addr;
        public readonly ulong Len;
        public readonly ulong Pgoff;
        public readonly uint Maj;
        public readonly uint Min;
        public readonly ulong Ino;
        public readonly ulong Ino_generation;
        public readonly uint Prot;
        public readonly uint Flags;
        public readonly byte[] filename;

        public PerfRecordMmap2(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Addr = stream.Read<ulong>();
            Len = stream.Read<ulong>();
            Pgoff = stream.Read<ulong>();
            Maj = stream.Read<uint>();
            Min = stream.Read<uint>();
            Ino = stream.Read<ulong>();
            Ino_generation = stream.Read<ulong>();
            Prot = stream.Read<uint>();
            Flags = stream.Read<uint>();

            var remainingSize = header.GetRemainingBytes() - (sizeof(uint) * 6) - (sizeof(ulong) * 5);
            filename = stream.ReadArray<byte>(remainingSize);
        }
    }
}