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
** File: Transformers.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes that perform coordinate transformations
**
=============================================================================*/

namespace MapAround.CoordinateSystems.Transformations
{
    using System.Collections.Generic;
    using MapAround.Geometry;
    using MapAround.Mapping;

    /// <summary>
    /// Applies transformations to features' coordinates.
    /// </summary>
    public class FeatureTransformer
    {
        /// <summary>
        /// Applies transformation to features' coordinates.
        /// </summary>
        /// <param name="feature">Feature which coordinates should be transformed</param>
        /// <param name="transform">The transformation to apply</param>
        public static void TransformFeatureInPlace(Feature feature, IMathTransform transform)
        {
            switch (feature.FeatureType)
            { 
                case FeatureType.Point:
                    feature.Point = GeometryTransformer.TransformPoint(feature.Point, transform);
                    break;
                case FeatureType.Polyline:
                    feature.Polyline = GeometryTransformer.TransformPolyline(feature.Polyline, transform);
                    break;
                case FeatureType.Polygon:
                    feature.Polygon = GeometryTransformer.TransformPolygon(feature.Polygon, transform);
                    break;
                case FeatureType.MultiPoint:
                    feature.MultiPoint = GeometryTransformer.TransformMultiPoint(feature.MultiPoint, transform);
                    break;
            }
        }
    }

    /// <summary>
    /// Applies transformation to geometries' coordinates.
    /// </summary>
    public class GeometryTransformer
    {
        /// <summary>
        /// Transforms coordinates of the bounding rectangle.
        /// </summary>
        /// <param name="box">Rectangle to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed rectangle</returns>
        public static BoundingRectangle TransformBoundingRectangle(BoundingRectangle box, IMathTransform transform)
        {
            if (box == null)
                return null;
            ICoordinate[] corners = new ICoordinate[4];
            corners[0] = PlanimetryEnvironment.NewCoordinate(transform.Transform(box.Min.Values()));
            corners[1] = PlanimetryEnvironment.NewCoordinate(transform.Transform(box.Max.Values()));
            corners[2] = PlanimetryEnvironment.NewCoordinate(transform.Transform(PlanimetryEnvironment.NewCoordinate(box.MinX, box.MaxY).Values()));
            corners[3] = PlanimetryEnvironment.NewCoordinate(transform.Transform(PlanimetryEnvironment.NewCoordinate(box.MaxX, box.MinY).Values())); 

            BoundingRectangle result = new BoundingRectangle();
            for (int i = 0; i < 4; i++)
                result.Join(corners[i]);
            return result;
        }

        /// <summary>
        /// Transforms coordinates of the point geometry.
        /// </summary>
        /// <param name="p">Point to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed point</returns>
        public static PointD TransformPoint(PointD p, IMathTransform transform)
        {
            return new PointD(transform.Transform(p.CoordsArray()));
        }

        /// <summary>
        /// Transforms coordinates of the segment.
        /// </summary>
        /// <param name="s">Segment to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed segment</returns>
        public static Segment TransformSegment(Segment s, IMathTransform transform)
        {
            Segment result = new Segment();
            result.V1 = PlanimetryEnvironment.NewCoordinate(transform.Transform(s.V1.Values()));
            result.V2 = PlanimetryEnvironment.NewCoordinate(transform.Transform(s.V2.Values()));
            return result;
        }

        /// <summary>
        /// Transforms coordinates of the multipoint.
        /// </summary>
        /// <param name="multiPoint">Multipoint to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed multipoint</returns>
        public static MultiPoint TransformMultiPoint(MultiPoint multiPoint, IMathTransform transform)
        {
            List<double[]> points = new List<double[]>();

            for (int i = 0; i < multiPoint.Points.Count; i++)
                points.Add(new double[2] { multiPoint.Points[i].X, multiPoint.Points[i].Y });

            return new MultiPoint(transform.TransformList(points));
        }

        /// <summary>
        /// Transforms coordinates of the contour.
        /// </summary>
        /// <param name="contour">Contour to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed contour</returns>
        public static Contour TransformContour(Contour contour, IMathTransform transform)
        {
            List<double[]> points = new List<double[]>();

            for (int i = 0; i < contour.Vertices.Count; i++)
                points.Add(new double[2] { contour.Vertices[i].X, contour.Vertices[i].Y });

            return new Contour(transform.TransformList(points));
        }

        /// <summary>
        /// Transforms coordinates of the line path.
        /// </summary>
        /// <param name="linePath">Line path to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed line path</returns>
        public static LinePath TransformLinePath(LinePath linePath, IMathTransform transform)
        {
            List<double[]> points = new List<double[]>();

            for (int i = 0; i < linePath.Vertices.Count; i++)
                points.Add(new double[2] { linePath.Vertices[i].X, linePath.Vertices[i].Y });

            return new LinePath(transform.TransformList(points));
        }

        /// <summary>
        /// Transforms coordinates of the polyline.
        /// </summary>
        /// <param name="polyline">Polyline to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed polyline</returns>
        public static Polyline TransformPolyline(Polyline polyline, IMathTransform transform)
        {
            Polyline result = new Polyline();
            foreach (LinePath path in polyline.Paths)
                result.Paths.Add(TransformLinePath(path, transform));

            return result;
        }

        /// <summary>
        /// Transforms coordinates of the polygon.
        /// </summary>
        /// <param name="polygon">Polygon to transform</param>
        /// <param name="transform">The transformation to apply</param>
        /// <returns>The transformed polygon</returns>
        public static Polygon TransformPolygon(Polygon polygon, IMathTransform transform)
        {
            Polygon result = new Polygon();
            foreach (Contour contour in polygon.Contours)
                result.Contours.Add(TransformContour(contour, transform));

            return result;
        }
    }
}