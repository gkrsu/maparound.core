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
** File: RasterAlgorithms.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Raster processing algorithms
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;

    using MapAround.Geometry;

    /// <summary>
    /// Implements raster processing algorithms.
    /// </summary>
    public static class RasterAlgorithms
    {
        private static Matrix getAffineTransformMatrix(PointF p01, PointF p02, PointF p03,
                                                        PointF p11, PointF p12, PointF p13)
        {
            Matrix a = new Matrix(p02.X - p01.X,
                                  p02.Y - p01.Y,
                                  p03.X - p01.X,
                                  p03.Y - p01.Y,
                                  p01.X,
                                  p01.Y);

            Matrix b = new Matrix(p12.X - p11.X,
                                  p12.Y - p11.Y,
                                  p13.X - p11.X,
                                  p13.Y - p11.Y,
                                  p11.X,
                                  p11.Y);

            if (!a.IsInvertible)
                return null;

            a.Invert();
            a.Multiply(b, MatrixOrder.Append);

            return a;
        }

        private static int[] calculateOptimalAffineTransformPoints(Point[] sourceNodes, ICoordinate[] destNodes)
        {
            int[] result = new int[3];
            double minNorm = double.MaxValue;

            for (int i1 = 0; i1 < sourceNodes.Length - 2; i1++)
                for (int i2 = i1 + 1; i2 < sourceNodes.Length - 1; i2++)
                    for (int i3 = i2 + 1; i3 < sourceNodes.Length; i3++)
                    {
                        PointF p01 = new PointF(sourceNodes[i1].X, sourceNodes[i1].Y);
                        PointF p02 = new PointF(sourceNodes[i2].X, sourceNodes[i2].Y);
                        PointF p03 = new PointF(sourceNodes[i3].X, sourceNodes[i3].Y);

                        PointF p11 = new PointF((float)destNodes[i1].X, (float)destNodes[i1].Y);
                        PointF p12 = new PointF((float)destNodes[i2].X, (float)destNodes[i2].Y);
                        PointF p13 = new PointF((float)destNodes[i3].X, (float)destNodes[i3].Y);

                        Matrix m = getAffineTransformMatrix(p01, p02, p03, p11, p12, p13);
                        if (m != null)
                        {
                            PointF[] tempPoints = new PointF[sourceNodes.Length];
                            for (int i = 0; i < sourceNodes.Length; i++)
                                tempPoints[i] = new PointF(sourceNodes[i].X, sourceNodes[i].Y);
                            m.TransformPoints(tempPoints);

                            double currentNorm = 0;
                            for (int i = 0; i < tempPoints.Length; i++)
                                currentNorm += PlanimetryAlgorithms.Distance(destNodes[i], PlanimetryEnvironment.NewCoordinate(tempPoints[i].X, tempPoints[i].Y));

                            if (currentNorm < minNorm)
                            {
                                minNorm = currentNorm;
                                result[0] = i1;
                                result[1] = i2;
                                result[2] = i3;
                            }
                        }
                    }

            return result;
        }

        private static void calculateRubberSheetTransform(int width, int height, PointF[,] source, ICoordinate[,] result, ICoordinate[] sourceNodes, ICoordinate[] destNodesShifts, RasterBindingProgress progress)
        {
            double[] w = new double[sourceNodes.Length];
            double[] distances = new double[sourceNodes.Length];

            int startProgressPercent = 30;
            int endProgressPercent = 70;
            double completed = 0;
            double previousPercent = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    // проверяем попали ли мы в узел интерполяции
                    bool isNode = false;
                    for (int k = 0; k < sourceNodes.Length; k++)
                    {
                        if (sourceNodes[k].X == source[i, j].X && 
                            sourceNodes[k].Y == source[i, j].Y)
                        {
                            // попали
                            result[i, j] =
                                PlanimetryEnvironment.NewCoordinate(destNodesShifts[k].X + source[i, j].X,
                                    destNodesShifts[k].Y + source[i, j].Y);
                            isNode = true;
                        }
                    }

                    if (isNode) continue;

                    // расчет расстояний до узлов
                    double maxDistance = 0;
                    for (int t = 0; t < sourceNodes.Length; t++)
                    {
                        ICoordinate p = PlanimetryEnvironment.NewCoordinate(sourceNodes[t].X, sourceNodes[t].Y);
                        distances[t] = PlanimetryAlgorithms.Distance(p,
                                PlanimetryEnvironment.NewCoordinate(source[i, j].X, 
                                           source[i, j].Y));

                        if (maxDistance < distances[t])
                            maxDistance = distances[t];
                    }

                    // расчет знаменателей весов узловых точек
                    double sum = 0;
                    for (int k = 0; k < sourceNodes.Length; k++)
                    {
                        double temp = (maxDistance - distances[k]) / (maxDistance * distances[k]);
                        sum += Math.Pow(temp, 2);
                    }

                    // расчет весов узловых точек
                    for (int k = 0; k < sourceNodes.Length; k++)
                    {
                        double temp = (maxDistance - distances[k]) / (maxDistance * distances[k]);
                        w[k] = Math.Pow(temp, 2) / sum;
                    }

                    // расчет значений новых координат
                    result[i, j] = PlanimetryEnvironment.NewCoordinate(source[i, j].X, source[i, j].Y);
                    for (int k = 0; k < sourceNodes.Length; k++)
                    {
                        result[i, j].X += w[k] * destNodesShifts[k].X;
                        result[i, j].Y += w[k] * destNodesShifts[k].Y;
                    }
                }

                notifyProgessListenerIfNeeded(progress, startProgressPercent, endProgressPercent, ref completed, ref previousPercent, width);
            }
        }

        private static int getDestRasterX(int width, ICoordinate point, BoundingRectangle rectangle)
        {
            int x = (int)((point.X - rectangle.MinX) / (rectangle.Width / (double)width));

            if (x < width)
                return x;
            else
                return width - 1;
        }

        private static int getDestRasterY(int height, ICoordinate point, BoundingRectangle rectangle)
        {
            int y =(int)((point.Y - rectangle.MinY) / (rectangle.Height / (double)height));

            if (y < height)
                return y;
            else
                return height - 1;
        }

        private static void fillPixel(int x, int y, Bitmap bmp, byte[,] fillCount, int width, int height)
        {
            int r = 0, g = 0, b = 0;
            int neighborCount = 0;

            if (x > 0 && x < width - 1)
                if (fillCount[x + 1, y] > 0)
                {
                    Color c = bmp.GetPixel(x + 1, y);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    neighborCount++;
                }

            if (x < width && x > 0)
                if (fillCount[x - 1, y] > 0)
                {
                    Color c = bmp.GetPixel(x - 1, y);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    neighborCount++;
                }

            if (y > 0 && y < height - 1)
                if (fillCount[x, y + 1] > 0)
                {
                    Color c = bmp.GetPixel(x, y + 1);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    neighborCount++;
                }

            if (y < height && y > 0)
                if (fillCount[x, y - 1] > 0)
                {
                    Color c = bmp.GetPixel(x, y - 1);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    neighborCount++;
                }

            if (neighborCount == 0)
            {
                bmp.SetPixel(x, y, Color.Transparent);
                return;
            }

            r /= neighborCount;
            g /= neighborCount;
            b /= neighborCount;

            bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
        }

        private static void calculateAffinneTransform(int width, int height, PointF[,] affinneTransformResult, Point[] sourceNodes, ICoordinate[] destNodes, RasterBindingProgress progress)
        {
            int[] r =
                RasterAlgorithms.calculateOptimalAffineTransformPoints(sourceNodes, destNodes);

            PointF p01 = new PointF(sourceNodes[r[0]].X, sourceNodes[r[0]].Y);
            PointF p02 = new PointF(sourceNodes[r[1]].X, sourceNodes[r[1]].Y);
            PointF p03 = new PointF(sourceNodes[r[2]].X, sourceNodes[r[2]].Y);

            PointF p11 = new PointF((float)destNodes[r[0]].X, (float)destNodes[r[0]].Y);
            PointF p12 = new PointF((float)destNodes[r[1]].X, (float)destNodes[r[1]].Y);
            PointF p13 = new PointF((float)destNodes[r[2]].X, (float)destNodes[r[2]].Y);

            int startProgressPercent = 0;
            int endProgressPercent = 30;
            double completed = 0;
            double previousPercent = 0;

            Matrix m = getAffineTransformMatrix(p01, p02, p03, p11, p12, p13);
            for (int i = 0; i < width; i++)
            {
                PointF[] pts = new PointF[height];
                for (int j = 0; j < height; j++)
                    pts[j] = new PointF(i, j);

                m.TransformPoints(pts);
                for (int j = 0; j < height; j++)
                    affinneTransformResult[i, j] = pts[j];

                if (progress != null)
                {
                    completed += ((double)endProgressPercent - (double)startProgressPercent) / width;
                    if (Math.Truncate(completed) > previousPercent)
                    {
                        progress((int)Math.Truncate(completed + startProgressPercent));
                        previousPercent = Math.Truncate(completed);
                    }
                }
            }
        }

        /// <summary>
        /// Defines a method which is called to notify a subscriber
        /// about completion state.
        /// </summary>
        /// <returns>Completion percent</returns>
        public delegate void RasterBindingProgress(int percent);

        private static void notifyProgessListenerIfNeeded(RasterBindingProgress progress, 
                        int startProgressPercent,
                        int endProgressPercent,
                        ref double completed,
                        ref double previousPercent,
                        int sourceWidth)
        {
            if (progress == null) return;

            completed += ((double)endProgressPercent - (double)startProgressPercent) / sourceWidth;
            if (Math.Truncate(completed) > previousPercent)
            {
                progress((int)Math.Truncate(completed + startProgressPercent));
                previousPercent = Math.Truncate(completed);
            }
        }

        private static Bitmap calcDestRaster(Bitmap source, BoundingRectangle rectangle, ICoordinate[,] warpTransformResult, RasterBindingProgress progress)
        {
            byte[,] fillCount = new byte[source.Width, source.Height];

            Int16[,] r = new Int16[source.Width, source.Height];
            Int16[,] g = new Int16[source.Width, source.Height];
            Int16[,] b = new Int16[source.Width, source.Height];

            for (int i = 0; i < source.Width; i++)
                for (int j = 0; j < source.Height; j++)
                {
                    fillCount[i, j] = 0;
                    r[i, j] = 0;
                    g[i, j] = 0;
                    b[i, j] = 0;
                }

            Bitmap result = new Bitmap(source.Width, source.Height);

            int startProgressPercent = 70;
            int endProgressPercent = 80;
            double completed = 0;
            double previousPercent = 0;

            for (int i = 0; i < source.Width; i++)
            {
                for (int j = 0; j < source.Height; j++)
                {
                    int x = getDestRasterX(source.Width, warpTransformResult[i, j], rectangle);
                    int y = getDestRasterY(source.Height, warpTransformResult[i, j], rectangle);

                    if (!double.IsNaN(warpTransformResult[i, j].X) && !double.IsNaN(warpTransformResult[i, j].Y) &&
                        x >= 0 && y >= 0)
                    {
                        fillCount[x, y]++;
                        Color c = source.GetPixel(i, j);
                        r[x, y] += c.R;
                        g[x, y] += c.G;
                        b[x, y] += c.B;
                    }
                }

                notifyProgessListenerIfNeeded(progress, startProgressPercent, endProgressPercent, ref completed, ref previousPercent, source.Width);
            }

            startProgressPercent = 80;
            endProgressPercent = 90;
            completed = 0;

            for (int i = 0; i < source.Width; i++)
            {
                for (int j = 0; j < source.Height; j++)
                {
                    int fc = fillCount[i, j];
                    if (fc > 0)
                    {
                        result.SetPixel(i, j,
                            Color.FromArgb(r[i, j] / fc,
                                           g[i, j] / fc,
                                           b[i, j] / fc));
                    }
                    else
                        result.SetPixel(i, j, Color.Transparent);
                }

                notifyProgessListenerIfNeeded(progress, startProgressPercent, endProgressPercent, ref completed, ref previousPercent, source.Width);
            }

            startProgressPercent = 90;
            endProgressPercent = 100;
            completed = 0;

            for (int i = 0; i < source.Width; i++)
            {
                for (int j = 0; j < source.Height; j++)
                    if (fillCount[i, j] == 0)
                        fillPixel(i, j, result, fillCount, result.Width, result.Height);

                notifyProgessListenerIfNeeded(progress, startProgressPercent, endProgressPercent, ref completed, ref previousPercent, source.Width);
            }

            return result;
        }

        private static bool checkControlPoints(int sourceWidth, int sourceHeight, Point[] sourceControlPoints)
        {
            foreach(Point p in sourceControlPoints)
                if (p.X < 0 || p.Y < 0 || p.X >= sourceWidth || p.Y >= sourceHeight)
                    return false;

            return true;
        }

        /// <summary>
        /// Performs a rubbersheeting transformation of raster.
        /// </summary>
        /// <param name="source">A System.Drawing.Bitmap instance containing the source image</param>
        /// <param name="sourceControlPoints">Control points of source</param>
        /// <param name="destinationControlPoints">Control points on the map</param>
        /// <param name="rectangle">A bounding rectangle defining a bouns of transformed raster</param>
        /// <param name="progress">Defines a method which is called to notify a subscriber about completion state.</param>
        /// <returns>A System.Drawing.Bitmap instance containing the transformed image</returns>
        public static Bitmap BindRaster(Bitmap source, Point[] sourceControlPoints, ICoordinate[] destinationControlPoints, out BoundingRectangle rectangle, RasterBindingProgress progress)
        {
#if DEMO
            throw new NotImplementedException("This method is not implemented in demo version.");
#else
            if (source == null)
                throw new ArgumentNullException("source");

            if (sourceControlPoints.Length != destinationControlPoints.Length)
                throw new ArgumentException("Number of control points of raster and map should be the same.");

            if (sourceControlPoints.Length < 3)
                throw new ArgumentException("Number of control points should not be less than 3");

            if (!checkControlPoints(source.Width, source.Height, sourceControlPoints))
                throw new ArgumentException("At least one source control point is outside raster", "sourceControlPoints");

            ICoordinate[,] warpTransformResult = new ICoordinate[source.Width, source.Height];
            PointF[,] affinneTransformResult = new PointF[source.Width, source.Height];

            // вычисляем результат аффинного преобразования примененного к координатам точек исходного растра
            calculateAffinneTransform(source.Width, source.Height, affinneTransformResult, sourceControlPoints, destinationControlPoints, progress);
            
            ICoordinate[] shifts = new ICoordinate[destinationControlPoints.Length];
            for (int i = 0; i < shifts.Length; i++)
            {
                PointF p = affinneTransformResult[sourceControlPoints[i].X, sourceControlPoints[i].Y];
                shifts[i] = PlanimetryEnvironment.NewCoordinate(destinationControlPoints[i].X - p.X, destinationControlPoints[i].Y - p.Y);
            }

            // вычисляем новые координаты точек исходного растра, полученные в результате "коробления"
            calculateRubberSheetTransform(source.Width, source.Height, affinneTransformResult, warpTransformResult, destinationControlPoints, shifts, progress);

            // вычисляем ограничивающий прямоугольник преобразованного растра
            rectangle = new BoundingRectangle();
            for (int i = 0; i < source.Width; i++)
                for (int j = 0; j < source.Height; j++)
                {
                    if (!double.IsNaN(warpTransformResult[i, j].X) && !double.IsNaN(warpTransformResult[i, j].Y))
                        rectangle.Join(warpTransformResult[i, j]);
                }

            return calcDestRaster(source, rectangle, warpTransformResult, progress);
#endif
        }
    }
}