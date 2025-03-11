using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRC32B
{
    // Courtesy of VelouriasMoon's TotkRSTB: https://github.com/VelouriasMoon/TotkRSTB/blob/main/TotkRSTB/Crc32.cs
    public static class CRC32B
    {
        private static readonly uint[] _table = CreateTable();

        /// <summary>
        /// Compute the checksum of a binary buffer.
        /// </summary>
        /// <param name="chars">Buffer to calculate</param>
        /// <returns></returns>
        public static uint Compute(ReadOnlySpan<char> chars)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < chars.Length; ++i)
            {
                byte index = (byte)(((crc) & 0xff) ^ chars[i]);
                crc = (crc >> 8) ^ _table[index];
            }

            return unchecked((~crc));
        }

        static uint[] CreateTable()
        {
            const uint poly = 0xEDB88320;
            var table = new uint[256];
            for (uint i = 0; i < table.Length; ++i)
            {
                uint temp = i;
                for (int j = 8; j > 0; --j)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (uint)((temp >> 1) ^ poly);
                    }
                    else
                    {
                        temp >>= 1;
                    }
                }

                table[i] = temp;
            }

            return table;
        }
    }
}
