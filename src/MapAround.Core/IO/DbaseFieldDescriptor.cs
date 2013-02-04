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



using System;

namespace MapAround.IO
{
	/// <summary>
    /// Represents a descriprion of dBase field.
	/// </summary>
	public class DbaseFieldDescriptor
	{
		private string _name;
		private char _dbaseType;
		private int _dataAddress;
		private int _length;
		private int _decimalCount;
        private Type _dataType;

        /// <summary>
        /// Gets or sets a CLR type corresponding 
        /// dBase type of field.
        /// </summary>
        public Type DataType
        {
            get { return _dataType; }
            set { _dataType = value; }
        }

        /// <summary>
        /// Converts a CLR type to corresponding dBase type.
        /// </summary>
        /// <param name="type">A CLR type</param>
        /// <returns>A corresponding dBase type</returns>
		public static char GetDbaseType(Type type)
		{
			DbaseFieldDescriptor dbaseColumn = new DbaseFieldDescriptor();
            if (type == typeof(Char))
                return 'C';
            if (type == typeof(string))
                return 'C';
            else if (type == typeof(Double))
                return 'N';
            else if (type == typeof(Single))
                return 'N';
            else if (type == typeof(Int16))
                return 'N';
            else if (type == typeof(Int32))
                return 'N';
            else if (type == typeof(Int64))
                return 'N';
            else if (type == typeof(UInt16))
                return 'N';
            else if (type == typeof(UInt32))
                return 'N';
            else if (type == typeof(UInt64))
                return 'N';
            else if (type == typeof(Decimal))
                return 'N';
            else if (type == typeof(Boolean))
                return 'L';
            else if (type == typeof(DateTime))
                return 'D';

			throw new NotSupportedException(String.Format("{0} does not have a corresponding dbase type.", type.Name));
		}

        /// <summary>
        /// Gets or sets a name of dBase field.
        /// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}
        		
        /// <summary>
        /// Gets or sets a character defining a dBase field type (C N L D or M).
        /// </summary>
		public char DbaseType
		{
			get
			{
				return _dbaseType;
			}
			set
			{
				_dbaseType = value;
			}
		}
        
        /// <summary>
        /// Gets or sets an offset of field data from the begining of record.
        /// </summary>
		public int DataAddress
		{
			get
			{
				return _dataAddress;
			}
			set
			{
				_dataAddress = value;
			}
		}
        
        /// <summary>
        /// Gets or sets a data length (in bytes)
        /// </summary>
		public int Length
		{
			get
			{
				return _length;
			}
			set
			{
				_length = value;
			}
		}
        
        /// <summary>
        /// Gets or sets a number of decimal symbols.
        /// </summary>
		public int DecimalCount
		{
			get
			{
				return _decimalCount;
			}
			set
			{
				_decimalCount = value;
			}
		}

		/// <summary>
        /// Gets a CLR type by the character defining a dBase field type.
		/// </summary>
		public static Type GetDataType(char DbaseType)
        {
            Type type;
            switch (DbaseType)
            {
                case 'L': // logical data type, one character (T,t,F,f,Y,y,N,n)
                    type = typeof(bool);
                    break;
                case 'C': // char or string
                    type = typeof(string);
                    break;
                case 'D': // date
                    type = typeof(DateTime);
                    break;
                case 'N': // numeric
                    type = typeof(double);
                    break;
                case 'F': // double
                    type = typeof(float);
                    break;
                case 'B': // BLOB - not a dbase but this will hold the WKB for a geometry object.
                    type = typeof(byte[]);
                    break;
                default:
                    throw new NotSupportedException("Do not know how to parse Field type " + DbaseType);
            }
            return type;
        }
	}
}
