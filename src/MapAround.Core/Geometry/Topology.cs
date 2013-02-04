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
** File: Topology.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Common classes and interfaces for the topological algorithms
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using MapAround.Indexing;

    /// <summary>
    /// Represents a graph embedded into 2D plane.
    /// <remarks>
    /// Planar graph is used in plenty of spatial operations 
    /// such as overlay or Clementini operators.
    /// </remarks>
    /// </summary>
    public class PlanarGraph 
    {
        internal class SourceSegment
        {
            public Segment Segment;
            public int ObjectNumber;
        }

        internal class SplittedSegment : IIndexable
        {
            public Segment Segment;
            public bool UsedByObject1;
            public bool UsedByObject2;

            public int Object1OccurrencesCount = 0;
            public int Object2OccurrencesCount = 0;

            #region IIndexable Members

            public BoundingRectangle BoundingRectangle
            {
                get
                {
                    BoundingRectangle br = Segment.GetBoundingRectangle();
                    br.Grow(PlanimetryAlgorithms.Tolerance);
                    return br;
                }
            }

            #endregion

            #region ICloneable Members

            public object Clone()
            {
                SplittedSegment ss = new SplittedSegment();
                ss.Segment = this.Segment;
                ss.UsedByObject1 = this.UsedByObject1;
                ss.UsedByObject2 = this.UsedByObject2;
                ss.Object1OccurrencesCount = this.Object1OccurrencesCount;
                ss.Object2OccurrencesCount = this.Object2OccurrencesCount;
                return ss;
            }

            #endregion
        }

        private List<PlanarGraphNode> _nodes = new List<PlanarGraphNode>();
        private List<PlanarGraphEdge> _edges = new List<PlanarGraphEdge>();

        private List<SourceSegment> _sourceSegments = new List<SourceSegment>();

        private ISpatialIndex _splittedSegmentIndex;
        private List<SplittedSegment> _splittedSegments = new List<SplittedSegment>();

        private IGeometry _sourceGeometry1;
        private IGeometry _sourceGeometry2;
        private BoundingRectangle _sourceBounds = null;

        bool _performSnapping = false;
        private ICoordinate _gridOrigin = PlanimetryEnvironment.NewCoordinate(0, 0);

        private ICoordinate _pointStub = null;
        private Segment _segmentStub = new Segment();

        private bool _isBuilt = false;

        private void addVerticesAsNodes(IGeometry g)
        {
            if (g is Polygon)
            {
                foreach (Contour c in ((Polygon)g).Contours)
                    foreach (ICoordinate coordinate in c.Vertices)
                        addOrMergeNode(coordinate, g);
                return;
            }

            if (g is Polyline)
            {
                foreach (LinePath path in ((Polyline)g).Paths)
                    foreach (ICoordinate coordinate in path.Vertices)
                        addOrMergeNode(coordinate, g);
                return;
            }

            if (g is PointD)
            {
                addOrMergeNode(((PointD)g).Coordinate, g);
                return;
            }

            if (g is MultiPoint)
            {
                foreach (ICoordinate coordinate in ((MultiPoint)g).Points)
                        addOrMergeNode(coordinate, g);
                return;
            }

            throw new NotSupportedException(string.Format("Planar graph building for \"{0}\" is not supported.", g.GetType().FullName));
        }

        private void addSourceSegments(IGeometry g, int objectNumber)
        {
            SourceSegment ss;
            if (g is Polygon)
            {
                foreach (Contour c in ((Polygon)g).Contours)
                {
                    if (c.Vertices.Count > 1)
                    {
                        int i;
                        Segment s;
                        ICoordinate initialPoint = c.Vertices[0];
                        for (i = 0; i < c.Vertices.Count - 1; i++)
                        {
                            s = new Segment(initialPoint.X, initialPoint.Y, c.Vertices[i + 1].X, c.Vertices[i + 1].Y);
                            ss = new SourceSegment();
                            ss.Segment = s;

                            if (!ss.Segment.V1.ExactEquals(ss.Segment.V2))
                            {
                                ss.ObjectNumber = objectNumber;
                                _sourceSegments.Add(ss);
                            }
                            initialPoint = c.Vertices[i + 1];
                        }

                        s = new Segment(initialPoint.X, initialPoint.Y, c.Vertices[0].X, c.Vertices[0].Y);
                        ss = new SourceSegment();
                        ss.Segment = s;
                        if (!ss.Segment.V1.ExactEquals(ss.Segment.V2))
                        {
                            ss.ObjectNumber = objectNumber;
                            _sourceSegments.Add(ss);
                        }
                    }
                }

                return;
            }

            if (g is Polyline)
            {
                foreach (LinePath path in ((Polyline)g).Paths)
                {
                    if (path.Vertices.Count > 1)
                    {
                        int i;
                        Segment s;
                        ICoordinate initialPoint = path.Vertices[0];
                        for (i = 0; i < path.Vertices.Count - 1; i++)
                        {
                            s = new Segment(initialPoint.X, initialPoint.Y, path.Vertices[i + 1].X, path.Vertices[i + 1].Y);
                            ss = new SourceSegment();
                            ss.Segment = s;
                            ss.ObjectNumber = objectNumber;
                            _sourceSegments.Add(ss);
                            initialPoint = path.Vertices[i + 1];
                        }
                    }
                }

                return;
            }

            if (g is PointD || g is MultiPoint)
                return;

            throw new NotSupportedException(string.Format("Planar graph building for \"{0}\" is not supported.", g.GetType().FullName));
        }

        private void addOrMergeNode(ICoordinate coordinate, IGeometry obj)
        {
            double tolerance = PlanimetryAlgorithms.Tolerance;
            double x = coordinate.X;
            PlanarGraphNode node;

            int startIndex = 0, endIndex = _nodes.Count - 1;

            while (endIndex - startIndex > 1)
            {
                int index = startIndex + (endIndex - startIndex) / 2;
                if (_nodes[index].Point.X < x)
                    startIndex = index;
                else
                    endIndex = index;
            }

            while (startIndex > 0 &&
                   _nodes[startIndex].Point.X + tolerance > x)
                startIndex--;

            while (endIndex < _nodes.Count - 1 &&
                   _nodes[endIndex].Point.X - tolerance < x)
                endIndex++;

            for (int i = startIndex; i <= endIndex; i++)
            {
                ICoordinate p = _nodes[i].Point;

                if (Math.Abs(p.X - x) < tolerance &&
                    Math.Abs(p.Y - coordinate.Y) < tolerance)
                    if ((_performSnapping && p.ExactEquals(coordinate)) ||
                        (!_performSnapping && p.Equals(coordinate)))
                    {
                        _nodes[i].Label.UsedByObject1 = obj == _sourceGeometry1 || _nodes[i].Label.UsedByObject1;
                        _nodes[i].Label.UsedByObject2 = obj == _sourceGeometry2 || _nodes[i].Label.UsedByObject2;
                        return;
                    }

                if (p.X > x)
                {
                    node = new PlanarGraphNode(coordinate);
                    node.Label.UsedByObject1 = obj == _sourceGeometry1;
                    node.Label.UsedByObject2 = obj == _sourceGeometry2;

                    if(obj == _sourceGeometry1)
                        node.Label.Object1OccurrencesCount++;

                    if(obj == _sourceGeometry2)
                        node.Label.Object2OccurrencesCount++;

                    _nodes.Insert(i, node);
                    return;
                }
            }

            node = new PlanarGraphNode(coordinate);
            node.Label.UsedByObject1 = obj == _sourceGeometry1;
            node.Label.UsedByObject2 = obj == _sourceGeometry2;

            if (obj == _sourceGeometry1)
                node.Label.Object1OccurrencesCount++;

            if (obj == _sourceGeometry2)
                node.Label.Object2OccurrencesCount++;

            _nodes.Add(node);
        }

        private void init()
        {
            _nodes.Clear();
            _edges.Clear();
            _sourceSegments.Clear();

            int pointCount = 0;

            _sourceBounds = new BoundingRectangle();
            if (_sourceGeometry1 != null)
            {
                _sourceBounds.Join(_sourceGeometry1.GetBoundingRectangle());
                pointCount += _sourceGeometry1.CoordinateCount;
            }

            if (_sourceGeometry2 != null)
            {
                _sourceBounds.Join(_sourceGeometry2.GetBoundingRectangle());
                pointCount += _sourceGeometry2.CoordinateCount;
            }

            if (pointCount > 800)
            {
                _sourceBounds.Grow(PlanimetryAlgorithms.Tolerance * 10);

                _splittedSegmentIndex = new KDTree(_sourceBounds);
                _splittedSegmentIndex.MaxDepth = 20;
                _splittedSegmentIndex.BoxSquareThreshold = _sourceBounds.Width * _sourceBounds.Height / 10000;
            }
        }
        
        private void build()
        {
            init();

            if (_sourceGeometry1 != null)
            {
                addVerticesAsNodes(_sourceGeometry1);
                addSourceSegments(_sourceGeometry1, 1);
            }

            if (_sourceGeometry2 != null)
            {
                addVerticesAsNodes(_sourceGeometry2);
                addSourceSegments(_sourceGeometry2, 2);
            }

            // divide the points of intersection of the segments
            splitSegments();

            // add edges
            addEdges();
            
            _sourceSegments = new List<SourceSegment>();
            _splittedSegmentIndex = null;
            _splittedSegments = new List<SplittedSegment>();

            // Earl built
            _isBuilt = true;
        }
               
        private bool updateSplittedSegment(List<SplittedSegment> relatedSegments, SourceSegment segment)
        {
            foreach (SplittedSegment ss in relatedSegments)
                if ((_performSnapping && ss.Segment.ExactEquals(segment.Segment)) ||
                    (!_performSnapping && ss.Segment == segment.Segment))
                {
                    // current segment coincided with the previously calculated

                    if (_sourceGeometry1 is Polygon)
                        // For polygons segment - the border. "Use" is defined by the parity of entry.
                        ss.UsedByObject1 = ss.UsedByObject1 ^ segment.ObjectNumber == 1;
                    else
                    {
                        // For polyline segment - interior. "Use" is defined by the fact of the entry.
                        ss.UsedByObject1 = ss.UsedByObject1 || segment.ObjectNumber == 1;
                    }

                    if (_sourceGeometry2 is Polygon)
                        ss.UsedByObject2 = ss.UsedByObject2 ^ segment.ObjectNumber == 2;
                    else
                        ss.UsedByObject2 = ss.UsedByObject2 || segment.ObjectNumber == 2;

                    if (segment.ObjectNumber == 1)
                        ss.Object1OccurrencesCount++;
                    else
                        ss.Object2OccurrencesCount++;

                    return true;
                }

            return false;
        }

        private bool splitSegmentByNodes(SourceSegment segment)
        {
            double tolerance = PlanimetryAlgorithms.Tolerance;
            List<ICoordinate> nodesCrossPoints = null;

            double x1 = segment.Segment.V1.X;
            double x2 = segment.Segment.V2.X;
            double minX = Math.Min(x1, x2);
            double maxX = Math.Max(x1, x2);

            int startNodesIndex = 0, endNodesIndex = _nodes.Count - 1;

            while (endNodesIndex - startNodesIndex > 1)
            {
                int index = startNodesIndex + (endNodesIndex - startNodesIndex) / 2;
                if (_nodes[index].Point.X < minX)
                    startNodesIndex = index;
                else
                    endNodesIndex = index;
            }

            while (startNodesIndex > 0 &&
                _nodes[startNodesIndex].Point.X >= minX)
                startNodesIndex--;

            while (endNodesIndex < _nodes.Count - 1 &&
                   _nodes[endNodesIndex].Point.X <= maxX)
                endNodesIndex++;

            for (int i = startNodesIndex; i <= endNodesIndex; i++)
            {
                PlanarGraphNode node = _nodes[i];

                // exclude the end-points
                bool isNotCurrentSegmentVertex =
                    (!_performSnapping &&
                     (PlanimetryAlgorithms.Distance(node.Point, segment.Segment.V1) > tolerance &&
                      PlanimetryAlgorithms.Distance(node.Point, segment.Segment.V2) > tolerance)) ||
                    (_performSnapping &&
                     (!node.Point.ExactEquals(segment.Segment.V1) &&
                      !node.Point.ExactEquals(segment.Segment.V2)));

                if (isNotCurrentSegmentVertex)
                {
                    if (PlanimetryAlgorithms.DistanceToSegment(node.Point, segment.Segment) < tolerance / 1.45)
                    {
                        if (nodesCrossPoints == null)
                            nodesCrossPoints = new List<ICoordinate>();

                        nodesCrossPoints.Add(node.Point);
                    }
                }
            }

            if (nodesCrossPoints != null)
            {
                for (int i = 0; i < nodesCrossPoints.Count; i++)
                {
                    IGeometry obj = segment.ObjectNumber == 1 ? _sourceGeometry1 : _sourceGeometry2;
                    addOrMergeNode(nodesCrossPoints[i], obj);
                }

                PlanimetryAlgorithms.OrderPointsOverSegment(nodesCrossPoints, segment.Segment);
                nodesCrossPoints.Insert(0, segment.Segment.V1);
                nodesCrossPoints.Add(segment.Segment.V2);

                for (int i = 0; i < nodesCrossPoints.Count - 1; i++)
                {
                    SourceSegment ss = new SourceSegment();
                    ss.Segment = new Segment(nodesCrossPoints[i], nodesCrossPoints[i + 1]);
                    if (!ss.Segment.V1.ExactEquals(ss.Segment.V2))
                    {
                        ss.ObjectNumber = segment.ObjectNumber;
                        _sourceSegments.Add(ss);
                    }
                }
            }

            return nodesCrossPoints != null;
        }

        private bool splitSegmentBySegments(List<SplittedSegment> relatedSegments, SourceSegment currentSegment)
        {
            // look for the intersection
            List<ICoordinate> segmentsCrossPoints = null;
            List<SplittedSegment> segmentsToSplit = null;
            foreach (SplittedSegment ss in relatedSegments)
            {
                Dimension crossKind =
                    PlanimetryAlgorithms.RobustSegmentsIntersection(ss.Segment,
                                                                    currentSegment.Segment,
                                                                    out _pointStub,
                                                                    out _segmentStub);

                if (crossKind == Dimension.One)
                {
                    if (_performSnapping)
                    {
                        PlanimetryAlgorithms.SnapToGrid(ref _segmentStub.V1, _gridOrigin, PlanimetryAlgorithms.Tolerance);
                        PlanimetryAlgorithms.SnapToGrid(ref _segmentStub.V2, _gridOrigin, PlanimetryAlgorithms.Tolerance);

                        if (_segmentStub.Length() / PlanimetryAlgorithms.Tolerance > 1.42)
                            throw new InvalidOperationException();
                        else
                        {
                            crossKind = Dimension.Zero;
                            _pointStub = _segmentStub.V1;
                        }
                    }
                    else
                        throw new InvalidOperationException();
                }

                if (crossKind == Dimension.Zero)
                    if (PerformSnapping)
                    {
                        PlanimetryAlgorithms.SnapToGrid(ref _pointStub, _gridOrigin, PlanimetryAlgorithms.Tolerance);

                        if (!_pointStub.ExactEquals(currentSegment.Segment.V1) &&
                            !_pointStub.ExactEquals(currentSegment.Segment.V2))
                        {

                            if (segmentsCrossPoints == null)
                            {
                                segmentsCrossPoints = new List<ICoordinate>();
                                segmentsToSplit = new List<SplittedSegment>();
                            }

                            segmentsCrossPoints.Add(_pointStub);
                            segmentsToSplit.Add(ss);
                        }
                    }
                    else
                    {
                        if (!_pointStub.Equals(currentSegment.Segment.V1) &&
                            !_pointStub.Equals(currentSegment.Segment.V2))
                        {

                            if (segmentsCrossPoints == null)
                            {
                                segmentsCrossPoints = new List<ICoordinate>();
                                segmentsToSplit = new List<SplittedSegment>();
                            }

                            segmentsCrossPoints.Add(_pointStub);
                            segmentsToSplit.Add(ss);
                        }
                    }

            }

            if (segmentsCrossPoints != null)
                splitSegmentsByCrossPoints(segmentsToSplit, segmentsCrossPoints, currentSegment);

            return segmentsCrossPoints != null;
        }

        private void splitSegmentsByCrossPoints(List<SplittedSegment> segmentsToSplit, List<ICoordinate> segmentsCrossPoints, SourceSegment currentSegment)
        {
            // Removes a previously processed the broken pieces and add the pieces to the list of unprocessed segments
            int i = 0;
            foreach (SplittedSegment ss in segmentsToSplit)
            {
                removeSplittedSegment(ss);
                if (ss.UsedByObject1)
                {
                    SourceSegment s = new SourceSegment();
                    s.Segment = new Segment(ss.Segment.V1, segmentsCrossPoints[i]);
                    if (!s.Segment.V1.ExactEquals(s.Segment.V2))
                    {
                        s.ObjectNumber = 1;
                        _sourceSegments.Add(s);
                    }

                    s = new SourceSegment();
                    s.Segment = new Segment(segmentsCrossPoints[i], ss.Segment.V2);
                    if (!s.Segment.V1.ExactEquals(s.Segment.V2))
                    {
                        s.ObjectNumber = 1;
                        _sourceSegments.Add(s);
                    }
                }
                if (ss.UsedByObject2)
                {
                    SourceSegment s = new SourceSegment();
                    s.Segment = new Segment(ss.Segment.V1, segmentsCrossPoints[i]);
                    if (!s.Segment.V1.ExactEquals(s.Segment.V2))
                    {
                        s.ObjectNumber = 2;
                        _sourceSegments.Add(s);
                    }

                    s = new SourceSegment();
                    s.Segment = new Segment(segmentsCrossPoints[i], ss.Segment.V2);
                    if (!s.Segment.V1.ExactEquals(s.Segment.V2))
                    {
                        s.ObjectNumber = 2;
                        _sourceSegments.Add(s);
                    }
                }
                i++;
            }

            // add to the list of points of intersection zlov
            i = 0;
            foreach (ICoordinate point in segmentsCrossPoints)
            {
                // unit used "owners" segments which crossed the current
                if (segmentsToSplit[i].UsedByObject1)
                    addOrMergeNode(point, _sourceGeometry1);

                if (segmentsToSplit[i].UsedByObject2)
                    addOrMergeNode(point, _sourceGeometry2);

                //as well as the "owner" of the current segment
                if (currentSegment.ObjectNumber == 1)
                    addOrMergeNode(point, _sourceGeometry1);

                if (currentSegment.ObjectNumber == 2)
                    addOrMergeNode(point, _sourceGeometry2);
                i++;
            }

            // split the initial segment

            PlanimetryAlgorithms.OrderPointsOverSegment(segmentsCrossPoints, currentSegment.Segment);
            segmentsCrossPoints.Insert(0, currentSegment.Segment.V1);
            segmentsCrossPoints.Add(currentSegment.Segment.V2);
            for (i = 0; i < segmentsCrossPoints.Count - 1; i++)
            {
                SourceSegment s = new SourceSegment();
                s.Segment = new Segment(segmentsCrossPoints[i], segmentsCrossPoints[i + 1]);
                if (!s.Segment.V1.ExactEquals(s.Segment.V2))
                {
                    s.ObjectNumber = currentSegment.ObjectNumber;
                    _sourceSegments.Add(s);
                }
            }
        }

        private List<SplittedSegment> getRelatedSegments(Segment segment)
        {
            List<SplittedSegment> result = new List<SplittedSegment>();
            if (_splittedSegmentIndex != null)
                _splittedSegmentIndex.QueryObjectsInRectangle(segment.GetBoundingRectangle(), result);
            else
            {
                int startIndex = 0;
                int endIndex = _splittedSegments.Count - 1;
                double v2x = segment.V2.X;
                double v1x = segment.V1.X;
                double tolerance = PlanimetryAlgorithms.Tolerance;

                while (endIndex - startIndex > 1)
                {
                    int index = startIndex + (endIndex - startIndex) / 2;
                    if (_splittedSegments[index].Segment.V1.X > v2x)
                        endIndex = index;
                    else
                        startIndex = index;
                }

                while (endIndex < _splittedSegments.Count - 1 &&
                       _splittedSegments[endIndex].Segment.V1.X < v2x + tolerance)
                    endIndex++;

                for (int i = endIndex; i >= 0; i--)
                {
                    if (_splittedSegments[i].Segment.V2.X + tolerance < v1x ||
                        _splittedSegments[i].Segment.V1.X - tolerance > v2x)
                        continue;

                    result.Add(_splittedSegments[i]);
                }
            }

            return result;
        }

        private void addSplittedSegment(SplittedSegment ss)
        {
            if (_splittedSegmentIndex != null)
                _splittedSegmentIndex.Insert(ss);
            else
            {
                double tolerance = PlanimetryAlgorithms.Tolerance;
                double v1x = ss.Segment.V1.X;

                int startIndex = 0, endIndex = _splittedSegments.Count - 1;

                while (endIndex - startIndex > 1)
                {
                    int index = startIndex + (endIndex - startIndex) / 2;
                    if (_splittedSegments[index].Segment.V1.X < v1x)
                        startIndex = index;
                    else
                        endIndex = index;
                }

                while (startIndex > 0 &&
                       _splittedSegments[startIndex].Segment.V1.X + tolerance > v1x)
                    startIndex--;

                while (endIndex < _splittedSegments.Count - 1 &&
                       _splittedSegments[endIndex].Segment.V1.X - tolerance < v1x)
                    endIndex++;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (_splittedSegments[i].Segment.V1.X > v1x)
                    {
                        _splittedSegments.Insert(i, ss);
                        return;
                    }
                }

                _splittedSegments.Add(ss);
            }
        }

        private void removeSplittedSegment(SplittedSegment ss)
        {
            if (_splittedSegmentIndex != null)
                _splittedSegmentIndex.Remove(ss);
            else
                _splittedSegments.Remove(ss);
        }

        private void normalizeSourceSegment(SourceSegment s)
        {
            if (s.Segment.V1.X > s.Segment.V2.X)
            {
                double f = s.Segment.V1.X;
                s.Segment.V1.X = s.Segment.V2.X;
                s.Segment.V2.X = f;
                f = s.Segment.V1.Y;
                s.Segment.V1.Y = s.Segment.V2.Y;
                s.Segment.V2.Y = f;
            }
        }

        private void splitSegments()
        {
            double tolerance = PlanimetryAlgorithms.Tolerance;
            while (_sourceSegments.Count > 0)
            {
                SourceSegment currentSegment = _sourceSegments[_sourceSegments.Count - 1];
                _sourceSegments.RemoveAt(_sourceSegments.Count - 1);

                normalizeSourceSegment(currentSegment);

                // calculate the lengths that can match the current or intersects with the
                List<SplittedSegment> relatedSegments = getRelatedSegments(currentSegment.Segment);

                // update previously treated segment in the case of complete coincidence, if it was a coincidence, continue
                if (updateSplittedSegment(relatedSegments, currentSegment))
                    continue;

                // Splits the current segment of the nodes if the segment was split, continue
                if (splitSegmentByNodes(currentSegment))
                    continue;

                // Splits the current segment of the other segment if the segment was split, continue
                if (splitSegmentBySegments(relatedSegments, currentSegment))
                    continue;

                // if we got here, then the segment does not require additional processing, add it to the list of processed segments
                SplittedSegment ss = new SplittedSegment();
                ss.UsedByObject1 = currentSegment.ObjectNumber == 1;
                ss.UsedByObject2 = currentSegment.ObjectNumber == 2;
                ss.Object1OccurrencesCount = currentSegment.ObjectNumber == 1 ? 1 : 0;
                ss.Object2OccurrencesCount = currentSegment.ObjectNumber == 2 ? 1 : 0;
                ss.Segment = currentSegment.Segment;
                addSplittedSegment(ss);
            }
        }

        private void addEdges()
        {
            List<SplittedSegment> list;
            if (_splittedSegmentIndex != null)
            {
                list = new List<SplittedSegment>();
                _splittedSegmentIndex.QueryObjectsInRectangle(_splittedSegmentIndex.IndexedSpace, list);
            }
            else
                list = _splittedSegments;

            _edges.Clear();

            foreach (SplittedSegment ss in list)
            {
                //if ((ss.Segment.V1.X == 161.836898803711 &&
                //     ss.Segment.V2.X == 162.26921081543)
                //    ||
                //    (ss.Segment.V2.X == 161.836898803711 &&
                //     ss.Segment.V1.X == 162.26921081543))
                //{
                //    int a = 1;
                //}

                PlanarGraphNode node1 = getNodeAt(ref ss.Segment.V1);
                PlanarGraphNode node2 = getNodeAt(ref ss.Segment.V2);

                if (node1 == null || node2 == null)
                    throw new InvalidOperationException("Internal error");

                if (node1 == node2)
                    throw new TopologyException();
                else
                {
                    PlanarGraphEdge edge = new PlanarGraphEdge(node1, node2);
                    edge.Label.UsedByObject1 = ss.UsedByObject1;
                    edge.Label.UsedByObject2 = ss.UsedByObject2;
                    edge.Label.Object1OccurrencesCount = ss.Object1OccurrencesCount;
                    edge.Label.Object2OccurrencesCount = ss.Object2OccurrencesCount;
                    node1.IncidentEdges.Add(edge);
                    node2.IncidentEdges.Add(edge);
                    addEdge(edge);
                }
            }
        }

        private void addEdge(PlanarGraphEdge edge)
        {
            _edges.Add(edge);
        }

        private PlanarGraphNode getNodeAt(ref ICoordinate point)
        {
            double tolerance = PlanimetryAlgorithms.Tolerance;
            int startNodesIndex = 0, endNodesIndex = _nodes.Count - 1;
            double minX = point.X - tolerance;
            double maxX = point.X + tolerance;

            while (endNodesIndex - startNodesIndex > 1)
            {
                int index = startNodesIndex + (endNodesIndex - startNodesIndex) / 2;
                if (_nodes[index].Point.X < minX)
                    startNodesIndex = index;
                else
                    endNodesIndex = index;
            }

            while (startNodesIndex > 0 &&
                _nodes[startNodesIndex].Point.X >= minX)
                startNodesIndex--;

            while (endNodesIndex < _nodes.Count - 1 &&
                   _nodes[endNodesIndex].Point.X <= maxX)
                endNodesIndex++;

            for (int j = startNodesIndex; j <= endNodesIndex; j++)
            {
                if (_performSnapping)
                {
                    if (_nodes[j].Point.ExactEquals(point))
                        return _nodes[j];
                }
                else
                    if (PlanimetryAlgorithms.Distance(_nodes[j].Point, point) < tolerance)
                        return _nodes[j];
            }

            return null;
        }

        /// <summary>
        /// Gets an angle between two segments that share an endpoint.
        /// </summary>
        private double getAngleBetweenEdges(ref Segment s1, ref Segment s2, bool counterClockwise)
        {
            ICoordinate p1 = null;
            ICoordinate p2 = null;
            ICoordinate p3 = null;

            if (s2.V1.ExactEquals(s1.V1))
            { p1 = s1.V2; p2 = s1.V1; p3 = s2.V2; }
            else
            {
                if (s2.V2.ExactEquals(s1.V1))
                { p1 = s1.V2; p2 = s1.V1; p3 = s2.V1; }
                else
                {
                    if (s2.V2.ExactEquals(s1.V2))
                    { p1 = s1.V1; p2 = s1.V2; p3 = s2.V1; }
                    else
                    {
                        if (s2.V1.ExactEquals(s1.V2))
                        { p1 = s1.V1; p2 = s1.V2; p3 = s2.V2; }
                        else
                            throw new ArgumentException("The segments don't have a common endpoint");
                    }
                }
            }

            return getAngle(p1, p2, p3, counterClockwise);
        }

        /// <summary>
        /// Gets an angle between p2p1 and p2p3 rays.
        /// Angle is measured from p2p1 ray.
        /// </summary>
        private double getAngle(ICoordinate p1, ICoordinate p2, ICoordinate p3, bool counterClockwise)
        {
            p1 = (ICoordinate)p1.Clone();
            p2 = (ICoordinate)p2.Clone();
            p3 = (ICoordinate)p3.Clone();

            // translating the origin
            p1.X -= p2.X; p1.Y -= p2.Y;
            p3.X -= p2.X; p3.Y -= p2.Y;

            double alpha = p1.X != 0 ? Math.Atan(Math.Abs(p1.Y / p1.X)) : _halfPI;
            double betta = p3.X != 0 ? Math.Atan(Math.Abs(p3.Y / p3.X)) : _halfPI;

            alpha = translateAngleQuadrant(alpha, pointQuadrantNumber(p1));
            betta = translateAngleQuadrant(betta, pointQuadrantNumber(p3));

            if (counterClockwise)
                return alpha < betta ? (betta - alpha) : (_twoPi - alpha + betta);
            else
                return alpha > betta ? (alpha - betta) : (_twoPi - betta + alpha);
        }

        private static double translateAngleQuadrant(double angle, int quadrantNumber)
        {
            switch (quadrantNumber)
            {
                case 1: { return angle; }
                case 2: { return Math.PI - angle; }
                case 3: { return Math.PI + angle; }
                case 4: { return _twoPi - angle; }
            }

            return angle;
        }

        private static int pointQuadrantNumber(ICoordinate p)
        {
            if (p.X > 0)
            {
                if (p.Y > 0) return 1; else return 4;
            }
            else
            {
                if (p.Y > 0) return 2; else return 3;
            }
        }

        private void throwIfNotBuilt()
        {
            if (!_isBuilt)
                throw new InvalidOperationException("Graph was not built");
        }

        /// <summary>
        /// Gets a value indicating whether a graph is built.
        /// </summary>
        public bool IsBuilt
        {
            get { return _isBuilt; }
        }

        /// <summary>
        /// Gets a collection of edges of this graph.
        /// </summary>
        public ReadOnlyCollection<PlanarGraphEdge> Edges
        {
            get { return _edges.AsReadOnly(); }
        }

        /// <summary>
        /// Gets a collection of nodes of this graph.
        /// </summary>
        public ReadOnlyCollection<PlanarGraphNode> Nodes
        {
            get { return _nodes.AsReadOnly(); }
        }

        /// <summary>
        /// Builds a points from graph nodes.
        /// </summary>
        /// <returns>List containing visited points</returns>
        public List<PointD> BuildPoints()
        {
            throwIfNotBuilt();

            List<PointD> result = new List<PointD>();

            foreach (PlanarGraphNode node in _nodes)
                if (node.Enabled)
                    result.Add(new PointD(node.Point));

            return result;
        }

        /// <summary>
        /// Builds a polyline from graph edges.
        /// </summary>
        /// <returns>Polyline</returns>
        public Polyline BuildPolyline(bool markObject1EdgesOrientation, bool markObject2EdgesOrientation)
        {
            throwIfNotBuilt();

            List<LinePath> result = new List<LinePath>();

            for (int i = 0; i < _nodes.Count; i++)
            {
                LinePath path = processLinePath(i,
                                           markObject1EdgesOrientation,
                                           markObject2EdgesOrientation);

                if (path != null)
                    result.Add(path);
            }

            Polyline pl = new Polyline();
            pl.Paths = result;

            return pl;
        }

        private LinePath processLinePath(int startNodeIndex, bool markObject1EdgesOrientation, bool markObject2EdgesOrientation)
        {
            //number of "branches" polyline
            int sideNumber = 0;
            LinePath result = null;

            PlanarGraphNode startNode = _nodes[startNodeIndex];

            for (int j = 0; j < startNode.IncidentEdges.Count; j++)
            {
                if (!startNode.IncidentEdges[j].IsVisited &&
                     startNode.IncidentEdges[j].Enabled) //incident to Fail
                {
                    j = -1;
                    sideNumber++;
                    if (sideNumber > 2)
                        break;

                    if (result == null && sideNumber == 1)
                    {
                        result = new LinePath();
                        result.Vertices.Add((ICoordinate)startNode.Point.Clone()); // the first peak of the new polyline
                    }

                    PlanarGraphNode currentNode = startNode;

                    Segment previousEdge = new Segment();
                    bool edgeDetected = false;

                    List<PlanarGraphEdge> possibleEdges = new List<PlanarGraphEdge>();

                    while (true)
                    {
                        // definition of edges to turn to the right
                        possibleEdges.Clear();

                        foreach (PlanarGraphEdge edge in currentNode.IncidentEdges)
                            if (!edge.IsVisited && edge.Enabled)
                                possibleEdges.Add(edge);

                        if (possibleEdges.Count > 0)
                        {
                            if (!edgeDetected)
                            {
                                previousEdge.V1 =(ICoordinate)currentNode.Point.Clone();
                                previousEdge.V2 = (ICoordinate)currentNode.Point.Clone();

                                double l = PlanimetryAlgorithms.Tolerance * 1e10;

                                previousEdge.V2.Y += l;
                                edgeDetected = true;
                            }

                            double maxAngle = 0; // maximum rotation angle found
                            double minAngle = _twoPi; // минимальный найденный угол поворота
                            PlanarGraphEdge targetEdge = null; // Target edge

                            if (possibleEdges.Count == 1)
                                targetEdge = possibleEdges[0];
                            else
                            {
                                for (int k = 0; k < possibleEdges.Count; k++) // find the edge with the minimum angle
                                {
                                    PlanarGraphEdge edge = possibleEdges[k];
                                    Segment kEdge = new Segment(edge.Node1.Point, edge.Node2.Point);
                                    double angle;
                                    try
                                    {
                                        angle = getAngleBetweenEdges(ref previousEdge, ref kEdge, false);
                                    }
                                    catch (ArgumentException)
                                    {
                                        throw new TopologyException();
                                    }

                                    if (angle >= _twoPi)
                                        angle -= _twoPi;

                                    if (result.Vertices.Count > 1)
                                    {
                                        if (angle > maxAngle)
                                        {
                                            maxAngle = angle;
                                            targetEdge = edge;
                                        }
                                    }
                                    else // first rib - a special case, we search for the minimum angle
                                    {
                                        if (angle < minAngle)
                                        {
                                            minAngle = angle;
                                            targetEdge = edge;
                                        }
                                    }
                                }
                            }

                            if (targetEdge != null)
                            {
                                if (targetEdge.Node1.Point.ExactEquals(currentNode.Point))
                                {
                                    if (markObject1EdgesOrientation)
                                        targetEdge.OrientationInObject1 = PlanarGraphEdge.EdgeOrientation.Forward;

                                    if (markObject2EdgesOrientation)
                                        targetEdge.OrientationInObject2 = PlanarGraphEdge.EdgeOrientation.Forward;

                                    ICoordinate coord = (ICoordinate)targetEdge.Node2.Point.Clone();
                                    if (sideNumber == 1)
                                        result.Vertices.Add(coord);
                                    else
                                        result.Vertices.Insert(0, coord);
                                    currentNode = targetEdge.Node2;
                                }
                                else
                                {
                                    if (markObject1EdgesOrientation)
                                        targetEdge.OrientationInObject1 = PlanarGraphEdge.EdgeOrientation.Backward;

                                    if (markObject2EdgesOrientation)
                                        targetEdge.OrientationInObject2 = PlanarGraphEdge.EdgeOrientation.Backward;

                                    ICoordinate coord = (ICoordinate)targetEdge.Node1.Point.Clone();
                                    if (sideNumber == 1)
                                        result.Vertices.Add(coord);
                                    else
                                        result.Vertices.Insert(0, coord);
                                    currentNode = targetEdge.Node1;
                                }

                                previousEdge = new Segment(targetEdge.Node1.Point, targetEdge.Node2.Point);
                                targetEdge.IsVisited = true;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a polygon from graph edges.
        /// </summary>
        /// <returns>Polygon</returns>
        public Polygon BuildPolygon(bool markObject1EdgesOrientation, bool markObject2EdgesOrientation)
        {
            throwIfNotBuilt();

            // generated contours
            List<Contour> result = new List<Contour>();

            for (int i = 0; i < _nodes.Count; i++)
            {
                // the first node is involved in at least one outer loop
                if (i == 0)
                    _nodes[i].Layout = PlanarGraphNode.NodeLayout.External;

                // If the location of the site is not yet known
                if (_nodes[i].Layout == PlanarGraphNode.NodeLayout.Unknown)
                {
                    ICoordinate point = _nodes[i].Point;
                    bool isInternalNode = Polygon.ContainsPoint(point, result);

                    if (isInternalNode)
                        _nodes[i].Layout = PlanarGraphNode.NodeLayout.Internal;
                    else
                        _nodes[i].Layout = PlanarGraphNode.NodeLayout.External;
                }

                Contour c;
                do
                {
                    c = processContour(i,
                                               _nodes[i].Layout,
                                               markObject1EdgesOrientation,
                                               markObject2EdgesOrientation);

                    if (c != null)
                        if (c.Vertices.Count > 2) //discard degenerate contours
                        {
                            if (_nodes[i].Layout == PlanarGraphNode.NodeLayout.Internal)
                                c.Layout = Contour.ContourLayout.Internal;
                            else
                                c.Layout = Contour.ContourLayout.External;

                            c.Reverse();
                            result.Add(c);
                        }
                } while (c != null);
            }

            Polygon polygon = new Polygon();

            // Ориентированные контуры могут содержать самокасания
            // Для того, чтобы области, ограничиваемые снаружи внешними контурами, 
            // удовлетворяли свойству линейной связности, нужно разбить контуры 
            // в точках самокасания.

            //Технически это выражено и в спецификации OGC как: Linear Rings may not self-touch.
            normalizeContours(result);

            polygon.Contours = result;

            return polygon;
        }

        private enum EdgeUsage { None, Forward, Backward, Both };

        private struct ContourData
        {
            public Contour C;
            public double Area;
            public Contour.Orientation Orientation;
        }

        /// <summary>
        /// Builds all faces.
        /// </summary>
        /// <returns>List of polygons defining faces</returns>
        public List<Polygon> BuildFaces(out IList<Segment> redundantSegments)
        {
            List<Polygon> result = new List<Polygon>();
            List<Contour> contours = new List<Contour>();

            _edges.ForEach(edge => edge.Label.Tag = EdgeUsage.None);

            // traverse graph
            foreach (PlanarGraphEdge edge in _edges)
            {
                if(edge.Enabled)
                {
                    if ((EdgeUsage)edge.Label.Tag == EdgeUsage.None || (EdgeUsage)edge.Label.Tag == EdgeUsage.Backward)
                        contours.Add(processContour(edge, false));

                    if ((EdgeUsage)edge.Label.Tag == EdgeUsage.None || (EdgeUsage)edge.Label.Tag == EdgeUsage.Forward)
                        contours.Add(processContour(edge, true));
                }
            }

            _edges.ForEach(edge => edge.Label.Tag = null);

            // put contours into three collections: clockwise, counterclockwise and degenerates
            List<ContourData> cwContours = new List<ContourData>();
            List<ContourData> ccwContours = new List<ContourData>();
            List<Contour> degenerateContours = new List<Contour>();
            foreach (Contour c in contours)
            {
                Contour.Orientation orientation = c.GetOrientation();
                if (orientation != Contour.Orientation.Undefined)
                {
                    ContourData cd = new ContourData()
                    {
                        C = c,
                        Orientation = orientation,
                        Area = c.SimpleArea()
                    };

                    c.Layout = cd.Orientation ==
                        Contour.Orientation.CW ?
                        Contour.ContourLayout.External : Contour.ContourLayout.Internal;

                    c.Reverse();

                    if (cd.Orientation == Contour.Orientation.CW)
                        cwContours.Add(cd);
                    else
                        ccwContours.Add(cd);
                }
                else
                    degenerateContours.Add(c);
            }

            // split degenerate contours to linear and polygonal parts
            List<Contour> splittedContours = new List<Contour>();
            List<Segment> segments = new List<Segment>();
            foreach (Contour c in degenerateContours)
            {
                List<Contour> cc = new List<Contour>();
                List<Segment> ss = new List<Segment>();

                splitContour(c, cc, ss);

                cc.ForEach(contour => splittedContours.Add(contour));
                ss.ForEach(segment => segments.Add(segment));
            }

            redundantSegments = segments;

            // put splitted contours into two collections: clockwise and counterclockwise
            foreach (Contour c in splittedContours)
            {
                ContourData cd = new ContourData()
                {
                    C = c,
                    Orientation = c.GetOrientation(),
                    Area = c.SimpleArea()
                };

                c.Layout = cd.Orientation ==
                    Contour.Orientation.CW ?
                    Contour.ContourLayout.External : Contour.ContourLayout.Internal;

                c.Reverse();

                if (cd.Orientation == Contour.Orientation.CW)
                    cwContours.Add(cd);
                else
                    ccwContours.Add(cd);
            }

            // sort contours by area
            cwContours.Sort((cd1, cd2) => cd1.Area == cd2.Area ? 0 : (cd1.Area < cd2.Area ? 1 : -1));
            ccwContours.Sort((cd1, cd2) => cd1.Area == cd2.Area ? 0 : (cd1.Area < cd2.Area ? 1 : -1));

            // put external contours to the resulting collection
            foreach (ContourData cd in cwContours)
                result.Add(new Polygon(new Contour[] { cd.C }));

            // assign internal contours to the resulting collection
            for(int i = ccwContours.Count - 1; i >= 0; i--)
            {
                ContourData currentHole = ccwContours[i];
                ICoordinate holeInteriorPoint = 
                    (new Polygon(new Contour[] { currentHole.C })).PointOnSurface();

                for (int j = result.Count - 1; j >= 0; j--)
                {
                    if (cwContours[j].Area > currentHole.Area &&
                        result[j].Contours[0].PointLiesInside(holeInteriorPoint))
                    {
                        // tricky case:
                        // area comparisions may produce wrong results for topologically equal contours
                        // so, check the equality directly 
                        if (result[j].Contours[0].Vertices.Count == currentHole.C.Vertices.Count &&
                            areEqualContours(result[j].Contours[0], currentHole.C))
                        {
                            continue;
                        }

                        result[j].Contours.Add(currentHole.C);
                        break;
                    }
                }
            }

            return result;
        }

        private bool areEqualContours(Contour c1, Contour c2)
        {
            ICoordinate start = c1.Vertices[0];
            int f = -1;
            for (int k = c2.Vertices.Count - 1; k >= 0; k-- )
                if (start.ExactEquals(c2.Vertices[k]))
                {
                    f = k;
                    break;
                }

            if(f == -1) return false;

            for (int i = 0; i < c1.Vertices.Count; i++, f--)
            {
                if (f == -1)
                    f = c2.Vertices.Count - 1;

                if (!c1.Vertices[i].ExactEquals(c2.Vertices[f]))
                    return false;
            }

            return true;
        }

        private void splitContour(Contour contour, List<Contour> contours, List<Segment> segments)
        {
            for (int i = 0; i < contour.Vertices.Count; i++)
                segments.Add(new Segment(contour.Vertices[i], i == contour.Vertices.Count - 1 ? contour.Vertices[0] : contour.Vertices[i + 1]));

            bool[] isDegenerate = new bool[segments.Count];
            bool[] degenFlag = new bool[segments.Count];

            for (int i = 0; i < segments.Count - 1; i++)
            {
                for (int j = i + 1; j < segments.Count; j++)
                {
                    if (segments[i].V1.ExactEquals(segments[j].V2) &&
                        segments[i].V2.ExactEquals(segments[j].V1))
                    {
                        isDegenerate[i] = true;
                        isDegenerate[j] = true;
                        degenFlag[i] = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                if (!isDegenerate[i])
                {
                    bool segmentAdded = false;
                    foreach (Contour c in contours)
                        if (c.Vertices[c.Vertices.Count - 1].ExactEquals(segments[i].V1))
                        {
                            c.Vertices.Add(segments[i].V2);
                            segmentAdded = true;
                            break;
                        }

                    if (!segmentAdded)
                    {
                        Contour newContour = new Contour();
                        newContour.Vertices.Add(segments[i].V2);
                        contours.Add(newContour);
                    }
                }
            }

            normalizeContours(contours);

            for (int i = isDegenerate.Length - 1; i >= 0; i--)
                if (!degenFlag[i])
                    segments.RemoveAt(i);


        }

        /// <summary>
        /// Constructs a contour.
        /// </summary>
        private Contour processContour(PlanarGraphEdge startEdge,
                                       bool backward)
        {
            Contour result = new Contour();

            PlanarGraphNode startNode = backward ? startEdge.Node2 : startEdge.Node1;
            result.Vertices.Add(startNode.Point);

            PlanarGraphNode currentNode = backward ? startEdge.Node1 : startEdge.Node2;
            result.Vertices.Add(currentNode.Point);

            // mark first edge
            if ((EdgeUsage)startEdge.Label.Tag != EdgeUsage.None)
            {
                startEdge.Label.Tag = EdgeUsage.Both;
                startEdge.IsVisited = true;
            }
            else
                startEdge.Label.Tag = backward ? EdgeUsage.Backward : EdgeUsage.Forward;

            PlanarGraphEdge currentEdge = startEdge;

            while (true)
            {
                List<PlanarGraphEdge> possibleEdges = new List<PlanarGraphEdge>();
                foreach (PlanarGraphEdge edge in currentNode.IncidentEdges)
                    if (edge.Enabled && edge != currentEdge)
                    {
                        switch ((EdgeUsage)edge.Label.Tag)
                        {
                            case EdgeUsage.Both: break;
                            case EdgeUsage.None:
                                possibleEdges.Add(edge);
                                break;
                            case EdgeUsage.Forward:
                                if (edge.Node2 == currentNode)
                                    possibleEdges.Add(edge);
                                break;
                            case EdgeUsage.Backward:
                                if (edge.Node1 == currentNode)
                                    possibleEdges.Add(edge);
                                break;
                        }
                    }

                double maxAngle = 0; 
                Segment previousEdge = new Segment(currentEdge.Node1.Point, currentEdge.Node2.Point);
                PlanarGraphEdge targetEdge = null;

                if (possibleEdges.Count == 0 && (EdgeUsage)currentEdge.Label.Tag != EdgeUsage.Both)
                    possibleEdges.Add(currentEdge);

                if (possibleEdges.Count == 1)
                    targetEdge = possibleEdges[0];
                else
                    for (int k = 0; k < possibleEdges.Count; k++)
                    {
                        PlanarGraphEdge edge = possibleEdges[k];
                        Segment kEdge = new Segment(edge.Node1.Point, edge.Node2.Point);
                        double angle;
                        try
                        {
                            angle = getAngleBetweenEdges(ref previousEdge, ref kEdge, false);
                        }
                        catch (ArgumentException)
                        {
                            throw new TopologyException();
                        }

                        if (angle >= _twoPi)
                            angle -= _twoPi;

                        if (angle > maxAngle)
                        {
                            maxAngle = angle;
                            targetEdge = edge;
                        }
                    }

                if (targetEdge != null)
                {
                    if (targetEdge.Node1.Point.ExactEquals(currentNode.Point))
                    {
                        if ((EdgeUsage)targetEdge.Label.Tag == EdgeUsage.None)
                            targetEdge.Label.Tag = EdgeUsage.Forward;
                        else
                        {
                            targetEdge.Label.Tag = EdgeUsage.Both;
                            targetEdge.IsVisited = true;
                        }

                        result.Vertices.Add((ICoordinate)targetEdge.Node2.Point.Clone());
                        currentNode = targetEdge.Node2;
                    }
                    else
                    {
                        if ((EdgeUsage)targetEdge.Label.Tag == EdgeUsage.None)
                            targetEdge.Label.Tag = EdgeUsage.Backward;
                        else
                        {
                            targetEdge.Label.Tag = EdgeUsage.Both;
                            targetEdge.IsVisited = true;
                        }

                        result.Vertices.Add((ICoordinate)targetEdge.Node1.Point.Clone());
                        currentNode = targetEdge.Node1;
                    }

                    previousEdge = new Segment(targetEdge.Node1.Point, targetEdge.Node2.Point);
                    currentEdge = targetEdge;

                    if (currentNode == startNode) // пришли, контур сформирован
                    {
                        result.Vertices.RemoveAt(result.Vertices.Count - 1);
                        break;
                    }
                }
                else break;
            }

            return result;
        }

        private void normalizeContours(List<Contour> contours)
        {
            List<Contour> result = new List<Contour>();

            foreach (Contour c in contours)
            {
                List<Contour> normalizedContours = normalizeContour(c, true);
                foreach (Contour nomalizedContour in normalizedContours)
                    result.Add(nomalizedContour);
            }

            contours.Clear();
            foreach (Contour c in result)
                contours.Add(c);
        }

        private List<Contour> normalizeContour(Contour contour, bool heterogenous)
        {
            List<Contour> result = new List<Contour>();

            int i1, i2;

            while (getContourSelfTouchIndicies(contour, out i1, out i2))
            {
                Contour cut = cutSelfTouch(contour, i1, i2, heterogenous);
                List<Contour> normalizedCut = normalizeContour(cut, false);
                foreach (Contour nomalizedContour in normalizedCut)
                    result.Add(nomalizedContour);
            }

            result.Add(contour);
            return result;
        }

        private Contour cutSelfTouch(Contour contour, int i1, int i2, bool heterogenous)
        {
            Contour result = new Contour();
            result.Layout = contour.Layout;

            for (int i = i1; i < i2; i++)
                result.Vertices.Add((ICoordinate)contour.Vertices[i].Clone());

            for (int i = i1; i < i2; i++)
                contour.Vertices.RemoveAt(i1);

            if (heterogenous)
            {
                if (result.Layout == Contour.ContourLayout.External)
                    result.Layout = Contour.ContourLayout.Internal;
                else
                    if (result.Layout == Contour.ContourLayout.Internal)
                        result.Layout = Contour.ContourLayout.External;
            }
            return result;
        }

        private bool getContourSelfTouchIndicies(Contour contour, out int i1, out int i2)
        {
            i1 = 0;
            i2 = 0;
            for (int i = 0; i < contour.Vertices.Count - 1; i++)
                for (int j = i + 1; j < contour.Vertices.Count; j++)
                    if (contour.Vertices[i].ExactEquals(contour.Vertices[j]))
                    {
                        i1 = i;
                        i2 = j;
                        return true;
                    }

            return false;
        }

        private static double _twoPi = Math.PI * 2;
        private static double _halfPI = Math.PI * 0.5;

        /// <summary>
        /// Constructs a contour.
        /// </summary>
        private Contour processContour(int startNodeIndex,
                                       PlanarGraphNode.NodeLayout layout,
                                       bool markObject1EdgesOrientation,
                                       bool markObject2EdgesOrientation)
        {
            Contour result = null;

            PlanarGraphNode startNode = _nodes[startNodeIndex];
            for (int j = 0; j < startNode.IncidentEdges.Count; j++)
            {
                if (!startNode.IncidentEdges[j].IsVisited &&
                    startNode.IncidentEdges[j].Enabled) // инцидентное ребро не пройдено
                {
                    if (result == null)
                        result = new Contour();

                    result.Vertices.Add((ICoordinate)startNode.Point.Clone()); // первая вершина нового контура

                    PlanarGraphNode currentNode = startNode;

                    Segment previousEdge = new Segment();
                    bool edgeDetected = false;

                    List<PlanarGraphEdge> possibleEdges = new List<PlanarGraphEdge>();

                    while (true)
                    {
                        // определение ребра для поворота направо (налево для дырки)
                        possibleEdges.Clear();

                        foreach (PlanarGraphEdge edge in currentNode.IncidentEdges)
                            if (!edge.IsVisited && edge.Enabled)
                                possibleEdges.Add(edge);

                        if (possibleEdges.Count > 0)
                        {
                            if (!edgeDetected)
                            {
                                previousEdge.V1 = (ICoordinate)currentNode.Point.Clone();
                                previousEdge.V2 = (ICoordinate)currentNode.Point.Clone();

                                double l = PlanimetryAlgorithms.Tolerance * 1e10;

                                if (layout == PlanarGraphNode.NodeLayout.Internal) // для 1-го ребра должен определяться угол с вертикалью
                                    previousEdge.V2.Y -= l;
                                else
                                    previousEdge.V2.Y += l;

                                edgeDetected = true;
                            }

                            double maxAngle = 0; // максимальный найденный угол поворота
                            double minAngle = _twoPi; // минимальный найденный угол поворота
                            PlanarGraphEdge targetEdge = null; // целевое ребро

                            if (possibleEdges.Count == 1)
                                targetEdge = possibleEdges[0];
                            else
                            {
                                for (int k = 0; k < possibleEdges.Count; k++) // находим ребро с минимальным углом
                                {
                                    PlanarGraphEdge edge = possibleEdges[k];
                                    Segment kEdge = new Segment(edge.Node1.Point, edge.Node2.Point);
                                    double angle;
                                    try
                                    {
                                        if (result.Vertices.Count < 2)
                                            angle = Math.Min(getAngleBetweenEdges(ref previousEdge, ref kEdge, true),
                                                             getAngleBetweenEdges(ref previousEdge, ref kEdge, false));
                                        else
                                            angle = getAngleBetweenEdges(ref previousEdge, ref kEdge, layout == PlanarGraphNode.NodeLayout.Internal);
                                    }
                                    catch (ArgumentException)
                                    {
                                        throw new TopologyException();
                                    }

                                    if (angle >= _twoPi)
                                        angle -= _twoPi;

                                    if (result.Vertices.Count > 1)
                                    {
                                        if (angle > maxAngle)
                                        {
                                            maxAngle = angle;
                                            targetEdge = edge;
                                        }
                                    }
                                    else // первое ребро - особый случай, ищем минимальный угол
                                    {
                                        if (angle < minAngle)
                                        {
                                            minAngle = angle;
                                            targetEdge = edge;
                                        }
                                    }
                                }
                            }

                            if (targetEdge != null)
                            {
                                if (targetEdge.Node1.Point.ExactEquals(currentNode.Point))
                                {
                                    if (markObject1EdgesOrientation)
                                            targetEdge.OrientationInObject1 = PlanarGraphEdge.EdgeOrientation.Forward;

                                    if (markObject2EdgesOrientation)
                                            targetEdge.OrientationInObject2 = PlanarGraphEdge.EdgeOrientation.Forward;

                                    result.Vertices.Add((ICoordinate)targetEdge.Node2.Point.Clone());
                                    targetEdge.Node2.Layout = layout;
                                    currentNode = targetEdge.Node2;
                                }
                                else
                                {
                                    if (markObject1EdgesOrientation)
                                            targetEdge.OrientationInObject1 = PlanarGraphEdge.EdgeOrientation.Backward;

                                    if (markObject2EdgesOrientation)
                                            targetEdge.OrientationInObject2 = PlanarGraphEdge.EdgeOrientation.Backward;

                                    result.Vertices.Add((ICoordinate)targetEdge.Node1.Point.Clone());
                                    targetEdge.Node1.Layout = layout;
                                    currentNode = targetEdge.Node1;
                                }

                                previousEdge = new Segment(targetEdge.Node1.Point, targetEdge.Node2.Point);
                                targetEdge.IsVisited = true;

                                if (currentNode == startNode) // пришли, контур сформирован
                                {
                                    result.Vertices.RemoveAt(result.Vertices.Count - 1);
                                    break;
                                }
                            }
                            else
                                break;
                        }
                        else
                        {
                            // по какой-то причине контур не может быть завершен
                            throw new TopologyException();
                        }
                    }
                    break;
                }
            }

            return result;
        }

        private void internalBuild(IGeometry geometry1, IGeometry geometry2)
        {
            if (geometry1 != null)
                this._sourceGeometry1 = (IGeometry)geometry1.Clone();
            else
                this._sourceGeometry1 = null;
            if (geometry2 != null)
                this._sourceGeometry2 = (IGeometry)geometry2.Clone();
            else
                this._sourceGeometry2 = null;

            this.build();
        }

        /// <summary>
        /// Gets an origin of snapping grid.
        /// </summary>
        public ICoordinate GridOrigin
        {
            get { return _gridOrigin; }
        }

        /// <summary>
        /// Gets a value indicating whether the snapping should be performed.
        /// </summary>
        public bool PerformSnapping
        {
            get { return _performSnapping; }
        }

        /// <summary>
        /// Builds a planar graph of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>Planar graph of two geometries</returns>
        public static PlanarGraph Build(IGeometry geometry1, IGeometry geometry2)
        {
            PlanarGraph graph = new PlanarGraph();
            graph.internalBuild(geometry1, geometry2);
            return graph;
        }

        /// <summary>
        /// Builds a planar graph of two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <param name="gridOrigin">Snapping grid origin</param>
        /// <returns>Planar graph of two geometries</returns>
        public static PlanarGraph BuildWithSnap(IGeometry geometry1, IGeometry geometry2, ICoordinate gridOrigin)
        {
            PlanarGraph graph = new PlanarGraph();
            graph._gridOrigin = gridOrigin;
            graph._performSnapping = true;
            graph.internalBuild(geometry1, geometry2);
            return graph;
        }

        /// <summary>
        /// Sets enabled state to all graph nodes and edges.
        /// </summary>
        /// <param name="enabled">Enabled state</param>
        public void SetElementsEnabledState(bool enabled)
        {
            foreach (PlanarGraphEdge edge in Edges)
                edge.Enabled = enabled;

            foreach (PlanarGraphNode node in Nodes)
                node.Enabled = enabled;
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.PlanarGraph
        /// </summary>
        internal PlanarGraph()
        {
        }
    }

    /// <summary>
    /// Node of the planar graph.
    /// </summary>
    public class PlanarGraphNode
    {
        private ICoordinate _coordinate = null;
        private List<PlanarGraphEdge> _incidentEdges = new List<PlanarGraphEdge>();

        private TopologyLabel _label = new TopologyLabel();

        private bool _enabled = false;

        /// <summary>
        /// Gets or sets an enabled state of this node.
        /// This value is used to indicate that a node 
        /// is logically deleted from the graph.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// Gets or sets a topology label.
        /// </summary>
        public TopologyLabel Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        /// Layouts of node.
        /// </summary>
        public enum NodeLayout : int
        {
            /// <summary>
            /// Unknown.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Node of exterior contour.
            /// </summary>
            External = 1,
            /// <summary>
            /// Node of interior contour.
            /// </summary>
            Internal = 2,
        }

        /// <summary>
        /// Layout of this node.
        /// </summary>
        public NodeLayout Layout = NodeLayout.Unknown;

        /// <summary>
        /// Gets or sets a list containing the incident edges.
        /// </summary>
        public List<PlanarGraphEdge> IncidentEdges
        {
            get { return _incidentEdges; }
            set { _incidentEdges = value; }
        }

        /// <summary>
        /// Gets a coordinate of  this node.
        /// </summary>
        public ICoordinate Point
        {
            get { return _coordinate; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.PlanarGraphNode.
        /// </summary>
        /// <param name="coordinate">Coordinate of a node</param>
        public PlanarGraphNode(ICoordinate coordinate)
        {
            _coordinate = (ICoordinate)coordinate.Clone();
        }
    }

    /// <summary>
    /// Represents a topology label.
    /// Instances of this class is used to mark a planar graph elements.
    /// </summary>
    public class TopologyLabel 
    {
        private bool _usedByObject1 = false;
        private bool _usedByObject2 = false;

        private int _object1OccurrencesCount = 0;
        private int _object2OccurrencesCount = 0;

        private object _tag = null;

        /// <summary>
        /// An object for custom usage.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the graph element is used by first geometry object.
        /// </summary>
        public bool UsedByObject1
        {
            get { return _usedByObject1; }
            set { _usedByObject1 = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the graph element is used by second geometry object.
        /// </summary>
        public bool UsedByObject2
        {
            get { return _usedByObject2; }
            set { _usedByObject2 = value; }
        }

        /// <summary>
        /// Gets or sets a usage count of the graph element 
        /// by second geometry object.
        /// </summary>
        public int Object2OccurrencesCount
        {
            get { return _object2OccurrencesCount; }
            set { _object2OccurrencesCount = value; }
        }

        /// <summary>
        /// Gets or sets a usage count of the graph element 
        /// by first geometry object.
        /// </summary>
        public int Object1OccurrencesCount
        {
            get { return _object1OccurrencesCount; }
            set { _object1OccurrencesCount = value; }
        }
    }

    /// <summary>
    /// Edge of the planar graph.
    /// </summary>
    public class PlanarGraphEdge
    {
        /// <summary>
        /// Edge orientations relative to the direction of traversal;
        /// </summary>
        public enum EdgeOrientation : int
        {
            /// <summary>
            /// Unknown orientation.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Edge is oriented along the direction of traversal.
            /// </summary>
            Forward = 1,
            /// <summary>
            /// Edge is oriented against the direction of traversal.
            /// </summary>
            Backward = 2
        }

        private PlanarGraphNode _node1;
        private PlanarGraphNode _node2;

        private bool _isVisited = false;
        private bool _enabled = false;

        private TopologyLabel _label = new TopologyLabel();

        /// <summary>
        /// Gets or sets a topology label.
        /// </summary>
        public TopologyLabel Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        /// Computes a coordinate of the center point.
        /// </summary>
        /// <returns>Coordinate of the center point</returns>
        public ICoordinate CenterPoint()
        {
            return (new Segment(Node1.Point, Node2.Point)).Center();
        }

        /// <summary>
        /// Edge orientation relative to the direction of first object traversal;
        /// </summary>
        public EdgeOrientation OrientationInObject1 = EdgeOrientation.Unknown;

        /// <summary>
        /// Edge orientation relative to the direction of second object traversal;
        /// </summary>
        public EdgeOrientation OrientationInObject2 = EdgeOrientation.Unknown;

        /// <summary>
        /// Gets or sets a value indicating whether this edge is visited.
        /// </summary>
        public bool IsVisited
        {
            get { return _isVisited; }
            set { _isVisited = value; }
        }


        /// <summary>
        /// Gets or sets an enabled state of this edge.
        /// This value is used to indicate that an edge
        /// is logically deleted from the graph.
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// Gets first node of this edge.
        /// </summary>
        public PlanarGraphNode Node1
        {
            get { return _node1; }
        }

        /// <summary>
        /// 
        /// Gets second node of this edge.
        /// </summary>
        public PlanarGraphNode Node2
        {
            get { return _node2; }
        }

        /// <summary>
        /// Swap nodes of edges.
        /// </summary>
        internal void SwapNodes()
        {
            PlanarGraphNode node = _node1;
            _node1 = _node2;
            _node2 = node;
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.PlanarGraphEdge
        /// </summary>
        /// <param name="node1">First node</param>
        /// <param name="node2">Second node</param>
        public PlanarGraphEdge(PlanarGraphNode node1, PlanarGraphNode node2)
        {
            _node1 = node1;
            _node2 = node2;
        }
    }
}