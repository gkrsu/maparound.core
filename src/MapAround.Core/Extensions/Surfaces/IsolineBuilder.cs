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
** File: IsolineBuilder.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Building isolines and polygons of z-value ranges.
**
=============================================================================*/

#if!DEMO
namespace MapAround.Extensions.Surfaces
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using MapAround.Geometry;
    using MapAround.Geometry.Tessellations;
    using MapAround.Indexing;
    using MapAround.Mapping;


    /// <summary>
    /// Contains classes implementing 2.5D-surface algorithms.
    /// </summary>
    internal class NamespaceDoc
    { 
    }

    /// <summary>
    /// Builds isolines and polygons of z-value ranges.
    /// </summary>
    public class IsolineBuilder
    {
        /// <summary>
        /// Describes a range of z-value.
        /// </summary>
        public struct LevelRange
        {
            /// <summary>
            /// Minimum value.
            /// </summary>
            public double Min;

            /// <summary>
            /// Maximum value.
            /// </summary>
            public double Max;

            /// <summary>
            /// Determines if this interval contains the z-value.
            /// </summary>
            /// <param name="z">Z-value</param>
            /// <returns>true if this interval contains the z-value, false otherwise</returns>
            public bool Contains(double z)
            {
                return z >= Min && z <= Max;
            }

            /// <summary>
            /// Initializes a new instance of the MapAround.Geometry.Extensions.Surfaces.IsolineBuilder.LevelRange
            /// </summary>
            /// <param name="min">Minimum value</param>
            /// <param name="max">Maximum value</param>
            public LevelRange(double min, double max)
            {
                Min = min;
                Max = max;
            }
        }

        /// <summary>
        /// Describes a polygon of z-value range.
        /// </summary>
        public struct LevelRangePolygon
        {
            /// <summary>
            /// Polygon.
            /// </summary>
            public Polygon Polygon;

            /// <summary>
            /// A range of z-value
            /// </summary>
            public LevelRange Range;

            /// <summary>
            /// Initializes a new instance of the MapAround.Geometry.Extensions.Surfaces.IsolineBuilder.LevelRangePolygon
            /// </summary>
            /// <param name="range">A range</param>
            /// <param name="polygon">A polygon</param>
            public LevelRangePolygon(LevelRange range, Polygon polygon)
            {
                Range = range;
                Polygon = polygon;
            }
        }

        private bool liesBetween(double z, double z1, double z2)
        {
            return (z <= z1 && z >= z2) || (z <= z2 && z >= z1);
        }

        private Coordinate3D getPlaneSegmentIntersection(Coordinate3D v1, Coordinate3D v2, double z)
        {
            double f = (z - v1.Z) / (v2.Z - v1.Z);

            return new Coordinate3D(
                f * (v2.X - v1.X) + v1.X, 
                f * (v2.Y - v1.Y) + v1.Y, 
                z);
        }

        private Polyline getIsoline(List<Triangle> triangles, double z)
        {
            Polyline result = new Polyline();

            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];

                Coordinate3D v1 = ((Coordinate3D)t.Cell1.DataPoint);
                Coordinate3D v2 = ((Coordinate3D)t.Cell2.DataPoint);
                Coordinate3D v3 = ((Coordinate3D)t.Cell3.DataPoint);

                List<double> tz = new List<double>();
                tz.Add(v1.Z);
                tz.Add(v2.Z);
                tz.Add(v3.Z);

                tz.Sort();

                // triangle is below the plane
                if (z > tz[2]) continue;

                // triangle lies in the plane
                if (z == tz[0] && z == tz[1] && z == tz[2])
                    continue;

                // Triangle is above the plane. 
                // This means that all the remaining triangles are also above the plane.
                // Stop processing.
                if (z <= tz[0]) break;

                List<ICoordinate> coords = new List<ICoordinate>();
                // one edge of triangle lies in the plane
                if ((z == tz[0] && z == tz[1]) ||
                    (z == tz[1] && z == tz[2]))
                {
                    if (z == v1.Z) coords.Add(v1);
                    if (z == v2.Z) coords.Add(v2);
                    if (z == v3.Z) coords.Add(v3);

                    result.Paths.Add(new LinePath(coords));
                    continue;
                }

                // triangle intersects the plane
                if (liesBetween(z, v1.Z, v2.Z))
                    coords.Add(getPlaneSegmentIntersection(v1, v2, z));

                if (liesBetween(z, v2.Z, v3.Z))
                    coords.Add(getPlaneSegmentIntersection(v2, v3, z));

                if (liesBetween(z, v3.Z, v1.Z))
                    coords.Add(getPlaneSegmentIntersection(v3, v1, z));

                result.Paths.Add(new LinePath(coords));
            }
            result.Simplify();

            return result;
        }

        private Polyline[] buildIsolinesInternal(IEnumerable<Coordinate3D> surfacePoints, double[] zLevels, List<Triangle> triangles)
        {
            Polyline[] result = new Polyline[zLevels.Length];
            if (zLevels.Length > 0)
            {
                double minZ = zLevels.Min();
                double maxZ = zLevels.Max();
                double surfaceMin = surfacePoints.Min(coord => coord.Z);
                double surfaceMax = surfacePoints.Max(coord => coord.Z);

                if (surfaceMin > maxZ || surfaceMax < minZ)
                    return result;

                List<ICoordinate> coords = new List<ICoordinate>();
                surfacePoints.ToList().ForEach(coord => coords.Add(coord));

                // build tesselation
                VoronoiBuilder vb = new VoronoiBuilder();
                VoronoiTesselation tesselation = vb.Build(coords, true);
                tesselation.Triangles.ToList().ForEach(t => triangles.Add(t));

                vb = null;
                tesselation = null;

                Comparison<Triangle> comparision = delegate(Triangle t1, Triangle t2)
                {
                    if (t1 == t2)
                        return 0;

                    double z11 = ((Coordinate3D)t1.Cell1.DataPoint).Z;
                    double z12 = ((Coordinate3D)t1.Cell2.DataPoint).Z;
                    double z13 = ((Coordinate3D)t1.Cell3.DataPoint).Z;

                    double z21 = ((Coordinate3D)t2.Cell1.DataPoint).Z;
                    double z22 = ((Coordinate3D)t2.Cell2.DataPoint).Z;
                    double z23 = ((Coordinate3D)t2.Cell3.DataPoint).Z;

                    double z1 = Math.Min(Math.Min(z11, z12), z13);
                    double z2 = Math.Min(Math.Min(z21, z22), z23);

                    if (z1 < z2) return -1;
                    if (z1 > z2) return 1;
                    return 0;
                };

                // sort triangles by minimal z-value
                triangles.Sort(comparision);

                for (int i = 0; i < zLevels.Length; i++)
                {
                    if (zLevels[i] < surfaceMin)
                    {
                        result[i] = new Polyline();
                        continue;
                    }

                    result[i] = getIsoline(triangles, zLevels[i]);
                }
            }

            return result;
        }

        private Polyline[] buildIsolinesInternal(IEnumerable<Triangle> triangles, double[] zLevels)
        {
            Polyline[] result = new Polyline[zLevels.Length];
            if (zLevels.Length > 0)
            {
                double minZ = zLevels.Min();
                double maxZ = zLevels.Max();
                double surfaceMin = 
                    triangles.Min(tr => 
                        Math.Min(
                            Math.Min(((Coordinate3D)tr.Cell1.DataPoint).Z, ((Coordinate3D)tr.Cell2.DataPoint).Z),
                            ((Coordinate3D)tr.Cell3.DataPoint).Z));

                double surfaceMax = triangles.Max(tr =>
                        Math.Max(
                            Math.Max(((Coordinate3D)tr.Cell1.DataPoint).Z, ((Coordinate3D)tr.Cell2.DataPoint).Z),
                            ((Coordinate3D)tr.Cell3.DataPoint).Z));

                if (surfaceMin > maxZ || surfaceMax < minZ)
                    return result;

                Comparison<Triangle> comparision = delegate(Triangle t1, Triangle t2)
                {
                    if (t1 == t2)
                        return 0;

                    double z11 = ((Coordinate3D)t1.Cell1.DataPoint).Z;
                    double z12 = ((Coordinate3D)t1.Cell2.DataPoint).Z;
                    double z13 = ((Coordinate3D)t1.Cell3.DataPoint).Z;

                    double z21 = ((Coordinate3D)t2.Cell1.DataPoint).Z;
                    double z22 = ((Coordinate3D)t2.Cell2.DataPoint).Z;
                    double z23 = ((Coordinate3D)t2.Cell3.DataPoint).Z;

                    double z1 = Math.Min(Math.Min(z11, z12), z13);
                    double z2 = Math.Min(Math.Min(z21, z22), z23);

                    if (z1 < z2) return -1;
                    if (z1 > z2) return 1;
                    return 0;
                };

                List<Triangle> tempTriangles = triangles.ToList();

                // sort triangles by minimal z-value
                tempTriangles.Sort(comparision);

                for (int i = 0; i < zLevels.Length; i++)
                {
                    if (zLevels[i] < surfaceMin)
                    {
                        result[i] = new Polyline();
                        continue;
                    }

                    result[i] = getIsoline(tempTriangles, zLevels[i]);
                }
            }

            return result;
        }

        private LevelRange[] getRanges(double surfaceMin, double surfaceMax, double[] zLevels)
        {
            List<LevelRange> result = new List<LevelRange>();
            List<double> values = new List<double>();
            
            values.Add(surfaceMax);

            foreach (double d in zLevels)
                if (d > surfaceMin && d < surfaceMax)
                    values.Add(d);

            values.Add(surfaceMin);

            for (int i = 0; i < values.Count - 1; i++)
                result.Add(new LevelRange(values[i + 1], values[i]));

            return result.ToArray();
        }

        private List<LinePath> getPathsForPolygonBuilding(IEnumerable<Polyline> isolines, IList<ICoordinate> convexHull)
        {
            List<LinePath> result = new List<LinePath>();

            foreach (Polyline p in isolines)
                foreach (LinePath lp in p.Paths)
                    result.Add(lp);

            Polyline bounds = new Polyline(convexHull);

            // close boundary
            bounds.Paths[0].Vertices.Add(bounds.Paths[0].Vertices[0]);

            result.Add(bounds.Paths[0]);

            return result;
        }

        private List<LinePath> getPathsForPolygonBuilding(IEnumerable<Coordinate3D> surfacePoints, IEnumerable<Polyline> isolines, out IList<ICoordinate> convexHull)
        {
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach (Coordinate3D c in surfacePoints)
                coords.Add(c);

            convexHull = PlanimetryAlgorithms.GetConvexHull(coords);
            Polyline bounds = new Polyline(convexHull);

            return getPathsForPolygonBuilding(isolines, convexHull);
        }

        private List<LinePath> getPathsForPolygonBuilding(IEnumerable<Triangle> triangles, IEnumerable<Polyline> isolines, out IList<ICoordinate> convexHull)
        {
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach (Triangle t in triangles)
            {
                coords.Add(t.Cell1.DataPoint);
                coords.Add(t.Cell2.DataPoint);
                coords.Add(t.Cell3.DataPoint);
            }

            convexHull = PlanimetryAlgorithms.GetConvexHull(coords);
            Polyline bounds = new Polyline(convexHull);

            return getPathsForPolygonBuilding(isolines, convexHull);
        }

        private LevelRangePolygon[] assignLevelsToPolygons(IEnumerable<Coordinate3D> surfacePoints, IList<Polygon> polygons, List<Triangle> triangles, double[] zLevels, BoundingRectangle bounds)
        {
            double surfaceMin = surfacePoints.Min(coord => coord.Z);
            double surfaceMax = surfacePoints.Max(coord => coord.Z);

            return assignLevelsToPolygons(surfaceMin, surfaceMax, polygons, triangles, zLevels, bounds);
        }

        private LevelRangePolygon[] assignLevelsToPolygons(IList<Polygon> polygons, List<Triangle> triangles, double[] zLevels, BoundingRectangle bounds)
        {
            double surfaceMin =
                triangles.Min(tr =>
                    Math.Min(
                        Math.Min(((Coordinate3D)tr.Cell1.DataPoint).Z, ((Coordinate3D)tr.Cell2.DataPoint).Z),
                        ((Coordinate3D)tr.Cell3.DataPoint).Z));

            double surfaceMax = triangles.Max(tr =>
                    Math.Max(
                        Math.Max(((Coordinate3D)tr.Cell1.DataPoint).Z, ((Coordinate3D)tr.Cell2.DataPoint).Z),
                        ((Coordinate3D)tr.Cell3.DataPoint).Z));

            return assignLevelsToPolygons(surfaceMin, surfaceMax, polygons, triangles, zLevels, bounds);
        }

        private LevelRangePolygon[] assignLevelsToPolygons(double surfaceMin, double surfaceMax, IList<Polygon> polygons, List<Triangle> triangles, double[] zLevels, BoundingRectangle bounds)
        {
            List<Feature> triangularFeatures = new List<Feature>();

            foreach (Triangle t in triangles)
            {
                Polygon p = new Polygon(new ICoordinate[] 
                { 
                    new Coordinate3D(t.Cell1.DataPoint.Values()),
                    new Coordinate3D(t.Cell2.DataPoint.Values()),
                    new Coordinate3D(t.Cell3.DataPoint.Values())
                });
                triangularFeatures.Add(new Feature(p));
            }

            triangles = null;

            KDTree index = new KDTree(bounds);
            index.MinObjectCount = 10;
            index.MaxDepth = 20;
            index.BoxSquareThreshold = index.IndexedSpace.Width * index.IndexedSpace.Height / 10000;

            index.Build(triangularFeatures);

            LevelRange[] ranges = getRanges(surfaceMin, surfaceMax, zLevels);

            List<LevelRangePolygon> result = new List<LevelRangePolygon>();

            foreach (Polygon p in polygons)
            {
                ICoordinate c = p.PointOnSurface();
                List<Feature> t = new List<Feature>();
                index.QueryObjectsContainingPoint(c, t);
                foreach (Feature f in t)
                {
                    Polygon triangle = f.Polygon;
                    if (triangle.ContainsPoint(c))
                    {
                        double z =
                            getZ(c,
                                 new Coordinate3D(triangle.Contours[0].Vertices[0].Values()),
                                 new Coordinate3D(triangle.Contours[0].Vertices[1].Values()),
                                 new Coordinate3D(triangle.Contours[0].Vertices[2].Values()));

                        LevelRange range = getRange(z, ranges);
                        result.Add(new LevelRangePolygon(range, p));
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        private double getZ(ICoordinate p, Coordinate3D v1, Coordinate3D v2, Coordinate3D v3)
        {
            double a = (v2.Y - v1.Y) * (v3.Z - v1.Z) - (v3.Y - v1.Y) * (v2.Z - v1.Z);
            double b = (v2.X - v1.X) * (v3.Z - v1.Z) - (v3.X - v1.X) * (v2.Z - v1.Z);
            double c = (v2.X - v1.X) * (v3.Y - v1.Y) - (v3.X - v1.X) * (v2.Y - v1.Y);
            double z = v1.Z + ((p.Y - v1.Y) * b - (p.X - v1.X) * a) / c;

            return z;
        }

        private LevelRange getRange(double z, IEnumerable<LevelRange> ranges)
        {
            foreach (LevelRange range in ranges)
                if (range.Contains(z))
                    return range;

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Builds isolines.
        /// </summary>
        /// <param name="surfacePoints">A 3D-coordinates defining source</param>
        /// <param name="zLevels">A descending array of z-values that define cutting planes</param>
        /// <returns>An array containing isolines</returns>
        public Polyline[] BuildIsolines(IEnumerable<Coordinate3D> surfacePoints, double[] zLevels)
        {
            if (surfacePoints == null)
                throw new ArgumentNullException("surfacePoints");

            for (int i = 0; i < zLevels.Length - 1; i++)
                if(zLevels[i] <= zLevels[i + 1])
                    throw new ArgumentException("Z-levels aren't descending and/or distinct", "zLevels");

            List<Triangle> triangles = new List<Triangle>();

            return buildIsolinesInternal(surfacePoints, zLevels, triangles);
        }

        /// <summary>
        /// Builds isolines.
        /// </summary>
        /// <param name="triangles">An object that enumerates triangles defining surface. All triangle coordinates should be instances of the MapAround.Geometry.Coordinate3D.</param>
        /// <param name="zLevels">A descending array of z-values that define cutting planes</param>
        /// <returns>An array containing isolines</returns>
        public Polyline[] BuildIsolines(IEnumerable<Triangle> triangles, double[] zLevels)
        {
            if (triangles == null)
                throw new ArgumentNullException("triangles");

            for (int i = 0; i < zLevels.Length - 1; i++)
                if (zLevels[i] <= zLevels[i + 1])
                    throw new ArgumentException("Z-levels aren't descending and/or distinct", "zLevels");

            return buildIsolinesInternal(triangles, zLevels);
        }

        /// <summary>
        /// Builds polygons for the z-value ranges.
        /// </summary>
        /// <param name="surfacePoints">A 3D-coordinates defining source</param>
        /// <param name="zLevels">A descending array of z-values that define ranges</param>
        /// <returns>An array containing level range polygons</returns>
        public LevelRangePolygon[] BuildPolygonsForLevelRanges(IEnumerable<Coordinate3D> surfacePoints, double[] zLevels)
        {
            if (surfacePoints == null)
                throw new ArgumentNullException("surfacePoints");

            for (int i = 0; i < zLevels.Length - 1; i++)
                if (zLevels[i] <= zLevels[i + 1])
                    throw new ArgumentException("Z-levels aren't descending and/or distinct", "zLevels");

            List<Triangle> triangles = new List<Triangle>();
            Polyline[] isolines = buildIsolinesInternal(surfacePoints, zLevels, triangles);

            IList<ICoordinate> coonvexHull;

            List<LinePath> paths = getPathsForPolygonBuilding(surfacePoints, isolines, out coonvexHull);

            IList<Polygon> polygons;
            IList<Segment> dangles;
            IList<Segment> cuts;
            PolygonBuilder.BuildPolygons(paths, out polygons, out dangles, out cuts);

            LevelRangePolygon[] result = 
                assignLevelsToPolygons(surfacePoints, polygons, triangles, zLevels, PlanimetryAlgorithms.GetPointsBoundingRectangle(coonvexHull));

            return result;
        }

        /// <summary>
        /// Builds polygons for the z-value ranges.
        /// </summary>
        /// <param name="triangles">An object that enumerates triangles defining surface. All triangle coordinates should be instances of the MapAround.Geometry.Coordinate3D.</param>
        /// <param name="zLevels">A descending array of z-values that define ranges</param>
        /// <returns>An array containing level range polygons</returns>
        public LevelRangePolygon[] BuildPolygonsForLevelRanges(IEnumerable<Triangle> triangles, double[] zLevels)
        {
            if (triangles == null)
                throw new ArgumentNullException("triangles");

            for (int i = 0; i < zLevels.Length - 1; i++)
                if (zLevels[i] <= zLevels[i + 1])
                    throw new ArgumentException("Z-levels aren't descending and/or distinct", "zLevels");

            Polyline[] isolines = buildIsolinesInternal(triangles, zLevels);

            IList<ICoordinate> coonvexHull;

            List<LinePath> paths = getPathsForPolygonBuilding(triangles, isolines, out coonvexHull);

            IList<Polygon> polygons;
            IList<Segment> dangles;
            IList<Segment> cuts;
            PolygonBuilder.BuildPolygons(paths, out polygons, out dangles, out cuts);

            List<Triangle> tempTriangles = triangles.ToList();

            LevelRangePolygon[] result =
                assignLevelsToPolygons(polygons, tempTriangles, zLevels, PlanimetryAlgorithms.GetPointsBoundingRectangle(coonvexHull));

            return result;
        }
    }
}
#endif