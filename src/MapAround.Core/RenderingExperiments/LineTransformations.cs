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
** Файл: LineTransformations.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Назначение: Преобразование линейных объектов в линейные объекты.
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System.Collections.Generic;
    using System;

    using MapAround.Geometry;

    /// <summary>
    /// Интерфейс объекта преобразующего линейные объекты в другие линейные объекты.
    /// Такие преобразования используются для генерации штрихов, зигзагов (и других
    /// повторяющихся структур), для генерации параллельных линий.
    /// </summary>
    public interface ILineTransformer
    {
        /// <summary>
        /// Преобразует ломаную линию в коллекцию линейных объектов.
        /// </summary>
        /// <param name="path">Ломаная линия</param>
        /// <returns>Коллекция линейных объектов (LinePath и Contour)</returns>
        GeometryCollection GetLines(LinePath path);

        /// <summary>
        /// Преобразует контур в коллекцию линейных объектов.
        /// </summary>
        /// <param name="contour">Контур</param>
        /// <returns>Коллекция линейных объектов (LinePath и Contour)</returns>
        GeometryCollection GetLines(Contour contour);

        /// <summary>
        /// Смещение шаблона.
        /// </summary>
        double Offset
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Последовательность преобразований линий.
    /// </summary>
    public class LineTransformationSequence
    {
        List<ILineTransformer> _transformers = new List<ILineTransformer>();

        /// <summary>
        /// Преобразователи линий.
        /// </summary>
        public IList<ILineTransformer> Transformers
        {
            get { return _transformers; }
        }

        /// <summary>
        /// Применяет последовательность преобразований к ломаной линии.
        /// </summary>
        /// <param name="path">Ломаная линия</param>
        /// <returns>Коллекция преобразованных объектов</returns>
        public GeometryCollection GetLines(LinePath path)
        {
            GeometryCollection gc = new GeometryCollection();
            gc.Add(path);
            return GetLines(gc);
        }

        /// <summary>
        /// Применяет последовательность преобразований к контуру.
        /// </summary>
        /// <param name="contour">Контур</param>
        /// <returns>Коллекция преобразованных объектов</returns>
        public GeometryCollection GetLines(Contour contour)
        {
            GeometryCollection gc = new GeometryCollection();
            gc.Add(contour);
            return GetLines(gc);
        }

        /// <summary>
        /// Применяет последовательность преобразований к коллекции линейных объектов.
        /// </summary>
        /// <param name="sourceGeometries">Коллекция линейных объектов</param>
        /// <returns>Коллекция преобразованных объектов</returns>
        public GeometryCollection GetLines(GeometryCollection sourceGeometries)
        {
            GeometryCollection tempSource = new GeometryCollection();
            foreach (IGeometry g in sourceGeometries)
                tempSource.Add(g);

            GeometryCollection result = new GeometryCollection();

            foreach (ILineTransformer transformer in _transformers)
            {
                result.Clear();
                foreach (IGeometry g in tempSource)
                {
                    if (g.CoordinateCount < 2)
                        continue;

                    GeometryCollection gc = null;
                    if (g is LinePath)
                    {
                        gc = transformer.GetLines((LinePath)g);
                    }
                    else if (g is Contour)
                    {
                        gc = transformer.GetLines((Contour)g);
                    }
                    else
                        throw new ArgumentException("Source collection should contain only contours and linepaths.", "sourceGeometries");

                    foreach(IGeometry g1 in gc)
                    {
                        if(!(g1 is LinePath || g1 is Contour))
                            throw new InvalidOperationException("LineTransformation \"" + transformer.GetType().FullName + "\" has wrong output.");

                        result.Add(g1);
                    }
                }

                tempSource.Clear();
                foreach (IGeometry g in result)
                    tempSource.Add(g);
            }
            return result;
        }
    }

    /// <summary>
    /// Генератор штрихов.
    /// </summary>
    public class StrokeGenerator : ILineTransformer
    {
        double _offset = 0;
        double[] _strokes = new double[] { 20, 5 };

        /// <summary>
        /// Значения штрихов.
        /// </summary>
        public double[] Strokes
        {
            get { return _strokes; }
            set 
            {
                if (_strokes.Length < 2)
                    throw new ArgumentException("Strokes array should contain at least 2 values.", "value");

                if (_strokes.Length % 2 != 0)
                    throw new ArgumentException("Strokes array should contain an even number of items.", "value");

                foreach (double d in value)
                    if(d <= 0)
                        throw new ArgumentException("Invalid stroke length. Should be greater than zero.", "value");

                _strokes = value; 
            }
        }

        /// <summary>
        /// Смещение шаблона штриховки относительно начала линии.
        /// </summary>
        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        private List<ICoordinate> getStrokePoints(IList<ICoordinate> points, 
            double startLength, 
            double strokeLength, 
            ref int segmentIndex,
            ref double traversedLength)
        {
            List<ICoordinate> result = new List<ICoordinate>();

            double endLength = startLength + strokeLength;
            bool strokeStarted = false;

            for (int i = segmentIndex; i < points.Count - 1; i++)
            {
                double currentSegmentLength = PlanimetryAlgorithms.Distance(points[i], points[i + 1]);

                if (!strokeStarted)
                {
                    if (traversedLength + currentSegmentLength < startLength)
                    {
                        traversedLength += currentSegmentLength;
                        continue;
                    }
                    else
                    {
                        double remaindeLength = startLength - traversedLength;
                        double f = remaindeLength / (currentSegmentLength - remaindeLength);
                        ICoordinate p = PlanimetryEnvironment.NewCoordinate(
                            (points[i].X + f * points[i + 1].X) / (1 + f),
                            (points[i].Y + f * points[i + 1].Y) / (1 + f));
                        result.Add(p);
                        strokeStarted = true;
                    }
                }
                if (strokeStarted)
                {
                    if (traversedLength + currentSegmentLength <= endLength)
                    {
                        traversedLength += currentSegmentLength;
                        result.Add(points[i + 1]);
                        continue;
                    }
                    else
                    {
                        double remainderLength = endLength - traversedLength;
                        double f = remainderLength / (currentSegmentLength - remainderLength);
                        ICoordinate p = PlanimetryEnvironment.NewCoordinate(
                            (points[i].X + f * points[i + 1].X) / (1 + f),
                            (points[i].Y + f * points[i + 1].Y) / (1 + f));
                        result.Add(p);
                        segmentIndex = i;
                        return(result);
                    }
                }
            }

            segmentIndex = points.Count - 1;
            return result;
        }

        #region ILineTransformer Members

        /// <summary>
        /// Преобразует ломаную линию в коллекцию линейных объектов.
        /// </summary>
        /// <param name="path">Ломаная линия</param>
        /// <returns>Коллекция линейных объектов (LinePath и Contour)</returns>
        public GeometryCollection GetLines(LinePath path)
        {
            GeometryCollection result = new GeometryCollection();
            double length = path.Length();
            double strokePatternLength = 0;
            foreach (double d in _strokes)
                strokePatternLength += d;

            double offset = _offset % strokePatternLength;
            double currentLength = offset;
            double traversedLength = 0;
            int segmentIndex = 0;

            int strokeIndex = 0;
            while (currentLength < length)
            {
                double strokeLength = _strokes[strokeIndex];
                List<ICoordinate> strokePoints = 
                    getStrokePoints(path.Vertices, 
                        currentLength, 
                        strokeLength,
                        ref segmentIndex,
                        ref traversedLength);

                if (strokePoints.Count > 0)
                {
                    LinePath lp = new LinePath();
                    foreach (ICoordinate p in strokePoints)
                        lp.Vertices.Add(p);

                    result.Add(lp);
                }
                currentLength += strokeLength;

                strokeIndex++;
                currentLength += _strokes[strokeIndex];
                if (strokeIndex == _strokes.Length - 1)
                    strokeIndex = 0;
                else
                    strokeIndex++;
            }

            currentLength = offset;
            bool isSpace = true;
            for (int i = _strokes.Length - 1; i >= 0; i--)
            {
                double strokeLength = _strokes[i];
                if (strokeLength > currentLength)
                    strokeLength = currentLength;
                currentLength -= strokeLength;

                traversedLength = 0;
                segmentIndex = 0;
                if (!isSpace)
                {
                    List<ICoordinate> strokePoints = 
                        getStrokePoints(path.Vertices, 
                            currentLength, 
                            strokeLength,
                            ref segmentIndex,
                            ref traversedLength);

                    if (strokePoints.Count > 0)
                    {
                        LinePath lp = new LinePath();
                        foreach (ICoordinate p in strokePoints)
                            lp.Vertices.Add(p);

                        result.Add(lp);
                    }
                }
                if (currentLength == 0)
                    break;

                isSpace = !isSpace;
            }

            return result;
        }

        /// <summary>
        /// Преобразует контур в коллекцию линейных объектов.
        /// </summary>
        /// <param name="contour">Контур</param>
        /// <returns>Коллекция линейных объектов (LinePath и Contour)</returns>
        public GeometryCollection GetLines(Contour contour)
        {
            LinePath path = new LinePath(contour.Vertices);
            path.Vertices.Add(path.Vertices[0]);
            return GetLines(path);
        }

        #endregion
    }
}