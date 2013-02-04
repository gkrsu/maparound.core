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
** File: MultiLineHandler.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Handling linear objects of the Shape-file
**
=============================================================================*/

namespace MapAround.IO.Handlers
{
    using System;
    using System.IO;

    using MapAround.Geometry;
    using MapAround.IO;

    /// <summary>
    /// Обработчик для фигуры "Ломаная линия"
    /// </summary>
    internal class MultiLineHandler : ShapeHandler
    {
        /// <summary> Тип фигуры</summary>
        public override ShapeType ShapeType
        {
            get { return ShapeType.Polyline; }
        }

        /// <summary>
        /// Читает запись представляющую полигон.
        /// </summary>
        /// <param name="file">Входной поток</param>
        /// <param name="record">Запись Shape-файла в которую будет помещена прочитанная информация</param>
        /// <param name="bounds">Ограничивающий прямоугольник, с которым должен пересекаться ограничивающий прямоугольник записи</param>
        /// <returns>Успешность операции</returns>
        public override bool Read(Stream file, BoundingRectangle bounds, ShapeFileRecord record)
        {
            record.MinX = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            record.MinY = file.ReadDouble(); //ShapeFile.ReadDouble64_LE(stream);
            record.MaxX = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);
            record.MaxY = file.ReadDouble();// ShapeFile.ReadDouble64_LE(stream);

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
        /// Записывает данные ломаной линии в указанный поток. 
        ///</summary>
        /// <param name="geometry">объект ломаной линии для записи</param>
        /// <param name="file">Поток записи</param>        
        public override void Write(IGeometry geometry, BinaryWriter file)//, IGeometryFactory geometryFactory)
        {
            Polyline multi = (Polyline)geometry;

            file.Write(int.Parse(Enum.Format(typeof(ShapeType), ShapeType, "d")));

            BoundingRectangle box = multi.GetBoundingRectangle();
            file.Write(box.MinX);
            file.Write(box.MinY);
            file.Write(box.MaxX);
            file.Write(box.MaxY);

            int numParts = multi.Paths.Count;
            int numPoints = multi.CoordinateCount;

            file.Write(numParts);
            file.Write(numPoints);

            // Write the offsets
            int offset = 0;
            for (int i = 0; i < numParts; i++)
            {
                IGeometry g = multi.Paths[i];
                file.Write(offset);
                offset = offset + g.CoordinateCount;
            }

            ICoordinate[] points = multi.ExtractCoordinates();
            for (int i = 0; i < numPoints; i++)
            {
                file.Write(points[i].X);
                file.Write(points[i].Y);
            }           
        }


        /// <summary>
        /// Получить длину ломаной линии в байтах (для записи в файл)
        /// </summary>
        /// <param name="geometry">объект ломаной линии</param>
        /// <returns>
        /// Длина для 16битового формата </returns>
        public override int GetLength(IGeometry geometry)
        {
            int numParts = ((Polyline)geometry).Paths.Count;// GetNumParts(geometry);
            return (22 + (2 * numParts) + geometry.CoordinateCount/*.NumPoints*/ * 8); // 22 => shapetype(2) + bbox(4*4) + numparts(2) + numpoints(2)
        }
        
    }
}