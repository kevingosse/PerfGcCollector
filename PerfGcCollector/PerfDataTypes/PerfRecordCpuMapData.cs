using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordCpuMapData
    {
        public readonly ushort Type;
        public readonly byte[] Data;

        public PerfRecordCpuMapData(Stream stream, int size)
        {
            Type = stream.Read<ushort>();
            Data = stream.ReadArray<byte>(size - sizeof(ushort));
        }
    }
}