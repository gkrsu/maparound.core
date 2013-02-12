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



/*===========================================================================
** 
** File: ShapeFile.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes that provides access to the shape-file reading and writing
**
=============================================================================*/

namespace MapAround.IO
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Linq;

    using MapAround.Geometry;
    using MapAround.IO.Handlers;
    using MapAround.Mapping;

    /// <summary>
    /// Types of shape file.
    /// </summary>
    public enum ShapeType
    {
        /// <summary>
        /// Null shape.
        /// </summary>
        NullShape = 0,

        /// <summary>
        /// Point.
        /// </summary>
        Point = 1,

        /// <summary>
        /// Polyline.
        /// </summary>
        Polyline = 3,

        /// <summary>
        /// Polygon.
        /// </summary>
        Polygon = 5,

        /// <summary>
        /// Multipoint.
        /// </summary>
        Multipoint = 8

        // Unsupported:
        // PointZ = 11,        
        // PolyLineZ = 13,        
        // PolygonZ = 15,        
        // MultiPointZ = 18,        
        // PointM = 21,        
        // PolyLineM = 23,        
        // PolygonM = 25,        
        // MultiPointM = 28,        
        // MultiPatch = 31
    }

    /// <summary>
    /// Represents a header of shape file.
    /// </summary>
    public class ShapeFileHeader
    {
        #region Private fields

        private int _fileCode;
        private int _fileLength;
        private int _version;
        private int _shapeType;

        private double _minX;
        private double _minY;
        private double _maxX;
        private double _maxY;

        #endregion

        /// <summary>
        /// Initializes a new instance of MapAround.IO.ShapeFileHeader.
        /// </summary>
        public ShapeFileHeader()
        {
        }

        #region Properties

        /// <summary>
        /// Gets a length of header in bytes.
        /// </summary>
        public static int Length
        {
            get { return 100; }
        }

        /// <summary>
        /// Gets or sets a shape-file code.
        /// Should be equal to 9994.
        /// </summary>
        public int FileCode
        {
            get { return _fileCode; }
            set { _fileCode = value; }
        }

        /// <summary>
        /// Gets or sets a length of file.
        /// </summary>
        public int FileLength
        {
            get { return _fileLength; }
            set { _fileLength = value; }
        }

        /// <summary>
        /// Gets or sets the format version.
        /// </summary>
        public int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Gets or sets an integer defining a type of shape-file.
        /// </summary>
        public int ShapeType
        {
            get { return _shapeType; }
            set { _shapeType = value; }
        }

        /// <summary>
        /// Gets or sets a minimal X value of shapes
        /// storing in shape file.
        /// </summary>
        public double MinX
        {
            get { return _minX; }
            set { _minX = value; }
        }

        /// <summary>
        /// Gets or sets a minimal Y value of shapes
        /// storing in shape file.
        /// </summary>
        public double MinY
        {
            get { return _minY; }
            set { _minY = value; }
        }

        /// <summary>
        /// Gets or sets a maximal X value of shapes
        /// storing in shape file.
        /// </summary>       
        public double MaxX
        {
            get { return _maxX; }
            set { _maxX = value; }
        }

        /// <summary>
        /// Gets or sets a maximal Y value of shapes
        /// storing in shape file.
        /// </summary>
        public double MaxY
        {
            get { return _maxY; }
            set { _maxY = value; }
        }

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Returns a System.String that represents the current MapAround.IO.ShapeFileHeader.
        /// </summary>
        /// <returns>A System.String that represents the current MapAround.IO.ShapeFileHeader</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileHeader: FileCode={0}, FileLength={1}, Version={2}, ShapeType={3}",
                this._fileCode, this._fileLength, this._version, this._shapeType);

            return sb.ToString();
        }

        /// <summary>
        /// Sets a bounds of all shapes in file.
        /// </summary>
        /// <param name="Bounds">A bounding rectangle defining the bounds</param>
        public  void SetBounds(BoundingRectangle Bounds)
        {
            this.MinX = Bounds.MinX;
            this.MinY = Bounds.MinY;
            this.MaxX = Bounds.MaxX;
            this.MaxY = Bounds.MaxY;
        }

        /// <summary>
        /// Sets a bounds of all shapes in file.
        /// </summary>
        /// <param name="headerBounds">A header wrom which to take a bounds</param>
        public void SetBounds(ShapeFileHeader headerBounds)
        {
            this.MinX = headerBounds.MinX;
            this.MinY = headerBounds.MinY;
            this.MaxX = headerBounds.MaxX;
            this.MaxY = headerBounds.MaxY;
        }

        #endregion Public methods

        /// <summary>
        /// Writes this header to the stream.
        /// </summary>
        /// <param name="file">A System.IO.BinaryWriter instance to write the header</param>
        /// <param name="ShapeType">Shape type</param>
        internal void Write(BigEndianBinaryWriter file, ShapeType ShapeType)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (this.FileLength == -1)
                throw new InvalidOperationException("The header properties need to be set before writing the header record.");
            int pos = 0;
            file.WriteIntBE(this.FileCode);
            pos += 4;
            for (int i = 0; i < 5; i++)
            {
                file.WriteIntBE(0);//Skip unused part of header
                pos += 4;
            }
            file.WriteIntBE(this.FileLength);
            pos += 4;
            file.Write(this.Version);
            pos += 4;

            file.Write(int.Parse(Enum.Format(typeof(ShapeType), ShapeType, "d")));

            pos += 4;
            // Write the bounding box
            file.Write(this.MinX);
            file.Write(this.MinY);
            file.Write(this.MaxX);
            file.Write(this.MaxY);
            pos += 8 * 4;

            // Skip remaining unused bytes
            for (int i = 0; i < 4; i++)
            {
                file.Write(0.0); // Skip unused part of header
                pos += 8;
            }
        }
    }

    /// <summary>
    /// Represents a record of shape file.
    /// </summary>
    public class ShapeFileRecord
    {
        #region Private fields

        private int _recordNumber;
        private int _contentLength;

        private int _shapeType;

        private double _minX;
        private double _minY;
        private double _maxX;
        private double _maxY;

        private long _offset;

        private Collection<int> _parts = new Collection<int>();
        private Collection<ICoordinate> _points = new Collection<ICoordinate>();

        private DataRow _attributes;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MapAround.IO.ShapeFileRecord 
        /// </summary>
        public ShapeFileRecord()
        {
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets or sets an offset of this record 
        /// from begining of file.
        /// </summary>
        public long Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        /// <summary>
        /// Gets or sets a number of this record.
        /// </summary>
        public int RecordNumber
        {
            get { return _recordNumber; }
            set { _recordNumber = value; }
        }

        /// <summary>
        /// Gets or sets the length (in bytes) of this record.
        /// </summary>
        public int ContentLength
        {
            get { return _contentLength; }
            set { _contentLength = value; }
        }

        /// <summary>
        /// Gets or sets the shape type.
        /// </summary>
        public int ShapeType
        {
            get { return _shapeType; }
            set { _shapeType = value; }
        }

        /// <summary>
        /// Gets or sets the minimum X value.
        /// </summary>
        public double MinX
        {
            get { return _minX; }
            set { _minX = value; }
        }

        /// <summary>
        /// Gets or sets the minimum Y value.
        /// </summary>
        public double MinY
        {
            get { return _minY; }
            set { _minY = value; }
        }

        /// <summary>
        /// Gets or sets the maximum X value.
        /// </summary>
        public double MaxX
        {
            get { return _maxX; }
            set { _maxX = value; }
        }

        /// <summary>
        /// Gets or sets the maximum Y value.
        /// </summary>
        public double MaxY
        {
            get { return _maxY; }
            set { _maxY = value; }
        }

        /// <summary>
        /// Gets a number of parts of the geometry.
        /// </summary>
        public int NumberOfParts
        {
            get { return _parts.Count; }
        }

        /// <summary>
        /// Gets a number of points of the geometry.
        /// </summary>
        public int NumberOfPoints
        {
            get { return _points.Count; }
        }

        /// <summary>    
        /// Gets a collection containing the indices of 
        /// coordinate sequences corresponding parts of
        /// geometry.
        /// </summary>
        public Collection<int> Parts
        {
            get { return _parts; }
        }

        /// <summary>
        /// Gets a collection of coordinates of
        /// the geometry.
        /// </summary>
        public Collection<ICoordinate> Points
        {
            get { return _points; }
        }

        /// <summary>
        /// Gets or sets an attributes row associated
        /// with this  record.
        /// </summary>
        public DataRow Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Returns a System.String that represents the current MapAround.IO.ShapeFileRecord.
        /// </summary>
        /// <returns>A System.String that represents the current MapAround.IO.ShapeFilerecord</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileRecord: RecordNumber={0}, ContentLength={1}, ShapeType={2}",
                this._recordNumber, this._contentLength, this._shapeType);

            return sb.ToString();
        }
        #endregion Public methods
    }

    /// <summary>
    /// Represents an information of reading shape file.
    /// </summary>
    public class ShapeFileReadInfo
    {
        #region Private fields

        private string _fileName;
        private ShapeFile _shapeFile;
        private Stream _stream;
        private int _numBytesRead;
        private int _recordIndex;

        #endregion

        /// <summary>
        /// Initializes a new instance of the MapAround.IO.ShapeFileReadInfo.
        /// </summary>
        public ShapeFileReadInfo()
        {
        }

        #region Properties

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        /// <summary>
        /// Gets or sets a reference to read MapAround.IO.ShapeFile.
        /// </summary>
        public ShapeFile ShapeFile
        {
            get { return _shapeFile; }
            set { _shapeFile = value; }
        }

        /// <summary>
        /// Gets or sets a stream from which to read MapAround.IO.ShapeFile.
        /// </summary>
        public Stream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

        /// <summary>
        /// Gets or sets a number of bytes read.
        /// </summary>
        public int NumberOfBytesRead
        {
            get { return _numBytesRead; }
            set { _numBytesRead = value; }
        }

        /// <summary>
        /// Gets or sets a number of current record.
        /// </summary>
        public int RecordIndex
        {
            get { return _recordIndex; }
            set { _recordIndex = value; }
        }

        #endregion Properties

        #region Public methods

        /// <summary>
        /// Returns a System.String that represents the current MapAround.IO.ShapeFileReadInfo.
        /// </summary>
        /// <returns>A System.String that represents the current MapAround.IO.ShapeFileReadInfo</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ShapeFileReadInfo: FileName={0}, ", this._fileName);
            sb.AppendFormat("NumberOfBytesRead={0}, RecordIndex={1}", this._numBytesRead, this._recordIndex);

            return sb.ToString();
        }
        #endregion
    }

    /// <summary>
    /// Represents an ESRI Shape-file.
    /// Implements methods for reading and writing.
    /// </summary>
    public class ShapeFile
    {
        private const int _expectedFileCode = 9994;

        #region Private fields

        // header
        private ShapeFileHeader _fileHeader = new ShapeFileHeader();
        private DbaseFileHeader _dbaseHeader;

        private Collection<ShapeFileRecord> _records = new Collection<ShapeFileRecord>();
        private List<string> _attributeNames = new List<string>();

        private Encoding _attributesEncoding = Encoding.UTF8;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of MapAround.IO.Shapefile.
        /// </summary>
        public ShapeFile()
        {
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets or sets a header of dBase file 
        /// (attributes file).
        /// </summary>
        public DbaseFileHeader DbaseHeader
        {
            get { return _dbaseHeader; }
            set { _dbaseHeader = value; }
        }

        /// <summary>
        /// Gets an object representing the header of this shape file.
        /// </summary>
        public ShapeFileHeader FileHeader
        {
            get { return this._fileHeader; }
        }

        /// <summary>
        /// Gets a collection containing the attribute names.
        /// </summary>
        public ReadOnlyCollection<string> AttributeNames
        {
            get { return _attributeNames.AsReadOnly(); }
        }

        /// <summary>
        /// Gets a collection containing the records of this shape file.
        /// </summary>
        public Collection<ShapeFileRecord> Records
        {
            get { return _records; }
        }

        /// <summary>
        /// Gets or sets an encoding of attributes.
        /// </summary>
        public Encoding AttributesEncoding
        {
            get { return _attributesEncoding; }
            set { _attributesEncoding = value; }
        }

        #endregion

        #region Public methods

        #region Read

        /// <summary>
        /// Reads a shape-file data (geometries and attributes).
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="bounds">The bounding rectangle. Only those records are read, 
        /// which bounding rectangles intersect with this rectangle</param>
        public void Read(string fileName, BoundingRectangle bounds)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            string indexFile = fileName.Replace(".shp", ".shx");
            indexFile = indexFile.Replace(".SHP", ".SHX");
            int[] offsets;

            // .shx-файл необходим по спецификации, но прочесть shape-файл можно и без него.
            if (File.Exists(indexFile))
                offsets = ReadIndex(indexFile);
            else
                offsets = new int[] { };

            this.ReadShapes(fileName, offsets, bounds);

            string dbaseFile = fileName.Replace(".shp", ".dbf");
            dbaseFile = dbaseFile.Replace(".SHP", ".DBF");

            //!!!
            this.ReadAttributes(dbaseFile);
        }

        /// <summary>
        /// Reads the index of shape-file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        public int[] ReadIndex(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                ReadHeader(stream);
                int featureCount = (2 * this._fileHeader.FileLength - 100) / 8;

                int[] offsets = new int[featureCount];
                stream.Seek(100, 0);

                for (int x = 0; x < featureCount; ++x)
                {
                    offsets[x] = 2 * stream.ReadInt32BE();// ReadInt32_BE(stream);
                    stream.Seek(stream.Position + 4, 0);
                }

                return offsets;
            }
        }

        /// <summary>
        /// Reads the shapes of shape-file.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="offsets">An array containing offsets of the records to read</param>
        /// <param name="bounds">The bounding rectangle. Only those records are read, 
        /// which bounding rectangles intersect with this rectangle</param>
        public void ReadShapes(string fileName, int[] offsets, BoundingRectangle bounds)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    this.ReadShapes(stream, offsets, bounds);
                }
                catch
                {
                    stream.Flush();
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Reads the shapes from specified stream.
        /// </summary>
        /// <param name="stream">A System.IO.Stream instance to read shapes</param>
        /// <param name="offsets">An array containing the offset of records to read</param>
        /// <param name="bounds">The bounding rectangle. Only those records are read, 
        /// which bounding rectangles intersect with this rectangle</param>
        public void ReadShapes(Stream stream, int[] offsets, BoundingRectangle bounds)
        {
            this.ReadHeader(stream);
            this._records.Clear();

            if (offsets.Length == 0)
            {
                while (true)
                {
                    try
                    {
                        this.ReadRecord(stream, null, bounds);
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
            else
            {
                int i = 0;
                foreach (int offset in offsets)
                {
                    //!!!!!
                    int lPos = offset;// -4 * i;
                    this.ReadRecord(stream, lPos, bounds);
                    i++;
                }
            }
        }

        /// <summary>
        /// Reads the header oe shape-file.
        /// <remarks>
        /// Headers are placed into .shp and .shx files.
        /// </remarks>
        /// </summary>
        /// <param name="stream">A System.IO.Stream instance to read</param>
        public void ReadHeader(Stream stream)
        {
            // код формата
            this._fileHeader.FileCode = stream.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);
            if (this._fileHeader.FileCode != ShapeFile._expectedFileCode)
            {
                string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Invalid FileCode encountered. Expecting {0}.", ShapeFile._expectedFileCode);
                throw new InvalidDataException(msg);
            }

            stream.ReadInt32BE();//ShapeFile.ReadInt32_BE(stream);
            stream.ReadInt32BE();//ShapeFile.ReadInt32_BE(stream);
            stream.ReadInt32BE();//ShapeFile.ReadInt32_BE(stream);
            stream.ReadInt32BE();//ShapeFile.ReadInt32_BE(stream);
            stream.ReadInt32BE();//ShapeFile.ReadInt32_BE(stream);

            this._fileHeader.FileLength = stream.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);

            this._fileHeader.Version = stream.ReadInt32();// ShapeFile.ReadInt32_LE(stream);

            this._fileHeader.ShapeType = stream.ReadInt32();// ShapeFile.ReadInt32_LE(stream);

            this._fileHeader.MinX = stream.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            this._fileHeader.MinY = stream.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            this._fileHeader.MaxX = stream.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            this._fileHeader.MaxY = stream.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);

            if (Math.Abs(this._fileHeader.MaxX - this._fileHeader.MinX) < 1)
            {
                this._fileHeader.MinX -= 5;
                this._fileHeader.MaxX += 5;
            }
            if (Math.Abs(this._fileHeader.MaxY - this._fileHeader.MinY) < 1)
            {
                this._fileHeader.MinY -= 5;
                this._fileHeader.MaxY += 5;
            }

            stream.Seek(100, SeekOrigin.Begin);
        }

        /// <summary>
        /// Reads a record from the specified stream.
        /// </summary>
        /// <param name="stream">A System.IO.Stream instance to read</param>
        /// <param name="recordOffset">An offset of record</param>
        /// <param name="bounds">An object representing a bounds of the reading area</param>
        public ShapeFileRecord ReadRecord(Stream stream, int? recordOffset, BoundingRectangle bounds)
        {
                #region old

                //if (recordOffset != null)
                //    stream.Seek(recordOffset.Value, 0);

                //ShapeFileRecord record = new ShapeFileRecord();
                //record.Offset = stream.Position;

                //// заголовок записи
                //record.RecordNumber = ShapeFile.ReadInt32_BE(stream);
                //record.ContentLength = ShapeFile.ReadInt32_BE(stream);

                //// тип геометрической фигуры
                //record.ShapeType = ShapeFile.ReadInt32_LE(stream);

                //bool wasRead = false;
                //switch (record.ShapeType)
                //{
                //    case (int)ShapeType.NullShape:
                //        break;
                //    case (int)ShapeType.Point:
                //        wasRead = ShapeFile.ReadPoint(stream, bounds, record);
                //        break;
                //    case (int)ShapeType.PolyLine:
                //        wasRead = ShapeFile.ReadPolygon(stream, bounds, record);
                //        break;
                //    case (int)ShapeType.Polygon:
                //        wasRead = ShapeFile.ReadPolygon(stream, bounds, record);
                //        break;
                //    case (int)ShapeType.Multipoint:
                //        wasRead = ShapeFile.ReadMultipoint(stream, bounds, record);
                //        break;
                //    default:
                //        {
                //            string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, "ShapeType {0} is not supported.", (int)record.ShapeType);
                //            throw new InvalidDataException(msg);
                //        }
                //}

                //if (wasRead)
                //{
                //    this._records.Add(record);
                //    return record;
                //}
                //else return null;

                #endregion

                #region New

                if (recordOffset != null)
                    stream.Seek(recordOffset.Value, 0);

                ShapeFileRecord record = new ShapeFileRecord();
                record.Offset = stream.Position;

                // заголовок записи
                //BigEndianBinaryReader reader = new BigEndianBinaryReader(stream);
                //record.RecordNumber = reader.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);
                //record.ContentLength = reader.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);
                record.RecordNumber = stream.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);
                record.ContentLength = stream.ReadInt32BE();// ShapeFile.ReadInt32_BE(stream);


                // тип геометрической фигуры
                record.ShapeType = stream.ReadInt32();//.ReadInt32BE();// ShapeFile.ReadInt32_LE(stream);


                ShapeHandler handler = ShapeFile.GetShapeHandler((ShapeType)record.ShapeType);

                if (handler.Read(stream, bounds, record))
                {
                    this._records.Add(record);
                    return record;
                }
                else return null;

                #endregion
        }

        /// <summary>
        /// Reads a dBase file and merges dBase records with shapes.
        /// </summary>
        /// <param name="dbaseFile">The dBase file name</param>
        public void ReadAttributes(string dbaseFile)
        {
            if (string.IsNullOrEmpty(dbaseFile))
                throw new ArgumentNullException("dbaseFile");

            // отсутствие файла атрибутов - не ошибка
            if (!File.Exists(dbaseFile))
                return;

            using (DbaseReader reader = new DbaseReader(dbaseFile))
            {
                reader.Open();

                if (reader.DbaseHeader.Encoding == Encoding.UTF8)
                    reader.DbaseHeader.Encoding = _attributesEncoding;                          

                // чтение наименований атрибутов
                DataTable schema = reader.GetSchemaTable();
                _attributeNames.Clear();
                for (int i = 0; i < schema.Rows.Count; i++)
                    _attributeNames.Add(schema.Rows[i]["ColumnName"].ToString());

                // чтение значений атрибутов
                DataTable table = reader.NewTable;
                for (int i = 0; i < Records.Count; i++)
                    table.Rows.Add(reader.GetRow((uint)_records[i].RecordNumber - 1, table));

                //byte lEndFile = reader.ReadByte();
                //if (lEndFile != default(byte))
                //{
                //}

                MergeAttributes(table);
            }
        }

        #endregion

        /// <summary>
        /// Returns a System.String that represents the current MapAround.IO.ShapeFiler.
        /// </summary>
        /// <returns>A System.String that represents the current MapAround.IO.ShapeFile</returns>
        public override string ToString()
        {
            return "ShapeFile: " + this._fileHeader.ToString();
        }

        #region Write

        /// <summary>
        /// Writes the attribute file.
        /// </summary>
        /// <param name="featureCollection">A collection containing features which attributes is to be written</param>
        /// <param name="dbaseFile">file attributes</param>
        public void WriteAttributes(string dbaseFile, ICollection<Feature> featureCollection)
        {
                if (this._dbaseHeader == null)
                    throw new NullReferenceException("dbaseHeader");

                if (string.IsNullOrEmpty(dbaseFile))
                    throw new ArgumentNullException("dbaseFile");

                //if there is no file attributes - create
                if (!File.Exists(dbaseFile))
                {
                    Stream file = File.Create(dbaseFile);
                    file.Close();
                }

                this.RecountColumnLengths(this._dbaseHeader, featureCollection);
                this.RecountRecords(this._dbaseHeader,featureCollection);
                

                DbaseWriter  dbaseWriter = new DbaseWriter(dbaseFile, this._dbaseHeader);
                //dbaseWriter.WriteHeader();//this._dbaseHeader);
                try
                {

                    //this.RecountColumnLengths(this._dbaseHeader, featureCollection);
                    dbaseWriter.WriteHeader();//this._dbaseHeader);
                    int j = 0;
                    foreach (Feature feature in featureCollection)
                    {
                        ArrayList values = new ArrayList();
                        for (int i = 0; i < dbaseWriter.Header.NumFields; i++)
                            values.Add(feature[dbaseWriter.Header.DBaseColumns[i].Name]);//   attribs[Header.Fields[i].Name]);
                        dbaseWriter.Write(values, j);
                        j++;
                    }

                    //end of file
                    dbaseWriter.Write((byte)26);
                }
                finally
                {
                    dbaseWriter.Close();
                }
        }

        /// <summary>
        /// Writes shapes.
        /// </summary>
        /// <param name="filename">The string value defining shape file name without .shp extension</param>
        /// <param name="geometryCollection"> MapAround.Geometry.GeometryCollection instance containing
        /// the geometries to write to shape file</param>		
        public void WriteShapes(string filename, GeometryCollection geometryCollection)
        {
            if(geometryCollection.HasDifferentTypeInstances)
                throw new ArgumentException("Geometries in the shape file should be the instances of the same type.", "geometryCollection");

            using (FileStream shpStream = new FileStream(filename + ".shp", FileMode.Create))
            {
                using (FileStream shxStream = new FileStream(filename + ".shx", FileMode.Create))
                {
                    BigEndianBinaryWriter shpBinaryWriter = new BigEndianBinaryWriter(shpStream);//, Encoding.ASCII);
                    BigEndianBinaryWriter shxBinaryWriter = new BigEndianBinaryWriter(shxStream);//, Encoding.ASCII);

                    // body type and a handler
                    Handlers.ShapeHandler handler = ShapeFile.GetShapeHandler(ShapeFile.GetShapeType(geometryCollection[0]));//.Geometries[0]));
                    int numShapes = geometryCollection.Count;
                    // calc the length of the shp file, so it can put in the header.
                    int shpLength = 50;
                    for (int i = 0; i < numShapes; i++)
                    {
                        IGeometry body = (IGeometry)geometryCollection[i];//.Geometries[i];
                        shpLength += 4; // length of header in WORDS
                        shpLength += handler.GetLength(body); // length of shape in WORDS
                    }

                    int shxLength = 50 + (4 * numShapes);

                    // write the .shp header
                    ShapeFileHeader shpHeader = new ShapeFileHeader();
                    shpHeader.FileLength = shpLength;

                    // get envelope in external coordinates
                    BoundingRectangle bounds = geometryCollection.GetBoundingRectangle(); 
                    shpHeader.SetBounds(bounds);

                    shpHeader.FileCode = 9994;
                    shpHeader.ShapeType = (int)ShapeFile.GetShapeType(geometryCollection[0]);//.Geometries[0]);
                    shpHeader.Write(shpBinaryWriter, ShapeFile.GetShapeType(geometryCollection[0]));

                    // write the .shx header
                    ShapeFileHeader shxHeader = new ShapeFileHeader();
                    shxHeader.FileLength = shxLength;
                    shxHeader.SetBounds(shpHeader);//.Bounds = shpHeader.Bounds;

                    // assumes Geometry type of the first item will the same for all other items in the collection.
                    shxHeader.FileCode = 9994;
                    shxHeader.ShapeType = (int)ShapeFile.GetShapeType(geometryCollection[0]);
                    shxHeader.Write(shxBinaryWriter, ShapeFile.GetShapeType(geometryCollection[0]));

                    // write the individual records.
                    int _pos = 50; // header length in WORDS
                    for (int i = 0; i < numShapes; i++)
                    {
                        IGeometry body = geometryCollection[i];//.Geometries[i];
                        int recordLength = handler.GetLength(body);
                        shpBinaryWriter.WriteIntBE(i + 1);
                        shpBinaryWriter.WriteIntBE(recordLength);

                        shxBinaryWriter.WriteIntBE(_pos);
                        shxBinaryWriter.WriteIntBE(recordLength);

                        _pos += 4; // length of header in WORDS
                        handler.Write(body, shpBinaryWriter);//, geometryFactory);
                        _pos += recordLength; // length of shape in WORDS
                    }

                    shxBinaryWriter.Flush();
                    shxBinaryWriter.Close();
                    shpBinaryWriter.Flush();
                    shpBinaryWriter.Close();
                }
            }
        }

        /// <summary>
        /// Writes a collection of features into the shape-file.
        /// </summary>
        /// <param name="fileName">The string defining file name without .shp extension</param>
        /// <param name="features">The collection of features to write</param>
        public void Write(string fileName, ICollection<Feature> features)
        {
            // Test if the Header is initialized
            if (this._dbaseHeader == null)
                throw new ApplicationException("Dbase header should be set first.");

            // Write shp and shx  
            IGeometry[] geometries = new IGeometry[features.Count];
            int index = 0;
            foreach (Feature feature in features)
                geometries[index++] = feature.Geometry;

            GeometryCollection geomTmp = new GeometryCollection(geometries);

            string shpFile = fileName;
            string dbfFile = fileName + ".dbf";

            this.WriteShapes(shpFile, geomTmp);
            this.WriteAttributes(dbfFile, features);
        }

        #endregion

        #endregion

        #region Private methods

      

        private static bool isRecordInView(BoundingRectangle bounds, ShapeFileRecord record)
        {
            if (bounds != null && !bounds.IsEmpty())
            {
                if (!bounds.Intersects(
                    new BoundingRectangle(PlanimetryEnvironment.NewCoordinate(record.MinX, record.MinY),
                                          PlanimetryEnvironment.NewCoordinate(record.MaxX, record.MaxY))))
                    return false;
            }
            return true;
        }

       
        /// <summary>
        /// Merges attribute rows with the shape file records.
        /// </summary>
        /// <param name="table">The system.Data.DataTable instance containing the attribute values</param>
        private void MergeAttributes(DataTable table)
        {
            int index = 0;
            foreach (DataRow row in table.Rows)
            {
                if (index >= _records.Count)
                    break;
                _records[index].Attributes = row;
                ++index;
            }
        }

        /// <summary>
        /// Computes the sizes of attribute fileds taking into accound 
        /// attribute values of the features.
        /// </summary>
        /// <param name="DbaseHeader">The header of the dBase attribute file</param>
        /// <param name="Features">Enumerator of features</param>
        private void RecountColumnLengths(DbaseFileHeader DbaseHeader, IEnumerable Features)
        {
            foreach (DbaseFieldDescriptor field in DbaseHeader.DBaseColumns)
            {
                var fieldValues = Features.OfType<Feature>().Select(f => f[field.Name]);
                DbaseHeader.RecountColumnLength(field, fieldValues);
            }
        }

        /// <summary>
        /// Computes the count of DBF records
        ///  </summary>
        /// <param name="DbaseHeader">The header of the dBase attribute file</param>
        /// <param name="Features">Enumerator of features</param>
        private void RecountRecords(DbaseFileHeader DbaseHeader, IEnumerable Features)
        {
            DbaseHeader.NumRecords = Features.Cast<Feature>().Count();
        }

        #endregion Private methods

        #region Static Methods

        /// <summary>
        /// Gets a stub of dBase file header
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <param name="count">The record count</param>
        /// <param name="attributeNames">A list containing the attribute names</param>
        /// <returns>A stub of dBase file header</returns>
        public static DbaseFileHeader GetDbaseHeader(Feature feature, List<string> attributeNames, int count)
        {
            //string[] names = feature.Layer.FeatureAttributeNames.ToArray();// attribs.GetNames();
            string[] names = attributeNames.ToArray();// attribs.GetNames();
            DbaseFileHeader header = new DbaseFileHeader();
            header.NumRecords = count;
            int i = 0;
            foreach (string name in names)
            {
                Type type = feature.Attributes[i].GetType();// attribs.GetType(name);
                header.AddColumn(name, type);
                i++; 
            }
            header.Encoding = System.Text.ASCIIEncoding.ASCII;
            return header;
        }

        /// <summary>
        /// Generates a dBase file header.
        /// </summary>
        /// <param name="dbFields">An array containing the dBase filed descriptors</param>
        /// <param name="count">The record count</param>
        /// <returns>A stub of dBase file header</returns>
        public static DbaseFileHeader GetDbaseHeader(DbaseFieldDescriptor[] dbFields, int count)
        {
            DbaseFileHeader header = new DbaseFileHeader();
            header.NumRecords = count;

            foreach (DbaseFieldDescriptor dbField in dbFields)
                header.AddColumn(dbField.Name, dbField.DbaseType, dbField.DataType, dbField.Length, dbField.DecimalCount);

            return header;
        }

        /// <summary>
        /// Gets the header from a dbf file.
        /// </summary>
        /// <param name="dbfFile">The DBF file.</param>
        /// <returns>The dBase file header</returns>
        public static DbaseFileHeader GetDbaseHeader(string dbfFile)
        {
            if (!File.Exists(dbfFile))
                throw new FileNotFoundException(dbfFile + " not found");
            DbaseFileHeader header = new DbaseFileHeader();
            header.Read(new BinaryReader(new FileStream(dbfFile, FileMode.Open, FileAccess.Read, FileShare.Read)));
            return header;
        }

        /// <summary>
        /// Returns the appropriate class to convert a shaperecord to an MapAround geometry given the type of shape.
        /// </summary>
        /// <param name="type">The shape file type.</param>
        /// <returns>An instance of the appropriate handler to convert the shape record to a Geometry</returns>
        internal static ShapeHandler GetShapeHandler(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Point:
                    //case ShapeGeometryType.PointM:
                    //case ShapeGeometryType.PointZ:
                    //case ShapeGeometryType.PointZM:
                    return new PointHandler();

                case ShapeType.Polygon:
                    //case ShapeGeometryType.PolygonM:
                    //case ShapeGeometryType.PolygonZ:
                    //case ShapeGeometryType.PolygonZM:
                    return new PolygonHandler();

                case ShapeType.Polyline: //.LineString:
                    //case ShapeGeometryType.LineStringM:
                    //case ShapeGeometryType.LineStringZ:
                    //case ShapeGeometryType.LineStringZM:
                    return new MultiLineHandler();

                case ShapeType.Multipoint:
                    //case ShapeGeometryType.MultiPointM:
                    //case ShapeGeometryType.MultiPointZ:
                    //case ShapeGeometryType.MultiPointZM:
                    return new MultiPointHandler();

                default:
                    string msg = String.Format(System.Globalization.CultureInfo.InvariantCulture, "ShapeType {0} is not supported.", (int)type);
                    throw new InvalidDataException(msg);
            }
        }

        /// <summary>
        /// Given a geomtery object, returns the equilivent shape file type.
        /// </summary>
        /// <param name="geom">A Geometry object.</param>
        /// <returns>The equilivent for the geometry object.</returns>
        public static ShapeType GetShapeType(IGeometry geom)
        {
            if (geom is PointD)
                return ShapeType.Point;
            if (geom is Polygon)
                return ShapeType.Polygon;
            //if (geom is IMultiPolygon)
            //    return ShapeGeometryType.Polygon;
            if (geom is LinePath)
                return ShapeType.Polyline;//.LineString;
            if (geom is Polyline)
                return ShapeType.Polyline;//.LineString;
            if (geom is MultiPoint)
                return ShapeType.Multipoint;
            return ShapeType.NullShape;
        }

        #endregion
    }    
}

