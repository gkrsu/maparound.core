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
** File: Buffer.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes for building buffers with Euclidean distance.
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    /// <summary>
    /// Calculates buffers.
    /// <remarks>
    /// Supports <see cref="MapAround.Geometry.PointD"/>, 
    /// <see cref="MapAround.Geometry.Polyline"/>, 
    /// <see cref="MapAround.Geometry.Polygon"/>, 
    /// <see cref="MapAround.Geometry.MultiPoint"/>. 
    /// Buffer distance for polygon may be negative.
    /// </remarks>
    /// </summary>
    public class BufferBuilder
    {
        private static double _twoPi = Math.PI * 2;

        private static Polygon getCoordinateBuffer(ICoordinate point, double distance, int pointsPerCircle)
        {
            if (distance < 0)
                return new Polygon();

            double angle = 0;
            double da = _twoPi / pointsPerCircle;
            ICoordinate[] vertices = new ICoordinate[pointsPerCircle];
            for (int i = 0; i < pointsPerCircle; i++)
            {
                vertices[i] = 
                    PlanimetryEnvironment.NewCoordinate(point.X + distance * Math.Cos(angle), point.Y + distance * Math.Sin(angle));
                angle += da;
            }
            
            return new Polygon(vertices);
        }

        private static Polygon getPointBuffer(PointD point, double distance, int pointsPerCircle)
        {
            return getCoordinateBuffer(point.Coordinate, distance, pointsPerCircle);
        }

        private static ICoordinate[] getArcPoints(ICoordinate point, double startAngle, double endAngle, double distance, int pointsPerCircle)
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

        private static Polygon getSegmentPreBuffer(Segment segment, double distance, int pointsPerCircle, bool bothSides)
        {
            if (distance < 0)
                return new Polygon();

            ICoordinate[] points1 = null;
            ICoordinate[] points2 = null;

            if (segment.V1.X == segment.V2.X)
            {
                bool downToUp = segment.V1.Y < segment.V2.Y;

                points1 = getArcPoints(downToUp ? segment.V1 : segment.V2, 
                                       Math.PI, 
                                       2 * Math.PI, 
                                       distance, 
                                       downToUp && !bothSides ? 2 : pointsPerCircle);
                points2 = getArcPoints(downToUp ? segment.V2 : segment.V1, 
                                       0, 
                                       Math.PI, 
                                       distance,
                                       !downToUp && !bothSides ? 2 : pointsPerCircle);

                Polygon result = new Polygon(points1);
                foreach (ICoordinate p in points2)
                    result.Contours[0].Vertices.Add(p);

                return result;
            }

            if (segment.V1.Y == segment.V2.Y)
            {
                bool leftToRight = segment.V1.X < segment.V2.X;

                points1 = getArcPoints(leftToRight ? segment.V1 : segment.V2, 
                                       0.5 * Math.PI, 
                                       1.5 * Math.PI, 
                                       distance, 
                                       leftToRight && !bothSides ? 2 : pointsPerCircle);
                points2 = getArcPoints(leftToRight ? segment.V2 : segment.V1, 
                                       1.5 * Math.PI, 
                                       2.5 * Math.PI, 
                                       distance,
                                       !leftToRight && !bothSides ? 2 : pointsPerCircle);

                Polygon result = new Polygon(points1);
                foreach (ICoordinate p in points2)
                    result.Contours[0].Vertices.Add(p);

                return result;
            }

            double angle = Math.Atan(Math.Abs(segment.V2.Y - segment.V1.Y) / 
                                     Math.Abs(segment.V2.X - segment.V1.X));

            bool wasSwapped = false;
            if (segment.V1.X > segment.V2.X)
            {
                ICoordinate p = segment.V1;
                segment.V1 = segment.V2;
                segment.V2 = p;
                wasSwapped = true;
            }

            if (segment.V1.Y < segment.V2.Y)
            {
                points1 = getArcPoints(segment.V1, 
                                       angle + 0.5 * Math.PI, 
                                       angle + 1.5 * Math.PI, 
                                       distance,
                                       !bothSides && !wasSwapped ? 2 : pointsPerCircle);
                points2 = getArcPoints(segment.V2, 
                                       angle - 0.5 * Math.PI, 
                                       angle + 0.5 * Math.PI, 
                                       distance, 
                                       !bothSides && wasSwapped ? 2 : pointsPerCircle);

                Polygon result = new Polygon(points1);
                foreach (ICoordinate p in points2)
                    result.Contours[0].Vertices.Add(p);

                return result;
            }
            else
            {
                points1 = getArcPoints(segment.V1, 
                                       0.5 * Math.PI - angle, 
                                       1.5 * Math.PI - angle, 
                                       distance,
                                       !bothSides && !wasSwapped ? 2 : pointsPerCircle);
                points2 = getArcPoints(segment.V2, 
                                       1.5 * Math.PI - angle, 
                                       2.5 * Math.PI - angle, 
                                       distance,
                                       !bothSides && wasSwapped ? 2 : pointsPerCircle);

                Polygon result = new Polygon(points1);
                foreach (ICoordinate p in points2)
                    result.Contours[0].Vertices.Add(p);

                return result;
            }
        }

        private class ThreadStartData
        {
            public List<Polygon> Buffers;
            public Polygon Result;
        }

        private static Polygon mergePartialBuffers(List<Polygon> buffers)
        {
            Polygon temp = new Polygon();
            ICollection<IGeometry> gc;
            while (buffers.Count > 1)
            {
                List<Polygon> tempBuffers = new List<Polygon>();
                for (int i = 0; i < buffers.Count; i += 2)
                {
                    if (i + 1 == buffers.Count)
                        tempBuffers.Add(buffers[i]);
                    else
                    {
                        gc = buffers[i].Union(buffers[i + 1]);
                        if (gc.Count > 0)
                            temp = (Polygon)((GeometryCollection)gc)[0];
                        tempBuffers.Add(temp);
                    }
                }
                buffers = tempBuffers;
            }

            if (buffers.Count == 0)
                return null;

            return buffers[0];

        }

        private static void mergePartialBuffers(object buffers)
        { 
            if (buffers is ThreadStartData)
                (buffers as ThreadStartData).Result = mergePartialBuffers((buffers as ThreadStartData).Buffers);
        }

        private static Polygon mergePartialBuffers(List<Polygon> buffers, bool allowParallels)
        {
            if (!allowParallels || buffers.Count < 20)
                return mergePartialBuffers(buffers);
            else
            {
                Thread t = new Thread(mergePartialBuffers);
                List<Polygon> buffersForAnotherThread = new List<Polygon>();
                List<Polygon> buffersForThisThread = new List<Polygon>();
                for (int i = 0; i < buffers.Count; i++)
                {
                    if(i > buffers.Count / 2)
                        buffersForAnotherThread.Add(buffers[i]);
                    else
                        buffersForThisThread.Add(buffers[i]);
                }

                ThreadStartData tsd = new ThreadStartData();
                tsd.Buffers = buffersForAnotherThread;
                t.Start(tsd);
                Polygon p = mergePartialBuffers(buffersForThisThread);
                t.Join();
                ICollection<IGeometry> gc = p.Union(tsd.Result);

                if (gc.Count > 0)
                    return (Polygon)((GeometryCollection)gc)[0];
                else
                    return null;
            }
        }

        private static Polygon getBoundsBuffer(Polygon polygon, double distance, int pointsPerCircle, bool allowParallels)
        {
            Polygon temp = new Polygon();

            List<Polygon> partialBuffers = new List<Polygon>();

            ICollection<IGeometry> gc;
            int c = 0;
            foreach (Contour contour in polygon.Contours)
            {
                for (int i = 0; i < contour.Vertices.Count; i++)
                {
                    int j = i == contour.Vertices.Count - 1 ? 0 : i + 1;
                    Segment s = new Segment(contour.Vertices[i], contour.Vertices[j]);
                    gc = temp.Union(getSegmentPreBuffer(s, Math.Abs(distance), pointsPerCircle, false));
                    if (gc.Count > 0)
                        temp = (Polygon)((GeometryCollection)gc)[0];

                    c++;
                    if (c == 3)
                    {
                        partialBuffers.Add(temp);
                        temp = new Polygon();
                        c = 0;
                    }
                }
            }

            if (temp.CoordinateCount > 0)
                partialBuffers.Add(temp);

            return mergePartialBuffers(partialBuffers, allowParallels);
        }

        private static Polygon getPolygonBuffer(Polygon polygon, double distance, int pointsPerCircle, bool allowParallels)
        {
            polygon = (Polygon)polygon.Clone();
            polygon.Weed(distance - distance * Math.Cos(Math.PI / pointsPerCircle));
            polygon.Simplify();

            Polygon boundaryBuffer = getBoundsBuffer(polygon, distance, pointsPerCircle, allowParallels);

            ICollection<IGeometry> result;
            if (distance > 0)
                result = polygon.Union(boundaryBuffer);
            else
                result = polygon.Difference(boundaryBuffer);

            foreach (IGeometry g in result)
                if (g is Polygon)
                    return (Polygon)g;

            return new Polygon();
        }

        private static Polygon getPolylineBuffer(Polyline polyline, double distance, int pointsPerCircle, bool allowParallels)
        {
            polyline = (Polyline)polyline.Clone();
            polyline.Weed(distance - distance * Math.Cos(Math.PI / pointsPerCircle));

            Polygon temp = new Polygon();
            List<Polygon> partialBuffers = new List<Polygon>();

            ICollection<IGeometry> gc;
            int c = 0;
            foreach (LinePath path in polyline.Paths)
            {
                for (int i = 0; i < path.Vertices.Count - 1; i++)
                {
                    Segment s = new Segment(path.Vertices[i], path.Vertices[i + 1]);
                    gc = temp.Union(getSegmentPreBuffer(s, Math.Abs(distance), pointsPerCircle, i == 0));
                    if (gc.Count > 0)
                        temp = (Polygon)((GeometryCollection)gc)[0];

                    c++;
                    if (c == 3)
                    {
                        partialBuffers.Add(temp);
                        temp = new Polygon();
                        c = 0;
                    }
                }
            }

            if (temp.CoordinateCount > 0)
                partialBuffers.Add(temp);

            return mergePartialBuffers(partialBuffers, allowParallels);
        }

        private static IGeometry getMultiPointBuffer(MultiPoint multiPoint, double distance, int pointsPerCircle, bool allowParallels)
        {
            Polygon temp = new Polygon();
            List<Polygon> partialBuffers = new List<Polygon>();

            ICollection<IGeometry> gc;
            int c = 0;
            foreach (ICoordinate p in multiPoint.Points)
            {
                gc = temp.Union(getCoordinateBuffer(p, distance, pointsPerCircle));
                if (gc.Count > 0)
                    temp = (Polygon)((GeometryCollection)gc)[0];

                c++;
                if (c == 3)
                {
                    partialBuffers.Add(temp);
                    temp = new Polygon();
                    c = 0;
                }
            }

            if (temp.CoordinateCount > 0)
                partialBuffers.Add(temp);

            return mergePartialBuffers(partialBuffers, allowParallels);
        }

        /// <summary>
        /// Builds a buffer for the specified geometry.
        /// </summary>
        /// <param name="geometry">The geometry to build a buffer</param>
        /// <param name="distance">Buffer distance</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>>A geometry that represents the resulting buffer</returns>
        public static IGeometry GetBuffer(IGeometry geometry, double distance, int pointsPerCircle, bool allowParallels)
        {
#if DEMO
            throw new NotSupportedException("Buffer builder is not supported in demo version");
#else
            if (!(geometry is PointD) && !(geometry is Polyline) && !(geometry is Polygon) && !(geometry is MultiPoint))
                throw new NotSupportedException("Buffer calculation for \"" + geometry.GetType().FullName + "\" is not supported.");

            if (distance == 0)
                return (IGeometry)geometry.Clone();

            if (pointsPerCircle <= 2)
                throw new ArgumentOutOfRangeException("pointsPerCircle");

            if (geometry is Polygon)
                return getPolygonBuffer((Polygon)geometry, distance, pointsPerCircle, allowParallels);

            if (distance < 0)
                throw new ArgumentException("Buffer value should not be negative for this geometry", "distance");

            if (geometry is PointD)
                return getPointBuffer((PointD)geometry, distance, pointsPerCircle);

            if (geometry is Polyline)
                return getPolylineBuffer((Polyline)geometry, distance, pointsPerCircle, allowParallels);

            if (geometry is MultiPoint)
                return getMultiPointBuffer((MultiPoint)geometry, distance, pointsPerCircle, allowParallels);

            throw new InvalidOperationException("Internal error");
#endif
        }
    }
}