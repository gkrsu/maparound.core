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
** File: ShadedReliefBuilder.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Building equally illuminated polygons of 2.5D-surface
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

    /// <summary>
    /// Builds equally illuminated areas of 2.5D-surface.
    /// </summary>
    public class ShadedReliefBuilder
    {
        /// <summary>
        /// Describes a lightened polygon.
        /// </summary>
        public struct LightenedPolygon
        {
            /// <summary>
            /// Polygon.
            /// </summary>
            public Polygon Polygon;

            /// <summary>
            /// Luminocity value.
            /// </summary>
            public double Luminocity;

            /// <summary>
            /// Initializes a new instance of the MapAround.Extensions.Surfaces.ShadedReliefBuilder.LightenedPolygon
            /// </summary>
            /// <param name="polygon"></param>
            /// <param name="luminance"></param>
            public LightenedPolygon(Polygon polygon, double luminance)
            {
                Polygon = polygon;
                Luminocity = luminance;
            }
        }

        private Polygon mergeTriangles(List<Polygon> polygons)
        {
            List<Polygon> activePolygons = polygons;
            List<Polygon> mergedPolygons = new List<Polygon>();

            while (activePolygons.Count != 1)
            {
                for (int i = 0; i < activePolygons.Count; i += 2)
                {
                    Polygon union;
                    if (i == activePolygons.Count - 1)
                        union = activePolygons[i];
                    else
                        union = (Polygon)activePolygons[i].Union(activePolygons[i + 1])[0];

                    mergedPolygons.Add(union);
                }
                activePolygons = mergedPolygons;
                mergedPolygons = new List<Polygon>();
            }

            return activePolygons[0];
        }

        /// <summary>
        /// Calculates luminosity of the triangle.
        /// </summary>
        /// <param name="triangle">A triangle</param>
        /// <param name="lightX">An X component of the light vector</param>
        /// <param name="lightY">A Y component of the light vector</param>
        /// <param name="lightZ">A Z component of the light vector</param>
        /// <param name="zFactor">A value at which to multiply z-values for luminosity calculation</param>
        /// <returns>A luminosity value ranging from zero to one</returns>
        public static double GetLuminosity(Triangle triangle, double lightX, double lightY, double lightZ, double zFactor)
        {
            if (!(triangle.Cell1.DataPoint is Coordinate3D) ||
                !(triangle.Cell2.DataPoint is Coordinate3D) ||
                !(triangle.Cell3.DataPoint is Coordinate3D))
                throw new ArgumentException("All coordinates should be instances of the MapAround.Geometry.Coordinate3D", "triangle");

            Coordinate3D p1 = (Coordinate3D)triangle.Cell1.DataPoint.Clone();
            Coordinate3D p2 = (Coordinate3D)triangle.Cell2.DataPoint.Clone();
            Coordinate3D p3 = (Coordinate3D)triangle.Cell3.DataPoint.Clone();

            p1.Z = p1.Z * zFactor;
            p2.Z = p2.Z * zFactor;
            p3.Z = p3.Z * zFactor;

            if (PlanimetryAlgorithms.OrientationIndex(p1, p2, p3) < 0)
            {
                Coordinate3D temp = p1;
                p1 = p2;
                p2 = temp;
            }

            double A = p1.Y * (p2.Z - p3.Z) + p2.Y * (p3.Z - p1.Z) + p3.Y * (p1.Z - p2.Z);
            double B = p1.Z * (p2.X - p3.X) + p2.Z * (p3.X - p1.X) + p3.Z * (p1.X - p2.X);
            double C = p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y);
            double D = -(p1.X * (p2.Y * p3.Z - p3.Y * p2.Z) + p2.X * (p3.Y * p1.Z - p1.Y * p3.Z) + p3.X * (p1.Y * p2.Z - p2.Y * p1.Z));

            double sinePhi =
                Math.Abs(A * lightX + B * lightY + C * lightZ) /
                Math.Sqrt(A * A + B * B + C * C) /
                Math.Sqrt(lightX * lightX + lightY * lightY + lightZ * lightZ);

            Coordinate3D lightPoint = new Coordinate3D();
            lightPoint.X = p1.X + lightX;
            lightPoint.Y = p1.Y + lightY;
            lightPoint.Z = p1.Z + lightZ;

            if (A * lightPoint.X + B * lightPoint.Y + C * lightPoint.Z + D > 0)
                return 0;

            return sinePhi;
        }

        /// <summary>
        /// Builds shaded relief.
        /// </summary>
        /// <param name="triangles">An object that enumerates triangles defining surface. All triangle coordinates should be instances of the MapAround.Geometry.Coordinate3D.</param>
        /// <param name="lightX">An X component of the light vector</param>
        /// <param name="lightY">A Y component of the light vector</param>
        /// <param name="lightZ">A Z component of the light vector</param>
        /// <param name="zFactor">A value at which to multiply z-values for luminosity calculation</param>
        /// <param name="luminosityLevelNumber">A number of resoluted luminosity levels</param>
        /// <returns>An array containing lightened polygons</returns>
        public LightenedPolygon[] BuildShadedRelief(IEnumerable<Triangle> triangles,
            double lightX,
            double lightY,
            double lightZ,
            double zFactor,
            int luminosityLevelNumber)
        {
            LightenedPolygon[] result = new LightenedPolygon[luminosityLevelNumber];

            List<double> luminosity = new List<double>();

            foreach (Triangle t in triangles)
                luminosity.Add(GetLuminosity(t, lightX, lightY, lightZ, zFactor));

            double minL = luminosity.Min();
            double maxL = luminosity.Max();

            List<Polygon>[] collectors = new List<Polygon>[luminosityLevelNumber];

            int i = 0;
            foreach (Triangle t in triangles)
            {
                int j = (int)((luminosity[i] - minL) / ((maxL - minL) / (double)luminosityLevelNumber));

                if (j == collectors.Length)
                    j--;

                if (collectors[j] == null)
                    collectors[j] = new List<Polygon>();

                collectors[j].Add(new Polygon(new ICoordinate[] { t.Cell1.DataPoint, t.Cell2.DataPoint, t.Cell3.DataPoint }));

                i++;
            }

            i = 0;
            double luminosityRange = (maxL - minL) / (double)luminosityLevelNumber;
            foreach (List<Polygon> collector in collectors)
            {
                result[i] = new LightenedPolygon(mergeTriangles(collector), minL + luminosityRange * i + luminosityRange * 0.5);
                i++;
            }

            return result;
        }

        /// <summary>
        /// Builds shaded relief.
        /// </summary>
        /// <param name="surfacePoints">A 3D-coordinates defining source</param>
        /// <param name="lightX">An X component of the light vector</param>
        /// <param name="lightY">A Y component of the light vector</param>
        /// <param name="lightZ">A Z component of the light vector</param>
        /// <param name="zFactor">A value at which to multiply z-values for luminosity calculation</param>
        /// <param name="luminosityLevelNumber">A number of resoluted luminosity levels</param>
        /// <returns>An array containing lightened polygons</returns>
        public LightenedPolygon[] BuildShadedRelief(IEnumerable<Coordinate3D> surfacePoints,
            double lightX,
            double lightY,
            double lightZ,
            double zFactor,
            int luminosityLevelNumber)
        {
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach(Coordinate3D c in surfacePoints)
                coords.Add(c);

            VoronoiBuilder vb = new VoronoiBuilder();
            VoronoiTesselation tesselation = vb.Build(coords, true);

            return BuildShadedRelief(tesselation.Triangles, lightX, lightY, lightZ, zFactor, luminosityLevelNumber);
        }
    }
}

#endif