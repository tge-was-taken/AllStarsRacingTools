using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllStarsRacingLib
{
    public static class StringHasher
    {
        private const int cBlockSize = 12;
        private const uint cSalt = 0x9E3779B9;

        public static uint ComputeHash( string value, uint key = 0x04C11DB7 )
        {
            uint v4 = key;
            uint v3 = cSalt;
            uint v7 = cSalt;
            uint v14 = cSalt;
            uint v15 = cSalt;

            int lengthRemainder = value.Length;
            int offset = 0;

            if ( value.Length >= cBlockSize )
            {
                int blockCount = value.Length / cBlockSize;

                for ( int blockIndex = 0; blockIndex < blockCount; blockIndex++, lengthRemainder -= cBlockSize, offset += cBlockSize )
                {
                    v15 += ( uint )( ( ( byte )value[01 + offset] + ( ( ( byte )value[02 + offset] + ( ( byte )value[03 + offset] << 8 ) ) << 8 ) )      + ( byte )value[00 + offset] );
                    v14 =  ( uint )( ( ( byte )value[05 + offset] + ( ( ( byte )value[06 + offset] + ( ( byte )value[07 + offset] << 8 ) ) << 8 ) ) + v3 + ( byte )value[04 + offset] );
                    key =  ( uint )( ( ( byte )value[09 + offset] + ( ( ( byte )value[10 + offset] + ( ( byte )value[11 + offset] << 8 ) ) << 8 ) ) + v4 + ( byte )value[08 + offset] );
                    ComputeBlockHash( ref key, ref v15, ref v14 );
                    v3 = v14;
                    v4 = key;
                }

                v7 = v15;
            }

            uint v12 = ( uint )( value.Length + v4 );
            key = v12;

            if ( lengthRemainder == 11 )
            {
                v12 += ( uint )( ( byte )value[offset + 10] << 24 );
            }

            if ( lengthRemainder >= 10 )
            {
                v12 += ( uint )( ( byte )value[offset + 9] << 16 );
            }

            if ( lengthRemainder >= 9 )
            {
                key = ( uint )( ( byte )( ( value[offset + 8] << 8 ) + v12 ) );
            }

            if ( lengthRemainder >= 8 )
            {
                v3 += ( uint )( ( byte )value[offset + 7] << 24 );
            }

            if ( lengthRemainder >= 7 )
            {
                v3 += ( uint )( ( byte )value[offset + 6] << 16 );
            }

            if ( lengthRemainder >= 6 )
            {
                v3 += ( uint )( ( byte )value[offset + 5] << 8 );
            }

            if ( lengthRemainder >= 5 )
            {
                v14 = value[offset + 4] + v3;
            }

            if ( lengthRemainder >= 4 )
            {
                v7 += ( uint )( ( byte )value[offset + 3] << 24);
            }

            if ( lengthRemainder >= 3 )
            {
                v7 += ( uint )( ( byte )value[offset + 2] << 16);
            }

            if ( lengthRemainder >= 2 )
            {
                v7 += ( uint )( ( byte )value[offset + 1] << 8);
            }

            if ( lengthRemainder >= 1 )
            {
                v15 = value[offset] + v7;
            }

            ComputeBlockHash( ref key, ref v15, ref v14 );

            return key;
        }

        /// <summary>
        /// Computes a simple and inexpensive hash. Used for file system.
        /// </summary>
        /// <param name="value">String to hash.</param>
        /// <returns>Hash value.</returns>
        public static uint ComputeSimpleHash( string value )
        {
            uint hash = 0;
            for ( int i = value.Length - 1; i >= 0; --i )
            {
                char c = char.ToUpper( value[i] );
                if ( c == '/' )
                    c = '\\';

                hash = c + 0x83 * hash;
            }

            return hash;
        }

        private static void ComputeBlockHash( ref uint result, ref uint a2, ref uint a3 )
        {
            a2 -= a3;
            a2 = a2 - result ^ ( result >> 13 );
            a3 -= result;
            a3 = a3 - a2 ^ ( a2 << 8 );
            result -= a2;
            result = result - a3 ^ ( a3 >> 13 );
            a2 -= a3;
            a2 = a2 - result ^ ( result >> 12 );
            a3 -= result;
            a3 = a3 - a2 ^ ( a2 << 16 );
            result -= a2;
            result = result - a3 ^ ( a3 >> 5 );
            a2 -= a3;
            a2 = a2 - result ^ ( result >> 3 );
            a3 -= result;
            a3 = a3 - a2 ^ ( a2 << 10 );
            result -= a2;
            result = result - a3 ^ ( a3 >> 15 );
        }
    }
}
