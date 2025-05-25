// PKCS5Padding.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;

namespace WelsonJS.Cryptography
{
    public class PKCS5Padding
    {
        /// <summary>
        /// Add PKCS#5 padding to the input data to make it a multiple of the block size.
        /// </summary>
        /// <param name="data">The data to be padded.</param>
        /// <param name="blockSize">The block size to pad to.</param>
        /// <returns>Padded data with PKCS#5 padding.</returns>
        public static byte[] AddPadding(byte[] data, int blockSize)
        {
            int paddingLength = blockSize - (data.Length % blockSize);

            // If the data is already a multiple of the block size, no padding is needed
            if (paddingLength == blockSize)
            {
                return data;
            }

            byte[] paddedData = new byte[data.Length + paddingLength];

            // Copy original data into the padded array
            Array.Copy(data, paddedData, data.Length);

            // Fill padding with the padding length (PKCS5)
            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingLength;
            }

            return paddedData;
        }

        /// <summary>
        /// Removes PKCS#5 padding from the given data.
        /// </summary>
        /// <param name="data">The input data, including padding.</param>
        /// <param name="blockSize">The block size used for padding.</param>
        /// <param name="ignoreErrors">If true, ignores errors and attempts to process the input data as-is.</param>
        /// <returns>The unpadded data as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown if the input data or padding is invalid and ignoreErrors is false.</exception>
        public static byte[] RemovePadding(byte[] data, int blockSize, bool ignoreErrors = false)
        {
            // If data length is 0, return empty array
            if (data.Length == 0)
            {
                return new byte[] { };
            }

            // If data length is smaller than block size, treat it as unpadded
            if (data.Length < blockSize)
            {
                return data;
            }

            // Check if the last byte is valid padding (PKCS5)
            int paddingLength = data[data.Length - 1];

            // Validate padding length
            if (paddingLength <= 0 || paddingLength > blockSize)
            {
                if (!ignoreErrors)
                {
                    throw new ArgumentException($"Invalid padding length: {paddingLength}. Must be between 1 and {blockSize}.");
                }

                return data;
            }

            // Check if the padding is correct (i.e., all padding bytes must be equal to paddingLength)
            for (int i = data.Length - paddingLength; i < data.Length; i++)
            {
                if (data[i] != paddingLength)
                {
                    if (!ignoreErrors)
                    {
                        throw new ArgumentException("Invalid padding detected.");
                    }
                }
            }

            // Remove the padding
            byte[] unpaddedData = new byte[data.Length - paddingLength];
            Array.Copy(data, unpaddedData, unpaddedData.Length);

            return unpaddedData;
        }
    }
}

/* References:
 * [1] ChatGPT - The prompt "PKCS5Padding with C#"
 * [2] ChatGPT - The prompt "PKCS5Padding with C#, Add a flag to decide how to handle possible errors when removing padding."
 */
