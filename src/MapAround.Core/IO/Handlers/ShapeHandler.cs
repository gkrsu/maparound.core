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
** Description: Handling Shape-file objects
**
=============================================================================*/


namespace MapAround.IO.Handlers
{
    using System.IO;

    using MapAround.Geometry;
    using MapAround.IO;

    /// <summary>
    /// Базовый класс обработчиков геометрической фигуры 
    /// (для чтения из потока и записи данных в поток)
    /// </summary>
    internal abstract class ShapeHandler
    {
        /// <summary> Тип фигуры</summary>
        public abstract ShapeType ShapeType { get; }

        /// <summary>
        /// Читать потока данные по геометрическому объекту и заполнять запись shape-файла 
        /// </summary>
        /// <param name="file">Входной поток для чтения</param>
        /// <param name="bounds">Ограничивающий прямоугольник, с которым должен пересекаться ограничивающий прямоугольник записи</param>
        /// <param name="Record">Запись Shape-файла в которую будет помещена прочитанная информация</param>
        /// <returns>Успешность операции</returns>
        public abstract bool Read(Stream file, BoundingRectangle bounds, ShapeFileRecord Record);

        /// <summary>
        /// Записать данные геометрического  объекта в указанный поток 
        /// </summary>
        /// <param name="geometry">Геометрический объект для записи</param>
        /// <param name="file">Поток записи</param>
        public abstract void Write(IGeometry geometry, BinaryWriter file);

        /// <summary>
        /// Получить длину в байтах геометрического объекта (для записи в файл)
        /// </summary>
        /// <param name="geometry">Геометрический объект </param>
        /// <returns>
        /// Длина для 16битового формата </returns>
        public abstract int GetLength(IGeometry geometry);

        

        /// <summary>Проверка записи на нахождение границ фигуры в указанной области</summary>
        /// <param name="bounds">Границы области</param>
        /// <param name="record">Запись shape-файла</param>
        /// <returns></returns>
        protected static bool IsRecordInView(BoundingRectangle bounds, ShapeFileRecord record)
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

        #region Deprecated

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="shapeType"></param>
        ///// <returns></returns>
        //[System.Obsolete]
        //public static bool IsPoint(ShapeType shapeType)
        //{
        //    return shapeType == ShapeType.Point;
        //    //||
        //    //       shapeType == ShapeGeometryType.PointZ ||
        //    //       shapeType == ShapeGeometryType.PointM ||
        //    //       shapeType == ShapeGeometryType.PointZM;
        //}


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="shapeType"></param>
        ///// <returns></returns>
        //[System.Obsolete]
        //public static bool IsMultiPoint(ShapeType shapeType)
        //{
        //    return shapeType == ShapeType.Multipoint;
        //    //||
        //    //       shapeType == ShapeGeometryType.MultiPointZ ||
        //    //       shapeType == ShapeGeometryType.MultiPointM ||
        //    //       shapeType == ShapeGeometryType.MultiPointZM;
        //}


        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="shapeType"></param>
        ///// <returns></returns>
        //[System.Obsolete]
        //public static bool IsLineString(ShapeType shapeType)
        //{
        //    return shapeType == ShapeType.Polyline;//.LineString;
        //    //||
        //    //       shapeType == ShapeGeometryType.LineStringZ ||
        //    //       shapeType == ShapeGeometryType.LineStringM ||
        //    //       shapeType == ShapeGeometryType.LineStringZM;            
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="shapeType"></param>
        ///// <returns></returns>
        //[System.Obsolete]
        //public static bool IsPolygon(ShapeType shapeType)
        //{
        //    return shapeType == ShapeType.Polygon;
        //    //||
        //    //   shapeType == ShapeGeometryType.PolygonZ ||
        //    //   shapeType == ShapeGeometryType.PolygonM ||
        //    //   shapeType == ShapeGeometryType.PolygonZM;
        //}

        #endregion
    }
}