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
** File: Simplifier.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Generalization of geometries
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using MapAround.Indexing;

    #if !DEMO

    /// <summary>
    /// Instances of Maparound.Geometry.SDMinVertexWeightNeededEventArgs contains data
    /// for the SDMinVertexWeightNeeded event.
    /// </summary>
    public class SDMinVertexWeightNeededEventArgs : EventArgs
    {
        private double _weight = 0;
        private int _pathIndex;
        private int _pointIndex;
        private ICoordinate _point;
        private ICoordinate _previousPoint;
        private ICoordinate _nextPoint;

        /// <summary>
        /// Gets an index of point into line path.
        /// </summary>
        public int PointIndex
        {
            get { return _pointIndex; }
        }

        /// <summary>
        /// Gets a line path index.
        /// Gets the index of the line.
        /// </summary>
        public int PathIndex
        {
            get { return _pathIndex; }
        }

        /// <summary>
        /// Gets a coordinate of point to determine a weight.
        /// </summary>
        public ICoordinate Point
        {
            get { return _point; }
        }

        /// <summary>
        /// Gets a coordinates of previous point.
        /// </summary>
        public ICoordinate PreviousPoint
        {
            get { return _previousPoint; }
        }

        /// <summary>
        /// Gets a coordinates of next point.
        /// </summary>
        public ICoordinate NextPoint
        {
            get { return _nextPoint; }
        }

        /// <summary>
        /// Gets or sets the point weight.
        /// </summary>
        public double Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.SDMinVertexWeightNeededEventArgs.
        /// </summary>
        /// <param name="previousPoint">Coordinate of previous point</param>
        /// <param name="point">Coordinate of point for defining a weight</param>
        /// <param name="nextPoint">Coordinate of next point</param>
        /// <param name="pathIndex">Line path index</param>
        /// <param name="pointIndex">Index of a point in the line path</param>
        public SDMinVertexWeightNeededEventArgs(ICoordinate previousPoint, 
                                                ICoordinate point, 
                                                ICoordinate nextPoint, 
                                                int pathIndex,
                                                int pointIndex)
        {
            _previousPoint = previousPoint;
            _point = point;
            _nextPoint = nextPoint;
            _pathIndex = pathIndex;
            _pointIndex = pointIndex;
        }
    }

    /// <summary>
    /// Simplifies a geometry, ensuring that the result 
    /// is a geometry having the same topology.
    /// Implements an S-DMin algorithm.
    /// </summary>
    public class GeometrySimplifier
    {
        /// <summary>
        /// types of the weighting function.
        /// </summary>
        public enum VertexWeightingType 
        { 
            /// <summary>
            /// Normalized linear weight.
            /// </summary>
            NormalizedLinear,

            /// <summary>
            /// The cube of rotation angle.
            /// </summary>
            AngleCube,

            /// <summary>
            /// Area difference.
            /// </summary>
            SquareDifference,

            /// <summary>
            /// A custom weighting function.
            /// </summary>
            Custom
        }

        /// <summary>
        /// Represents a weighted vertex in the S-DMin algorithm.
        /// </summary>
        private class SDMinVertex : IIndexable
        {
            public double Weight = 0;
            public int PathIndex = 0;
            public int PointIndex = 0;

            public bool Deleted = false;
            public bool IsCrossSegmentVertex = false;

            public SDMinVertex Previous = null;
            public SDMinVertex Next = null;

            private BoundingRectangle _boundingRectangle;

            #region IIndexable Members

            public BoundingRectangle BoundingRectangle
            {
                get
                {
                    return _boundingRectangle;
                }
                set
                {
                    _boundingRectangle = value;
                }
            }

            #endregion

            #region ICloneable Members

            public object Clone()
            {
                SDMinVertex vertex = new SDMinVertex();
                vertex.Weight = this.Weight;
                vertex.Next = this.Next;
                vertex.Previous = this.Previous;
                vertex.PathIndex = this.PathIndex;
                vertex.PointIndex = this.PointIndex;

                return vertex;
            }

            #endregion
        }

        /// <summary>
        /// Represents an intersection point in the S-DMin algorithm.
        /// </summary>
        private class SDMinCrossPoint : IIndexable
        {
            public ICoordinate Point = null;
            private BoundingRectangle _boundingRectangle;

            #region IIndexable Members

            public BoundingRectangle BoundingRectangle
            {
                get
                {
                    return _boundingRectangle;
                }
                set
                {
                    _boundingRectangle = value;
                }
            }

            #endregion

            #region ICloneable Members

            public object Clone()
            {
                SDMinCrossPoint result = new SDMinCrossPoint();
                result.Point = this.Point;
                result.BoundingRectangle = this.BoundingRectangle;
                return result;
            }

            #endregion
        }

        private VertexWeightingType _vertexWeighting = VertexWeightingType.NormalizedLinear;

        private double getVertexWeight(Polyline polyline, int pathIndex, int pointIndex)
        {
            ICoordinate p1 = polyline.Paths[pathIndex].Vertices[pointIndex - 1];
            ICoordinate p2 = polyline.Paths[pathIndex].Vertices[pointIndex];
            ICoordinate p3 = polyline.Paths[pathIndex].Vertices[pointIndex + 1];

            switch (_vertexWeighting)
            {
                case VertexWeightingType.NormalizedLinear:
                case VertexWeightingType.AngleCube:
                    // length of the segments
                    double s1 = PlanimetryAlgorithms.Distance(p1, p2);
                    double s2 = PlanimetryAlgorithms.Distance(p2, p3);

                    double s1s2 = s1 * s2;

                    //   angle of rotation
                    double angle = Math.PI - Math.Abs(Math.Acos(((p1.X - p2.X) * (p3.X - p2.X) + (p1.Y - p2.Y) * (p3.Y - p2.Y)) / s1s2));

                    if (_vertexWeighting == VertexWeightingType.SquareDifference)
                        return s1s2 * angle / (s1 + s2);
                    else
                        return s1s2 * angle * angle * angle;
                case VertexWeightingType.SquareDifference:
                    return Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y));
                case VertexWeightingType.Custom:
                    if (SDMinVertexWeightNeeded != null)
                    {
                        SDMinVertexWeightNeededEventArgs args = 
                            new SDMinVertexWeightNeededEventArgs(p1, p2, p3, pathIndex, pointIndex);
                        SDMinVertexWeightNeeded(this, args);
                        return args.Weight;
                    }
                    break;
            }

            return 0;
        }

        private SDMinVertex getWeightedVertex(Polyline polyline, int pathIndex, int pointIndex)
        {
            SDMinVertex result = new SDMinVertex();
            result.PathIndex = pathIndex;
            result.PointIndex = pointIndex;
            result.Weight = getVertexWeight(polyline, pathIndex, pointIndex);
            BoundingRectangle br = new PointD(polyline.Paths[pathIndex].Vertices[pointIndex]).GetBoundingRectangle();
            br.Grow(PlanimetryAlgorithms.Tolerance);
            result.BoundingRectangle = br;
            return result;
        }

        private List<SDMinVertex> getWeightedVertices(Polyline polyline)
        {
            List<SDMinVertex> result = new List<SDMinVertex>();
            for (int i = 0; i < polyline.Paths.Count; i++)
                for (int j = 0; j < polyline.Paths[i].Vertices.Count; j++)
                    if (j != 0 && j != polyline.Paths[i].Vertices.Count - 1)
                    {
                        SDMinVertex vertex = getWeightedVertex(polyline, i, j);
                        vertex.Previous = result[result.Count - 1];
                        vertex.Previous.Next = vertex;
                        result.Add(vertex);
                    }
                    else
                    {
                        SDMinVertex vertex = new SDMinVertex();
                        vertex.PathIndex = i;
                        vertex.PointIndex = j;
                        BoundingRectangle br = new PointD(polyline.Paths[i].Vertices[j]).GetBoundingRectangle();
                        br.Grow(PlanimetryAlgorithms.Tolerance);
                        vertex.BoundingRectangle = br;
                        vertex.Weight = double.PositiveInfinity;
                        if (j != 0)
                        {
                            vertex.Previous = result[result.Count - 1];
                            vertex.Previous.Next = vertex;
                        }
                        result.Add(vertex);
                    }

            Comparison<SDMinVertex> comparision = delegate(SDMinVertex weight1, SDMinVertex weight2)
            {
                if(double.IsInfinity(weight1.Weight) && double.IsInfinity(weight2.Weight)) 
                    return 0;

                if (double.IsInfinity(weight1.Weight))
                    return 1;

                if (double.IsInfinity(weight2.Weight))
                    return -1;

                if (weight1 == weight2)
                    return 0;

                return weight1.Weight > weight2.Weight ? 1 : -1;
            };

            // sort list
            result.Sort(comparision);
            return result;
        }

        private KDTree getCrossPointsIndex(Polyline polyline)
        {
            List<MonotoneChain> chains = new List<MonotoneChain>();
            foreach (LinePath path in polyline.Paths)
                path.AppendMonotoneChains(chains);

            List<SDMinCrossPoint> crossPoints = new List<SDMinCrossPoint>();

            for (int i = 0; i < chains.Count - 1; i++)
                for (int j = i + 1; j < chains.Count; j++)
                    if (chains[i].BoundsIntersect(chains[j]))
                    {
                        List<ICoordinate> points = chains[i].GetCrossPoints(chains[j]);
                        foreach (ICoordinate p in points)
                        {
                            bool isChainIBoundsPoint = p.ExactEquals(chains[i].FirstPoint) ||
                                 p.ExactEquals(chains[i].LastPoint);

                            bool isChainJBoundsPoint = p.ExactEquals(chains[j].FirstPoint) ||
                                 p.ExactEquals(chains[j].LastPoint);

                            if (!(isChainIBoundsPoint && isChainJBoundsPoint))
                            {
                                SDMinCrossPoint cp = new SDMinCrossPoint();
                                cp.Point = p;
                                cp.BoundingRectangle = new PointD(p).GetBoundingRectangle();
                                cp.BoundingRectangle.Grow(PlanimetryAlgorithms.Tolerance);
                                crossPoints.Add(cp);
                            }
                        }
                    }

            BoundingRectangle br = new BoundingRectangle();
            foreach (SDMinCrossPoint p in crossPoints)
                br.Join(p.BoundingRectangle);

            KDTree result = new KDTree(br);
            result.MaxDepth = 10;
            result.MinObjectCount = 10;
            if (br.IsEmpty())
                br.Join(PlanimetryEnvironment.NewCoordinate(0, 0));
            result.BoxSquareThreshold = br.Width * br.Height / 10000;
            result.Build(crossPoints);
            return result;
        }

        private KDTree buildVertexIndex(List<SDMinVertex> weightedVertices, Polyline polyline)
        { 
            BoundingRectangle br = polyline.GetBoundingRectangle();
            br.Grow(PlanimetryAlgorithms.Tolerance);
            KDTree result = new KDTree(br);
            result.MaxDepth = 14;
            result.MinObjectCount = 10;
            result.BoxSquareThreshold = br.Width * br.Height / 10000;
            result.Build(weightedVertices);
            return result;
        }

        private Polyline simplifyPolylineSDMin(Polyline polyline, double compressionLevel)
        {
            polyline = (Polyline)polyline.Clone();
            polyline.ReduceSegments(PlanimetryAlgorithms.Tolerance);

            //are counting vertex weights
            List<SDMinVertex> weightedVertices = getWeightedVertices(polyline);

            // find the points of intersection
            KDTree crossPointIndex = getCrossPointsIndex(polyline);

            // building codes
            KDTree vertexIndex = buildVertexIndex(weightedVertices, polyline);

            List<SDMinVertex> deletedVertices = new List<SDMinVertex>();

            int pointCount = polyline.CoordinateCount;
            int n = 0;

            while (n < weightedVertices.Count &&
                   (double)(pointCount - deletedVertices.Count) / (double)pointCount > compressionLevel)
            {
                SDMinVertex currentVertex = weightedVertices[n];
                if (currentVertex.Deleted || 
                    currentVertex.IsCrossSegmentVertex ||
                    currentVertex.Previous == null || 
                    currentVertex.Next == null)
                {
                    n++;
                    continue;
                }

                if (checkWeightedVertex(polyline, vertexIndex, currentVertex, crossPointIndex))
                {
                    //the top can be removed
                    currentVertex.Previous.Next = currentVertex.Next;
                    currentVertex.Next.Previous = currentVertex.Previous;
                    deletedVertices.Add(currentVertex);
                    vertexIndex.Remove(currentVertex);
                    currentVertex.Deleted = true;
                    n = 0;
                }
                else
                    n++;
            }

            removeVertices(polyline, deletedVertices);

            return polyline;
        }

        private void removeVertices(Polyline polyline, List<SDMinVertex> deletedVertices)
        {
            Comparison<SDMinVertex> comparision = delegate(SDMinVertex vertex1, SDMinVertex vertex2)
            {
                if (vertex1.PathIndex != vertex2.PathIndex)
                    return vertex1.PathIndex > vertex2.PathIndex ? 1 : -1;

                return vertex1.PointIndex > vertex2.PointIndex ? -1 : 1;
            };

            deletedVertices.Sort(comparision);

            for (int i = 0; i < deletedVertices.Count; i++)
                polyline.Paths[deletedVertices[i].PathIndex].Vertices.RemoveAt(deletedVertices[i].PointIndex);
        }

        private bool checkWeightedVertex(Polyline polyline, KDTree vertexIndex, SDMinVertex currentVertex, KDTree crossPointIndex)
        {
            // probably not an internal vertex
            if (currentVertex.Previous == null || currentVertex.Next == null)
                return true;

            // top with infinite weight ("do not remove")
            if (double.IsPositiveInfinity(currentVertex.Weight))
                return true;


            SDMinVertex previous = currentVertex.Previous;
            SDMinVertex next = currentVertex.Next;

            // One of the segments formed by the vertex in question may be one of the intersection points.
            // If so, you can not remove the top, as point of self-intersection, it may be removed.
            Segment s1 = new Segment(pointOfWeightedVertex(polyline, currentVertex),
                                     pointOfWeightedVertex(polyline, previous));
            Segment s2 = new Segment(pointOfWeightedVertex(polyline, currentVertex),
                                     pointOfWeightedVertex(polyline, next));

            List<SDMinCrossPoint> crossPoints = new List<SDMinCrossPoint>();
            crossPointIndex.QueryObjectsInRectangle(s1.GetBoundingRectangle(), crossPoints);
            crossPointIndex.QueryObjectsInRectangle(s2.GetBoundingRectangle(), crossPoints);
            foreach (SDMinCrossPoint point in crossPoints)
            {
                if (PlanimetryAlgorithms.LiesOnSegment(point.Point, s1))
                {
                    currentVertex.IsCrossSegmentVertex = true;
                    currentVertex.Previous.IsCrossSegmentVertex = true;
                    return false;
                }
                if(PlanimetryAlgorithms.LiesOnSegment(point.Point, s2))
                {
                    currentVertex.IsCrossSegmentVertex = true;
                    currentVertex.Next.IsCrossSegmentVertex = true;
                    return false;
                }
            }

            //One of the polyline vertices can belong to a triangle, 
            //the apex of which is considered the top. In this case,
            //the top can not be deleted because will be a new point of self-intersection.
            Polygon triangle = new Polygon(new ICoordinate[] { pointOfWeightedVertex(polyline, previous), 
                                                          pointOfWeightedVertex(polyline, currentVertex),
                                                          pointOfWeightedVertex(polyline, next) });

            List<SDMinVertex> vertices = new List<SDMinVertex>();
            vertexIndex.QueryObjectsInRectangle<SDMinVertex>(triangle.GetBoundingRectangle(), vertices);

            foreach (SDMinVertex vertex in vertices)
            {
                ICoordinate p = pointOfWeightedVertex(polyline, vertex);

                //point should not be the top of the triangle
                if (p.ExactEquals(triangle.Contours[0].Vertices[0]) ||
                   p.ExactEquals(triangle.Contours[0].Vertices[1]) ||
                   p.ExactEquals(triangle.Contours[0].Vertices[2]))
                    continue;

                if (triangle.ContainsPoint(p))
                    return false;
            }

            return true;
        }

        private ICoordinate pointOfWeightedVertex(Polyline polyline, SDMinVertex vertex)
        {
            return polyline.Paths[vertex.PathIndex].Vertices[vertex.PointIndex];
        }

        private Polyline getPolygonBounds(Polygon polygon)
        {
            Polyline result = new Polyline();
            foreach (Contour c in polygon.Contours)
            { 
                LinePath path = new LinePath();
                result.Paths.Add(path);
                foreach (ICoordinate p in c.Vertices)
                    path.Vertices.Add(p);

                if (!c.Vertices[0].ExactEquals(c.Vertices[c.Vertices.Count - 1]))
                    path.Vertices.Add(c.Vertices[0]);
            }

            return result;
        }

        private Polygon getPolygonFromBounds(Polyline polyline)
        {
            Polygon result = new Polygon();
            foreach (LinePath path in polyline.Paths)
            {
                if (path.Vertices.Count <= 2)
                    continue;
                Contour c = new Contour();
                result.Contours.Add(c);
                for (int i = 0; i < path.Vertices.Count - 1; i++ )
                    c.Vertices.Add(path.Vertices[i]);
            }
            return result;
        }

        /// <summary>
        /// Gets or sets a vertex weighting function
        /// <remarks>
        /// Points with the low weights remove first.
        /// </remarks>
        /// </summary>
        public VertexWeightingType VertexWeighting
        {
            get { return _vertexWeighting; }
            set { _vertexWeighting = value; }
        }

        /// <summary>
        /// Raises when VertexWeighting == VertexWeightingType.Custom and weight value needed.
        /// </summary>
        public event EventHandler<SDMinVertexWeightNeededEventArgs> SDMinVertexWeightNeeded = null;

        /// <summary>
        /// Simplifies a geometry using S-DMin method.
        /// </summary>
        /// <param name="geometry">A geometry for simplification</param>
        /// <param name="compressionLevel">A compression level (the ratio of the 
        /// number of coordinates in a simplified geometry to the number of coordinates 
        /// in the original figure).</param>
        /// <returns>Simlpified geometry</returns>
        public IGeometry SymplifySDMin(IGeometry geometry, double compressionLevel)
        {
            if (!(geometry is Polygon) && !(geometry is Polyline))
                throw new NotSupportedException("Simplifications of \"" + geometry.GetType().FullName + "\" is not supported");

            if (geometry is Polyline)
                return simplifyPolylineSDMin((Polyline)geometry, compressionLevel);

            if (geometry is Polygon)
            {
                Polyline temp = getPolygonBounds((Polygon)geometry);
                Polyline pl = simplifyPolylineSDMin((Polyline)temp, compressionLevel);
                return getPolygonFromBounds(pl);
            }

            return null;
        }
    }

    #endif
}