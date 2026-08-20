/*
 * SZDDComp: Microsoft "compress.exe/expand.exe" compatible compressor
 *
 * The original copyright information for SZDDComp could not be fully recovered.
 * Therefore, all individuals and organizations identified as possible contributors
 * to the source code have been acknowledged below.
 *
 * Copyright (c) 2000 Martin Hinner <mhi@penguin.cz>
 * Algorithm & data structures by M. Winterhoff <100326.2776@compuserve.com>
 * C# port Copyright (c) 2011 Francis Gagné <fragag@hotmail.com>
 * GitHub: @MainMemory - http://mm.reimuhakurei.net/, https://github.com/MainMemory
 * Sonic Retro Team: https://sonicretro.org/, https://github.com/sonicretro
 *
 * This source code is currently used and maintained as part of the
 * WelsonJS open-source project.
 *
 * Copyright (c) 2026 Namhyeon Go <gnh1201@catswords.re.kr>
 * https://github.com/gnh1201/welsonjs
 *
 * The SZDDComp source code was originally distributed under the
 * GNU General Public License version 2 (GPLv2).
 */
using System;
using System.IO;

namespace WelsonJS.ManagedObject.External
{
    public static class SZDDComp
    {
        private class Buffer
        {
            private const int DadOffset = 1;

            private const int LeftSonOffset = 4097;

            private const int RightSonOffset = 8193;

            private const int RootOffset = 12289;

            private int pos;

            private byte[] head = new byte[4112];

            private int[] node = new int[12545];

            public int Position => pos;

            public Buffer()
            {
                for (int i = 0; i < 256; i++)
                {
                    SetRoot(i, -1);
                }
                for (int j = -1; j < 4096; j++)
                {
                    SetDad(j, -1);
                }
            }

            public int Insert(int i, int run)
            {
                int num = 0;
                int val2;
                int val = (val2 = 1);
                int num2 = 2;
                int num3 = 12289 + head[i];
                SetLeftSon(i, -1);
                SetRightSon(i, -1);
                int num4;
                while ((num4 = node[num3]) != -1)
                {
                    int j;
                    for (j = Math.Min(val, val2); j < run; j++)
                    {
                        if ((num = head[num4 + j] - head[i + j]) != 0)
                        {
                            break;
                        }
                    }
                    if (j > num2)
                    {
                        num2 = j;
                        pos = num4;
                    }
                    if (num < 0)
                    {
                        num3 = 4097 + num4;
                        val = j;
                        continue;
                    }
                    if (num > 0)
                    {
                        num3 = 8193 + num4;
                        val2 = j;
                        continue;
                    }
                    SetDad(num4, -1);
                    SetDad(GetLeftSon(num4), 4097 + i);
                    SetDad(GetRightSon(num4), 8193 + i);
                    SetLeftSon(i, GetLeftSon(num4));
                    SetRightSon(i, GetRightSon(num4));
                    break;
                }
                SetDad(i, num3);
                node[num3] = i;
                return num2;
            }

            public void Delete(int z)
            {
                if (GetDad(z) == -1)
                {
                    return;
                }
                int num;
                if (GetRightSon(z) == -1)
                {
                    num = GetLeftSon(z);
                }
                else if (GetLeftSon(z) == -1)
                {
                    num = GetRightSon(z);
                }
                else
                {
                    num = GetLeftSon(z);
                    if (GetRightSon(num) != -1)
                    {
                        do
                        {
                            num = GetRightSon(num);
                        }
                        while (GetRightSon(num) != -1);
                        node[GetDad(num)] = GetLeftSon(num);
                        SetDad(GetLeftSon(num), GetDad(num));
                        SetLeftSon(num, GetLeftSon(z));
                        SetDad(GetLeftSon(z), 4097 + num);
                    }
                    SetRightSon(num, GetRightSon(z));
                    SetDad(GetRightSon(z), 8193 + num);
                }
                SetDad(num, GetDad(z));
                node[GetDad(z)] = num;
                SetDad(z, -1);
            }

            public byte GetHead(int index)
            {
                return head[index];
            }

            public void SetHead(int index, byte value)
            {
                head[index] = value;
            }

            private int GetDad(int index)
            {
                return node[1 + index];
            }

            private int GetLeftSon(int index)
            {
                return node[4097 + index];
            }

            private int GetRightSon(int index)
            {
                return node[8193 + index];
            }

            private void SetDad(int index, int value)
            {
                node[1 + index] = value;
            }

            private void SetLeftSon(int index, int value)
            {
                node[4097 + index] = value;
            }

            private void SetRightSon(int index, int value)
            {
                node[8193 + index] = value;
            }

            private void SetRoot(int index, int value)
            {
                node[12289 + index] = value;
            }
        }

        private const int N = 4096;

        private const int F = 16;

        private const int THRESHOLD = 3;

        private const int NIL = -1;

        private static readonly byte[] Magic = new byte[10] { 83, 90, 68, 68, 136, 240, 39, 51, 65, 0 };

        private static void Encode(Stream input, Stream output)
        {
            byte[] array = new byte[17];
            byte[] array2 = new byte[8];
            Buffer buffer = new Buffer();
            output.Write(Magic, 0, Magic.Length);
            Int32ToBytesLE(checked((int)(input.Length - input.Position)), array2);
            output.Write(array2, 0, 4);
            int num2;
            int num = (num2 = 1);
            array[0] = 0;
            int num3 = 4064;
            int i;
            for (i = 0; i < 16; i++)
            {
                int num4;
                if ((num4 = input.ReadByte()) == -1)
                {
                    break;
                }
                buffer.SetHead(num3 + 16, (byte)num4);
                num3 = (num3 + 1) & 0xFFF;
            }
            int num5 = i;
            do
            {
                int num4 = input.ReadByte();
                if (num3 >= 4080)
                {
                    buffer.Delete(num3 + 16 - 4096);
                    buffer.SetHead(num3 + 16, (byte)num4);
                    buffer.SetHead(num3 + 16 - 4096, (byte)num4);
                }
                else
                {
                    buffer.Delete(num3 + 16);
                    buffer.SetHead(num3 + 16, (byte)num4);
                }
                int num6 = buffer.Insert(num3, num5);
                if (num4 == -1)
                {
                    num5--;
                    i--;
                }
                if (i++ >= num5)
                {
                    if (num6 >= 3)
                    {
                        array[num++] = (byte)buffer.Position;
                        array[num++] = (byte)(((buffer.Position >> 4) & 0xF0) + (num6 - 3));
                        i -= num6;
                    }
                    else
                    {
                        array[0] |= (byte)num2;
                        array[num++] = buffer.GetHead(num3);
                        i--;
                    }
                    num2 += num2;
                    if ((num2 & 0xFF) == 0)
                    {
                        output.Write(array, 0, num);
                        num = (num2 = 1);
                        array[0] = 0;
                    }
                }
                num3 = (num3 + 1) & 0xFFF;
            }
            while (i > 0);
            if (num > 1)
            {
                output.Write(array, 0, num);
            }
        }

        private static void Int32ToBytesLE(int value, byte[] bytes)
        {
            bytes[0] = (byte)value;
            bytes[1] = (byte)(value >> 8);
            bytes[2] = (byte)(value >> 16);
            bytes[3] = (byte)(value >> 24);
        }

        private static int Decode(Stream infile, Stream outfile)
        {
            byte[] array = new byte[4];
            if (infile.Read(array, 0, 4) == -1)
            {
                throw new Exception();
            }
            switch (BitConverter.ToUInt32(array, 0))
            {
                case 1145330259u:
                    {
                        if (infile.Read(array, 0, 4) == -1)
                        {
                            throw new Exception();
                        }
                        uint num = BitConverter.ToUInt32(array, 0);
                        if (infile.Read(array, 0, 2) == -1)
                        {
                            throw new Exception();
                        }
                        BitConverter.ToUInt16(array, 0);
                        if (infile.Read(array, 0, 4) == -1)
                        {
                            throw new Exception();
                        }
                        BitConverter.ToUInt32(array, 0);
                        if ((ulong)num != 858255496)
                        {
                            throw new Exception("This is not a MS-compressed file!");
                        }
                        byte[] array2 = new byte[4096];
                        for (int i = 0; i < array2.Length; i++)
                        {
                            array2[i] = 32;
                        }
                        int num3 = 4080;
                        while (true)
                        {
                            int num4 = infile.ReadByte();
                            if (num4 == -1)
                            {
                                break;
                            }
                            int num5 = 1;
                            while ((num5 & 0xFF) != 0)
                            {
                                if ((num4 & num5) == 0)
                                {
                                    int num6 = infile.ReadByte();
                                    if (num6 == -1)
                                    {
                                        break;
                                    }
                                    int num7 = infile.ReadByte();
                                    num6 += (num7 & 0xF0) << 4;
                                    num7 = (num7 & 0xF) + 3;
                                    while (num7-- != 0)
                                    {
                                        array2[num3] = array2[num6];
                                        outfile.WriteByte(array2[num3]);
                                        num6++;
                                        num6 %= 4096;
                                        num3++;
                                        num3 %= 4096;
                                    }
                                }
                                else
                                {
                                    int num8 = infile.ReadByte();
                                    if (num8 == -1)
                                    {
                                        break;
                                    }
                                    array2[num3] = (byte)num8;
                                    outfile.WriteByte(array2[num3]);
                                    num3++;
                                    num3 %= 4096;
                                }
                                num5 <<= 1;
                            }
                        }
                        return 0;
                    }
                case 1245796171u:
                    {
                        if (infile.Read(array, 0, 4) == -1)
                        {
                            throw new Exception();
                        }
                        uint num = BitConverter.ToUInt32(array, 0);
                        if (infile.Read(array, 0, 4) == -1)
                        {
                            throw new Exception();
                        }
                        uint num2 = BitConverter.ToUInt32(array, 0);
                        if (infile.Read(array, 0, 2) == -1)
                        {
                            throw new Exception();
                        }
                        BitConverter.ToUInt16(array, 0);
                        if ((ulong)num != 3509055624u || (ulong)num2 != 1179651)
                        {
                            throw new Exception("This is not a MS-compressed file!");
                        }
                        throw new Exception("Unsupported version 6.22!");
                    }
                default:
                    throw new Exception("This is not a MS-compressed file!");
            }
        }

        public static byte[] Decompress(string sourceFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Decompress(input, memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static void Decompress(byte[] sourceData, string destinationFilePath)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            using (FileStream output = File.Create(destinationFilePath))
            {
                Decompress(input, output);

            }
        }

        public static void Decompress(string sourceFilePath, string destinationFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            using (FileStream output = File.Create(destinationFilePath))
            {
                Decompress(input, output);
            }
        }

        public static byte[] Decompress(byte[] sourceData)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Decompress(input, memoryStream);
                return memoryStream.ToArray();
            }
                
        }

        public static void Decompress(Stream input, Stream output)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            Decode(input, output);
        }

        public static byte[] Compress(string sourceFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Compress(input, memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static void Compress(byte[] sourceData, string destinationFilePath)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            using (FileStream output = File.Create(destinationFilePath))
            {
                Compress(input, output);
            }
        }

        public static void Compress(string sourceFilePath, string destinationFilePath)
        {
            using (FileStream input = File.OpenRead(sourceFilePath))
            using (FileStream output = File.Create(destinationFilePath))
            {
                Compress(input, output);
            }
        }

        public static byte[] Compress(byte[] sourceData)
        {
            using (MemoryStream input = new MemoryStream(sourceData))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Compress(input, memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static void Compress(Stream input, Stream output)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            Encode(input, output);
        }
    }
}
