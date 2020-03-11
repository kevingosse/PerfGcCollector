using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordFork
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Ppid;
        public readonly uint Tid;
        public readonly uint Ptid;
        public readonly ulong Time;

        public PerfRecordFork(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Ppid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Ptid = stream.Read<uint>();
            Time = stream.Read<ulong>();
        }
    }
}