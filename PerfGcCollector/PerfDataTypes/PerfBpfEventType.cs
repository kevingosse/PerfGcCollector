namespace PerfGcCollector.PerfDataTypes
{
    public enum PerfBpfEventType : ushort
    {
        PERF_BPF_EVENT_UNKNOWN = 0,
        PERF_BPF_EVENT_PROG_LOAD = 1,
        PERF_BPF_EVENT_PROG_UNLOAD = 2,
    }
}