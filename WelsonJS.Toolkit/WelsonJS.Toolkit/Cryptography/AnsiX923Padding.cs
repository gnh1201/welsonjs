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
        /// Applies ANSI X.923 padding to the input data to make it a multiple of the block size.
        /// </summary>
        /// <param name="data">The data to be padded.</param>
        /// <param name="blockSize">The block size to pad to.</param>
        /// <returns>Padded data with ANSI X.923 padding.</returns>
        public static byte[] ApplyPadding(byte[] data, int blockSize)
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
        /// Removes the ANSI X.923 padding from the data.
        /// </summary>
        /// <param name="data">The padded data to remove padding from.</param>
        /// <param name="blockSize">The block size used during padding.</param>
        /// <returns>Data without padding.</returns>
        public static byte[] RemovePadding(byte[] data, int blockSize)
        {
            // The last byte is the padding length
            int paddingLength = data[data.Length - 1];

            // Remove the padding
            byte[] unpaddedData = new byte[data.Length - paddingLength];
            Array.Copy(data, unpaddedData, unpaddedData.Length);

            return unpaddedData;
        }
    }
}
