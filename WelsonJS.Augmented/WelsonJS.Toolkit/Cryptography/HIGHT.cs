// HIGHT.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// HIGHT(ISO/IEC 18033-3) cryptography algorithm implementation
// 
using System;

namespace WelsonJS.Cryptography
{
    public class HIGHT
    {
        private static readonly byte[] hightDelta = {
            0x5A,0x6D,0x36,0x1B,0x0D,0x06,0x03,0x41,
            0x60,0x30,0x18,0x4C,0x66,0x33,0x59,0x2C,
            0x56,0x2B,0x15,0x4A,0x65,0x72,0x39,0x1C,
            0x4E,0x67,0x73,0x79,0x3C,0x5E,0x6F,0x37,
            0x5B,0x2D,0x16,0x0B,0x05,0x42,0x21,0x50,
            0x28,0x54,0x2A,0x55,0x6A,0x75,0x7A,0x7D,
            0x3E,0x5F,0x2F,0x17,0x4B,0x25,0x52,0x29,
            0x14,0x0A,0x45,0x62,0x31,0x58,0x6C,0x76,
            0x3B,0x1D,0x0E,0x47,0x63,0x71,0x78,0x7C,
            0x7E,0x7F,0x3F,0x1F,0x0F,0x07,0x43,0x61,
            0x70,0x38,0x5C,0x6E,0x77,0x7B,0x3D,0x1E,
            0x4F,0x27,0x53,0x69,0x34,0x1A,0x4D,0x26,
            0x13,0x49,0x24,0x12,0x09,0x04,0x02,0x01,
            0x40,0x20,0x10,0x08,0x44,0x22,0x11,0x48,
            0x64,0x32,0x19,0x0C,0x46,0x23,0x51,0x68,
            0x74,0x3A,0x5D,0x2E,0x57,0x6B,0x35,0x5A
        };

        private static readonly byte[] hightF0 = {
            0x00,0x86,0x0D,0x8B,0x1A,0x9C,0x17,0x91,
            0x34,0xB2,0x39,0xBF,0x2E,0xA8,0x23,0xA5,
            0x68,0xEE,0x65,0xE3,0x72,0xF4,0x7F,0xF9,
            0x5C,0xDA,0x51,0xD7,0x46,0xC0,0x4B,0xCD,
            0xD0,0x56,0xDD,0x5B,0xCA,0x4C,0xC7,0x41,
            0xE4,0x62,0xE9,0x6F,0xFE,0x78,0xF3,0x75,
            0xB8,0x3E,0xB5,0x33,0xA2,0x24,0xAF,0x29,
            0x8C,0x0A,0x81,0x07,0x96,0x10,0x9B,0x1D,
            0xA1,0x27,0xAC,0x2A,0xBB,0x3D,0xB6,0x30,
            0x95,0x13,0x98,0x1E,0x8F,0x09,0x82,0x04,
            0xC9,0x4F,0xC4,0x42,0xD3,0x55,0xDE,0x58,
            0xFD,0x7B,0xF0,0x76,0xE7,0x61,0xEA,0x6C,
            0x71,0xF7,0x7C,0xFA,0x6B,0xED,0x66,0xE0,
            0x45,0xC3,0x48,0xCE,0x5F,0xD9,0x52,0xD4,
            0x19,0x9F,0x14,0x92,0x03,0x85,0x0E,0x88,
            0x2D,0xAB,0x20,0xA6,0x37,0xB1,0x3A,0xBC,
            0x43,0xC5,0x4E,0xC8,0x59,0xDF,0x54,0xD2,
            0x77,0xF1,0x7A,0xFC,0x6D,0xEB,0x60,0xE6,
            0x2B,0xAD,0x26,0xA0,0x31,0xB7,0x3C,0xBA,
            0x1F,0x99,0x12,0x94,0x05,0x83,0x08,0x8E,
            0x93,0x15,0x9E,0x18,0x89,0x0F,0x84,0x02,
            0xA7,0x21,0xAA,0x2C,0xBD,0x3B,0xB0,0x36,
            0xFB,0x7D,0xF6,0x70,0xE1,0x67,0xEC,0x6A,
            0xCF,0x49,0xC2,0x44,0xD5,0x53,0xD8,0x5E,
            0xE2,0x64,0xEF,0x69,0xF8,0x7E,0xF5,0x73,
            0xD6,0x50,0xDB,0x5D,0xCC,0x4A,0xC1,0x47,
            0x8A,0x0C,0x87,0x01,0x90,0x16,0x9D,0x1B,
            0xBE,0x38,0xB3,0x35,0xA4,0x22,0xA9,0x2F,
            0x32,0xB4,0x3F,0xB9,0x28,0xAE,0x25,0xA3,
            0x06,0x80,0x0B,0x8D,0x1C,0x9A,0x11,0x97,
            0x5A,0xDC,0x57,0xD1,0x40,0xC6,0x4D,0xCB,
            0x6E,0xE8,0x63,0xE5,0x74,0xF2,0x79,0xFF
        };

        private static readonly byte[] hightF1 = {
            0x00,0x58,0xB0,0xE8,0x61,0x39,0xD1,0x89,
            0xC2,0x9A,0x72,0x2A,0xA3,0xFB,0x13,0x4B,
            0x85,0xDD,0x35,0x6D,0xE4,0xBC,0x54,0x0C,
            0x47,0x1F,0xF7,0xAF,0x26,0x7E,0x96,0xCE,
            0x0B,0x53,0xBB,0xE3,0x6A,0x32,0xDA,0x82,
            0xC9,0x91,0x79,0x21,0xA8,0xF0,0x18,0x40,
            0x8E,0xD6,0x3E,0x66,0xEF,0xB7,0x5F,0x07,
            0x4C,0x14,0xFC,0xA4,0x2D,0x75,0x9D,0xC5,
            0x16,0x4E,0xA6,0xFE,0x77,0x2F,0xC7,0x9F,
            0xD4,0x8C,0x64,0x3C,0xB5,0xED,0x05,0x5D,
            0x93,0xCB,0x23,0x7B,0xF2,0xAA,0x42,0x1A,
            0x51,0x09,0xE1,0xB9,0x30,0x68,0x80,0xD8,
            0x1D,0x45,0xAD,0xF5,0x7C,0x24,0xCC,0x94,
            0xDF,0x87,0x6F,0x37,0xBE,0xE6,0x0E,0x56,
            0x98,0xC0,0x28,0x70,0xF9,0xA1,0x49,0x11,
            0x5A,0x02,0xEA,0xB2,0x3B,0x63,0x8B,0xD3,
            0x2C,0x74,0x9C,0xC4,0x4D,0x15,0xFD,0xA5,
            0xEE,0xB6,0x5E,0x06,0x8F,0xD7,0x3F,0x67,
            0xA9,0xF1,0x19,0x41,0xC8,0x90,0x78,0x20,
            0x6B,0x33,0xDB,0x83,0x0A,0x52,0xBA,0xE2,
            0x27,0x7F,0x97,0xCF,0x46,0x1E,0xF6,0xAE,
            0xE5,0xBD,0x55,0x0D,0x84,0xDC,0x34,0x6C,
            0xA2,0xFA,0x12,0x4A,0xC3,0x9B,0x73,0x2B,
            0x60,0x38,0xD0,0x88,0x01,0x59,0xB1,0xE9,
            0x3A,0x62,0x8A,0xD2,0x5B,0x03,0xEB,0xB3,
            0xF8,0xA0,0x48,0x10,0x99,0xC1,0x29,0x71,
            0xBF,0xE7,0x0F,0x57,0xDE,0x86,0x6E,0x36,
            0x7D,0x25,0xCD,0x95,0x1C,0x44,0xAC,0xF4,
            0x31,0x69,0x81,0xD9,0x50,0x08,0xE0,0xB8,
            0xF3,0xAB,0x43,0x1B,0x92,0xCA,0x22,0x7A,
            0xB4,0xEC,0x04,0x5C,0xD5,0x8D,0x65,0x3D,
            0x76,0x2E,0xC6,0x9E,0x17,0x4F,0xA7,0xFF
        };

        public class ECB
        {
            //whitening Key [8] + sub Key[128]
            byte[] scheduleKey = new byte[136];
            public ECB(byte[] userKey)
            {
                if (userKey.Length < 16)
                {
                    // Pad the key with 0x00 if its length is less than 16 bytes
                    byte[] paddedKey = new byte[16];
                    Array.Copy(userKey, 0, paddedKey, 0, userKey.Length);
                    userKey = paddedKey;
                }
                else if (userKey.Length > 16)
                {
                    // If the key is longer than 16 bytes, truncate to 16 bytes
                    byte[] truncatedKey = new byte[16];
                    Array.Copy(userKey, truncatedKey, 16);
                    userKey = truncatedKey;
                }

                KeySched(userKey);
            }

            void KeySched(byte[] userKey)
            {
                for (int i = 0; i < 4; i++)
                {
                    scheduleKey[i] = userKey[i + 12];
                    scheduleKey[i + 4] = userKey[i];
                }

                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        scheduleKey[8 + 16 * i + j] = (byte)((userKey[(j - i) & 7] + hightDelta[(16 * i + j)]) & 0xFF);
                    }
                    for (int j = 0; j < 8; j++)
                    {
                        scheduleKey[8 + 16 * i + j + 8] = (byte)((userKey[((j - i) & 7) + 8] + hightDelta[16 * i + j + 8]) & 0xFF);
                    }
                }
            }

            void DecryptBlock(byte[] dataIn, byte[] dataOut)
            {
                byte[] xx = new byte[8];

                xx[2] = dataIn[1];
                xx[4] = dataIn[3];
                xx[6] = dataIn[5];
                xx[0] = dataIn[7];

                //void HIGHT_DEC(byte[] scheduleKey, byte[] xx, int k, int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7)
                void HIGHT_DEC(int k, int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7)
                {
                    xx[i1] = (byte)((xx[i1] - (hightF1[xx[i2]] ^ scheduleKey[4 * k + 2])) & 0xFF);
                    xx[i3] = (byte)((xx[i3] ^ (hightF0[xx[i4]] + scheduleKey[4 * k + 1])) & 0xFF);
                    xx[i5] = (byte)((xx[i5] - (hightF1[xx[i6]] ^ scheduleKey[4 * k + 0])) & 0xFF);
                    xx[i7] = (byte)((xx[i7] ^ (hightF0[xx[i0]] + scheduleKey[4 * k + 3])) & 0xFF);
                }

                xx[1] = (byte)(dataIn[0] - scheduleKey[4]);
                xx[3] = (byte)(dataIn[2] ^ scheduleKey[5]);
                xx[5] = (byte)(dataIn[4] - scheduleKey[6]);
                xx[7] = (byte)(dataIn[6] ^ scheduleKey[7]);

                HIGHT_DEC(33, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_DEC(32, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_DEC(31, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_DEC(30, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_DEC(29, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_DEC(28, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_DEC(27, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_DEC(26, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_DEC(25, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_DEC(24, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_DEC(23, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_DEC(22, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_DEC(21, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_DEC(20, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_DEC(19, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_DEC(18, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_DEC(17, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_DEC(16, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_DEC(15, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_DEC(14, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_DEC(13, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_DEC(12, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_DEC(11, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_DEC(10, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_DEC(9, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_DEC(8, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_DEC(7, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_DEC(6, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_DEC(5, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_DEC(4, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_DEC(3, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_DEC(2, 6, 5, 4, 3, 2, 1, 0, 7);

                dataOut[1] = (byte)(xx[1] & 0xFF);
                dataOut[3] = (byte)(xx[3] & 0xFF);
                dataOut[5] = (byte)(xx[5] & 0xFF);
                dataOut[7] = (byte)(xx[7] & 0xFF);

                dataOut[0] = (byte)((xx[0] - scheduleKey[0]) & 0xFF);
                dataOut[2] = (byte)((xx[2] ^ scheduleKey[1]) & 0xFF);
                dataOut[4] = (byte)((xx[4] - scheduleKey[2]) & 0xFF);
                dataOut[6] = (byte)((xx[6] ^ scheduleKey[3]) & 0xFF);
            }

            void EncryptBlock(byte[] dataIn, byte[] dataOut)
            {
                byte[] xx = new byte[8];
                xx[1] = dataIn[1];
                xx[3] = dataIn[3];
                xx[5] = dataIn[5];
                xx[7] = dataIn[7];

                xx[0] = (byte)((dataIn[0] + scheduleKey[0]) & 0xFF);
                xx[2] = (byte)((dataIn[2] ^ scheduleKey[1]));
                xx[4] = (byte)((dataIn[4] + scheduleKey[2]) & 0xFF);
                xx[6] = (byte)((dataIn[6] ^ scheduleKey[3]));

                void HIGHT_ENC(int k, int i0, int i1, int i2, int i3, int i4, int i5, int i6, int i7)
                {
                    xx[i0] = (byte)((xx[i0] ^ (hightF0[xx[i1]] + scheduleKey[4 * k + 3])) & 0xFF);
                    xx[i2] = (byte)((xx[i2] + (hightF1[xx[i3]] ^ scheduleKey[4 * k + 2])) & 0xFF);
                    xx[i4] = (byte)((xx[i4] ^ (hightF0[xx[i5]] + scheduleKey[4 * k + 1])) & 0xFF);
                    xx[i6] = (byte)((xx[i6] + (hightF1[xx[i7]] ^ scheduleKey[4 * k + 0])) & 0xFF);
                }

                HIGHT_ENC(2, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_ENC(3, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_ENC(4, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_ENC(5, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_ENC(6, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_ENC(7, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_ENC(8, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_ENC(9, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_ENC(10, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_ENC(11, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_ENC(12, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_ENC(13, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_ENC(14, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_ENC(15, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_ENC(16, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_ENC(17, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_ENC(18, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_ENC(19, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_ENC(20, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_ENC(21, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_ENC(22, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_ENC(23, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_ENC(24, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_ENC(25, 0, 7, 6, 5, 4, 3, 2, 1);
                HIGHT_ENC(26, 7, 6, 5, 4, 3, 2, 1, 0);
                HIGHT_ENC(27, 6, 5, 4, 3, 2, 1, 0, 7);
                HIGHT_ENC(28, 5, 4, 3, 2, 1, 0, 7, 6);
                HIGHT_ENC(29, 4, 3, 2, 1, 0, 7, 6, 5);
                HIGHT_ENC(30, 3, 2, 1, 0, 7, 6, 5, 4);
                HIGHT_ENC(31, 2, 1, 0, 7, 6, 5, 4, 3);
                HIGHT_ENC(32, 1, 0, 7, 6, 5, 4, 3, 2);
                HIGHT_ENC(33, 0, 7, 6, 5, 4, 3, 2, 1);

                dataOut[1] = (byte)(xx[2] & 0xFF);
                dataOut[3] = (byte)(xx[4] & 0xFF);
                dataOut[5] = (byte)(xx[6] & 0xFF);
                dataOut[7] = (byte)(xx[0] & 0xFF);

                dataOut[0] = (byte)((xx[1] + scheduleKey[4]) & 0xFF);
                dataOut[2] = (byte)((xx[3] ^ scheduleKey[5]) & 0xFF);
                dataOut[4] = (byte)((xx[5] + scheduleKey[6]) & 0xFF);
                dataOut[6] = (byte)((xx[7] ^ scheduleKey[7]) & 0xFF);
            }

            public byte[] Encrypt(byte[] dataIn)
            {
                int length;
                if (dataIn.Length == 0)
                {
                    return null;
                }
                if ((dataIn.Length % 8) != 0)
                {
                    length = dataIn.Length + (8 - (dataIn.Length % 8));
                }
                else
                {
                    length = dataIn.Length;
                }
                byte[] dataOut = new byte[length];
                byte[] tempIn = new byte[8];
                byte[] tempOut = new byte[8];
                byte[] dataInWithPadding = new byte[length];
                Array.Copy(dataIn, dataInWithPadding, dataIn.Length);

                for (int i = 0; i < (int)(length / 8); i++)
                {
                    Array.Copy(dataInWithPadding, i * 8, tempIn, 0, 8);
                    EncryptBlock(tempIn, tempOut);
                    Array.Copy(tempOut, 0, dataOut, i * 8, 8);
                }
                return dataOut;
            }

            public byte[] Decrypt(byte[] dataIn)
            {
                int length;
                if (dataIn.Length == 0)
                {
                    return null;
                }
                if ((dataIn.Length % 8) != 0)
                {
                    length = dataIn.Length + (8 - (dataIn.Length % 8));
                }
                else
                {
                    length = dataIn.Length;
                }
                byte[] dataOut = new byte[length];
                byte[] tempIn = new byte[8];
                byte[] tempOut = new byte[8];
                byte[] dataInWithPadding = new byte[length];
                Array.Copy(dataIn, dataInWithPadding, dataIn.Length);
                for (int i = 0; i < (int)(length / 8); i++)
                {
                    Array.Copy(dataInWithPadding, i * 8, tempIn, 0, 8);
                    DecryptBlock(tempIn, tempOut);
                    Array.Copy(tempOut, 0, dataOut, i * 8, 8);
                }
                return dataOut;
            }

            public void Test()
            {
                byte[] dataIn = new byte[15];
                byte[] dataOut = new byte[15];
                byte[] dataOut2 = new byte[15];
                for (int i = 0; i < 15; i++)
                {
                    dataIn[i] = (byte)i;
                }
                Console.WriteLine("schedule key data");
                Console.WriteLine(string.Format("0x{0:x2}", scheduleKey));

                dataOut = Encrypt(dataIn);
                dataOut2 = Decrypt(dataOut);
                Console.WriteLine("origin data");
                Console.WriteLine(string.Format("0x{0:x2}", dataIn));
                Console.WriteLine("encryption data");
                Console.WriteLine(string.Format("0x{0:x2}", dataOut));
                Console.WriteLine("decryption data");
                Console.WriteLine(string.Format("0x{0:x2}", dataOut2));
            }
        }
    }
}

/* References:
 * [1] KISA(Korea Internet & Security Agency) - HIGHT
 *     https://seed.kisa.or.kr/kisa/algorithm/EgovHightInfo.do
 * [2] GitHub - crypto hight ecb examples for csharp (chandong83/csharp_crypto_hight_ecb_examples)
 *     https://github.com/chandong83/csharp_crypto_hight_ecb_examples
 * [3] Naver Blog - C#(CSharp) - HIGHT ECB 암/복호화 알고리즘 소스 코드 (@chandong83)
 *     https://blog.naver.com/chandong83/222198351602
 * [4] ISO - ISO/IEC 18033-3:2010 Information technology — Security techniques — Encryption algorithms - Part 3: Block ciphers
 *     https://www.iso.org/standard/54531.html
 * [5] KISA(Korea Internet & Security Agency) - HIGHT Algorithm Specification(2009. 07.)
 *     https://ics.catswords.net/HIGHT-algorithm-specification-english.pdf
 * [6] HIGHT 블록암호 알고리즘 사양 및 세부 명세서(2009. 07.)
 *     https://ics.catswords.net/HIGHT-algorithm-specification-korean.pdf
 * [7] HIGHT 블록암호 알고리즘에 대한 소스코드 활용 메뉴얼(2009. 07.)
 *     https://ics.catswords.net/HIGHT-sourcecode-explanation.pdf
 */
