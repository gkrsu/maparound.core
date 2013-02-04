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



namespace MapAround.IO
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

	/// <summary>
    /// Extends the <see cref="BinaryWriter" /> class to allow the writing of integers 
    /// and double values in the Big Endian format.
	/// </summary>
	internal class BigEndianBinaryWriter : BinaryWriter
	{		
		/// <summary>
		/// Initializes a new instance of the BigEndianBinaryWriter class.
		/// </summary>
		public BigEndianBinaryWriter() : base() { }

		/// <summary>
		/// Initializes a new instance of BigEndianBinaryWriter class 
        /// based on the supplied stream and using UTF-8 as the encoding for strings.
		/// </summary>
		/// <param name="output">The supplied stream.</param>
		public BigEndianBinaryWriter(Stream output) : base(output) { }

		/// <summary>
		/// Initializes a new instance of BigEndianBinaryWriter class 
        /// based on the supplied stream and a specific character encoding.
		/// </summary>
		/// <param name="output">The supplied stream.</param>
		/// <param name="encoding">The character encoding.</param>
		public BigEndianBinaryWriter(Stream output, Encoding encoding): base(output,encoding) { }

		/// <summary>
		/// Reads a 4-byte signed integer using the big-endian layout from the current stream 
        /// and advances the current position of the stream by two bytes.
		/// </summary>
        /// <param name="value">The four-byte signed integer to write.</param>
		public void WriteIntBE(int value)
		{
            byte[] bytes = BitConverter.GetBytes(value);
            Debug.Assert(bytes.Length == 4);

            Array.Reverse(bytes, 0, 4);
            Write(bytes);				
		}

        /// <summary>
        /// Reads a 8-byte signed integer using the big-endian layout from the current stream 
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <param name="value">The four-byte signed integer to write.</param>
        public void WriteDoubleBE(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Debug.Assert(bytes.Length == 8);

            Array.Reverse(bytes, 0, 8);
            Write(bytes);
        }
	}
}
