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
using System.Collections;
using System.IO;
using System.Text;

namespace MapAround.IO
{
    /// <summary>
    /// Represents a header of dBase-file.
    /// </summary>
    public class DbaseFileHeader
    {
        #region Declare

        #region Consts (размеры типов по умолчанию)
               

        private const int DoubleLength = 18;//18
        private const int DoubleDecimals = 8;
        private const int IntLength = 8;//8
        private const int IntDecimals = 0;
        private const int StringLength = 254;
        private const int StringDecimals = 0;
        private const int BoolLength = 1;
        private const int BoolDecimals = 0;
        private const int DateLength = 8;
        private const int DateDecimals = 0;

        #endregion

        private int _fileDescriptorSize = 32;
        private int _fileType = 0x03;
        private DateTime _updateDate;
        private int _numRecords = 0;
        private int _headerLength;
        private int _recordLength;
        private int _numFields;
        private Encoding _encoding;

        private DbaseFieldDescriptor[] _dbaseColumns;

        /// <summary>
        /// Gets an array containing 
        /// descriptions of dBase fileds.
        /// </summary>
        public DbaseFieldDescriptor[] DBaseColumns
        {
            get { return _dbaseColumns; }
        }

        /// <summary>
        /// Gets or sets an encoding which is used 
        /// to read or write string values.
        /// </summary>
        public Encoding Encoding
        {
            get { return _encoding; }
            set { _encoding = value; }
        }

        /// <summary>
        /// Gets a last update date.
        /// </summary>
        public DateTime LastUpdateDate
        {
            get
            {
                return _updateDate;
            }
        }

        /// <summary>
        /// Gets or sets a number of fields.
        /// </summary>
        public int NumFields
        {
            get { return _numFields; }
            set { _numFields = value; }
        }

        /// <summary>
        /// Gets or sets a number of recodrs.
        /// </summary>
        public int NumRecords
        {
            get { return _numRecords; }
            set { _numRecords = value; }
        }

        /// <summary>
        /// Gets a length of record.
        /// </summary>
        public int RecordLength
        {
            get
            {
                return _recordLength;
            }
        }

        /// <summary>
        /// Gets a length of header.
        /// </summary>
        public int HeaderLength
        {
            get
            {
                return _headerLength;
            }
        }        

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of MapAround.IO.DbaseFileHeader.
        /// </summary>
        public DbaseFileHeader() 
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a column.
        ///</summary>
        /// <param name="fieldName">A filed name</param>
        /// <param name="fieldType">A character defining a dBAse filed type (C N L or D)</param>
        /// <param name="fieldLength">A length of field in bytes</param>
        /// <param name="decimalCount">A number of decimal characters</param>
        /// <param name="DataType">A CLR data type</param>
        public void AddColumn(string fieldName, char fieldType, Type DataType, int fieldLength, int decimalCount)
        {
            if (fieldLength <= 0) fieldLength = 1;
            if (_dbaseColumns == null) _dbaseColumns = new DbaseFieldDescriptor[0];
            int tempLength = 1;  // the length is used for the offset, and there is a * for deleted as the first byte
            DbaseFieldDescriptor[] tempFieldDescriptors = new DbaseFieldDescriptor[_dbaseColumns.Length + 1];
            for (int i = 0; i < _dbaseColumns.Length; i++)
            {
                _dbaseColumns[i].DataAddress = tempLength;
                tempLength = tempLength + _dbaseColumns[i].Length;
                tempFieldDescriptors[i] = _dbaseColumns[i];
            }
            tempFieldDescriptors[_dbaseColumns.Length] = new DbaseFieldDescriptor();
            tempFieldDescriptors[_dbaseColumns.Length].Length = fieldLength;
            tempFieldDescriptors[_dbaseColumns.Length].DecimalCount = decimalCount;
            tempFieldDescriptors[_dbaseColumns.Length].DataAddress = tempLength;

            // set the field name
            string tempFieldName = fieldName;
            if (tempFieldName == null) tempFieldName = "NoName";
            if (tempFieldName.Length > 11)
            {
                tempFieldName = tempFieldName.Substring(0, 11);
                //!!!Trace.Write("FieldName " + fieldName + " is longer than 11 characters, truncating to " + tempFieldName);
            }
            tempFieldDescriptors[_dbaseColumns.Length].Name = tempFieldName;

            // the field type
            tempFieldDescriptors[_dbaseColumns.Length].DataType = DataType;
            if ((fieldType == 'C') || (fieldType == 'c'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'C';
            }
            else if ((fieldType == 'S') || (fieldType == 's'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'C';
                tempFieldDescriptors[_dbaseColumns.Length].Length = 8;
            }
            else if ((fieldType == 'D') || (fieldType == 'd'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'D';
                tempFieldDescriptors[_dbaseColumns.Length].Length = 8;
            }
            else if ((fieldType == 'F') || (fieldType == 'f'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'F';
            }
            else if ((fieldType == 'N') || (fieldType == 'n'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'N';
                if (decimalCount < 0)
                {
                    tempFieldDescriptors[_dbaseColumns.Length].DecimalCount = 0;
                }
                if (decimalCount > fieldLength - 1)
                {
                    tempFieldDescriptors[_dbaseColumns.Length].DecimalCount = fieldLength - 1;
                }
            }
            else if ((fieldType == 'L') || (fieldType == 'l'))
            {
                tempFieldDescriptors[_dbaseColumns.Length].DbaseType = 'L';
                tempFieldDescriptors[_dbaseColumns.Length].Length = 1;
            }
            else
            {
                throw new NotSupportedException("Unsupported field type " + fieldType + " For column " + fieldName);
            }
            // the length of a record
            tempLength = tempLength + tempFieldDescriptors[_dbaseColumns.Length].Length;

            // set the new fields.
            _dbaseColumns = tempFieldDescriptors;
            _headerLength = 33 + 32 * _dbaseColumns.Length;
            _numFields = _dbaseColumns.Length;
            _recordLength = tempLength;
        }


        /// <summary>
        /// Adds a column.
        /// </summary>
        /// <param name="fieldName">A filed name</param>
        /// <param name="dataType">A CLR data type</param>
        public void AddColumn(string fieldName, Type dataType)
        {
            if (dataType == typeof(double) || dataType == typeof(float)
                || dataType == typeof(decimal)) 
                this.AddColumn(fieldName, 'N', dataType, DoubleLength, DoubleDecimals);
            else if (dataType == typeof(short) || dataType == typeof(ushort) ||
                         dataType == typeof(int) || dataType == typeof(uint) ||
                         dataType == typeof(long) || dataType == typeof(ulong))
                this.AddColumn(fieldName, 'N', dataType, IntLength, IntDecimals);
            else if (dataType == typeof(string))
                this.AddColumn(fieldName, 'C', dataType, StringLength, StringDecimals);
            else if (dataType == typeof(bool))
                this.AddColumn(fieldName, 'L', dataType, BoolLength, BoolDecimals);
            else if (dataType == typeof(DateTime))
                this.AddColumn(fieldName, 'D', dataType, DateLength, DateDecimals);           
            else throw new ArgumentException("Type " + dataType.Name + " not supported");
        }

        /// <summary>
        /// Removes a column.
        /// </summary>
        /// <param name="fieldName">A filed name</param>
        /// <returns>An index of the removed filed, -1 if filed is not found</returns>
        public int RemoveColumn(string fieldName)
        {
            int retCol = -1;
            int tempLength = 1;
            DbaseFieldDescriptor[] tempFieldDescriptors =
                new DbaseFieldDescriptor[_dbaseColumns.Length - 1];
            for (int i = 0, j = 0; i < _dbaseColumns.Length; i++)
            {
                if (fieldName.ToLower() != (_dbaseColumns[i].Name.Trim().ToLower()))
                {
                    // if this is the last field and we still haven't found the
                    // named field
                    if (i == j && i == _dbaseColumns.Length - 1)
                        return retCol;
                    tempFieldDescriptors[j] = _dbaseColumns[i];
                    tempFieldDescriptors[j].DataAddress = tempLength;
                    tempLength += tempFieldDescriptors[j].Length;
                    // only increment j on non-matching fields
                    j++;
                }
                else retCol = i;
            }

            // set the new fields.
            _dbaseColumns = tempFieldDescriptors;
            _headerLength = 33 + 32 * _dbaseColumns.Length;
            _numFields = _dbaseColumns.Length;
            _recordLength = tempLength;

            return retCol;
        }

        /// <summary>
        /// Reads a dBase header.
        /// </summary>
        /// <param name="reader">A System.IO.BinaryReader instance to read header</param>
        public void Read(BinaryReader reader)
        {
            // type of reader.
            _fileType = reader.ReadByte();
            if (_fileType != 0x03)
                throw new NotSupportedException("Unsupported DBF Type " + _fileType);

            // parse the update date information.
            int year = (int)reader.ReadByte();
            int month = (int)reader.ReadByte();
            int day = (int)reader.ReadByte();
            _updateDate = new DateTime(year + 1900, month, day);

            // read the number of records.
            _numRecords = reader.ReadInt32();

            // read the length of the header structure.
            _headerLength = reader.ReadInt16();

            // read the length of a record
            _recordLength = reader.ReadInt16();

            // skip the reserved bytes in the header.
            //in.skipBytes(20);
            //reader.ReadBytes(20);
            reader.BaseStream.Seek(29, SeekOrigin.Begin);

            //языковой драйвер
            _encoding = getDbaseLanguageDriver(reader.ReadByte());           
            
            reader.BaseStream.Seek(32, SeekOrigin.Begin);

            // calculate the number of Fields in the header
            _numFields = (_headerLength - _fileDescriptorSize - 1) / _fileDescriptorSize;

            // read all of the header records
            _dbaseColumns = new DbaseFieldDescriptor[_numFields];

            for (int i = 0; i < _numFields; i++)
            {
                _dbaseColumns[i] = new DbaseFieldDescriptor();

                // read the field name				
                byte[] buffer = reader.ReadBytes(11);
                string name = _encoding.GetString(buffer);

                if (name.Contains("\0"))
                    name = name.Substring(0, name.IndexOf('\0'));

                name = name.Replace("\0", "").Trim();

                int nullPoint = name.IndexOf((char)0);
                if (nullPoint != -1)
                    name = name.Substring(0, nullPoint);
                _dbaseColumns[i].Name = name;

                // read the field type
                _dbaseColumns[i].DbaseType = (char)reader.ReadByte();
                _dbaseColumns[i].DataType = DbaseFieldDescriptor.GetDataType(_dbaseColumns[i].DbaseType);

                // read the field data address, offset from the start of the record.
                _dbaseColumns[i].DataAddress = reader.ReadInt32();

                // read the field length in bytes
                int tempLength = (int)reader.ReadByte();
                if (tempLength < 0) tempLength = tempLength + 256;
                _dbaseColumns[i].Length = tempLength;

                // read the field decimal count in bytes
                _dbaseColumns[i].DecimalCount = (int)reader.ReadByte();
                if (_dbaseColumns[i].DecimalCount == 0 && _dbaseColumns[i].DataType == typeof(double))
                    if (_dbaseColumns[i].Length <= 2)
                        _dbaseColumns[i].DataType = typeof(Int16);
                    else if (_dbaseColumns[i].Length <= 4)
                        _dbaseColumns[i].DataType = typeof(Int32);
                    else
                        _dbaseColumns[i].DataType = typeof(Int64);

                // read the reserved bytes.
                //reader.skipBytes(14);
                reader.ReadBytes(14);
            }

            // Last byte is a marker for the end of the field definitions.
            reader.ReadBytes(1);
        }       

        

        /// <summary>
        /// Writes a dBase header into stream.
        /// </summary>
        /// <param name="writer">A System.IO.BinaryWriter instance to write header</param>
        public void Write(BinaryWriter writer)
        {
            // write the output file type.
            writer.Write((byte)_fileType);

            if (this._updateDate == default(DateTime))
                this._updateDate = DateTime.Now;

            writer.Write((byte)(_updateDate.Year - 1900));
            writer.Write((byte)_updateDate.Month);
            writer.Write((byte)_updateDate.Day);

            // write the number of records in the datafile.
            writer.Write(_numRecords);

            // write the length of the header structure.
            writer.Write((short)_headerLength);

            // write the length of a record
            writer.Write((short)_recordLength);

            #region Old
            //// write the reserved bytes in the header
            //for (int i = 0; i < 20; i++)
            //    writer.Write((byte)0);

            #endregion

            #region New

            // write the reserved bytes in the header
            //for (int i = 0; i < 17; i++)
            //    writer.Write((byte)0);
            writer.BaseStream.Seek(29, SeekOrigin.Begin);

            //encoding
            writer.Write(this.getDbaseLanguageDriver(_encoding.CodePage));

            //for (int i = 0; i < 2; i++)
            //    writer.Write((byte)0);
            writer.BaseStream.Seek(32, SeekOrigin.Begin);

            #endregion

            // write all of the header records
            int tempOffset = 0;
            for (int i = 0; i < _dbaseColumns.Length; i++)
            {
                // write the field name
                for (int j = 0; j < 11; j++)
                {
                    if (_dbaseColumns[i].Name.Length > j)
                        writer.Write((byte)_dbaseColumns[i].Name[j]);
                    else writer.Write((byte)0);
                }

                // write the field type
                writer.Write((char)_dbaseColumns[i].DbaseType);

                // write the field data address, offset from the start of the record.
                //!!!
                writer.Write(0);
                tempOffset += _dbaseColumns[i].Length;

                // write the length of the field.
                writer.Write((byte)_dbaseColumns[i].Length);

                // write the decimal count.
                writer.Write((byte)_dbaseColumns[i].DecimalCount);

                // write the reserved bytes.
                //for (int j = 0; j < 14; j++) writer.Write((byte)0);
                //!!!!
                writer.BaseStream.Seek(14, SeekOrigin.Current);
            }

            // write the end of the field definitions marker
            writer.Write((byte)0x0D);
        }

        /// <summary>
        /// Computes a maximum length of field taking into account all the values.
        /// </summary>
        /// <param name="field">A descriptor of field</param>
        /// <param name="columnValues">A field value enumerator</param>
        public void RecountColumnLength(DbaseFieldDescriptor field, IEnumerable columnValues)
        {
            int size = 0;
            int decimalCount = 0;

            System.Globalization.NumberFormatInfo numberFormatInfo = new System.Globalization.NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";

            foreach (object value in columnValues)
            {
                if (field.DataType == typeof(string))
                {
                    string svalue = Convert.ToString(value);
                    int s = this.Encoding.GetBytes(svalue).Length;

                    if (s > size) size = s;
                }
                //else if (field.DataType == typeof(Int16) || field.DataType == typeof(Int32) || field.DataType == typeof(Int64)
                //         || field.DataType == typeof(UInt16) || field.DataType == typeof(UInt32) || field.DataType == typeof(UInt64))
                //{
                //    size = System.Runtime.InteropServices.Marshal.SizeOf(field.DataType);
                //    decimalCount = 0;
                //}
                else if (field.DataType == typeof(Double) ||field.DataType == typeof(Single) || field.DataType == typeof(Decimal) ||
                         field.DataType == typeof(Int16) || field.DataType == typeof(Int32) || field.DataType == typeof(Int64)
                         || field.DataType == typeof(UInt16) || field.DataType == typeof(UInt32) || field.DataType == typeof(UInt64))
                {
                    string svalue = Convert.ToString(value, numberFormatInfo);
                    if (svalue.Length > size) size = svalue.Length;
                    int decimalSepIndex = svalue.IndexOf(".");
                    if (decimalSepIndex != -1)
                    {
                        if (svalue.Length - decimalSepIndex - 1 > decimalCount)
                            decimalCount = Math.Max(decimalCount, svalue.Length - decimalSepIndex - 1);
                    }
                    //else if (decimalCount == 0)
                    //{
                    //    size += 3; // с учетом разделителя
                    //    decimalCount = 2;
                    //}
                }
            }

            if (field.DataType == typeof(Double) || field.DataType == typeof(Single) || field.DataType == typeof(Decimal))
            {
                if (decimalCount == 0)
                {
                    size += 3; // с учетом разделителя
                    decimalCount = 2;
                }
            }

            if (size != 0)
            {
                this._recordLength = this._recordLength - field.Length + size;
                field.Length = size;
                if (decimalCount != 0) field.DecimalCount = decimalCount;
            }
        }

        private byte getDbaseLanguageDriver(int dbasecode)
        {
            switch (dbasecode)
            {
                case 437: return 0x01; //DOS USA code page 437 
                case 850: return 0x02; // DOS Multilingual code page 850 
                case 1252: return 0x03; // Windows ANSI code page 1252 
                case 10000: return 0x04; // Standard Macintosh 
                case 865: return 0x08; // Danish OEM
                //case 437: return 0x09; // Dutch OEM
                //case 850: return 0x0A; // Dutch OEM Secondary codepage
                //case 437 : return 0x0B; // Finnish OEM
                //case 437: return 0x0D; // French OEM
                //case 0x0E: return 850); // French OEM Secondary codepage
                //case 0x0F: return 437); // German OEM
                //case 0x10: return 850); // German OEM Secondary codepage
                //case 0x11: return 437); // Italian OEM
                //case 0x12: return 850); // Italian OEM Secondary codepage
                case 932: return 0x13; // Japanese Shift-JIS
                //case 0x14: return 850); // Spanish OEM secondary codepage
                //case 0x15: return 437); // Swedish OEM
                //case 0x16: return 850); // Swedish OEM secondary codepage
                //case 0x17: return 865); // Norwegian OEM
                //case 0x18: return 437); // Spanish OEM
                //case 0x19: return 437); // English OEM (Britain)
                //case 0x1A: return 850); // English OEM (Britain) secondary codepage
                //case 0x1B: return 437); // English OEM (U.S.)
                case 863: return 0x1C; // French OEM (Canada)
                //case 0x1D: return 850); // French OEM secondary codepage
                case 852: return 0x1F; // Czech OEM
                //case 0x22: return 852); // Hungarian OEM
                //case 0x23: return 852); // Polish OEM
                case 860: return 0x24; // Portuguese OEM
                //case 0x25: return 850); // Portuguese OEM secondary codepage
                case 866: return 0x26; // Russian OEM
                //case 0x37: return 850); // English OEM (U.S.) secondary codepage
                //case 0x40: return 852); // Romanian OEM
                case 936: return 0x4D; // Chinese GBK (PRC)
                case 949: return 0x4E; // Korean (ANSI/OEM)
                case 950: return 0x4F; // Chinese Big5 (Taiwan)
                case 874: return 0x50; // Thai (ANSI/OEM)
                //case 1252: return 0x57 ; // ANSI
                //case 0x58: return 1252); // Western European ANSI
                //case 0x59: return 1252); // Spanish ANSI
                //case 0x64: return 852); // Eastern European MS–DOS
                //case 0x65: return 866); // Russian MS–DOS
                //case 0x66: return 865); // Nordic MS–DOS
                case 861: return 0x67; // Icelandic MS–DOS
                case 895: return 0x68; // Kamenicky (Czech) MS-DOS 
                case 620: return 0x69; // Mazovia (Polish) MS-DOS 
                case 737: return 0x6A; // Greek MS–DOS (437G)
                case 857: return 0x6B; // Turkish MS–DOS
                //case 0x6C: return 863); // French–Canadian MS–DOS
                //case 0x78: return 950); // Taiwan Big 5
                //case 0x79: return 949); // Hangul (Wansung)
                //case 0x7A: return 936); // PRC GBK
                //case 0x7B: return 932); // Japanese Shift-JIS
                //case 0x7C: return 874); // Thai Windows/MS–DOS
                case 1255: return 0x7D; // Hebrew Windows 
                case 1256: return 0x7E; // Arabic Windows 
                //case 0x86: return 737); // Greek OEM
                //case 0x87: return 852); // Slovenian OEM
                //case 0x88: return 857); // Turkish OEM
                case 10007: return 0x96; // Russian Macintosh 
                case 10029: return 0x97; // Eastern European Macintosh 
                case 10006: return 0x98; // Greek Macintosh 
                case 1250: return 0xC8; // Eastern European Windows
                case 1251: return 0xC9; // Russian Windows
                case 1254: return 0xCA; // Turkish Windows
                case 1253: return 0xCB; // Greek Windows
                case 1257: return 0xCC; // Baltic Windows                
                default:
                    return (byte)0;//System.Text.Encoding.UTF8;
            }
        }

        private Encoding getDbaseLanguageDriver(byte dbasecode)
        {
            switch (dbasecode)
            {
                case 0x01: return System.Text.Encoding.GetEncoding(437); //DOS USA code page 437 
                case 0x02: return System.Text.Encoding.GetEncoding(850); // DOS Multilingual code page 850 
                case 0x03: return System.Text.Encoding.GetEncoding(1252); // Windows ANSI code page 1252 
                case 0x04: return System.Text.Encoding.GetEncoding(10000); // Standard Macintosh 
                case 0x08: return System.Text.Encoding.GetEncoding(865); // Danish OEM
                case 0x09: return System.Text.Encoding.GetEncoding(437); // Dutch OEM
                case 0x0A: return System.Text.Encoding.GetEncoding(850); // Dutch OEM Secondary codepage
                case 0x0B: return System.Text.Encoding.GetEncoding(437); // Finnish OEM
                case 0x0D: return System.Text.Encoding.GetEncoding(437); // French OEM
                case 0x0E: return System.Text.Encoding.GetEncoding(850); // French OEM Secondary codepage
                case 0x0F: return System.Text.Encoding.GetEncoding(437); // German OEM
                case 0x10: return System.Text.Encoding.GetEncoding(850); // German OEM Secondary codepage
                case 0x11: return System.Text.Encoding.GetEncoding(437); // Italian OEM
                case 0x12: return System.Text.Encoding.GetEncoding(850); // Italian OEM Secondary codepage
                case 0x13: return System.Text.Encoding.GetEncoding(932); // Japanese Shift-JIS
                case 0x14: return System.Text.Encoding.GetEncoding(850); // Spanish OEM secondary codepage
                case 0x15: return System.Text.Encoding.GetEncoding(437); // Swedish OEM
                case 0x16: return System.Text.Encoding.GetEncoding(850); // Swedish OEM secondary codepage
                case 0x17: return System.Text.Encoding.GetEncoding(865); // Norwegian OEM
                case 0x18: return System.Text.Encoding.GetEncoding(437); // Spanish OEM
                case 0x19: return System.Text.Encoding.GetEncoding(437); // English OEM (Britain)
                case 0x1A: return System.Text.Encoding.GetEncoding(850); // English OEM (Britain) secondary codepage
                case 0x1B: return System.Text.Encoding.GetEncoding(437); // English OEM (U.S.)
                case 0x1C: return System.Text.Encoding.GetEncoding(863); // French OEM (Canada)
                case 0x1D: return System.Text.Encoding.GetEncoding(850); // French OEM secondary codepage
                case 0x1F: return System.Text.Encoding.GetEncoding(852); // Czech OEM
                case 0x22: return System.Text.Encoding.GetEncoding(852); // Hungarian OEM
                case 0x23: return System.Text.Encoding.GetEncoding(852); // Polish OEM
                case 0x24: return System.Text.Encoding.GetEncoding(860); // Portuguese OEM
                case 0x25: return System.Text.Encoding.GetEncoding(850); // Portuguese OEM secondary codepage
                case 0x26: return System.Text.Encoding.GetEncoding(866); // Russian OEM
                case 0x37: return System.Text.Encoding.GetEncoding(850); // English OEM (U.S.) secondary codepage
                case 0x40: return System.Text.Encoding.GetEncoding(852); // Romanian OEM
                case 0x4D: return System.Text.Encoding.GetEncoding(936); // Chinese GBK (PRC)
                case 0x4E: return System.Text.Encoding.GetEncoding(949); // Korean (ANSI/OEM)
                case 0x4F: return System.Text.Encoding.GetEncoding(950); // Chinese Big5 (Taiwan)
                case 0x50: return System.Text.Encoding.GetEncoding(874); // Thai (ANSI/OEM)
                case 0x57: return System.Text.Encoding.GetEncoding(1252); // ANSI
                case 0x58: return System.Text.Encoding.GetEncoding(1252); // Western European ANSI
                case 0x59: return System.Text.Encoding.GetEncoding(1252); // Spanish ANSI
                case 0x64: return System.Text.Encoding.GetEncoding(852); // Eastern European MS–DOS
                case 0x65: return System.Text.Encoding.GetEncoding(866); // Russian MS–DOS
                case 0x66: return System.Text.Encoding.GetEncoding(865); // Nordic MS–DOS
                case 0x67: return System.Text.Encoding.GetEncoding(861); // Icelandic MS–DOS
                case 0x68: return System.Text.Encoding.GetEncoding(895); // Kamenicky (Czech) MS-DOS 
                case 0x69: return System.Text.Encoding.GetEncoding(620); // Mazovia (Polish) MS-DOS 
                case 0x6A: return System.Text.Encoding.GetEncoding(737); // Greek MS–DOS (437G)
                case 0x6B: return System.Text.Encoding.GetEncoding(857); // Turkish MS–DOS
                case 0x6C: return System.Text.Encoding.GetEncoding(863); // French–Canadian MS–DOS
                case 0x78: return System.Text.Encoding.GetEncoding(950); // Taiwan Big 5
                case 0x79: return System.Text.Encoding.GetEncoding(949); // Hangul (Wansung)
                case 0x7A: return System.Text.Encoding.GetEncoding(936); // PRC GBK
                case 0x7B: return System.Text.Encoding.GetEncoding(932); // Japanese Shift-JIS
                case 0x7C: return System.Text.Encoding.GetEncoding(874); // Thai Windows/MS–DOS
                case 0x7D: return System.Text.Encoding.GetEncoding(1255); // Hebrew Windows 
                case 0x7E: return System.Text.Encoding.GetEncoding(1256); // Arabic Windows 
                case 0x86: return System.Text.Encoding.GetEncoding(737); // Greek OEM
                case 0x87: return System.Text.Encoding.GetEncoding(852); // Slovenian OEM
                case 0x88: return System.Text.Encoding.GetEncoding(857); // Turkish OEM
                case 0x96: return System.Text.Encoding.GetEncoding(10007); // Russian Macintosh 
                case 0x97: return System.Text.Encoding.GetEncoding(10029); // Eastern European Macintosh 
                case 0x98: return System.Text.Encoding.GetEncoding(10006); // Greek Macintosh 
                case 0xC8: return System.Text.Encoding.GetEncoding(1250); // Eastern European Windows
                case 0xC9: return System.Text.Encoding.GetEncoding(1251); // Russian Windows
                case 0xCA: return System.Text.Encoding.GetEncoding(1254); // Turkish Windows
                case 0xCB: return System.Text.Encoding.GetEncoding(1253); // Greek Windows
                case 0xCC: return System.Text.Encoding.GetEncoding(1257); // Baltic Windows
                default:
                    return System.Text.Encoding.UTF8;
            }
        }

        #endregion
    }

}
