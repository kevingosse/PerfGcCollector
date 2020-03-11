using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PerfReader
{
    internal static class StreamExtensions
    {
        public static unsafe T Read<T>(this Stream stream) where T : unmanaged
        {
            var size = sizeof(T);

            Span<byte> buffer = stackalloc byte[size];

            stream.ReadSpan(buffer);

            return MemoryMarshal.Read<T>(buffer);
        }

        public static bool ReadSpan(this Stream stream, Span<byte> buffer)
        {
            int index = 0;

            while (index < buffer.Length)
            {
                int read = stream.Read(buffer.Slice(index, buffer.Length - index));

                if (read == 0 && index == 0)
                {
                    throw new EndOfStreamException();
                }

                index += read;
            }

            return true;
        }

        public static T[] ReadArray<T>(this Stream stream, int size) where T : struct
        {
            Span<byte> buffer = stackalloc byte[size];

            stream.ReadSpan(buffer);

            return MemoryMarshal.Cast<byte, T>(buffer).ToArray();
        }

        public static void Skip(this Stream stream, int bytes)
        {
            Span<byte> buffer = stackalloc byte[bytes];

            stream.ReadSpan(buffer);
        }
    }
}