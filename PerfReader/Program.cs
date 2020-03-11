using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using PerfReader;

namespace PerfGcCollector
{
    class Program
    {
        public const ulong MagicNumber = 0x32454c4946524550;

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Missing pid");
                return;
            }

            if (!int.TryParse(args[0], out var pid))
            {
                Console.WriteLine("Invalid pid");
                return;
            }

            var map = new Dictionary<ulong, string>();

            var perfMapFile = $"/tmp/perf-{pid}.map";

            if (File.Exists(perfMapFile))
            {
                foreach (var line in File.ReadLines(perfMapFile))
                {
                    var values = line.Split(' ', 3);

                    map.Add(Convert.ToUInt64(values.First(), 16), values.Last());
                }
            }

            Console.WriteLine($"Loaded {map.Count} symbols from {perfMapFile}");

            var symbols = new SortedDictionary<ulong, string>();

            using var input = Console.OpenStandardInput();

            var fileHeader = input.Read<PerfPipeFileHeader>();

            if (fileHeader.Magic != MagicNumber)
            {
                Console.WriteLine($"Magic number mismatch. Expected {MagicNumber:x2}, found {fileHeader.Magic:x2}");
                return;
            }

            bool endOfFile = false;

            long expectedPosition = 0;
            long currentType = 0;

            if (input.CanSeek)
            {
                expectedPosition = input.Position;
            }

            PerfRecordIndexes indexes = null;

            while (!endOfFile)
            {
                if (input.CanSeek)
                {
                    if (input.Position != expectedPosition)
                    {
                        Console.WriteLine("Mismatch after reading type " + currentType);
                    }
                }

                var header = input.Read<PerfEventHeader>();

                if (input.CanSeek)
                {
                    expectedPosition = input.Position + header.GetRemainingBytes();
                    currentType = header.Type;
                }

                switch (header.Type)
                {
                    case 0:
                        endOfFile = true;
                        break;

                    case 1:
                        var perfRecordMmap = new PerfRecordMmap(input, header);

                        if (perfRecordMmap.Pid == pid)
                        {
                            var filename = ReadString(perfRecordMmap.Filename);

                            symbols.Add(perfRecordMmap.Addr, filename);

                            ReadSymbols(filename, perfRecordMmap.Addr, symbols);
                        }

                        break;

                    case 3:
                        // PERF_RECORD_COMM
                        var perfRecordComm = new PerfRecordComm(input, header);

                        //Console.WriteLine($"{Encoding.ASCII.GetString(perfRecordComm.Comm)}:{perfRecordComm.Pid}");

                        //input.Skip(header.GetRemainingBytes());
                        break;

                    case 4:
                        // PERF_RECORD_EXIT
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 7:
                        //var perfRecordFork = new PerfRecordFork(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 9:
                        var sample = new PerfRecordSample(input, header, indexes);

                        if (sample.Pid == pid)
                        {
                            Console.WriteLine("Sample:");

                            foreach (var frame in sample.Callchain)
                            {
                                string symbol;

                                if (!map.TryGetValue(frame, out symbol))
                                {
                                    symbol = "UNKNOWN";

                                    foreach (var kvp in symbols)
                                    {
                                        if (kvp.Key < frame)
                                        {
                                            symbol = kvp.Value;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                Console.WriteLine(symbol);
                            }
                        }

                        break;

                    case 10:
                        var perfRecordMmap2 = new PerfRecordMmap2(input, header);

                        if (perfRecordMmap2.Pid == pid)
                        {
                            var filename = ReadString(perfRecordMmap2.filename);

                            symbols.Add(perfRecordMmap2.Addr, filename);

                            Console.WriteLine("Attempting to extract symbols for " + filename);
                            
                            ReadSymbols(filename, perfRecordMmap2.Addr, symbols);
                        }

                        break;

                    case 17:
                        //var perfRecordKSymbol = new PerfRecordKSymbol(input, header);
                        //Console.WriteLine("KSymbol: " + Encoding.ASCII.GetString(perfRecordKSymbol.Name));
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 18:
                        //var perfRecordBpfEvent = new PerfRecordBpfEvent(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 64:
                        var perfRecordHeaderAttr = PerfRecordHeaderAttr.Read(input, header);

                        foreach (ulong value in Enum.GetValues(typeof(PerfEventSampleFormat)))
                        {
                            if (((ulong) perfRecordHeaderAttr.Attr.SampleType & value) == value)
                            {
                                Console.WriteLine((PerfEventSampleFormat) value);
                            }
                        }

                        indexes = new PerfRecordIndexes(perfRecordHeaderAttr.Attr.SampleType);

                        break;

                    case 68:
                        // perf_record_finished_round
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 73:
                        //var perfRecordThreadMap = PerfRecordThreadMap.Read(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 74:
                        //var perfRecordCpuMap = new PerfRecordCpuMap(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 78:
                        //var perfRecordEventUpdate = PerfRecordEventUpdate.Read(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 79:
                        //var perfRecordTimeConv = PerfRecordTimeConv.Read(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case 80:
                        //var perfRecordHeaderFeature = PerfRecordHeaderFeature.Read(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    default:
                        throw new NotSupportedException("Header not supported: " + header.Type);
                }
            }
        }

        private static string ReadString(byte[] data)
        {
            int length = 0;

            for (length = 0; length < data.Length; length++)
            {
                if (data[length] == 0x0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(data.AsSpan(0, length));
        }

        private static void ReadSymbols(string filename, ulong baseAddress, SortedDictionary<ulong, string> symbols)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("{0} does not exist", filename);
                return;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "nm",
                Arguments = $"-C --defined-only -a -v {filename}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                var values = line.Split(' ', 3);

                if (values[1] == "U")
                {
                    continue;
                }

                try
                {
                    if (values[0].Trim().Length == 0)
                    {
                        continue;
                    }

                    var addr = Convert.ToUInt64(values[0], 16);

                    symbols[baseAddress + addr] = values[2];
                }
                catch (FormatException)
                {
                    continue;
                }
            }
        }
    }

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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordHeaderAttr
    {
        public readonly PerfEventHeader Header;
        public readonly PerfEventAttr Attr;
        public readonly ulong[] Id;

        public PerfRecordHeaderAttr(PerfEventHeader header, PerfEventAttr attr, ulong[] id)
        {
            Header = header;
            Attr = attr;
            Id = id;
        }

        public static unsafe PerfRecordHeaderAttr Read(Stream stream, PerfEventHeader header)
        {
            var remainingBytes = header.GetRemainingBytes() - sizeof(PerfEventAttr);

            var attr = stream.Read<PerfEventAttr>();

            return new PerfRecordHeaderAttr(header, attr, stream.ReadArray<ulong>(remainingBytes));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfEventHeader
    {
        public readonly uint Type;
        public readonly ushort Misc;
        public readonly ushort Size;

        public unsafe int GetRemainingBytes()
        {
            return Size - sizeof(PerfEventHeader);
        }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfPipeFileHeader
    {
        public readonly ulong Magic;
        public readonly ulong Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordHeaderFeature
    {
        public readonly PerfEventHeader Header;
        public readonly ulong FeatureId;
        public readonly byte[] Data;

        public PerfRecordHeaderFeature(PerfEventHeader header, ulong featureId, byte[] data)
        {
            Header = header;
            FeatureId = featureId;
            Data = data;
        }

        public static PerfRecordHeaderFeature Read(Stream stream, PerfEventHeader header)
        {
            var featureId = stream.Read<ulong>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong);

            return new PerfRecordHeaderFeature(header, featureId, stream.ReadArray<byte>(remainingBytes));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordTimeConv
    {
        public readonly PerfEventHeader Header;
        public readonly ulong TimeShift;
        public readonly ulong TimeMult;
        public readonly ulong TimeZero;

        public PerfRecordTimeConv(PerfEventHeader header, ulong timeShift, ulong timeMult, ulong timeZero)
        {
            Header = header;
            TimeShift = timeShift;
            TimeMult = timeMult;
            TimeZero = timeZero;
        }

        public static PerfRecordTimeConv Read(Stream stream, PerfEventHeader header)
        {
            return new PerfRecordTimeConv(header, stream.Read<ulong>(), stream.Read<ulong>(), stream.Read<ulong>());
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordEventUpdate
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Type;
        public readonly ulong Id;
        public readonly byte[] Data;

        public PerfRecordEventUpdate(PerfEventHeader header, ulong type, ulong id, byte[] data)
        {
            Header = header;
            Type = type;
            Id = id;
            Data = data;
        }

        public static PerfRecordEventUpdate Read(Stream stream, PerfEventHeader header)
        {
            var type = stream.Read<ulong>();
            var id = stream.Read<ulong>();

            var remainingBytes = header.GetRemainingBytes() - (sizeof(ulong) * 2);

            return new PerfRecordEventUpdate(header, type, id, stream.ReadArray<byte>(remainingBytes));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordThreadMap
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Nr;
        public readonly PerfRecordThreadMapEntry[] Entries;

        public PerfRecordThreadMap(PerfEventHeader header, ulong nr, PerfRecordThreadMapEntry[] entries)
        {
            Header = header;
            Nr = nr;
            Entries = entries;
        }

        public static PerfRecordThreadMap Read(Stream stream, PerfEventHeader header)
        {
            var nr = stream.Read<ulong>();
            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong);
            //var entries = stream.ReadArray<PerfRecordThreadMapEntry>(remainingBytes);
            var entries = new PerfRecordThreadMapEntry[nr];

            for (ulong i = 0; i < nr; i++)
            {
                entries[i] = PerfRecordThreadMapEntry.Read(stream);
            }

            return new PerfRecordThreadMap(header, nr, entries);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PerfRecordThreadMapEntry
    {
        public readonly ulong Pid;
        public readonly byte[] Comm;

        public PerfRecordThreadMapEntry(ulong pid, byte[] comm)
        {
            Pid = pid;
            Comm = comm;
        }

        public static PerfRecordThreadMapEntry Read(Stream stream)
        {
            var pid = stream.Read<ulong>();
            var comm = stream.ReadArray<byte>(16);

            return new PerfRecordThreadMapEntry(pid, comm);
        }
    }

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

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //public unsafe struct PerfRecordThreadMapEntry
    //{
    //    public readonly ulong Pid;
    //    public fixed byte Comm[16];
    //}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordKSymbol
    {
        public readonly PerfEventHeader Header;
        public readonly ulong Addr;
        public readonly uint Len;
        public readonly ushort KsymType;
        public readonly ushort Flags;

        public readonly byte[] Name;
        //public readonly SampleId SampleId;

        public unsafe PerfRecordKSymbol(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Addr = stream.Read<ulong>();
            Len = stream.Read<uint>();
            KsymType = stream.Read<ushort>();
            Flags = stream.Read<ushort>();

            var remainingBytes = header.GetRemainingBytes() - sizeof(ulong) - sizeof(uint) - (sizeof(ushort) * 2);
            //var remainingBytes = header.GetRemainingBytes() - sizeof(ulong) - sizeof(uint) - (sizeof(ushort) * 2) - sizeof(SampleId);

            Name = stream.ReadArray<byte>(remainingBytes);
            //SampleId = stream.Read<SampleId>();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct SampleId
    {
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly ulong Time;
        public readonly ulong Id;
        public readonly ulong StreamId;
        public readonly uint Cpu;
        public readonly uint Res;
        public readonly ulong Identifier;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordBpfEvent
    {
        public readonly PerfEventHeader Header;
        public readonly PerfBpfEventType Type;
        public readonly ushort Flags;
        public readonly uint Id;

        public readonly byte[] Tag;
        //public readonly SampleId SampleId;

        public unsafe PerfRecordBpfEvent(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Type = stream.Read<PerfBpfEventType>();
            Flags = stream.Read<ushort>();
            Id = stream.Read<uint>();

            var remainingBytes =
                header.GetRemainingBytes() - sizeof(PerfBpfEventType) - sizeof(ushort) -
                sizeof(uint); // - sizeof(SampleId);
            //var remainingBytes = header.GetRemainingBytes() - sizeof(PerfBpfEventType) - sizeof(ushort) - sizeof(uint) - sizeof(SampleId);
            Tag = stream.ReadArray<byte>(remainingBytes);
            //SampleId = stream.Read<SampleId>();
        }
    }


    public enum PerfBpfEventType : ushort
    {
        PERF_BPF_EVENT_UNKNOWN = 0,
        PERF_BPF_EVENT_PROG_LOAD = 1,
        PERF_BPF_EVENT_PROG_UNLOAD = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordFork
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Ppid;
        public readonly uint Tid;
        public readonly uint Ptid;

        public readonly ulong Time;
        //public readonly SampleId SampleId;

        public PerfRecordFork(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Ppid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Ptid = stream.Read<uint>();
            Time = stream.Read<ulong>();
            //SampleId = stream.Read<SampleId>();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordMmap
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly ulong Addr;
        public readonly ulong Len;
        public readonly ulong Pgoff;
        public readonly byte[] Filename;

        public PerfRecordMmap(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Addr = stream.Read<ulong>();
            Len = stream.Read<ulong>();
            Pgoff = stream.Read<ulong>();
            Filename = stream.ReadArray<byte>(header.GetRemainingBytes() - sizeof(uint) * 2 - sizeof(ulong) * 3);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordMmap2
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly ulong Addr;
        public readonly ulong Len;
        public readonly ulong Pgoff;
        public readonly uint Maj;
        public readonly uint Min;
        public readonly ulong Ino;
        public readonly ulong Ino_generation;
        public readonly uint Prot;
        public readonly uint Flags;

        public readonly byte[] filename;
        //public readonly SampleId SampleId;

        public unsafe PerfRecordMmap2(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Addr = stream.Read<ulong>();
            Len = stream.Read<ulong>();
            Pgoff = stream.Read<ulong>();
            Maj = stream.Read<uint>();
            Min = stream.Read<uint>();
            Ino = stream.Read<ulong>();
            Ino_generation = stream.Read<ulong>();
            Prot = stream.Read<uint>();
            Flags = stream.Read<uint>();

            var remainingSize = header.GetRemainingBytes() - (sizeof(uint) * 6) - (sizeof(ulong) * 5);
            //var remainingSize = header.GetRemainingBytes() - (sizeof(uint) * 6) - (sizeof(ulong) * 5) - sizeof(SampleId);
            filename = stream.ReadArray<byte>(remainingSize);
            //SampleId = stream.Read<SampleId>();
        }
    }

    public enum PerfEventSampleFormat : ulong
    {
        PERF_SAMPLE_IP = 1U << 0,
        PERF_SAMPLE_TID = 1U << 1,
        PERF_SAMPLE_TIME = 1U << 2,
        PERF_SAMPLE_ADDR = 1U << 3,
        PERF_SAMPLE_READ = 1U << 4,
        PERF_SAMPLE_CALLCHAIN = 1U << 5,
        PERF_SAMPLE_ID = 1U << 6,
        PERF_SAMPLE_CPU = 1U << 7,
        PERF_SAMPLE_PERIOD = 1U << 8,
        PERF_SAMPLE_STREAM_ID = 1U << 9,
        PERF_SAMPLE_RAW = 1U << 10,
        PERF_SAMPLE_BRANCH_STACK = 1U << 11,
        PERF_SAMPLE_REGS_USER = 1U << 12,
        PERF_SAMPLE_STACK_USER = 1U << 13,
        PERF_SAMPLE_WEIGHT = 1U << 14,
        PERF_SAMPLE_DATA_SRC = 1U << 15,
        PERF_SAMPLE_IDENTIFIER = 1U << 16,
        PERF_SAMPLE_TRANSACTION = 1U << 17,
        PERF_SAMPLE_REGS_INTR = 1U << 18,
        PERF_SAMPLE_PHYS_ADDR = 1U << 19,
        PERF_SAMPLE_AUX = 1U << 20,

        PERF_SAMPLE_MAX = 1U << 21, /* non-ABI */

        __PERF_SAMPLE_CALLCHAIN_EARLY = 1U << 63 /* non-ABI; internal use */
    }

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

        public uint Pid => (uint) (Array[_indexes.Pid] & LOW_MASK);

        public uint Tid => (uint) (Array[_indexes.Tid] >> 32);

        public ulong Time => Array[_indexes.Time];

        public ulong Addr => Array[_indexes.Addr];

        public ulong Id => Array[_indexes.Id];

        public ulong StreamId => Array[_indexes.StreamId];

        public uint Cpu => (uint) (Array[_indexes.Cpu] & LOW_MASK);

        public uint Res => (uint) (Array[_indexes.Res] >> 32);

        public ulong Period => Array[_indexes.Period];

        public Span<ulong> Callchain
        {
            get
            {
                var count = Array[_indexes.Callchain];

                return Array.AsSpan().Slice(_indexes.Callchain, (int) count);
            }
        }
    }

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

    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    //public readonly struct PerfRecordSample
    //{
    //    public readonly PerfEventHeader Header;
    //    public ulong Id;
    //    public ulong Ip;
    //    public uint Pid;
    //    public uint Tid;
    //    public ulong Time;
    //    public ulong Addr;
    //    public ulong Id;
    //    public ulong StreamId;
    //    public uint Cpu;
    //    public uint Res;
    //    public ulong Period;
    //    public ReadFormat Values;
    //    public ulong NrIps;
    //    public ulong[] Ips;
    //    public uint Size;
    //    public byte[] Data;
    //    public ulong NrLbr;
    //    public PerfSampleBranchStack[] Lbr;
    //    public ulong Abi;
    //    public 


    //}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct PerfRecordComm
    {
        public readonly PerfEventHeader Header;
        public readonly uint Pid;
        public readonly uint Tid;
        public readonly byte[] Comm;

        public PerfRecordComm(Stream stream, PerfEventHeader header)
        {
            Header = header;
            Pid = stream.Read<uint>();
            Tid = stream.Read<uint>();
            Comm = stream.ReadArray<byte>(header.GetRemainingBytes() - sizeof(uint) * 2);
        }
    }
}