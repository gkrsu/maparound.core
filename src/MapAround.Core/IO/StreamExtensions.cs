//  MapAround - .NET tools for developing web and desktop mapping applications 

//  Copyright (coffee) 2009-2012 OOO "GKR"
//  This program is free software; you can redistribute it and/or 
//  modify it under the terms of the GNU General Public License 
//   as published by the Free Software Foundation; either version 3 
//  of the License, or (at your option) any later version. 
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; If not, see <http://www.gnu.org/licenses/>



﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace MapAround.IO
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Reads a 8-byte signed double using the big-endian layout 
        /// from the current stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>The read value</returns>       
        public static double ReadDoubleBE(this Stream stream)
        {
            byte[] byteArray = new byte[8];
            int iBytesRead = stream.Read(byteArray, 0, 8);

            Array.Reverse(byteArray);
            return BitConverter.ToDouble(byteArray, 0);
        }

        /// <summary>
        /// Reads a 8-byte signed double using the little-endian layout 
        /// from the current stream and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>The read value</returns>       
        public static double ReadDouble(this Stream stream)
        {
            byte[] doubleBytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                doubleBytes[i] = (byte)b;
            }

            return BitConverter.ToDouble(doubleBytes, 0);
        }

        /// <summary>
        /// Reads a 4-byte integer using the big-endian layout 
        /// from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>The read value</returns>       
        public static int ReadInt32BE(this Stream stream)
        {
            byte[] byteArray = new byte[4];
            int iBytesRead = stream.Read(byteArray, 0, 4);

            Array.Reverse(byteArray);
            return BitConverter.ToInt32(byteArray, 0);
        }

        /// <summary>
        /// Reads a 4-byte integer using the little-endian layout 
        /// from the current stream and advances the current position of the stream by four bytes.
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <returns>The read value</returns>       
        public static int ReadInt32(this Stream stream)
        {
            byte[] intBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                intBytes[i] = (byte)b;
            }

            return BitConverter.ToInt32(intBytes, 0);
        }        
    }
}
