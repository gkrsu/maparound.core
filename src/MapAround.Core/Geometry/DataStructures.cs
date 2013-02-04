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
** File: DataStructures.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Data structures commonly used in geometric algorithms
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using MapAround.Indexing;

    /// <summary>
    /// Represents a monotone chain of segments.
    /// All segments in chain are placed in single quadrant
    /// <remarks>
    /// Monotone chain segments are used to avoid a large 
    /// number of segments intersection checks.
    /// </remarks>
    /// </summary>
    public class MonotoneChain : IIndexable
    {
        private BoundingRectangle _boundingRectangle = new BoundingRectangle();
        private List<Segment> _segments = new List<Segment>();
        private List<SegmentLabel> _labels = new List<SegmentLabel>();

        /// <summary>
        /// Orientation of segments in chain.
        /// </summary>
        public enum Orientation 
        {
            /// <summary>
            /// Left up.
            /// </summary>
            LeftUp, 
            /// <summary>
            /// Right up.
            /// </summary>
            RightUp,
            /// <summary>
            /// Right down.
            /// </summary>
            RightDown,
            /// <summary>
            /// Left down.
            /// </summary>
            LeftDown
        }

        private Orientation _orientation;

        private bool checkSegment(Segment segment)
        {
            if (_segments.Count > 0)
            {
                if (!segment.V1.ExactEquals(_segments[_segments.Count - 1].V2) &&
                   !segment.V2.ExactEquals(_segments[0].V1))
                    return false;
            }
            return _orientation == GetSegmentOrientation(segment);
        }

        private void internalInsertSegment(Segment segment, SegmentLabel tag)
        {
            segment = (Segment)segment.Clone();

            _boundingRectangle.Join(segment.V1);
            _boundingRectangle.Join(segment.V2);

            if (_segments.Count > 0)
            {
                if (segment.V2.ExactEquals(_segments[0].V1))
                {
                    _segments.Insert(0, segment);
                    _labels.Insert(0, tag);
                    return;
                }
            }
            _segments.Add(segment);
            _labels.Add(tag);
        }

        private void boundsChanged()
        {
            BoundingRectangle br = new BoundingRectangle();
            if (_segments.Count > 0)
            {
                br.Join(_segments[0].V1);
                br.Join(_segments[0].V2);
                br.Join(_segments[_segments.Count - 1].V1);
                br.Join(_segments[_segments.Count - 1].V2);
            }
            _boundingRectangle = br;
        }

        #region IIndexEntry Members

        /// <summary>
        /// Gets BoundingRectangle of all chain segments.
        /// </summary>
        public BoundingRectangle BoundingRectangle
        {
            get
            {
                return _boundingRectangle;
            }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            MonotoneChain clone = new MonotoneChain(this._orientation);
            foreach (Segment s in this._segments)
                clone._segments.Add(s);

            foreach (SegmentLabel label in this._labels)
                clone._labels.Add(label);

            clone._boundingRectangle = (BoundingRectangle)this._boundingRectangle.Clone();

            return clone;
        }

        #endregion

        /// <summary>
        /// Gets a collection of chain segments.
        /// </summary>
        public ReadOnlyCollection<Segment> Segments
        {
            get { return _segments.AsReadOnly(); }
        }

        /// <summary>
        /// Gets a collection of labels associated with chain segments.
        /// </summary>s
        public ReadOnlyCollection<SegmentLabel> Labels
        {
            get { return _labels.AsReadOnly(); }
        }

        /// <summary>
        /// Gets a coordinate of first point in chain.
        /// </summary>
        public ICoordinate FirstPoint
        {
            get 
            {
                if (_segments.Count > 0)
                    return _segments[0].V1;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets a coordinate of last point in chain.
        /// </summary>
        public ICoordinate LastPoint
        {
            get
            {
                if (_segments.Count > 0)
                    return _segments[_segments.Count - 1].V2;
                else
                    return null;
            }
        }

        /// <summary>
        /// Computes an orientation of specified segment.
        /// </summary>
        /// <param name="segment">Segment to compute orientation</param>
        /// <returns>An object representing segment orientation</returns>
        public static Orientation GetSegmentOrientation(Segment segment)
        {
            if (segment.V1.ExactEquals(segment.V2))
                throw new ArgumentException("Singular segment", "segment");

            // right
            if (segment.V1.X <= segment.V2.X)
            {
                if (segment.V1.Y <= segment.V2.Y)
                    // up
                    return Orientation.RightUp;
                else
                    // down
                    return Orientation.RightDown;
            } // left
            else
            {
                if (segment.V1.Y < segment.V2.Y)
                    // up
                    return Orientation.LeftUp;
                else
                    // down
                    return Orientation.LeftDown;
            }
        }

        /// <summary>
        /// Adds a segment to the chain.
        /// </summary>
        /// <param name="segment">The segment to add</param>
        /// <returns>true, if the segment was added, false otherwise</returns>
        public bool InsertSegment(Segment segment)
        {
            if (checkSegment(segment))
            {
                internalInsertSegment(segment, new SegmentLabel());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a segment to the chain.
        /// </summary>
        /// <param name="segment">The segment to add</param>
        /// <param name="label">The label of the segment</param>
        /// <returns>true, if the segment was added, false otherwise</returns>
        public bool InsertSegment(Segment segment, SegmentLabel label)
        {
            if (checkSegment(segment))
            {
                internalInsertSegment(segment, label);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Replaces the label associated with segment having specified index in the chain.
        /// </summary>
        /// <param name="index">The index of segment in the chain</param>
        /// <param name="newValue">Instance of label</param>
        public void ReplaceLabel(int index, SegmentLabel newValue)
        {
            if (index > 0 || index < _labels.Count)
                _labels[index] = newValue;
            else
                throw new ArgumentOutOfRangeException("The index should not be negative and smaller than the size of the collection", "index");
        }

        /// <summary>
        /// Determines whether the bounding rectangle of this chain 
        /// intersects the bounding rectangle of specified chain.
        /// </summary>
        /// <param name="chain">Monotone chain</param>
        /// <returns>true, if the bounding rectangle of this chain 
        /// intersects the bounding rectangle of specified chain, false otherwise</returns>
        public bool BoundsIntersect(MonotoneChain chain)
        {
            if (this._boundingRectangle.IsEmpty() || chain._boundingRectangle.IsEmpty())
                return false;

            if (this._boundingRectangle.Intersects(chain._boundingRectangle))
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether the bounding rectangle of this 
        /// monotone chain contains point.
        /// </summary>
        /// <param name="point">Point coordinate</param>
        /// <returns>true, if the bounding rectangle of this monotone chain contains point, false otherwise</returns>
        public bool BoundsContainPoint(ICoordinate point)
        {
            if (this._boundingRectangle.IsEmpty())
                return false;

            if (this._boundingRectangle.ContainsPoint(point))
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the intersection points of two chains.
        /// </summary>
        /// <param name="chain">Monotone chain</param>
        /// <returns>A list containing coordinates of intersections</returns>
        public List<ICoordinate> GetCrossPoints(MonotoneChain chain)
        {
            List<ICoordinate> result = new List<ICoordinate>();

            if (BoundsIntersect(chain))
            {
                Segment stub = new Segment();
                ICoordinate crossPoint = null;

                foreach (Segment s1 in this._segments)
                    foreach (Segment s2 in chain._segments)
                    {
                        Dimension crossKind = PlanimetryAlgorithms.RobustSegmentsIntersection(s1, s2, out crossPoint, out stub);
                        if (crossKind == Dimension.Zero)
                            result.Add(crossPoint);
                    }
            }
                        
            return result;
        }

        /// <summary>
        /// Determines whether this chain crosses with other.
        /// </summary>
        /// <param name="chain">Chain</param>
        /// <returns>true, if this chain crosses with the specified chain, false otherwise</returns>
        public bool CrossesWith(MonotoneChain chain)
        {
            if (BoundsIntersect(chain))
            {
                Segment stub = new Segment();
                ICoordinate crossPoint = null;

                foreach (Segment s1 in this._segments)
                    foreach (Segment s2 in chain._segments)
                    {
                        Dimension crossKind = PlanimetryAlgorithms.RobustSegmentsIntersection(s1, s2, out crossPoint, out stub);
                        if (crossKind == Dimension.Zero)
                            return true;
                    }
            }

            return false;
        }

        /// <summary>
        /// Computes the 2D intersections of two chains.
        /// </summary>
        /// <param name="chain">Monotone chain</param>
        /// <returns>A list containing segments that represent 2D intersections of chain</returns>
        public List<Segment> GetCrossSegments(MonotoneChain chain)
        {
            List<Segment> result = new List<Segment>();

            if (BoundsIntersect(chain))
            {
                Segment crossSegment = new Segment();
                ICoordinate stub = null;

                foreach (Segment s1 in this._segments)
                    foreach (Segment s2 in chain._segments)
                    {
                        Dimension crossKind = PlanimetryAlgorithms.RobustSegmentsIntersection(s1, s2, out stub, out crossSegment);
                        if (crossKind == Dimension.One)
                            result.Add(crossSegment);
                    }
            }

            return result;
        }

        /// <summary>
        /// Removes all segments from chain which length is less than specified. 
        /// The remaining segments are connected.
        /// </summary>
        /// <param name="minLength">The minimum length of segment</param>
        public void ReduceSegments(double minLength)
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
                if (_segments[i].Length() < minLength)
                {
                    if (i > 0)
                        _segments[i - 1] = new Segment(_segments[i - 1].V1, _segments[i].V2);

                    _segments.RemoveAt(i);
                    _labels.RemoveAt(i);
                }

            boundsChanged();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MonotoneChain"/>.
        /// </summary>
        /// <param name="orientation">Orientation of segments</param>
        public MonotoneChain(Orientation orientation)
        { 
            _orientation = orientation;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MonotoneChain"/>.
        /// </summary>
        /// <param name="segment">An initial segment</param>
        public MonotoneChain(Segment segment)
        {
            _orientation = GetSegmentOrientation(segment);
            internalInsertSegment(segment, new SegmentLabel());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MonotoneChain"/>.
        /// </summary>
        /// <param name="segment">An initial segment</param>
        /// <param name="tag">A label of initial segment</param>
        public MonotoneChain(Segment segment, SegmentLabel tag)
        {
            _orientation = GetSegmentOrientation(segment);
            internalInsertSegment(segment, tag);
        }

        private BoundingRectangle getSubChainBounds(int index1, int index2)
        {
            Segment s1 = _segments[index1];
            Segment s2 = _segments[index2];

            switch (_orientation)
            { 
                case Orientation.RightUp:
                    return new BoundingRectangle(s1.V1, s2.V2);
                case Orientation.RightDown:
                    return new BoundingRectangle(s1.V1.X, s2.V2.Y, s2.V2.X, s1.V1.Y);
                case Orientation.LeftUp:
                    return new BoundingRectangle(s2.V2.X, s1.V1.Y, s1.V1.X, s2.V2.Y);
                case Orientation.LeftDown:
                    return new BoundingRectangle(s2.V2, s1.V1);
            }

            return new BoundingRectangle();
        }

        private bool isPointInSegmentsBounds(ICoordinate point, int minIndex, int maxIndex)
        {
            while (minIndex != maxIndex)
            {
                int middleIndex = minIndex + (maxIndex - minIndex) / 2;
                BoundingRectangle br1 = getSubChainBounds(minIndex, middleIndex);
                BoundingRectangle br2 = getSubChainBounds(middleIndex + 1, maxIndex);
                if (br1.ContainsPoint(point))
                    maxIndex = middleIndex;
                else if (br2.ContainsPoint(point))
                    minIndex = middleIndex + 1;
                else
                    return false;
            }

            return _segments[minIndex].GetBoundingRectangle().ContainsPoint(point);
        }

        /// <summary>
        /// Defines if a specified point lies into the 
        /// bounding rectangle of any segment of chain.
        /// </summary>
        /// <param name="point">Coordinate of point</param>
        /// <returns>true, if a specified point lies into the 
        /// bounding rectangle of any segment of chain, false otherwise</returns>
        public bool IsPointInSegmentsBounds(ICoordinate point)
        {
            return isPointInSegmentsBounds(point, 0, _segments.Count - 1);
        }

        /// <summary>
        /// Splits the segments of the chain at the specified points.
        /// </summary>
        /// <param name="list">A list contatinig points where you need to split the chain</param>
        /// <returns>true, if se segment was splitted, false otherwise</returns>
        public bool Split(List<ICoordinate> list)
        {
            if (list.Count == 0)
                return false;

            PlanimetryAlgorithms.SortCoordsHorizontally(list);
            List<Segment> newSegments = new List<Segment>();
            List<SegmentLabel> newLabels = new List<SegmentLabel>();

            List<Segment> splittedSegments = new List<Segment>();
            int objectIndex = this.Labels[0].ObjectIndex;
            int sequenceEndex = this.Labels[0].SequenceIndex;

            double tolerance = PlanimetryAlgorithms.Tolerance;

            bool hasSplits = false;

            int k = 0;
            foreach (Segment s in _segments)
            {
                bool wasSplitted = false;

                List<ICoordinate> splitPoints = null;
                List<SegmentLabel> splittedLabels = null;
                BoundingRectangle br = s.GetBoundingRectangle();

                for(int i = 0; i < list.Count; i++)
                {
                    if(br.ContainsPoint(list[i]))
                        if (PlanimetryAlgorithms.DistanceToSegment(list[i], s) < tolerance &&
                            PlanimetryAlgorithms.Distance(list[i], s.V1) > tolerance &&
                            PlanimetryAlgorithms.Distance(list[i], s.V2) > tolerance)
                        {
                            wasSplitted = true;

                            if (splitPoints == null)
                            {
                                splitPoints = new List<ICoordinate>();
                                splittedLabels = new List<SegmentLabel>();
                            }

                            splitPoints.Add(list[i]);
                            splittedLabels.Add(new SegmentLabel(objectIndex, sequenceEndex, _labels[k].IndexInSequence));
                        }
                }

                if (!wasSplitted)
                {
                    newSegments.Add(s);
                    newLabels.Add(Labels[k]);
                }
                else
                {
                    hasSplits = true;
                    splitPoints.Add(s.V1);
                    splittedLabels.Add(new SegmentLabel(objectIndex, sequenceEndex, _labels[k].IndexInSequence));
                    splitPoints.Add(s.V2);
                    splittedLabels.Add(new SegmentLabel(objectIndex, sequenceEndex, _labels[k].IndexInSequence));
                    PlanimetryAlgorithms.OrderPointsOverSegment(splitPoints, s);
                    if (splitPoints[0].ExactEquals(s.V1))
                    {
                        for (int i = 0; i < splitPoints.Count - 1; i++)
                        {
                            newSegments.Add(new Segment(splitPoints[i], splitPoints[i + 1]));
                            newLabels.Add(splittedLabels[i]);
                        }
                    }
                    else
                    {
                        for (int i = splitPoints.Count - 1; i > 0; i--)
                        {
                            newSegments.Add(new Segment(splitPoints[i], splitPoints[i - 1]));
                            newLabels.Add(splittedLabels[i]);
                        }
                    }
                }
                k++;
            }

            if (hasSplits)
            {
                _segments.Clear();
                _labels.Clear();
                for (int i = 0; i < newSegments.Count; i++)
                    internalInsertSegment(newSegments[i], newLabels[i]);
            }

            return hasSplits;
        }
    }

    /// <summary>
    /// Represents an object for labeling segment in the monotone chain.
    /// </summary>
    public struct SegmentLabel
    {
        private int _objectIndex;
        private int _sequenceIndex;
        private int _indexInSequence;

        /// <summary>
        /// Index in the coordinate sequence.
        /// </summary>
        public int IndexInSequence
        {
            get { return _indexInSequence; }
        }

        /// <summary>
        /// Object index.
        /// </summary>
        public int ObjectIndex
        {
            get { return _objectIndex; }
        }

        /// <summary>
        /// Index of coordinate sequence.
        /// </summary>
        public int SequenceIndex
        {
            get { return _sequenceIndex; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SegmentLabel"/>.
        /// </summary>
        /// <param name="objectIndex"></param>
        /// <param name="sequenceIndex"></param>
        /// <param name="indexInSequence"></param>
        public SegmentLabel(int objectIndex, int sequenceIndex, int indexInSequence)
        {
            _objectIndex = objectIndex;
            _sequenceIndex = sequenceIndex;
            _indexInSequence = indexInSequence;
        }
    }

    /// <summary>
    /// Calculates a distances.
    /// This class allows to find the Euclidean distance between 
    /// two plane geometries .
    /// Supports Points, MultiPoints, Polylines and Polygons.
    /// All other geometries, for example, the bounding rectangle 
    /// or the line segment should be converted to these four.
    /// </summary>
    public static class DistanceCalculator
    {
        private static void checkGeometry(IGeometry geometry)
        {
            if (!(geometry is PointD) && 
                !(geometry is Polyline) && 
                !(geometry is Polygon) && 
                !(geometry is MultiPoint))
                throw new NotSupportedException("Distance calculation for \"" + geometry.GetType().FullName  + "\" is not supported");
        }

        private static bool hasPointInside(Polygon polygon, List<ICoordinate> points)
        {
            foreach (ICoordinate point in points)
                if (polygon.ContainsPoint(point))
                    return true;

            return false;
        }

        private static List<MonotoneChain> getGeometryChains(IGeometry geometry)
        {
            if (geometry is Polygon)
            {
                Polygon p = geometry as Polygon;
                List<MonotoneChain> chains = new List<MonotoneChain>();

                foreach (Contour c in p.Contours)
                {
                    c.ReduceSegments(PlanimetryAlgorithms.Tolerance);
                    c.AppendMonotoneChains(chains);
                }

                return chains;
            }

            if (geometry is Polyline)
            {
                Polyline p = geometry as Polyline;
                List<MonotoneChain> chains = new List<MonotoneChain>();

                foreach (LinePath path in p.Paths)
                {
                    path.ReduceSegments(PlanimetryAlgorithms.Tolerance);
                    path.AppendMonotoneChains(chains);
                }

                return chains;
            }

            return null;
        }

        private static double calculateSegmentsPointsDistance(List<MonotoneChain> chains, 
                                                              List<ICoordinate> points,
                                                              double minDistance,
                                                              double threshold)
        {
            foreach (MonotoneChain chain in chains)
                foreach (Segment s in chain.Segments)
                    foreach (ICoordinate p in points)
                    {
                        if (p.X > Math.Max(s.V1.X, s.V2.X) + minDistance)
                            break;
                        double d = PlanimetryAlgorithms.DistanceToSegment(p, s);
                        if (d < minDistance)
                            minDistance = d;
                        if (minDistance <= threshold)
                            return minDistance;
                    }

            return minDistance;
        }

        private static double calculateDistanceBrutForce(IGeometry geometry1, 
                                                         IGeometry geometry2, 
                                                         List<ICoordinate> points1, 
                                                         List<ICoordinate> points2,
                                                         double threshold)
        {
            List<MonotoneChain> chains1 = getGeometryChains(geometry1);
            List<MonotoneChain> chains2 = getGeometryChains(geometry2);

            // If both pieces contain sections, they can overlap
            // if the segments intersect - the distance between the figures of zero
            if ((geometry1 is Polygon || geometry1 is Polyline) &&
                (geometry2 is Polygon || geometry2 is Polyline))
            {
                foreach (MonotoneChain chain1 in chains1)
                    foreach (MonotoneChain chain2 in chains2)
                        if (chain1.BoundsIntersect(chain2) && 
                            chain1.CrossesWith(chain2))
                                return 0;
            }

            PlanimetryAlgorithms.SortCoordsHorizontally(points1);
            PlanimetryAlgorithms.SortCoordsHorizontally(points2);

            double minDistance = double.MaxValue;

            // handle the distance from the segments of the first figure to the points in the second
            if (geometry1 is Polygon || geometry1 is Polyline)
            {
                minDistance = calculateSegmentsPointsDistance(chains1, points2, minDistance, threshold);
                if (minDistance <= threshold)
                    return minDistance;
            }

            // handle the distance from the segments of the second figure to the points of the first
            if (geometry2 is Polygon || geometry2 is Polyline)
            {
                minDistance = calculateSegmentsPointsDistance(chains2, points1, minDistance, threshold);
                if (minDistance <= threshold)
                    return minDistance;
            }

            // process the distance between the points of the two figures
            foreach(ICoordinate p1 in points1)
                foreach (ICoordinate p2 in points2)
                {
                    if (p1.X < p2.X - minDistance) continue;
                    if (p2.X > p1.X + minDistance) break;
                    double d = PlanimetryAlgorithms.Distance(p1, p2);
                    if (d < minDistance)
                        minDistance = d;
                    if (minDistance <= threshold)
                        return minDistance;
                }

            return minDistance;
        }

        /// <summary>
        /// Calculates the Euclidean distance between two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// /// <param name="threshold">The threshold value of the distance 
        /// at which a search is terminated</param>
        /// <returns>A distance between geometries (less than or equal to the threshold value)</returns>
        public static double EuclideanDistance(IGeometry geometry1, IGeometry geometry2, double threshold)
        {
            if (threshold < 0)
                throw new ArgumentOutOfRangeException("threshold");

            double result = 0;
            checkGeometry(geometry1);
            checkGeometry(geometry2);

            List<ICoordinate> points1 = new List<ICoordinate>(geometry1.ExtractCoordinates());
            List<ICoordinate> points2 = new List<ICoordinate>(geometry2.ExtractCoordinates());

            if (geometry1 is Polygon || geometry2 is Polygon)
            {
                if (geometry1.GetBoundingRectangle().Intersects(geometry2.GetBoundingRectangle()))
                    if (geometry1 is Polygon)
                    {
                        if (hasPointInside((Polygon)geometry1, points2))
                            return 0;
                    }
                    else
                    {
                        if (hasPointInside((Polygon)geometry2, points1))
                            return 0;
                    }
            }

            result = calculateDistanceBrutForce(geometry1, geometry2, points1, points2, threshold);

            return result;
        }


        /// <summary>
        /// Computes the minimum Euclidean distance between two geometries.
        /// </summary>
        /// <param name="geometry1">First geometry</param>
        /// <param name="geometry2">Second geometry</param>
        /// <returns>The minimum Euclidean distance between two geometries</returns>
        public static double MinimumEuclideanDistance(IGeometry geometry1, IGeometry geometry2)
        {
            return EuclideanDistance(geometry1, geometry2, 0);
        }
    }
}