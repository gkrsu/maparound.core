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
** File: Voronoi.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Building of the Voronoi tessellation and Delaunay triangulation
**
=============================================================================*/

#if !DEMO

namespace MapAround.Geometry.Tessellations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// The MapAround.Geometry.Tesselations namespace contains interfaces and classes
    /// representing tessellations of space.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Represents Voronoi tessellation and 
    /// the dual structure - Delaunay triangulation.
    /// </summary>
    public class VoronoiTesselation
    {
        List<VoronoiCell> _cells = null;
        List<Triangle> _triangles = null;

        /// <summary>
        /// Gets a collection of cells of this tessellation.
        /// </summary>
        public ReadOnlyCollection<VoronoiCell> Cells
        {
            get { return _cells.AsReadOnly(); }
        }

        /// <summary>
        /// Gets a collection of triangles forming the Delaunay triangulation.
        /// </summary>
        public ReadOnlyCollection<Triangle> Triangles
        {
            get { return _triangles.AsReadOnly(); }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tesselations.VoronoiTesselation.
        /// </summary>
        /// <param name="cells">A list containing cells of the tessellation</param>
        /// <param name="triangles">A list containing the Delaunay triangles</param>
        internal VoronoiTesselation(List<VoronoiCell> cells, List<Triangle> triangles)
        {
            _cells = cells;
            _triangles = triangles;
        }
    }

    /// <summary>
    /// Represents a cell in the Voronoi tessellation.
    /// </summary>
    public class VoronoiCell
    {
        private ICoordinate _dataPoint = null;
        List<VoronoiEdge> _edges = new List<VoronoiEdge>();
        private object _tag = null;

        /// <summary>
        /// Adds an edge to the cell.
        /// </summary>
        /// <param name="edge">An edge to add</param>
        internal void AddEdge(VoronoiEdge edge)
        {
            if (edge.Cell1 != this && edge.Cell2 != this)
                throw new ArgumentException("Edge does not contain a reference to this cell", "edge");

            _edges.Add(edge);
        }

        /// <summary>
        /// Removes an edge from the cell.
        /// </summary>
        /// <param name="edge">An edge to remove</param>
        internal void RemoveEdge(VoronoiEdge edge)
        {
            if (_edges.Contains(edge))
                _edges.Remove(edge);
            else
                throw new ArgumentException("Cell does not contain this edge", "edge");
        }

        /// <summary>
        /// Gets or sets a custom object associated with this cell.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets a collection of edges of this cell.
        /// </summary>
        public ReadOnlyCollection<VoronoiEdge> Edges
        {
            get { return _edges.AsReadOnly(); }
        }

        /// <summary>
        /// Gats a value indicating whether this cell contains an infinit edge.
        /// </summary>
        public bool IsInfinit
        {
            get 
            {
                foreach (VoronoiEdge edge in _edges)
                    if (edge.IsInfinit)
                        return true;

                return false; 
            }
        }

        /// <summary>
        /// The point this cell was built for.
        /// </summary>
        public ICoordinate DataPoint
        {
            get { return _dataPoint; }
            set { _dataPoint = value; }
        }
    }

    /// <summary>
    /// Represents an edge of the Voronoi tessellation.
    /// </summary>
    public class VoronoiEdge
    {
        private VoronoiNode _node1 = null;
        private VoronoiNode _node2 = null;
        private VoronoiCell _cell1;
        private VoronoiCell _cell2;
        private object _tag = null;

        /// <summary>
        /// Gets or sets a custom object associated with this edge.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets or sets first node of this edge.
        /// </summary>
        public VoronoiNode Node1
        {
            get { return _node1; }
            set { _node1 = value; }
        }

        /// <summary>
        /// Gets or sets second node of this edge.
        /// </summary>
        public VoronoiNode Node2
        {
            get { return _node2; }
            set { _node2 = value; }
        }

        /// <summary>
        /// Gats a value indicating whether this edge is infinit.
        /// </summary>
        public bool IsInfinit
        {
            get 
            { 
                return Node1.IsInfinit || Node2.IsInfinit; 
            }
        }

        /// <summary>
        /// Gats a first Voronoi cell including this edge.
        /// </summary>
        public VoronoiCell Cell1
        {
            get { return _cell1; }
        }

        /// <summary>
        /// Gats a second Voronoi cell including this edge.
        /// </summary>
        public VoronoiCell Cell2
        {
            get { return _cell2; }
        }

        /// <summary>
        /// Initializes a new instance of  MapAround.Geometry.Tesselations.VoronoiEdge.
        /// </summary>
        /// <param name="cell1">First cell</param>
        /// <param name="cell2">Second cell</param>
        public VoronoiEdge(VoronoiCell cell1, VoronoiCell cell2)
        {
            if (cell1 == null)
                throw new ArgumentNullException("cell1");

            if (cell2 == null)
                throw new ArgumentNullException("cell2");

            _cell1 = cell1;
            _cell2 = cell2;
        }
    }

    /// <summary>
    /// Represents a node of Voronoi tesselation.
    /// Node is an ednpoint of any VoronoiEdge or 
    /// an infinit dangle of edge.
    /// </summary>
    public class VoronoiNode
    {
        private ICoordinate _point = null;
        private bool _isInfinity = false;
        private object _tag = null;

        /// <summary>
        /// Gets or sets a custom object associated with this node.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gats a value indicating whether this node 
        /// represents an infinit dangle of edge.
        /// </summary>
        public bool IsInfinit
        {
            get { return _isInfinity; }
            set { _isInfinity = value; }
        }

        /// <summary>
        /// Gets or sets a value determining a location of this node
        /// or a direction of the edge (if it is infinit)
        /// </summary>
        public ICoordinate Point
        {
            get { return _point; }
            set { _point = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tesselations.VoronoiNode
        /// </summary>
        public VoronoiNode(double x, double y)
        {
            _point = PlanimetryEnvironment.NewCoordinate(x, y);

            _point.X = x;
            _point.Y = y;
        }
    }

    /// <summary>
    /// Represents a triangle.
    /// </summary>
    public class Triangle
    {
        private VoronoiCell _cell1 = null;
        private VoronoiCell _cell2 = null;
        private VoronoiCell _cell3 = null;

        /// <summary>
        /// Gets or sets a custom object associated with this triangle.
        /// </summary>
        public object Tag = null;

        /// <summary>
        /// The first cell of the tessellation, the center 
        /// of which is the vertex of the triangle.
        /// </summary>
        public VoronoiCell Cell1
        {
            get { return _cell1; }
        }

        /// <summary>
        /// The second cell of the tessellation, the center 
        /// of which is the vertex of the triangle.
        /// </summary>
        public VoronoiCell Cell2
        {
            get { return _cell2; }
        }

        /// <summary>
        /// The third cell of the tessellation, the center 
        /// of which is the vertex of the triangle.
        /// </summary>
        public VoronoiCell Cell3
        {
            get { return _cell3; }
        }

        /// <summary>
        /// Initializes a new instance of  MapAround.Geometry.Tesselations.Triangle.
        /// </summary>
        /// <param name="cell1">First cell</param>
        /// <param name="cell2">Second cell</param>
        /// <param name="cell3">Third sell</param>
        internal Triangle(VoronoiCell cell1, VoronoiCell cell2, VoronoiCell cell3)
        {
            _cell1 = cell1;
            _cell2 = cell2;
            _cell3 = cell3;
        }
    }

    /// <summary>
    /// Represents an event in Fortune algorithm.
    /// </summary>
    internal abstract class FortuneEvent
    {
        internal enum EventKind 
        { 
            /// <summary>
            /// A point event.
            /// </summary>
            Point,
            /// <summary>
            /// A circle event.
            /// </summary>
            Circle
        }

        private EventKind _kind = EventKind.Point;

        /// <summary>
        /// Sets a type of event.
        /// </summary>
        /// <param name="kind">Type of event.</param>
        protected void SetKind(EventKind kind)
        {
            _kind = kind;
        }

        /// <summary>
        /// A kind of this event.
        /// </summary>
        internal EventKind Kind
        {
            get { return _kind; }
        }

        /// <summary>
        /// An occurence location of this event.
        /// </summary>
        internal ICoordinate Point = PlanimetryEnvironment.NewCoordinate(0, 0);
    }

    /// <summary>
    /// Represents a point event in Fortune algorithm.
    /// </summary>
    internal class FortunePointEvent : FortuneEvent
    {
        /// <summary>
        /// The cell of Voronoi tessellation.
        /// </summary>
        internal VoronoiCell Cell = null;

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tesselations.FortunePointEvent.
        /// </summary>
        internal FortunePointEvent()
        {
            SetKind(EventKind.Point);
        }
    }

    /// <summary>
    /// Represents a circle event in Fortune algorithm.
    /// </summary>
    internal class FortuneCircleEvent : FortuneEvent
    {
        /// <summary>
        /// Center of circle.
        /// </summary>
        internal ICoordinate CircleCenter = null;

        /// <summary>
        /// Delaunay triangle.
        /// This field is used only if when 
        /// constructed Delaunay triangulation.
        /// </summary>
        internal Triangle Triangle = null;

        /// <summary>
        /// A Fortune arc.
        /// </summary>
        internal FortuneArc Arc = null;

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tesselations.FortuneCircleEvent.
        /// </summary>
        internal FortuneCircleEvent()
        {
            SetKind(EventKind.Circle);
        }
    }

    /// <summary>
    /// Represents an arc in Fortune algorithm
    /// </summary>
    internal class FortuneArc
    {
        private FortuneArc _left = null;
        private FortuneArc _right = null;
        private FortuneArc _parent = null;

        internal FortuneSite Site = new FortuneSite();
        internal FortuneEvent CircleEvent = null;
        internal VoronoiNode LeftNode = null;
        internal VoronoiNode RightNode = null;

        internal FortuneArc Left
        {
            get { return _left; }
            set 
            { 
                _left = value;
                if(value != null)
                    value.Parent = this;
            }
        }

        internal FortuneArc Right
        {
            get { return _right; }
            set 
            { 
                _right = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        internal FortuneArc Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        ///Gets the right arc in the left subtree.
        /// </summary>
        internal FortuneArc LeftSubtreeBound
        {
            get 
            {
                FortuneArc result = _left;
                if (result != null)
                    while (result._right != null)
                        result = result._right;
                return result; 
            }
        }

        /// <summary>
        /// Gets the leftmost arc in the right subtree.
        /// </summary>
        internal FortuneArc RightSubtreeBound
        {
            get
            {
                FortuneArc result = _right;
                if (result != null)
                    while (result._left != null)
                        result = result._left;
                return result;
            }
        }

        /// <summary>
        /// Gets the left next to the arc of coastline.
        /// </summary>
        internal FortuneArc LeftNeighbor
        {
            get
            {
                FortuneArc result = _left;
                if (result != null)
                {
                    while (result._right != null)
                        result = result._right;
                }
                else
                {
                    if (_parent == null)
                        return null;

                    if (_parent._right == this)
                        return _parent;

                    result = this;
                    while (result._parent != null)
                    {
                        if (result._parent._left == result)
                            result = result._parent;
                        else
                            if (result._parent._right == result)
                                return result._parent;
                    }

                    return null;
                }
                return result;
            }
        }

        /// <summary>
        ///Gets right to the next arc of coastline.
        /// </summary>
        internal FortuneArc RightNeighbor
        {
            get
            {
                FortuneArc result = _right;
                if (result != null)
                {
                    while (result._left != null)
                        result = result._left;
                    
                }
                else
                {
                    if (_parent == null)
                        return null;

                    if (_parent._left == this)
                        return _parent;

                    result = this;
                    while (result._parent != null)
                    {
                        if (result._parent._right == result)
                            result = result._parent;
                        else
                            if (result._parent._left == result)
                                return result._parent;
                    }

                    return null;
                }
                return result;
            }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tessellations.FortuneArc
        /// </summary>
        internal FortuneArc()
        { 
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.Tessellations.FortuneArc
        /// </summary>
        internal FortuneArc(FortuneArc left, FortuneArc right)
        {
            _left = left;
            _right = right;
        }
    }

    /// <summary>
    /// Represents a site in Fortune algorithm.
    /// </summary>
    internal class FortuneSite
    {
        internal VoronoiCell Cell = null;
    }

    /// <summary>
    /// Represents a shore line in Fortune algorithm.
    /// </summary>
    internal class FortuneShoreLine
    {
        internal FortuneArc Root = null;

        private static ICoordinate[] getArcIntersections(ICoordinate p1, ICoordinate p2, double ly)
        {
            if (p1.X > p2.X)
            {
                ICoordinate p = p1;
                p1 = p2;
                p2 = p;
            }

            if (ly == p2.Y)
                return new ICoordinate[] { PlanimetryEnvironment.NewCoordinate((p1.X + p2.X) / 2, double.PositiveInfinity) };

            double f = (ly - p1.Y) / (ly - p2.Y);
            double a = 1 - f;
            double lpow2 = ly * ly;
            double yn = lpow2 - p2.X * p2.X - p2.Y * p2.Y;
            if (a == 0)
            {
                double x = 0.5 * (p1.X + p2.X);
                double y = (yn - x * x + 2 * p2.X * x) / (ly - p2.Y);
                return new ICoordinate[] { PlanimetryEnvironment.NewCoordinate(x, y) };
            }
            double b = 2 * (p2.X * f - p1.X);
            double c = f * yn + p1.X * p1.X + p1.Y * p1.Y - lpow2;

            double d = b * b - 4 * a * c;
            if (d < 0)
                // no real roots
                return new ICoordinate[] { };
            else
            {
                double sb = b == 0 ? 1 : Math.Sign(b);

                // This "trick" to avoid loss of accuracy in the calculation 
                // of one of the roots due to the subtraction of large numbers
                double x1 = 0.5 * (-b + sb * Math.Sqrt(d)) / a;
                double x2 = (x1 != 0 ? c / a / x1 : 0);

                double p1ypow2 = p1.Y * p1.Y;
                double den = 2 * (p1.Y - ly);

                double y1 = (p1ypow2 + (p1.X - x1) * (p1.X - x1) - lpow2) / den;
                double y2 = (p1ypow2 + (p1.X - x2) * (p1.X - x2) - lpow2) / den;

                return new ICoordinate[] 
                    { 
                        PlanimetryEnvironment.NewCoordinate(x1, y1), 
                        PlanimetryEnvironment.NewCoordinate(x2, y2) 
                    };
            }
        }

        private double getDistanceToArc(ICoordinate arcPoint, double ly, double x)
        {
            double qy = ly + arcPoint.Y + (arcPoint.X - x) * 0.5 * (arcPoint.X - x) / (ly - arcPoint.Y);
            return qy - ly;
        }

        public FortuneArc SplitArc(FortuneArc splittingArc, FortuneArc splitter)
        {
            FortuneArc left = new FortuneArc();
            left.Site = splittingArc.Site;

            FortuneArc right = new FortuneArc();
            right.Site = splittingArc.Site;

            // create a new edge
            VoronoiEdge edge = new VoronoiEdge(splittingArc.Site.Cell, splitter.Site.Cell);

            // both nodes of the edge are initially points at infinity, 
            // but can become final after the event range
            double x = splitter.Site.Cell.DataPoint.X;
            double y = splitter.Site.Cell.DataPoint.Y;
            double distance = getDistanceToArc(splittingArc.Site.Cell.DataPoint, y, x);
            edge.Node1 = new VoronoiNode(x, y - distance);
            edge.Node1.IsInfinit = true;
            edge.Node2 = new VoronoiNode(x, y - distance);
            edge.Node2.IsInfinit = true;

            // add this edge in both cell diagram
            splitter.Site.Cell.AddEdge(edge);
            splittingArc.Site.Cell.AddEdge(edge);

            //add nodes to the arcs of the ribs
            splitter.LeftNode = edge.Node1;
            splitter.RightNode = edge.Node2;

            left.LeftNode = splittingArc.LeftNode;
            left.RightNode = edge.Node1;
            right.LeftNode = edge.Node2;
            right.RightNode = splittingArc.RightNode; 
            
            left.Left = splittingArc.Left;
            right.Right = splittingArc.Right;
            splitter.Left = left;
            splitter.Right = right;

            if (splittingArc.Parent != null)
            {
                if (splittingArc.Parent.Left == splittingArc)
                    splittingArc.Parent.Left = splitter;
                else if (splittingArc.Parent.Right == splittingArc)
                    splittingArc.Parent.Right = splitter;
            }
            else
                Root = splitter;

            return splitter;
        }

        /// <summary>
        /// Finds the arc of the shoreline, under which arose a point event.
        /// </summary>
        /// <param name="eventX">An X coordinate of event</param>
        /// <param name="ly">A Y coordinate of the sweepline</param>
        /// <returns>The arc finded or null</returns>
        public FortuneArc FindArc(double eventX, double ly)
        {
            if (Root == null)
                return null;

            FortuneArc currentArc = Root;
            while (true)
            {
                if (currentArc.Left == null && currentArc.Right == null)
                    return currentArc;

                if (currentArc.Site.Cell.DataPoint.Y == ly)
                {
                    currentArc = currentArc.Site.Cell.DataPoint.X < eventX ? currentArc.Right : currentArc.Left;
                    continue;
                }

                //find the boundaries of the left and right subtrees
                FortuneArc leftNeighbor = currentArc.LeftSubtreeBound;
                FortuneArc rightNeighbor = currentArc.RightSubtreeBound;

                ICoordinate[] pointsPrev = null;
                ICoordinate[] pointsNext = null;

                if (leftNeighbor != null)
                {
                    pointsPrev = getArcIntersections(leftNeighbor.Site.Cell.DataPoint, currentArc.Site.Cell.DataPoint, ly);
                    if (pointsPrev.Length == 2)
                        if ((pointsPrev[1].X < pointsPrev[0].X ^ leftNeighbor.Site.Cell.DataPoint.Y < currentArc.Site.Cell.DataPoint.Y))
                            pointsPrev[0] = pointsPrev[1];
                }

                if (rightNeighbor != null)
                {
                    pointsNext = getArcIntersections(rightNeighbor.Site.Cell.DataPoint, currentArc.Site.Cell.DataPoint, ly);
                    if (pointsNext.Length == 2)
                        if ((pointsNext[1].X < pointsNext[0].X ^ rightNeighbor.Site.Cell.DataPoint.Y > currentArc.Site.Cell.DataPoint.Y))
                            pointsNext[0] = pointsNext[1];
                }

                if ((currentArc.Left == null || pointsPrev[0].X <= eventX) &&
                    (currentArc.Right == null || pointsNext[0].X >= eventX))
                    return currentArc;

                if (leftNeighbor != null && pointsPrev[0].X > eventX)
                {
                    currentArc = currentArc.Left;
                    continue;
                }

                if (rightNeighbor != null && pointsNext[0].X < eventX)
                {
                    currentArc = currentArc.Right;
                    continue;
                }
            }
        }

        private bool removeArcRecursive(FortuneArc root, FortuneArc arc)
        {
            if (root == null)
                return false;

            if (root == arc)
            {
                // no children
                if (arc.Left == null && arc.Right == null)
                {
                    if (arc.Parent != null)
                    {
                        if (arc.Parent.Left == arc)
                            arc.Parent.Left = null;
                        else if (arc.Parent.Right == arc)
                            arc.Parent.Right = null;
                    }
                    if (Root == arc) Root = null;
                    return true;
                }

                // one child
                if (arc.Left != null ^ arc.Right != null)
                {
                    FortuneArc child = arc.Left != null ? arc.Left : arc.Right;
                    if (arc.Parent != null)
                    {
                        if (arc.Parent.Left == arc)
                            arc.Parent.Left = child;
                        else if (arc.Parent.Right == arc)
                            arc.Parent.Right = child;
                    }
                    else
                    {
                        Root = child;
                        Root.Parent = null;
                    }

                    return true;
                }

                // have both child
                FortuneArc rightLeft = root.Right;
                while (rightLeft.Left != null)
                    rightLeft = rightLeft.Left;

                rightLeft.Left = arc.Left;

                if (arc.Parent != null)
                {
                    if (arc.Parent.Left == arc)
                        arc.Parent.Left = arc.Right;
                    else if (arc.Parent.Right == arc)
                        arc.Parent.Right = arc.Right;
                }
                if (Root == arc)
                {
                    Root = arc.Right;
                    Root.Parent = null;
                }
                return true;
            }
            else
            {
                if(removeArcRecursive(root.Left, arc))
                    return true;

                return removeArcRecursive(root.Right, arc);
            }
        }

        /// <summary>
        /// Removes an arc from the shoreline.
        /// </summary>
        /// <param name="arc">An arc to remove</param>
        public void RemoveArc(FortuneArc arc)
        {
            removeArcRecursive(Root, arc);
        }

        private void normalizeSegment(ref Segment s)
        {
            if (s.V1.X > s.V2.X)
            {
                double temp = s.V1.X;
                s.V1.X = s.V2.X;
                s.V2.X = temp;
                temp = s.V1.Y;
                s.V1.Y = s.V2.Y;
                s.V2.Y = temp;
            }
        }

        /// <summary>
        /// Sets the directions of dangles for infinite edges.
        /// </summary>
        /// <param name="rectangle">Bounding rectangle of the tessellation nodes</param>
        /// <param name="startVerticalNodes">The initial nodes of the infinit vertical edges, 
        /// formed by points with the maximum Y coordinate</param>
        internal void Finish(BoundingRectangle rectangle, List<VoronoiNode> startVerticalNodes)
        {
            double l = rectangle.MinY - rectangle.Width - rectangle.Height;

            FortuneArc arc = Root;
            while (arc.Left != null)
                arc = arc.Left;
            while (arc != null)
            {
                FortuneArc rn = arc.RightNeighbor;

                if (rn != null)
                {
                    ICoordinate[] points =
                        getArcIntersections(arc.Site.Cell.DataPoint, rn.Site.Cell.DataPoint, 2 * l);

                    if (points.Length == 2)
                    {
                        ICoordinate p1 = points[0].X < points[1].X ? points[0] : points[1];
                        ICoordinate p2 = points[0].X < points[1].X ? points[1] : points[0];

                        if (arc.RightNode != null && arc.RightNode.IsInfinit)
                        {
                            if (arc.Site.Cell.DataPoint.Y > rn.Site.Cell.DataPoint.Y)
                                arc.RightNode.Point = p1;
                            else
                                arc.RightNode.Point = p2;
                        }
                    }

                    if (points.Length == 1)
                        if (arc.RightNode != null && arc.RightNode.IsInfinit)
                            arc.RightNode.Point = points[0];
                }

                arc = rn;
            }

            foreach (VoronoiNode node in startVerticalNodes)
                node.Point = PlanimetryEnvironment.NewCoordinate(node.Point.X, rectangle.MaxY + rectangle.Height);
        }
    }

    /// <summary>
    /// Builds a Voronoi tessellation. Implements a Fortune algorithm.
    /// http://en.wikipedia.org/wiki/Fortune%27s_algorithm
    /// </summary>
    public class VoronoiBuilder
    {
        private FortuneShoreLine _shoreLine = new FortuneShoreLine();
        private LinkedList<FortuneEvent> _eventList;
        private BoundingRectangle _rectangle = null;
        private List<VoronoiNode> _startVerticalNodes = null;

        private bool _buildTriangles = false;
        private List<Triangle> _triangles = null;

        private ICoordinate getCircleCenter(ICoordinate p1, ICoordinate p2, ICoordinate p3)
        {
            if (p1.ExactEquals(p2) || p1.ExactEquals(p3) || p2.ExactEquals(p3))
                return null;

            // get rid of the vertical lines
            if (p1.X == p2.X)
            {
                ICoordinate temp = p1;
                p1 = p3;
                p3 = temp;
            }

            if (p2.X == p3.X)
            {
                ICoordinate temp = p2;
                p2 = p1;
                p1 = temp;
            }

            double ma = (p2.Y - p1.Y) / (p2.X - p1.X);
            double mb = (p3.Y - p2.Y) / (p3.X - p2.X);

            // collinear lines, circles do not
            if (mb == ma || (double.IsInfinity(ma) && double.IsInfinity(mb)))
                return null;

            //coordinates of the center of the circle
            double x = 0.5 * (ma * mb * (p1.Y - p3.Y) + mb * (p1.X + p2.X) - ma * (p2.X + p3.X)) / (mb - ma);

            double y = double.NaN;
            if(ma != 0 && !double.IsInfinity(ma))
                y = -1 / ma * (x - 0.5 * (p1.X + p2.X)) + 0.5 * (p1.Y + p2.Y);
            else
                y = -1 / mb * (x - 0.5 * (p2.X + p3.X)) + 0.5 * (p2.Y + p3.Y);

            return PlanimetryEnvironment.NewCoordinate(x, y);
        }

        private void addCircleEvent(FortuneEvent ev)
        {
            LinkedListNode<FortuneEvent> currentEvent = _eventList.First;
            while (currentEvent != null)
            {
                if (currentEvent.Value.Point.Y < ev.Point.Y)
                {
                    _eventList.AddBefore(currentEvent, ev);
                    return;
                }
                currentEvent = currentEvent.Next;
            }
            _eventList.AddLast(ev);
        }

        private FortuneEvent getCircleEvent(FortuneArc arc1,
                                            FortuneArc arc2,
                                            FortuneArc arc3,
                                            double y)
        {
            ICoordinate a = arc1.Site.Cell.DataPoint;
            ICoordinate b = arc2.Site.Cell.DataPoint;
            ICoordinate c = arc3.Site.Cell.DataPoint;
            //bc should turn to the right with respect to ab
            if ((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y) > 0)
                return null;

            ICoordinate point = getCircleCenter(a, b, c);
            if (point != null)
            {
                FortuneCircleEvent newEvent = new FortuneCircleEvent();
                newEvent.CircleCenter = point;

                double distance = PlanimetryAlgorithms.Distance(point, a);
                point = PlanimetryEnvironment.NewCoordinate(point.X, point.Y - distance);

                newEvent.Point = point;
                newEvent.Arc = arc2;

                if (_buildTriangles)
                    newEvent.Triangle = new Triangle(arc1.Site.Cell, arc2.Site.Cell, arc3.Site.Cell);

                return newEvent;
            }
            return null;
        }

        private void handlePointEvent(FortunePointEvent ev)
        {
            if (_shoreLine.Root == null)
            {
                FortuneArc arc = new FortuneArc();
                FortuneSite site = new FortuneSite();
                site.Cell = ev.Cell;
                arc.Site = site;

                _shoreLine.Root = arc;

                return;
            }

            FortuneArc arcAbove = _shoreLine.FindArc(ev.Point.X, ev.Point.Y);

            // remove events range from the queue
            if (arcAbove.CircleEvent != null)
                _eventList.Remove(arcAbove.CircleEvent);

            // create an arc
            FortuneArc splitter = new FortuneArc();
            splitter.Site = new FortuneSite();
            splitter.Site.Cell = ev.Cell;

            FortuneArc newArc = _shoreLine.SplitArc(arcAbove, splitter);

            FortuneArc ln = newArc.LeftNeighbor;
            FortuneArc lnln = null;
            if (ln != null)
                lnln = ln.LeftNeighbor;

            FortuneArc rn = newArc.RightNeighbor;
            FortuneArc rnrn = null;
            if (rn != null)
                rnrn = rn.RightNeighbor;

            if (ln != null)
                if (lnln != null)
                {
                    FortuneEvent eventToAdd = getCircleEvent(lnln, ln, newArc, ev.Point.Y);
                    if (eventToAdd != null)
                        addCircleEvent(eventToAdd);
                    ln.CircleEvent = eventToAdd;
                }

            if (rn != null)
                if (rnrn != null)
                {
                    FortuneEvent eventToAdd = getCircleEvent(newArc, rn, rnrn, ev.Point.Y);
                    if (eventToAdd != null)
                        addCircleEvent(eventToAdd);
                    rn.CircleEvent = eventToAdd;
                }
        }

        private void handleCircleEvent(FortuneCircleEvent ev)
        {
            FortuneArc ln = ev.Arc.LeftNeighbor;
            FortuneArc rn = ev.Arc.RightNeighbor;

            // remove events range associated with this arc
            if (ln.CircleEvent != null)
                _eventList.Remove(ln.CircleEvent);
            if (rn.CircleEvent != null)
                _eventList.Remove(rn.CircleEvent);

            // remove the arc of coastline
            _shoreLine.RemoveArc(ev.Arc);

            //fix slave nodes arc ribs
            if (ev.Arc.LeftNode != null)
            {
                ev.Arc.LeftNode.Point = ev.CircleCenter;
                ev.Arc.LeftNode.IsInfinit = false;
            }

            if (ev.Arc.RightNode != null)
            {
                ev.Arc.RightNode.Point = ev.CircleCenter;
                ev.Arc.RightNode.IsInfinit = false;
            }

            // add a new edge
            VoronoiEdge edge = new VoronoiEdge(ln.Site.Cell, rn.Site.Cell);
            edge.Node1 = new VoronoiNode(ev.CircleCenter.X, ev.CircleCenter.Y);
            edge.Node2 = new VoronoiNode((ln.Site.Cell.DataPoint.X + rn.Site.Cell.DataPoint.X) / 2,
                                         (ln.Site.Cell.DataPoint.Y + rn.Site.Cell.DataPoint.Y) / 2);

            //expand the bounding rectangle of the chart
            _rectangle.Join(ev.CircleCenter);

            // one node of the new edge is fixed, the second - no
            edge.Node1.IsInfinit = false;
            edge.Node2.IsInfinit = true;

            // dobavleyaem edge to cells
            ln.Site.Cell.AddEdge(edge);
            rn.Site.Cell.AddEdge(edge);

            //add a triangle in the Delaunay triangulation, if necessary
            if (_buildTriangles)
                _triangles.Add(ev.Triangle);

            // not a fixed node is a new edge now vanished neighbors arc
            ln.RightNode = edge.Node2;
            rn.LeftNode = edge.Node2;
            
            FortuneArc lnln = ln.LeftNeighbor;
            FortuneArc lnrn = ln.RightNeighbor;

            FortuneArc rnln = rn.LeftNeighbor;
            FortuneArc rnrn = rn.RightNeighbor;

            // add events to the newly formed circle arcs triples
            if (lnln != null)
                if (lnrn != null)
                {
                    FortuneEvent eventToAdd = getCircleEvent(lnln, ln, lnrn, ev.Point.Y);
                    if (eventToAdd != null)
                        addCircleEvent(eventToAdd);
                    ln.CircleEvent = eventToAdd;
                }

            if (rnln != null)
                if (rnrn != null)
                {
                    FortuneEvent eventToAdd = getCircleEvent(rnln, rn, rnrn, ev.Point.Y);
                    if (eventToAdd != null)
                        addCircleEvent(eventToAdd);
                    rn.CircleEvent = eventToAdd;
                }
        }

        private void init(List<VoronoiCell> cells, int lastMaxYIndex)
        {
            _rectangle = new BoundingRectangle();
            _eventList = new LinkedList<FortuneEvent>();
            _startVerticalNodes = new List<VoronoiNode>();

            // Where several points have the minimum ordinate requires a separate pre-treatment
            int skipEventCount = 0;
            if (lastMaxYIndex > 0)
            {
                VoronoiNode previousNode = null;
                FortuneArc currentArc = null;
                skipEventCount = lastMaxYIndex + 1;
                for (int i = 0; i <= lastMaxYIndex; i++)
                {
                    // add an arc of coastline
                    FortuneArc arc = new FortuneArc();
                    FortuneSite site = new FortuneSite();
                    site.Cell = cells[i];
                    arc.Site = site;

                    if (currentArc != null)
                        currentArc.Right = arc;

                    currentArc = arc;
                    if (_shoreLine.Root == null)
                        _shoreLine.Root = arc;

                    if (previousNode != null)
                        arc.LeftNode = previousNode;

                    // add the vertical edges of the Voronoi diagram
                    if (i < lastMaxYIndex)
                    {
                        VoronoiEdge edge = new VoronoiEdge(cells[i], cells[i + 1]);
                        double middleX = (cells[i].DataPoint.X + cells[i + 1].DataPoint.X) / 2;
                        double middleY = cells[i].DataPoint.Y;

                        edge.Node1 = new VoronoiNode(middleX, middleY);
                        edge.Node1.IsInfinit = true;
                        _startVerticalNodes.Add(edge.Node1);

                        edge.Node2 = new VoronoiNode(middleX, middleY);
                        edge.Node2.IsInfinit = true;

                        previousNode = edge.Node2;

                        arc.RightNode = edge.Node2;

                        cells[i].AddEdge(edge);
                        cells[i + 1].AddEdge(edge);
                    }
                }
            }

            // fill all point events
            int j = 0;
            foreach (VoronoiCell cell in cells)
            {
                if (skipEventCount > j++)
                    continue;

                FortunePointEvent ev = new FortunePointEvent();
                ev.Point = cell.DataPoint;
                ev.Cell = cell;
                _eventList.AddLast(ev);
            }
        }

        private void clean(List<VoronoiCell> cells)
        {
            foreach(VoronoiCell cell in cells)
            {
                for(int i = cell.Edges.Count - 1; i >= 0; i--)
                    if (cell.Edges[i].Node1.Point.ExactEquals(cell.Edges[i].Node2.Point))
                        cell.RemoveEdge(cell.Edges[i]);
            }
        }

        private void build(List<VoronoiCell> cells, int lastMaxYIndex)
        {
            init(cells, lastMaxYIndex);

            // handle all
            while (_eventList.Count > 0)
            {
                FortuneEvent currentEvent = _eventList.First.Value;
                _eventList.RemoveFirst();
                switch (currentEvent.Kind)
                { 
                    case FortuneEvent.EventKind.Point :
                        handlePointEvent((FortunePointEvent)currentEvent);
                        break;

                    case FortuneEvent.EventKind.Circle:
                        handleCircleEvent((FortuneCircleEvent)currentEvent);
                        break;
                }
            }

            // calculate the coordinates of the "free" sites
            _shoreLine.Finish(_rectangle, _startVerticalNodes);

            // remove degenerate edges
            clean(cells);
        }

        /// <summary>
        /// Compares abscissas. Used to sort the point with minimum ordinate.
        /// </summary>
        internal class StartPointsComparer : IComparer<ICoordinate>
        {
            int IComparer<ICoordinate>.Compare(ICoordinate p1, ICoordinate p2)
            {
                return p1.X < p2.X ? -1 : 1;
            }
        }

        private void sortPoints(List<ICoordinate> points)
        {
            // sort ordinate
            points.Sort((ICoordinate p1, ICoordinate p2) => p1.Y > p2.Y ? -1 : 1);

            // sort horizontal sequence
            int startIndex = 0;
            int endIndex = 0;
            bool horSequenceFinded = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i].Y == points[i + 1].Y)
                {
                    horSequenceFinded = true;
                    endIndex = i + 1;
                }
                else
                {
                    if (horSequenceFinded)
                    {
                        sortHorizontalSequence(points, startIndex, endIndex);
                        horSequenceFinded = false;
                    }
                    startIndex = i + 1;
                }
            }

            if (horSequenceFinded)
                sortHorizontalSequence(points, startIndex, endIndex);
        }

        private class HorizontalPointsComparer : IComparer<ICoordinate>
        {
            public int Compare(ICoordinate p1, ICoordinate p2)
            {
                return p1.X < p2.X ? -1 : 1;
            }
        }

        private void sortHorizontalSequence(List<ICoordinate> points, int startIndex, int endIndex)
        {
            points.Sort(startIndex, endIndex - startIndex + 1, new HorizontalPointsComparer());
        }

        /// <summary>
        /// Builds a Voronoi tessellation.
        /// </summary>
        /// <param name="points">Enumerator of points for tesselation building</param>
        /// <param name="buildTraingles">A value indicating whether the constructed Delaunay triangulation</param>
        /// <returns>A Voronoi tessellation for cpecified points</returns>
        public VoronoiTesselation Build(IEnumerable<ICoordinate> points, bool buildTraingles)
        {
            List<ICoordinate> sourcePoints = new List<ICoordinate>();
            foreach (ICoordinate point in points)
                sourcePoints.Add(point);

            if (sourcePoints.Count < 3)
                throw new ArgumentException("Voronoi tesselation building for this number of point is not supported", "points");

            _buildTriangles = buildTraingles;
            if(_buildTriangles)
                _triangles = new List<Triangle>();

            // sort list
            sortPoints(sourcePoints);

            // remove coincident points
            for (int i = sourcePoints.Count - 1; i > 0; i--)
                if (sourcePoints[i].ExactEquals(sourcePoints[i - 1]))
                    sourcePoints.RemoveAt(i);

            // need to count the number of points with the same (maximum) coordinates as they must be handled in a special way
            int lastMaxYIndex = 0;
            while (lastMaxYIndex < sourcePoints.Count - 1 && sourcePoints[lastMaxYIndex].Y == sourcePoints[lastMaxYIndex + 1].Y)
                lastMaxYIndex++;
            if (lastMaxYIndex > 0)
                sourcePoints.Sort(0, lastMaxYIndex + 1, new StartPointsComparer());

            List<VoronoiCell> cells = new List<VoronoiCell>();
            foreach (ICoordinate point in sourcePoints)
            {
                VoronoiCell cell = new VoronoiCell();
                cell.DataPoint = point;
                cells.Add(cell);
            }

            build(cells, lastMaxYIndex);

            VoronoiTesselation result = new VoronoiTesselation(cells, _triangles);

            _triangles = null;

            return result;
        }
    }
}

#endif
