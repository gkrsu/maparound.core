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
** File: PlanimetryAlgorithms.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Frequently used robust 2D-plane geometry algorithms 
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a dimension of geometric object.
    /// Dimension is the number of independent parameters you need 
    /// to pick out a unique point inside. This is an intuitive explanation. 
    /// See http://en.wikipedia.org/wiki/Lebesgue_covering_dimension
    /// for accurate definition.
    /// </summary>
    public enum Dimension : int
    {
        /// <summary>
        /// Applies to objects that have no dimension. Empty geometries etc.
        /// </summary>
        None = -1,
        /// <summary>
        /// Dimension value of zero-dimensional objects like points.
        /// </summary>
        Zero = 0,
        /// <summary>
        /// Dimension value of one-dimensional objects like curves.
        /// </summary>
        One = 1,
        /// <summary>
        /// Dimension value of two-dimensional objects like surfaces.
        /// </summary>
        Two = 2
    }

    /// <summary>
    /// Implements basic 2D spatial algorithms.
    /// </summary>
    public static class PlanimetryAlgorithms
    {
        [ThreadStatic]
        private static double _tolerance;
        [ThreadStatic]
        private static bool _toleranceAssigned;

        /// <summary>
        /// Default tolerance value.
        /// </summary>
        public static readonly double DefaultTolerance = 1.0e-9;

        /// <summary>
        /// Gets or sets a tolerance value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the distance between two points is less than this value, 
        /// algorithms operate by comparing the coordinates of the points 
        /// considered to coincide. 
        /// To handle all situations correctly, you should select this value 
        /// based on the size of the treated area. 
        /// </para>
        /// <para>
        /// Each thread have a separate instance of Tolerance.
        /// </para>
        /// </remarks>
        public static double Tolerance
        {
            get 
            {
                if (!_toleranceAssigned)
                    _tolerance = DefaultTolerance;
                return _tolerance; 
            }
            set 
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Tolerance should not be negative", "value");

                _toleranceAssigned = true;
                _tolerance = value; 
            }
        }

        /// <summary>
        /// Determines if the axis-aligned rectangle contains the point.
        /// </summary>
        /// <param name="rectangle">The diagonal of the bounding rectangle</param>
        /// <param name="p">The point</param>
        /// <returns>true, if the axis-aligned rectangle contains the point, false otherwise</returns>       
        public static bool RectangleContainsPoint(Segment rectangle, ICoordinate p)
        {
            return p.X >= rectangle.V1.X && p.X <= rectangle.V2.X &&
                p.Y >= rectangle.V1.Y && p.Y <= rectangle.V2.Y;
        }

        /// <summary>
        /// Determines if one axis-aligned rectangle contains ather axis-aligned rectangle.
        /// </summary>
        /// <param name="container">Container diagonal</param>
        /// <param name="content">Content diagonal</param>
        /// <returns>true, if one container contains content, false otherwise</returns>
        public static bool RectangleContainsRectangle(Segment container, Segment content)
        {
            return container.V1.X <= content.V1.X && container.V1.Y <= content.V1.Y &&
                       container.V2.X >= content.V2.X && container.V2.Y >= content.V2.Y;
        }


        /// <summary>
        /// Determines if two axis-aligned rectangles are intersect.
        /// </summary>
        /// <param name="rectangle1">Diagonal of first rectangle</param>
        /// <param name="rectangle2">Diagonal of second rectangle</param>
        /// <returns>true, if two axis-aligned rectangles are intersect, false otherwise</returns>
        public static bool AreRectanglesIntersect(Segment rectangle1, Segment rectangle2)
        {
            return AreRectanglesIntersect(ref rectangle1, ref rectangle2);
        }

        /// <summary>
        /// Joins two axis-aligned rectangles.
        /// </summary>
        /// <param name="rectangle1">Diagonal of first rectangle</param>
        /// <param name="rectangle2">Diagonal of second rectangle</param>
        /// <returns>Diagonal of the axis-aligned rectangle that contains both rectangles</returns>
        public static Segment JoinRectangles(Segment rectangle1, Segment rectangle2)
        {
            return new Segment(Math.Min(rectangle1.V1.X, rectangle2.V1.X),
                               Math.Min(rectangle1.V1.Y, rectangle2.V1.Y),
                               Math.Max(rectangle1.V2.X, rectangle2.V2.X),
                               Math.Max(rectangle1.V2.Y, rectangle2.V2.Y));
        }

        /// <summary>
        /// Determines if two axis-aligned rectangles are intersect.
        /// </summary>
        /// <param name="rectangle1">Diagonal of first rectangle</param>
        /// <param name="rectangle2">Diagonal of second rectangle</param>
        /// <returns>true, if two axis-aligned rectangles are intersect, false otherwise</returns>
        public static bool AreRectanglesIntersect(ref Segment rectangle1, ref Segment rectangle2)
        {
            double s1xMin, s2xMin, s1xMax, s2xMax;

            if (rectangle1.V1.X > rectangle1.V2.X)
            { s1xMax = rectangle1.V1.X; s1xMin = rectangle1.V2.X; }
            else
            { s1xMax = rectangle1.V2.X; s1xMin = rectangle1.V1.X; }

            if (rectangle2.V1.X > rectangle2.V2.X)
            { s2xMax = rectangle2.V1.X; s2xMin = rectangle2.V2.X; }
            else
            { s2xMax = rectangle2.V2.X; s2xMin = rectangle2.V1.X; }

            // verification of projections on X axis
            if (s1xMax < s2xMin || s1xMin > s2xMax)
                return false;

            double s1yMin, s2yMin, s1yMax, s2yMax;

            if (rectangle1.V1.Y > rectangle1.V2.Y)
            { s1yMax = rectangle1.V1.Y; s1yMin = rectangle1.V2.Y; }
            else
            { s1yMax = rectangle1.V2.Y; s1yMin = rectangle1.V1.Y; }

            if (rectangle2.V1.Y > rectangle2.V2.Y)
            { s2yMax = rectangle2.V1.Y; s2yMin = rectangle2.V2.Y; }
            else
            { s2yMax = rectangle2.V2.Y; s2yMin = rectangle2.V1.Y; }

            // check the projections on the Y axis
            if (s1yMax < s2yMin || s1yMin > s2yMax)
                return false;

            return true;
        }

        /// <summary>
        /// Calculates a bounding rectangle for a list of points.
        /// </summary>
        /// <param name="points">A list of points</param>
        /// <returns>A bounding rectangle</returns>
        public static BoundingRectangle GetPointsBoundingRectangle(IList<ICoordinate> points)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (ICoordinate p in points)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new BoundingRectangle(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Determines if the point lies on the line.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">A segment defining straight line</param>
        /// <returns>true, if the point lies on the line, false otherwise</returns>
        public static bool LiesOnLine(ICoordinate coordinate, Segment s)
        {
            return NearTheLine(coordinate, s, PlanimetryAlgorithms._tolerance);
        }

        /// <summary>
        /// Determines if the distance between line and point 
        /// is less than specified value.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">A segment defining straight line</param>
        /// <param name="epsilon">A distance</param>
        /// <returns>true, if the distance between line and point 
        /// is less than the epsilon value</returns>
        public static bool NearTheLine(ICoordinate coordinate, Segment s, double epsilon)
        {
            return DistanceToLine(s, coordinate) < epsilon;
        }

        /// <summary>
        /// Computes a scalar product of two vectors.
        /// </summary>
        /// <param name="s1">A segment defining first vector</param>
        /// <param name="s2">A segment defining second vector</param>
        /// <returns>A scalar product value</returns>
        public static double ScalarProduct(Segment s1, Segment s2)
        {
            return ((s1.V2.X - s1.V1.X) * (s2.V2.X - s2.V1.X) + (s1.V2.Y - s1.V1.Y) * (s2.V2.Y - s2.V1.Y));
        }

        /// <summary>
        /// Determines whether the point lies on segment.
        /// Comparisions used MapAround.Geometry.PlanimetryAlgorithms.Tolerance value.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">Segment</param>
        /// <returns>true, if the point lies on segment, false otherwise</returns>
        public static bool LiesOnSegment(ICoordinate coordinate, Segment s)
        {
            if (coordinate.X < Math.Min(s.V1.X, s.V2.X) - PlanimetryAlgorithms._tolerance)
                return false;

            if (coordinate.X > Math.Max(s.V1.X, s.V2.X) + PlanimetryAlgorithms._tolerance)
                return false;

            if (coordinate.Y < Math.Min(s.V1.Y, s.V2.Y) - PlanimetryAlgorithms._tolerance)
                return false;

            if (coordinate.Y > Math.Max(s.V1.Y, s.V2.Y) + PlanimetryAlgorithms._tolerance)
                return false;

            return DistanceToSegment(coordinate, s) < PlanimetryAlgorithms._tolerance;
        }

        /// <summary>
        /// Determines whether the point lies on ray.
        /// Comparisions used MapAround.Geometry.PlanimetryAlgorithms.Tolerance value.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">Segment defining a ray. V1 is the begining of the ray.</param>
        /// <returns>true, if the point lies on ray, false otherwise</returns>
        public static bool LiesOnRay(ICoordinate coordinate, Segment s)
        {
            return DistanceToRay(coordinate, s) < PlanimetryAlgorithms._tolerance;
        }

        /// <summary>
        /// Computes a distance between the point and the segment.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">Segment</param>
        /// <returns>Distance between the point and the segment</returns>
        public static double DistanceToSegment(ICoordinate coordinate, Segment s)
        {
            if (s.IsSingular())
                return PlanimetryAlgorithms.Distance(coordinate, s.V1);

            Segment w0 = new Segment();
            w0.V1 = s.V1;
            w0.V2 = coordinate;

            double w0v = ScalarProduct(s, w0);

            if (w0v <= 0) // minimum distance to the vertex v1
                return Distance(coordinate, s.V1);

            if (ScalarProduct(s, s) <= w0v) // minimum distance to the vertex v2
                return Distance(coordinate, s.V2);

            // minimum perpendicular to the line
            return DistanceToLine(s, coordinate);
        }

        /// <summary>
        /// Computes a distance between the point and the ray.
        /// </summary>
        /// <param name="coordinate">Point coordinates</param>
        /// <param name="s">Segment defining ray. V1 is the begining of ray.</param>
        /// <returns>Distance between the point and the ray</returns>
        public static double DistanceToRay(ICoordinate coordinate, Segment s)
        {
            Segment w0 = new Segment();
            w0.V1 = s.V1;
            w0.V2 = coordinate;

            double w0v = ScalarProduct(s, w0);

            if (w0v <= 0) // minimum distance to the vertex v1
                return Distance(coordinate, s.V1);

            //minimum perpendicular to the line
            return DistanceToLine(s, coordinate);
        }

        /// <summary>
        /// Computes a distance between the point and the straight line.
        /// </summary>
        /// <param name="p">Point coordinates</param>
        /// <param name="s">Segment defining line</param>
        /// <returns>Distance between the point and the straight line</returns>
        public static double DistanceToLine(Segment s, ICoordinate p)
        {
            double result = SignedDistanceToLine(s, p);
            if (double.IsNaN(result))
                return result;

            return
                Math.Abs(result);
        }

        /// <summary>
        /// Computes a signed distance between the point and the straight line.
        /// </summary>
        /// <param name="c">Point coordinates</param>
        /// <param name="s">Segment defining line</param>
        /// <returns>Distance between the point and the straight line</returns>
        public static double SignedDistanceToLine(Segment s, ICoordinate c)
        {
            double length = s.Length();

            if (length < PlanimetryAlgorithms._tolerance)
                return Double.NaN; // segment degenerate

            double v1x = s.V1.X;
            double v2x = s.V2.X;
            double v1y = s.V1.Y;
            double v2y = s.V2.Y;

            double x = c.X;
            double y = c.Y;

            double mx = (v1x + v2x + x) / 3;
            double my = (v1y + v2y + y) / 3;
            v1x -= mx; v2x -= mx; x -= mx;
            v1y -= my; v2y -= my; y -= my;

            return
                ((v1y - v2y) * x + (v2x - v1x) * y + v1x * v2y - v2x * v1y) / length;
        }

        /// <summary>
        /// Computes Euclidean distance between two points.
        /// </summary>
        /// <param name="c1">Coordinates of first point</param>
        /// <param name="c2">Coordinates of second point</param>
        /// <returns>Euclidean distance between two points</returns>
        public static double Distance(ICoordinate c1, ICoordinate c2)
        {
            double dx = c1.X - c2.X;
            double dy = c1.Y - c2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Computes value indicating whether 
        /// the distance between two points is less than
        /// the tolerance value.
        /// </summary>
        /// <param name="p1">Coordinate of first point</param>
        /// <param name="p2">Coordinate of second point</param>
        /// <returns>true, if the distance between the points is less than <see cref="Tolerance"/>, otherwise false</returns>
        public static bool DistanceTolerant(ICoordinate p1, ICoordinate p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= _tolerance;
        }


        /// <summary>
        /// Computes an intersection of two straight lines.
        /// </summary>
        /// <param name="s1">Segment defining first line</param>
        /// <param name="s2">Segment defining second line</param>
        /// <param name="crossCoord">MapAround.Geometry.ICoordinate implementor for storing
        /// coordinate of cross point</param>
        /// <returns>The object representing an intersection dimension</returns>
        public static Dimension DirectsIntersection(Segment s1,
                                               Segment s2,
                                               ref ICoordinate crossCoord)
        {
            double uNumerator = (s2.V2.X - s2.V1.X) * (s1.V1.Y - s2.V1.Y) - (s2.V2.Y - s2.V1.Y) * (s1.V1.X - s2.V1.X);
            double Denominator = (s2.V2.Y - s2.V1.Y) * (s1.V2.X - s1.V1.X) - (s2.V2.X - s2.V1.X) * (s1.V2.Y - s1.V1.Y);

            if (Denominator == 0) // parallel or coincident lines
            {
                if (uNumerator == 0)
                {
                    if ((s1.V2.X - s1.V1.X) * (s1.V1.Y - s2.V1.Y) - (s1.V2.Y - s1.V1.Y) * (s1.V1.X - s2.V1.X) == 0)
                        // lines coincide
                        return Dimension.One;
                }
                else
                    // the lines are parallel
                    return Dimension.None;
            }

            double u = uNumerator / Denominator;

            crossCoord.X = s1.V1.X + u * (s1.V2.X - s1.V1.X); // rascchityvaem coordinates of the point of intersection
            crossCoord.Y = s1.V1.Y + u * (s1.V2.Y - s1.V1.Y);

            return Dimension.Zero;
        }

        /// <summary>
        /// Builds perpendicular segment.
        /// </summary>
        /// <param name="segment">Source segment</param>
        /// <returns>Segment that is perpendicular to source</returns>
        public static Segment GetPerpendicular(Segment segment)
        {
            ICoordinate center = segment.Center();

            if (segment.V1.X == segment.V2.X)
                return new Segment(center, PlanimetryEnvironment.NewCoordinate(center.X + segment.Length(), center.Y));

            if (segment.V1.Y == segment.V2.Y)
                return new Segment(center, PlanimetryEnvironment.NewCoordinate(center.X, center.Y + segment.Length()));

            double a = segment.V1.Y - segment.V2.Y;
            double b = segment.V2.X - segment.V1.X;

            Segment result = new Segment(center, PlanimetryEnvironment.NewCoordinate(center.X + a, center.Y + b));

            return result;
        }

        /// <summary>
        /// Computes an intersection of two line segments.
        /// This implementation performs a robust checks on disjoining.
        /// </summary>
        /// <param name="s1">First segment</param>
        /// <param name="s2">Second segment</param>
        /// <param name="intersectionPoint">An intersection point</param>
        /// <param name="intersectionSegment">An intersection segment</param>
        /// <returns>A dimension of intersection</returns>
        public static Dimension RobustSegmentsIntersection(Segment s1,
                                                            Segment s2,
                                                            out ICoordinate intersectionPoint,
                                                            out Segment intersectionSegment)
        {
            intersectionSegment = new Segment();
            intersectionPoint = PlanimetryEnvironment.NewCoordinate(0, 0);

            if (!AreRectanglesIntersect(ref s1, ref s2)) // disjoint segments can be dropped by checking
                return Dimension.None;          // by not crossing the projections on the axes

            // Perhaps both ends of the segment are on the same side of another segment 
            // in which case there is no intersection, and the calculation 
            // of relative position and straight points - resistant to loss of accuracy.
            int p11 = PlanimetryAlgorithms.OrientationIndex(s1, s2.V1);
            int p21 = PlanimetryAlgorithms.OrientationIndex(s1, s2.V2);

            if(p11 != 0 && p11 == p21)
                return Dimension.None;

            int p12 = PlanimetryAlgorithms.OrientationIndex(s2, s1.V1);
            int p22 = PlanimetryAlgorithms.OrientationIndex(s2, s1.V2);

            if (p12 != 0 && p12 == p22)
                return Dimension.None;

            //Collinear segments.
            if (p11 == 0 && p12 == 0 && p21 == 0 && p22 == 0)
            {
                List<ICoordinate> list = new List<ICoordinate>();
                list.Add(s1.V1);
                list.Add(s1.V2);
                list.Add(s2.V1);
                list.Add(s2.V2);

                //if the segments are vertical, vertical sorting
                if (s1.V1.X == s1.V2.X)
                    list.Sort((ICoordinate p1, ICoordinate p2) => p1.Y > p2.Y ? -1 : 1);
                //otherwise horizontally
                else
                    list.Sort((ICoordinate p1, ICoordinate p2) => p1.X > p2.X ? -1 : 1);
                intersectionSegment = new Segment(list[2], list[1]);

                if (intersectionSegment.Length() <= PlanimetryAlgorithms.Tolerance)
                {
                    intersectionPoint = list[2];
                    return Dimension.Zero;
                }

                return Dimension.One;
            }

            if (p11 == 0)
            {
                intersectionPoint = (ICoordinate)s2.V1.Clone();
                return Dimension.Zero;
            }

            if (p21 == 0)
            {
                intersectionPoint = (ICoordinate)s2.V2.Clone();
                return Dimension.Zero;
            }

            if (p12 == 0)
            {
                intersectionPoint = (ICoordinate)s1.V1.Clone();
                return Dimension.Zero;
            }

            if (p22 == 0)
            {
                intersectionPoint = (ICoordinate)s1.V2.Clone();
                return Dimension.Zero;
            }


            // Handled better broadcast segments so that the origin
            // was located at the center of their coverage. 
            // In this case, we may get a few more digits.
            BoundingRectangle br = s1.GetBoundingRectangle();
            br.Join(s2.V1);
            br.Join(s2.V2);
            ICoordinate translationPoint = br.Center();

            Segment s11 = new Segment(s1.V1.X - translationPoint.X, s1.V1.Y - translationPoint.Y,
                s1.V2.X - translationPoint.X, s1.V2.Y - translationPoint.Y);

            Segment s21 = new Segment(s2.V1.X - translationPoint.X, s2.V1.Y - translationPoint.Y,
                s2.V2.X - translationPoint.X, s2.V2.Y - translationPoint.Y);

            Dimension dim = SegmentsIntersection(s11, s21, out intersectionPoint, out intersectionSegment);
            
            if (dim == Dimension.Zero)
            {
                intersectionPoint.X += translationPoint.X;
                intersectionPoint.Y += translationPoint.Y;
            }

            if (dim == Dimension.One)
            {
                intersectionSegment.V1.X += translationPoint.X;
                intersectionSegment.V1.Y += translationPoint.Y;

                intersectionSegment.V2.X += translationPoint.X;
                intersectionSegment.V2.Y += translationPoint.Y;
            }

            return dim;
        }

        /// <summary>
        /// Computes an intersection of two line segments.
        /// Non-robust but fast version.
        /// </summary>
        /// <param name="s1">First segment</param>
        /// <param name="s2">Second segment</param>
        /// <param name="intersectionPoint">An intersection point</param>
        /// <param name="intersectionSegment">An intersection segment</param>
        /// <returns>A dimension of intersection</returns>
        public static Dimension SegmentsIntersection(Segment s1,
                                                            Segment s2,
                                                            out ICoordinate intersectionPoint,
                                                            out Segment intersectionSegment)
        {
            intersectionSegment = new Segment();
            intersectionPoint = PlanimetryEnvironment.NewCoordinate(0, 0);

            if (!AreRectanglesIntersect(ref s1, ref s2)) // disjoint segments can be dropped by checking
                return Dimension.None;          // by not crossing the projections on the axes

            double s1v1x = s1.V1.X;
            double s1v1y = s1.V1.Y;
            double s1v2x = s1.V2.X;
            double s1v2y = s1.V2.Y;

            double s2v1x = s2.V1.X;
            double s2v1y = s2.V1.Y;
            double s2v2x = s2.V2.X;
            double s2v2y = s2.V2.Y;

            double u1Numerator = (s2v2x - s2v1x) * (s1v1y - s2v1y) - (s2v2y - s2v1y) * (s1v1x - s2v1x);
            double u2Numerator = (s1v2x - s1v1x) * (s1v1y - s2v1y) - (s1v2y - s1v1y) * (s1v1x - s2v1x);
            double denominator = (s2v2y - s2v1y) * (s1v2x - s1v1x) - (s2v2x - s2v1x) * (s1v2y - s1v1y);

            if (denominator == 0) //parallel or coincident lines
                if (u1Numerator == 0 && u2Numerator == 0)
                {
                    // OPPORTUNITY segments degenerate, then their intersection - a point
                    if (s1.IsSingular() || s2.IsSingular())
                    {
                        intersectionPoint = (ICoordinate)s1.V1.Clone();
                        return Dimension.Zero;
                    }

                    // if the lines coincide, and segments intersect.
                    // We have already examined the intersection of their projections on the coordinate axes

                    List<ICoordinate> list = new List<ICoordinate>();
                    list.Add((ICoordinate)s1.V1.Clone());
                    list.Add((ICoordinate)s1.V2.Clone());
                    list.Add((ICoordinate)s2.V1.Clone());
                    list.Add((ICoordinate)s2.V2.Clone());

                    //if the segments are vertical, vertical sorting
                    if (s1.V1.X == s1.V2.X)
                        list.Sort((ICoordinate p1, ICoordinate p2) => p1.Y > p2.Y ? -1 : 1);
                    //otherwise horizontally
                    else
                        list.Sort((ICoordinate p1, ICoordinate p2) => p1.X > p2.X ? -1 : 1);
                    intersectionSegment = new Segment(list[2], list[1]);

                    if (intersectionSegment.IsSingular())
                    {
                        intersectionPoint = list[2];
                        return Dimension.Zero;
                    }

                    return Dimension.One;
                }

            double u1 = u1Numerator / denominator;
            double u2 = u2Numerator / denominator;

            intersectionPoint.X = s1v1x + u1 * (s1v2x - s1v1x); // are counting the coordinates of the intersection
            intersectionPoint.Y = s1v1y + u1 * (s1v2y - s1v1y);

            // segments intersect if u1 and u2 are in the range [0, 1]
            return (u1 >= 0 && u1 <= 1 && u2 >= 0 && u2 <= 1) ? Dimension.Zero : Dimension.None;
        }

        /// <summary>
        /// Calculates the center of mass of points.
        /// </summary>
        /// <remarks>
        /// Masses of points are set equal.
        /// </remarks>
        public static ICoordinate GetCentroid(IList<ICoordinate> coordinates)
        {
            if (coordinates.Count == 0)
                return null;

            ICoordinate result = PlanimetryEnvironment.NewCoordinate(0, 0);
            foreach (ICoordinate p in coordinates)
            {
                result.X += p.X;
                result.Y += p.Y;
            }

            result.X = result.X / coordinates.Count;
            result.Y = result.Y / coordinates.Count;

            return result;
        }

        /// <summary>
        /// Orders points over coordinate axis (horizontal or vertical).
        /// </summary>
        /// <param name="coordinates">A list containing the points coordinates</param>
        public static void OrderPointsOverAxis(List<ICoordinate> coordinates)
        {
            if (coordinates.Count < 2)
                return;
            // if the segment is not vertical, horizontal sorting
            if (Math.Abs(coordinates[0].X - coordinates[1].X) > PlanimetryAlgorithms.Tolerance)
                SortCoordsHorizontally(coordinates);
            else
                SortPointsVertically(coordinates);
        }

        /// <summary>
        /// Orders points along a direction defined by specified segment.
        /// </summary>
        /// <param name="coordinates">A list containing the points coordinates</param>
        /// <param name="s">Segment defining the direction</param>
        public static void OrderPointsOverSegment(List<ICoordinate> coordinates, Segment s)
        {
            Segment perpendicular = PlanimetryAlgorithms.GetPerpendicular(s);
            coordinates.Sort((ICoordinate p1, ICoordinate p2) =>
                PlanimetryAlgorithms.SignedDistanceToLine(perpendicular, p1) <
                PlanimetryAlgorithms.SignedDistanceToLine(perpendicular, p2) ? -1 : 1);
        }

        /// <summary>
        /// Orders points horizontally.
        /// </summary>
        /// <param name="coordinates">A list containing the points coordinates</param>
        public static void SortCoordsHorizontally(List<ICoordinate> coordinates)
        {
            coordinates.Sort((ICoordinate p1, ICoordinate p2) => p1.X < p2.X ? -1 : 1);
        }

        /// <summary>
        /// Orders points vertically.
        /// </summary>
        /// <param name="coordinates">A list containing the points coordinates</param>
        public static void SortPointsVertically(List<ICoordinate> coordinates)
        {
            coordinates.Sort((ICoordinate p1, ICoordinate p2) => p1.Y < p2.Y ? -1 : 1);
        }

        private class CoordinateComparer : IEqualityComparer<ICoordinate>
        {
            public bool Equals(ICoordinate c1, ICoordinate c2)
            {
                if (object.ReferenceEquals(c1, c2)) return true;

                if (c1 == null || c2 == null) return false;

                return c1.ExactEquals(c2);
            }

            public int GetHashCode(ICoordinate c)
            {
                if (c == null) return 0;

                return c.X.GetHashCode() ^ c.Y.GetHashCode();
            }

        }

        /// <summary>
        /// Computes a convex hull coordinates for the specified points.
        /// </summary>
        /// <param name="coordinates">Enumerator of coordinates for which convex hull should be computed</param>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public static IList<ICoordinate> GetConvexHull(IEnumerable<ICoordinate> coordinates)
        {
            List<ICoordinate> result = new List<ICoordinate>();

            foreach (ICoordinate p in coordinates.Distinct(new CoordinateComparer()))
                result.Add(p);

            if (result.Count <= 2)
                return result;

            if (result.Count > 200)
                filterPointsForConvexHull(result);

            ICoordinate temp = null;

            // find "extreme" point (with a minimum ordinate,
            // if multiple points have the ordinate value, choose one with a minimum abscissa)
            for (int i = 1; i < result.Count; i++)
            {
                if ((result[i].Y < result[0].Y) || ((result[i].Y == result[0].Y)
                     && (result[i].X < result[0].X)))
                {
                    temp = result[0];
                    result[0] = result[i];
                    result[i] = temp;
                }
            }
            temp = result[0];
            result.RemoveAt(0);

            // Compare function for radial sorting
            Comparison<ICoordinate> comparision = delegate (ICoordinate p, ICoordinate q)
                {
                    double dxp = p.X - temp.X;
                    double dyp = p.Y - temp.Y;
                    double dxq = q.X - temp.X;
                    double dyq = q.Y - temp.Y;

                    int orient = PlanimetryAlgorithms.OrientationIndex(new Segment(temp, p), q);

                    if (orient == 1) return 1;
                    if (orient == -1) return -1;

                    // collinear points, are investigated further
                    double op = dxp * dxp + dyp * dyp;
                    double oq = dxq * dxq + dyq * dyq;
                    if (op < oq) return -1;
                    if (op > oq) return 1;
                    return 0;
                };

            // sort the list of points around the "extreme" point
            result.Sort(comparision);

            // reject coincident points
            List<ICoordinate> r = new List<ICoordinate>();
            foreach (ICoordinate p in result)
                addUniqCoordinate(r, p);

            result = r;

            result.Insert(0, temp);

            // perform scanning Graham
            Stack<ICoordinate> pointStack = new Stack<ICoordinate>();

            pointStack.Push(result[0]);
            pointStack.Push(result[1]);
            pointStack.Push(result[2]);
            for (int i = 3; i < result.Count; i++)
            {
                ICoordinate c = pointStack.Pop();

                while (PlanimetryAlgorithms.OrientationIndex(new Segment(pointStack.Peek(), c), result[i]) > 0)
                    c = pointStack.Pop();

                pointStack.Push(c);
                pointStack.Push(result[i]);
            }

            result.Clear();
            while (pointStack.Count > 0)
                result.Add(pointStack.Pop());

            return result;
        }

        private static void addUniqCoordinate(IList<ICoordinate> coordinates, ICoordinate coordinate)
        {
            foreach (ICoordinate c in coordinates)
            {
                if (c.X < coordinate.X - PlanimetryAlgorithms._tolerance ||
                   c.X > coordinate.X + PlanimetryAlgorithms._tolerance)
                    continue;

                if (c.Y < coordinate.Y - PlanimetryAlgorithms._tolerance ||
                    c.Y > coordinate.Y + PlanimetryAlgorithms._tolerance)
                    continue;

                if (c.Equals(coordinate))
                    return;
            }
            coordinates.Add(coordinate);
        }

        private static void filterPointsForConvexHull(List<ICoordinate> coordinates)
        {
            List<ICoordinate> upPoints = new List<ICoordinate>();
            List<ICoordinate> downPoints = new List<ICoordinate>();
            List<ICoordinate> leftPoints = new List<ICoordinate>();
            List<ICoordinate> rightPoints = new List<ICoordinate>();

            BoundingRectangle bounds = GetPointsBoundingRectangle(coordinates);
            foreach (ICoordinate c in coordinates)
            {
                if (c.X == bounds.MinX) leftPoints.Add(c);
                if (c.X == bounds.MaxX) rightPoints.Add(c);
                if (c.Y == bounds.MinY) downPoints.Add(c);
                if (c.X == bounds.MaxY) upPoints.Add(c);
            }

            leftPoints.Sort((c1, c2) => c1.Y > c2.Y ? 1 : -1);
            upPoints.Sort((c1, c2) => c1.X > c2.X ? 1 : -1);
            rightPoints.Sort((c1, c2) => c1.Y > c2.Y ? -1 : 1);
            downPoints.Sort((c1, c2) => c1.X > c2.X ? -1 : 1);
            Polygon p = new Polygon();
            p.Contours.Add(new Contour());

            leftPoints.ForEach(c => p.Contours[0].Vertices.Add(c));
            upPoints.ForEach(c => p.Contours[0].Vertices.Add(c));
            rightPoints.ForEach(c => p.Contours[0].Vertices.Add(c));
            downPoints.ForEach(c => p.Contours[0].Vertices.Add(c));

            for (int i = coordinates.Count - 1; i >= 0; i--)
            { 
                ICoordinate current = coordinates[i];
                if(current.X > bounds.MinX && current.X < bounds.MaxX &&
                   current.Y > bounds.MinY && current.Y < bounds.MaxY)
                {
                    if(p.ContainsPoint(current))
                        coordinates.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Computes an orientation of the line (segment) and the point.
        /// <remarks>
        /// If this method returns values with the same sign for two points, 
        /// then these points are located on the same side of the 
        /// line, otherwise - in different sides. 
        /// For a point lying on the line method returns zero.
        /// </remarks>
        /// </summary>
        /// <param name="s">A MapAround.Geometry.Segment instance</param>
        /// <param name="c">Point coordinates</param>
        /// <returns>Integer whose sign indicates the location of the point relative to the segment</returns>
        public static int OrientationIndex(Segment s, ICoordinate c)
        {
            return DeterminantSign(s.V2.X - s.V1.X,
                                   s.V2.Y - s.V1.Y,
                                   c.X - s.V2.X,
                                   c.Y - s.V2.Y);
        }

        /// <summary>
        /// Computes an orientation of the line (segment) and the point.
        /// </summary>
        /// <remarks>
        /// If this method returns a value of the same sign for two points, 
        /// then these points are located on the same side of the 
        /// line, otherwise - in different sides. 
        /// For a point lying on the line method returns zero.
        /// </remarks>
        /// <param name="с1">First endpoint of the segment</param>
        /// <param name="с2">Second endpoint of the segment</param>
        /// <param name="c">Point coordinates</param>
        /// <returns>Integer whose sign indicates the location of the point relative to the segment</returns>
        public static int OrientationIndex(ICoordinate с1, ICoordinate с2, ICoordinate c)
        {
            return DeterminantSign(с2.X - с1.X,
                                   с2.Y - с1.Y,
                                   c.X - с2.X,
                                   c.Y - с2.Y);
        }

        private static readonly string _gridUndefined = "Snapping grid is undefined at this area";


        /// <summary>
        /// Snaps coordinates to the grid nodes.
        /// Coordinates can be changed to a value less than cellSize / 2.
        /// </summary>
        /// <param name="coordinates">A list containing the coordinates</param>
        /// <param name="origin">The origin of the grid</param>
        /// <param name="cellSize">Cell size of the grid</param>
        public static void SnapCoordinatesToGrid(IList<ICoordinate> coordinates, ICoordinate origin, double cellSize)
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                ICoordinate p = coordinates[i];
                PlanimetryAlgorithms.SnapToGrid(ref p, origin, cellSize);
                coordinates[i] = p;
            }
        }

        /// <summary>
        /// Snaps coordinate to the grid nodes.
        /// Coordinate can be changed to a value less than cellSize / 2.
        /// </summary>
        /// <param name="coordinate">Coordinate to snap</param>
        /// <param name="origin">The origin of the grid</param>
        /// <param name="cellSize">Cell size of the grid</param>
        public static void SnapToGrid(ref ICoordinate coordinate, ICoordinate origin, double cellSize)
        {
            // network coverage align with the origin in the unit.
            double unitGridExtent = 1e15; 

            if (cellSize <= 0)
                throw new ArgumentOutOfRangeException("Cell size of snapping grid should be positive", "cellSize");

            if (cellSize <= 1 / unitGridExtent)
                throw new ArgumentException("Too small cell size", "cellSize");

            double relativeGridExtent = unitGridExtent / cellSize;

            // Required to perform a number of checks to hit a point in the domain of the grid alignment.
            if (coordinate.X == 0)
            {
                if (origin.X > relativeGridExtent || origin.X < -relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }
            else if (origin.X == 0)
            {
                if (coordinate.X > relativeGridExtent || coordinate.X < -relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }
            else
            {
                double x = Math.Abs(origin.X / coordinate.X);
                if (x > relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }

            if (coordinate.Y == 0)
            {
                if (origin.Y > relativeGridExtent || origin.Y < -relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }
            else if (origin.Y == 0) 
            {
                if (coordinate.Y > relativeGridExtent || coordinate.Y < -relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }
            else
            {
                double y = Math.Abs(origin.Y / coordinate.Y);
                if (y > relativeGridExtent)
                    throw new InvalidOperationException(_gridUndefined);
            }

            coordinate.X = Math.Floor((coordinate.X - origin.X) / cellSize + 0.5) * cellSize + origin.X;
            coordinate.Y = Math.Floor((coordinate.Y - origin.Y) / cellSize + 0.5) * cellSize + origin.Y;
        }

        /// <summary>
        /// Calculates the sign of 2x2 determinant.
        /// </summary>
        /// <param name="x1">Matrix element at 1, 1</param>
        /// <param name="y1">Matrix element at 1, 2</param>
        /// <param name="x2">Matrix element at 2, 1</param>
        /// <param name="y2">Matrix element at 2, 2</param>
        /// <remarks>
        /// Direct calculation of the determinant (Math.Sign (x1 * y2 - x2 * y1)) 
        /// is impossible, since the product of very large numbers can lead to loss 
        /// of precision greatly exceeding the difference between the products.
        /// This method provides a robust calculation.
        /// </remarks>
        /// <returns>
        /// -1, if the determinant is nagetive,
        /// 1, if the determinant is positive,
        /// 0, if the determinant equals zero.
        /// </returns>
        public static int DeterminantSign(double x1, double y1, double x2, double y2)
        {
            int sign = 1;
            double temp;
            double k;

            //  check for zero
            if ((x1 == 0) || (y2 == 0))
            {
                if ((y1 == 0) || (x2 == 0))
                    return 0;
                else
                {
                    if (y1 > 0)
                        return x2 > 0 ? -sign : sign;
                    else
                        return x2 > 0 ? sign : -sign;
                }
            }
            if ((y1 == 0) || (x2 == 0))
            {
                if (y2 > 0)
                    return x1 > 0 ? sign : -sign;
                else
                    return x1 > 0 ? -sign : sign;
            }

            // do y1 and y2 positive so that y2> = y1
            if (y1 > 0)
            {
                if (y2 > 0)
                {
                    if (y1 > y2)
                    {
                        sign = -sign;
                        temp = x1; x1 = x2; x2 = temp;
                        temp = y1; y1 = y2; y2 = temp;
                    }
                }
                else
                {
                    if (y1 <= -y2)
                    {
                        sign = -sign;
                        x2 = -x2;
                        y2 = -y2;
                    }
                    else
                    {
                        temp = x1; x1 = -x2; x2 = temp;
                        temp = y1; y1 = -y2; y2 = temp;
                    }
                }
            }
            else
            {
                if (y2 > 0)
                {
                    if (-y1 <= y2)
                    {
                        sign = -sign;
                        x1 = -x1;
                        y1 = -y1;
                    }
                    else
                    {
                        temp = -x1; x1 = x2; x2 = temp;
                        temp = -y1; y1 = y2; y2 = temp;
                    }
                }
                else
                {
                    if (y1 >= y2)
                    {
                        x1 = -x1;
                        y1 = -y1;
                        x2 = -x2;
                        y2 = -y2;
                    }
                    else
                    {
                        sign = -sign;
                        temp = -x1; x1 = -x2; x2 = temp;
                        temp = -y1; y1 = -y2; y2 = temp;
                    }
                }
            }

            // make positive x1 and x2
            if (x1 > 0)
            {
                if (x2 > 0)
                {
                    if (x1 > x2)
                        return sign;
                }
                else
                    return sign;
            }
            else
            {
                if (x2 > 0)
                    return -sign;
                else
                {
                    if (x1 >= x2)
                    {
                        sign = -sign;
                        x1 = -x1;
                        x2 = -x2;
                    }
                    else
                        return -sign;
                }
            }

            //  all elements are positive, x1 <= x2, y1 <= y2
            while (true)
            {
                k = Math.Floor(x2 / x1);
                x2 = x2 - k * x1;
                y2 = y2 - k * y1;

                if (y2 < 0)
                    return -sign;

                if (y2 > y1)
                    return sign;

                if (x1 > x2 + x2)
                {
                    if (y1 < y2 + y2)
                        return sign;
                }
                else
                {
                    if (y1 > y2 + y2)
                        return -sign;
                    else
                    {
                        sign = -sign;
                        x2 = x1 - x2;
                        y2 = y1 - y2;
                    }
                }
                if (y2 == 0)
                    return x2 == 0 ? 0 : -sign;

                if (x2 == 0.0)
                    return sign;

                k = Math.Floor(x1 / x2);
                x1 = x1 - k * x2;
                y1 = y1 - k * y2;

                if (y1 < 0)
                    return sign;

                if (y1 > y2)
                    return -sign;

                if (x2 > x1 + x1)
                {
                    if (y2 < y1 + y1)
                        return -sign;
                }
                else
                {
                    if (y2 > y1 + y1)
                        return sign;
                    else
                    {
                        x1 = x2 - x1;
                        y1 = y2 - y1;
                        sign = -sign;
                    }
                }
                if (y1 == 0)
                    return x1 == 0 ? 0 : sign;

                if (x1 == 0)
                    return -sign;
            }
        }
    }
}