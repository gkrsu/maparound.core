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
    using System.Collections;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Writes dBase IV files.
    /// </summary>
    /// <remarks>
    /// Used mainly for writing ESRI Shapefile attributes. 
    /// </remarks>
    internal class DbaseWriter
    {
        readonly BinaryWriter _writer;

        private bool _headerWritten;

        private DbaseFileHeader _header;

        /// <summary>
        /// Gets a dBase file header.
        /// </summary>
        public DbaseFileHeader Header
        {
            get { return _header; }            
        }
        
        /// <summary>
        /// Initializes a new instance of MapAround.IO.DbaseWriter
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="dbaseHeader">A dBase file header</param>
        public DbaseWriter(string filename, DbaseFileHeader dbaseHeader)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            this._header = dbaseHeader;
            FileStream filestream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Write);
            _writer = new BinaryWriter(filestream, this._header.Encoding);
        }

        /// <summary>
        /// Writes a header.
        /// </summary>
        public void WriteHeader()
        {
            if (_header == null)
                throw new ArgumentNullException("header");
            //if (_recordsWritten)
            //    throw new InvalidOperationException("Records have already been written. Header file needs to be written first.");
            _header.Write(_writer);
            _headerWritten = true;
            //_header = _header;
        }

        /// <summary>
        /// Writes a row.
        /// </summary>
        /// <param name="columnValues">A list containing the column values</param>
        /// <param name="RecNum">A number of record</param>
        public void Write(IList columnValues, int RecNum)
        {
            if (columnValues == null)
                throw new ArgumentNullException("columnValues");
            if (!_headerWritten)
                throw new InvalidOperationException("Header records need to be written first.");
            int i = 0;

            _writer.BaseStream.Seek(this._header.HeaderLength + RecNum * this._header.RecordLength, SeekOrigin.Begin);

            _writer.Write((byte)0x20); // the deleted flag
            foreach (object columnValue in columnValues)
            {
                DbaseFieldDescriptor headerField = _header.DBaseColumns[i];

                if (columnValue == null)
                    // Don't corrupt the file by not writing if the value is null.
                    // Instead, treat it like an empty string.
                    Write(string.Empty, headerField.Length);
                else if (headerField.DataType == typeof(string))
                    // If the column is a character column, the values in that
                    // column should be treated as text, even if the column value
                    // is not a string.
                    Write(columnValue.ToString(), headerField.Length);
                else if (IsRealType(columnValue.GetType()))
                {
                    decimal decValue = Convert.ToDecimal(columnValue);
                    Write(decValue, headerField.Length, headerField.DecimalCount);
                }
                else if (IsIntegerType(columnValue.GetType()))
                    Write(Convert.ToDecimal(columnValue), headerField.Length, headerField.DecimalCount);
                else if (columnValue is Decimal)
                    Write((decimal)columnValue, headerField.Length, headerField.DecimalCount);
                else if (columnValue is Boolean)
                    Write((bool)columnValue);
                else if (columnValue is string)
                    Write((string)columnValue, headerField.Length);
                else if (columnValue is DateTime)
                    Write((DateTime)columnValue);
                else if (columnValue is Char)
                    Write((Char)columnValue, headerField.Length);

                i++;
            }
        }

        /// <summary>
        /// Indicates whether a specified type is a real type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>true, if a specified type is a real type, false otherwise</returns>
        static private bool IsRealType(Type type)
        {
            return ((type == typeof(Double)) || (type == typeof(Single)));
        }

        /// <summary>
        /// Indicates whether a specified type is an integer type.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>true, if a specified type is an integer type, false otherwise</returns>
        static private bool IsIntegerType(Type type)
        {
            return ((type == typeof(Int16)) || (type == typeof(Int32)) || (type == typeof(Int64)) ||
                    (type == typeof(UInt16)) || (type == typeof(UInt32)) || (type == typeof(UInt64)));
        }

        /// <summary>
        /// Writes a decimal value.
        /// </summary>
        /// <param name="number">A value to write</param>
        /// <param name="length">A length</param>
        /// <param name="decimalCount">A number of decimal characters</param>
        public void Write(decimal number, int length, int decimalCount)
        {
            string outString = string.Empty;

            int wholeLength = length;
            if (decimalCount > 0)
                wholeLength -= (decimalCount + 1);

            // Force to use point as decimal separator
            System.Globalization.NumberFormatInfo numberFormatInfo = new System.Globalization.NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";
            string strNum = Convert.ToString(number, numberFormatInfo);// Global.GetNfi());
            int decimalIndex = strNum.IndexOf('.');
            if (decimalIndex < 0)
                decimalIndex = strNum.Length;

            if (decimalIndex > wholeLength)
            {
                // Too many digits to the left of the decimal. Use the left
                // most "wholeLength" number of digits. All values to the right
                // of the decimal will be '0'.
                StringBuilder sb = new StringBuilder();
                sb.Append(strNum.Substring(0, wholeLength));
                if (decimalCount > 0)
                {
                    sb.Append('.');
                    for (int i = 0; i < decimalCount; ++i)
                        sb.Append('0');
                }
                outString = sb.ToString();
            }
            else
            {
                // Chop extra digits to the right of the decimal.
                StringBuilder sb = new StringBuilder();
                sb.Append("{0:0");
                if (decimalCount > 0)
                {
                    sb.Append('.');
                    for (int i = 0; i < decimalCount; ++i)
                        sb.Append('0');
                }
                sb.Append('}');
                // Force to use point as decimal separator
                outString = String.Format(/*Global.GetNfi()*/numberFormatInfo, sb.ToString(), number);
            }

            //заполнение оставшегося пространства
            //for (int i = 0; i < length - outString.Length + 1; i++)
            //    _writer.Write((byte)0x20);

            _writer.BaseStream.Seek(_writer.BaseStream.Position + length - outString.Length, SeekOrigin.Begin);

            //_writer.Write(outString);
            foreach (char c in outString)
                _writer.Write(c);

            //_writer.Write((byte)0);
        }

        /// <summary>
        /// Writes a double value.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="length"></param>
        /// <param name="decimalCount"></param>
        public void Write(double number, int length, int decimalCount)
        {
            Write(Convert.ToDecimal(number), length, decimalCount);
        }

        /// <summary>
        /// Writes a float value.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="length"></param>
        /// <param name="decimalCount"></param>
        public void Write(float number, int length, int decimalCount)
        {
            Write(Convert.ToDecimal(number), length, decimalCount);
        }

        /// <summary>
        /// Writes a string value.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        public void Write(string text, int length)
        {
            string dbaseString = text;

            byte[] bytes = new byte[0];
            int k = dbaseString.Length;
            do
            {
                dbaseString = dbaseString.Substring(0, k);
                bytes = Header.Encoding.GetBytes(dbaseString);
                k--;
            } while (bytes.Length > length);

            _writer.Write(bytes);

            int extraPadding = length - bytes.Length;
            for (int i = 0; i < extraPadding; i++)
                _writer.Write((byte)0x20);
        }

        /// <summary>
        /// Writes a datetime value.
        /// </summary>
        /// <param name="date"></param>
        public void Write(DateTime date)
        {
            if (date.Year < 10) _writer.Write('0');
            if (date.Year < 100) _writer.Write('0');
            if (date.Year < 1000) _writer.Write('0');

            foreach (char c in date.Year.ToString())
                _writer.Write(c);

            if (date.Month < 10)
                _writer.Write('0');
            foreach (char c in date.Month.ToString())
                _writer.Write(c);

            if (date.Day < 10)
                _writer.Write('0');
            foreach (char c in date.Day.ToString())
                _writer.Write(c);
        }

        /// <summary>
        /// Writes a bool value.
        /// </summary>
        /// <param name="flag"></param>
        public void Write(bool flag)
        {
            _writer.Write(flag ? 'T' : 'F');
        }

        /// <summary>
        /// Writes a character.
        /// </summary>
        /// <param name="c">The character to write.</param>
        /// <param name="length">The length of the column to write in. Writes
        /// left justified, filling with spaces.</param>
        public void Write(char c, int length)
        {
            string str = string.Empty;
            str += c;
            Write(str, length);
        }

        /// <summary>
        /// Writes a byte.
        /// </summary>
        /// <param name="number">The byte.</param>
        public void Write(byte number)
        {
            _writer.Write(number);
        }

        /// <summary>
        /// Closes current record and stream.
        /// </summary>
        public void Close()
        {
            _writer.Close();
        }       

    }
}
