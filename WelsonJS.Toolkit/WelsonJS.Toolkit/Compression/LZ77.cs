/*
 * WelsonJS.Toolkit: WelsonJS dotNET native component
 * 
 *     filename:
 *         LZ77.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 */
using System.Text;

namespace WelsonJS.Compression
{
    public class LZ77
    {
        public static string Compress(string input)
        {
            StringBuilder compressed = new StringBuilder();
            int searchBufferIndex = 0;

            while (searchBufferIndex < input.Length)
            {
                int longestMatchLength = 0;
                int longestMatchOffset = 0;

                // Search for the longest match in the look-ahead buffer
                for (int i = 0; i < searchBufferIndex; i++)
                {
                    int matchLength = 0;
                    while (matchLength < input.Length - searchBufferIndex && input[i + matchLength] == input[searchBufferIndex + matchLength])
                    {
                        matchLength++;
                    }

                    if (matchLength > longestMatchLength)
                    {
                        longestMatchLength = matchLength;
                        longestMatchOffset = searchBufferIndex - i;
                    }
                }

                // Output the token (offset, length)
                if (longestMatchLength > 0)
                {
                    compressed.Append($"({longestMatchOffset},{longestMatchLength})");
                    searchBufferIndex += longestMatchLength;
                }
                else
                {
                    compressed.Append($"(0,{input[searchBufferIndex]})");
                    searchBufferIndex++;
                }
            }

            return compressed.ToString();
        }

        public static string Decompress(string compressedData)
        {
            StringBuilder decompressed = new StringBuilder();
            int currentIndex = 0;

            while (currentIndex < compressedData.Length)
            {
                if (compressedData[currentIndex] == '(')
                {
                    // Match case
                    int commaIndex = compressedData.IndexOf(',', currentIndex);
                    int offset = int.Parse(compressedData.Substring(currentIndex + 1, commaIndex - currentIndex - 1));
                    int closingParenIndex = compressedData.IndexOf(')', commaIndex);
                    int length = int.Parse(compressedData.Substring(commaIndex + 1, closingParenIndex - commaIndex - 1));

                    for (int i = 0; i < length; i++)
                    {
                        int copyIndex = decompressed.Length - offset;
                        decompressed.Append(decompressed[copyIndex]);
                    }

                    currentIndex = closingParenIndex + 1;
                }
                else
                {
                    // Literal case
                    decompressed.Append(compressedData[currentIndex]);
                    currentIndex++;
                }
            }

            return decompressed.ToString();
        }
    }
}
