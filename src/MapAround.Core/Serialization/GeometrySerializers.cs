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
** File: GeometrySerialization.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Serialization of spatial-related objects
**
=============================================================================*/

namespace MapAround.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Globalization;

    using MapAround.Geometry;
    using MapAround.IO;

    /// <summary>
    /// The MapAround.Serialization contains interfaces and classes
    /// for serializing and deserializing geometries and other 
    /// geospatial objects.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Serializes geometries into binary format.
    /// Also performs inverse operation.
    /// <para>
    /// Binary representation used by this class is non standart and 
    /// leaves original coordinate sequences unchanged. While other formats 
    /// like WKB has a strong constraints of topology and causes a changes 
    /// in coordinate sequences.
    /// </para>
    /// </summary>
    public static class BinaryGeometrySerializer
    {
        private static double readDouble(Stream stream)
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

        private static void writeDouble(Stream stream, double value)
        {
            byte[] doubleBytes = new byte[8];
            doubleBytes = BitConverter.GetBytes(value);
            for (int i = 0; i < 8; i++)
                stream.WriteByte(doubleBytes[i]);
        }

        private static int readInt(Stream stream)
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

        private static void writeInt(Stream stream, int value)
        {
            byte[] intBytes = new byte[4];
            intBytes = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
                stream.WriteByte(intBytes[i]);
        }

        /// <summary>
        /// Deserializes an instance of MapAround.Geometry.PointD from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the point geometry</param>
        /// <returns>Deserialized instance of MapAround.Geometry.PointD</returns>
        public static PointD DeserializePoint(Stream stream)
        {
            double x = readDouble(stream);
            double y = readDouble(stream);
            return new PointD(x, y);
        }

        /// <summary>
        /// Deserializes an instance of MapAround.Geometry.Polyline from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the polyline geometry</param>
        /// <returns>Deserialized instance of MapAround.Geometry.Polyline</returns>
        public static Polyline DeserializePolyline(Stream stream)
        {
            int partsCount = readInt(stream);
            Polyline pl = new Polyline();
            for (int i = 0; i < partsCount; i++)
            {
                int partLength = readInt(stream);
                LinePath path = new LinePath();
                for (int j = 0; j < partLength; j++)
                {
                    double x = readDouble(stream);
                    double y = readDouble(stream);
                    path.Vertices.Add(PlanimetryEnvironment.NewCoordinate(x, y));
                }
                pl.Paths.Add(path);
            }

            return pl;
        }

        /// <summary>
        /// Deserializes an instance of MapAround.Geometry.MultiPoint from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the multipoint geometry</param>
        /// <returns>Deserialized instance of MapAround.Geometry.MultiPoint</returns>
        public static MultiPoint DeserializeMultiPoint(Stream stream)
        {
            int partLength = readInt(stream);
            MultiPoint multiPoint = new MultiPoint();
            for (int j = 0; j < partLength; j++)
            {
                double x = readDouble(stream);
                double y = readDouble(stream);
                multiPoint.Points.Add(PlanimetryEnvironment.NewCoordinate(x, y));
            }

            return multiPoint;
        }

        /// <summary>
        /// Deserializes an instance of MapAround.Geometry.Polygon from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the polygon geometry</param>
        /// <returns>Deserialized instance of MapAround.Geometry.Polygon</returns>
        public static Polygon DeserializePolygon(Stream stream)
        {
            int contourCount = readInt(stream);
            Polygon pg = new Polygon();
            for (int i = 0; i < contourCount; i++)
            {
                int contourLength = readInt(stream);
                Contour c = new Contour();
                for (int j = 0; j < contourLength; j++)
                {
                    double x = readDouble(stream);
                    double y = readDouble(stream);
                    c.Vertices.Add(PlanimetryEnvironment.NewCoordinate(x, y));
                }
                pg.Contours.Add(c);
            }

            return pg;
        }

        /// <summary>
        /// Deserializes a geometry from the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing the geometry</param>
        /// <returns>Deserialized geomtry</returns>
        public static IGeometry DeserializeGeometry(Stream stream)
        {
            int featureType = readInt(stream);
            switch (featureType)
            {
                case 1: //Point
                    return BinaryGeometrySerializer.DeserializePoint(stream);
                case 2: //Polyline
                    return BinaryGeometrySerializer.DeserializePolyline(stream);
                case 3: //Polygon
                    return BinaryGeometrySerializer.DeserializePolygon(stream);
                case 4: //MultiPoint
                    return BinaryGeometrySerializer.DeserializeMultiPoint(stream);
            }

            throw new NotSupportedException("Geometry \"" + featureType.ToString() + "\" is not supported");
        }

        /// <summary>
        /// Serializes specified geometry into stream.
        /// </summary>
        /// <param name="stream">A stream to write serializing geometry</param>
        /// <param name="geometry">A geometry</param>
        public static void SerializeGeometry(Stream stream, IGeometry geometry)
        {
            if (geometry == null)
                return;

            if (geometry is PointD)
            {
                writeInt(stream, 1);
                SerializePoint(stream, (PointD)geometry);
                return;
            }

            if (geometry is Polyline)
            {
                writeInt(stream, 2);
                SerializePolyline(stream, (Polyline)geometry);
                return;
            }

            if (geometry is Polygon)
            {
                writeInt(stream, 3);
                SerializePolygon(stream, (Polygon)geometry);
                return;
            }

            if (geometry is MultiPoint)
            {
                writeInt(stream, 4);
                SerializeMultiPoint(stream, (MultiPoint)geometry);
                return;
            }

            throw new NotSupportedException("Geometry \"" + geometry.GetType().Name + "\" is not supported");
        }

        /// <summary>
        /// Serializes an instance of MapAround.Geometry.MultiPoint into stream.
        /// </summary>
        /// <param name="stream">A stream to write serializing geometry</param>
        /// <param name="multiPoint">A multipoint geometry</param>
        public static void SerializeMultiPoint(Stream stream, MultiPoint multiPoint)
        {
            writeInt(stream, multiPoint.Points.Count);

            foreach (ICoordinate p in multiPoint.Points)
            {
                writeDouble(stream, p.X);
                writeDouble(stream, p.Y);
            }
        }

        /// <summary>
        /// Serializes an instance of MapAround.Geometry.PointD into stream.
        /// </summary>
        /// <param name="stream">A stream to write serializing geometry</param>
        /// <param name="point">A point geometry</param>
        public static void SerializePoint(Stream stream, PointD point)
        {
            writeDouble(stream, point.X);
            writeDouble(stream, point.Y);
        }

        /// <summary>
        /// Serializes an instance of MapAround.Geometry.Polyline into stream.
        /// </summary>
        /// <param name="stream">A stream to write serializing geometry</param>
        /// <param name="polyline">A polyline geometry</param>
        public static void SerializePolyline(Stream stream, Polyline polyline)
        {
            writeInt(stream, polyline.Paths.Count);
            foreach (LinePath path in polyline.Paths)
            {
                writeInt(stream, path.Vertices.Count);

                foreach (ICoordinate p in path.Vertices)
                {
                    writeDouble(stream, p.X);
                    writeDouble(stream, p.Y);
                }
            }
        }

        /// <summary>
        /// Serializes an instance of MapAround.Geometry.Polygon into stream.
        /// </summary>
        /// <param name="stream">A stream to write serializing geometry</param>
        /// <param name="polygon">A polygon geometry</param>
        public static void SerializePolygon(Stream stream, Polygon polygon)
        {
            writeInt(stream, polygon.Contours.Count);
            foreach (Contour c in polygon.Contours)
            {
                writeInt(stream, c.Vertices.Count);

                foreach (ICoordinate p in c.Vertices)
                {
                    writeDouble(stream, p.X);
                    writeDouble(stream, p.Y);
                }
            }
        }
    }

    /// <summary>
    /// Serializes geometries into well-known text 
    /// and constructs geometries from such representation.
    /// </summary>
    public static class WKTGeometrySerializer
    {
        private static readonly string _wrongPolylinePart = "Linepath should contain more than one point";
        private static readonly string _degeneratePolygon = "Degenerate polygon";

        /// <summary>
        /// Computes a well-known text representation 
        /// of the point geometry.
        /// </summary>
        /// <param name="point">An instance of MapAround.Geometry.PointD to setialize</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetPointWKT(PointD point)
        {
            return string.Format("Point ({0} {1})",
                                 point.X.ToString(CultureInfo.InvariantCulture),
                                 point.Y.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Computes a well-known text representation 
        /// of the polyline geometry.
        /// </summary>
        /// <param name="polyline">An instance of MapAround.Geometry.Polyline to setialize</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetPolylineWKT(Polyline polyline)
        {
            if (polyline.Paths.Count == 0)
                throw new ArgumentException("Empty polyline", "polyline");

            if (polyline.Paths.Count == 1)
            {
                StringBuilder sb = new StringBuilder("LineString (");
                IList<ICoordinate> points = polyline.Paths[0].Vertices;
                int cnt = points.Count;
                if(cnt < 2)
                    throw new ArgumentException(_wrongPolylinePart, "polyline");

                for (int i = 0; i < cnt - 1; i++)
                    sb.Append(pointCoordsString(points[i]) + ", ");

                sb.Append(pointCoordsString(points[cnt - 1]) + ")");
                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder("MultiLineString (");
                for (int i = 0; i < polyline.Paths.Count; i++)
                {
                    sb.Append("(");
                    IList<ICoordinate> points = polyline.Paths[i].Vertices;
                    int cnt = points.Count;
                    if (cnt < 2)
                        throw new ArgumentException(_wrongPolylinePart, "polyline");
                    for (int j = 0; j < cnt - 1; j++)
                        sb.Append(pointCoordsString(points[j]) + ", ");

                    sb.Append(pointCoordsString(points[cnt - 1]) + ")");
                    if (i < polyline.Paths.Count - 1)
                        sb.Append(", "); ;
                }
                sb.Append(")");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Computes a well-known text representation 
        /// of the multipoint geometry.
        /// </summary>
        /// <param name="multiPoint">An instance of MapAround.Geometry.MultiPoint to setialize</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetMultiPointWKT(MultiPoint multiPoint)
        {
            StringBuilder sb = new StringBuilder("MultiPoint (");
            IList<ICoordinate> points = multiPoint.Points;
            int cnt = points.Count;

            for (int i = 0; i < cnt - 1; i++)
                sb.Append("(" + pointCoordsString(points[i]) + "), ");

            sb.Append("(" + pointCoordsString(points[cnt - 1]) + "))");
            return sb.ToString();
        }

        /// <summary>
        /// Computes a well-known text representation 
        /// of the polygon geometry.
        /// </summary>
        /// <param name="polygon">An instance of MapAround.Geometry.Polygon to setialize</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetPolygonWKT(Polygon polygon)
        {
            return GetPolygonWKT(polygon, false);
        }

        /// <summary>
        /// Computes a well-known text representation 
        /// of the polygon geometry.
        /// </summary>
        /// <param name="polygon">An instance of MapAround.Geometry.Polygon to setialize</param>
        /// <param name="reverseVerticesOrder">A value indicating whether a standart order of vertices
        /// should be inverted in WKT</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetPolygonWKT(Polygon polygon, bool reverseVerticesOrder)
        {
            List<Polygon> polygons = polygon.SplitToConnectedDomains();

            for (int i = polygons.Count - 1; i >= 0; i--)
            {
                if (reverseVerticesOrder)
                    foreach (Contour c in polygons[i].Contours)
                        c.Reverse();

                if (polygons[i].Contours.Count == 0)
                    polygons.RemoveAt(i);
            }

            if (polygons.Count == 0)
                throw new ArgumentException(_degeneratePolygon, "polygon");

            if (polygons.Count == 1)
            {
                if (polygon.Contours.Count == 0)
                    throw new ArgumentException(_degeneratePolygon, "polygon");

                StringBuilder sb = new StringBuilder("Polygon (");
                int cnt = polygons[0].Contours.Count;
                for (int i = 0; i < cnt; i++)
                {
                    sb.Append(contourString(polygons[0].Contours[i]));
                    if (i < cnt - 1)
                        sb.Append(", ");
                }
                sb.Append(")");
                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder("MultiPolygon (");
                for (int i = 0; i < polygons.Count; i++)
                {
                    sb.Append("(");
                    int cnt = polygons[i].Contours.Count;
                    for (int j = 0; j < cnt; j++)
                    {
                        sb.Append(contourString(polygons[i].Contours[j]));
                        if (j < cnt - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                    if (i < polygons.Count - 1)
                        sb.Append(", ");
                }
                sb.Append(")");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Computes a well-known text representation of geometry.
        /// </summary>
        /// <param name="geometry">A geometry to setialize</param>
        /// <returns>A well-known text representation of specified geometry.</returns>
        public static string GetGeometryWKT(IGeometry geometry)
        {
            if (geometry is PointD)
                return GetPointWKT((PointD)geometry);

            if (geometry is Polyline)
                return GetPolylineWKT((Polyline)geometry);

            if (geometry is Polygon)
                return GetPolygonWKT((Polygon)geometry);

            if (geometry is MultiPoint)
                return GetMultiPointWKT((MultiPoint)geometry);

            throw new NotSupportedException(string.Format("Geometry \"{0}\" is not supported", geometry.GetType().Name));
        }

        /// <summary>
        /// Constructs a geometry from its well-known text representation.
        /// </summary>
        /// <returns>A constructed geometry</returns>
        public static IGeometry GeometryFromWKT(string wkt)
        {
            StringReader sr = new StringReader(wkt);
            WktStreamTokenizer tokenizer = new WktStreamTokenizer(sr);
            tokenizer.NextToken();
            switch (tokenizer.GetStringValue().ToUpper())
            {
                case "POINT": return readPoint(tokenizer);
                case "LINESTRING": return readLineString(tokenizer);
                case "MULTILINESTRING": return readMultiLineString(tokenizer);
                case "POLYGON": return readPolygon(tokenizer);
                case "MULTIPOLYGON": return readMultiPolygon(tokenizer);
                case "MULTIPOINT": return readMultiPoint(tokenizer);
            }

            throw new ArgumentException(string.Format("Unknown geometry \"{0}\"", tokenizer.GetStringValue()));
        }

        private static MultiPoint readMultiPoint(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            List<ICoordinate> points = new List<ICoordinate>();

            while (true)
            {
                points.Add(readPoint(tokenizer).Coordinate);
                tokenizer.NextToken(true);
                if (tokenizer.GetStringValue() == ",")
                    continue;

                if (tokenizer.GetStringValue() == ")")
                    break;
            }

            if (tokenizer.GetStringValue() == ")")
            {
                MultiPoint multiPoint = new MultiPoint(points);
                return multiPoint;
            }
            else
                throwMissingCloseBracket(tokenizer);
            return null;
        }

        private static PointD readPoint(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            PointD point = new PointD(readPointCoords(tokenizer));
            tokenizer.ReadToken(")");
            return point;
        }

        private static Polyline readLineString(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            List<ICoordinate> points = readPointCoordsList(tokenizer);
            if (tokenizer.GetStringValue() == ")")
            {
                Polyline polyline = new Polyline();
                polyline.Paths.Add(new LinePath());
                polyline.Paths[0].Vertices = points;
                return polyline;
            }
            else 
                throwMissingCloseBracket(tokenizer);
            return null;
        }

        private static Polyline readMultiLineString(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            List<List<ICoordinate>> lists = readPointsCoordLists(tokenizer);
            if (tokenizer.GetStringValue() == ")")
            {
                Polyline polyline = new Polyline();
                foreach (List<ICoordinate> list in lists)
                {
                    LinePath path = new LinePath();
                    path.Vertices = list;
                    polyline.Paths.Add(path);
                }
                return polyline;
            }
            else 
                throwMissingCloseBracket(tokenizer);
            return null;
        }

        private static Polygon readPolygon(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            List<List<ICoordinate>> lists = readPointsCoordLists(tokenizer);
            if (tokenizer.GetStringValue() == ")")
            {
                Polygon polygon = new Polygon();
                foreach (List<ICoordinate> list in lists)
                {
                    Contour contour = new Contour();
                    list.RemoveAt(list.Count - 1);
                    contour.Vertices = list;
                    polygon.Contours.Add(contour);
                }
                return polygon;
            }
            else 
                throwMissingCloseBracket(tokenizer);
            return null;
        }

        private static Polygon readMultiPolygon(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("(");
            Polygon polygon = new Polygon();
            bool comma = true;
            while (comma)
            {
                tokenizer.ReadToken("(");
                List<List<ICoordinate>> lists = readPointsCoordLists(tokenizer);
                if (tokenizer.GetStringValue() == ")")
                {
                    foreach (List<ICoordinate> list in lists)
                    {
                        Contour contour = new Contour();
                        list.RemoveAt(list.Count - 1);
                        contour.Vertices = list;
                        polygon.Contours.Add(contour);
                    }
                }
                else
                    throwMissingCloseBracket(tokenizer);
                tokenizer.NextToken();
                comma = tokenizer.GetStringValue() == ",";
            }
            if (tokenizer.GetStringValue() != ")")
                throwMissingCloseBracket(tokenizer);

            return polygon;
        }

        private static void throwMissingCloseBracket(WktStreamTokenizer tokenizer)
        {
            throw
                new ArgumentException(String.Format(CultureInfo.InvariantCulture.NumberFormat,
                                        "Missing close bracket. Line {0}, position {1}.",
                                        tokenizer.LineNumber,
                                        tokenizer.Column));
        }

        private static List<List<ICoordinate>> readPointsCoordLists(WktStreamTokenizer tokenizer)
        {
            List<List<ICoordinate>> result = new List<List<ICoordinate>>();
            bool comma = true;
            while (comma)
            {
                tokenizer.ReadToken("(");
                result.Add(readPointCoordsList(tokenizer));
                if (tokenizer.GetStringValue() == ")")
                {
                    tokenizer.NextToken();
                    comma = tokenizer.GetStringValue() == ",";
                }
                else
                    throwMissingCloseBracket(tokenizer);
            }
            return result;
        }

        private static List<ICoordinate> readPointCoordsList(WktStreamTokenizer tokenizer)
        {
            List<ICoordinate> points = new List<ICoordinate>();
            bool comma = true;
            while(comma)
            {
                points.Add(readPointCoords(tokenizer));
                tokenizer.NextToken();
                comma = tokenizer.GetStringValue() == ",";
            }

            return points;
        }

        private static ICoordinate readPointCoords(WktStreamTokenizer tokenizer)
        {
            ICoordinate point = PlanimetryEnvironment.NewCoordinate(0, 0);
            tokenizer.NextToken();
            point.X = tokenizer.GetNumericValue();
            tokenizer.NextToken();
            point.Y = tokenizer.GetNumericValue();
            return point;
        }

        private static string contourString(Contour contour)
        {
            if (contour.Vertices.Count <= 2)
                throw new ArgumentException("Degenerate contour", "contour");

            StringBuilder sb = new StringBuilder("(");

            for (int i = 0; i < contour.Vertices.Count; i++)
            {
                sb.Append(pointCoordsString(contour.Vertices[i]));
                sb.Append(", ");
            }

            sb.Append(pointCoordsString(contour.Vertices[0]) + ")");
            return sb.ToString();
        }

        private static string pointCoordsString(ICoordinate point)
        {
            return point.X.ToString(CultureInfo.InvariantCulture) +
                   " " +
                   point.Y.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// WKB Geometry Types.
    /// </summary>
    public enum WKBGeometryType : uint
    {
        /// <summary>
        /// Point.
        /// </summary>
        Point = 1,
        /// <summary>
        /// LineString.
        /// </summary>
        LineString = 2,
        /// <summary>
        /// Polygon.
        /// </summary>
        Polygon = 3,
        /// <summary>
        /// MultiPoint.
        /// </summary>
        MultiPoint = 4,
        /// <summary>
        /// MultiLineString.
        /// </summary>
        MultiLineString = 5,
        /// <summary>
        /// MultiPolygon.
        /// </summary>
        MultiPolygon = 6,
        /// <summary>
        /// GeometryCollection.
        /// </summary>
        GeometryCollection = 7
    }

    /// <summary>
    /// Byte order.
    /// </summary>
    public enum WKBByteOrder : byte
    {
        /// <summary>
        /// XDR (Big Endian) 
        /// </summary>
        /// <remarks>
        /// <para>The XDR representation of an Unsigned Integer is Big Endian (most significant byte first).</para>
        /// <para>The XDR representation of a Double is Big Endian (sign bit is first byte).</para>
        /// </remarks>
        Xdr = 0,
        /// <summary>
        /// NDR (Little Endian) 
        /// </summary>
        /// <remarks>
        /// <para>The NDR representation of an Unsigned Integer is Little Endian (least significant byte first).</para>
        /// <para>The NDR representation of a Double is Little Endian (sign bit is last byte).</para>
        /// </remarks>
        Ndr = 1
    }

    /// <summary>
    /// Serializes geometries into well-known binary 
    /// and constructs them from such representation.
    /// </summary>
    public static class WKBGeometrySerializer
    {
        private static readonly string _degeneratePolygon = "Degenerate polygon";
        private static readonly string _unrecognizedByteOrder = "Unknown byte order";

        /// <summary>
        /// Writes an unsigned integer to stream using specified byte order.
        /// </summary>
        /// <param name="value">An integer value to write</param>
        /// <param name="stream">A stream instance</param>
        /// <param name="byteOrder">A byte order</param>
        private static void writeUInt32(UInt32 value, Stream stream, WKBByteOrder byteOrder)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (byteOrder == WKBByteOrder.Xdr)
            {
                Array.Reverse(bytes);
                stream.Write(bytes, 0, bytes.Length);
            }
            else
                stream.Write(bytes, 0, bytes.Length);
        }

        private static UInt32 readUInt32(Stream stream, WKBByteOrder byteOrder)
        {
            byte[] bytes = new byte[sizeof(UInt32)];
            stream.Read(bytes, 0, sizeof(UInt32));
            if (byteOrder == WKBByteOrder.Xdr)
                Array.Reverse(bytes);
            else
                if (byteOrder != WKBByteOrder.Ndr)
                    throw new ArgumentException(_unrecognizedByteOrder);

            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Writes a double value to stream using specified byte order.
        /// </summary>
        /// <param name="value">A double value to write</param>
        /// <param name="stream">A stream instance</param>
        /// <param name="byteOrder">A byte order</param>
        private static void writeDouble(double value, Stream stream, WKBByteOrder byteOrder)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (byteOrder == WKBByteOrder.Xdr)
            {
                Array.Reverse(bytes);
                stream.Write(bytes, 0, bytes.Length);
            }
            else
                stream.Write(bytes, 0, bytes.Length);
        }

        private static double readDouble(Stream stream, WKBByteOrder byteOrder)
        {
            byte[] bytes = new byte[sizeof(double)];
            stream.Read(bytes, 0, sizeof(double));
            if (byteOrder == WKBByteOrder.Xdr)
                Array.Reverse(bytes);
            else
                if (byteOrder != WKBByteOrder.Ndr)
                    throw new ArgumentException(_unrecognizedByteOrder);

            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Serializes a geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="geometry">A geometry to serialize</param>
        /// /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        public static void SerializeGeometry(Stream stream, IGeometry geometry, WKBByteOrder byteOrder)
        {
            if (geometry == null)
                return;

            if (geometry is PointD)
            {
                SerializePoint(stream, (PointD)geometry, byteOrder);
                return;
            }

            if (geometry is Polyline)
            {
                SerializePolyline(stream, (Polyline)geometry, byteOrder);
                return;
            }

            if (geometry is Polygon)
            {
                SerializePolygon(stream, (Polygon)geometry, byteOrder);
                return;
            }

            if (geometry is MultiPoint)
            {
                SerializeMultiPoint(stream, (MultiPoint)geometry, byteOrder);
                return;
            }

            throw new NotSupportedException("Geometry \"" + geometry.GetType().Name + "\" is not supported");
        }

        /// <summary>
        /// Serializes a multipoint geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="multiPoint">A multipoint geometry to serialize</param>
        /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        public static void SerializeMultiPoint(Stream stream, MultiPoint multiPoint, WKBByteOrder byteOrder)
        {
            // порядок байтов
            stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));

            // тип геометрической фигуры
            writeUInt32((uint)WKBGeometryType.MultiPoint, stream, byteOrder);

            // количество вершин
            writeUInt32((uint)multiPoint.Points.Count, stream, byteOrder);

            // вершины
            foreach (ICoordinate p in multiPoint.Points)
            {
                // порядок байтов
                stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));
                // тип геометрической фигуры
                writeUInt32((uint)WKBGeometryType.Point, stream, byteOrder);

                writeDouble(p.X, stream, byteOrder);
                writeDouble(p.Y, stream, byteOrder);
            }
        }

        /// <summary>
        /// Serializes a point geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="point">A point geometry to serialize</param>
        /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        public static void SerializePoint(Stream stream, PointD point, WKBByteOrder byteOrder)
        {
            // порядок байтов
            stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));

            // тип геометрической фигуры
            writeUInt32((uint)WKBGeometryType.Point, stream, byteOrder);

            // значения координат
            writeDouble(point.X, stream, byteOrder);
            writeDouble(point.Y, stream, byteOrder);
        }

        /// <summary>
        /// Serializes a polyline geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="polyline">A polyline geometry to serialize</param>
        /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        public static void SerializePolyline(Stream stream, Polyline polyline, WKBByteOrder byteOrder)
        {
            if (polyline == null)
                return;

            if (polyline.Paths.Count == 0)
                throw new ArgumentException("Polyline must contain at least two points", "polyline");

            // порядок байтов
            stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));

            if (polyline.Paths.Count == 1)
                writeLineString(stream, polyline.Paths[0], byteOrder);
            else
            {
                // тип геометрической фигуры
                writeUInt32((uint)WKBGeometryType.MultiLineString, stream, byteOrder);

                // количество частей полилинии
                writeUInt32((uint)polyline.Paths.Count, stream, byteOrder);

                // части
                foreach (LinePath path in polyline.Paths)
                {
                    // порядок байтов
                    stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));
                    writeLineString(stream, path, byteOrder);
                }
            }
        }

        /// <summary>
        /// Serializes a polygon geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="polygon">A polygon geometry to serialize</param>
        /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        public static void SerializePolygon(Stream stream, Polygon polygon, WKBByteOrder byteOrder)
        {
            SerializePolygon(stream, polygon, byteOrder, false);
        }

        /// <summary>
        /// Serializes a polygon geometry into stream using a specified byte order.
        /// </summary>
        /// <param name="stream">A stream instance</param>
        /// <param name="polygon">A polygon geometry to serialize</param>
        /// <param name="byteOrder">A byte order which is used to write integer and double values</param>
        /// <param name="reverseVertices">A value indicating whether a standart order of vertices
        /// should be inverted in WKB</param>
        public static void SerializePolygon(Stream stream, Polygon polygon, WKBByteOrder byteOrder, bool reverseVertices)
        {
            List<Polygon> polygons = polygon.SplitToConnectedDomains();
            if (polygons.Count == 0)
                throw new ArgumentException(_degeneratePolygon, "polygon");

            if (reverseVertices)
                foreach (Polygon p in polygons)
                    foreach (Contour c in p.Contours)
                        c.Reverse();

            // порядок байтов
            stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));

            if (polygons.Count == 1)
            {
                // тип геометрической фигуры
                writeUInt32((uint)WKBGeometryType.Polygon, stream, byteOrder);

                // количество контуров
                writeUInt32((uint)polygons[0].Contours.Count, stream, byteOrder);

                foreach (Contour c in polygons[0].Contours)
                    writeContour(stream, c, byteOrder);
            }
            else
            {
                // тип геометрической фигуры
                writeUInt32((uint)WKBGeometryType.MultiPolygon, stream, byteOrder);

                // количество полигонов
                writeUInt32((uint)polygons.Count, stream, byteOrder);

                // полигоны
                foreach (Polygon p in polygons)
                {
                    // порядок байтов
                    stream.Write(new byte[] { (byte)byteOrder }, 0, sizeof(byte));

                    // тип геометрической фигуры
                    writeUInt32((uint)WKBGeometryType.Polygon, stream, byteOrder);

                    // количество контуров
                    writeUInt32((uint)p.Contours.Count, stream, byteOrder);

                    // контуры
                    foreach (Contour c in p.Contours)
                        writeContour(stream, c, byteOrder);
                }
            }

        }

        private static void writeContour(Stream stream, Contour contour, WKBByteOrder byteOrder)
        {
            // количество вершин
            writeUInt32((uint)contour.Vertices.Count + 1, stream, byteOrder);

            // вершины
            foreach (ICoordinate p in contour.Vertices)
            {
                writeDouble(p.X, stream, byteOrder);
                writeDouble(p.Y, stream, byteOrder);
            }

            writeDouble(contour.Vertices[0].X, stream, byteOrder);
            writeDouble(contour.Vertices[0].Y, stream, byteOrder);
        }

        private static void writeLineString(Stream stream, LinePath part, WKBByteOrder byteOrder)
        {
            // тип геометрической фигуры
            writeUInt32((uint)WKBGeometryType.LineString, stream, byteOrder);

            // количество вершин
            writeUInt32((uint)part.Vertices.Count, stream, byteOrder);

            // вершины
            foreach (ICoordinate p in part.Vertices)
            {
                writeDouble(p.X, stream, byteOrder);
                writeDouble(p.Y, stream, byteOrder);
            }
        }

        /// <summary>
        /// Seserializes geometry from its well-known binary representation.
        /// </summary>
        /// <param name="stream">A stream containing well-known binary representation of geometry</param>
        /// <returns>A deserialized geometry</returns>
        public static IGeometry DeserializeGeometry(Stream stream)
        {
            int b = stream.ReadByte();
            if (b == -1)
                throw new ArgumentException("Unexpected end of stream", "stream");

            byte byteOrder = (byte)b;

            UInt32 type = readUInt32(stream, (WKBByteOrder)byteOrder);

            switch ((WKBGeometryType)type)
            {
                case WKBGeometryType.Point:
                    return readPoint(stream, (WKBByteOrder)byteOrder);

                case WKBGeometryType.LineString:
                    return readLineString(stream, (WKBByteOrder)byteOrder);

                case WKBGeometryType.MultiLineString:
                    return readMultiLineString(stream, (WKBByteOrder)byteOrder);

                case WKBGeometryType.Polygon:
                    return readPolygon(stream, (WKBByteOrder)byteOrder);

                case WKBGeometryType.MultiPolygon:
                    return readMultiPolygon(stream, (WKBByteOrder)byteOrder);

                case WKBGeometryType.MultiPoint:
                    return readMultiPoint(stream, (WKBByteOrder)byteOrder);
            }

            throw new NotSupportedException("Geometry '" + type.ToString() + "' is not supported.");
        }

        private static MultiPoint readMultiPoint(Stream stream, WKBByteOrder byteOrder)
        {
            MultiPoint multiPoint = new MultiPoint();

            int numPoints = (int)readUInt32(stream, byteOrder);

            for (int i = 0; i < numPoints; i++)
            {
                // порядок байтов
                stream.ReadByte();
                // тип фигуры
                readUInt32(stream, byteOrder);

                multiPoint.Points.Add(PlanimetryEnvironment.NewCoordinate(readDouble(stream, byteOrder), readDouble(stream, byteOrder)));
            }
            return multiPoint;
        }

        private static ICoordinate readPointCoords(Stream stream, WKBByteOrder byteOrder)
        {
            return PlanimetryEnvironment.NewCoordinate(readDouble(stream, byteOrder), readDouble(stream, byteOrder));
        }

        private static PointD readPoint(Stream stream, WKBByteOrder byteOrder)
        {
            return new PointD(PlanimetryEnvironment.NewCoordinate(readDouble(stream, byteOrder), readDouble(stream, byteOrder)));
        }

        private static Polyline readLineString(Stream stream, WKBByteOrder byteOrder)
        {
            LinePath path = new LinePath();

            path.Vertices = readCoordsList(stream, byteOrder);

            Polyline pl = new Polyline();
            pl.Paths.Add(path);
            return pl;
        }

        private static List<ICoordinate> readCoordsList(Stream stream, WKBByteOrder byteOrder)
        {
            int numPoints = (int)readUInt32(stream, byteOrder);

            List<ICoordinate> result = new List<ICoordinate>(numPoints);

            for (int i = 0; i < numPoints; i++)
                result.Add(PlanimetryEnvironment.NewCoordinate(readDouble(stream, byteOrder), readDouble(stream, byteOrder)));

            return result;
        }

        private static Polyline readMultiLineString(Stream stream, WKBByteOrder byteOrder)
        {
            int numLineStrings = (int)readUInt32(stream, byteOrder);

            Polyline result = new Polyline();
            result.Paths = new List<LinePath>((int)numLineStrings);

            for (int i = 0; i < numLineStrings; i++)
            {
                // порядок байтов
                stream.ReadByte();
                // тип фигуры
                readUInt32(stream, byteOrder);
                // количество вершин
                int numPoints = (int)readUInt32(stream, byteOrder);

                result.Paths.Add(new LinePath());
                // вершины
                for (int j = 0; j < numPoints; j++)
                    result.Paths[i].Vertices.Add(PlanimetryEnvironment.NewCoordinate(readDouble(stream, byteOrder), readDouble(stream, byteOrder)));
            }

            return result;
        }

        private static Polygon readPolygon(Stream stream, WKBByteOrder byteOrder)
        {
            int numContours = (int)readUInt32(stream, byteOrder);

            if (numContours < 1)
                throw new ArgumentException(_degeneratePolygon, "stream");

            Polygon polygon = new Polygon();
            for (int i = 0; i < numContours; i++)
                polygon.Contours.Add(readContour(stream, byteOrder));

            return polygon;
        }

        private static Contour readContour(Stream stream, WKBByteOrder byteOrder)
        {
            Contour c = new Contour();
            c.Vertices = readCoordsList(stream, byteOrder);

            int cnt = c.Vertices.Count - 1;

            if (c.Vertices[0].Equals(c.Vertices[cnt]))
                c.Vertices.RemoveAt(cnt);

            //if (c.Vertices.Count < 3)
            //    throw new ArgumentException("Degenerate contour.", "stream");

            return c;
        }

        private static Polygon readMultiPolygon(Stream stream, WKBByteOrder byteOrder)
        {
            // количество полигонов
            int numPolygons = (int)readUInt32(stream, byteOrder);

            Polygon polygon = new Polygon();

            // полигоны
            for (int i = 0; i < numPolygons; i++)
            {
                // порядок байтов
                stream.ReadByte();
                // тип геометрической фигуры
                readUInt32(stream, byteOrder);
                // количество контуров
                int numContours = (int)readUInt32(stream, byteOrder);

                if (numContours < 1)
                    throw new ArgumentException(_degeneratePolygon, "stream");

                for (int j = 0; j < numContours; j++)
                    polygon.Contours.Add(readContour(stream, byteOrder));
            }

            return polygon;
        }
    }
}