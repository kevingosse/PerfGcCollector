using System;

namespace PerfGcCollector.PerfDataTypes
{
    public class PerfRecordIndexes
    {
        public PerfRecordIndexes(PerfEventSampleFormat format)
        {
            int index = 0;

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_IDENTIFIER))
            {
                Identifier = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_IP))
            {
                Ip = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_TID))
            {
                Pid = index;
                Tid = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_TIME))
            {
                Time = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_ADDR))
            {
                Addr = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_ID))
            {
                Id = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_STREAM_ID))
            {
                StreamId = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_CPU))
            {
                Cpu = index;
                Res = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_PERIOD))
            {
                Period = index;
                index++;
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_READ))
            {
                // TODO: Format is dynamic :(
                throw new NotSupportedException("PERF_SAMPLE_READ isn't supported");
            }

            if (format.HasFlag(PerfEventSampleFormat.PERF_SAMPLE_CALLCHAIN))
            {
                Callchain = index;
            }
        }

        public int Identifier { get; }
        public int Ip { get; }
        public int Pid { get; }
        public int Tid { get; }
        public int Time { get; }
        public int Addr { get; }
        public int Id { get; }
        public int StreamId { get; }
        public int Cpu { get; }
        public int Res { get; }
        public int Period { get; }
        public int Read { get; }
        public int Callchain { get; }
    }
}