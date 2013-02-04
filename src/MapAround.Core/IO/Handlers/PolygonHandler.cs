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
** File: PolygonHandler.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Handling polygon objects of the Shape-file
**
=============================================================================*/

namespace MapAround.IO.Handlers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    using MapAround.Geometry;

    /// <summary>
    /// Обработчик полигона.
    /// </summary>
    internal class PolygonHandler : ShapeHandler
    {
        /// <summary>
        /// Получает тип фигуры.
        /// </summary>
        public override ShapeType ShapeType
        {
            get { return ShapeType.Polygon; }
        }

        /// <summary>
        /// Читает запись представляющую полигон.
        /// </summary>
        /// <param name="file">Входной поток</param>
        /// <param name="record">Запись Shape-файла в которую будет помещена прочитанная информация</param>
        /// <param name="bounds">Ограничивающий прямоугольник, с которым должен пересекаться ограничивающий прямоугольник записи</param>
        public override bool Read(/*BigEndianBinaryReader*/ Stream file, BoundingRectangle bounds, ShapeFileRecord record)
        {
            record.MinX = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            record.MinY = file.ReadDouble(); //ShapeFile.ReadDouble64_LE(stream);
            record.MaxX = file.ReadDouble(); ;// ShapeFile.ReadDouble64_LE(stream);
            record.MaxY = file.ReadDouble(); ;// ShapeFile.ReadDouble64_LE(stream);

            int numParts = file.ReadInt32();// ShapeFile.ReadInt32_LE(stream);
            int numPoints = file.ReadInt32(); //ShapeFile.ReadInt32_LE(stream);

            if (!ShapeHandler.IsRecordInView(bounds, record))
            {
                file.Seek((long)numPoints * 16 + numParts * 4, SeekOrigin.Current);
                return false;
            }

            for (int i = 0; i < numParts; i++)
                record.Parts.Add(file.ReadInt32());//ShapeFile.ReadInt32_LE(stream));

            for (int i = 0; i < numPoints; i++)
            {
                ICoordinate p =
                    PlanimetryEnvironment.NewCoordinate(
                        file.ReadDouble(),//ShapeFile.ReadDouble64_LE(stream),
                        file.ReadDouble());//ShapeFile.ReadDouble64_LE(stream));

                record.Points.Add(p);
            }

            return true;
        }


        /// <summary>
        /// Записывает данные геометрического  объекта в указанный поток.
        /// </summary>
        /// <param name="geometry">Геометрический объект для записи</param>
        /// <param name="file">Поток записи</param>
        public override void Write(IGeometry geometry, BinaryWriter file)
        {            
            file.Write(int.Parse(Enum.Format(typeof(ShapeType), ShapeType, "d")));

            BoundingRectangle bounds = geometry.GetBoundingRectangle(); // GetEnvelopeExternal(/*geometryFactory.PrecisionModel,*/  box);
            file.Write(bounds.MinX);
            file.Write(bounds.MinY);
            file.Write(bounds.MaxX);
            file.Write(bounds.MaxY);

            int numParts = GetNumParts(geometry);
            int numPoints = geometry.CoordinateCount + numParts;
            file.Write(numParts);
            file.Write(numPoints);

            //parts
            int offset = 0;
            foreach (Contour contour in ((Polygon)geometry).Contours)
            {
                file.Write(offset);
                offset += contour.Vertices.Count + 1;
            }            
            
            foreach (Contour contour in ((Polygon)geometry).Contours)
            {
                System.Collections.Generic.List<ICoordinate> points = contour.Vertices.ToList();
                points.Add(PlanimetryEnvironment.NewCoordinate(contour.Vertices[0].X, contour.Vertices[0].Y));
                WriteCoords(points, file);
            }
        }

        private void WriteCoords(IEnumerable<ICoordinate> points, BinaryWriter file)
        {
            foreach (ICoordinate point in points)
            {
                file.Write(point.X);
                file.Write(point.Y);
            }
        }

        /// <summary>
        /// Получает длину в байтах геометрического объекта (для записи в файл)
        /// </summary>
        /// <param name="geometry">Геометрический объект </param>
        /// <returns>
        /// Длина для 16битового формата </returns>
        public override int GetLength(IGeometry geometry)
        {
            int numParts = GetNumParts(geometry);
            return (22 + (2 * numParts) + (geometry.CoordinateCount + numParts) * 8); // 22 => shapetype(2) + bbox(4*4) + numparts(2) + numpoints(2)
        }

        private int GetNumParts(IGeometry geometry)
        {
            int numParts = ((Polygon)geometry).Contours.Count;// -1 + 1;
            //else throw new InvalidOperationException("Should not get here.");
            return numParts;
        }

        /// <summary>
        /// Test if a point is in a list of coordinates.
        /// </summary>
        /// <param name="testPoint">TestPoint the point to test for.</param>
        /// <param name="pointList">PointList the list of points to look through.</param>
        /// <returns>true if testPoint is a point in the pointList list.</returns>
        private bool PointInList(ICoordinate testPoint, IEnumerable<ICoordinate> pointList)
        {
            foreach (ICoordinate p in pointList)
                if (p.ExactEquals(testPoint)) 
                    return true;
            return false;
        }
    }
}