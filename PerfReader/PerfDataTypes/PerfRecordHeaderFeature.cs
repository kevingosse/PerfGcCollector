using System.IO;
using System.Runtime.InteropServices;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordHeaderFeature
    {
        public readonly PerfEventHeader Header;
        public readonly ulong FeatureId;
        public readonly byte[] Data;

        public PerfRecordHeaderFeature(Stream stream, PerfEventHeader header)
        {
            Header = header;
            FeatureId = stream.Read<ulong>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong);
            Data = stream.ReadArray<byte>(remainingBytes);
        }
    }
}