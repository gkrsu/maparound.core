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
** File: GeoBuffer.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Class that implements buffer building on the Earth's surface
**
=============================================================================*/

#if !DEMO

namespace MapAround.Geography
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using MapAround.Geometry;
    using MapAround.Geography;

    /// <summary>
    /// Calculates buffers on the Earth's surface.
    /// </summary>
    public class GeoBufferBuilder
    {
        private static GeoPolygon getPointBuffer(GeoPoint point, double angleDistance, int pointsPerCircle)
        {
            if (angleDistance < 0)
                return new GeoPolygon();

            GnomonicProjection projection = new GnomonicProjection(point.L, point.Phi);
            PointD planePoint = new PointD(0, 0);
            Polygon planePolygon = (Polygon)planePoint.Buffer(Math.Tan(angleDistance), pointsPerCircle, false);

            GeometryCollection geometryColllection = new GeometryCollection();
            geometryColllection.Add(planePolygon);
            GeographyCollection gc = GeometrySpreader.GetGeographies(geometryColllection, projection);

            if(gc[0] is GeoPolygon)
                return (GeoPolygon)gc[0];

            return new GeoPolygon();
        }

        private static ICoordinate[] getArcPoints(ICoordinate point, double startAngle, double endAngle, double distance, int pointsPerCircle)
        {
            return null;
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
                    if (i > buffers.Count / 2)
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

        private static Polygon getBoundsBuffer(GeoPolygon polygon, GnomonicProjection projection, double distance, int pointsPerCircle, bool allowParallels)
        {
            Polygon temp = new Polygon();

            List<Polygon> partialBuffers = new List<Polygon>();
            GeographyCollection geographyCollection = new GeographyCollection();
            ICollection<IGeometry> unionResult = null;

            int c = 0;
            foreach (GeoContour contour in polygon.Contours)
            {
                for (int i = 0; i < contour.Vertices.Count; i++)
                {
                    GeoPoint p = contour.Vertices[i];

                    GeoPolygon tempPolygon = getPointBuffer(p, Math.Abs(distance), pointsPerCircle);
                    geographyCollection.Clear();
                    geographyCollection.Add(tempPolygon);

                    GeometryCollection gc = GeometrySpreader.GetGeometries(geographyCollection, projection);
                    if(gc[0] is Polygon)
                        unionResult = temp.Union((Polygon)gc[0]);
                    if (unionResult.Count > 0)
                        temp = (Polygon)((GeometryCollection)unionResult)[0];

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

        private static void projectGeography(IGeography geography, out GnomonicProjection projection, out IGeometry geometry)
        {
            GeographyCollection geographyCollection = new GeographyCollection();
            geographyCollection.Add(geography);

            double centerLatitude, centerLongitude;
            GnomonicProjection.GetCenter(geography.ExtractPoints(), out centerLatitude, out centerLongitude);
            projection = new GnomonicProjection(centerLongitude, centerLatitude);

            GeometryCollection geometryCollection = GeometrySpreader.GetGeometries(geographyCollection, projection);
            if (geometryCollection.Count > 0)
                geometry = geometryCollection[0];
            else
                geometry = null;
        }

        private static GeoPolygon getPolygonBuffer(GeoPolygon geoPolygon, double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            geoPolygon = (GeoPolygon)geoPolygon.Clone();
            double minAngle = Math.Sin(Math.Abs(angleDistance)) * Math.Sin(Math.PI / pointsPerCircle);
            geoPolygon.ReduceSegments(minAngle);
            geoPolygon.Densify(minAngle);

            GnomonicProjection projection;
            IGeometry geometry;
            projectGeography(geoPolygon, out projection, out geometry);
            Polygon planePolygon = (Polygon)geometry;

            Polygon boundaryBuffer = getBoundsBuffer(geoPolygon, projection, angleDistance, pointsPerCircle, allowParallels);

            ICollection<IGeometry> result;
            if (angleDistance > 0)
                result = planePolygon.Union(boundaryBuffer);
            else
                result = planePolygon.Difference(boundaryBuffer);

            GeographyCollection geographyCollection = GeometrySpreader.GetGeographies(result, projection);

            foreach (IGeography g in geographyCollection)
                if (g is GeoPolygon)
                    return (GeoPolygon)g;

            return new GeoPolygon();
        }

        private static GeoPolygon getPolylineBuffer(GeoPolyline geoPolyline, double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            geoPolyline = (GeoPolyline)geoPolyline.Clone();
            double minAngle = Math.Sin(Math.Abs(angleDistance)) * Math.Sin(Math.PI / pointsPerCircle);
            geoPolyline.ReduceSegments(minAngle);
            geoPolyline.Densify(minAngle);

            GnomonicProjection projection;
            IGeometry geometry;
            projectGeography(geoPolyline, out projection, out geometry);
            Polyline planePolyline = (Polyline)geometry;
            GeographyCollection geographyCollection = new GeographyCollection();

            Polygon temp = new Polygon();
            List<Polygon> partialBuffers = new List<Polygon>();

            ICollection<IGeometry> unionResult = null;

            int c = 0;
            foreach (GeoPath path in geoPolyline.Paths)
            {
                for (int i = 0; i < path.Vertices.Count - 1; i++)
                {
                    GeoPoint p = path.Vertices[i];

                    GeoPolygon tempPolygon = getPointBuffer(p, angleDistance, pointsPerCircle);
                    geographyCollection.Clear();
                    geographyCollection.Add(tempPolygon);

                    GeometryCollection gc = GeometrySpreader.GetGeometries(geographyCollection, projection);
                    if (gc[0] is Polygon)
                        unionResult = temp.Union((Polygon)gc[0]);
                    if (unionResult.Count > 0)
                        temp = (Polygon)((GeometryCollection)unionResult)[0];

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

            Polygon planeBuffer = mergePartialBuffers(partialBuffers, allowParallels);
            GeometryCollection geometryCollection = new GeometryCollection();
            geometryCollection.Add(planeBuffer);
            geographyCollection = GeometrySpreader.GetGeographies(geometryCollection, projection);

            foreach (IGeography g in geographyCollection)
                if (g is GeoPolygon)
                    return (GeoPolygon)g;

            return new GeoPolygon();
        }

        /// <summary>
        /// Builds a buffer for the specified geography.
        /// </summary>
        /// <param name="geography">A geography to build a buffer</param>
        /// <param name="angleDistance">An angle distance of buffer (in radians)</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geography that represents the resulting buffer</returns>
        public static IGeography GetBuffer(IGeography geography, double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            if (!(geography is GeoPoint) && !(geography is GeoPolyline) && !(geography is GeoPolygon))
                throw new NotSupportedException("Buffer calculation for \"" + geography.GetType().FullName + "\" is not supported.");

            if (angleDistance == 0)
                return (IGeography)geography.Clone();

            if (pointsPerCircle <= 2)
                throw new ArgumentOutOfRangeException("pointsPerCircle");

            if (geography is GeoPolygon)
                return getPolygonBuffer((GeoPolygon)geography, angleDistance, pointsPerCircle, allowParallels);

            if (angleDistance < 0)
                throw new ArgumentException("Buffer value should not be negative for this geography", "distance");

            if (geography is GeoPoint)
                return getPointBuffer((GeoPoint)geography, angleDistance, pointsPerCircle);

            if (geography is GeoPolyline)
                return getPolylineBuffer((GeoPolyline)geography, angleDistance, pointsPerCircle, allowParallels);

            throw new InvalidOperationException("Internal error");
        }
    }
}

#endif