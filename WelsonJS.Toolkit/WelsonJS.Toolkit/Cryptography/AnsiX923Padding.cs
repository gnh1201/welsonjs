/*
 * WelsonJS.Toolkit: WelsonJS native component
 * 
 *     filename:
 *         AnsiX923Padding.cs
 * 
 *     description:
 *         AnsiX923Padding implementation
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 *         - https://teams.live.com/l/community/FEACHncAhq8ldnojAI
 *         - https://discord.gg/XKG5CjtXEj
 * 
 *     authors:
 *         - Namhyeon Go (@gnh1201) <abuse@catswords.net>
 *     
 *     references:
 *         - https://github.com/eGovFrame/egovframework.rte.root/blob/master/Foundation/egovframework.rte.fdl.crypto/src/main/java/egovframework/rte/fdl/cryptography/impl/aria/AnsiX923Padding.java
 *         - ChatGPT prompt "AnsiX923Padding with C#" (chatgpt.com)
 *         - ChatGPT prompt "AnsiX923Padding with C#, Add a flag to decide how to handle possible errors when removing padding." (chatgpt.com)
 *         
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 */
using System;

namespace WelsonJS.Cryptography
{
    class AnsiX923Padding
    {
        /// <summary>
        /// Add ANSI X.923 padding to the input data to make it a multiple of the block size.
        /// </summary>
        /// <param name="data">The data to be padded.</param>
        /// <param name="blockSize">The block size to pad to.</param>
        /// <returns>Padded data with ANSI X.923 padding.</returns>
        public static byte[] AddPadding(byte[] data, int blockSize)
        {
            int paddingLength = blockSize - (data.Length % blockSize);

            // If the data is already a multiple of the block size, no padding is needed
            if (paddingLength == blockSize)
            {
                return data;
            }

            byte[] paddedData = new byte[data.Length + paddingLength];
            Array.Copy(data, paddedData, data.Length);

            // Fill with 0x00 bytes, and the last byte is the padding length
            for (int i = data.Length; i < paddedData.Length - 1; i++)
            {
                paddedData[i] = 0x00;
            }

            // Last byte is the padding length
            paddedData[paddedData.Length - 1] = (byte)paddingLength;

            return paddedData;
        }

        /// <summary>
        /// Removes ANSI X.923 padding from the given data.
        /// </summary>
        /// <param name="data">The input data, including padding.</param>
        /// <param name="blockSize">The block size used for padding.</param>
        /// <param name="ignoreErrors">If true, ignores errors and attempts to process the input data as-is.</param>
        /// <returns>The unpadded data as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown if the input data or padding is invalid and ignoreErrors is false.</exception>
        public static byte[] RemovePadding(byte[] data, int blockSize, bool ignoreErrors = false)
        {
            // Check for null or empty data
            if (data == null || data.Length == 0)
            {
                if (ignoreErrors)
                {
                    return new byte[] { };
                }
                throw new ArgumentException("Input data cannot be null or empty.");
            }

            // Ensure the data length is a multiple of the block size
            if (data.Length % blockSize != 0)
            {
                if (ignoreErrors)
                {
                    // Return the original data if errors are ignored
                    return data;
                }
                throw new ArgumentException("Input data length must be a multiple of the block size.");
            }

            // Retrieve the padding length from the last byte
            int paddingLength = data[data.Length - 1];

            // Validate padding length
            if (paddingLength <= 0 || paddingLength > blockSize)
            {
                if (ignoreErrors)
                {
                    // Treat padding length as 0 and return the full data
                    return data;
                }
                throw new ArgumentException($"Invalid padding length: {paddingLength}. Must be between 1 and {blockSize}.");
            }

            // Validate the padding region (last paddingLength - 1 bytes must be 0x00)
            for (int i = data.Length - paddingLength; i < data.Length - 1; i++)
            {
                if (data[i] != 0x00)
                {
                    if (ignoreErrors)
                    {
                        // Ignore invalid padding and return data up to the detected length
                        byte[] fallbackData = new byte[data.Length - paddingLength];
                        Array.Copy(data, 0, fallbackData, 0, fallbackData.Length);
                        return fallbackData;
                    }
                    throw new ArgumentException("Invalid padding detected. Expected padding bytes to be 0x00.");
                }
            }

            // Extract unpadded data
            byte[] unpaddedData = new byte[data.Length - paddingLength];
            Array.Copy(data, 0, unpaddedData, 0, unpaddedData.Length);

            return unpaddedData;
        }
    }
}
