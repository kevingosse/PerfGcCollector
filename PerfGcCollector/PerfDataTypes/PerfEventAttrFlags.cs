using System;

namespace PerfGcCollector.PerfDataTypes
{
    [Flags]
    public enum PerfEventAttrFlags : ulong
    {
        Disabled,
        Inherit,
        Pinned,
        Exclusive,
        ExcludeUser,
        ExcludeKernel,
        ExcludeHv,
        ExcludeIdle,
        Mmap,
        Comm,
        Freq,
        InheritStat,
        EnableOnExec,
        Task,
        Watermark,
        ConstantSkid,
        ZeroSkid,
        MmapData,
        SampleIdAll,
        ExcludeHost,
        ExcludeGuest,
        ExcludeCallchainKernel,
        ExcludeCallchainUser,
        Mmap2,
        CommExec,
        UseClockid,
        ContextSwitch,
        WriteBackward,
        Namespaces,
        Ksymbol,
        BpfEvent,
        AuxOutput
    }
}