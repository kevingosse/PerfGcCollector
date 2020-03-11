using System.IO;
using System.Runtime.InteropServices;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordCpuMap
    {
        public readonly PerfEventHeader Header;
        public readonly PerfRecordCpuMapData Data;

        public PerfRecordCpuMap(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Data = new PerfRecordCpuMapData(stream, header.GetRemainingBytes());
        }
    }
}