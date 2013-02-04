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
** File: Overlays.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Overlay calculator
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Represents an overlay type .
    /// </summary>
    public enum OverlayType
    {
        /// <summary>
        /// Union.
        /// </summary>
        Union,
        /// <summary>
        /// Intersection.
        /// </summary>
        Intersection,
        /// <summary>
        /// Difference.
        /// </summary>
        Difference,
        /// <summary>
        /// Symmetric difference.
        /// </summary>
        SymmetricDifference
    }

    /// <summary>
    /// Overlay calculator.
    /// <remarks>
    /// Instances of this class are used to calculate the results of Boolean operations 
    /// (union, intersection, difference, symmetric difference) over the point sets. 
    /// Point sets can be defined as a polygons, polylines or points.
    /// </remarks>
    /// </summary>
    public class OverlayCalculator
    {
        private IGeometry _geometry1;
        private IGeometry _geometry2;

        private ICoordinate _translationCenter;

        private List<Contour> _polygon1Contours;
        private List<Contour> _polygon2Contours;

        /// <summary>
        /// Calculates a union of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>A union of two geometries</returns>
        public GeometryCollection Union(IGeometry geometry1, IGeometry geometry2)
        {
            return CalculateOverlay(geometry1, geometry2, OverlayType.Union);
        }

        /// <summary>
        /// Calculates an intersection of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>An intersection of two geometries</returns>
        public GeometryCollection Intersection(IGeometry geometry1, IGeometry geometry2)
        {
            return CalculateOverlay(geometry1, geometry2, OverlayType.Intersection);
        }

        /// <summary>
        /// Calculates a difference of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>A difference of two geometries</returns>
        public GeometryCollection Difference(IGeometry geometry1, IGeometry geometry2)
        {
            return CalculateOverlay(geometry1, geometry2, OverlayType.Difference);
        }

        /// <summary>
        /// Calculates a symmetric difference of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>A symmetric difference of two geometries</returns>
        public GeometryCollection SymmetricDifference(IGeometry geometry1, IGeometry geometry2)
        {
            return CalculateOverlay(geometry1, geometry2, OverlayType.SymmetricDifference);
        }

        /// <summary>
        /// Calculates an overlay of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <param name="operation">Overlay type</param>
        /// <returns>An overlay of two geometries</returns>
        public GeometryCollection CalculateOverlay(IGeometry geometry1, IGeometry geometry2, OverlayType operation)
        {
            if (!(geometry1 is Polygon))
                if (!(geometry1 is Polyline))
                    if (!(geometry1 is PointD))
                        if (!(geometry1 is MultiPoint))
                            throw new NotSupportedException(string.Format("Overlay calculation for \"{0}\" is not supported", geometry1.GetType().FullName));

            if (!(geometry2 is Polygon))
                if (!(geometry2 is Polyline))
                    if (!(geometry2 is PointD))
                        if (!(geometry2 is MultiPoint))
                            throw new NotSupportedException(string.Format("Overlay calculation for \"{0}\" is not supported", geometry2.GetType().FullName));

            return calculateOverlay(geometry1, geometry2, operation, false);
        }

        /// <summary>
        /// Computes the contours of the topologically normalized polygon.
        /// </summary>
        /// <remarks>
        /// Contours that are not intersected. 
        /// Orientations are set clockwise for holes and 
        /// counter clockwise for shells.
        /// Sections of the boundary repeated an even number of times will be removed.
        /// </remarks>
        /// <param name="contours">A list containing initial contours</param>
        /// <returns>A list containing contours of the topologically normalized polygon</returns>
        public List<Contour> SimplifyContours(IList<Contour> contours)
        {
            return simplifyContours(contours, false);
        }

        /// <summary>
        /// Computes a list of line paths of the topologically normalized polyline.
        /// </summary>
        /// <summary>
        /// These line paths do not contain intersections.
        /// Allowed only the mutual and self-touches at the start and end points.
        /// </summary>
        /// <param name="paths">A list containing initial line paths</param>
        /// <returns>A list of line paths of the topologically normalized polyline</returns>
        public List<LinePath> SimplifyLinePaths(IList<LinePath> paths)
        {
            return simplifyLinePaths(paths, false);
        }

        private GeometryCollection calculateNonIntersectedObjectsOverlay(IGeometry geometry1, IGeometry geometry2, OverlayType operation, bool performSnapping)
        {
            GeometryCollection result = new GeometryCollection();

            // zero crossing
            if (operation == OverlayType.Intersection)
                return result;

            // difference is equal to the first object
            if (operation == OverlayType.Difference)
            {
                result.Add((IGeometry)geometry1.Clone());
                return result;
            }

            // symmetric difference and the union concatenating
            try
            {
                if (geometry1 is Polygon && geometry2 is Polygon)
                {
                    _polygon1Contours = SimplifyContours(((Polygon)geometry1).Contours);
                    _polygon2Contours = SimplifyContours(((Polygon)geometry2).Contours);

                    Polygon p = new Polygon();
                    foreach (Contour c in _polygon1Contours)
                        p.Contours.Add(c);

                    foreach (Contour c in _polygon2Contours)
                        p.Contours.Add(c);

                    result.Add(p);
                }
                else if (geometry1 is Polyline && geometry2 is Polyline)
                {
                    Polyline p = new Polyline();

                    foreach (LinePath path in ((Polyline)geometry1).Paths)
                        p.Paths.Add(path);

                    foreach (LinePath path in ((Polyline)geometry2).Paths)
                        p.Paths.Add(path);

                    result.Add(p);
                }
                else
                {
                    // If a figure is still a proving ground to normalize topology
                    if (geometry1 is Polygon)
                    {
                        geometry1 = (IGeometry)((Polygon)geometry1).Clone();
                        ((Polygon)geometry1).Simplify();
                    }

                    if (geometry2 is Polygon)
                    {
                        geometry2 = (IGeometry)((Polygon)geometry2).Clone();
                        ((Polygon)geometry2).Simplify();
                    }

                    result.Add(geometry1);
                    result.Add(geometry2);
                }

                return result;
            }
            finally
            {
                _polygon1Contours = null;
                _polygon2Contours = null;
            }
        }

        private void addPoints(PlanarGraph graph, OverlayType operation, Polygon p1, Polygon p2, Collection<IGeometry> collection)
        { 
            foreach (PlanarGraphNode node in graph.Nodes)
                node.Enabled = isNodeEnabled(node, operation, p1, p2);

            List<PointD> points = graph.BuildPoints();

            foreach (PointD p in points)
                collection.Add(p);
        }

        private void addPolyline(PlanarGraph graph, OverlayType operation, Polygon p1, Polygon p2, Collection<IGeometry> collection)
        {
            foreach (PlanarGraphEdge edge in graph.Edges)
            {
                edge.IsVisited = false;
                edge.Enabled = isLinearEdgeEnabled(edge, operation, p1, p2);
            }
            Polyline polyline = graph.BuildPolyline(false, false);
            if (polyline.CoordinateCount > 0)
                collection.Add(polyline);
        }

        private void addPolygon(PlanarGraph graph, OverlayType operation, Polygon p1, Polygon p2, Collection<IGeometry> collection)
        {
            foreach (PlanarGraphEdge edge in graph.Edges)
            {
                edge.IsVisited = false;
                edge.Enabled = isAreaEdgeEnabled(edge, operation, p1, p2);
            }
            foreach (PlanarGraphNode node in graph.Nodes)
                node.Layout = PlanarGraphNode.NodeLayout.Unknown;
            Polygon polygon = graph.BuildPolygon(false, false);
            if (polygon.CoordinateCount > 0)
                collection.Add(polygon);
        }

        #region Calculation overlays homogeneous objects

        //private void getPointPointOverlay(PointD p1, PointD p2, OverlayType operation, ICollection<IGeometry> result)
        //{
        //    bool isEqual = p1.Equals(p2);
        //    switch (operation)
        //    {
        //        case OverlayType.Intersection:
        //            if (isEqual)
        //                result.Add((IGeometry)p1.Clone());
        //            break;
        //        case OverlayType.Union:
        //            result.Add((IGeometry)p1.Clone());
        //            if (!isEqual)
        //                result.Add((IGeometry)p2.Clone());
        //            break;
        //        case OverlayType.Difference:
        //            if (!isEqual)
        //                result.Add((IGeometry)p1.Clone());
        //            break;
        //        case OverlayType.SymmetricDifference:
        //            if (!isEqual)
        //            {
        //                result.Add((IGeometry)p1.Clone());
        //                result.Add((IGeometry)p2.Clone());
        //            }
        //            break;
        //    }
        //}

        private void getPointPointOverlay(MultiPoint mp1, MultiPoint mp2, OverlayType operation, ICollection<IGeometry> result)
        {
            PlanarGraph graph = PlanarGraph.Build(mp1, mp2);

            switch (operation)
            {
                case OverlayType.Intersection:
                    foreach (PlanarGraphNode node in graph.Nodes)
                        if (node.Label.UsedByObject1 && node.Label.UsedByObject2)
                            result.Add(new PointD(node.Point));
                    break;
                case OverlayType.Union:
                    foreach (PlanarGraphNode node in graph.Nodes)
                        result.Add(new PointD(node.Point));
                    break;
                case OverlayType.Difference:
                    foreach (PlanarGraphNode node in graph.Nodes)
                        if (node.Label.UsedByObject1 && !node.Label.UsedByObject2)
                            result.Add(new PointD(node.Point));
                    break;
                case OverlayType.SymmetricDifference:
                    foreach (PlanarGraphNode node in graph.Nodes)
                        if (node.Label.UsedByObject1 ^ node.Label.UsedByObject2)
                            result.Add(new PointD(node.Point));
                    break;
            }
        }

        private void getPolygonPolygonOverlay(Polygon polygon1, Polygon polygon2, OverlayType operation, GeometryCollection result, bool performSnapping)
        {
            try
            {
                _geometry1 = polygon1;
                _geometry2 = polygon2;
                bool isValid = true;

                try
                {
                    init(performSnapping);

                    PlanarGraph graph = PlanarGraph.Build(_geometry1, _geometry2);

                    // build the first facility
                    foreach (PlanarGraphNode node in graph.Nodes)
                        node.Layout = PlanarGraphNode.NodeLayout.Unknown;

                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject1;
                    }
                    Polygon p1 = graph.BuildPolygon(true, false);

                    // building a second facility
                    foreach (PlanarGraphNode node in graph.Nodes)
                        node.Layout = PlanarGraphNode.NodeLayout.Unknown;

                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject2;
                    }
                    Polygon p2 = graph.BuildPolygon(false, true);

                    // build results:
                    // point
                    if (operation == OverlayType.Intersection)
                        addPoints(graph, operation, p1, p2, result);

                    // polyline
                    if (operation == OverlayType.Intersection)
                        addPolyline(graph, operation, p1, p2, result);

                    // ground
                    addPolygon(graph, operation, p1, p2, result);

                    for (int i = 0; i < result.Count; i++)
                    {
                        IGeometry g = translateGeometry(result[i], _translationCenter.X, _translationCenter.Y);
                        if (g is PointD)
                            result[i] = g;
                    }
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }

                if (isValid)
                    return;
                else
                {
                    // overlay has not been calculated.
                    // it may be possible to calculate the overlay aligned to the grid
                    getPolygonPolygonOverlay(polygon1, polygon2, operation, result, true);
                    return;
                }
            }
            finally
            {
                _geometry1 = null;
                _geometry2 = null;
            }
        }

        private void getPolylinePolylineOverlay(Polyline polyline1, Polyline polyline2, OverlayType operation, GeometryCollection result, bool performSnapping)
        {
            try
            {
                _geometry1 = polyline1;
                _geometry2 = polyline2;
                bool isValid = true;

                try
                {
                    init(performSnapping);

                    PlanarGraph graph = PlanarGraph.Build(_geometry1, _geometry2);

                    // classify edges and nodes
                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        switch (operation)
                        {
                            case OverlayType.Intersection:
                                edge.Enabled = edge.Label.UsedByObject1 && edge.Label.UsedByObject2;
                                break;
                            case OverlayType.Union:
                                edge.Enabled = edge.Label.UsedByObject1 || edge.Label.UsedByObject2;
                                break;
                            case OverlayType.Difference:
                                edge.Enabled = edge.Label.UsedByObject1 && !edge.Label.UsedByObject2;
                                break;
                            case OverlayType.SymmetricDifference:
                                edge.Enabled = edge.Label.UsedByObject1 ^ edge.Label.UsedByObject2;
                                break;
                        }
                    }

                    foreach (PlanarGraphNode node in graph.Nodes)
                    {
                        bool hasEnabledEdges = false;
                        foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            if (edge.Enabled)
                            {
                                hasEnabledEdges = true;
                                break;
                            }
                        if (hasEnabledEdges)
                            node.Enabled = false;
                        else
                        {
                            switch (operation)
                            {
                                case OverlayType.Intersection:
                                    node.Enabled = node.Label.UsedByObject1 && node.Label.UsedByObject2;
                                    break;
                                case OverlayType.Union:
                                    node.Enabled = node.Label.UsedByObject1 || node.Label.UsedByObject2;
                                    break;
                                case OverlayType.Difference:
                                    node.Enabled = node.Label.UsedByObject1 && !node.Label.UsedByObject2;
                                    break;
                                case OverlayType.SymmetricDifference:
                                    node.Enabled = node.Label.UsedByObject1 ^ node.Label.UsedByObject2;
                                    break;
                            }
                        }
                    }

                    //build results:
                    // point
                    List<PointD> points = graph.BuildPoints();

                    foreach (PointD p in points)
                        result.Add(p);

                    // polyline
                    Polyline polyline = graph.BuildPolyline(false, false);

                    if (polyline.CoordinateCount > 0)
                        result.Add(polyline);

                    for (int i = 0; i < result.Count; i++)
                    {
                        IGeometry g = translateGeometry(result[i], _translationCenter.X, _translationCenter.Y);
                        if (g is PointD)
                            result[i] = g;
                    }
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }

                if (isValid)
                    return;
                else
                {
                    // overlay has not been calculated.
                    // it may be possible to calculate the overlay aligned to the grid
                    getPolylinePolylineOverlay(polyline1, polyline2, operation, result, true);
                    return;
                }
            }
            finally
            {
                _geometry1 = null;
                _geometry2 = null;
            }
        }

        #endregion

        #region Calculation overlay heterogeneous objects

        private void getPointPolylineOverlay(MultiPoint mp, Polyline polyline, OverlayType operation, GeometryCollection result, bool performSnapping, bool inverseArgs)
        {
            try
            {
                _geometry1 = mp;
                _geometry2 = polyline;
                bool isValid = true;

                try
                {
                    init(performSnapping);

                    PlanarGraph graph = PlanarGraph.Build(_geometry1, _geometry2);

                    // classify edges and nodes
                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        switch (operation)
                        {
                            case OverlayType.Intersection:
                                edge.Enabled = false;
                                break;
                            case OverlayType.Union:
                                edge.Enabled = edge.Label.UsedByObject2;
                                break;
                            case OverlayType.Difference:
                                edge.Enabled = inverseArgs ? edge.Label.UsedByObject2 : false;
                                break;
                            case OverlayType.SymmetricDifference:
                                edge.Enabled = edge.Label.UsedByObject2;
                                break;
                        }
                    }

                    foreach (PlanarGraphNode node in graph.Nodes)
                    {
                        bool hasEnabledEdges = false;
                        foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            if (edge.Enabled)
                            {
                                hasEnabledEdges = true;
                                break;
                            }
                        if (hasEnabledEdges)
                            node.Enabled = false;
                        else
                        {
                            switch (operation)
                            {
                                case OverlayType.Intersection:
                                    node.Enabled = node.Label.UsedByObject1 && node.Label.UsedByObject2;
                                    break;
                                case OverlayType.Union:
                                    node.Enabled = node.Label.UsedByObject1;
                                    break;
                                case OverlayType.Difference:
                                    node.Enabled = inverseArgs ? false : !node.Label.UsedByObject2;
                                    break;
                                case OverlayType.SymmetricDifference:
                                    node.Enabled = true;
                                    break;
                            }
                        }
                    }

                    // build results:
                    // point
                    List<PointD> points = graph.BuildPoints();

                    foreach (PointD p in points)
                        result.Add(p);

                    // polyline
                    Polyline pl = graph.BuildPolyline(false, false);

                    if (pl.CoordinateCount > 0)
                        result.Add(pl);

                    for (int i = 0; i < result.Count; i++)
                    {
                        IGeometry g = translateGeometry(result[i], _translationCenter.X, _translationCenter.Y);
                        if (g is PointD)
                            result[i] = g;
                    }
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }

                if (isValid)
                    return;
                else
                {
                    // overlay has not been calculated.
                    // it may be possible to calculate the overlay aligned to the grid
                    getPointPolylineOverlay(mp, polyline, operation, result, true, inverseArgs);
                    return;
                }
            }
            finally
            {
                _geometry1 = null;
                _geometry2 = null;
            }
        }

        private void getPointPolygonOverlay(MultiPoint mp, Polygon polygon, OverlayType operation, GeometryCollection result, bool performSnapping, bool inverseArgs)
        {
            try
            {
                _geometry1 = mp;
                _geometry2 = polygon;
                bool isValid = true;

                try
                {
                    init(performSnapping);

                    PlanarGraph graph = PlanarGraph.Build(_geometry1, _geometry2);

                    // duration test of the point inside the polygon to "collect" the original ground
                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject2;
                    }

                    Polygon pg = graph.BuildPolygon(inverseArgs, !inverseArgs);

                    // classify edges and nodes
                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        switch (operation)
                        {
                            case OverlayType.Intersection:
                                edge.Enabled = false;
                                break;
                            case OverlayType.Union:
                                edge.Enabled = edge.Label.UsedByObject2;
                                break;
                            case OverlayType.Difference:
                                edge.Enabled = inverseArgs ? edge.Label.UsedByObject2 : false;
                                break;
                            case OverlayType.SymmetricDifference:
                                edge.Enabled = edge.Label.UsedByObject2;
                                break;
                        }
                    }

                    foreach (PlanarGraphNode node in graph.Nodes)
                    {
                        bool hasEnabledEdges = false;
                        foreach (PlanarGraphEdge edge in node.IncidentEdges)
                            if (edge.Enabled)
                            {
                                hasEnabledEdges = true;
                                break;
                            }
                        if (hasEnabledEdges)
                            node.Enabled = false;
                        else
                        {
                            switch (operation)
                            {
                                case OverlayType.Intersection:
                                    node.Enabled = (pg.ContainsPoint(node.Point) && !node.Label.UsedByObject2) ||
                                                   (node.Label.UsedByObject2 && node.Label.UsedByObject1);
                                    break;
                                case OverlayType.Union:
                                    node.Enabled = !pg.ContainsPoint(node.Point) && !node.Label.UsedByObject2;
                                    break;
                                case OverlayType.Difference:
                                    node.Enabled = inverseArgs ? false : !pg.ContainsPoint(node.Point) && !node.Label.UsedByObject2;
                                    break;
                                case OverlayType.SymmetricDifference:
                                    node.Enabled = !pg.ContainsPoint(node.Point) && !node.Label.UsedByObject2;
                                    break;
                            }
                        }
                    }

                    // build results:
                    // point
                    List<PointD> points = graph.BuildPoints();

                    foreach (PointD p in points)
                        result.Add(p);

                    // The landfill has been constructed to assess the position of the point.
                    // But it must be added to the result only in the case of the calculation of association,
                    // symmetric difference and the difference, which is a decrease.
                    if (operation == OverlayType.Union || operation == OverlayType.SymmetricDifference ||
                       (operation == OverlayType.Difference && inverseArgs))
                    {
                        if (pg.CoordinateCount > 0)
                            result.Add(pg);
                    }

                    for (int i = 0; i < result.Count; i++)
                    {
                        IGeometry g = translateGeometry(result[i], _translationCenter.X, _translationCenter.Y);
                        if (g is PointD)
                            result[i] = g;
                    }
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }

                if (isValid)
                    return;
                else
                {
                    // overlay has not been calculated.
                    // it may be possible to calculate the overlay aligned to the grid
                    getPointPolygonOverlay(mp, polygon, operation, result, true, inverseArgs);
                    return;
                }
            }
            finally
            {
                _geometry1 = null;
                _geometry2 = null;
            }
        }

        private void getPolylinePolygonOverlay(Polyline polyline, Polygon polygon, OverlayType operation, GeometryCollection result, bool performSnapping, bool inverseArgs)
        {
            try
            {
                _geometry1 = polyline;
                _geometry2 = polygon;
                bool isValid = true;

                try
                {
                    init(performSnapping);

                    PlanarGraph graph = PlanarGraph.Build(_geometry1, _geometry2);

                    //building polygon
                    foreach (PlanarGraphNode node in graph.Nodes)
                        node.Layout = PlanarGraphNode.NodeLayout.Unknown;

                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject2;
                    }
                    Polygon pg = graph.BuildPolygon(true, false);

                    // build results:
                    // point
                    foreach (PlanarGraphNode node in graph.Nodes)
                        node.Enabled = isNodeEnabled(node, operation, polyline, pg, inverseArgs);

                    List<PointD> points = graph.BuildPoints();

                    foreach (PointD p in points)
                        result.Add(p);

                    // polyline
                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = isLinearEdgeEnabled(edge, operation, pg, inverseArgs);
                    }

                    Polyline pl = graph.BuildPolyline(false, false);
                    if (pl.CoordinateCount > 0)
                        result.Add(pl);

                    // landfill has been constructed, it may be added to the result
                    if (operation == OverlayType.SymmetricDifference || operation == OverlayType.Union ||
                        (operation == OverlayType.Difference && inverseArgs))
                        if(pg.CoordinateCount > 0)
                            result.Add(pg);

                    for (int i = 0; i < result.Count; i++)
                    {
                        IGeometry g = translateGeometry(result[i], _translationCenter.X, _translationCenter.Y);
                        if (g is PointD)
                            result[i] = g;
                    }
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }

                if (isValid)
                    return;
                else
                {
                    //overlay has not been calculated.
                    // it may be possible to calculate the overlay aligned to the grid
                    getPolylinePolygonOverlay(polyline, polygon, operation, result, true, inverseArgs);
                    return;
                }
            }
            finally
            {
                _geometry1 = null;
                _geometry2 = null;
            }
        }

        #endregion

        private GeometryCollection calculateOverlay(IGeometry geometry1, IGeometry geometry2, OverlayType operation, bool performSnapping)
        {
            GeometryCollection result = new GeometryCollection();

            if (geometry1 == null && geometry2 == null)
                return result;

            if (geometry2 == null)
            {
                if (operation != OverlayType.Intersection)
                    result.Add((IGeometry)geometry1.Clone());

                return result;
            }

            if (geometry1 == null)
            {
                if (operation == OverlayType.Intersection || operation == OverlayType.Difference)
                    return result;
                result.Add((IGeometry)geometry2.Clone());

                return result;
            }

            // If the bounding rectangles do not intersect, the result of all operations can be obtained easily
            BoundingRectangle br1 = geometry1.GetBoundingRectangle();
            BoundingRectangle br2 = geometry2.GetBoundingRectangle();
            if(!br1.IsEmpty())
                br1.Grow(PlanimetryAlgorithms.Tolerance);
            if (!br2.IsEmpty())
                br2.Grow(PlanimetryAlgorithms.Tolerance);

            if (!br1.Intersects(br2))
                return calculateNonIntersectedObjectsOverlay(geometry1, geometry2, operation, performSnapping);

            // easier to convert the point-to-multipoint to preserve generality
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

            int minDim = Math.Min((int)geometry1.Dimension, (int)geometry2.Dimension);
            int maxDim = Math.Max((int)geometry1.Dimension, (int)geometry2.Dimension);

            // overlay calculation points
            if (minDim == 0 && maxDim == 0)
                getPointPointOverlay((MultiPoint)geometry1, (MultiPoint)geometry2, operation, result);

            // calculation overlay polylines
            if (minDim == 1 && maxDim == 1)
                getPolylinePolylineOverlay((Polyline)geometry1, (Polyline)geometry2, operation, result, false);

            // calculation of polygon overlay
            if (minDim == 2 && maxDim == 2)
                getPolygonPolygonOverlay((Polygon)geometry1, (Polygon)geometry2, operation, result, false);

            // calculation overlay points and polylines
            if (minDim == 0 && maxDim == 1)
            {
                if (geometry1 is MultiPoint)
                    getPointPolylineOverlay((MultiPoint)geometry1, (Polyline)geometry2, operation, result, false, false);
                else
                    getPointPolylineOverlay((MultiPoint)geometry2, (Polyline)geometry1, operation, result, false, true);
            }

            // calculation point and polygon overlay
            if (minDim == 0 && maxDim == 2)
            {
                if (geometry1 is MultiPoint)
                    getPointPolygonOverlay((MultiPoint)geometry1, (Polygon)geometry2, operation, result, false, false);
                else
                    getPointPolygonOverlay((MultiPoint)geometry2, (Polygon)geometry1, operation, result, false, true);
            }

            // calculation overlay polylines and polygons
            if (minDim == 1 && maxDim == 2)
            {
                if (geometry1 is Polyline)
                    getPolylinePolygonOverlay((Polyline)geometry1, (Polygon)geometry2, operation, result, false, false);
                else
                    getPolylinePolygonOverlay((Polyline)geometry2, (Polygon)geometry1, operation, result, false, true);
            }

            return result;
        }

        private List<LinePath> simplifyLinePaths(IList<LinePath> paths, bool performSnapping)
        {
            try
            {
                _geometry1 = new Polyline(paths);
                _geometry2 = null;

                List<LinePath> result = new List<LinePath>();
                bool isValid = true;
                try
                {
                    init(performSnapping);

                    PlanarGraph graph;
                    if (performSnapping)
                        graph = PlanarGraph.BuildWithSnap(_geometry1, null, PlanimetryEnvironment.NewCoordinate(0, 0));
                    else
                        graph = PlanarGraph.Build(_geometry1, null);

                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject1;
                    }
                    Polyline p = graph.BuildPolyline(false, false);

                    translateGeometry(p, _translationCenter.X, _translationCenter.Y);

                    foreach (LinePath path in p.Paths)
                        result.Add(path);

                    return result;
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }
                if (isValid)
                    return result;
                else
                    return simplifyLinePaths(paths, true);
            }
            finally
            {
                _geometry1 = null;
            }
        }

        private List<Contour> simplifyContours(IList<Contour> contours, bool performSnapping)
        {
            // Algorithm similar to the algorithm computing overlay.
            // The difference is that in calculations involving the contours 
            // of one polygon and finding the resulting circuit (a circuit of edges) 
            // suitable recognized all the edges found in the landfill.
            try
            {
                _geometry1 = new Polygon(contours);
                _geometry2 = null;

                List<Contour> result = new List<Contour>();
                bool isValid = true;
                try
                {
                    init(performSnapping);

                    PlanarGraph graph;
                    if (performSnapping)
                        graph = PlanarGraph.BuildWithSnap(_geometry1, null, PlanimetryEnvironment.NewCoordinate(0, 0));
                    else
                        graph = PlanarGraph.Build(_geometry1, null);

                    foreach (PlanarGraphEdge edge in graph.Edges)
                    {
                        edge.IsVisited = false;
                        edge.Enabled = edge.Label.UsedByObject1;
                    }
                    Polygon p = graph.BuildPolygon(false, false);

                    translateGeometry(p, _translationCenter.X, _translationCenter.Y);

                    foreach (Contour c in p.Contours)
                        result.Add(c);

                    result.Sort((Contour c1, Contour c2) => c1.Layout >= c2.Layout ? 1 : -1);

                    return result;
                }
                catch (TopologyException)
                {
                    if (!performSnapping)
                        isValid = false;
                    else
                        throw new InvalidOperationException("Unable to complete operation correctly with this value of tolerance (PlanimertyAlgorithms.Tolerance)");
                }
                if (isValid)
                    return result;
                else
                    return simplifyContours(contours, true);
            }
            finally
            {
                _geometry1 = null;
            }
        }

        #region Classification of elements of a planar graph

        private bool isNodeEnabled(PlanarGraphNode node, OverlayType operation, Polygon p1, Polygon p2)
        {
            switch (operation)
            {
                case OverlayType.Intersection:
                    if (!node.Label.UsedByObject1 || !node.Label.UsedByObject2)
                        return false;
                    break;
                case OverlayType.Union:
                    return false;
                case OverlayType.Difference:
                    return false;
                case OverlayType.SymmetricDifference:
                    return false;
            }
            bool hasEnabledEdges = false;
            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                if (isAreaEdgeEnabled(edge, operation, p1, p2) ||
                    isLinearEdgeEnabled(edge, operation, p1, p2))
                {
                    hasEnabledEdges = true;
                    break;
                }

           return !hasEnabledEdges;
        }

        private bool isLinearEdgeEnabled(PlanarGraphEdge edge, OverlayType operation, Polygon p1, Polygon p2)
        {
            if (isAreaEdgeEnabled(edge, operation, p1, p2))
                return false;

            switch (operation)
            {
                case OverlayType.Intersection:
                    return edge.Label.UsedByObject1 && edge.Label.UsedByObject2;
                case OverlayType.Union:
                    return false;
                case OverlayType.Difference:
                    return false;
                case OverlayType.SymmetricDifference:
                    return false;
            }

            return false;
        }

        private bool isAreaEdgeEnabled(PlanarGraphEdge edge, OverlayType operation, Polygon p1, Polygon p2)
        {
            bool usebyPolygon1 = edge.Label.UsedByObject1;
            bool usebyPolygon2 = edge.Label.UsedByObject2;

            switch (operation)
            {
                case OverlayType.Intersection:
                    if (usebyPolygon1 && usebyPolygon2 && edge.OrientationInObject1 == edge.OrientationInObject2)
                        return true;

                    if ((usebyPolygon1 ^ usebyPolygon2))
                        if (usebyPolygon1)
                        {
                            if (p2.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                        else
                        {
                            if (p1.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                    break;
                case OverlayType.Union:
                    if (usebyPolygon1 && usebyPolygon2 && edge.OrientationInObject1 == edge.OrientationInObject2)
                        return true;

                    if ((usebyPolygon1 ^ usebyPolygon2))
                        if (usebyPolygon1)
                        {
                            if (!p2.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                        else
                        {
                            if (!p1.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                    break;
                case OverlayType.Difference:
                    if (usebyPolygon1 && usebyPolygon2 && edge.OrientationInObject1 != edge.OrientationInObject2)
                        return true;

                    if ((usebyPolygon1 ^ usebyPolygon2))
                        if (usebyPolygon1)
                        {
                            if (!p2.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                        else
                        {
                            if (p1.ContainsPoint(edge.CenterPoint()))
                                return true;
                        }
                    break;
                case OverlayType.SymmetricDifference:
                    if ((usebyPolygon1 ^ usebyPolygon2))
                        return true;
                    break;
            }

            return false;
        }

        private bool isNodeEnabled(PlanarGraphNode node, OverlayType operation, Polyline polyline, Polygon polygon, bool inverseArgs)
        {
            switch (operation)
            {
                case OverlayType.Intersection:
                    if (!node.Label.UsedByObject1 || !node.Label.UsedByObject2)
                        return false;
                    break;
                case OverlayType.Union:
                    return false;
                case OverlayType.Difference:
                case OverlayType.SymmetricDifference:
                    return false;
            }
            bool hasEnabledEdges = false;
            foreach (PlanarGraphEdge edge in node.IncidentEdges)
                if (isAreaEdgeEnabled(edge, operation, polygon, inverseArgs) ||
                    isLinearEdgeEnabled(edge, operation, polygon, inverseArgs))
                {
                    hasEnabledEdges = true;
                    break;
                }

            return !hasEnabledEdges;
        }

        private bool isLinearEdgeEnabled(PlanarGraphEdge edge, OverlayType operation, Polygon polygon, bool inverseArgs)
        {
            if (isAreaEdgeEnabled(edge, operation, polygon, inverseArgs))
                return false;

            switch (operation)
            {
                case OverlayType.Intersection:
                    return edge.Label.UsedByObject1 && (edge.Label.UsedByObject2 || polygon.ContainsPoint(edge.CenterPoint()));
                case OverlayType.Union:
                    return edge.Label.UsedByObject1 && !polygon.ContainsPoint(edge.CenterPoint());
                case OverlayType.Difference:
                    return inverseArgs ? false : 
                                         edge.Label.UsedByObject1 && 
                                         !polygon.ContainsPoint(edge.CenterPoint()) &&
                                         !edge.Label.UsedByObject2;
                case OverlayType.SymmetricDifference:
                    return edge.Label.UsedByObject1 &&
                           !polygon.ContainsPoint(edge.CenterPoint()) &&
                           !edge.Label.UsedByObject2;
            }

            return false;
        }

        private bool isAreaEdgeEnabled(PlanarGraphEdge edge, OverlayType operation, Polygon polygon, bool inverseArgs)
        {
            bool usebyPolyline = edge.Label.UsedByObject1;
            bool usebyPolygon = edge.Label.UsedByObject2;

            switch (operation)
            {
                case OverlayType.Intersection:
                    return false;
                case OverlayType.Union:
                    if (usebyPolygon) return true;
                    break;
                case OverlayType.Difference:
                    return inverseArgs;
                case OverlayType.SymmetricDifference:
                    if (usebyPolygon) return true;
                    break;
            }

            return false;
        }

        #endregion

        private void init(bool performSnapping)
        {
            _geometry1 = (IGeometry)_geometry1.Clone();
            if (_geometry2 != null)
                _geometry2 = (IGeometry)_geometry2.Clone();

            BoundingRectangle br = _geometry1.GetBoundingRectangle();
            if (_geometry2 != null)
                br.Join(_geometry2.GetBoundingRectangle());

            // Translating the pieces so that the origin was located in the center of the coverage.
            // This increases the accuracy of intermediate calculations.
            if (!br.IsEmpty())
            {
                _translationCenter = br.Center();

                _geometry1 = translateGeometry(_geometry1, -_translationCenter.X, -_translationCenter.Y);
                if (_geometry2 != null)
                    _geometry2 = translateGeometry(_geometry2, -_translationCenter.X, -_translationCenter.Y);

                if (performSnapping)
                {
                    _geometry1 = snapGeometryPoints(_geometry1);
                    reduceGeometrySegments(_geometry1, PlanimetryAlgorithms.Tolerance * 1.42);

                    if (_geometry2 != null)
                    {
                        _geometry2 = snapGeometryPoints(_geometry2);
                        reduceGeometrySegments(_geometry2, PlanimetryAlgorithms.Tolerance * 1.42);
                    }
                }
                else
                {
                    reduceGeometrySegments(_geometry1, PlanimetryAlgorithms.Tolerance);
                    if (_geometry2 != null)
                        reduceGeometrySegments(_geometry2, PlanimetryAlgorithms.Tolerance);
                }
            }
            else
                _translationCenter = PlanimetryEnvironment.NewCoordinate(0, 0);
        }

        private IGeometry snapGeometryPoints(IGeometry g)
        {
            Polygon p = g as Polygon;
            if (p != null)
                p.SnapVertices(PlanimetryEnvironment.NewCoordinate(0, 0), PlanimetryAlgorithms.Tolerance);

            Polyline pl = g as Polyline;
            if (pl != null)
                pl.SnapVertices(PlanimetryEnvironment.NewCoordinate(0, 0), PlanimetryAlgorithms.Tolerance);

            MultiPoint mp = g as MultiPoint;
            if (mp != null)
                mp.SnapToGrid(PlanimetryEnvironment.NewCoordinate(0, 0), PlanimetryAlgorithms.Tolerance);

            if (g is PointD)
            {
                PointD point = (PointD)g;
                ICoordinate coord = point.Coordinate;
                PlanimetryAlgorithms.SnapToGrid(ref coord, PlanimetryEnvironment.NewCoordinate(0, 0), PlanimetryAlgorithms.Tolerance);
                point.Coordinate = coord;
            }

            return g;
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

        private IGeometry translateGeometry(IGeometry geometry, double x, double y)
        {
            if (geometry is Polygon)
            {
                foreach (Contour c in ((Polygon)geometry).Contours)
                    for (int i = c.Vertices.Count - 1; i >= 0; i--)
                    {
                        ICoordinate p = PlanimetryEnvironment.NewCoordinate(c.Vertices[i].X + x, c.Vertices[i].Y + y);
                        c.Vertices[i] = p;
                    }
            }
            else if (geometry is Polyline)
            {
                foreach (LinePath path in ((Polyline)geometry).Paths)
                    for (int i = path.Vertices.Count - 1; i >= 0; i--)
                    {
                        ICoordinate p = PlanimetryEnvironment.NewCoordinate(path.Vertices[i].X + x, path.Vertices[i].Y + y);
                        path.Vertices[i] = p;
                    }
            }
            else if (geometry is PointD)
            {
                PointD p = ((PointD)geometry);
                p.Translate(x, y);
                return p;
            }
            else if (geometry is MultiPoint)
            {
                for (int i = ((MultiPoint)geometry).Points.Count - 1; i >= 0; i--)
                    ((MultiPoint)geometry).Points[i] =
                        PlanimetryEnvironment.NewCoordinate(((MultiPoint)geometry).Points[i].X + x, 
                                   ((MultiPoint)geometry).Points[i].Y + y);
            }
            return geometry;
        }
    }

    /// <summary>
    /// Represents a topology exception.
    /// Throws when the topology can not be handled correctly.
    /// For example, because of the effect of 
    /// loss of transitivity of numbers.
    /// </summary>
    public class TopologyException : Exception
    { 
    }
}