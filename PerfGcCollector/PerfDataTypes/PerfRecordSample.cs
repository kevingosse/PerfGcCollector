using System;
using System.IO;
using PerfReader;

namespace PerfGcCollector.PerfDataTypes
{
    public readonly struct PerfRecordSample
    {
        const long LOW_MASK = ((1L << 32) - 1);
        public readonly PerfEventHeader Header;
        public readonly ulong[] Array;

        private readonly PerfRecordIndexes _indexes;

        public PerfRecordSample(Stream stream, PerfEventHeader header, PerfRecordIndexes indexes)
        {
            Header = header;
            Array = stream.ReadArray<ulong>(header.GetRemainingBytes());
            _indexes = indexes;
        }

        public ulong Identifier => Array[_indexes.Identifier];
        public ulong Ip => Array[_indexes.Ip];

        public uint Pid => (uint)(Array[_indexes.Pid] & LOW_MASK);

        public uint Tid => (uint)(Array[_indexes.Tid] >> 32);

        public ulong Time => Array[_indexes.Time];

        public ulong Addr => Array[_indexes.Addr];

        public ulong Id => Array[_indexes.Id];

        public ulong StreamId => Array[_indexes.StreamId];

        public uint Cpu => (uint)(Array[_indexes.Cpu] & LOW_MASK);

        public uint Res => (uint)(Array[_indexes.Res] >> 32);

        public ulong Period => Array[_indexes.Period];

        public Span<ulong> Callchain
        {
            get
            {
                var count = Array[_indexes.Callchain];

                return Array.AsSpan().Slice(_indexes.Callchain, (int)count);
            }
        }
    }
}