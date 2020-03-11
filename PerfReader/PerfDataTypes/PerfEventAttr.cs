using System.Runtime.InteropServices;

namespace PerfGcCollector.PerfDataTypes
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public readonly struct PerfEventAttr
    {
        [FieldOffset(0)] public readonly uint Type;

        [FieldOffset(4)] public readonly uint Size;

        [FieldOffset(8)] public readonly ulong Config;

        [FieldOffset(16)] public readonly ulong SamplePeriod;
        [FieldOffset(16)] public readonly ulong SampleFreq;

        [FieldOffset(24)] public readonly PerfEventSampleFormat SampleType;

        [FieldOffset(32)] public readonly ulong ReadFormat;

        [FieldOffset(40)] public readonly PerfEventAttrFlags Flags;

        [FieldOffset(48)] public readonly uint WakeupEvents;
        [FieldOffset(48)] public readonly uint WakeupWatermark;

        [FieldOffset(52)] public readonly uint BpType;

        [FieldOffset(56)] public readonly ulong BpAddr;
        [FieldOffset(56)] public readonly ulong KprobeFunc;
        [FieldOffset(56)] public readonly ulong UprobePath;
        [FieldOffset(56)] public readonly ulong Config1;

        [FieldOffset(64)] public readonly ulong BpLen;
        [FieldOffset(64)] public readonly ulong KprobeAddr;
        [FieldOffset(64)] public readonly ulong ProbeOffset;
        [FieldOffset(64)] public readonly ulong Config2;

        [FieldOffset(72)] public readonly ulong BranchSampleType;

        [FieldOffset(80)] public readonly ulong SampleRegsUser;

        [FieldOffset(88)] public readonly uint SampleStackUser;

        [FieldOffset(92)] public readonly int Clockid;

        [FieldOffset(96)] public readonly ulong SampleRegsIntr;

        [FieldOffset(104)] public readonly uint AuxWatermark;

        [FieldOffset(108)] public readonly ushort SampleMaxStack;

        [FieldOffset(110)] public readonly ushort Reserved2;

        [FieldOffset(112)] public readonly uint AuxSampleSize;

        [FieldOffset(116)] public readonly uint Reserved3;
    }
}