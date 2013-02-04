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
** Файл: Scaning.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Назначение: Полигонизация.
**
=============================================================================*/

namespace MapAround.Rendering
{
    using MapAround.Geometry;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Интерфейс объекта выполняющего преобразование
    /// линейных объектов в площадные.
    /// </summary>
    interface IPolygonizer
    {
        /// <summary>
        /// Преобразует ломаную линию в полигон.
        /// </summary>
        /// <param name="path">Ломаная линия</param>
        /// <returns>Контуры полигона</returns>
        IList<Contour> GetPolygon(LinePath path);

        /// <summary>
        /// Преобразует контур в полигон.
        /// </summary>
        /// <param name="contour">Контур</param>
        /// <returns>Контуры полигона</returns>
        IList<Contour> GetPolygon(Contour contour);
    }

    /// <summary>
    /// Окончание линии.
    /// </summary>
    public enum LineCap
    {
        /// <summary>
        /// Плоское окончание.
        /// </summary>
        Flat,
        /// <summary>
        /// Квадратное окончание.
        /// </summary>
        Square,
        /// <summary>
        /// Круглое окончание.
        /// </summary>
        Round,
        /// <summary>
        /// Треугольное окончание.
        /// </summary>
        Triangle
    }

    /// <summary>
    /// Соединение линии.
    /// </summary>
    public enum LineJoin
    {
        /// <summary>
        /// Ус.
        /// </summary>
        Miter,
        /// <summary>
        /// Скос.
        /// </summary>
        Bevel,
        /// <summary>
        /// Круг.
        /// </summary>
        Round,
        /// <summary>
        /// Ус с ограничением.
        /// </summary>
        MiterClipped
    }

    /// <summary>
    /// Преобразователь линий в полигон.
    /// </summary>
    public class Polygonizer : IPolygonizer
    {
        private float _width = 1;
        private float _miterLimit = 3;

        private LineCap _lineStartCap = LineCap.Flat;
        private LineCap _lineEndCap = LineCap.Flat;
        private LineJoin _lineJoin = LineJoin.Bevel;

        private static double _twoPi = Math.PI * 2;
        private static double _halfPI = Math.PI * 0.5;
        private static double _squareCapAngle = Math.PI / 4;
        private static double _sqrt2 = Math.Sqrt(2);

        /// <summary>
        /// Получает или устанавливает значение ограничивающее длину
        /// соединения линий типа "ус".
        /// </summary>
        public float MiterLimit
        {
            get { return _miterLimit; }
            set 
            {
                if (value <= 0 || value > 10)
                    throw new ArgumentOutOfRangeException("value");

                _miterLimit = value; 
            }
        }

        /// <summary>
        /// Получает или устанавливает соединение линий.
        /// </summary>
        public LineJoin LineJoin
        {
            get { return _lineJoin; }
            set { _lineJoin = value; }
        }

        /// <summary>
        /// Получает или устанавливает базовую толщину полигона.
        /// </summary>
        public float Width
        {
            get { return _width; }
            set 
            {
                if(value <= 0)
                    throw new ArgumentOutOfRangeException("value");

                _width = value; 
            }
        }

        /// <summary>
        /// Получает или устанавливает тип начала линии.
        /// </summary>
        public LineCap LineStartCap
        {
            get { return _lineStartCap; }
            set { _lineStartCap = value; }
        }

        /// <summary>
        /// Получает или устанавливает тип окончания линии.
        /// </summary>
        public LineCap LineEndCap
        {
            get { return _lineEndCap; }
            set { _lineEndCap = value; }
        }

        private static ICoordinate[] getCirclePoints(ICoordinate point, double startAngle, double endAngle, double distance, int pointsPerCircle)
        {
            int n = (int)Math.Round(pointsPerCircle * (endAngle - startAngle) / _twoPi);
            ICoordinate[] result = new ICoordinate[n + 1];
            double angle = startAngle;
            double da = _twoPi / pointsPerCircle;

            for (int i = 0; i < n; i++)
            {
                result[i] = PlanimetryEnvironment.NewCoordinate(point.X + distance * Math.Cos(angle), point.Y + distance * Math.Sin(angle));
                angle += da;
            }
            result[n] = PlanimetryEnvironment.NewCoordinate(point.X + distance * Math.Cos(endAngle), point.Y + distance * Math.Sin(endAngle));

            return result;
        }

        private enum CapLocation
        { 
            Start,
            End
        }

        private double getSegmentAngle(double x1, double y1, double x2, double y2)
        {
            double result;
            if (x1 == x2)
                result = y1 < y2 ? Math.PI / 2 : 1.5 * Math.PI;
            else
                if (y1 == y2)
                    result = x1 < x2 ? 0 : Math.PI;
                else
                {
                    result = Math.Atan(Math.Abs(y2 - y1) / Math.Abs(x2 - x1));
                    if (x1 > x2 && y1 < y2)
                        result = Math.PI - result;
                    if (x1 > x2 && y1 > y2)
                        result += Math.PI;
                    if (x1 < x2 && y1 > y2)
                        result = _twoPi - result;
                }

            return result;
        }

        private void addCap(LineCap cap, 
            IList<ICoordinate> vertexList, 
            CapLocation capLocation, 
            double x1, 
            double y1, 
            double x2, 
            double y2)
        {
            double angle = getSegmentAngle(x1, y1, x2, y2);

            ICoordinate startPoint = PlanimetryEnvironment.NewCoordinate(0, 0);
            switch (capLocation)
            { 
                case CapLocation.Start:
                    angle += Math.PI / 2;
                    startPoint.X = x1;
                    startPoint.Y = y1;
                    break;
                case CapLocation.End:
                    angle -= Math.PI / 2;
                    startPoint.X = x2;
                    startPoint.Y = y2;
                    break;
            }

            switch (cap)
            { 
                case LineCap.Flat:
                    ICoordinate[] points = getCirclePoints(startPoint, angle, angle + Math.PI, _width / 2, 2);
                    foreach (ICoordinate p in points)
                        vertexList.Add(p);
                    break;
                case LineCap.Round:
                    int pointsPerCircle = (int)(Math.Round(Width * Math.PI) / 2);
                    points = getCirclePoints(startPoint, angle, angle + Math.PI, _width / 2, pointsPerCircle);
                    foreach (ICoordinate p in points)
                        vertexList.Add(p);
                    break;
                case LineCap.Square:
                    double r = _width * _sqrt2 / 2;
                    vertexList.Add(PlanimetryEnvironment.NewCoordinate(startPoint.X + r * Math.Cos(angle + _squareCapAngle),
                                              startPoint.Y + r * Math.Sin(angle + _squareCapAngle)));
                    vertexList.Add(PlanimetryEnvironment.NewCoordinate(startPoint.X + r * Math.Cos(angle - _squareCapAngle + Math.PI),
                                              startPoint.Y + r * Math.Sin(angle - _squareCapAngle + Math.PI)));
                    break;
                case LineCap.Triangle:
                    double halfWidth = _width / 2;
                    vertexList.Add(PlanimetryEnvironment.NewCoordinate(startPoint.X + halfWidth * Math.Cos(angle),
                          startPoint.Y + halfWidth * Math.Sin(angle)));

                    vertexList.Add(PlanimetryEnvironment.NewCoordinate(startPoint.X + halfWidth * Math.Cos(angle + _halfPI),
                          startPoint.Y + halfWidth * Math.Sin(angle + _halfPI)));

                    vertexList.Add(PlanimetryEnvironment.NewCoordinate(startPoint.X + halfWidth * Math.Cos(angle + Math.PI),
                          startPoint.Y + halfWidth * Math.Sin(angle + Math.PI)));
                    break;
            }
        }

        private static double translateAngleQuadrant(double angle, int quadrantNumber)
        {
            switch (quadrantNumber)
            {
                case 1: { return angle; }
                case 2: { return Math.PI - angle; }
                case 3: { return Math.PI + angle; }
                case 4: { return _twoPi - angle; }
            }

            return angle;
        }

        private static int pointQuadrantNumber(ICoordinate p)
        {
            if (p.X > 0)
            {
                if (p.Y > 0) return 1; else return 4;
            }
            else
            {
                if (p.Y > 0) return 2; else return 3;
            }
        }

        /// <summary>
        /// Возвращает угол между лучами p2p1 и p2p3 отсчитываемый 
        /// от луча p2p1
        /// </summary>
        private double getAngle(ICoordinate p1, ICoordinate p2, ICoordinate p3, bool counterClockwise)
        {
            // транслируем в начало координат
            p1.X -= p2.X; p1.Y -= p2.Y;
            p3.X -= p2.X; p3.Y -= p2.Y;

            double alpha = p1.X != 0 ? Math.Atan(Math.Abs(p1.Y / p1.X)) : _halfPI;
            double betta = p3.X != 0 ? Math.Atan(Math.Abs(p3.Y / p3.X)) : _halfPI;

            alpha = translateAngleQuadrant(alpha, pointQuadrantNumber(p1));
            betta = translateAngleQuadrant(betta, pointQuadrantNumber(p3));

            if (counterClockwise)
                return alpha < betta ? (betta - alpha) : (_twoPi - alpha + betta);
            else
                return alpha > betta ? (alpha - betta) : (_twoPi - betta + alpha);
        }

        private ICoordinate pointOnCircle(double baseX, double baseY, double radius, double angle)
        {
            return PlanimetryEnvironment.NewCoordinate(baseX + radius * Math.Cos(angle), baseY + radius * Math.Sin(angle));
        }

        private void addInteriorJoin(IList<ICoordinate> vertexList, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double angle1 = getSegmentAngle(x1, y1, x2, y2) + _halfPI;
            double angle2 = getSegmentAngle(x2, y2, x3, y3) + _halfPI;
            double distance = _width / 2;

            Segment s1 = new Segment(pointOnCircle(x1, y1, distance, angle1), pointOnCircle(x2, y2, distance, angle1));
            Segment s2 = new Segment(pointOnCircle(x2, y2, distance, angle2), pointOnCircle(x3, y3, distance, angle2));
            ICoordinate intersection = null;
            if (PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out intersection, out s1) == Dimension.Zero)
            {
                vertexList.Add(intersection);
                return;
            }

            angle1 -= Math.PI;
            angle2 -= Math.PI;

            vertexList.Add(pointOnCircle(x2, y2, distance, angle1));
            vertexList.Add(PlanimetryEnvironment.NewCoordinate(x2, y2));
            vertexList.Add(pointOnCircle(x2, y2, distance, angle2));
        }

        private void addJoin(LineJoin join, IList<ICoordinate> vertexList, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double angleBetweenSegments = getAngle(PlanimetryEnvironment.NewCoordinate(x1, y1), PlanimetryEnvironment.NewCoordinate(x2, y2), PlanimetryEnvironment.NewCoordinate(x3, y3), true);

            if (angleBetweenSegments < Math.PI)
            {
                addInteriorJoin(vertexList, x1, y1, x2, y2, x3, y3);
                return;
            }

            double angle1 = getSegmentAngle(x1, y1, x2, y2) - _halfPI;
            double angle2 = getSegmentAngle(x2, y2, x3, y3) - _halfPI;

            if (join == LineJoin.Round && _width <= 2)
                join = LineJoin.Bevel;

            double halfWidth = _width / 2;
            switch (join)
            { 
                case LineJoin.Bevel:
                    vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle1));
                    vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle2));
                    break;
                case LineJoin.Round:
                    ICoordinate[] points =
                        getCirclePoints(PlanimetryEnvironment.NewCoordinate(x2, y2),
                        angle1,
                        angle1 - Math.PI + angleBetweenSegments, 
                        halfWidth,
                        (int)(Math.Round(_width * Math.PI) / 2));

                    foreach (ICoordinate p in points)
                        vertexList.Add(p);
                    break;
                case LineJoin.Miter:
                case LineJoin.MiterClipped:

                    Segment s1 = new Segment(pointOnCircle(x1, y1, halfWidth, angle1), pointOnCircle(x2, y2, halfWidth, angle1));
                    Segment s2 = new Segment(pointOnCircle(x3, y3, halfWidth, angle2), pointOnCircle(x2, y2, halfWidth, angle2));

                    ICoordinate miterPoint = null;

                    if (PlanimetryAlgorithms.DirectsIntersection(s1, s2, ref miterPoint) == Dimension.Zero)
                    {
                        double miterDistance2 = PlanimetryAlgorithms.Distance(miterPoint, PlanimetryEnvironment.NewCoordinate(x2, y2));
                        if (miterDistance2 < _miterLimit * _width / 2)
                            vertexList.Add(miterPoint);
                        else
                        {
                            if (join == LineJoin.MiterClipped)
                            {
                                vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle1));
                                vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle2));
                            }
                            else
                            {
                                double l = _miterLimit * _width * 0.5;
                                double miterDistance1 = PlanimetryAlgorithms.Distance(s1.V2, miterPoint);
                                double clipDistance = ((miterDistance2 - l) * (miterDistance2 - l) + l) / miterDistance1;

                                double f = clipDistance / (miterDistance1 - clipDistance);
                                vertexList.Add(PlanimetryEnvironment.NewCoordinate((f * s1.V2.X + miterPoint.X) / (1 + f),
                                                          (f * s1.V2.Y + miterPoint.Y) / (1 + f)));
                                vertexList.Add(PlanimetryEnvironment.NewCoordinate((f * s2.V2.X + miterPoint.X) / (1 + f),
                                                          (f * s2.V2.Y + miterPoint.Y) / (1 + f)));
                            }
                        }
                    }
                    else
                    {
                        if (join == LineJoin.MiterClipped)
                        {
                            vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle1));
                            vertexList.Add(pointOnCircle(x2, y2, halfWidth, angle2));
                        }
                        else
                        {
                            double d = Math.Sqrt(_width * _width / 4 + _miterLimit * _miterLimit);
                            double alpha = Math.Atan(1 / _miterLimit);
                            vertexList.Add(pointOnCircle(x2, y2, d, angle1 - alpha));
                            vertexList.Add(pointOnCircle(x2, y2, d, angle1 + alpha));
                        }
                    }
                    break;
            }
        }

        private Contour getPathContour(LinePath path)
        {
            Contour c = new Contour();
            IList<ICoordinate> pv = path.Vertices;
            if (pv.Count > 1)
            {
                // начало линии
                addCap(_lineStartCap,
                    c.Vertices,
                    CapLocation.Start,
                    pv[0].X, pv[0].Y,
                    pv[1].X, pv[1].Y);

                int cnt = path.Vertices.Count;
                for (int i = 0; i < cnt - 2; i++)
                    addJoin(_lineJoin,
                        c.Vertices,
                        pv[i].X, pv[i].Y,
                        pv[i + 1].X, pv[i + 1].Y,
                        pv[i + 2].X, pv[i + 2].Y);

                // окончание линии
                addCap(_lineEndCap,
                    c.Vertices,
                    CapLocation.End,
                    pv[cnt - 2].X, pv[cnt - 2].Y,
                    pv[cnt - 1].X, pv[cnt - 1].Y);

                // соединения линий "по правую сторону"
                for (int i = cnt - 1; i > 1; i--)
                    addJoin(_lineJoin,
                        c.Vertices,
                        pv[i].X, pv[i].Y,
                        pv[i - 1].X, pv[i - 1].Y,
                        pv[i - 2].X, pv[i - 2].Y);
            }

            return c;
        }

        private Contour geContourContour(Contour contour)
        {
            Contour c = new Contour();
            IList<ICoordinate> cv = contour.Vertices;
            if (cv.Count > 2)
            {
                // начало линии
                addCap(LineCap.Flat,
                    c.Vertices,
                    CapLocation.Start,
                    cv[0].X, cv[0].Y,
                    cv[1].X, cv[1].Y);

                int cnt = contour.Vertices.Count;
                for (int i = 0; i < cnt; i++)
                {
                    int i1 = i < cnt - 1 ? i + 1 : 0;
                    int i2 = i1 < cnt - 1 ? i1 + 1 : 0;
                    addJoin(_lineJoin,
                        c.Vertices,
                        cv[i].X, cv[i].Y,
                        cv[i1].X, cv[i1].Y,
                        cv[i2].X, cv[i2].Y);
                }

                // окончание линии
                addCap(LineCap.Flat,
                    c.Vertices,
                    CapLocation.End,
                    cv[0].X, cv[0].Y,
                    cv[1].X, cv[1].Y);


                int ii = 2;    
                for (int j = 0; j < cnt; j++)
                {
                    ii--;
                    if (ii == -1)
                        ii = cnt - 1;
                    int i1 = ii > 0 ? ii - 1 : cnt - 1;
                    int i2 = i1 > 0 ? i1 - 1 : cnt - 1;

                    addJoin(_lineJoin,
                        c.Vertices,
                        cv[ii].X, cv[ii].Y,
                        cv[i1].X, cv[i1].Y,
                        cv[i2].X, cv[i2].Y);
                }
            }
            return c;
        }

        #region IPolygonizer Members

        /// <summary>
        /// Преобразует ломаную линию в полигон.
        /// </summary>
        /// <param name="path">Ломаная линия</param>
        /// <returns>Контуры полигона</returns>
        public IList<Contour> GetPolygon(LinePath path)
        {
            List<Contour> contours = new List<Contour>();
            contours.Add(getPathContour(path));

            return contours;
        }

        /// <summary>
        /// Преобразует контур в полигон.
        /// </summary>
        /// <param name="contour">Контур</param>
        /// <returns>Контуры полигона</returns>
        public IList<Contour> GetPolygon(Contour contour)
        {
            List<Contour> contours = new List<Contour>();
            contours.Add(geContourContour(contour));
            return contours;
        }

        #endregion
    }
}