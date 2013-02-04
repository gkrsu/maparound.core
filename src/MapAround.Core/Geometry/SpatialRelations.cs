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
** File: SpatialRelations.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classses and interfaces providing computation of spatial relations
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Provides access to members that determine if a certain spatial 
    /// relationship (such as touching or overlapping) exists between 
    /// two geometries.
    /// <remarks>
    /// Methods compare two geometries and return a boolean indicating 
    /// whether or not the desired relationship exists.  Some relationships 
    /// require that the input geometries be of the same dimension while 
    /// other have more flexible dimensional constraints.  Most of the 
    /// methods are mutually exclusive Clementini operators.
    /// </remarks>
    /// </summary>
    public interface ISpatialRelative : IGeometry
    {
        /// <summary>
        /// Indicates if the two geometries are define the same set of points in the plane.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the equality</param>
        /// <returns>true, if the two geometries are define the same set of points in the plane, false otherwise</returns>
        bool Equals(ISpatialRelative other);

        /// <summary>
        /// Indicates if the two geometries intersect in a geometry of lesser dimension.
        /// Both interiors should intersect exterior of another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the crosses predicate</param>
        /// <returns>true, if the two geometries intersect in a geometry of lesser dimension, false otherwise</returns>
        bool Crosses(ISpatialRelative other);

        /// <summary>
        /// Indicates if the two geometries share no points in common.
        /// The intersection of interiors should be an empty set.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the two geometries share no points in common, false otherwise</returns>
        bool Disjoint(ISpatialRelative other);

        /// <summary>
        /// Indicates if the intersection of the two geometries has the same dimension as one of the input geometries.
        /// Both exterior should intersect interior of other geometry. Implementation should always return false in 
        /// the case of different dimensions.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the intersection of the two geometries has the same dimension as one of the input geometries, false otherwise</returns>
        bool Overlaps(ISpatialRelative other);

        /// <summary>
        /// Indicates if the boundaries of the geometries intersect.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the touches predicate</param>
        /// <returns>true, if the boundaries of the geometries intersect, false otherwise</returns>
        bool Touches(ISpatialRelative other);

        /// <summary>
        /// Indicates if the two geometries share at least one point in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the intersects predicate</param>
        /// <returns>true, if the two geometries share at least one points in common, false otherwise</returns>
        bool Intersects(ISpatialRelative other);

        /// <summary>
        /// Indicates if this geometry is contained (is within) another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the within predicate</param>
        /// <returns>true, if this geometry is contained another geometry, false otherwise</returns>
        bool Within(ISpatialRelative other);

        /// <summary>
        /// Indicates if this geometry contains the other geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the contains predicate</param>
        /// <returns>true, if this geometry contains the other geometry, false otherwise</returns>
        bool Contains(ISpatialRelative other);

        /// <summary>
        /// Indicates if the defined relationship exists.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the defined relation</param>
        /// <param name="template">Template of the intersection matrix that defines relation</param>
        /// <returns>true, if the defined relationship exists, false otherwise</returns>
        bool Relate(ISpatialRelative other, string template);
    }

    /// <summary>
    /// Dimensionally extended intersection matrix.
    /// </summary>
    public class IntersectionMatrix
    {
        /// <summary>
        /// Possible values of an element of intersection matrix.
        /// </summary>
        public enum ElementValue : int
        {
            /// <summary>
            /// Undefined.
            /// </summary>
            Undefined = -2,
            /// <summary>
            /// Empty set.
            /// </summary>
            Empty = -1,
            /// <summary>
            /// Zero-dimensional set.
            /// </summary>
            Zero = 0,
            /// <summary>
            /// Single-dimensional set.
            /// </summary>
            One = 1,
            /// <summary>
            /// Two-dimensional set.
            /// </summary>
            Two = 2
        }

        /// <summary>
        /// Symbolic names of index values in the intersection matrix.
        /// </summary>
        public enum ElementPosition : int
        {
            /// <summary>
            /// Interior.
            /// </summary>
            Internal = 0,
            /// <summary>
            /// Boundary.
            /// </summary>
            Boundary = 1,
            /// <summary>
            /// Exterior.
            /// </summary>
            External = 2
        }

        private enum DimensionPair
        { 
            ZeroZero,
            ZeroOne,
            ZeroTwo,
            OneOne,
            OneTwo,
            TwoTwo
        }

        private DimensionPair _sourceDimensions = DimensionPair.ZeroZero;

        private ElementValue[,] _values = new ElementValue[3, 3];
        private IGeometry _sourceGeometry1 = null;
        private IGeometry _sourceGeometry2 = null;
        private PlanarGraph _graph = null;

        private void getDimensionPair()
        {
            int dim1 = (int)_sourceGeometry1.Dimension;
            int dim2 = (int)_sourceGeometry2.Dimension;
            int minDim = Math.Min(dim1, dim2);
            int maxDim = Math.Max(dim1, dim2);

            if (maxDim == 0)
            {
                _sourceDimensions = DimensionPair.ZeroZero;
            }
            else if (maxDim == 1)
            {
                if (minDim == 0)
                    _sourceDimensions = DimensionPair.ZeroOne;
                else
                    _sourceDimensions = DimensionPair.OneOne;
            }
            else if (maxDim == 2)
            {
                switch (minDim)
                {
                    case 0: _sourceDimensions = DimensionPair.ZeroTwo;
                        break;
                    case 1: _sourceDimensions = DimensionPair.OneTwo;
                        break;
                    case 2: _sourceDimensions = DimensionPair.TwoTwo;
                        break;
                }
            }
        }

        private char elementToChar(int x, int y)
        {
            if (x < 0 || x > 2)
                throw new ArgumentOutOfRangeException("x");

            if (y < 0 || y > 2)
                throw new ArgumentOutOfRangeException("x");

            switch (_values[x, y])
            { 
                case ElementValue.Empty: return 'F';
                case ElementValue.Zero: return '0';
                case ElementValue.One: return '1';
                case ElementValue.Two: return '2';
            }

            return 'U';
        }

        private IGeometry snapGeometryPoints(IGeometry g, ICoordinate snapCenter)
        {
            Polygon p = g as Polygon;
            if (p != null)
                p.SnapVertices(snapCenter, PlanimetryAlgorithms.Tolerance);

            Polyline pl = g as Polyline;
            if (pl != null)
                pl.SnapVertices(snapCenter, PlanimetryAlgorithms.Tolerance);

            MultiPoint mp = g as MultiPoint;
            if (mp != null)
                mp.SnapToGrid(snapCenter, PlanimetryAlgorithms.Tolerance);

            if (g is PointD)
            {
                PointD point = ((PointD)g);
                ICoordinate coord = point.Coordinate;
                PlanimetryAlgorithms.SnapToGrid(ref coord, snapCenter, PlanimetryAlgorithms.Tolerance);
                point.Coordinate = coord;
            }

            return g;
        }

        private void buildGraph()
        {
            try
            {
                _graph = PlanarGraph.Build(_sourceGeometry1, _sourceGeometry2);
            }
            catch(Exception ex)
            {
                if (ex is TopologyException || ex is InvalidOperationException)
                {
                    List<ICoordinate> points = new List<ICoordinate>();
                    foreach (ICoordinate p in _sourceGeometry1.ExtractCoordinates())
                        points.Add(p);
                    foreach (ICoordinate p in _sourceGeometry2.ExtractCoordinates())
                        points.Add(p);

                    ICoordinate center = PlanimetryAlgorithms.GetCentroid(points);

                    _sourceGeometry1 = snapGeometryPoints(_sourceGeometry1, center);
                    _sourceGeometry2 = snapGeometryPoints(_sourceGeometry2,center);

                    _graph = PlanarGraph.BuildWithSnap(_sourceGeometry1, _sourceGeometry2, center);
                }
                else
                    throw;
            }
        }

        private void calculateValue(int x, int y)
        {
            // intersection additions objects always has a dimension equal to two,
            // as objects are always limited to a finite part of the plane
            if (x == 2 && y == 2)
            {
                _values[2, 2] = ElementValue.Two;
                return;
            }

            if (_graph == null)
                buildGraph();

            //the intersection of the interior of objects
            if (x == 0 && y == 0)
            {
                calculteInternalInternal();
                return;
            } // the intersection of the interior of the first object with the second boundary
            else if (x == 0 && y == 1)
            {
                calculateInternalBounds(false);
                return;
            } //the intersection of the interior of the first object with the addition of the second
            else if (x == 0 && y == 2)
            {
                calculateInternalExternal(false);
                return;
            } // crossing the border of the first object to the interior of the second
            else if (x == 1 && y == 0)
            {
                calculateInternalBounds(true);
                return;
            } // border crossing facilities
            else if (x == 1 && y == 1)
            {
                calculateBoundsBounds();
                return;
            } // crossing the border of the first object with the addition of the second
            else if (x == 1 && y == 2)
            {
                calculateBoundsExternal(false);
                return;
            } // intersection of the complement of the first object to the interior of the second
            else if (x == 2 && y == 0)
            {
                calculateInternalExternal(true);
                return;
            } // intersection of the complement of the first object with the boundary of the second
            else if (x == 2 && y == 1)
            {
                calculateBoundsExternal(true);
            } 
        }

        private void markPolygonsOrientation()
        {
            foreach (PlanarGraphNode node in _graph.Nodes)
                node.Layout = PlanarGraphNode.NodeLayout.Unknown;

            foreach (PlanarGraphEdge edge in _graph.Edges)
            {
                edge.IsVisited = false;
                edge.Enabled = edge.Label.UsedByObject1;
            }
            _graph.BuildPolygon(true, false);

            foreach (PlanarGraphNode node in _graph.Nodes)
                node.Layout = PlanarGraphNode.NodeLayout.Unknown;
            foreach (PlanarGraphEdge edge in _graph.Edges)
            {
                edge.IsVisited = false;
                edge.Enabled = edge.Label.UsedByObject2;
            }
            _graph.BuildPolygon(false, true);
        }

        private void calculteInternalInternal()
        {
            bool isFirstGeometryPoint = _sourceGeometry1 is MultiPoint;

            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Undefined;
            switch (_sourceDimensions)
            {
                // ---------------------------- the intersection of the interior of the two points
                case DimensionPair.ZeroZero:

                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;

                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Zero;
                            break;
                        }
                    break;
                // ---------------------------- the intersection of the interior of a point and a polyline
                case DimensionPair.ZeroOne: 
                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2 && node.IncidentEdges.Count % 2 != 1)
                        {
                            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Zero;
                            break;
                        }
                    break;
                // ---------------------------- crossing point, and the interior of the polygon
                case DimensionPair.ZeroTwo: 
                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;

                    Polygon polygon = _sourceGeometry1 is Polygon ? (Polygon)_sourceGeometry1 : (Polygon)_sourceGeometry2;

                    foreach (PlanarGraphNode node in _graph.Nodes)
                    {
                        if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                            //node is used by one of the objects
                            if (isFirstGeometryPoint ^ node.Label.UsedByObject2)
                                //not a polygon. need to check whether it is inside the polygon.
                                if (polygon.ContainsPoint(node.Point))
                                {
                                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Zero;
                                    break;
                                }
                    }
                    break;
                // ---------------------------- the intersection of the interior of polylines
                case DimensionPair.OneOne:
                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;
                    // lines may intersect at a point, then the graph has a node used by both 
                    // lines and does not coincide with their boundaries
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            // node was found, make sure that it does not limit:
                            // at the boundary points of an odd number of edges used line
                            int use1Count = 0;
                            int use2Count = 0;
                            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            {
                                if (edge.Label.UsedByObject1) use1Count++;
                                if (edge.Label.UsedByObject2) use2Count++;
                            }
                            if (use1Count % 2 != 1 && use2Count % 2 != 1)
                            {
                                // polylines intersect at least at
                                this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Zero;
                                break;
                            }
                        }

                    // possible intersection of the one-dimensional,
                    // then they should be shared at least one edge of the graph
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if (edge.Label.UsedByObject1 && edge.Label.UsedByObject2)
                        {
                            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.One;
                            return;
                        }
                    break;
                // ---------------------------- the intersection of the interior of the polyline and polygon
                case DimensionPair.OneTwo: 
                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;
                    polygon = _sourceGeometry1 is Polygon ? (Polygon)_sourceGeometry1 : (Polygon)_sourceGeometry2;
                    bool flag = _sourceGeometry1 is Polygon;

                    // perhaps one-dimensional intersection, then the edge must lie 
                    // within the polygon, and used only the line
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if (((flag && edge.Label.UsedByObject2 && !edge.Label.UsedByObject1) ||
                              (!flag && edge.Label.UsedByObject1 && !edge.Label.UsedByObject2)) && polygon.ContainsPoint(edge.CenterPoint()))
                        {
                            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.One;
                            return;
                        }
                    break;
                // ---------------------------- the intersection of the interior of polygons
                case DimensionPair.TwoTwo: 
                    this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Empty;

                    // possible two-dimensional intersection, in which case at least one edge only
                    // have one ground should lie within the other, or at least one edge of the polygon 
                    // is used both on the same side of the two polygons to mark up orientation 
                    // of the edges of polygons along the bypass
                    markPolygonsOrientation();

                    foreach (PlanarGraphEdge edge in _graph.Edges)
                    {
                        flag = false;
                        if (edge.Label.UsedByObject1 && !edge.Label.UsedByObject2)
                            if (((Polygon)_sourceGeometry2).ContainsPoint(edge.CenterPoint()))
                                flag = true;

                        if (edge.Label.UsedByObject2 && !edge.Label.UsedByObject1)
                            if (((Polygon)_sourceGeometry1).ContainsPoint(edge.CenterPoint()))
                                flag = true;

                        if (edge.Label.UsedByObject1 && edge.Label.UsedByObject2)
                            if (edge.OrientationInObject1 == edge.OrientationInObject2)
                                flag = true;

                        if(flag)
                        {
                            this[ElementPosition.Internal, ElementPosition.Internal] = ElementValue.Two;
                            return;
                        }
                    }
                    break;
            }
        }

        private void calculateBoundsBounds()
        {
            this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Undefined;

            switch (_sourceDimensions)
            {
                // ---------------------------- two border crossing points
                case DimensionPair.ZeroZero:
                    // border points - the empty set, and their intersection is also
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;
                    break;
                // ---------------------------- border crossing points and polylines
                case DimensionPair.ZeroOne:
                    // border points - the empty set, the intersection of the boundary polyline too
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;
                    break;
                // ----------------------------border crossing point and polygon
                case DimensionPair.ZeroTwo:
                    //border points - the empty set, the intersection of the boundary of the landfill, too
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;
                    break;
                // ---------------------------- border crossing polylines
                case DimensionPair.OneOne:
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;

                    // possible boundary lines coincide, in this case in the graph, 
                    // there must exist a node incident to the edges of which are used an odd number of times both lines
                    foreach (PlanarGraphNode node in _graph.Nodes)
                    {
                        int use1count = 0;
                        int use2count = 0;
                        foreach (PlanarGraphEdge edge in node.IncidentEdges)
                        {
                            if (edge.Label.UsedByObject1) use1count++;
                            if (edge.Label.UsedByObject2) use2count++;
                        }
                        if (use1count % 2 == 1 && use2count % 2 == 1)
                        {
                            this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Zero;
                            return;
                        }
                    }
                    break;
                // ---------------------------- border crossing polyline and polygon
                case DimensionPair.OneTwo:
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;

                    // possibly zero-dimensional intersection, in this case in the graph, 
                    // there must exist a node used polygon and polyline, and the number of edges 
                    // incident to a node used polyline, must be odd                   
                    bool flag = _sourceGeometry1 is Polygon;
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            int usedEdgesCount = 0;
                            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            {
                                if (flag && edge.Label.UsedByObject2) usedEdgesCount++;
                                if (!flag && edge.Label.UsedByObject1) usedEdgesCount++;
                            }
                            if (usedEdgesCount % 2 == 1)
                            {
                                this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Zero;
                                return;
                            }
                        }

                    break;
                // ----------------------------intersection polygon boundaries
                case DimensionPair.TwoTwo:
                    this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Empty;

                    // possibly zero-dimensional intersection, in this case in the graph, there must exist a node used by both polygons
                    flag = false;
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.Zero;
                            flag = true;
                            break;
                        }

                    if (!flag) return;

                    // perhaps there is a one-dimensional intersection, in which case the graph must find at least one shared edge
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if (edge.Label.UsedByObject1 && edge.Label.UsedByObject2)
                        {
                            this[ElementPosition.Boundary, ElementPosition.Boundary] = ElementValue.One;
                            return;
                        }
                    break;
            }
        }

        private void calculateInternalBounds(bool inverseArgs)
        {
            ElementPosition xPos, yPos;
            if (inverseArgs)
            {
                xPos = ElementPosition.Boundary;
                yPos = ElementPosition.Internal;
            }
            else
            {
                xPos = ElementPosition.Internal;
                yPos = ElementPosition.Boundary;
            }
            bool isFirstGeometryPoint = _sourceGeometry1 is MultiPoint;
            bool isFirstGeometryPolyline = _sourceGeometry1 is Polyline;

            switch (_sourceDimensions)
            {
                // ----------------------------the interior of the intersection points with the boundary point - always empty
                case DimensionPair.ZeroZero:
                    this[xPos, yPos] = ElementValue.Empty;
                    break;
                // ---------------------------- crossing the border and the interior of the points and polylines
                case DimensionPair.ZeroOne:
                    this[xPos, yPos] = ElementValue.Empty;
                    // crossing the border points - empty
                    if ((isFirstGeometryPoint && inverseArgs) || (!isFirstGeometryPoint && !inverseArgs)) 
                        return;

                    // possibly zero-dimensional intersection, in this case, there must exist a node count of incident edges is odd
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                            if (node.IncidentEdges.Count % 2 == 1)
                            {
                                this[xPos, yPos] = ElementValue.Zero;
                                return;
                            }

                    break;
                // ---------------------------- crossing the border and the interior of the point and polygon
                case DimensionPair.ZeroTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    // crossing the border points - empty
                    if ((isFirstGeometryPoint && inverseArgs) || (!isFirstGeometryPoint && !inverseArgs))
                        return;

                    // possibly zero-dimensional intersection, in this case, there must exist a shared graph node
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            this[xPos, yPos] = ElementValue.Zero;
                            return;
                        }
                    break;
                // ---------------------------- crossing the border and the interior of the polyline
                case DimensionPair.OneOne:
                    this[xPos, yPos] = ElementValue.Empty;
                    // possibly zero-dimensional intersection, in this case in the graph, 
                    // there must exist a node for which the number of incident edges,
                    // used a polyline - odd, the other used a polyline - even
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                        {
                            int usageCount1 = 0;
                            int usageCount2 = 0;
                            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            {
                                if (edge.Label.UsedByObject1) usageCount1++;
                                if (edge.Label.UsedByObject2) usageCount2++;
                            }
                            if ((!inverseArgs && usageCount1 % 2 == 0 && usageCount2 % 2 == 1) ||
                               (inverseArgs && usageCount1 % 2 == 1 && usageCount2 % 2 == 0))
                            {
                                this[xPos, yPos] = ElementValue.Zero;
                                return;
                            }
                        }

                    break;
                // ---------------------------- crossing the border and the interior of the polyline and polygon
                case DimensionPair.OneTwo:
                    this[xPos, yPos] = ElementValue.Empty;

                    Polygon polygon = isFirstGeometryPolyline ? (Polygon)_sourceGeometry2 : (Polygon)_sourceGeometry1;
                    if ((isFirstGeometryPolyline && inverseArgs) ||
                        (!isFirstGeometryPolyline && !inverseArgs))
                    {
                        // boundary polyline must be inside the polygon
                        foreach (PlanarGraphNode node in _graph.Nodes)
                            if ((isFirstGeometryPolyline && node.Label.UsedByObject1 && !node.Label.UsedByObject2) ||
                                (!isFirstGeometryPolyline && node.Label.UsedByObject2 && !node.Label.UsedByObject1))
                            {
                                int count = 0;
                                foreach (PlanarGraphEdge edge in node.IncidentEdges)
                                {
                                    if (inverseArgs && edge.Label.UsedByObject1) count++;
                                    if (!inverseArgs && edge.Label.UsedByObject2) count++;
                                }

                                if (count % 2 == 1 && polygon.ContainsPoint(node.Point))
                                {
                                    this[xPos, yPos] = ElementValue.Zero;
                                    return;
                                }
                            }
                    }
                    else
                    {
                        bool flag = false;
                        // boundary of the polygon can intersect the interior of the polyline at
                        foreach (PlanarGraphNode node in _graph.Nodes)
                            if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                            {
                                int count = 0;
                                foreach (PlanarGraphEdge edge in node.IncidentEdges)
                                {
                                    if (isFirstGeometryPolyline && edge.Label.UsedByObject1) count++;
                                    if (!isFirstGeometryPolyline && edge.Label.UsedByObject2) count++;
                                }

                                if (count % 2 == 0)
                                {
                                    this[xPos, yPos] = ElementValue.Zero;
                                    flag = true;
                                    break;
                                }
                            }

                        if (!flag) return;

                        // perhaps also one-dimensional intersection, in which 
                        // case the graph must find at least one shared edge
                        foreach (PlanarGraphEdge edge in _graph.Edges)
                            if (edge.Label.UsedByObject1 && edge.Label.UsedByObject2)
                            {
                                this[xPos, yPos] = ElementValue.One;
                                return;
                            }
                    }
                    break;
                // ---------------------------- crossing the border and the interior of the polygon
                case DimensionPair.TwoTwo:
                    Polygon p1 = (Polygon)_sourceGeometry1;
                    Polygon p2 = (Polygon)_sourceGeometry2;
                    this[xPos, yPos] = ElementValue.Empty;

                    // perhaps one-dimensional intersection, in which case the
                    // graph must be some edge, used by one of the sites and are inside other
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if (edge.Label.UsedByObject1 ^ edge.Label.UsedByObject2)
                            if ((inverseArgs && edge.Label.UsedByObject1 && p2.ContainsPoint(edge.CenterPoint())) ||
                                (!inverseArgs && edge.Label.UsedByObject2 && p1.ContainsPoint(edge.CenterPoint())))
                            {
                                this[xPos, yPos] = ElementValue.One;
                                return;
                            }
                    break;
            }
        }

        private void calculateInternalExternal(bool inverseArgs)
        {
            ElementPosition xPos, yPos;
            if (inverseArgs)
            {
                xPos = ElementPosition.External;
                yPos = ElementPosition.Internal;
            }
            else
            {
                xPos = ElementPosition.Internal;
                yPos = ElementPosition.External;
            }
            bool isFirstGeometryPoint = _sourceGeometry1 is MultiPoint;
            bool isFirstGeometryPolyline = _sourceGeometry1 is Polyline;

            switch (_sourceDimensions)
            {
                // ---------------------------- the interior of the intersection point with the addition of a point
                case DimensionPair.ZeroZero:
                    this[xPos, yPos] = ElementValue.Empty;

                    //if (!((PointD)_sourceGeometry1).Equals((PointD)_sourceGeometry2))
                    //    this[xPos, yPos] = ElementValue.Zero;

                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                            if (node.Label.UsedByObject1 ^ inverseArgs)
                            {
                                this[xPos, yPos] = ElementValue.Zero;
                                return;
                            }
                    break;
                // ----------------------------crossing the inner region and add points and polylines
                case DimensionPair.ZeroOne:
                    this[xPos, yPos] = ElementValue.Empty;

                    if ((isFirstGeometryPolyline && !inverseArgs) ||
                        (!isFirstGeometryPolyline && inverseArgs))
                    {
                        // intersection point with the additions interior of the polyline - dimensional (not in the degenerate case)
                        if (_graph.Edges.Count > 0)
                            this[xPos, yPos] = ElementValue.One;
                    }
                    else
                    {
                        // intersection of the complement to the interior of the polyline point, perhaps a zero-dimensional intersection
                        foreach (PlanarGraphNode node in _graph.Nodes)
                            if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                                if ((isFirstGeometryPoint && node.Label.UsedByObject1) ||
                                    (!isFirstGeometryPoint && node.Label.UsedByObject2))
                                {
                                    this[xPos, yPos] = ElementValue.Zero;
                                    return;
                                }
                    }
                    break;
                // ---------------------------- crossing the inner region and add a point and polygon
                case DimensionPair.ZeroTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    if ((inverseArgs && isFirstGeometryPoint) ||
                       (!inverseArgs && !isFirstGeometryPoint))
                    {
                        // addition point is always two-dimensional intersection with the inner region of the landfill
                        if(_graph.Edges.Count > 0)
                            this[xPos, yPos] = ElementValue.Two;
                    }
                    else
                    {
                        Polygon polygon = isFirstGeometryPoint ? (Polygon)_sourceGeometry2 : (Polygon)_sourceGeometry1;

                        // addition, the landfill has a zero-dimensional intersection with the interior of the point, 
                        // if the point does not lie within the polygon
                        foreach (PlanarGraphNode node in _graph.Nodes)
                        {
                            if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                                if (isFirstGeometryPoint ^ node.Label.UsedByObject2)
                                {
                                    // node is not shared, if it lies outside the range - we have a zero-dimensional intersection
                                    if (!polygon.ContainsPoint(node.Point))
                                    {
                                        this[xPos, yPos] = ElementValue.Zero;
                                        return;
                                    }
                                    return;
                                }
                        }
                    }
                    break;
                // ---------------------------- crossing the inner region and add polylines
                case DimensionPair.OneOne:
                    this[xPos, yPos] = ElementValue.Empty;
                    //perhaps one-dimensional intersection, in which case there must be an edge is used by only one line
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if (edge.Label.UsedByObject1 ^ edge.Label.UsedByObject2)
                            if ((!inverseArgs && edge.Label.UsedByObject1) ||
                               (inverseArgs && edge.Label.UsedByObject2))
                            {
                                this[xPos, yPos] = ElementValue.One;
                                return;
                            }
                    break;
                // ---------------------------- crossing the inner region and add polylines and polygons
                case DimensionPair.OneTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    Polygon p1 = isFirstGeometryPolyline ? (Polygon)_sourceGeometry2 : (Polygon)_sourceGeometry1;
                    if ((inverseArgs && isFirstGeometryPolyline) ||
                        (!inverseArgs && !isFirstGeometryPolyline))
                    {
                        // intersection of the complement to the interior of the polyline is two-dimensional polygon
                        this[xPos, yPos] = ElementValue.Two;
                        return;
                    }
                    else
                    {
                        //perhaps one-dimensional intersection polygon additions to the interior of the polyline,
                        // in this case there must be an edge that belongs only polylines, and does not lie within the polygon
                        foreach (PlanarGraphEdge edge in _graph.Edges)
                            if (edge.Label.UsedByObject1 ^ edge.Label.UsedByObject2)
                                if ((!inverseArgs && edge.Label.UsedByObject1) ||
                                    (inverseArgs && edge.Label.UsedByObject2))
                                    if (!p1.ContainsPoint(edge.CenterPoint()))
                                    {
                                        this[xPos, yPos] = ElementValue.One;
                                        return;
                                    }
                    }
                    break;
                // ---------------------------- the intersection of the interior of the polygon and Supplies
                case DimensionPair.TwoTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    // possible two-dimensional intersection, if there is at least one edge is used 
                    // first ground does not lie within the other, or there is an edge is used only
                    // in the second polygon lies inside the first, or there is an edge used
                    // by both polygons so that they lie on opposite sides of it
                    Polygon polygon1 = (Polygon)_sourceGeometry1;
                    Polygon polygon2 = (Polygon)_sourceGeometry2;
                    bool _orientationMarked = false;

                    foreach (PlanarGraphEdge edge in _graph.Edges)
                    {
                        if (edge.Label.UsedByObject1 ^ edge.Label.UsedByObject2)
                        {
                            if ((inverseArgs && edge.Label.UsedByObject2 && !polygon1.ContainsPoint(edge.CenterPoint())) ||
                               (!inverseArgs && edge.Label.UsedByObject1 && !polygon2.ContainsPoint(edge.CenterPoint())))
                            {
                                this[xPos, yPos] = ElementValue.Two;
                                return;
                            }

                            if ((inverseArgs && edge.Label.UsedByObject1 && polygon2.ContainsPoint(edge.CenterPoint())) ||
                               (!inverseArgs && edge.Label.UsedByObject2 && polygon1.ContainsPoint(edge.CenterPoint())))

                            {
                                this[xPos, yPos] = ElementValue.Two;
                                return;
                            }
                        }
                        else
                            if (edge.Label.UsedByObject1 && edge.Label.UsedByObject2)
                            {
                                // possible orientations of the layout do not have to, if the intersection is calculated by other edges
                                if (_orientationMarked)
                                {
                                    markPolygonsOrientation();
                                    _orientationMarked = true;
                                }
                                if (edge.OrientationInObject1 != edge.OrientationInObject2)
                                {
                                    this[xPos, yPos] = ElementValue.Two;
                                    return;
                                }
                            }
                    }
                    break;
            }

        }

        private void calculateBoundsExternal(bool inverseArgs)
        {
            ElementPosition xPos, yPos;
            if (inverseArgs)
            {
                xPos = ElementPosition.External;
                yPos = ElementPosition.Boundary;
            }
            else
            {
                xPos = ElementPosition.Boundary;
                yPos = ElementPosition.External;
            }
            bool isFirstGeometryPoint = _sourceGeometry1 is MultiPoint;
            bool isFirstGeometryPolyline = _sourceGeometry1 is Polyline;

            switch (_sourceDimensions)
            {
                // ---------------------------- border crossing point with the addition of a point
                case DimensionPair.ZeroZero:
                    // always empty
                    this[xPos, yPos] = ElementValue.Empty;
                    break;
                // ---------------------------- crossing the border and add points and polylines
                case DimensionPair.ZeroOne:
                    this[xPos, yPos] = ElementValue.Empty;
                    if ((inverseArgs && !isFirstGeometryPoint) ||
                        (!inverseArgs && isFirstGeometryPoint))
                        // always empty
                        this[xPos, yPos] = ElementValue.Empty;
                    else
                    {
                        foreach (PlanarGraphNode node in _graph.Nodes)
                            if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                                if (node.IncidentEdges.Count % 2 == 1)
                                {
                                    this[xPos, yPos] = ElementValue.Zero;
                                    return;
                                }
                    }
                    break;
                // ---------------------------- crossing the border and add a point and polygon
                case DimensionPair.ZeroTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    if ((inverseArgs && isFirstGeometryPoint) ||
                        (!inverseArgs && !isFirstGeometryPoint))
                    {
                        // one-dimensional intersection in the nondegenerate case
                        if (_graph.Edges.Count > 0)
                            this[xPos, yPos] = ElementValue.One;
                    }
                    // otherwise empty
                    break;
                // ---------------------------- crossing the border and add polylines
                case DimensionPair.OneOne:
                    this[xPos, yPos] = ElementValue.Empty;
                    // possibly zero-dimensional intersection, in this case in the graph, 
                    // there must exist a node that is used only a polyline with an odd number of edges incident
                    foreach (PlanarGraphNode node in _graph.Nodes)
                        if ((!inverseArgs && node.Label.UsedByObject1 && !node.Label.UsedByObject2) ||
                            (inverseArgs && node.Label.UsedByObject2 && !node.Label.UsedByObject1))
                            if (node.IncidentEdges.Count % 2 == 1)
                            {
                                this[xPos, yPos] = ElementValue.Zero;
                                return;
                            }
                    break;
                // ---------------------------- crossing the border and add polyline and polygon
                case DimensionPair.OneTwo:
                    this[xPos, yPos] = ElementValue.Empty;
                    
                    if ((inverseArgs && isFirstGeometryPolyline) ||
                        (!inverseArgs && !isFirstGeometryPolyline))
                    {
                        //perhaps one-dimensional intersection, if there is at least one edge ispolzeumoe only ground
                        foreach (PlanarGraphEdge edge in _graph.Edges)
                            if ((isFirstGeometryPolyline && edge.Label.UsedByObject2 && !edge.Label.UsedByObject1) ||
                               (!isFirstGeometryPolyline && edge.Label.UsedByObject1 && !edge.Label.UsedByObject2))
                            {
                                this[xPos, yPos] = ElementValue.One;
                                return;
                            }
                    }
                    else
                    {
                        //possibly zero-dimensional intersection, if there is at least one node, 
                        // use only the line that has an odd number of incident edges and lying within the polygon
                        Polygon p1 = isFirstGeometryPolyline ? (Polygon)_sourceGeometry2 : (Polygon)_sourceGeometry1;
                        foreach (PlanarGraphNode node in _graph.Nodes)
                            if ((isFirstGeometryPolyline && node.Label.UsedByObject1 && !node.Label.UsedByObject2) ||
                               (!isFirstGeometryPolyline && node.Label.UsedByObject2 && !node.Label.UsedByObject1))
                                if(node.IncidentEdges.Count % 2 == 1 && !p1.ContainsPoint(node.Point))
                                {
                                    this[xPos, yPos] = ElementValue.Zero;
                                    return;
                                }
                    }
                    break;
                // ---------------------------- crossing the border and add polygons
                case DimensionPair.TwoTwo:
                    this[xPos, yPos] = ElementValue.Empty;

                    Polygon polygon1 = (Polygon)_sourceGeometry1; 
                    Polygon polygon2 = (Polygon)_sourceGeometry2;

                    // perhaps one-dimensional intersection, if there is at least one edge 
                    // belongs to only one polygon and not contained within another
                    foreach (PlanarGraphEdge edge in _graph.Edges)
                        if ((!inverseArgs && edge.Label.UsedByObject1 && !edge.Label.UsedByObject2 && !polygon2.ContainsPoint(edge.CenterPoint())) ||
                            (inverseArgs && edge.Label.UsedByObject2 && !edge.Label.UsedByObject1 && !polygon1.ContainsPoint(edge.CenterPoint())))
                        {
                            this[xPos, yPos] = ElementValue.One;
                            return;
                        }
                    break;
            }
        }

        private void calculateValues()
        {
            //if (_sourceGeometry1.GetBoundingRectangle().Intersect(_sourceGeometry2.GetBoundingRectangle()))
            //{
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        calculateValue(i, j);
            //}
            //else
            //    calculateDisjointMatrix();
        }

        private void calculateValuesPartial(string template)
        {
            //if (_sourceGeometry1.GetBoundingRectangle().Intersect(_sourceGeometry2.GetBoundingRectangle()))
            //{
            int pos = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    if(template[pos] != '*')
                        calculateValue(i, j);
                    pos++;
                }
            //}
            //else
            //    calculateDisjointMatrix();
        }

        private void checkGeometry(IGeometry geometry)
        {
            if (!(geometry is PointD) && !(geometry is Polyline) && !(geometry is Polygon) && !(geometry is MultiPoint))
                throw new NotSupportedException(string.Format("Geometry \"{0}\" is not supported",
                                                geometry.GetType().FullName));
        }

        private void reduceGeometrySegments(IGeometry g, double minLength)
        {
            Polygon p = g as Polygon;
            if (p != null)
                p.ReduceSegments(minLength);

            Polyline pl = g as Polyline;
            if (pl != null)
                pl.ReduceSegments(minLength);
        }

        private void calculateDisjointMatrix()
        {
            throw new NotImplementedException();
        }

        private void initSourceGeometries(IGeometry geometry1, IGeometry geometry2)
        {
            // transform point-to-multipoint
            if (geometry1 is PointD)
            {
                PointD p = (PointD)geometry1.Clone();
                geometry1 = new MultiPoint(new ICoordinate[] { p.Coordinate });
            }

            if (geometry2 is PointD)
            {
                PointD p = (PointD)geometry2.Clone();
                geometry2 = new MultiPoint(new ICoordinate[] { p.Coordinate });
            }

            _sourceGeometry1 = (IGeometry)geometry1.Clone();
            _sourceGeometry2 = (IGeometry)geometry2.Clone();

            reduceGeometrySegments(_sourceGeometry1, PlanimetryAlgorithms.Tolerance * 1.42);
            reduceGeometrySegments(_sourceGeometry2, PlanimetryAlgorithms.Tolerance * 1.42);
        }

        /// <summary>
        /// Indicates whether this matrix matches a specified template.
        /// </summary>
        /// <param name="template">String template of matrix</param>
        /// <returns>true, if this matrix matches a specified template, false otherwise</returns>
        public bool Matches(string template)
        {
            if (template.Length != 9)
                throw new ArgumentException("Intersection matrix template should be 9-character string", "template");

            char[] templateChars = template.ToCharArray();

            int i = 0;
            int j = 0;
            foreach (char ch in templateChars)
            {
                switch (ch)
                { 
                    case '*':break;
                    case 'F':
                        if (_values[j, i] != ElementValue.Empty)
                            return false;
                        break;
                    case 'T': 
                        if (_values[j, i] == ElementValue.Empty ||
                            _values[j, i] == ElementValue.Undefined)
                            return false;
                        break;
                    case '0':
                        if (_values[j, i] != ElementValue.Zero)
                            return false;
                        break;
                    case '1': 
                        if (_values[j, i] != ElementValue.One)
                            return false;
                        break;
                    case '2':
                        if (_values[j, i] != ElementValue.Two)
                            return false;
                        break;
                }

                i++;
                if (i == 3)
                {
                    j++;
                    i = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Represents an element of this intersection matrix
        /// </summary>
        /// <param name="x">Row number</param>
        /// <param name="y">Column number</param>
        /// <returns></returns>
        public ElementValue this[ElementPosition x, ElementPosition y]
        {
            get 
            {
                return _values[(int)x, (int)y];
            }
            set 
            {
                _values[(int)x, (int)y] = value;
            }
        }

        /// <summary>
        /// Converts this instance to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this instance</returns>
        public override string ToString()
        {
            string result = string.Empty;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    result += elementToChar(i, j);

            return result;
        }

        /// <summary>
        /// Calculates a specified elements of intersection matrix 
        /// for two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <param name="template">String template of intersection matrix
        /// Will be calculated all the elements that do not correspond to the symbol '*'.</param>
        public void CalculatePartial(IGeometry geometry1, IGeometry geometry2, string template)
        {
            checkGeometry(geometry1);
            checkGeometry(geometry2);

            if(template.Length != 9)
                throw new ArgumentException("Intersection matrix template should be 9-character string", "template");

            _graph = null;

            initSourceGeometries(geometry1, geometry2);

            getDimensionPair();

            calculateValuesPartial(template);
        }

        /// <summary>
        /// Calculates an intersection matrix for two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        public void Calculate(IGeometry geometry1, IGeometry geometry2)
        {
            checkGeometry(geometry1);
            checkGeometry(geometry2);

            _graph = null;

            initSourceGeometries(geometry1, geometry2);

            getDimensionPair();

            calculateValues();
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.IntersectionMatrix
        /// </summary>
        public IntersectionMatrix()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    _values[i, j] = ElementValue.Undefined;
        }
    }
}