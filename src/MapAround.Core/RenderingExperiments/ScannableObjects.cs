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
** Назначение: Классы поддерживающие горизонтальное сканирование.
** (код выглядит странно, оптимизация производительности заставила
**  писать так)
**
=============================================================================*/

namespace MapAround.Rendering
{
    using System;
    using System.Drawing;

    using MapAround.Geometry;
    using System.Collections.Generic;

    /// <summary>
    /// Простая окружность.
    /// </summary>
    public class SimpleCircle : IScannable
    {
        private float _radius;
        private float _rPow2;
        private ICoordinate _center;
        private BoundingRectangle _br;

        private int _minX = 0;
        private int _maxX = 0;
        private int _minY = 0;
        private int _maxY = 0;

        #region ICloneable Members

        /// <summary>
        /// Возвращает копию этого объекта.
        /// </summary>
        public object Clone()
        {
            SimpleCircle result = new SimpleCircle(_center, _radius);
            return result;
        }

        #endregion

        #region IScannable Members

        /// <summary>
        /// Уведомляет объект о начале процесса сканирования.
        /// </summary>
        /// <param name="minX">Минимальная координата X области сканирования</param>
        /// <param name="maxX">Максимальная координата X области сканирования</param>
        /// <param name="minY">Минимальная координата Y области сканирования</param>
        /// <param name="maxY">Максимальная координата Y области сканирования</param>
        /// <param name="orientation">Направление сканирования</param>
        public void InitScaning(int minX, int maxX, int minY, int maxY, Orientation orientation)
        {
            _minX = minX;
            _minY = minY;
            _maxX = maxX;
            _maxY = maxY;
        }

        /// <summary>
        /// Ограничивающий прямоугольник окружности.
        /// </summary>
        public BoundingRectangle BoundingBox
        {
            get { return _br; }
        }

        /// <summary>
        /// Вычисляет пересечения границ объекта с горизонтальным отрезком.
        /// </summary>
        /// <param name="scanY">Координата Y горизонтального отрезка</param>
        /// <param name="intersections">Пересечения</param>
        public void ComputeHorizontalIntersections(float scanY, out float[] intersections)
        {
            double sc = Math.Abs(scanY - _center.Y);
            if (_radius < sc)
            {
                intersections = new float[] { };
                return;
            }

            double sqrt = (float)Math.Sqrt(_rPow2 - sc * sc);
            double x1 = -sqrt + _center.X;
            double x2 = sqrt + _center.X;

            if (x1 < _minX)
                x1 = _minX;

            if (x2 > _maxX)
                x2 = _maxX;

            if (x2 < _minX)
            {
                intersections = new float[] { };
                return;
            }

            intersections = new float[] { (float)x1, (float)x2 };
        }

        /// <summary>
        /// Вычисляет пересечения границ объекта с вертикальным отрезком.
        /// </summary>
        /// <param name="scanX">Координата X вертикального отрезка</param>
        /// <param name="intersections">Пересечения</param>
        public void ComputeVerticalIntersections(float scanX, out float[] intersections)
        {
            double sc = Math.Abs(scanX - _center.X);
            if (_radius < sc)
            {
                intersections = new float[] { };
                return;
            }

            double sqrt = (float)Math.Sqrt(_rPow2 - sc * sc);
            double y1 = -sqrt + _center.Y;
            double y2 = sqrt + _center.Y;

            if (y1 < _minY)
                y1 = _minY;

            if (y2 > _maxY)
                y2 = _maxY;

            if (y2 < _minY)
            {
                intersections = new float[] {};
                return;
            }

            intersections = new float[] { (float)y1, (float)y2 };
        }

        #endregion

        /// <summary>
        /// Создает экземпляр SimpleCircle.
        /// </summary>
        /// <param name="center">Центр</param>
        /// <param name="radius">Радиус</param>
        public SimpleCircle(ICoordinate center, float radius)
        {
            _center = center;
            _radius = radius;
            _rPow2 = _radius * _radius;
            _br = new BoundingRectangle(
                    PlanimetryEnvironment.NewCoordinate(_center.X - _radius, _center.Y - _radius),
                    PlanimetryEnvironment.NewCoordinate(_center.X + _radius, _center.Y + _radius));
        }
    }

    /// <summary>
    /// Регион (обертка для полигона).
    /// </summary>
    public class Region : IScannable
    {
        private Polygon _polygon;
        private BoundingRectangle _br;
        private InteriorFillMode _fillingMode;

        private BoundingRectangle[] _contourBounds;
        private bool _contourOdd = true;

        private class EdgeTableItem
        {
            public double V1x;
            public double V1y;
            public double V2x;
            public double V2y; 
            public Contour Contour;
            public int IndexInContour;
            public int Index;
        }

        private List<EdgeTableItem> _edgeTable = new List<EdgeTableItem>();

        private int _minX = 0;
        private int _maxX = 0;
        private int _minY = 0;
        private int _maxY = 0;

        #region ICloneable Members

        /// <summary>
        /// Возвращает копию этого объекта.
        /// </summary>
        public object Clone()
        {
            Region result = new Region(_polygon, _fillingMode);
            return result;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public InteriorFillMode FillMode
        {
            get { return _fillingMode; }
            set { _fillingMode = value; }
        }

        #region IScannable Members

        private void fillEdgeTableForHorizontalScanning()
        {
            double minX = _minX, minY = _minY;
            double maxX = _maxX, maxY = _maxY;

            int k = -1;
            foreach (Contour c in _polygon.Contours)
            {
                int cnt = c.Vertices.Count;
                for (int i = 0; i < cnt; i++)
                {
                    k++;

                    int j = i == cnt - 1 ? 0 : i + 1;
                    double v1x, v2x, v1y, v2y;

                    v1y = c.Vertices[i].Y;
                    v2y = c.Vertices[j].Y;

                    if (v1y > maxY && v2y > maxY)
                        continue;
                    if (v1y < minY && v2y < minY)
                        continue;

                    v1x = c.Vertices[i].X;
                    v2x = c.Vertices[j].X;

                    if (_fillingMode == InteriorFillMode.Alternate)
                    {
                        if (v1x < minX && v2x < minX)
                            continue;
                        if (v1x > maxX && v2x > maxX)
                            continue;
                    }

                    _edgeTable.Add(
                        new EdgeTableItem()
                        {
                            V1x = v1x, V1y = v1y,
                            V2x = v2x, V2y = v2y,
                            Contour = c,
                            IndexInContour = i,
                            Index = k
                        });
                }
            }

            _edgeTable.Sort((EdgeTableItem s1, EdgeTableItem s2) =>
                Math.Min(s1.V1y, s1.V2y) < Math.Min(s2.V1y, s2.V2y) ? -1 : 1);
        }

        private void fillEdgeTableForVerticalScanning()
        {
            double minX = _minX, minY = _minY;
            double maxX = _maxX, maxY = _maxY;

            int k = -1;
            foreach (Contour c in _polygon.Contours)
            {
                int cnt = c.Vertices.Count;
                for (int i = 0; i < cnt; i++)
                {
                    k++;

                    int j = i == cnt - 1 ? 0 : i + 1;
                    double v1x, v2x, v1y, v2y;

                    v1x = c.Vertices[i].X;
                    v2x = c.Vertices[j].X;

                    if (v1x > maxX && v2x > maxX)
                        continue;
                    if (v1x < minX && v2x < minX)
                        continue;

                    v1y = c.Vertices[i].Y;
                    v2y = c.Vertices[j].Y;

                    if (_fillingMode == InteriorFillMode.Alternate)
                    {
                        if (v1y < minY && v2y < minY)
                            continue;
                        if (v1y > maxY && v2y > maxY)
                            continue;
                    }

                    _edgeTable.Add(
                        new EdgeTableItem()
                        {
                            V1x = v1x, V1y = v1y,
                            V2x = v2x, V2y = v2y,
                            Contour = c,
                            IndexInContour = i,
                            Index = k
                        });
                }
            }

            _edgeTable.Sort((EdgeTableItem s1, EdgeTableItem s2) =>
                Math.Min(s1.V1x, s1.V2x) < Math.Min(s2.V1x, s2.V2x) ? -1 : 1);
        }

        /// <summary>
        /// Уведомляет объект о начале процесса сканирования.
        /// </summary>
        /// <param name="minX">Минимальная координата X области сканирования</param>
        /// <param name="maxX">Максимальная координата X области сканирования</param>
        /// <param name="minY">Минимальная координата Y области сканирования</param>
        /// <param name="maxY">Максимальная координата Y области сканирования</param>
        /// <param name="orientation">Направление сканирования</param>
        public void InitScaning(int minX, int maxX, int minY, int maxY, Orientation orientation)
        {
            _minX = minX;
            _minY = minY;
            _maxX = maxX;
            _maxY = maxY;

            switch (orientation)
            { 
                case Orientation.Horizontal:
                    fillEdgeTableForHorizontalScanning();
                    break;
                case Orientation.Vertical:
                    fillEdgeTableForVerticalScanning();
                    break;
            }
        }

        /// <summary>
        /// Возвращает ограничивающий прямоугольник объекта.
        /// </summary>
        public BoundingRectangle BoundingBox
        {
            get
            { return _br; }
        }

        /// <summary>
        /// Вычисляет пересечения границобъекта с горизонтальным сканирующим отрезком.
        /// </summary>
        /// <param name="scanY">Координата Y сканирующего отрезка</param>
        /// <param name="intersections">Пересечения</param>
        public void ComputeHorizontalIntersections(float scanY, out float[] intersections)
        {
            switch (_fillingMode)
            {
                case InteriorFillMode.Alternate:
                    computeHorizontalAlternateIntersections(scanY, out intersections);
                    return;
                case InteriorFillMode.Winding:
                    computeHorizontalWindingIntersections(scanY, out intersections);
                    return;
            }

            intersections = new float[0];
        }

        /// <summary>
        /// Вычисляет пересечения границ объекта с вертикальным сканирующим отрезком.
        /// </summary>
        /// <param name="scanX">Координата X вертикального отрезка</param>
        /// <param name="intersections">Пересечения</param>
        public void ComputeVerticalIntersections(float scanX, out float[] intersections)
        {
            switch (_fillingMode)
            {
                case InteriorFillMode.Alternate:
                    computeVerticalAlternateIntersections(scanX, out intersections);
                    return;
                case InteriorFillMode.Winding:
                    computeVerticalWindingIntersections(scanX, out intersections);
                    return;
            }

            intersections = new float[0];
        }

        #endregion

        private bool containsPoint(double x, double y)
        {
            int crossCount = 0;

            foreach (Contour c in _polygon.Contours)
            {
                int vc = c.Vertices.Count;
                for (int i = 0; i < vc; i++)
                {
                    int j = i == vc - 1 ? 0 : i + 1;
                    double p1y = c.Vertices[i].Y;
                    double p2y = c.Vertices[j].Y;

                    if ((p1y > y) && (p2y <= y) || (p1y <= y) && (p2y > y))
                    {
                        double x1 = c.Vertices[i].X - x;
                        double y1 = p1y - y;
                        double x2 = c.Vertices[j].X - x;
                        double y2 = p2y - y;

                        if ((x1 * y2 - x2 * y1) / (y2 - y1) > 0)
                            crossCount++;
                    }
                }
            }

            return crossCount % 2 == 1;
        }

        private void computeHorizontalProperCrossesWinding(double scanY, Contour targetContour, List<double> crosses)
        {
            List<int> indicesToDelete = new List<int>();
            List<KeyValuePair<int, double>> crs = new List<KeyValuePair<int,double>>();
            double minCross = double.MaxValue;

            for (int i = 0; i < _edgeTable.Count; i++)
            {
                EdgeTableItem eti = _edgeTable[i];

                if (eti.Contour != targetContour)
                    continue;

                double v1y = eti.V1y;
                double v2y = eti.V2y;

                if (v1y < scanY && v2y < scanY)
                    indicesToDelete.Add(i);
                else
                {
                    if (v1y > scanY && v2y > scanY)
                        break;

                    if (v2y == scanY && v1y == scanY)
                        continue;

                    double v1x = eti.V1x;
                    double v2x = eti.V2x;
                    int etiIndex = eti.Index;

                    double crossX = v1x + (scanY - v1y) / (v2y - v1y) * (v2x - v1x);
                    if (!(v1x == crossX && v1y == scanY))
                        if (v2x == crossX && v2y == scanY)
                        {
                            Contour c = eti.Contour;
                            int index = eti.IndexInContour;
                            bool isDownToUp;
                            if (needToAddEndPointHorizontal(c, index == c.Vertices.Count - 1 ? 0 : index + 1, out isDownToUp))
                            {
                                crs.Add(new KeyValuePair<int, double>(etiIndex, crossX));
                                if (crossX < minCross)
                                {
                                    minCross = crossX;
                                    _contourOdd = isDownToUp;
                                }
                            }
                        }
                        else
                        {
                            crs.Add(new KeyValuePair<int, double>(etiIndex, crossX));
                            if (crossX < minCross)
                            {
                                minCross = crossX;
                                _contourOdd = v1y > v2y;
                            }
                        }
                }
            }

            for (int i = indicesToDelete.Count - 1; i >= 0; i--)
                _edgeTable.RemoveAt(indicesToDelete[i]);

            crs.Sort((KeyValuePair<int, double> s1, KeyValuePair<int, double> s2) =>
                s1.Key < s2.Key ? -1 : 1);

            for (int i = 0; i < crs.Count; i++)
                crosses.Add(crs[i].Value);
        }

        private void computeHorizontalProperCrossesAlternate(double scanY, List<double> crosses)
        {
            List<int> indicesToDelete = new List<int>();
            for(int i = 0; i < _edgeTable.Count; i++)
            {
                EdgeTableItem eti = _edgeTable[i];

                double v1y = eti.V1y;
                double v2y = eti.V2y;

                if (v1y < scanY && v2y < scanY)
                    indicesToDelete.Add(i);
                else
                {
                    if (v1y > scanY && v2y > scanY)
                        break;

                    if (v2y == scanY && v1y == scanY)
                        continue;

                    double v1x = eti.V1x;
                    double v2x = eti.V2x;

                    double crossX = v1x + (scanY - v1y) / (v2y - v1y) * (v2x - v1x);
                    if (crossX > _minX && crossX <= _maxX)
                    {
                        if (!(v1x == crossX && v1y == scanY))
                            if (v2x == crossX && v2y == scanY)
                            {
                                Contour c = eti.Contour;
                                int index = eti.IndexInContour;
                                if (needToAddEndPointHorizontal(c, index == c.Vertices.Count - 1 ? 0 : index + 1))
                                    crosses.Add(crossX);
                            }
                            else
                                crosses.Add(crossX);
                    }
                    else
                        if (crossX == _minX)
                        {
                            if (!crosses.Contains(crossX))
                                crosses.Add(crossX);
                        }
                }
            }

            for (int i = indicesToDelete.Count - 1; i >= 0; i--)
                _edgeTable.RemoveAt(indicesToDelete[i]);
        }

        private void computeVerticalProperCrossesAlternate(double scanX, List<double> crosses)
        {
            List<int> indicesToDelete = new List<int>();
            for (int i = 0; i < _edgeTable.Count; i++)
            {
                EdgeTableItem eti = _edgeTable[i];

                double v1x = eti.V1x;
                double v2x = eti.V2x;

                if (v1x < scanX && v2x < scanX)
                    indicesToDelete.Add(i);
                else
                {
                    if (v1x > scanX && v2x > scanX)
                        break;

                    if (v2x == scanX && v1x == scanX)
                        continue;

                    double v1y = eti.V1y;
                    double v2y = eti.V2y;

                    double crossY = v1y + (scanX - v1x) / (v2x - v1x) * (v2y - v1y);
                    if (crossY > _minY && crossY <= _maxY)
                    {
                        if (!(v1y == crossY && v1x == scanX))
                            if (v2y == crossY && v2x == scanX)
                            {
                                Contour c = eti.Contour;
                                int index = eti.IndexInContour;
                                if (needToAddEndPointVertical(c, index == c.Vertices.Count - 1 ? 0 : index + 1))
                                    crosses.Add(crossY);
                            }
                            else
                                crosses.Add(crossY);
                    }
                    else
                        if (crossY == _minY)
                        {
                            if (!crosses.Contains(crossY))
                                crosses.Add(crossY);
                        }
                }
            }


            for (int i = indicesToDelete.Count - 1; i >= 0; i--)
                _edgeTable.RemoveAt(indicesToDelete[i]);
        }

        private void computeVerticalProperCrossesWinding(double scanX, Contour targetContour, List<double> crosses)
        {
            List<int> indicesToDelete = new List<int>();
            List<KeyValuePair<int, double>> crs = new List<KeyValuePair<int, double>>();
            double minCross = double.MaxValue;

            for (int i = 0; i < _edgeTable.Count; i++)
            {
                EdgeTableItem eti = _edgeTable[i];

                if (eti.Contour != targetContour)
                    continue;

                double v1x = eti.V1x;
                double v2x = eti.V2x;

                if (v1x < scanX && v2x < scanX)
                    indicesToDelete.Add(i);
                else
                {
                    if (v1x > scanX && v2x > scanX)
                        break;

                    if (v2x == scanX && v1x == scanX)
                        continue;

                    double v1y = eti.V1y;
                    double v2y = eti.V2y;
                    int etiIndex = eti.Index;

                    double crossY = v1y + (scanX - v1x) / (v2x - v1x) * (v2y - v1y);
                    if (!(v1y == crossY && v1x == scanX))
                        if (v2y == crossY && v2x == scanX)
                        {
                            Contour c = eti.Contour;
                            int index = eti.IndexInContour;
                            bool isDowntoUp;
                            if (needToAddEndPointVertical(c, index == c.Vertices.Count - 1 ? 0 : index + 1, out isDowntoUp))
                            {
                                crs.Add(new KeyValuePair<int, double>(etiIndex, crossY));
                                if (minCross > crossY)
                                {
                                    minCross = crossY;
                                    _contourOdd = isDowntoUp;
                                }
                            }
                        }
                        else
                        {
                            crs.Add(new KeyValuePair<int, double>(etiIndex, crossY));
                            if (minCross > crossY)
                            {
                                minCross = crossY;
                                _contourOdd = v1x < v2x;
                            }
                        }
                }
            }

            for (int i = indicesToDelete.Count - 1; i >= 0; i--)
                _edgeTable.RemoveAt(indicesToDelete[i]);

            crs.Sort((KeyValuePair<int, double> s1, KeyValuePair<int, double> s2) =>
                s1.Key < s2.Key ? -1 : 1);

            for (int i = 0; i < crs.Count; i++)
                crosses.Add(crs[i].Value);
        }

        private class IntersectionItem
        {
            public float Value;
            public bool IsBeginPoint;
        }

        private void unionIntervals(List<IntersectionItem> intersectionItemList, out float[] intersections)
        {
            intersectionItemList.Sort((IntersectionItem i1, IntersectionItem i2) => i1.Value < i2.Value ? -1 : 1);
            List<float> result = new List<float>();
            int count = 0;
            int cnt = intersectionItemList.Count;
            for (int i = 0; i < cnt; i++)
            {
                IntersectionItem ii = intersectionItemList[i];
                if (count == 0) result.Add(ii.Value);

                if (ii.IsBeginPoint)
                    count++;
                else
                    count--;

                if (count == 0) result.Add(ii.Value);
            }

            int rc = result.Count;
            intersections = new float[rc];
            for (int i = 0; i < rc; i++)
                intersections[i] = result[i];
        }

        private void computeHorizontalWindingIntersections(float scanY, out float[] intersections)
        {
            List<IntersectionItem> intersectionItemList = new List<IntersectionItem>();
            int i = 0;
            foreach (Contour c in _polygon.Contours)
            {
                if (_contourBounds[i].MaxY > scanY && _contourBounds[i].MinY < scanY)
                {
                    float[] cInts;
                    computeHorizontalWindingIntersections(scanY, out cInts, c);

                    bool isBeginPoint = _contourOdd;
                    foreach (float f in cInts)
                    {
                        intersectionItemList.Add(new IntersectionItem() { Value = f, IsBeginPoint = isBeginPoint });
                        isBeginPoint = !isBeginPoint;
                    }
                }
                i++;
            }

            unionIntervals(intersectionItemList, out intersections);
        }

        private void computeHorizontalWindingIntersections(float scanY, out float[] intersections, Contour c)
        {
            intersections = new float[0];
            List<double> crosses = new List<double>();

            computeHorizontalProperCrossesWinding(scanY, c, crosses);

            if (crosses.Count % 2 == 1)
            {
                intersections = new float[0];
                return;
            }

            // индексы пересечений
            List<int> indices = new  List<int>(crosses.Count);
            for (int i = 0; i < crosses.Count; i++)
                indices.Add(i);

            // располагаем индексы парасечений в порядке возрастания абсциссы
            indices.Sort((int i1, int i2) => crosses[i1] < crosses[i2] ? -1 : 1);

            Stack<int> stack = new Stack<int>();
            List<double> tempList = new List<double>();

            // Помещаем индексы в стек.
            // Если помещаемый в стек индекс имеет четность не равную вершине стека,
            // то вершина стека удаляется.
            // Если после удаления стек пуст,
            // то мы нашли новый отрезок сканирующей линии.
            for (int i = 0; i < indices.Count; i++)
            {
                if (stack.Count == 0)
                    stack.Push(indices[i]);
                else
                { 
                    int top = stack.Peek();
                    if (top % 2 == indices[i] % 2)
                        stack.Push(indices[i]);
                    else
                    {
                        stack.Pop();
                        if(stack.Count == 0)
                        {
                            tempList.Add(crosses[top]);
                            tempList.Add(crosses[indices[i]]);
                        }
                    }
                }
            }

            crosses.Clear();
            bool needToAddMinX = false;
            bool needToAddMaxX = false;

            int cnt = tempList.Count;
            for (int i = 0; i < cnt; i++)
            {
                double currentItem = tempList[i];
                if (currentItem < _minX)
                    needToAddMinX = !needToAddMinX;
                else
                {
                    if (currentItem > _maxX)
                    {
                        needToAddMaxX = (cnt - i + 1) % 2 == 0;
                        break;
                    }

                    crosses.Add(currentItem);
                }
            }

            if (needToAddMinX)
                crosses.Insert(0, _minX);

            if (needToAddMaxX)
                crosses.Add(_maxX);

            intersections = new float[crosses.Count];
            for (int i = 0; i < crosses.Count; i++)
                intersections[i] = (float)crosses[i];
        }

        private void computeHorizontalAlternateIntersections(float scanY, out float[] intersections)
        {
            List<double> crosses = new List<double>();
            if (containsPoint(_minX, scanY))
                crosses.Add(_minX);

            computeHorizontalProperCrossesAlternate(scanY, crosses);

            crosses.Sort((double x1, double x2) => x1 < x2 ? -1 : 1);
            for (int i = crosses.Count - 1; i > 0; i--)
                if (crosses[i] == crosses[i - 1])
                {
                    crosses.RemoveAt(i);
                    crosses.RemoveAt(i - 1);
                    i--;
                }

            if (crosses.Count % 2 == 1)
            {
                if (containsPoint(_maxX, scanY))
                    crosses.Add(_maxX);
                else
                {
                    intersections = new float[0];
                    return;
                }
            }

            intersections = new float[crosses.Count];
            for (int i = 0; i < crosses.Count; i++)
                intersections[i] = (float)crosses[i];
        }

        private void computeVerticalWindingIntersections(float scanX, out float[] intersections)
        {
            List<IntersectionItem> intersectionItemList = new List<IntersectionItem>();
            int i = 0;
            foreach (Contour c in _polygon.Contours)
            {
                if (_contourBounds[i].MaxX > scanX && _contourBounds[i].MinX < scanX)
                {
                    float[] cInts;
                    computeVerticalWindingIntersections(scanX, out cInts, c);

                    bool isBeginPoint = _contourOdd;
                    foreach (float f in cInts)
                    {
                        intersectionItemList.Add(new IntersectionItem() { Value = f, IsBeginPoint = isBeginPoint });
                        isBeginPoint = !isBeginPoint;
                    }
                }
                i++;
            }

            unionIntervals(intersectionItemList, out intersections);
        }

        private void computeVerticalWindingIntersections(float scanX, out float[] intersections, Contour c)
        {
            intersections = new float[0];
            List<double> crosses = new List<double>();

            computeVerticalProperCrossesWinding(scanX, c, crosses);

            if (crosses.Count % 2 == 1)
            {
                intersections = new float[0];
                return;
            }

            // индексы пересечений
            List<int> indices = new List<int>(crosses.Count);
            for (int i = 0; i < crosses.Count; i++)
                indices.Add(i);

            // располагаем индексы парасечений в порядке возрастания ординаты
            indices.Sort((int i1, int i2) => crosses[i1] < crosses[i2] ? -1 : 1);

            Stack<int> stack = new Stack<int>();
            List<double> tempList = new List<double>();

            for (int i = 0; i < indices.Count; i++)
            {
                if (stack.Count == 0)
                    stack.Push(indices[i]);
                else
                {
                    int top = stack.Peek();
                    if (top % 2 == indices[i] % 2)
                        stack.Push(indices[i]);
                    else
                    {
                        stack.Pop();
                        if (stack.Count == 0)
                        {
                            tempList.Add(crosses[top]);
                            tempList.Add(crosses[indices[i]]);
                        }
                    }
                }
            }

            crosses.Clear();
            bool needToAddMinY = false;
            bool needToAddMaxY = false;

            int cnt = tempList.Count;
            for (int i = 0; i < cnt; i++)
            {
                double currentItem = tempList[i];
                if (currentItem < _minY)
                    needToAddMinY = !needToAddMinY;
                else
                {
                    if (currentItem > _maxY)
                    {
                        needToAddMaxY = (cnt - i + 1) % 2 == 0;
                        break;
                    }

                    crosses.Add(currentItem);
                }
            }

            if (needToAddMinY)
                crosses.Insert(0, _minY);

            if (needToAddMaxY)
                crosses.Add(_maxY);

            intersections = new float[crosses.Count];
            for (int i = 0; i < crosses.Count; i++)
                intersections[i] = (float)crosses[i];
        }

        private void computeVerticalAlternateIntersections(float scanX, out float[] intersections)
        {
            List<double> crosses = new List<double>();
            if (containsPoint(scanX, _minY))
                crosses.Add(_minY);

            computeVerticalProperCrossesAlternate(scanX, crosses);

            crosses.Sort((double y1, double y2) => y1 < y2 ? -1 : 1);
            for (int i = crosses.Count - 1; i > 0; i--)
                if (crosses[i] == crosses[i - 1])
                {
                    crosses.RemoveAt(i);
                    crosses.RemoveAt(i - 1);
                    i--;
                }

            if (crosses.Count % 2 == 1)
            {
                if (containsPoint(scanX, _maxY))
                    crosses.Add(_maxY);
                else
                {
                    intersections = new float[0];
                    return;
                }
            }

            intersections = new float[crosses.Count];
            for (int i = 0; i < crosses.Count; i++)
                intersections[i] = (float)crosses[i];
        }

        private bool needToAddEndPointHorizontal(Contour c, int i)
        {
            int cnt1 = c.Vertices.Count - 1;
            double y = c.Vertices[i].Y;
            double y1 = i == 0 ? c.Vertices[cnt1].Y : c.Vertices[i - 1].Y;
            double y2 = i == cnt1 ? c.Vertices[0].Y : c.Vertices[i + 1].Y;

            return (y - y1) * (y - y2) <= 0;
        }

        private bool needToAddEndPointHorizontal(Contour c, int i, out bool isDownToUp)
        {
            int cnt1 = c.Vertices.Count - 1;
            double y = c.Vertices[i].Y;
            double y1 = i == 0 ? c.Vertices[cnt1].Y : c.Vertices[i - 1].Y;
            double y2 = i == cnt1 ? c.Vertices[0].Y : c.Vertices[i + 1].Y;
            isDownToUp = y1 > y2;

            return (y - y1) * (y - y2) < 0;
        }


        private bool needToAddEndPointVertical(Contour c, int i, out bool isLeftToRight)
        {
            int cnt1 = c.Vertices.Count - 1;
            double x = c.Vertices[i].X;
            double x1 = i == 0 ? c.Vertices[cnt1].X : c.Vertices[i - 1].X;
            double x2 = i == cnt1 ? c.Vertices[0].X : c.Vertices[i + 1].X;
            isLeftToRight = x1 < x2;

            return (x - x1) * (x - x2) < 0;
        }

        private bool needToAddEndPointVertical(Contour c, int i)
        {
            int cnt1 = c.Vertices.Count - 1;
            double x = c.Vertices[i].X;
            double x1 = i == 0 ? c.Vertices[cnt1].X : c.Vertices[i - 1].X;
            double x2 = i == cnt1 ? c.Vertices[0].X : c.Vertices[i + 1].X;

            return (x - x1) * (x - x2) <= 0;
        }


        /// <summary>
        /// Создает экземпляр Region.
        /// </summary>
        /// <param name="polygon">Полигон</param>
        /// <param name="fillMode">Режим заполнения внутренних областей</param>
        public Region(Polygon polygon, InteriorFillMode fillMode)
        {
            if (polygon == null)
                throw new ArgumentNullException("polygon");

            _contourBounds = new BoundingRectangle[polygon.Contours.Count];

            for(int i = 0; i < polygon.Contours.Count; i++)
                _contourBounds[i] = polygon.Contours[i].GetBoundingRectangle();

            _fillingMode = fillMode;
            _polygon = polygon;
            _br = _polygon.GetBoundingRectangle();
        }
    }
}