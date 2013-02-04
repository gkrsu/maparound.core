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
** File: MultiPointHandler.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Handling MultiPoint objects of the Shape-file
**
=============================================================================*/

namespace MapAround.IO.Handlers
{
    using System;
    using System.IO;

    using MapAround.Geometry;
    using MapAround.IO;

    /// <summary>
    /// Обработчик коллекции точек
    /// </summary>
    internal class MultiPointHandler : ShapeHandler
    {
        /// <summary> Тип фигуры</summary>
        public override ShapeType ShapeType
        {
            get { return ShapeType.Multipoint; }// ShapeGeometryType.MultiPoint; }
        }

        /// <summary>
        /// Читает запись представляющую коллекцию точек.
        /// </summary>
        /// <param name="file">Входной поток</param>
        /// <param name="record">Запись Shape-файла в которую будет помещена прочитанная информация</param>
        /// <param name="bounds">Ограничивающий прямоугольник, с которым должен пересекаться ограничивающий прямоугольник записи</param>
        public override bool Read(Stream file, BoundingRectangle bounds, ShapeFileRecord record)
        {
            try
            {
                record.MinX = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
                record.MinY = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
                record.MaxX = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
                record.MaxY = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);

                int numPoints = file.ReadInt32();// ShapeFile.ReadInt32_LE(stream);

                if (!ShapeHandler.IsRecordInView(bounds, record))
                {
                    file.Seek((long)numPoints * 16, SeekOrigin.Current);
                    return false;
                }

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
            catch { throw; }
        }


        /// <summary>
        /// Записать данные геометрического  объекта в указанный поток 
        /// </summary>
        /// <param name="geometry">Геометрический объект для записи</param>
        /// <param name="file">Поток записи</param>
        public override void Write(IGeometry geometry, BinaryWriter file)
        {
            if (!(geometry is MultiPoint))
                throw new ArgumentException("Geometry Type error: MultiPoint expected, but the type retrieved is " + geometry.GetType().Name);

            MultiPoint mpoint = geometry as MultiPoint;
            
            file.Write(int.Parse(Enum.Format(typeof(ShapeType), ShapeType, "d")));

            BoundingRectangle bounds = geometry.GetBoundingRectangle();//GetEnvelopeExternal(/*geometryFactory.PrecisionModel,*/ box);
            file.Write(bounds.MinX);
            file.Write(bounds.MinY);
            file.Write(bounds.MaxX);
            file.Write(bounds.MaxY);

            int numPoints = mpoint.ExtractCoordinates().Length;//.NumPoints;
            file.Write(numPoints);						

            // write the points 
            for (int i = 0; i < numPoints; i++)
            {
                PointD point = new PointD(mpoint.Points[i]);// Geometries[i];
                file.Write(point.X);
                file.Write(point.Y);	
            }            
        }
		
        /// <summary>
        /// Получить длину в байтах геометрического объекта (для записи в файл)
        /// </summary>
        /// <param name="geometry">Геометрический объект </param>
        /// <returns>
        /// Длина для 16битового формата </returns>
        public override int GetLength(IGeometry geometry)
        {			
            return (20 + geometry.ExtractCoordinates().Length /*.NumPoints*/ * 8); // 20 => shapetype(2) + bbox (4*4) + numpoints
        }					
    }
}