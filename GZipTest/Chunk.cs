using System;

namespace GZipTest
{
    public class Chunk
    {
        public Chunk(int size)
        {
            Size = size;
            UncompressedData = GC.AllocateUninitializedArray<byte>(size);
            CompressedData = GC.AllocateUninitializedArray<byte>((int)(size * 1.2));
        }

        public int Size { get; }

        public int Sequence { get; set; }

        public bool IsEof { get; set; }

        public byte[] UncompressedData { get; }

        public int UncompressedSize { get; set; }

        public byte[] CompressedData { get; }

        public int CompressedSize { get; set; }

        public void Reset()
        {
            IsEof = false;
            Sequence = -1;
            UncompressedSize = -1;
            CompressedSize = -1;
        }
    }
}