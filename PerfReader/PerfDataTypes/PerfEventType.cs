namespace PerfGcCollector.PerfDataTypes
{
    public enum PerfEventType : uint
    {
        EndOfFile = 0,
        RecordMmap = 1,
        LostEvents = 2,
        RecordComm = 3,
        RecordExit = 4,
        RecordFork = 7,
        RecordSample = 9,
        RecordMmap2 = 10,
        RecordKSymbol = 17,
        RecordBpfEvent = 18,
        RecordHeaderAttr = 64,
        RecordFinishedRound = 68,
        RecordThreadMap = 73,
        RecordCpuMap = 74,
        RecordEventUpdate = 78,
        RecordTimeConv = 79,
        RecordHeaderFeature = 80
    }
}