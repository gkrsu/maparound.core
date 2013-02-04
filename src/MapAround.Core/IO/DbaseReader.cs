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



﻿/*===========================================================================
** 
** File: DbaseReader.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Reading of dBase files (mainly for reading shape-files attribute table)
**
=============================================================================*/

namespace MapAround.IO
{
    using System;
    using System.Text;
    using System.IO;
    using System.Data;
    using System.Globalization;

    /// <summary>
    /// Reads dBase-files.
    /// </summary>
    internal class DbaseReader : IDisposable
    {
        private static NumberFormatInfo _numberFormat = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

        //private struct DbaseField
        //{
        //    public string ColumnName;
        //    public Type DataType;
        //    public int Address;
        //    public int Length;
        //    public int Decimals;
        //}

        private DbaseFileHeader _dbaseHeader;



        //private DateTime _lastUpdate;
        //private int _numberOfRecords;
        //private Int16 _headerLength;
        //private Int16 _recordLength;
        private string _filename;
       // private DbaseFieldDescriptor[] _dbaseColumns;
        private FileStream _fs;
        private BinaryReader _br;
        private bool _headerIsParsed;
        private DataTable _baseTable;
        //private System.Text.Encoding _encoding;
        //private System.Text.Encoding _fileEncoding;


        public DbaseFileHeader DbaseHeader
        {
            get { return _dbaseHeader; }
            set { _dbaseHeader = value; }
        }
        public DbaseReader(string filename)
        {

            if (!File.Exists(filename))
                throw new FileNotFoundException(String.Format("File \"{0}\" not found", filename));
            _filename = filename;
            _headerIsParsed = false;
            _dbaseHeader = new DbaseFileHeader();
        }

        private bool _isOpen;

        public bool IsOpen
        {
            get { return _isOpen; }
            set { _isOpen = value; }
        }

        /// <summary>
        /// Opens a dBase file for reading.
        /// </summary>
        public void Open()
        {
            _fs = new FileStream(_filename, FileMode.Open, FileAccess.Read);
            _br = new BinaryReader(_fs);
            _isOpen = true;
            if (!_headerIsParsed) //Не парсим заголовок, если его уже распарсили            
            {
                //parseDbfHeader(_filename);
                _dbaseHeader.Read(_br);
                _headerIsParsed = true;
                сreateBaseTable();
            }
        }

        /// <summary>
        /// Closes a dBase file.
        /// </summary>
        public void Close()
        {
            _br.Close();
            _fs.Close();
            _isOpen = false;
        }

        public void Dispose()
        {
            if (_isOpen)
                this.Close();
            _br = null;
            _fs = null;
        }

        ///// <summary>
        ///// Получачет дату последнего изменения файла
        ///// </summary>
        //public DateTime LastUpdate
        //{
        //    get { return _lastUpdate; }
        //}

        //public byte ReadByte()
        //{
        //    return this._br.ReadByte();
        //}

        //private void parseDbfHeader(string filename)
        //{
        //    if (_br.ReadByte() != 0x03)
        //        throw new NotSupportedException("Unsupported dbf type");

        //    _lastUpdate = new DateTime((int)_br.ReadByte() + 1900, (int)_br.ReadByte(), (int)_br.ReadByte()); //Read the last update date
        //    _numberOfRecords = _br.ReadInt32(); // количество записей.
        //    _headerLength = _br.ReadInt16(); // длина заголовка.
        //    _recordLength = _br.ReadInt16(); // длина записи
        //    _fs.Seek(29, SeekOrigin.Begin); // поиск флага кодировки
        //    _fileEncoding = getDbaseLanguageDriver(_br.ReadByte());   //(_br.ReadInt32()); //языковой драйвер
        //    _fs.Seek(32, SeekOrigin.Begin); //пропуск зарезервированного пространства

        //    int NumberOfColumns = (_headerLength - 31) / 32;  // вычисление кол-ва столбцов в заголовке
        //    _dbaseColumns = new DbaseField[NumberOfColumns];
        //    for (int i = 0; i < _dbaseColumns.Length; i++)
        //    {
        //        _dbaseColumns[i] = new DbaseField();
        //        _dbaseColumns[i].ColumnName = System.Text.Encoding.UTF7.GetString((_br.ReadBytes(11))).Replace("\0", "").Trim();
        //        char fieldtype = _br.ReadChar();
        //        switch (fieldtype)
        //        {
        //            case 'L': _dbaseColumns[i].DataType = typeof(bool);
        //                break;
        //            case 'C': _dbaseColumns[i].DataType = typeof(string);
        //                break;
        //            case 'D': _dbaseColumns[i].DataType = typeof(DateTime);
        //                break;
        //            case 'N': _dbaseColumns[i].DataType = typeof(double);
        //                break;
        //            case 'F': _dbaseColumns[i].DataType = typeof(float);
        //                break;
        //            case 'B': _dbaseColumns[i].DataType = typeof(byte[]);
        //                break;
        //            default:
        //                throw new NotSupportedException("Unknown field type '" + fieldtype +
        //                        "' field name '" + _dbaseColumns[i].ColumnName + "'");
        //        }
        //        _dbaseColumns[i].Address = _br.ReadInt32();

        //        int Length = (int)_br.ReadByte();
        //        if (Length < 0) Length = Length + 256;
        //        _dbaseColumns[i].Length = Length;
        //        _dbaseColumns[i].Decimals = (int)_br.ReadByte();

        //        if (_dbaseColumns[i].Decimals == 0 && _dbaseColumns[i].DataType == typeof(double))
        //            if (_dbaseColumns[i].Length <= 2)
        //                _dbaseColumns[i].DataType = typeof(Int16);
        //            else if (_dbaseColumns[i].Length <= 4)
        //                _dbaseColumns[i].DataType = typeof(Int32);
        //            else
        //                _dbaseColumns[i].DataType = typeof(Int64);
        //        _fs.Seek(_fs.Position + 14, 0);
        //    }
        //    _headerIsParsed = true;
        //    сreateBaseTable();
        //}

        //private Encoding getDbaseLanguageDriver(byte dbasecode)
        //{
        //    switch (dbasecode)
        //    {
        //        case 0x01: return System.Text.Encoding.GetEncoding(437); //DOS USA code page 437 
        //        case 0x02: return System.Text.Encoding.GetEncoding(850); // DOS Multilingual code page 850 
        //        case 0x03: return System.Text.Encoding.GetEncoding(1252); // Windows ANSI code page 1252 
        //        case 0x04: return System.Text.Encoding.GetEncoding(10000); // Standard Macintosh 
        //        case 0x08: return System.Text.Encoding.GetEncoding(865); // Danish OEM
        //        case 0x09: return System.Text.Encoding.GetEncoding(437); // Dutch OEM
        //        case 0x0A: return System.Text.Encoding.GetEncoding(850); // Dutch OEM Secondary codepage
        //        case 0x0B: return System.Text.Encoding.GetEncoding(437); // Finnish OEM
        //        case 0x0D: return System.Text.Encoding.GetEncoding(437); // French OEM
        //        case 0x0E: return System.Text.Encoding.GetEncoding(850); // French OEM Secondary codepage
        //        case 0x0F: return System.Text.Encoding.GetEncoding(437); // German OEM
        //        case 0x10: return System.Text.Encoding.GetEncoding(850); // German OEM Secondary codepage
        //        case 0x11: return System.Text.Encoding.GetEncoding(437); // Italian OEM
        //        case 0x12: return System.Text.Encoding.GetEncoding(850); // Italian OEM Secondary codepage
        //        case 0x13: return System.Text.Encoding.GetEncoding(932); // Japanese Shift-JIS
        //        case 0x14: return System.Text.Encoding.GetEncoding(850); // Spanish OEM secondary codepage
        //        case 0x15: return System.Text.Encoding.GetEncoding(437); // Swedish OEM
        //        case 0x16: return System.Text.Encoding.GetEncoding(850); // Swedish OEM secondary codepage
        //        case 0x17: return System.Text.Encoding.GetEncoding(865); // Norwegian OEM
        //        case 0x18: return System.Text.Encoding.GetEncoding(437); // Spanish OEM
        //        case 0x19: return System.Text.Encoding.GetEncoding(437); // English OEM (Britain)
        //        case 0x1A: return System.Text.Encoding.GetEncoding(850); // English OEM (Britain) secondary codepage
        //        case 0x1B: return System.Text.Encoding.GetEncoding(437); // English OEM (U.S.)
        //        case 0x1C: return System.Text.Encoding.GetEncoding(863); // French OEM (Canada)
        //        case 0x1D: return System.Text.Encoding.GetEncoding(850); // French OEM secondary codepage
        //        case 0x1F: return System.Text.Encoding.GetEncoding(852); // Czech OEM
        //        case 0x22: return System.Text.Encoding.GetEncoding(852); // Hungarian OEM
        //        case 0x23: return System.Text.Encoding.GetEncoding(852); // Polish OEM
        //        case 0x24: return System.Text.Encoding.GetEncoding(860); // Portuguese OEM
        //        case 0x25: return System.Text.Encoding.GetEncoding(850); // Portuguese OEM secondary codepage
        //        case 0x26: return System.Text.Encoding.GetEncoding(866); // Russian OEM
        //        case 0x37: return System.Text.Encoding.GetEncoding(850); // English OEM (U.S.) secondary codepage
        //        case 0x40: return System.Text.Encoding.GetEncoding(852); // Romanian OEM
        //        case 0x4D: return System.Text.Encoding.GetEncoding(936); // Chinese GBK (PRC)
        //        case 0x4E: return System.Text.Encoding.GetEncoding(949); // Korean (ANSI/OEM)
        //        case 0x4F: return System.Text.Encoding.GetEncoding(950); // Chinese Big5 (Taiwan)
        //        case 0x50: return System.Text.Encoding.GetEncoding(874); // Thai (ANSI/OEM)
        //        case 0x57: return System.Text.Encoding.GetEncoding(1252); // ANSI
        //        case 0x58: return System.Text.Encoding.GetEncoding(1252); // Western European ANSI
        //        case 0x59: return System.Text.Encoding.GetEncoding(1252); // Spanish ANSI
        //        case 0x64: return System.Text.Encoding.GetEncoding(852); // Eastern European MS–DOS
        //        case 0x65: return System.Text.Encoding.GetEncoding(866); // Russian MS–DOS
        //        case 0x66: return System.Text.Encoding.GetEncoding(865); // Nordic MS–DOS
        //        case 0x67: return System.Text.Encoding.GetEncoding(861); // Icelandic MS–DOS
        //        case 0x68: return System.Text.Encoding.GetEncoding(895); // Kamenicky (Czech) MS-DOS 
        //        case 0x69: return System.Text.Encoding.GetEncoding(620); // Mazovia (Polish) MS-DOS 
        //        case 0x6A: return System.Text.Encoding.GetEncoding(737); // Greek MS–DOS (437G)
        //        case 0x6B: return System.Text.Encoding.GetEncoding(857); // Turkish MS–DOS
        //        case 0x6C: return System.Text.Encoding.GetEncoding(863); // French–Canadian MS–DOS
        //        case 0x78: return System.Text.Encoding.GetEncoding(950); // Taiwan Big 5
        //        case 0x79: return System.Text.Encoding.GetEncoding(949); // Hangul (Wansung)
        //        case 0x7A: return System.Text.Encoding.GetEncoding(936); // PRC GBK
        //        case 0x7B: return System.Text.Encoding.GetEncoding(932); // Japanese Shift-JIS
        //        case 0x7C: return System.Text.Encoding.GetEncoding(874); // Thai Windows/MS–DOS
        //        case 0x7D: return System.Text.Encoding.GetEncoding(1255); // Hebrew Windows 
        //        case 0x7E: return System.Text.Encoding.GetEncoding(1256); // Arabic Windows 
        //        case 0x86: return System.Text.Encoding.GetEncoding(737); // Greek OEM
        //        case 0x87: return System.Text.Encoding.GetEncoding(852); // Slovenian OEM
        //        case 0x88: return System.Text.Encoding.GetEncoding(857); // Turkish OEM
        //        case 0x96: return System.Text.Encoding.GetEncoding(10007); // Russian Macintosh 
        //        case 0x97: return System.Text.Encoding.GetEncoding(10029); // Eastern European Macintosh 
        //        case 0x98: return System.Text.Encoding.GetEncoding(10006); // Greek Macintosh 
        //        case 0xC8: return System.Text.Encoding.GetEncoding(1250); // Eastern European Windows
        //        case 0xC9: return System.Text.Encoding.GetEncoding(1251); // Russian Windows
        //        case 0xCA: return System.Text.Encoding.GetEncoding(1254); // Turkish Windows
        //        case 0xCB: return System.Text.Encoding.GetEncoding(1253); // Greek Windows
        //        case 0xCC: return System.Text.Encoding.GetEncoding(1257); // Baltic Windows
        //        default:
        //            return System.Text.Encoding.UTF7;
        //    }
        //}

        /// <summary>
        /// Generates a System.Data.DataTable instance 
        /// containing the schema of dBase file.
        /// </summary>
        /// <returns>A System.Data.DataTable instance 
        /// containing the schema of dBase file.</returns>
        public DataTable GetSchemaTable()
        {
            DataTable tab = new DataTable();
            // all of common, non "base-table" fields implemented
            tab.Columns.Add("ColumnName", typeof(System.String));
            tab.Columns.Add("ColumnSize", typeof(Int32));
            tab.Columns.Add("ColumnOrdinal", typeof(Int32));
            tab.Columns.Add("NumericPrecision", typeof(Int16));
            tab.Columns.Add("NumericScale", typeof(Int16));
            tab.Columns.Add("DataType", typeof(System.Type));
            tab.Columns.Add("AllowDBNull", typeof(bool));
            tab.Columns.Add("IsReadOnly", typeof(bool));
            tab.Columns.Add("IsUnique", typeof(bool));
            tab.Columns.Add("IsRowVersion", typeof(bool));
            tab.Columns.Add("IsKey", typeof(bool));
            tab.Columns.Add("IsAutoIncrement", typeof(bool));
            tab.Columns.Add("IsLong", typeof(bool));

            foreach (DbaseFieldDescriptor dbf in _dbaseHeader.DBaseColumns)
                tab.Columns.Add(dbf.Name, dbf.DataType);

            for (int i = 0; i < _dbaseHeader.DBaseColumns.Length; i++)
            {
                DataRow r = tab.NewRow();
                r["ColumnName"] = _dbaseHeader.DBaseColumns[i].Name;
                r["ColumnSize"] = _dbaseHeader.DBaseColumns[i].Length;
                r["ColumnOrdinal"] = i;
                r["NumericPrecision"] = _dbaseHeader.DBaseColumns[i].DecimalCount;
                r["NumericScale"] = 0;
                r["DataType"] = _dbaseHeader.DBaseColumns[i].DataType;
                r["AllowDBNull"] = true;
                r["IsReadOnly"] = true;
                r["IsUnique"] = false;
                r["IsRowVersion"] = false;
                r["IsKey"] = false;
                r["IsAutoIncrement"] = false;
                r["IsLong"] = false;

                tab.Rows.Add(r);
            }

            return tab;
        }

        private void сreateBaseTable()
        {
            _baseTable = new DataTable();
            foreach (DbaseFieldDescriptor dbf in _dbaseHeader.DBaseColumns)
                _baseTable.Columns.Add(dbf.Name, dbf.DataType);
        }

        /// <summary>
        /// Creates a new System.Data.DataTable instance 
        /// to filling with dBase data.
        /// </summary> 
        internal DataTable NewTable
        {
            get { return _baseTable.Clone(); }
        }

        internal object GetValue(uint oid, int colid)
        {
            if (!_isOpen)
                throw (new ApplicationException("Unable to read the closed dbf-file"));
            if (oid >= _dbaseHeader.NumRecords)
                throw (new ArgumentException("Incorrect record identity " + oid.ToString()));
            if (colid >= _dbaseHeader.DBaseColumns.Length || colid < 0)
                throw ((new ArgumentOutOfRangeException("Column index out of range", "colid")));

            _fs.Seek(_dbaseHeader.HeaderLength + oid * _dbaseHeader.RecordLength, 0);
            for (int i = 0; i < colid; i++)
                _br.BaseStream.Seek(_dbaseHeader.DBaseColumns[i].Length, SeekOrigin.Current);

            return readDbfValue(_dbaseHeader.DBaseColumns[colid]);
        }

        ///// <summary>
        ///// Получает или устанавливает кодировку используемую для интерпретации записей dbf-файла
        ///// </summary>
        ///// <remarks>
        ///// если не задана, будет предпринята попытка определить корректную кодировку по языковому драйверу
        ///// </remarks>
        //public Encoding Encoding
        //{
        //    get { return _encoding; }
        //    set { _encoding = value; }
        //}

        /// <summary>
        /// Gets a dBase row.
        /// </summary>
        /// <param name="oid">A row id value</param>
        /// <param name="table">A System.Data.DataTable instance containing dBase data</param>
        /// <returns>A data row</returns>
        internal DataRow GetRow(uint oid, DataTable table)
        {
            if (!_isOpen)
                throw new ApplicationException("Unable to read the closed dbf-file");
            if (oid >= _dbaseHeader.NumRecords)
                throw new ArgumentOutOfRangeException("Incorrect record identity", "oid");
            _fs.Seek(_dbaseHeader.HeaderLength + oid * _dbaseHeader.RecordLength, 0);

            DataRow dr = table.NewRow();

            if (_br.ReadChar() == '*') //is record marked as deleted?
                return null;

            for (int i = 0; i < _dbaseHeader.DBaseColumns.Length; i++)
            {
                DbaseFieldDescriptor dbf = _dbaseHeader.DBaseColumns[i];
                dr[dbf.Name] = readDbfValue(dbf);
            }
            return dr;
        }

        private object readDbfValue(DbaseFieldDescriptor dbf)
        {
            byte[] bytes = _br.ReadBytes(dbf.Length);

            switch (dbf.DataType.ToString())
            {
                case "System.String":
                    //if (_encoding == null)
                    //    return _fileEncoding.GetString(bytes).Replace("\0", "").Trim();
                    //else
                        return _dbaseHeader.Encoding.GetString(bytes).Replace("\0", "").Trim();

                case "System.Double":
                    string temp = System.Text.Encoding.UTF7.GetString(bytes).Replace("\0", "").Trim();
                    double dbl = 0;
                    if (double.TryParse(temp, System.Globalization.NumberStyles.Float, _numberFormat, out dbl))
                        return dbl;
                    else
                        return DBNull.Value;

                case "System.Int16":
                    string temp16 = System.Text.Encoding.UTF7.GetString((bytes)).Replace("\0", "").Trim();
                    Int16 i16 = 0;
                    if (Int16.TryParse(temp16, System.Globalization.NumberStyles.Float, _numberFormat, out i16))
                        return i16;
                    else
                        return DBNull.Value;

                case "System.Int32":
                    string temp32 = System.Text.Encoding.UTF7.GetString((bytes)).Replace("\0", "").Trim();
                    Int32 i32 = 0;
                    if (Int32.TryParse(temp32, System.Globalization.NumberStyles.Float, _numberFormat, out i32))
                        return i32;
                    else
                        return DBNull.Value;

                case "System.Int64":
                    string temp64 = System.Text.Encoding.UTF7.GetString((bytes)).Replace("\0", "").Trim();
                    Int64 i64 = 0;
                    if (Int64.TryParse(temp64, System.Globalization.NumberStyles.Float, _numberFormat, out i64))
                        return i64;
                    else
                        return DBNull.Value;

                case "System.Single":
                    string temp4 = System.Text.Encoding.UTF8.GetString((bytes));
                    float f = 0;
                    if (float.TryParse(temp4, System.Globalization.NumberStyles.Float, _numberFormat, out f))
                        return f;
                    else
                        return DBNull.Value;

                case "System.Boolean":
                    char tempChar = Convert.ToChar(bytes[0]);// BitConverter.ToChar(bytes, 0);// _br.ReadChar();
                    return ((tempChar == 'T') || (tempChar == 't') || (tempChar == 'Y') || (tempChar == 'y'));

                case "System.DateTime":
#if !MONO
                    DateTime date;
                    if (DateTime.TryParseExact(System.Text.Encoding.UTF7.GetString((bytes)),
                        "yyyyMMdd", _numberFormat, System.Globalization.DateTimeStyles.None, out date))
                        return date;
                    else
                        return DBNull.Value;

#else // в mono еще не реализован метод DateTime.TryParseExact
					try 
					{
						return DateTime.ParseExact (System.Text.Encoding.UTF7.GetString((bytes)), 	
						"yyyyMMdd", _numberFormat, System.Globalization.DateTimeStyles.None );
					}
					catch 
					{
						return DBNull.Value;
					}

#endif
                default:
                    throw (new NotSupportedException("Unable to process field '" + dbf.Name + "' (data type '" + dbf.DataType.ToString() + "')"));
            }

        }
    }
}
