// LEA.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
// LEA(KS X 3246:2016) cryptography algorithm implementation (Experimental)
// 
using System;
using System.Security.Cryptography;
using System.Text;

namespace WelsonJS.Cryptography
{
    public class LEA
    {
        private const int BLOCKSIZE = 16;
        private static readonly uint[] delta = {
            0xc3efe9db, 0x44626b02, 0x79e27c8a, 0x78df30ec,
            0x715ea49e, 0xc785da0a, 0xe04ef22a, 0xe5c40957
        };

        private Mode mode;
        private int rounds;
        private uint[,] roundKeys;
        private uint[] block;

        public LEA()
        {
            block = new uint[BLOCKSIZE / 4];
        }

        public void Init(Mode mode, byte[] mk)
        {
            this.mode = mode;
            GenerateRoundKeys(mk);
        }

        public void Reset()
        {
            Array.Clear(block, 0, block.Length);
        }

        public string GetAlgorithmName()
        {
            return "LEA";
        }

        public int GetBlockSize()
        {
            return BLOCKSIZE;
        }

        public int ProcessBlock(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            if (input == null || output == null)
                throw new ArgumentNullException("Input and output buffers must not be null.");

            if (input.Length - inputOffset < BLOCKSIZE)
                throw new InvalidOperationException("Input data is too short.");

            if (output.Length - outputOffset < BLOCKSIZE)
                throw new InvalidOperationException("Output buffer is too short.");

            return mode == Mode.ENCRYPT
                ? EncryptBlock(input, inputOffset, output, outputOffset)
                : DecryptBlock(input, inputOffset, output, outputOffset);
        }

        private int EncryptBlock(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            Pack(input, inputOffset, ref block, 0, 16);

            for (int i = 0; i < rounds; ++i)
            {
                block[3] = ROR((block[2] ^ roundKeys[i, 4]) + (block[3] ^ roundKeys[i, 5]), 3);
                block[2] = ROR((block[1] ^ roundKeys[i, 2]) + (block[2] ^ roundKeys[i, 3]), 5);
                block[1] = ROL((block[0] ^ roundKeys[i, 0]) + (block[1] ^ roundKeys[i, 1]), 9);
                ++i;

                block[0] = ROR((block[3] ^ roundKeys[i, 4]) + (block[0] ^ roundKeys[i, 5]), 3);
                block[3] = ROR((block[2] ^ roundKeys[i, 2]) + (block[3] ^ roundKeys[i, 3]), 5);
                block[2] = ROL((block[1] ^ roundKeys[i, 0]) + (block[2] ^ roundKeys[i, 1]), 9);

                ++i;
                block[1] = ROR((block[0] ^ roundKeys[i, 4]) + (block[1] ^ roundKeys[i, 5]), 3);
                block[0] = ROR((block[3] ^ roundKeys[i, 2]) + (block[0] ^ roundKeys[i, 3]), 5);
                block[3] = ROL((block[2] ^ roundKeys[i, 0]) + (block[3] ^ roundKeys[i, 1]), 9);

                ++i;
                block[2] = ROR((block[1] ^ roundKeys[i, 4]) + (block[2] ^ roundKeys[i, 5]), 3);
                block[1] = ROR((block[0] ^ roundKeys[i, 2]) + (block[1] ^ roundKeys[i, 3]), 5);
                block[0] = ROL((block[3] ^ roundKeys[i, 0]) + (block[0] ^ roundKeys[i, 1]), 9);
            }

            Unpack(block, 0, ref output, outputOffset, 4);

            return BLOCKSIZE;
        }

        private int DecryptBlock(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            Pack(input, inputOffset, ref block, 0, 16);

            for (int i = rounds - 1; i >= 0; --i)
            {
                block[0] = (ROR(block[0], 9) - (block[3] ^ roundKeys[i, 0])) ^ roundKeys[i, 1];
                block[1] = (ROL(block[1], 5) - (block[0] ^ roundKeys[i, 2])) ^ roundKeys[i, 3];
                block[2] = (ROL(block[2], 3) - (block[1] ^ roundKeys[i, 4])) ^ roundKeys[i, 5];
                --i;

                block[3] = (ROR(block[3], 9) - (block[2] ^ roundKeys[i, 0])) ^ roundKeys[i, 1];
                block[0] = (ROL(block[0], 5) - (block[3] ^ roundKeys[i, 2])) ^ roundKeys[i, 3];
                block[1] = (ROL(block[1], 3) - (block[0] ^ roundKeys[i, 4])) ^ roundKeys[i, 5];
                --i;

                block[2] = (ROR(block[2], 9) - (block[1] ^ roundKeys[i, 0])) ^ roundKeys[i, 1];
                block[3] = (ROL(block[3], 5) - (block[2] ^ roundKeys[i, 2])) ^ roundKeys[i, 3];
                block[0] = (ROL(block[0], 3) - (block[3] ^ roundKeys[i, 4])) ^ roundKeys[i, 5];
                --i;

                block[1] = (ROR(block[1], 9) - (block[0] ^ roundKeys[i, 0])) ^ roundKeys[i, 1];
                block[2] = (ROL(block[2], 5) - (block[1] ^ roundKeys[i, 2])) ^ roundKeys[i, 3];
                block[3] = (ROL(block[3], 3) - (block[2] ^ roundKeys[i, 4])) ^ roundKeys[i, 5];
            }

            Unpack(block, 0, ref output, outputOffset, 4);

            return BLOCKSIZE;
        }

        private void GenerateRoundKeys(byte[] mk)
        {
            if (mk == null || (mk.Length != 16 && mk.Length != 24 && mk.Length != 32))
                throw new ArgumentException("Illegal key size");

            uint[] T = new uint[8];
            rounds = (mk.Length >> 1) + 16;
            roundKeys = new uint[rounds, 6];

            Pack(mk, 0, ref T, 0, 16);

            if (mk.Length > 16)
            {
                Pack(mk, 16, ref T, 4, 8);
            }

            if (mk.Length > 24)
            {
                Pack(mk, 24, ref T, 6, 8);
            }

            if (mk.Length == 16)
            {
                for (int i = 0; i < 24; ++i)
                {
                    uint temp = ROL(delta[i & 3], i);

                    roundKeys[i, 0] = T[0] = ROL(T[0] + ROL(temp, 0), 1);
                    roundKeys[i, 1] = roundKeys[i, 3] = roundKeys[i, 5] = T[1] = ROL(T[1] + ROL(temp, 1), 3);
                    roundKeys[i, 2] = T[2] = ROL(T[2] + ROL(temp, 2), 6);
                    roundKeys[i, 4] = T[3] = ROL(T[3] + ROL(temp, 3), 11);
                }

            }
            else if (mk.Length == 24)
            {
                for (int i = 0; i < 28; ++i)
                {
                    uint temp = ROL(delta[i % 6], i);

                    roundKeys[i, 0] = T[0] = ROL(T[0] + ROL(temp, 0), 1);
                    roundKeys[i, 1] = T[1] = ROL(T[1] + ROL(temp, 1), 3);
                    roundKeys[i, 2] = T[2] = ROL(T[2] + ROL(temp, 2), 6);
                    roundKeys[i, 3] = T[3] = ROL(T[3] + ROL(temp, 3), 11);
                    roundKeys[i, 4] = T[4] = ROL(T[4] + ROL(temp, 4), 13);
                    roundKeys[i, 5] = T[5] = ROL(T[5] + ROL(temp, 5), 17);
                }

            }
            else
            {
                for (int i = 0; i < 32; ++i)
                {
                    uint temp = ROL(delta[i & 7], i & 0x1f);

                    roundKeys[i, 0] = T[(6 * i + 0) & 7] = ROL(T[(6 * i + 0) & 7] + temp, 1);
                    roundKeys[i, 1] = T[(6 * i + 1) & 7] = ROL(T[(6 * i + 1) & 7] + ROL(temp, 1), 3);
                    roundKeys[i, 2] = T[(6 * i + 2) & 7] = ROL(T[(6 * i + 2) & 7] + ROL(temp, 2), 6);
                    roundKeys[i, 3] = T[(6 * i + 3) & 7] = ROL(T[(6 * i + 3) & 7] + ROL(temp, 3), 11);
                    roundKeys[i, 4] = T[(6 * i + 4) & 7] = ROL(T[(6 * i + 4) & 7] + ROL(temp, 4), 13);
                    roundKeys[i, 5] = T[(6 * i + 5) & 7] = ROL(T[(6 * i + 5) & 7] + ROL(temp, 5), 17);
                }
            }
        }

        private static uint ROL(uint state, int num)
        {
            return (state << num) | state >> (32 - num);
        }

        private static uint ROR(uint state, int num)
        {
            return (state >> num) | state << (32 - num);
        }

        public static void Pack(in byte[] input, int inputOffset, ref uint[] output, int outputOffset, int inputLength)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException();
            }

            if ((inputLength & 3) != 0)
            {
                throw new ArgumentException("Length should be a multiple of 4.");
            }

            if (input.Length < inputOffset + inputLength || output.Length < outputOffset + inputLength / 4)
            {
                throw new IndexOutOfRangeException();
            }

            int outputIndex = 0;
            for (int inputIdx = 0; inputIdx < input.Length; ++inputIdx, ++outputIndex) {
                output[outputIndex] = (uint)(input[inputIdx] & 0xff);
                output[outputIndex] |= (uint)((input[++inputIdx] & 0xff) << 8);
                output[outputIndex] |= (uint)((input[++inputIdx] & 0xff) << 16);
                output[outputIndex] |= (uint)((input[++inputIdx] & 0xff) << 24);
            }
        }

        public static void Unpack(in uint[] input, int inputOffset, ref byte[] output, int outputOffset, int inputLength)
        {
            if (input == null || output == null)
            {
                throw new ArgumentNullException();
            }

            if (input.Length < inputOffset + inputLength || output.Length < outputOffset + inputLength * 4)
            {
                throw new IndexOutOfRangeException();
            }

            int outputIdx = outputOffset;
            int endInIdx = inputOffset + inputLength;
            for (int inputIdx = inputOffset; inputIdx < endInIdx; ++inputIdx, ++outputIdx)
            {
                output[outputIdx] = (byte)input[inputIdx] ;
                output[++outputIdx] = (byte)(input[inputIdx] >> 8);
                output[++outputIdx] = (byte)(input[inputIdx] >> 16);
                output[++outputIdx] = (byte)(input[inputIdx] >> 24);
            }
        }

        public enum Mode
        {
            ENCRYPT,
            DECRYPT
        }

        public class ECB
        {
            private Mode mode;
            private LEA engine;
            private int blockSize;

            ECB(Mode mode, string key)
            {
                engine = new LEA();
                Init(mode, CreateKey(key));
                blockSize = engine.GetBlockSize();
            }

            public string GetAlgorithmName()
            {
                return engine.GetAlgorithmName() + "/ECB";
            }

            public void Init(Mode mode, byte[] mk)
            {
                this.mode = mode;
                engine.Init(mode, mk);
            }

            private byte[] CreateKey(string key)
            {
                SHA256 hasher = SHA256.Create();
                byte[] hashData = hasher.ComputeHash(Encoding.Default.GetBytes(key));

                return hashData;
            }

            public byte[] Encrypt(byte[] data)
            {
                if (this.mode != Mode.ENCRYPT)
                    throw new InvalidOperationException("Not initialized for encryption mode.");

                byte[] inputData = PKCS5Padding.AddPadding(data, blockSize);
                byte[] outputData = new byte[inputData.Length];

                for (int i = 0; i < inputData.Length; i += blockSize)
                {
                    engine.ProcessBlock(inputData, i, outputData, i);
                }

                return outputData;
            }

            public byte[] Decrypt(byte[] data)
            {
                if (this.mode != Mode.DECRYPT)
                    throw new InvalidOperationException("Not initialized for decryption mode.");

                byte[] outputData = new byte[data.Length];

                for (int i = 0; i < data.Length; i += blockSize)
                {
                    engine.ProcessBlock(data, i, outputData, i);
                }

                return PKCS5Padding.RemovePadding(outputData, blockSize, true);
            }
        }
    }
}

/* References:
 * [1] KISA(Korea Internet & Security Agency) - 블록암호 LEA
 *     https://seed.kisa.or.kr/kisa/Board/20/detailView.do
 * [2] TTA(Telecommunications Technology Association) - TTAK.KO-12.0223, TTA표준화 위원회
 *     https://committee.tta.or.kr/data/standard_view.jsp?order=t.kor_standard&by=asc&pk_num=TTAK.KO-12.0223&commit_code=TC5
 * [3] NSRI(National Security Research Institute) - 128비트 블록암호 LEA 규격서
 *     https://ics.catswords.net/LEA%20A%20128-Bit%20Block%20Cipher%20Datasheets-Korean.pdf
 * [4] ETRI, Pusan National University - LEA: A 128-Bit Block Cipher for Fast Encryption on Common Processors
 *     https://ics.catswords.net/LEA%20A%20128-Bit%20Block%20Cipher%20for%20Fast%20Encryption%20on%20Common%20Processors-English.pdf
 * [5] NSRI(National Security Research Institute) - 블록암호 LEA 소스코드 사용 매뉴얼
 *     https://ics.catswords.net/LEA-sourcecode-explanation.pdf
 */
