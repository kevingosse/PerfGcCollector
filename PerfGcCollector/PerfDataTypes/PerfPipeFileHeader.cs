using System.Runtime.InteropServices;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfPipeFileHeader
    {
        public readonly ulong Magic;
        public readonly ulong Size;
    }
}