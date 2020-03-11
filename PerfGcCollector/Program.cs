using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PerfGcCollector.PerfDataTypes;
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
            var symbols = new SortedDictionary<ulong, string>();

            var perfMapFile = $"/tmp/perf-{pid}.map";

            if (File.Exists(perfMapFile))
            {
                foreach (var line in File.ReadLines(perfMapFile))
                {
                    var values = line.Split(' ', 3);

                    map.Add(Convert.ToUInt64(values.First(), 16), values.Last());
                    symbols.Add(Convert.ToUInt64(values.First(), 16), values.Last());
                }
            }

            Console.WriteLine($"Loaded {map.Count} symbols from {perfMapFile}");

            using var input = Console.OpenStandardInput();

            var fileHeader = input.Read<PerfPipeFileHeader>();

            if (fileHeader.Magic != MagicNumber)
            {
                Console.WriteLine($"Magic number mismatch. Expected {MagicNumber:x2}, found {fileHeader.Magic:x2}");
                return;
            }

            bool endOfFile = false;

            PerfRecordIndexes indexes = null;

            while (!endOfFile)
            {
                var header = input.Read<PerfEventHeader>();

                switch (header.Type)
                {
                    case PerfEventType.EndOfFile:
                        endOfFile = true;
                        break;

                    case PerfEventType.RecordMmap:
                        var perfRecordMmap = new PerfRecordMmap(input, header);

                        if (perfRecordMmap.Pid == pid)
                        {
                            var filename = ReadString(perfRecordMmap.Filename);

                            symbols.Add(perfRecordMmap.Addr, filename);

                            ReadSymbols(filename, perfRecordMmap.Addr, perfRecordMmap.Pgoff, perfRecordMmap.Len, symbols);
                        }

                        break;

                    case PerfEventType.LostEvents:
                        Console.WriteLine("Lost events");
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordComm:
                        //var perfRecordComm = new PerfRecordComm(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordExit:
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordFork:
                        //var perfRecordFork = new PerfRecordFork(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordSample:
                        var sample = new PerfRecordSample(input, header, indexes);

                        if (sample.Pid == pid)
                        {
                            Console.WriteLine("Sample:");

                            foreach (var frame in sample.Callchain)
                            {
                                if (!map.TryGetValue(frame, out var symbol))
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

                                Console.WriteLine($"{frame:x2} {symbol}");
                            }
                        }

                        break;

                    case PerfEventType.RecordMmap2:
                        var perfRecordMmap2 = new PerfRecordMmap2(input, header);

                        if (perfRecordMmap2.Pid == pid)
                        {
                            var filename = ReadString(perfRecordMmap2.filename);

                            symbols.Add(perfRecordMmap2.Addr, filename);

                            ReadSymbols(filename, perfRecordMmap2.Addr, perfRecordMmap2.Pgoff, perfRecordMmap2.Len, symbols);
                        }

                        break;

                    case PerfEventType.RecordKSymbol:
                        //var perfRecordKSymbol = new PerfRecordKSymbol(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordBpfEvent:
                        //var perfRecordBpfEvent = new PerfRecordBpfEvent(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordHeaderAttr:
                        var perfRecordHeaderAttr = new PerfRecordHeaderAttr(input, header);

                        foreach (ulong value in Enum.GetValues(typeof(PerfEventSampleFormat)))
                        {
                            if (((ulong)perfRecordHeaderAttr.Attr.SampleType & value) == value)
                            {
                                Console.WriteLine((PerfEventSampleFormat)value);
                            }
                        }

                        indexes = new PerfRecordIndexes(perfRecordHeaderAttr.Attr.SampleType);

                        break;

                    case PerfEventType.RecordFinishedRound:
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordThreadMap:
                        //var perfRecordThreadMap = new PerfRecordThreadMap(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordCpuMap:
                        //var perfRecordCpuMap = new PerfRecordCpuMap(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordEventUpdate:
                        //var perfRecordEventUpdate = new PerfRecordEventUpdate(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordTimeConv:
                        //var perfRecordTimeConv = new PerfRecordTimeConv(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    case PerfEventType.RecordHeaderFeature:
                        //var perfRecordHeaderFeature = new PerfRecordHeaderFeature(input, header);
                        input.Skip(header.GetRemainingBytes());
                        break;

                    default:
                        throw new NotSupportedException("Header not supported: " + header.Type);
                }
            }
        }

        private static string ReadString(byte[] data)
        {
            int length;

            for (length = 0; length < data.Length; length++)
            {
                if (data[length] == 0x0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(data.AsSpan(0, length));
        }

        private static void ReadSymbols(string filename, ulong baseAddress, ulong offset, ulong length, SortedDictionary<ulong, string> symbols)
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

            int count = 0;

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

                    if (addr >= offset && addr < (offset + length))
                    {
                        var destAddr = baseAddress + addr - offset;
                        symbols[destAddr] = values[2];

                        count++;
                    }
                }
                catch (FormatException)
                {
                }
            }

            Console.WriteLine($"{count} symbols loaded for {filename} at base address {baseAddress:x2}");
        }
    }
}