using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordComm
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly byte[] Comm;

        public PerfRecordComm(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Comm = stream.ReadArray<byte>(header.GetRemainingBytes() - sizeof(uint) * 2);
        }
    }
}