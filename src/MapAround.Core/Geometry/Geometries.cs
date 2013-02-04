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
** File: Geometries.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Classes that represents planar geometries
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using MapAround.Serialization;

    /// <summary>
    /// The MapAround.Geometry contains interfaces and 
    /// classes that represent geometries on  a 2D plane 
    /// and implement spatial algorithms.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides access to members that describe 
    /// properties and behavior of all geometric objects.
    /// </summary>
    public interface IGeometry : ICloneable
    {
        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        ICoordinate[] ExtractCoordinates();

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        BoundingRectangle GetBoundingRectangle();

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        IList<ICoordinate> GetConvexHull();

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        int CoordinateCount { get; }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        MapAround.Geometry.Dimension Dimension { get; }
    }

    /// <summary>
    /// Represents a collection of geometries.
    /// </summary>
    [Serializable]
    public class GeometryCollection : Collection<IGeometry>, IGeometry
    {
        #region IGeometry Members

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[this.CoordinateCount];
            int shift = 0;
            foreach (IGeometry g in this)
            {
                ICoordinate[] coordinates = g.ExtractCoordinates();
                coordinates.CopyTo(result, shift);
                shift += coordinates.Length;
            }

            return result;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            BoundingRectangle result = new BoundingRectangle();
            foreach (IGeometry g in this)
                result.Join(g.GetBoundingRectangle());

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(this.ExtractCoordinates());
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get 
            {
                int result = 0;
                foreach (IGeometry g in this)
                        result += g.CoordinateCount;

                return result;
            }
        }

        /// <summary>
        /// Gets a dimension.
        /// This value is equal to the maximum dimension 
        /// of the geometry in the collection.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get 
            {
                int result = -1;
                foreach (IGeometry g in this)
                    if ((int)g.Dimension > result)
                        result = (int)g.Dimension;

                return (Dimension)result;
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
            GeometryCollection result = new GeometryCollection();
            foreach (IGeometry g in this)
                result.Add((IGeometry)g.Clone());

            return result;
        }

        #endregion

        /// <summary>
        /// Gets a value indicating whether a collection is homogenous.
        /// The collection is homogenous when the dimensions of all geometries 
        /// are equal.
        /// </summary>
        public bool IsHomogenous
        {
            get
            {
                for (int i = 0; i < Count - 1; i++)
                    if (this[i].Dimension != this[i + 1].Dimension)
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a collection contains geometries 
        /// of different types.
        /// </summary>
        public bool HasDifferentTypeInstances
        {
            get
            {
                for (int i = 0; i < this.Count - 1; i++)
                    if (this[i].GetType() != this[i + 1].GetType())
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.GeometryCollection.
        /// </summary>
        /// <param name="geometries">Enumerator of geometries</param>
        public GeometryCollection(IEnumerable<IGeometry> geometries)
        {
            foreach (IGeometry g in geometries)
                this.Add(g);
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.GeometryCollection.
        /// </summary>
        public GeometryCollection()
        {
        }
    }

    /// <summary>
    /// Represents a point on a euclidean plane.
    /// </summary>
    [Serializable]
    public class PointD : ISpatialRelative
    {
        private ICoordinate _coordinate;
        private bool _locked = false;

        /// <summary>
        /// Gets or sets coordinate values.
        /// </summary>
        public ICoordinate Coordinate
        {
            get { return _coordinate; }
            set { _coordinate = value; }
        }

        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        public double X
        {
            get { return _coordinate.X; }
            set { _coordinate.X = value; }
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        public double Y
        {
            get { return _coordinate.Y; }
            set { _coordinate.Y = value; }
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets a value indicating whether this point instance is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        /// <param name="p">The instance of MapAround.Geometry.PointD to compare with the current MapAround.Geometry.PointD</param>
        public bool Equals(PointD p)
        {
            return this._coordinate.Equals(p._coordinate);
        }

        /// <summary>
        /// Gets a value indicating whether this point instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// </summary>
        /// <param name="p">The instance of MapAround.Geometry.PointD to compare with the current MapAround.Geometry.PointD</param>
        public bool ExactEquals(PointD p)
        {
            return this._coordinate.ExactEquals(p._coordinate);
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The System.Object to compare with the current MapAround.Geometry.PointD</param>
        public override bool Equals(object o)
        {
            if (o is PointD)
                return this._coordinate.Equals(((PointD)o)._coordinate);

            return false;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Returns true if points are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(PointD p1, PointD p2)
        {
            return object.Equals(p1, p2);
        }

        /// <summary>
        /// Returns true if points are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(PointD p1, PointD p2)
        {
            return !object.Equals(p1, p2);
        }

        /// <summary>
        /// Gets a double array containing coordinate values of the current MapAround.Geometry.PointD.
        /// </summary>
        public double[] CoordsArray()
        {
            return _coordinate.Values();
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>        
        public object Clone()
        {
            return new PointD((ICoordinate)_coordinate.Clone());
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }
        
        internal void Lock()
        {
            if (_locked) return;

            _coordinate = _coordinate.ReadOnlyCopy();
            _locked = true;
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _coordinate = PlanimetryEnvironment.NewCoordinate(_coordinate.Values());
            _locked = false;
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.Zero; }
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            return new ICoordinate[] { (ICoordinate)_coordinate.Clone() };
        }

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        public void Translate(double x, double y)
        {
            if (_locked)
                throw new InvalidOperationException("Object is read only");

            _coordinate.Translate(x, y);
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="distance">The distance of the buffer</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geometry that represents a buffer</returns>
        public IGeometry Buffer(double distance, int pointsPerCircle, bool allowParallels)
        {
            return BufferBuilder.GetBuffer(this, distance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            return new BoundingRectangle(PlanimetryEnvironment.NewCoordinate(_coordinate.X, _coordinate.Y), PlanimetryEnvironment.NewCoordinate(_coordinate.X, _coordinate.Y));
        }

        /// <summary>
        /// Computes an intersection of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the intersection</param>
        /// <returns>A collection of geometries that represents the intersection</returns>
        public GeometryCollection Intersection(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the union</param>
        /// <returns>A collection of geometries that represents the union</returns>
        public GeometryCollection Union(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the difference</param>
        /// <returns>A collection of geometries that represents the difference</returns>
        public GeometryCollection Difference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the symmetric difference</param>
        /// <returns>A collection of geometries that represents the symmetric difference</returns>
        public GeometryCollection SymmetricDifference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        #region ISpatialRelative Members

        /// <summary>
        /// Indicates if the two geometries are define the same set of points in the plane.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the equality</param>
        /// <returns>true, if the two geometries are define the same set of points in the plane, false otherwise</returns>
        bool ISpatialRelative.Equals(ISpatialRelative other)
        {
            if (!(other is PointD))
                return false;

            return Relate(other, "T*F**FFF*");
        }

        /// <summary>
        /// This method will always returns false.
        /// See <c>ISpatialRelative.Crosses</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the crosses predicate</param>
        /// <returns>false</returns>
        public bool Crosses(ISpatialRelative other)
        {
            return false;
        }

        /// <summary>
        /// Indicates if the two geometries share no points in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the two geometries share no points in common, false otherwise</returns>
        public bool Disjoint(ISpatialRelative other)
        {
            return Relate(other, "FF*FF****");
        }

        /// <summary>
        /// This method will always returns false.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the overlaps predicate</param>
        /// <returns>false</returns>
        public bool Overlaps(ISpatialRelative other)
        {
            return false;
        }

        /// <summary>
        /// Indicates if the two geometries share at least one point in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the intersects predicate</param>
        /// <returns>true, if the two geometries share at least one points in common, false otherwise</returns>
        public bool Intersects(ISpatialRelative other)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, "TT*TT****");
            if (matrix.Matches("T********") ||
               matrix.Matches("*T*******") ||
               matrix.Matches("***T*****") ||
               matrix.Matches("****T****"))
                return true;

            return false;
        }

        /// <summary>
        /// Indicates if the boundaries of the geometries intersect.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the touches predicate</param>
        /// <returns>true, if the boundaries of the geometries intersect, false otherwise</returns>
        public bool Touches(ISpatialRelative other)
        {
            if (other is PointD || other is MultiPoint)
                return false;

            return Relate(other, "FT*******");
        }

        /// <summary>
        /// Indicates if this geometry is contained (is within) another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the within predicate</param>
        /// <returns>true, if this geometry is contained another geometry, false otherwise</returns>
        public bool Within(ISpatialRelative other)
        {
            return Relate(other, "T*F**F***");
        }

        /// <summary>
        /// Indicates if this geometry contains the other geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the contains predicate</param>
        /// <returns>true, if this geometry contains the other geometry, false otherwise</returns>
        public bool Contains(ISpatialRelative other)
        {
            return Relate(other, "T*****FF*");
        }

        /// <summary>
        /// Indicates if the defined relationship exists.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the defined relation</param>
        /// <param name="template">Template of the intersection matrix that defines relation</param>
        /// <returns>true, if the defined relationship exists, false otherwise</returns>
        public bool Relate(ISpatialRelative other, string template)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, template);
            return matrix.Matches(template);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.PointD"/>.
        /// </summary>
        public PointD()
        {
            _coordinate = PlanimetryEnvironment.NewCoordinate(0, 0);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.PointD"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        public PointD(double x, double y)
        {
            _coordinate = PlanimetryEnvironment.NewCoordinate(x, y);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.PointD"/>.
        /// </summary>
        /// <param name="coord">An object representing coordinates</param>
        public PointD(ICoordinate coord)
        {
            _coordinate = (ICoordinate)coord.Clone();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.PointD"/>.
        /// </summary>
        /// <param name="coords">An array containing the coordinate values</param>
        public PointD(double[] coords)
        {
            _coordinate = PlanimetryEnvironment.NewCoordinate(coords);
        }
    }

    /// <summary>
    /// Represents a segment of the straight line.
    /// </summary>
    /// <remarks>
    /// Instances of this class defines a 2D straight line between a pair of 2D endpoints.
    /// </remarks>
    [Serializable]
    public struct Segment : IGeometry
    {
        /// <summary>
        /// First endpoint.
        /// </summary>
        public ICoordinate V1;

        /// <summary>
        /// Second endpoint.
        /// </summary>
        public ICoordinate V2;

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            return new ICoordinate[] { (ICoordinate)V1.Clone(), (ICoordinate)V2.Clone() };
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Calculates a length of this segment (euclidean distance between endpoints).
        /// </summary>
        public double Length()
        {
            return PlanimetryAlgorithms.Distance(V1, V2);
        }

        /// <summary>
        /// Indicates if this instance of MapAround.Geometry.Segment is equal to another.
        /// Comparision performs with the tolerance value stored in <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        /// <param name="s">The instance of MapAround.Geometry.Segment to compare</param>
        public bool Equals(Segment s)
        {
            return (V2.Equals(s.V2) && V1.Equals(s.V1)) ||
                   (V2.Equals(s.V1) && V1.Equals(s.V2));
        }

        /// <summary>
        /// Indicates if this instance of MapAround.Geometry.Segment is equal to another.
        /// Comparision performs with the zero tolerance value.
        /// </summary>
        /// <param name="s">The instance of MapAround.Geometry.Segment to compare</param>
        public bool ExactEquals(Segment s)
        {
            return (V2.ExactEquals(s.V2) && V1.ExactEquals(s.V1)) ||
                   (V2.ExactEquals(s.V1) && V1.ExactEquals(s.V2));
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            return new BoundingRectangle(Math.Min(V1.X, V2.X), 
                                         Math.Min(V1.Y, V2.Y), 
                                         Math.Max(V1.X, V2.X), 
                                         Math.Max(V1.Y, V2.Y));
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get { return 2; }
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The instance of the System.Object to compare</param>
        public override bool Equals(object o)
        {
            if(o is Segment)
                return (V1.Equals(((Segment)o).V1) && V2.Equals(((Segment)o).V2)) ||
                       (V1.Equals(((Segment)o).V2) && V2.Equals(((Segment)o).V1));

            return false;
        }

        /// <summary>
        /// Returns true if segments are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(Segment s1, Segment s2)
        {
            return s1.Equals(s2);
        }

        /// <summary>
        /// Returns true if segments are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(Segment s1, Segment s2)
        {
            return !s1.Equals(s2);
        }

        /// <summary>
        /// Indicates if this segment is singular (have equal endpoints).
        /// Used tolerance value stored in <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        public bool IsSingular()
        {
            if (V1.ExactEquals(V2))
                return true;

            return V1.Equals(V2);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            return new Segment((ICoordinate)V1.Clone(), (ICoordinate)V2.Clone());
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.One; }
        }

        /// <summary>
        /// Calculates a coordinate of the center point of this segment.
        /// </summary>
        /// <returns>A coordinate of the center point of this segment</returns>
        public ICoordinate Center()
        {
            return PlanimetryEnvironment.NewCoordinate((V1.X + V2.X) / 2, (V1.Y + V2.Y) / 2);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Segment"/>.
        /// </summary>
        /// <param name="p1">First endpoint</param>
        /// <param name="p2">Second endpoint</param>
        public Segment(ICoordinate p1, ICoordinate p2)
        {
            V1 = (ICoordinate)p1.Clone();
            V2 = (ICoordinate)p2.Clone();
        }

        /// <summary>
        /// Initializes a new instance of  <see cref="MapAround.Geometry.Segment"/>.
        /// </summary>
        /// <param name="x1">An X coordinate of first endpoint</param>
        /// <param name="y1">A Y coordinate of first endpoint</param>
        /// <param name="x2">An X coordinate of second endpoint</param>
        /// <param name="y2">A Y coordinate of second endpoint</param>
        public Segment(double x1, double y1, double x2, double y2)
        {
            V1 = PlanimetryEnvironment.NewCoordinate(x1, y1);
            V2 = PlanimetryEnvironment.NewCoordinate(x2, y2);
        }
    }

    /// <summary>
    /// An ordered collection of points.
    /// </summary>
    [Serializable]
    public class MultiPoint : ISpatialRelative
    {
        private bool _locked = false;
        private List<ICoordinate> _points = new List<ICoordinate>();
        private IList<ICoordinate> _p;

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            MultiPoint mp = new MultiPoint();

            foreach (ICoordinate c in Points)
                mp.Points.Add((ICoordinate)c.Clone());

            return mp;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }

        /// <summary>
        /// Gets or sets a list of coordinates of points 
        /// in the collection.
        /// </summary>
        public IList<ICoordinate> Points
        {
            get
            { return _p; }
            set
            {
                if (_locked)
                    throw new InvalidOperationException("Object is read only");

                if (value is List<ICoordinate>)
                    _p = _points = value as List<ICoordinate>;
                else
                {
                    _points.Clear();
                    foreach (ICoordinate c in value)
                        _points.Add((ICoordinate)c.Clone());

                    _p = _points;
                }
            }
        }

        internal void Lock()
        {
            if (_locked) return;

            _locked = true;
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach (ICoordinate c in _points)
                coords.Add(c.ReadOnlyCopy());

            _points = coords;
            _p = _points.AsReadOnly();
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _locked = false;

            for (int i = 0; i < _points.Count; i++)
                _points[i] = PlanimetryEnvironment.NewCoordinate(_points[i].Values());

            _p = _points;
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[Points.Count];

            int i = 0;
            foreach (ICoordinate c in Points)
            {
                result[i] = (ICoordinate)c.Clone();
                i++;
            }

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.Zero; }
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get
            {
                return Points.Count;
            }
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            return PlanimetryAlgorithms.GetPointsBoundingRectangle(Points);
        }

        /// <summary>
        /// Computes an intersection of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the intersection</param>
        /// <returns>A collection of geometries that represents the intersection</returns>
        public GeometryCollection Intersection(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the union</param>
        /// <returns>A collection of geometries that represents the union</returns>
        public GeometryCollection Union(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the difference</param>
        /// <returns>A collection of geometries that represents the difference</returns>
        public GeometryCollection Difference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the symmetric difference</param>
        /// <returns>A collection of geometries that represents the symmetric difference</returns>
        public GeometryCollection SymmetricDifference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        #region ISpatialRelative Members

        /// <summary>
        /// Indicates if the two geometries are define the same set of points in the plane.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the equality</param>
        /// <returns>true, if the two geometries are define the same set of points in the plane, false otherwise</returns>
        bool ISpatialRelative.Equals(ISpatialRelative other)
        {
            if (!(other is PointD))
                return false;

            return Relate(other, "T*F**FFF*");
        }

        /// <summary>
        /// This method will always returns false.
        /// See <c>ISpatialRelative.Crosses</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the crosses predicate</param>
        /// <returns>false</returns>
        public bool Crosses(ISpatialRelative other)
        {
            return false;
        }

        /// <summary>
        /// Indicates if the two geometries share no points in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the two geometries share no points in common, false otherwise</returns>
        public bool Disjoint(ISpatialRelative other)
        {
            return Relate(other, "FF*FF****");
        }

        /// <summary>
        /// Indicates if the intersection of the two geometries has the same dimension as one of the input geometries.
        /// See <c>ISpatialRelative.Overlaps</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the intersection of the two geometries has the same dimension as one of the input geometries, false otherwise</returns>
        public bool Overlaps(ISpatialRelative other)
        {
            if (other.Dimension != 0)
                return false;

            return this.Relate(other, "T*T***T**");
        }

        /// <summary>
        /// Indicates if the two geometries share at least one point in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the intersects predicate</param>
        /// <returns>true, if the two geometries share at least one points in common, false otherwise</returns>
        public bool Intersects(ISpatialRelative other)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, "TT*TT****");
            if (matrix.Matches("T********") ||
               matrix.Matches("*T*******") ||
               matrix.Matches("***T*****") ||
               matrix.Matches("****T****"))
                return true;

            return false;
        }

        /// <summary>
        /// Indicates if the boundaries of the geometries intersect.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the touches predicate</param>
        /// <returns>true, if the boundaries of the geometries intersect, false otherwise</returns>
        public bool Touches(ISpatialRelative other)
        {
            if (other is PointD || other is MultiPoint)
                return false;

            return Relate(other, "FT*******");
        }

        /// <summary>
        /// Indicates if this geometry is contained (is within) another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the within predicate</param>
        /// <returns>true, if this geometry is contained another geometry, false otherwise</returns>
        public bool Within(ISpatialRelative other)
        {
            return Relate(other, "T*F**F***");
        }

        /// <summary>
        /// Indicates if this geometry contains the other geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the contains predicate</param>
        /// <returns>true, if this geometry contains the other geometry, false otherwise</returns>
        public bool Contains(ISpatialRelative other)
        {
            return Relate(other, "T*****FF*");
        }

        /// <summary>
        /// Indicates if the defined relationship exists.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the defined relation</param>
        /// <param name="template">Template of the intersection matrix that defines relation</param>
        /// <returns>true, if the defined relationship exists, false otherwise</returns>
        public bool Relate(ISpatialRelative other, string template)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, template);
            return matrix.Matches(template);
        }

        #endregion

        /// <summary>
        /// Snaps all coordinates of this geometry to grid nodes.
        /// Coordinates can be changed to a value less than cellSize / 2.
        /// </summary>
        /// <param name="origin">The origin of the grid</param>
        /// <param name="cellSize">Cell size of the grid</param>
        public void SnapToGrid(ICoordinate origin, double cellSize)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                ICoordinate p = Points[i];
                PlanimetryAlgorithms.SnapToGrid(ref p, origin, cellSize);
                Points[i] = p;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.MultiPoint"/>.
        /// </summary>
        /// <param name="coordinates">Enumerator of points coordinates</param>
        public MultiPoint(IEnumerable<ICoordinate> coordinates)
        {
            _p = _points;
            foreach (ICoordinate coordinate in coordinates)
                Points.Add((ICoordinate)coordinate.Clone());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.MultiPoint"/>.
        /// </summary>
        /// <param name="points">Enumerator of coordinate arrays</param>
        public MultiPoint(IEnumerable<double[]> points)
        {
            _p = _points;
            foreach (double[] point in points)
                Points.Add(PlanimetryEnvironment.NewCoordinate(point));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.MultiPoint"/>.
        /// </summary>
        public MultiPoint()
        {
            _p = _points;
        }
    }

    /// <summary>
    /// Represents closed sequence of connected segments
    /// </summary>
    [Serializable]
    public class Contour : IGeometry
    {
        /// <summary>
        /// Contour layout in polygon.
        /// </summary>
        public enum ContourLayout 
        {
            /// <summary>
            /// The layout of contour is unknown.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Contour is the shell.
            /// </summary>
            External = 1,
            /// <summary>
            /// Contour is the hole.
            /// </summary>
            Internal = 2,
            /// <summary>
            /// Contour has self intersections or intersects another contours.
            /// </summary>
            Intersect = 3
        };

        private bool _locked = false;
        private List<ICoordinate> _vertices = new List<ICoordinate>();
        private IList<ICoordinate> _v;

        private void weedSection(int startIndex, int endIndex, bool[] marker, double epsilon)
        {
            if (endIndex - startIndex < 2)
                return;

            double maxDistance = 0;
            int indexOfMaxDistant = -1;
            Segment s = new Segment(Vertices[startIndex], Vertices[endIndex == Vertices.Count ? 0 : endIndex]);
            bool isSingular = s.IsSingular();
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (!marker[i])
                {
                    double currentDistance;

                    if (!isSingular)
                        currentDistance = PlanimetryAlgorithms.DistanceToLine(s, Vertices[i]);
                    else
                        currentDistance = PlanimetryAlgorithms.Distance(Vertices[i], s.V1);

                    if (maxDistance <= currentDistance)
                    {
                        maxDistance = currentDistance;
                        indexOfMaxDistant = i;
                    }
                }
            }

            if (maxDistance > epsilon)
            {
                marker[indexOfMaxDistant] = true;
                weedSection(startIndex, indexOfMaxDistant, marker, epsilon);
                weedSection(indexOfMaxDistant, endIndex, marker, epsilon);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }

        internal void Lock()
        {
            if (_locked) return;

            _locked = true;
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach (ICoordinate c in _vertices)
                coords.Add(c.ReadOnlyCopy());

            _vertices = coords;
            _v = _vertices.AsReadOnly();
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _locked = false;

            for (int i = 0; i < _vertices.Count; i++)
                _vertices[i] = PlanimetryEnvironment.NewCoordinate(_vertices[i].Values());

            _v = _vertices;
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[Vertices.Count];

            int i = 0;
            foreach (ICoordinate p in Vertices)
            {
                result[i] = (ICoordinate)p.Clone();
                i++;
            }

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Gets or sets a list of vertices coordinates.
        /// </summary>
        public IList<ICoordinate> Vertices
        {
            get
            { return _v; }
            set
            {
                if (_locked)
                    throw new InvalidOperationException("Object is read only");

                _v = _vertices = value as List<ICoordinate>;
            }
        }

        /// <summary>
        /// Contour layout in polygon.
        /// </summary>
        public ContourLayout Layout;

        /// <summary>
        /// Calculates a length of this contour.
        /// </summary>
        public double Length()
        {
            if (Vertices.Count <= 1)
                return 0;

            double result = 0;
            int i;
            for (i = 0; i < Vertices.Count - 1; i++)
                result += PlanimetryAlgorithms.Distance(Vertices[i], Vertices[i + 1]);

            result += PlanimetryAlgorithms.Distance(Vertices[i], Vertices[0]);

            return result;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            return PlanimetryAlgorithms.GetPointsBoundingRectangle(Vertices);
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get 
            {
                return Vertices.Count; 
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            Contour c = new Contour();

            foreach (ICoordinate p in Vertices)
                c.Vertices.Add((ICoordinate)p.Clone());

            c.Layout = Layout;

            return c;
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get 
            {
                return MapAround.Geometry.Dimension.One;
            }
        }


        /// <summary>
        /// Calculates a simple area of contour.
        /// Self intersections may lead to incorrect results.
        /// </summary>
        public double SimpleArea()
        {
            return Math.Abs(SignedArea());
        }

        /// <summary>
        /// Generalizes this contour.
        /// <para>
        /// The Douglas-Pecker algorithm implementation. 
        /// Points discarded if they are closer than epsilon 
        /// from the direction specified neighboring points.
        /// </para>
        /// </summary>
        public void Weed(double epsilon)
        {
            bool[] marker = new bool[Vertices.Count];
            for (int i = 0; i < marker.Length; i++)
                marker[i] = false;

            marker[0] = true;
            marker[marker.Length - 1] = true;

            weedSection(0, Vertices.Count, marker, epsilon);

            int j = 0;
            List<ICoordinate> result = new List<ICoordinate>();
            foreach (bool b in marker)
            {
                if (b) result.Add(Vertices[j]);
                j++;
            }
            Vertices = result;
        }

        /// <summary>
        /// Removes all segments with a length less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minLength">The minimum length value</param>
        public void ReduceSegments(double minLength)
        {
            if (minLength < 0)
                throw new ArgumentException("The minimum length should not be negative", "minLength");

            Contour contour = new Contour();

            if (this.Vertices.Count > 1)
            {
                ICoordinate initialPoint = this.Vertices[0];
                Segment segment;
                for (int i = 0; i < this.Vertices.Count - 1; i++)
                {
                    segment = new Segment(initialPoint.X, initialPoint.Y, this.Vertices[i + 1].X, this.Vertices[i + 1].Y);
                    if (segment.Length() > minLength)
                    {
                        contour.Vertices.Add(initialPoint);
                        initialPoint = this.Vertices[i + 1];
                    }
                }

                contour.Vertices.Add(this.Vertices[this.Vertices.Count - 1]);

                // need to check the length of the last (closing) of the segment circuit, maybe he should be removed
                while (contour.Vertices.Count > 1)
                {
                    if (PlanimetryAlgorithms.Distance(contour.Vertices[0], contour.Vertices[contour.Vertices.Count - 1]) > minLength)
                        break;
                    contour.Vertices.RemoveAt(contour.Vertices.Count - 1);
                }
            }

            // circuit does not contain segments
            if (contour.Vertices.Count == 1)
                contour.Vertices.Clear();

            this.Vertices = contour.Vertices;
        }

        /// <summary>
        /// Contour orientation.
        /// </summary>
        public enum Orientation : int
        {
            /// <summary>
            /// The orientation is unknown.
            /// This value used for self-intersected contours.
            /// </summary>
            Undefined = 0,
            /// <summary>
            /// Contour oriented counter clockwise.
            /// </summary>
            CCW = 1,
            /// <summary>
            /// Contour oriented clockwise.
            /// </summary>
            CW = 2
        }

        /// <summary>
        /// Computes a monotone chains of this contour and then 
        /// adds it to the specified list.
        /// </summary>
        /// <remarks>
        /// Singular segments can not be included in monotone chains and will throw an exception.
        /// The Labels collection contains segment indices.
        /// </remarks>
        /// <param name="chains">A list to adds monotone chains</param>
        public void AppendMonotoneChains(IList<MonotoneChain> chains)
        {
            MonotoneChain currentChain = null;
            Segment s = new Segment();

            //build a list of monotone chain segments
            for (int i = 0; i < this.Vertices.Count; i++)
            {
                s.V1 = this.Vertices[i];
                s.V2 = this.Vertices[i == this.Vertices.Count - 1 ? 0 : i + 1];

                if (currentChain == null)
                    currentChain = new MonotoneChain(s, new SegmentLabel(0, 0, i));
                else
                {
                    bool segmentAdded = currentChain.InsertSegment(s, new SegmentLabel(0, 0, i));
                    if (!segmentAdded)
                    {
                        chains.Add(currentChain);
                        currentChain = new MonotoneChain(s, new SegmentLabel(0, 0, i));
                    }
                }
            }

            if (currentChain != null)
                chains.Add(currentChain);
        }

        /// <summary>
        /// Computes a monotone chains of this contour.
        /// </summary>
        /// <remarks>
        /// Singular segments can not be included in monotone chains and will throw an exception.
        /// The Labels collection contains segment indices.
        /// </remarks>
        /// <returns>A list containing monotone chains</returns>
        public IList<MonotoneChain> GetMonotoneChains()
        {
            List<MonotoneChain> result = new List<MonotoneChain>();

            AppendMonotoneChains(result);

            return result;
        }

        /// <summary>
        /// Indicates whether a contour has self-intersections.
        /// </summary>
        /// <remarks>
        /// Contour has self-intersections if
        /// 1. Any non-adjacent segments intersect.
        /// 2. Any contour segments intersect and their intersection is a segment.
        /// </remarks>
        /// <returns>true, if a contour has self-intersections, false otherwise</returns>
        public bool HasSelfIntersections()
        {
            return GetSelfIntersectionPoint() != null;
        }

        /// <summary>
        /// Compuset a self-intersection point.
        /// </summary>
        /// <remarks>
        /// Contour has self-intersections if
        /// 1. Intersect any non-adjacent segments.
        /// 2. Intersect any contour segments and their intersection is a segment.
        /// </remarks>
        /// <returns>Coordinate of a self-intersection point or null</returns>
        public ICoordinate GetSelfIntersectionPoint()
        {
            Contour c = (Contour)this.Clone();
            c.ReduceSegments(PlanimetryAlgorithms.Tolerance);

            IList<MonotoneChain> chains = c.GetMonotoneChains();

            ICoordinate pointStub = null;
            Segment segmentStub = new Segment();
            Segment s1 = new Segment();
            Segment s2 = new Segment();

            // check all monotone chain
            for (int i = 0; i < chains.Count - 1; i++)
                for (int j = i + 1; j < chains.Count; j++)
                {
                    if (chains[i].BoundsIntersect(chains[j]))
                    {
                        ReadOnlyCollection<Segment> segments1 = chains[i].Segments;
                        ReadOnlyCollection<Segment> segments2 = chains[j].Segments;

                        // it would be necessary to use a binary search
                        for (int k = 0; k < segments1.Count; k++)
                            for (int l = 0; l < segments2.Count; l++)
                            {
                                s1 = segments1[k];
                                s2 = segments2[l];

                                Dimension crossKind =
                                    PlanimetryAlgorithms.RobustSegmentsIntersection(s1, s2, out pointStub, out segmentStub);
                                if (crossKind != MapAround.Geometry.Dimension.None)
                                {
                                    if (crossKind == MapAround.Geometry.Dimension.One)
                                        return segmentStub.V1;

                                    bool isS1Vertex = pointStub.Equals(s1.V1) || pointStub.Equals(s1.V2);
                                    bool isS2Vertex = pointStub.Equals(s2.V1) || pointStub.Equals(s2.V2);
                                    if (isS1Vertex && isS2Vertex)
                                    {
                                        int index1 = chains[i].Labels[k].IndexInSequence;
                                        int index2 = chains[j].Labels[l].IndexInSequence;

                                        if (Math.Abs(index1 - index2) == 1 ||
                                            (Math.Min(index1, index2) == 0 && 
                                             Math.Max(index1, index2) == c.Vertices.Count - 1))
                                            continue;

                                        return pointStub;
                                    }

                                    return pointStub;
                                }
                            }
                    }
                }

            return null;
        }

        /// <summary>
        /// Computes orientation of this contour.
        /// </summary>
        /// <returns>Orientation of this contour</returns>
        public Orientation GetOrientation()
        {
            if (HasSelfIntersections())
                return Orientation.Undefined;

            int nPts = Vertices.Count - 1;

            ICoordinate maxYPoint = Vertices[0];
            int maxYPointIndex = 0;
            for (int i = 1; i <= nPts; i++)
            {
                if (Vertices[i].Y > maxYPoint.Y)
                {
                    maxYPoint = Vertices[i];
                    maxYPointIndex = i;
                }
            }

            int iPrev = maxYPointIndex;
            do
            {
                iPrev--;
                if (iPrev < 0) 
                    iPrev = nPts;
            }
            while (Vertices[iPrev].Equals(maxYPoint) && iPrev != maxYPointIndex);

            int iNext = maxYPointIndex;
            do
            {
                iNext++;
                if (iNext == nPts + 1)
                    iNext = 0;
            }
            while (Vertices[iNext].Equals(maxYPoint) && iNext != maxYPointIndex);

            ICoordinate prev = Vertices[iPrev];
            ICoordinate next = Vertices[iNext];

            if (prev.Equals(maxYPoint) || next.Equals(maxYPoint) || prev.Equals(next))
                return Orientation.Undefined;

            int orientationIndex = 
                PlanimetryAlgorithms.OrientationIndex(new Segment(prev, maxYPoint), next);

            Orientation result = Orientation.Undefined;
            if (orientationIndex == 0)
                result = (prev.X > next.X) ? Orientation.CCW : Orientation.CW;
            else
                result = (orientationIndex > 0) ? Orientation.CCW : Orientation.CW;
            return result;
        }

        /// <summary>
        /// Computes a signed area of this contour.
        /// </summary>
        /// <remarks>
        /// If the contours of the polygon do not intersect, and points 
        /// oriented clockwise for shells (counter clockwise for holes), 
        /// the sum of signed areas is equal to the polygon area.
        /// </remarks>
        public double SignedArea()
        {
            if (Vertices.Count < 3)
                return 0;

            //before calculating the necessary circuit broadcast 
            //center at the origin in order to avoid loss of precision
            //in the multiplication of large numbers

            BoundingRectangle bounds = GetBoundingRectangle();
            ICoordinate delta = bounds.Center(); //point transmission

            double result = 0;

            int cnt = Vertices.Count, j;
            for (int i = 0; i < cnt; i++)
            {
                if (i == cnt - 1) j = 0; else j = i + 1;

                result += (Vertices[i].X + Vertices[j].X - delta.X * 2) * (Vertices[i].Y - Vertices[j].Y) / 2;
            }
            return result; 
        }

        /// <summary>
        /// Indicates if the point lies on the area bounded by the contour.
        /// </summary>
        /// <param name="coordinate">Coordinate of point</param>
        /// <returns>true, if the point lies on the area bounded by the contour, false otherwise</returns>
        public bool PointLiesInside(ICoordinate coordinate)
        {
            int crossCount = 0;
            double y = coordinate.Y;

            int vc = Vertices.Count;
            for (int i = 0; i < vc; i++)
            {
                int j = i == vc - 1 ? 0 : i + 1;
                double p1y = Vertices[i].Y;
                double p2y = Vertices[j].Y;

                if ((p1y > y) && (p2y <= y) || (p1y <= y) && (p2y > y))
                {
                    double x1 = Vertices[i].X - coordinate.X;
                    double y1 = p1y - y;
                    double x2 = Vertices[j].X - coordinate.X;
                    double y2 = p2y - y;

                    // line below the equivalent: if ((x1 * y2 - x2 * y1) / (y2 - y1) > 0)
                    if (PlanimetryAlgorithms.DeterminantSign(x1, y1, x2, y2) / (y2 - y1) > 0)
                        crossCount++;
                }
            }

            return crossCount % 2 == 1;
        }

        /// <summary>
        /// Indicates if the point lies on one of the contour segments.
        /// </summary>
        /// <param name="point">Coordinate of point</param>
        /// <returns>true, if the point lies on one of the contour segments, false otherwise</returns>
        public bool PointLiesOn(ICoordinate point)
        {
            int vc = Vertices.Count;
            for (int i = 0; i < vc; i++)
            {
                int j = i == vc - 1 ? 0 : i + 1;

                if (PlanimetryAlgorithms.LiesOnSegment(point, new Segment(Vertices[i], Vertices[j])))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the center of mass of the vertices of the contour.
        /// </summary>
        /// <remarks>
        /// Masses of vertices are set equal.
        /// </remarks>
        public ICoordinate VerticesCentroid()
        {
            return PlanimetryAlgorithms.GetCentroid(Vertices);
        }

        /// <summary>
        /// Calculates the center of mass of the segments of the contour.
        /// </summary>
        /// <remarks>
        /// The density of edges is identical.
        /// </remarks>
        public ICoordinate EdgesCentroid()
        {
            if (Vertices.Count == 0)
                return null;

            ICoordinate result = PlanimetryEnvironment.NewCoordinate(0, 0);
            double perimeter = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                int j = i < Vertices.Count - 1 ? i + 1 : 0;
                Segment s = new Segment(Vertices[i].X, Vertices[i].Y, Vertices[j].X, Vertices[j].Y);
                double l = s.Length();
                ICoordinate center = s.Center();
                result.X += l * center.X;
                result.Y += l * center.Y;
                perimeter += l;
            }

            result.X = result.X / perimeter;
            result.Y = result.Y / perimeter;

            return result;
        }

        /// <summary>
        /// Reverses the sequence of vertices.
        /// </summary>
        public void Reverse()
        {
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count / 2; i++, j--)
            {
                ICoordinate temp = Vertices[i];
                Vertices[i] = Vertices[j];
                Vertices[j] = temp;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Contour"/>.
        /// </summary>
        public Contour()
        {
            Layout = ContourLayout.Unknown;
            _v = _vertices;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Contour"/>.
        /// </summary>
        /// <param name="vertices">Enumerator of vertices coordinates</param>
        public Contour(IEnumerable<ICoordinate> vertices)
        {
            _v = _vertices;
            foreach (ICoordinate point in vertices)
                Vertices.Add((ICoordinate)point.Clone());

            Layout = ContourLayout.Unknown;
        }

        /// <summary>
        /// Initializes a new instance of  <see cref="MapAround.Geometry.Contour"/>.
        /// </summary>
        /// <param name="vertices">Enumerator of the vertices coordinate arrays</param>
        public Contour(IEnumerable<double[]> vertices)
        {
            _v = _vertices;
            foreach (double[] point in vertices)
                Vertices.Add(PlanimetryEnvironment.NewCoordinate(point));

            Layout = ContourLayout.Unknown;
        }
    }

    /// <summary>
    /// Represents a polygon on a 2D plane.
    /// </summary>
    /// <remarks>
    /// Set of closed line paths (contours) define a polygon boundary.
    /// Polygon is the locus of points drawn from which a ray crosses 
    /// the boundary an odd number of times. This definition does not 
    /// require connectivity in contrast to the definition providing 
    /// by OGC.
    /// </remarks>
    [Serializable]
    public class Polygon : ISpatialRelative
    {
        private bool _locked = false;
        private List<Contour> _contours = new List<Contour>();
        private IList<Contour> _c;

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }

        /// <summary>
        /// Gets or sets an object that represents a 
        /// list of polygon contours.
        /// </summary>
        public IList<Contour> Contours 
        {
            get 
            { return _c; }
            set
            {
                if (_locked)
                    throw new InvalidOperationException("Object is read only");

                _contours.Clear();
                foreach (Contour contour in value)
                {
                    Contour c = new Contour();
                    c.Layout = contour.Layout;
                    foreach (ICoordinate coord in contour.Vertices)
                        c.Vertices.Add((ICoordinate)coord.Clone());
                    _contours.Add(c);
                }
                _c = _contours;
            }
        }

        internal void Lock()
        {
            if (_locked) return;

            _locked = true;

            List<Contour> tempContours = new List<Contour>();

            foreach (Contour contour in _contours)
            {
                contour.Lock();
                tempContours.Add(contour);
            }
            _contours = tempContours;

            _c = _contours.AsReadOnly();
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _locked = false;
            foreach (Contour contour in _contours)
                contour.UnLock();

            _c = _contours;
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[CoordinateCount];

            int i = 0;
            foreach (Contour c in Contours)
                foreach (ICoordinate p in c.Vertices)
                {
                    result[i] = p;
                    i++;
                }

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="distance">The distance of the buffer</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geometry that represents a buffer</returns>
        public IGeometry Buffer(double distance, int pointsPerCircle, bool allowParallels)
        {
            return BufferBuilder.GetBuffer(this, distance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Calculates the length of the polygon boundary.
        /// </summary>
        /// <returns>The length of the polygon boundary</returns>
        public double Perimeter()
        {
            double result = 0;
            foreach (Contour c in Contours)
                result += c.Length();

            return result;
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get
            {
                int result = 0;
                foreach (Contour c in Contours)
                    result += c.CoordinateCount;

                return result;
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            Polygon polygon = new Polygon();
            foreach (Contour c in Contours)
                polygon.Contours.Add((Contour)c.Clone());

            return polygon;
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.Two; }
        }

        /// <summary>
        /// Generalizes this polygon.
        /// <para>
        /// The Douglas-Pecker algorithm implementation. 
        /// Points, that are closer than epsilon from the 
        /// direction specified neighboring points  - are 
        /// discarded.
        /// </para>
        /// </summary>
        public void Weed(double epsilon)
        {
            foreach (Contour c in Contours)
                c.Weed(epsilon);
        }

        /// <summary>
        /// Snaps coordinates of all vertices to grid nodes.
        /// Coordinates can be changed to a value less than cellSize / 2.
        /// </summary>
        /// <param name="origin">The origin of the grid</param>
        /// <param name="cellSize">Cell size of the grid</param>
        public void SnapVertices(ICoordinate origin, double cellSize)
        {
            foreach (Contour c in this.Contours)
                PlanimetryAlgorithms.SnapCoordinatesToGrid(c.Vertices, origin, cellSize);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Polygon"/>.
        /// </summary>
        /// <param name="contours">Enumerator of contours</param>
        public Polygon(IEnumerable<Contour> contours)
        {
            _c = _contours;

            foreach (Contour contour in contours)
                Contours.Add(contour);
        }

        /// <summary>
        /// Calculates an area of this polygon.
        /// </summary>
        public double Area()
        {
            // in the case of one circuit can try Lose a little blood
            if (Contours.Count == 1)
            {
                Contour.Orientation orientation = Contours[0].GetOrientation();

                // If the circuit is oriented, we can immediately Calculate the square.
                if(orientation == Contour.Orientation.CW)
                    return Contours[0].SignedArea();
                if (orientation == Contour.Orientation.CCW)
                    return -Contours[0].SignedArea();
            }

            List<Contour> contours = GetSimplifiedContours();

            double result = 0;

            foreach (Contour c in contours)
                result -= c.SignedArea();

            return result;
        }

        /// <summary>
        /// Element of the structure of the contour layout in polygons.
        /// Used by SplitToConnectedDomains method.
        /// </summary>
        private class ContourLayoutElement
        {
            private Contour _contour = new Contour();
            private ContourLayoutElement _parentElement = null;
            private List<ContourLayoutElement> _interiorContours = new List<ContourLayoutElement>();

            internal ContourLayoutElement ParentElement
            {
                get { return _parentElement; }
                set { _parentElement = value; }
            }

            internal Contour Contour
            {
                get { return _contour; }
                set { _contour = value; }
            }

            internal List<ContourLayoutElement> InnerContours
            {
                get { return _interiorContours; }
            }

            internal Polygon GetPolygon()
            {
                Polygon result = new Polygon();
                result.Contours.Add(_contour);
                foreach (ContourLayoutElement inner in _interiorContours)
                    result.Contours.Add(inner._contour);

                return result;
            }

            internal void AddPolygonsRecursively(List<Polygon> list)
            {
                list.Add(GetPolygon());
                foreach (ContourLayoutElement inner in _interiorContours)
                    foreach (ContourLayoutElement innerInner in inner._interiorContours)
                        innerInner.AddPolygonsRecursively(list);
            }

            internal void TryAddFromList(List<List<Polygon>> outerContours)
            {
                //list of units - "the way" in the tree nesting
                List<Contour> branchList = new List<Contour>();
                ContourLayoutElement tempElement = this;
                while (tempElement != null)
                {
                    branchList.Add(tempElement.Contour);
                    tempElement = tempElement.ParentElement;
                }

                // check the path in the tree for a match with the list item nesting
                for (int i = 0; i < outerContours.Count; i++)
                    //number of elements must be the same
                    if (branchList.Count == outerContours[i].Count - 1)
                    {
                        int matchesCount = 0;
                        foreach (Contour c in branchList)
                        {
                            for (int j = 1; j < outerContours[i].Count; j++)
                                if (c == outerContours[i][j].Contours[0])
                                {
                                    matchesCount++;
                                    break;
                                }
                        }

                        if (matchesCount == branchList.Count)
                        {
                            // path in the tree the same as the list element nesting
                            ContourLayoutElement newElement =
                                new ContourLayoutElement(this, outerContours[i][0].Contours[0]);
                            this.InnerContours.Add(newElement);
                            outerContours.RemoveAt(i);
                            return;
                        }
                    }

                // recursively nested
                foreach (ContourLayoutElement inner in _interiorContours)
                    inner.TryAddFromList(outerContours);
            }

            internal ContourLayoutElement(ContourLayoutElement parent, Contour contour)
            {
                _parentElement = parent;
                _contour = contour;
            }
        }

        /// <summary>
        /// Removes all segments with a length less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minLength">The minimum length value</param>
        public void ReduceSegments(double minLength)
        {
            foreach (Contour c in this.Contours)
                c.ReduceSegments(minLength);

            for (int i = Contours.Count - 1; i >= 0; i--)
            {
                if (Contours[i].CoordinateCount == 0)
                    Contours.RemoveAt(i);
            }
        }

        private List<List<Polygon>> calcContourNestingList(List<Polygon> singleContourPolygons)
        {
            List<List<Polygon>> result = new List<List<Polygon>>();

            for (int i = 0; i < singleContourPolygons.Count; i++)
            {
                result.Add(new List<Polygon>());
                result[i].Add(singleContourPolygons[i]);
            }

            for (int i = 0; i < singleContourPolygons.Count; i++)
                for (int j = 0; j < singleContourPolygons.Count; j++)
                {
                    if (i != j)
                    {
                        ICoordinate testPoint = null;
                        // As we take the middle of the check point of the first segment of the contour.
                        // Since segments - are edges of a planar graph, 
                        // the mid point of any length is guaranteed not to be owned by both circuits at once.
                        if (singleContourPolygons[i].Contours[0].Vertices.Count > 1)
                            testPoint = (new Segment(singleContourPolygons[i].Contours[0].Vertices[0], singleContourPolygons[i].Contours[0].Vertices[1])).Center();

                        if (testPoint != null && ContainsPoint(testPoint, singleContourPolygons[j].Contours))
                            result[i].Add(singleContourPolygons[j]);
                    }
                }

            return result;
        }

        /// <summary>
        /// Splits this polygon to connected domains.
        /// Each domain is representing by the polygon with 
        /// one exterior and many interior contours.
        /// </summary>
        /// <returns>List of polygons representing connected domains</returns>
        public List<Polygon> SplitToConnectedDomains()
        {
            // we outline a simplified polygon
            List<Contour> contours = GetSimplifiedContours();
            List<Polygon> singleContourPolygons = new List<Polygon>();

            // form a list of single-circuit landfills
            foreach (Contour c in contours)
            {
                Polygon p = new Polygon();
                p.Contours.Add(c);
                singleContourPolygons.Add(p);
            }

            // list of single-loop nesting grounds each other
            List<List<Polygon>> outerContours = calcContourNestingList(singleContourPolygons);

            // list of trees nested loops at each other
            List<ContourLayoutElement> nestingTreeList = new List<ContourLayoutElement>();

            List<int> contoursToDelete = new List<int>();
            // add external contours do not lie within the other circuits
            // (Yes, the outer contours can be placed inside other polygon lines)
            for (int i = 0; i < outerContours.Count; i++)
            {
                if (outerContours[i].Count == 1)
                {
                    nestingTreeList.Add(new ContourLayoutElement(null, outerContours[i][0].Contours[0]));
                    contoursToDelete.Add(i);
                }
            }

            for (int i = contoursToDelete.Count - 1; i >= 0; i--)
                outerContours.RemoveAt(contoursToDelete[i]);

            // forming a tree nesting
            while (outerContours.Count > 0)
                foreach (ContourLayoutElement layoutElement in nestingTreeList)
                    layoutElement.TryAddFromList(outerContours);

            // calculate the resulting list of polygons
            List<Polygon> result = new List<Polygon>();
            foreach (ContourLayoutElement layoutElement in nestingTreeList)
                layoutElement.AddPolygonsRecursively(result);

            nestingTreeList.Clear();
            
            return result;
        }

        /// <summary>
        /// Normalizes the topology of the polygon.
        /// Contours are transformed to not intersected. 
        /// Orientations are set clockwise for holes and 
        /// counter clockwise for shells.
        /// </summary>
        public void Simplify()
        {
            Contours = GetSimplifiedContours();
        }

        /// <summary>
        /// Checks the topology for validity in compliance with the OGC.
        /// First contour in list is external, the other - internal.
        /// Not connected domains considered invalid.
        /// <remarks>
        /// The MapAround.Geometry.Polygon class represents a 2D domain,
        /// that may not be connected. However, the OGC standards require 
        /// connectivity. To obtain such polygons you need to use a 
        /// MapAround.Geometry.Polygon.SplitToConnectedDomains method.
        /// </remarks>
        /// </summary>
        /// <returns>true, if this polygon is valid, false otherwise</returns>
        public bool IsOGCValid()
        {
            return GetOGCValidationError() == null;
        }

        /// <summary>
        /// Calculates the error in the polygon definition in compliance with the OGC.
        /// </summary>
        /// <returns>Aa object representing the error or null</returns>
        public ValidationError GetOGCValidationError()
        {
            if (Contours.Count > 0)
            {
                List<KeyValuePair<int, int>> touchedContourPairs = new List<KeyValuePair<int, int>>();

                // check repeat points
                ValidationError ve = checkRepeatedPoints();
                if (ve != null) return ve;

                // unauthorized persons checking circuits
                ve = checkContoursOrientation();
                if (ve != null) return ve;

                // check the location of the internal circuits within the outer
                ve = getHolesOutOfShellError(touchedContourPairs);
                if (ve != null) return ve;

                // check internal circuits disjointness
                ve = getHolesIntersectionError(touchedContourPairs);
                if (ve != null) return ve;

                // check cycle in contact with each other
                // cycle means a violation of a linear connection
                if (hasCycles(touchedContourPairs))
                    return new ValidationError(InvalidityCase.MutualTouchesViolateLinearConnectivity, null, -1);
            }
 
            return null;
        }

        #region helpers Mapcheck

        private ValidationError checkRepeatedPoints()
        {
            for (int j = 0; j < Contours.Count; j++)
            {
                Contour c = Contours[j];
                for (int i = 0; i < c.Vertices.Count - 1; i++)
                    if (c.Vertices[i].X == c.Vertices[i + 1].X &&
                       c.Vertices[i].Y == c.Vertices[i + 1].Y)
                        return new ValidationError(InvalidityCase.RepeatedPoints, (ICoordinate)c.Vertices[i].Clone(), j);
            }

            return null;
        }

        private ValidationError checkContoursOrientation()
        {
            if (Contours.Count > 0)
            {
                // check the orientation of the outer loop
                Contour.Orientation or = Contours[0].GetOrientation();
                if (or == Contour.Orientation.CCW)
                {
                    // check the orientation of the internal contours
                    for (int i = 1; i < Contours.Count; i++)
                    {
                        or = Contours[i].GetOrientation();
                        if (or != Contour.Orientation.CW)
                        {
                            switch (or)
                            {
                                case Contour.Orientation.Undefined:
                                    ICoordinate p = Contours[i].GetSelfIntersectionPoint();
                                    return new ValidationError(InvalidityCase.SelfIntersection, p, i);
                                case Contour.Orientation.CCW:
                                    return new ValidationError(InvalidityCase.WrongOrientation, null, i);
                            }
                        }
                    }
                }
                else
                {
                    switch (or)
                    {
                        case Contour.Orientation.Undefined:
                            ICoordinate p = Contours[0].GetSelfIntersectionPoint();
                            return new ValidationError(InvalidityCase.SelfIntersection, p, 0);
                        case Contour.Orientation.CW:
                            return new ValidationError(InvalidityCase.WrongOrientation, null, 0);
                    }
                }
            }
            return null;
        }

        private ValidationError getHolesOutOfShellError(List<KeyValuePair<int, int>> touchedContourPairs)
        {
            ValidationError ve = getShellPointInHole(touchedContourPairs);
            if (ve != null) return ve;
            ve = getHolePointInShell(touchedContourPairs);
            if (ve != null) return ve;

            return null;
        }

        private ValidationError getShellPointInHole(List<KeyValuePair<int, int>> touchedContourPairs)
        {
            for (int i = 1; i < Contours.Count; i++)
            {
                bool nonPointTouchesChecked = false;
                foreach (ICoordinate p in Contours[i].Vertices)
                {
                    if (!nonPointTouchesChecked)
                    {
                        if (Contours[0].PointLiesOn(p))
                        {
                            bool hasPointTouch = false;
                            if (hasNonSinglePointTouch(Contours[i], Contours[0], out hasPointTouch))
                                return new ValidationError(InvalidityCase.WrongMutualTouch, p, i);
                            if (hasPointTouch)
                                touchedContourPairs.Add(new KeyValuePair<int, int>(0, i));
                            nonPointTouchesChecked = true;
                            continue;
                        }
                    }
                    if (!Contours[0].PointLiesInside(p))
                    {
                        if (nonPointTouchesChecked || !Contours[0].PointLiesOn(p))
                            return new ValidationError(InvalidityCase.HoleOutOfShell, p, i);
                    }
                }
            }

            return null;
        }

        private ValidationError getHolePointInShell(List<KeyValuePair<int, int>> touchedContourPairs)
        {
            for (int i = 1; i < Contours.Count; i++)
            {
                bool nonPointTouchesChecked = false;
                foreach (ICoordinate p in Contours[0].Vertices)
                {
                    if (!nonPointTouchesChecked)
                    {
                        if (Contours[i].PointLiesOn(p))
                        {
                            bool hasPointTouch = false;
                            if (hasNonSinglePointTouch(Contours[i], Contours[0], out hasPointTouch))
                                return new ValidationError(InvalidityCase.WrongMutualTouch, p, i);
                            if (hasPointTouch)
                            {
                                KeyValuePair<int, int> kvp = new KeyValuePair<int, int>(0, i);
                                if (touchedContourPairs.Contains(kvp))
                                    return new ValidationError(InvalidityCase.WrongMutualTouch, p, i);
                                touchedContourPairs.Add(kvp);
                            }
                            nonPointTouchesChecked = true;
                            continue;
                        }
                    }
                    if (Contours[i].PointLiesInside(p))
                    {
                        if (nonPointTouchesChecked || !Contours[i].PointLiesOn(p))
                            return new ValidationError(InvalidityCase.HoleOutOfShell, p, i);
                    }
                }
            }

            return null;
        }

        private ValidationError getHolesIntersectionError(List<KeyValuePair<int, int>> touchedContourPairs)
        {
            for (int i = 1; i < Contours.Count; i++)
            {
                BoundingRectangle br1 = Contours[i].GetBoundingRectangle();
                for (int j = 1; j < Contours.Count; j++)
                {
                    if (j == i) continue;

                    BoundingRectangle br2 = Contours[j].GetBoundingRectangle();
                    if (br1.Intersects(br2))
                    {
                        bool nonPointTouchesChecked = false;
                        foreach (ICoordinate p in Contours[i].Vertices)
                        {
                            if (!nonPointTouchesChecked)
                            {
                                if (Contours[j].PointLiesOn(p))
                                {
                                    bool hasPointTouch = false;
                                    if (hasNonSinglePointTouch(Contours[i], Contours[j], out hasPointTouch))
                                        return new ValidationError(InvalidityCase.WrongMutualTouch, p, i);
                                    if (hasPointTouch)
                                        touchedContourPairs.Add(new KeyValuePair<int, int>(Math.Min(i, j), Math.Max(i, j)));
                                    nonPointTouchesChecked = true;
                                    continue;
                                }
                            }
                            if (Contours[j].PointLiesInside(p))
                            {
                                if (nonPointTouchesChecked || !Contours[j].PointLiesOn(p))
                                    return new ValidationError(InvalidityCase.HolesAreIntersected, p, i);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private bool hasCycles(List<KeyValuePair<int, int>> touchedContourPairs)
        {
            List<int> touchedContourIndices = new List<int>();
            foreach (KeyValuePair<int, int> a in touchedContourPairs)
            {
                if (!touchedContourIndices.Contains(a.Key))
                    touchedContourIndices.Add(a.Key);

                if (!touchedContourIndices.Contains(a.Value))
                    touchedContourIndices.Add(a.Value);
            }
            int cnt = touchedContourIndices.Count;

            bool[,] connectivity = new bool[cnt, cnt];
            for (int i = 0; i < cnt - 1; i++)
                for (int j = i + 1; j < cnt; j++)
                {
                    connectivity[i, j] = false;
                    connectivity[j, i] = false;
                    for (int k = 0; k < touchedContourPairs.Count; k++)
                    {
                        if ((touchedContourPairs[k].Key == touchedContourIndices[i] &&
                             touchedContourPairs[k].Value == touchedContourIndices[j]) ||
                            (touchedContourPairs[k].Key == touchedContourIndices[j] &&
                             touchedContourPairs[k].Value == touchedContourIndices[i]))
                        {
                            connectivity[i, j] = true;
                            connectivity[j, i] = true;
                        }
                    }
                }

            return hasCycles(connectivity);
        }

        private bool hasCycles(bool[,] connectivity)
        {
            for (int i = 0; i < connectivity.GetLength(0); i++)
                for (int j = i + 1; i < connectivity.GetLength(0); i++)
            {
                if (connectivity[i, j])
                {
                    List<int> chain = new List<int>();
                    chain.Add(i);
                    chain.Add(j);
                    if (processCycle(connectivity, chain, false))
                        return true;
                }
            }
            return false;
        }

        private bool processCycle(bool[,] connectivity, List<int> chain, bool horizontal)
        {
            for (int i = 0; i < connectivity.GetLength(0); i++)
            {
                int lastChainElement = chain[chain.Count - 1];
                if (lastChainElement == i) continue;

                bool currentConnection = 
                    horizontal ?
                    connectivity[lastChainElement, i] :
                    connectivity[i, lastChainElement];

                if (currentConnection)
                {
                    if (chain.Contains(i))
                    {
                        if (chain.Count > 2)
                        {
                            if(i != chain[chain.Count - 2])
                                return true;
                        }
                    }
                    else
                    {
                        chain.Add(i);
                        if (processCycle(connectivity, chain, !horizontal))
                            return true;

                        chain.RemoveAt(chain.Count - 1);
                    }
                }
            }
            return false;
        }

        private bool hasNonSinglePointTouch(Contour c1, Contour c2, out bool hasPointTouch)
        {
            int vc1 = c1.Vertices.Count;
            int vc2 = c2.Vertices.Count;
            ICoordinate pointStub = null;
            ICoordinate oldTouchPoint = null;
            hasPointTouch = true;

            int touchPointCount = 0;
            Segment segmentStub = new Segment(PlanimetryEnvironment.NewCoordinate(0, 0), PlanimetryEnvironment.NewCoordinate(0, 0));

            for (int i1 = 0; i1 < vc1; i1++)
                for (int i2 = 0; i2 < vc2; i2++)
            {
                int j1 = i1 == vc1 - 1 ? 0 : i1 + 1;
                int j2 = i2 == vc2 - 1 ? 0 : i2 + 1;

                Segment s1 = new Segment(c1.Vertices[i1], c1.Vertices[j1]);
                Segment s2 = new Segment(c2.Vertices[i2], c2.Vertices[j2]);
                Dimension dimension =
                    PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out pointStub, out segmentStub);

                if (dimension == MapAround.Geometry.Dimension.One)
                    return true;

                if (dimension == MapAround.Geometry.Dimension.Zero)
                {
                    if (touchPointCount > 0)
                    {
                        if (pointStub != oldTouchPoint)
                            touchPointCount++;
                    }
                    else
                        touchPointCount++;

                    oldTouchPoint = pointStub;
                }

                if (touchPointCount > 1)
                    return true;
            }

            if (touchPointCount == 0)
                hasPointTouch = false;
            return false;
        }

        #endregion



        /// <summary>
        /// Computes an intersection of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the intersection</param>
        /// <returns>A collection of geometries that represents the intersection</returns>
        public GeometryCollection Intersection(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the union</param>
        /// <returns>A collection of geometries that represents the union</returns>
        public GeometryCollection Union(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the difference</param>
        /// <returns>A collection of geometries that represents the difference</returns>
        public GeometryCollection Difference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the symmetric difference</param>
        /// <returns>A collection of geometries that represents the symmetric difference</returns>
        public GeometryCollection SymmetricDifference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SymmetricDifference(this, other);
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
        /// <returns>A list containing contours of the topologically normalized polygon</returns>
        public List<Contour> GetSimplifiedContours()
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SimplifyContours(Contours);
        }

        /// <summary>
        /// Indicates if the point lies on the area bounded by the specified contours.
        /// </summary>
        /// <param name="point">Coordinate of point</param>
        /// <param name="contours">A list of contours</param>
        /// <returns>true, if the point lies on the area bounded by the contours, false otherwise</returns>
        public static bool ContainsPoint(ICoordinate point, IList<Contour> contours)
        {
            if(contours == null)
                return false;

            if(contours.Count == 0)
                return false;

            int crossCount = 0;

            foreach (Contour c in contours)
                if (c.PointLiesInside(point))
                    crossCount++;

            return crossCount % 2 == 1;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            if (Contours.Count == 0 || CoordinateCount == 0)
                return new BoundingRectangle();

            Segment result = new Segment(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

            foreach (Contour c in Contours)
                foreach (ICoordinate p in c.Vertices)
                {
                    if (p.X < result.V1.X) result.V1.X = p.X;
                    if (p.X > result.V2.X) result.V2.X = p.X;
                    if (p.Y < result.V1.Y) result.V1.Y = p.Y;
                    if (p.Y > result.V2.Y) result.V2.Y = p.Y;
                }

            return new BoundingRectangle(result.V1.X, result.V1.Y, result.V2.X, result.V2.Y);;
        }

        /// <summary>
        /// Indicates if the point lies on the area bounded by 
        /// the polygon boundary.
        /// </summary>
        /// <param name="point">Coordinate of point</param>
        /// <returns>true, if the point lies on the area bounded by 
        /// the polygon boundary, false otherwise</returns>
        public bool ContainsPoint(ICoordinate point)
        {
            return ContainsPoint(point, Contours);
        }

        private ICoordinate getInteriorPoint(Segment besector, double minY)
        {
            bool isLooped = besector.V1.Y - minY < PlanimetryAlgorithms.Tolerance;

            List<ICoordinate> besections = new List<ICoordinate>();
            foreach (Contour c in Contours)
            {
                for (int i = 0; i < c.Vertices.Count; i++)
                {
                    ICoordinate p = null;
                    Segment s = new Segment();

                    Segment currentSegment = new Segment(c.Vertices[i], c.Vertices[i == c.Vertices.Count - 1 ? 0 : i + 1]);
                    double currentSegmentMinY = Math.Min(currentSegment.V1.Y, currentSegment.V2.Y);
                    Dimension cross = PlanimetryAlgorithms.SegmentsIntersection(besector, currentSegment, out p, out s);
                    if (cross == MapAround.Geometry.Dimension.Zero && p.Y != currentSegmentMinY)
                        besections.Add(p);
                    if (cross == MapAround.Geometry.Dimension.One)
                    {
                        if (isLooped)
                            return currentSegment.V1;

                        double newY = (besector.V1.Y + minY) / 2;
                        return getInteriorPoint(new Segment(besector.V1.X, newY, besector.V2.X, newY), minY);
                    }
                }
            }

            PlanimetryAlgorithms.OrderPointsOverAxis(besections);
            double maxDistance = -1;
            int maxIndex = -1;
            for (int i = 1; i < besections.Count; i += 2)
            {
                double currentDistance = PlanimetryAlgorithms.Distance(besections[i], besections[i - 1]);
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    maxIndex = i;
                }
            }
            if (maxIndex == -1)
            {
                //if (besections.Count > 0)
                //    return besections[0];
                //else
                    throw new InvalidOperationException("Unable to calculate interior point of polygon");
            }

            if (maxDistance <= PlanimetryAlgorithms.Tolerance)
            {
                if (isLooped)
                    throw new InvalidOperationException("Unable to calculate interior point of polygon");

                double newY = (besector.V1.Y + minY) / 2;
                return getInteriorPoint(new Segment(besector.V1.X, newY, besector.V2.X, newY), minY);
            }

            return (new Segment(besections[maxIndex], besections[maxIndex - 1])).Center();
        }

        /// <summary>
        /// Calculates the point located on this polygon.
        /// </summary>
        /// <returns>Coordinate of point located on this polygon</returns>
        public ICoordinate PointOnSurface()
        {
            if (Contours.Count == 0)
                throw new InvalidOperationException("Unable to calculate interior point of polygon. It has no contours.");

            bool isSingular = true;
            foreach(Contour c in Contours)
                if (c.Vertices.Count > 2)
                {
                    isSingular = false;
                    break;
                }

            if(isSingular)
                throw new InvalidOperationException("Unable to calculate interior point of polygon. It is degenerate.");

            // Bisektrisa must pass through at least one circuit.
            // The middle of the bounding box of the polygon does not guarantee that, 
            // if the region is not simply connected, so we get the middle 
            // of the bounding box of the circuit with the largest area.
            double maxArea = double.MinValue;
            Contour targetContour = null;
            foreach (Contour c in Contours)
            {
                if (c.Vertices.Count > 2)
                {
                    double currentArea = Math.Abs(c.SignedArea());
                    if (maxArea < currentArea)
                    {
                        maxArea = currentArea;
                        targetContour = c;
                    }
                }
            }

            if (targetContour != null)
            {
                BoundingRectangle box = this.GetBoundingRectangle();
                double y = targetContour.GetBoundingRectangle().Center().Y;
                Segment besector = new Segment(box.Min.X, y, box.Max.X, y);

                return getInteriorPoint(besector, box.MinY);
            }
            else
                throw new InvalidOperationException("Unable to calculate interior point of polygon. It is degenerate.");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Polygon"/>.
        /// </summary>
        public Polygon()
        {
            _c = _contours;
        }

        /// <summary>
        /// Initializes a new instance of  <see cref="MapAround.Geometry.Polygon"/> 
        /// with single contour.
        /// </summary>
        /// <param name="vertices">Enumerator of vertices</param>
        public Polygon(IEnumerable<ICoordinate> vertices)
        {
            _c = _contours;
            Contour c = new Contour(vertices);
            Contours.Add(c);
        }

        #region ISpatialRelative Members

        /// <summary>
        /// Indicates if the two geometries are define the same set of points in the plane.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the equality</param>
        /// <returns>true, if the two geometries are define the same set of points in the plane, false otherwise</returns>
        bool ISpatialRelative.Equals(ISpatialRelative other)
        {
            if (!(other is Polygon))
                return false;

            return Relate(other, "T*F**FFF*");
        }

        /// <summary>
        /// Indicates if the two geometries intersect in a geometry of lesser dimension.
        /// See <c>ISpatialRelative.Crosses</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the crosses predicate</param>
        /// <returns>true, if the two geometries intersect in a geometry of lesser dimension, false otherwise</returns>
        public bool Crosses(ISpatialRelative other)
        {
            if (other is Polyline)
                return Relate(other, "T*****T**");

            return false;
        }

        /// <summary>
        /// Indicates if the two geometries share no points in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the two geometries share no points in common, false otherwise</returns>
        public bool Disjoint(ISpatialRelative other)
        {
            return Relate(other, "FF*FF****");
        }

        /// <summary>
        /// Indicates if the intersection of the two geometries has the same dimension as one of the input geometries.
        /// See <c>ISpatialRelative.Overlaps</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the intersection of the two geometries has the same dimension as one of the input geometries, false otherwise</returns>
        public bool Overlaps(ISpatialRelative other)
        {
            if (other.Dimension != MapAround.Geometry.Dimension.Two)
                return false;

            return Relate(other, "T*T***T**");
        }

        /// <summary>
        /// Indicates if the two geometries share at least one point in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the intersects predicate</param>
        /// <returns>true, if the two geometries share at least one points in common, false otherwise</returns>
        public bool Intersects(ISpatialRelative other)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, "TT*TT****");
            if (matrix.Matches("T********") ||
               matrix.Matches("*T*******") ||
               matrix.Matches("***T*****") ||
               matrix.Matches("****T****"))
                return true;

            return false;
        }

        /// <summary>
        /// Indicates if the boundaries of the geometries intersect.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the touches predicate</param>
        /// <returns>true, if the boundaries of the geometries intersect, false otherwise</returns>
        public bool Touches(ISpatialRelative other)
        {
            if (other is PointD || other is MultiPoint)
                return Relate(other, "F**T*****");

            if (other is Polyline)
            {
                IntersectionMatrix matrix = new IntersectionMatrix();
                matrix.CalculatePartial(this, other, "F**TT****");
                if (matrix.Matches("F**T*****") ||
                   matrix.Matches("F***T****"))
                    return true;
            }

            if (other is Polygon)
                return Relate(other, "F***T****");

            return false;
        }

        /// <summary>
        /// Indicates if this geometry is contained (is within) another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the within predicate</param>
        /// <returns>true, if this geometry is contained another geometry, false otherwise</returns>
        public bool Within(ISpatialRelative other)
        {
            return Relate(other, "T*F**F***");
        }

        /// <summary>
        /// Indicates if this geometry contains the other geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the contains predicate</param>
        /// <returns>true, if this geometry contains the other geometry, false otherwise</returns>
        public bool Contains(ISpatialRelative other)
        {
            return Relate(other, "T*****FF*");
        }

        /// <summary>
        /// Indicates if the defined relationship exists.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the defined relation</param>
        /// <param name="template">Template of the intersection matrix that defines relation</param>
        /// <returns>true, if the defined relationship exists, false otherwise</returns>
        public bool Relate(ISpatialRelative other, string template)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, template);
            return matrix.Matches(template);
        }

        #endregion
    }

    /// <summary>
    /// Topology invalidity case.
    /// </summary>
    public enum InvalidityCase
    {
        /// <summary>
        /// The definition contains repeated points.
        /// </summary>
        RepeatedPoints,
        /// <summary>
        /// The definition contains invalid self-intersection.
        /// </summary>
        SelfIntersection,
        /// <summary>
        /// Contour has wrong orientation.
        /// </summary>
        WrongOrientation,
        /// <summary>
        /// The hole contour is exceeds the shell contour.
        /// </summary>
        HoleOutOfShell,
        /// <summary>
        /// The hole contours have intersection. 
        /// </summary>
        HolesAreIntersected,
        /// <summary>
        /// Contours have wrong mutual touch.
        /// Touching more than a single point or a common segment.
        /// </summary>
        WrongMutualTouch,
        /// <summary>
        /// Line path have a self-intersection or a common segment.
        /// </summary>
        WrongSelfTouch,
        /// <summary>
        /// Mutual touches of contours violate linear connectivity.
        /// </summary>
        MutualTouchesViolateLinearConnectivity
    }

    /// <summary>
    /// Represents an error in the definition of object .
    /// </summary>
    public class ValidationError
    {
        private InvalidityCase _case;
        private ICoordinate _errorLocation;
        private int _errorSequenceIndex;

        /// <summary>
        /// Invalidity case.
        /// </summary>
        public InvalidityCase Case
        {
            get { return _case; }
        }

        /// <summary>
        /// Gets a coordinate indicating the position of error.
        /// </summary>
        public ICoordinate ErrorLocation
        {
            get { return _errorLocation; }
        }

        /// <summary>
        /// Gets an index of the erroneous coordinate sequence.
        /// </summary>
        public int ErrorSequenceIndex
        {
            get { return _errorSequenceIndex; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.ValidationError.
        /// </summary>
        /// <param name="invCase">Invalidity case</param>
        /// <param name="location">Position of an error</param>
        /// <param name="sequenceIndex">An index of the erroneous coordinate sequence</param>
        internal ValidationError(InvalidityCase invCase, ICoordinate location, int sequenceIndex)
        {
            _case = invCase;
            _errorLocation = location;
            _errorSequenceIndex = sequenceIndex;
        }
    }

    /// <summary>
    /// A sequence of connected segments.
    /// <para>
    /// Contains no discontinuities. Any two points of this geometry can be 
    /// connected by a line, so that all points of this line will lie on 
    /// this geometry.
    /// </para>
    /// </summary>
    [Serializable]
    public class LinePath : IGeometry
    {
        private bool _locked = false;
        private List<ICoordinate> _vertices = new List<ICoordinate>();
        private IList<ICoordinate> _v = new List<ICoordinate>();

        private void weedSection(int startIndex, int endIndex, bool[] marker, double epsilon)
        {
            if (endIndex - startIndex < 2)
                return;

            double maxDistance = 0;
            int indexOfMaxDistant = -1;
            Segment s = new Segment(Vertices[startIndex], Vertices[endIndex]);
            bool isSingular = s.IsSingular();
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (!marker[i])
                {
                    double currentDistance;

                    if (!isSingular)
                        currentDistance = PlanimetryAlgorithms.DistanceToLine(s, Vertices[i]);
                    else
                        currentDistance = PlanimetryAlgorithms.Distance(Vertices[i], s.V1);

                    if (maxDistance <= currentDistance)
                    {
                        maxDistance = currentDistance;
                        indexOfMaxDistant = i;
                    }
                }
            }

            if (maxDistance > epsilon)
            {
                marker[indexOfMaxDistant] = true;
                weedSection(startIndex, indexOfMaxDistant, marker, epsilon);
                weedSection(indexOfMaxDistant, endIndex, marker, epsilon);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }

        /// <summary>
        /// Gets or sets a list of vertices coordinates.
        /// </summary>
        public IList<ICoordinate> Vertices 
        {
            get
            { return _v; }
            set
            {
                if (_locked)
                    throw new InvalidOperationException("Object is read only");

                _v = _vertices = value as List<ICoordinate>; 
            }
        }

        internal void Lock()
        {
            if (_locked) return;

            _locked = true;
            List<ICoordinate> coords = new List<ICoordinate>();
            foreach (ICoordinate c in _vertices)
                coords.Add(c.ReadOnlyCopy());

            _vertices = coords;
            _v = _vertices.AsReadOnly();
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _locked = false;

            for (int i = 0; i < _vertices.Count; i++)
                _vertices[i] = PlanimetryEnvironment.NewCoordinate(_vertices[i].Values());

            _v = _vertices;
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[Vertices.Count];

            int i = 0;
            foreach (ICoordinate p in Vertices)
            {
                result[i] = (ICoordinate)p.Clone();
                i++;
            }

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Calculates the length of the line path.
        /// </summary>
        public double Length()
        {
            if (Vertices.Count <= 1)
                return 0;

            double result = 0;
            int i;
            for (i = 0; i < Vertices.Count - 1; i++)
                result += PlanimetryAlgorithms.Distance(Vertices[i], Vertices[i + 1]);

            return result;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            return PlanimetryAlgorithms.GetPointsBoundingRectangle(Vertices);
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get
            {
                return Vertices.Count;
            }
        }

        /// <summary>
        /// Calculates the center of mass of the vertices of the line path.
        /// </summary>
        /// <remarks>
        /// Masses of vertices are set equal.
        /// </remarks>
        public ICoordinate VerticesCentroid()
        {
            return PlanimetryAlgorithms.GetCentroid(Vertices);
        }

        /// <summary>
        /// Calculates the center of mass of the segments of the line path.
        /// </summary>
        /// <remarks>
        /// The density of edges is identical.
        /// </remarks>
        public ICoordinate EdgesCentroid()
        {
            if (Vertices.Count == 0)
                return null;

            ICoordinate result = PlanimetryEnvironment.NewCoordinate(0, 0);
            double perimeter = 0;
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                Segment s = new Segment(Vertices[i].X, Vertices[i].Y, Vertices[i + 1].X, Vertices[i + 1].Y);
                double l = s.Length();
                ICoordinate center = s.Center();
                result.X += l * center.X;
                result.Y += l * center.Y;
                perimeter += l;
            }

            result.X = result.X / perimeter;
            result.Y = result.Y / perimeter;

            return result;
        }

        /// <summary>
        /// Generalizes this LinePath.
        /// <para>
        /// The Douglas-Pecker algorithm implementation. 
        /// Points, that are closer than epsilon from the 
        /// direction specified neighboring points  - are 
        /// discarded.
        /// </para>
        /// </summary>
        public void Weed(double epsilon)
        {
            bool[] marker = new bool[Vertices.Count];
            for (int i = 0; i < marker.Length; i++)
                marker[i] = false;

            marker[0] = true;
            marker[marker.Length - 1] = true;

            weedSection(0, Vertices.Count - 1, marker, epsilon);

            int j = 0;
            List<ICoordinate> result = new List<ICoordinate>();
            foreach(bool b in marker)
            {
                if (b) result.Add(Vertices[j]);
                j++;
            }
            Vertices = result;
        }

        /// <summary>
        /// Removes all segments with a length less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minLength">The minimum length value</param>
        public void ReduceSegments(double minLength)
        {
            if (minLength < 0)
                throw new ArgumentException("Minimum length should not be negative.", "minLength");

            if (Vertices.Count == 0)
                return;

            LinePath path = new LinePath();
            ICoordinate initialPoint = this.Vertices[0];
            Segment segment;
            for (int i = 0; i < this.Vertices.Count - 1; i++)
            {
                segment = new Segment(initialPoint.X, initialPoint.Y, this.Vertices[i + 1].X, this.Vertices[i + 1].Y);
                if (segment.Length() > minLength)
                {
                    path.Vertices.Add(initialPoint);
                    initialPoint = this.Vertices[i + 1];
                }
            }

            if (path.Vertices.Count > 0)
            {
                if (PlanimetryAlgorithms.Distance(path.Vertices[path.Vertices.Count - 1],
                                                  this.Vertices[this.Vertices.Count - 1]) > minLength)
                    path.Vertices.Add(this.Vertices[this.Vertices.Count - 1]);

                //part of the polyline does not contain segments
                if (path.Vertices.Count == 1)
                    path.Vertices.Clear();
            }

            this.Vertices = path.Vertices;
        }

        /// <summary>
        /// Computes a monotone chains of this line path.
        /// </summary>
        /// <remarks>
        /// Singular segments can not be included in monotone chains and will throw an exception.
        /// The Labels collection contains segment indices.
        /// </remarks>
        /// <returns>A list containing monotone chains</returns>
        public IList<MonotoneChain> GetMonotoneChains()
        {
            List<MonotoneChain> result = new List<MonotoneChain>();
            AppendMonotoneChains(result);
            return result;
        }

        /// <summary>
        /// Computes a monotone chains of this line path and then 
        /// adds it to the specified list.
        /// </summary>
        /// <remarks>
        /// Singular segments can not be included in monotone chains and will throw an exception.
        /// The Labels collection contains segment indices.
        /// </remarks>
        /// <param name="chains">A list to adds monotone chains</param>
        public void AppendMonotoneChains(IList<MonotoneChain> chains)
        {
            MonotoneChain currentChain = null;
            Segment s = new Segment();

            //build a list of monotone chain segments
            for (int i = 0; i < this.Vertices.Count - 1; i++)
            {
                s.V1 = this.Vertices[i];
                s.V2 = this.Vertices[i + 1];

                if (currentChain == null)
                    currentChain = new MonotoneChain(s, new SegmentLabel(0, 0, i));
                else
                {
                    bool segmentAdded = currentChain.InsertSegment(s, new SegmentLabel(0, 0, i));
                    if (!segmentAdded)
                    {
                        chains.Add(currentChain);
                        currentChain = new MonotoneChain(s, new SegmentLabel(0, 0, i));
                    }
                }
            }

            if (currentChain != null)
                chains.Add(currentChain);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            LinePath path = new LinePath();

            foreach (ICoordinate p in Vertices)
                path.Vertices.Add((ICoordinate)p.Clone());

            return path;
        }

        /// <summary>
        /// Reverses the sequence of vertices.
        /// </summary>
        public void Reverse()
        {
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count / 2; i++, j--)
            {
                ICoordinate temp = Vertices[i];
                Vertices[i] = Vertices[j];
                Vertices[j] = temp;
            }
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.One; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.LinePath"/>.
        /// </summary>
        public LinePath()
        {
            _v = _vertices;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.LinePath"/>.
        /// </summary>
        /// <param name="vertices">Enumerator of the vertices coordinate arrays</param>
        public LinePath(IEnumerable<double[]> vertices)
        {
            _v = _vertices;

            foreach (double[] point in vertices)
                Vertices.Add(PlanimetryEnvironment.NewCoordinate(point));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.LinePath"/>.
        /// </summary>
        /// <param name="vertices">Enumerator of vertices coordinates</param>
        public LinePath(IEnumerable<ICoordinate> vertices)
        {
            _v = _vertices;

            foreach (ICoordinate point in vertices)
                Vertices.Add((ICoordinate)point.Clone());
        }
    }


    /// <summary>
    /// Represents a polyline. 
    /// </summary>
    /// <remarks>
    /// Polyline is an ordered collection of line paths.
    /// </remarks>
    [Serializable]
    public class Polyline : ISpatialRelative
    {
        private bool _locked = false;
        private List<LinePath> _paths = new List<LinePath>();
        private IList<LinePath> _p;

        /// <summary>
        /// Gets a value indicating whether this instance is read only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _locked; }
        }

        /// <summary>
        /// Gets or sets a list containing line paths.
        /// </summary>
        public IList<LinePath> Paths 
        {
            get
            { return _p; }
            set
            {
                if (_locked)
                    throw new InvalidOperationException("Object is read only");

                _paths.Clear();
                foreach (LinePath path in value)
                {
                    LinePath p = new LinePath();
                    foreach (ICoordinate coord in path.Vertices)
                        p.Vertices.Add((ICoordinate)coord.Clone());
                    _paths.Add(p);
                }

                _p = _paths;
            }
        }

        internal void Lock()
        {
            if (_locked) return;

            _locked = true;

            List<LinePath> tempPaths = new List<LinePath>();

            foreach (LinePath path in _paths)
            {
                path.Lock();
                tempPaths.Add(path);
            }
            _paths = tempPaths;

            _p = _paths.AsReadOnly();
        }

        internal void UnLock()
        {
            if (!_locked) return;

            _locked = false;
            foreach (LinePath path in Paths)
                path.UnLock();

            _p = _paths;
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            ICoordinate[] result = new ICoordinate[CoordinateCount];

            int i = 0;
            foreach (LinePath path in Paths)
                foreach (ICoordinate p in path.Vertices)
                {
                    result[i] = p;
                    i++;
                }

            return result;
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="distance">The distance of the buffer</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geometry that represents a buffer</returns>
        public IGeometry Buffer(double distance, int pointsPerCircle, bool allowParallels)
        {
            return BufferBuilder.GetBuffer(this, distance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get
            {
                int result = 0;
                foreach (LinePath path in Paths)
                    result += path.CoordinateCount;

                return result;
            }
        }

        /// <summary>
        /// Calculates the length of the polyline.
        /// </summary>
        /// <returns>The length of the polyline</returns>
        public double Length()
        {
            double result = 0;
            foreach (LinePath path in Paths)
                result += path.Length();

                return result;
        }

        /// <summary>
        /// Generalizes this Polyline.
        /// <para>
        /// The Douglas-Pecker algorithm implementation. 
        /// Points, that are closer than epsilon from the 
        /// direction specified neighboring points  - are 
        /// discarded.
        /// </para>
        /// </summary>
        public void Weed(double epsilon)
        {
            foreach (LinePath path in Paths)
                path.Weed(epsilon);
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            if (Paths.Count == 0 || CoordinateCount == 0)
                return new BoundingRectangle();

            Segment result = new Segment(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

            foreach (LinePath path in Paths)
                foreach (ICoordinate p in path.Vertices)
                {
                    if (p.X < result.V1.X) result.V1.X = p.X;
                    if (p.X > result.V2.X) result.V2.X = p.X;
                    if (p.Y < result.V1.Y) result.V1.Y = p.Y;
                    if (p.Y > result.V2.Y) result.V2.Y = p.Y;
                }

            return new BoundingRectangle(result.V1.X, result.V1.Y, result.V2.X, result.V2.Y);
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            Polyline polyline = new Polyline();
            foreach (LinePath path in Paths)
                polyline.Paths.Add((LinePath)path.Clone());

            return polyline;
        }

        /// <summary>
        /// Snaps coordinates of all vertices to grid nodes.
        /// Coordinates can be changed to a value less than cellSize / 2.
        /// </summary>
        /// <param name="origin">The origin of the grid</param>
        /// <param name="cellSize">Cell size of the grid</param>
        public void SnapVertices(ICoordinate origin, double cellSize)
        {
            foreach (LinePath path in this.Paths)
                PlanimetryAlgorithms.SnapCoordinatesToGrid(path.Vertices, origin, cellSize);
        }

        /// <summary>
        /// Removes all segments with a length less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minLength">The minimum length value</param>
        public void ReduceSegments(double minLength)
        {
            foreach (LinePath path in this.Paths)
                path.ReduceSegments(minLength);

            for (int i = this.Paths.Count - 1; i >= 0; i--)
            {
                if (this.Paths[i].CoordinateCount == 0)
                    this.Paths.RemoveAt(i);
            }
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.One ; }
        }

        /// <summary>
        /// Computes a list of line paths of the topologically normalized polyline.
        /// </summary>
        /// <summary>
        /// These line paths do not contain intersections.
        /// Allowed only the mutual and self-touches at the start and end points.
        /// </summary>
        /// <returns>A list of line paths of the topologically normalized polyline</returns>
        public List<LinePath> GetSimplifiedPaths()
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SimplifyLinePaths(Paths);
        }

        /// <summary>
        /// Normalizes the topology of this polyline.
        /// </summary>
        public void Simplify()
        {
            Paths = GetSimplifiedPaths();
        }

        /// <summary>
        /// Determines the validity of the polyline. OGC standards does not define 
        /// polyline validity. However, this method checks validity 
        /// in accordance with the common (?) sense: 
        /// line paths can not contain duplicate points, 2D self-intersections 
        /// are not permitted.
        /// </summary>
        /// <returns>true, if the polyline is valid, false otherwise</returns>
        public bool IsValid()
        {
            return GetValidationError() == null;
        }

        /// <summary>
        /// Calculates the error in the polyline definition.
        /// </summary>
        /// <returns>An object representing an error in the polyline definition or null</returns>
        public ValidationError GetValidationError()
        {
            if (Paths.Count > 0)
            {
                // check repeat points
                ValidationError ve = checkRepeatedPoints();
                if (ve != null) return ve;

                // Checks Invalid intersections
                ve = getWrongSelfIntersectionError();
                if (ve != null) return ve;
            }

            return null;
        }

        private ValidationError getWrongSelfIntersectionError()
        {
            ICoordinate pointStub = PlanimetryEnvironment.NewCoordinate(0, 0);
            Segment segmentStub = new Segment();

            for (int i = 0; i < Paths.Count; i++)
            {
                for (int k1 = 0; k1 < Paths[i].Vertices.Count - 1; k1++)
                    for (int k2 = k1 + 1; k2 < Paths[i].Vertices.Count - 1; k2++)
                    {
                        Segment s1 = new Segment(Paths[i].Vertices[k1], Paths[i].Vertices[k1 + 1]);
                        Segment s2 = new Segment(Paths[i].Vertices[k2], Paths[i].Vertices[k2 + 1]);

                        if (PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out pointStub, out segmentStub) == MapAround.Geometry.Dimension.One)
                            return new ValidationError(InvalidityCase.WrongSelfTouch, pointStub, i);
                    }

                IList<MonotoneChain> chains1 = Paths[i].GetMonotoneChains();

                for (int j = i + 1; j < Paths.Count; j++)
                {
                    IList<MonotoneChain> chains2 = Paths[j].GetMonotoneChains();

                    List<Segment> segments = new List<Segment>();

                    foreach (MonotoneChain c1 in chains1)
                        foreach (MonotoneChain c2 in chains2)
                        { 
                            segments = c1.GetCrossSegments(c2);
                            if(segments.Count > 0)
                                return new ValidationError(InvalidityCase.SelfIntersection, segments[0].V1, i);
                        }
                }
            }

            return null;
        }

        private ValidationError checkRepeatedPoints()
        {
            for (int j = 0; j < Paths.Count; j++)
            {
                LinePath p = Paths[j];
                for (int i = 0; i < p.Vertices.Count - 1; i++)
                    if (p.Vertices[i].ExactEquals(p.Vertices[i + 1]))
                        return new ValidationError(InvalidityCase.RepeatedPoints, (ICoordinate)p.Vertices[i].Clone(), j);
            }

            return null;
        }

        /// <summary>
        /// Computes an intersection of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the intersection</param>
        /// <returns>A collection of geometries that represents the intersection</returns>
        public GeometryCollection Intersection(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the union</param>
        /// <returns>A collection of geometries that represents the union</returns>
        public GeometryCollection Union(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the difference</param>
        /// <returns>A collection of geometries that represents the difference</returns>
        public GeometryCollection Difference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geometry with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeometry to compute the symmetric difference</param>
        /// <returns>A collection of geometries that represents the symmetric difference</returns>
        public GeometryCollection SymmetricDifference(IGeometry other)
        {
            OverlayCalculator overlay = new OverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Polyline"/> 
        /// with single line path.
        /// </summary>
        /// <param name="vertices">Enumerator of vertices coordinates</param>
        public Polyline(IEnumerable<ICoordinate> vertices)
        {
            _p = _paths;

            LinePath path = new LinePath();
            foreach (ICoordinate point in vertices)
                path.Vertices.Add(point);

            Paths.Add(path);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Polyline"/>.
        /// </summary>
        /// <param name="paths">Enumerator of line paths</param>
        public Polyline(IEnumerable<LinePath> paths)
        {
            _p = _paths;

            foreach (LinePath path in paths)
                Paths.Add(path);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Polyline"/>.
        /// </summary>
        public Polyline()
        {
            _p = _paths;
        }

        #region ISpatialRelative Members

        /// <summary>
        /// Indicates if the two geometries are define the same set of points in the plane.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the equality</param>
        /// <returns>true, if the two geometries are define the same set of points in the plane, false otherwise</returns>
        bool ISpatialRelative.Equals(ISpatialRelative other)
        {
            if (!(other is Polyline))
                return false;

            return Relate(other, "T*F**FFF*");
        }

        /// <summary>
        /// Indicates if the two geometries intersect in a geometry of lesser dimension.
        /// See <c>ISpatialRelative.Crosses</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the crosses predicate</param>
        /// <returns>true, if the two geometries intersect in a geometry of lesser dimension, false otherwise</returns>
        public bool Crosses(ISpatialRelative other)
        {
            if (other is Polyline)
                return Relate(other, "0********");
            if(other is Polygon)
                return Relate(other, "T*T******");

            return false;
        }

        /// <summary>
        /// Indicates if the two geometries share no points in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the two geometries share no points in common, false otherwise</returns>
        public bool Disjoint(ISpatialRelative other)
        {
            return Relate(other, "FF*FF****");
        }

        /// <summary>
        /// Indicates if the intersection of the two geometries has the same dimension as one of the input geometries.
        /// See <c>ISpatialRelative.Overlaps</c> for details.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative to compute the disjoint predicate</param>
        /// <returns>true, if the intersection of the two geometries has the same dimension as one of the input geometries, false otherwise</returns>
        public bool Overlaps(ISpatialRelative other)
        {
            if (other.Dimension != MapAround.Geometry.Dimension.One)
                return false;

            return Relate(other, "1*T***T**");
        }

        /// <summary>
        /// Indicates if the two geometries share at least one point in common.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the intersects predicate</param>
        /// <returns>true, if the two geometries share at least one points in common, false otherwise</returns>
        public bool Intersects(ISpatialRelative other)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, "TT*TT****");
            if (matrix.Matches("T********") ||
               matrix.Matches("*T*******") ||
               matrix.Matches("***T*****") ||
               matrix.Matches("****T****"))
                return true;

            return false;
        }

        /// <summary>
        /// Indicates if the boundaries of the geometries intersect.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the touches predicate</param>
        /// <returns>true, if the boundaries of the geometries intersect, false otherwise</returns>
        public bool Touches(ISpatialRelative other)
        {
            if (other is PointD || other is MultiPoint)
                return Relate(other, "F**T*****");

            if (other is Polyline || other is Polygon)
            {
                // can cross the border, or the inner region in line with the boundary of the other
                IntersectionMatrix matrix = new IntersectionMatrix();
                matrix.CalculatePartial(this, other, "FT*TT****");
                if (matrix.Matches("F**T*****") ||
                    matrix.Matches("F***T****") ||
                    matrix.Matches("FT*******"))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates if this geometry is contained (is within) another geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the within predicate</param>
        /// <returns>true, if this geometry is contained another geometry, false otherwise</returns>
        public bool Within(ISpatialRelative other)
        {
            return Relate(other, "T*F**F***");
        }

        /// <summary>
        /// Indicates if this geometry contains the other geometry.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the contains predicate</param>
        /// <returns>true, if this geometry contains the other geometry, false otherwise</returns>
        public bool Contains(ISpatialRelative other)
        {
            return Relate(other, "T*****FF*");
        }

        /// <summary>
        /// Indicates if the defined relationship exists.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.ISpatialRelative 
        /// to compute the defined relation</param>
        /// <param name="template">Template of the intersection matrix that defines relation</param>
        /// <returns>true, if the defined relationship exists, false otherwise</returns>
        public bool Relate(ISpatialRelative other, string template)
        {
            IntersectionMatrix matrix = new IntersectionMatrix();
            matrix.CalculatePartial(this, other, template);
            return matrix.Matches(template);
        }

        #endregion
    }

    /// <summary>
    /// Represents an axis-aligned rectangle.
    /// <para>
    /// Instances of this class usually used for 
    /// storing envelope of 2D planar geometries.
    /// </para>
    /// </summary>
    [Serializable]
    public class BoundingRectangle : IGeometry
    {
        private Segment? _bounds = null;
        private static readonly string _boundsUndefined = "Undefined bounds";

        /// <summary>
        /// Gets a minimum X coordinate of this geometry.
        /// </summary>
        public double MinX
        {
            get 
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V1.X; 
            }
        }

        /// <summary>
        /// Gets a minimum Y coordinate of this geometry.
        /// </summary>
        public double MinY
        {
            get
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V1.Y;
            }
        }

        /// <summary>
        /// Gets a maximum X coordinate of this geometry.
        /// </summary>
        public double MaxX
        {
            get
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V2.X;
            }
        }

        /// <summary>
        /// Gets a maximum Y coordinate of this geometry.
        /// </summary>
        public double MaxY
        {
            get
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V2.Y;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this MapAround.Geometry.BoundingRectangle 
        /// instance is equal to another.
        /// </summary>
        /// <param name="br">The instance of MapAround.Geometry.BoundingRectangle to compare with current instance</param>
        public bool Equals(BoundingRectangle br)
        {
            if (br == null)
                return false;

            if (this.IsEmpty() ^ br.IsEmpty())
                return false;

            if (this.IsEmpty() && br.IsEmpty())
                return true;

            return
                this.Min.ExactEquals(br.Min) &&
                this.Max.ExactEquals(br.Max);
        }

        /// <summary>
        /// Extracts all coordinates that define this geometry.
        /// </summary>
        /// <returns>An array containing all coordinates defining this geometry</returns>
        public ICoordinate[] ExtractCoordinates()
        {
            if (_bounds == null)
                return new ICoordinate[0];

            return new ICoordinate[] {
                                    PlanimetryEnvironment.NewCoordinate(_bounds.Value.V1.X, _bounds.Value.V1.Y),
                                    PlanimetryEnvironment.NewCoordinate(_bounds.Value.V2.X, _bounds.Value.V1.Y),
                                    PlanimetryEnvironment.NewCoordinate(_bounds.Value.V2.X, _bounds.Value.V2.Y),
                                    PlanimetryEnvironment.NewCoordinate(_bounds.Value.V1.X, _bounds.Value.V2.Y)
                                };
        }

        /// <summary>
        /// Calculates convex hull.
        /// </summary>
        /// <returns>A list containing a convex hull coordinate sequence</returns>
        public IList<ICoordinate> GetConvexHull()
        {
            return PlanimetryAlgorithms.GetConvexHull(ExtractCoordinates());
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>
        /// A bounding rectangle of the geometry.
        /// </returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            if (_bounds == null)
                throw new InvalidOperationException(_boundsUndefined);

            return new BoundingRectangle(this.Min, this.Max);
        }

        /// <summary>
        /// Gets a number of coordinate.
        /// </summary>
        public int CoordinateCount
        {
            get 
            { 
                return _bounds == null ? 0 : 4; 
            }
        }

        /// <summary>
        /// Increases the distance from the center to each side of the 
        /// rectangle by the specified amount. The central point remains in place.
        /// </summary>
        /// <param name="delta">The value by which you want to increase the rectangle</param>
        public void Grow(double delta)
        {
            if(delta < 0) 
                if(-delta > Width / 2 && -delta > Height / 2)
                {
                    _bounds = new Segment(Center(), Center());
                    return;
                }

            if (_bounds != null)
            {
                Segment s = _bounds.Value;
                s.V1.X -= delta;
                s.V1.Y -= delta;
                s.V2.X += delta;
                s.V2.Y += delta;
                _bounds = s;
            }
            else
                throw new InvalidOperationException(_boundsUndefined);
        }

        /// <summary>
        /// Indicates whether the rectangle is not defined.
        /// </summary>
        /// <returns>true if the rectangle is not defined, false otherwise</returns>
        public bool IsEmpty()
        {
            return _bounds == null;
        }

        /// <summary>
        /// Gets a width of this rectangle.
        /// </summary>
        public double Width
        {
            get 
            {
                if (_bounds != null)
                    return _bounds.Value.V2.X - _bounds.Value.V1.X;
                else
                    throw new InvalidOperationException(_boundsUndefined);
            }
        }

        /// <summary>
        /// Gets a height of this rectangle.
        /// </summary>
        public double Height
        {
            get
            {
                if (_bounds != null)
                    return _bounds.Value.V2.Y - _bounds.Value.V1.Y;
                else
                    throw new InvalidOperationException(_boundsUndefined);
            }
        }

        /// <summary>
        /// Calculates a center point of this rectangle.
        /// </summary>
        public ICoordinate Center()
        { 
            if(_bounds == null)
                throw new InvalidOperationException(_boundsUndefined);

            return _bounds.Value.Center();
        }

        /// <summary>
        /// Gets the point of minimum coordinates of rectangle.
        /// </summary>
        public ICoordinate Min
        {
            get
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V1;
            }
        }

        /// <summary>
        /// Gets the point of maximum coordinates of rectangle.
        /// </summary>
        public ICoordinate Max
        {
            get
            {
                if (_bounds == null)
                    throw new InvalidOperationException(_boundsUndefined);

                return _bounds.Value.V2;
            }
        }


        /// <summary>
        /// Makes the rectangle undefined.
        /// </summary>
        public void Clear()
        {
            _bounds = null;
        }

        /// <summary>
        /// Gets a dimension.
        /// </summary>
        public MapAround.Geometry.Dimension Dimension
        {
            get { return MapAround.Geometry.Dimension.Two; }
        }

        /// <summary>
        /// Increases the rectangle so that it includes 
        /// a rectangle passed as parameter.
        /// </summary>
        /// <param name="rectangle">The MapAround.Geometry.BoundingRectangle instance to join</param>
        public void Join(BoundingRectangle rectangle)
        {
            if (rectangle._bounds == null)
                return;

            if (_bounds == null)
            {
                _bounds = rectangle._bounds;
                return;
            }

            _bounds = PlanimetryAlgorithms.JoinRectangles(this._bounds.Value, rectangle._bounds.Value);
        }

        /// <summary>
        /// Increases the rectangle so that it includes 
        /// a point whose coordinates are passed as a 
        /// parameter.
        /// </summary>
        /// <param name="point">Point</param>
        public void Join(ICoordinate point)
        {
            if (_bounds == null)
            {
                _bounds = new Segment(point.X, point.Y, point.X, point.Y);
                return;
            }

            _bounds = new Segment(Math.Min(_bounds.Value.V1.X, point.X),
                                  Math.Min(_bounds.Value.V1.Y, point.Y),
                                  Math.Max(_bounds.Value.V2.X, point.X),
                                  Math.Max(_bounds.Value.V2.Y, point.Y));
        }

        /// <summary>
        /// Determines whether this rectangle contains a specified point.
        /// </summary>
        /// <param name="p">Point</param>
        /// <returns>true, if this rectangle contains a specified point, false otherwise</returns>
        public bool ContainsPoint(ICoordinate p)
        { 
            if(_bounds == null)
                return false;

            return PlanimetryAlgorithms.RectangleContainsPoint(_bounds.Value, p);
        }

        /// <summary>
        /// Determines whether this rectangle contains a specified rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle</param>
        /// <returns>true, if this rectangle contains a specified rectangle, false otherwise</returns>
        public bool ContainsRectangle(BoundingRectangle rectangle)
        {
            if (_bounds == null || rectangle._bounds == null)
                return false;

            return PlanimetryAlgorithms.RectangleContainsRectangle(_bounds.Value, rectangle._bounds.Value);
        }

        /// <summary>
        /// Indicates if this bounding rectangle share at least one point in common with other.
        /// </summary>
        /// <param name="box">The instance of MapAround.Geometry.BoundingRectangle 
        /// to compute the intersects predicate</param>
        /// <returns>true, if this bounding rectangle share at least one point in common with other, false otherwise</returns>
        public bool Intersects(BoundingRectangle box)
        {
            if (_bounds == null || box._bounds == null)
                return false;

            return PlanimetryAlgorithms.AreRectanglesIntersect(_bounds.Value, box._bounds.Value);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BoundingRectangle"/>.
        /// </summary>
        public BoundingRectangle()
        {
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            if (_bounds == null)
                return new BoundingRectangle();

            return new BoundingRectangle(Min, Max);
        }

        /// <summary>
        /// Determines whether this rectangle intersects with the polyline.
        /// </summary>
        /// <param name="polyline">Polyline</param>
        /// <returns>true, if this rectangle intersects with the polyline, false otherwise</returns>
        public bool Intersects(Polyline polyline)
        {
            if (this.IsEmpty())
                return false;

            if (polyline == null)
                throw new ArgumentNullException("polyline");

            if (polyline.Paths.Count == 0)
                return false;

            // if there is no overlap with the bounding box, then there is no intersection
            if (!polyline.GetBoundingRectangle().Intersects(this))
                return false;

            // some of the points of the polyline can be located within the rectangle in this case there is an intersection
            foreach (LinePath path in polyline.Paths)
                foreach (ICoordinate p in path.Vertices)
                    if (this.ContainsPoint(p))
                        return true;

            // is the "bad" case:
            // possible crossing edges, but none of the polyline vertices does not lie within the rectangle
            List<MonotoneChain> polylineChains = new List<MonotoneChain>();

            Polyline clone = (Polyline)polyline.Clone();
            clone.ReduceSegments(PlanimetryAlgorithms.Tolerance);
            foreach (LinePath path in clone.Paths)
                path.AppendMonotoneChains(polylineChains);

            if (Min.Equals(Max))
                return false;

            MonotoneChain chain1, chain2, chain3, chain4;

            if (Height <= PlanimetryAlgorithms.Tolerance)
            {
                chain1 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
                chain3 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
            }
            else
            {
                chain1 = new MonotoneChain(new Segment(Min, PlanimetryEnvironment.NewCoordinate(MinX, MaxY)));
                chain3 = new MonotoneChain(new Segment(Max, PlanimetryEnvironment.NewCoordinate(MaxX, MinY)));
            }
            if (Width <= PlanimetryAlgorithms.Tolerance)
            {
                chain2 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
                chain4 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
            }
            else
            {
                chain2 = new MonotoneChain(new Segment(PlanimetryEnvironment.NewCoordinate(MinX, MaxY), Max));
                chain4 = new MonotoneChain(new Segment(PlanimetryEnvironment.NewCoordinate(MaxX, MinY), Min));
            }

            foreach (MonotoneChain chain in polylineChains)
            {
                List<ICoordinate> croosPoints;

                croosPoints = chain.GetCrossPoints(chain1);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain2);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain3);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain4);
                if (croosPoints.Count > 0) return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this rectangle intersects with the polygon.
        /// </summary>
        /// <param name="polygon">polygon</param>
        /// <returns>true, if this rectangle intersects with the polygon, false otherwise</returns>
        public bool Intersects(Polygon polygon)
        {
            if(this.IsEmpty())
                return false;

            if(polygon == null)
                throw new ArgumentNullException("polygon");

            if(polygon.Contours.Count == 0)
                return false;

            // if there is no overlap with the bounding box, then there is no intersection
            if (!polygon.GetBoundingRectangle().Intersects(this))
                return false;

            // some of the points of the rectangle may lie within the polygon is almost guaranteed to be the intersection
            if (polygon.ContainsPoint(Min) || polygon.ContainsPoint(Max) ||
                polygon.ContainsPoint(PlanimetryEnvironment.NewCoordinate(MinX, MaxY)) ||
                polygon.ContainsPoint(PlanimetryEnvironment.NewCoordinate(MaxX, MinY)))
                return true;

            // some of the points of the polygon can be inside the rectangle in this case there is an intersection
            foreach (Contour c in polygon.Contours)
                foreach (ICoordinate p in c.Vertices)
                    if (this.ContainsPoint(p))
                        return true;

            // is the "bad" case:
            // possible crossing edges, but none of the vertices of one figure does not lie within the other
            List<MonotoneChain> polygonChains = new List<MonotoneChain>();

            Polygon clone = (Polygon)polygon.Clone();
            clone.ReduceSegments(PlanimetryAlgorithms.Tolerance);
            foreach (Contour c in clone.Contours)
                c.AppendMonotoneChains(polygonChains);

            if (Min.Equals(Max))
                return false;

            MonotoneChain chain1,chain2, chain3, chain4;

            if (Height <= PlanimetryAlgorithms.Tolerance)
            {
                chain1 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
                chain3 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
            }
            else
            {
                chain1 = new MonotoneChain(new Segment(Min, PlanimetryEnvironment.NewCoordinate(MinX, MaxY)));
                chain3 = new MonotoneChain(new Segment(Max, PlanimetryEnvironment.NewCoordinate(MaxX, MinY)));
            }
            if (Width <= PlanimetryAlgorithms.Tolerance)
            {
                chain2 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
                chain4 = new MonotoneChain(MonotoneChain.Orientation.LeftUp);
            }
            else
            {
                chain2 = new MonotoneChain(new Segment(PlanimetryEnvironment.NewCoordinate(MinX, MaxY), Max));
                chain4 = new MonotoneChain(new Segment(PlanimetryEnvironment.NewCoordinate(MaxX, MinY), Min));
            }


            foreach (MonotoneChain chain in polygonChains)
            {
                List<ICoordinate> croosPoints;

                croosPoints = chain.GetCrossPoints(chain1);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain2);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain3);
                if (croosPoints.Count > 0) return true;

                croosPoints = chain.GetCrossPoints(chain4);
                if (croosPoints.Count > 0) return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the polygon whose vertices are equal 
        /// to vertices of this rectangle.
        /// </summary>
        /// <returns>polygon whose vertices are equal 
        /// to vertices of this rectangle</returns>
        public Polygon ToPolygon()
        {
            Polygon result = new Polygon(new ICoordinate[] 
                { 
                    Min, 
                    PlanimetryEnvironment.NewCoordinate(MinX, MaxY), 
                    Max,
                    PlanimetryEnvironment.NewCoordinate(MaxX, MinY)
                });
            return result;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BoundingRectangle"/>.
        /// </summary>
        /// <param name="minCorner">The point of minimum coordinate</param>
        /// <param name="maxCorner">The point of maximum coordinate</param>
        public BoundingRectangle(ICoordinate minCorner, ICoordinate maxCorner)
        {
            if (minCorner.X > maxCorner.X || minCorner.Y > maxCorner.Y)
                throw new ArgumentException("minCorner coordinates should not be greater than maxCorner");

            _bounds = new Segment(minCorner.X, minCorner.Y, maxCorner.X, maxCorner.Y);
        }

        /// <summary>
        /// Sets coordinates of this rectangle.
        /// </summary>
        /// <param name="minX">Minimum X coordinate</param>
        /// <param name="minY">Minimum Y coordinate</param>
        /// <param name="maxX">Maximum X coordinate</param>
        /// <param name="maxY">Maximum Y coordinate</param>
        public void SetBounds(double minX, double minY, double maxX, double maxY)
        {
            if (minX > maxX)
                throw new ArgumentException("minX should not be greater than maxX.");

            if (minY > maxY)
                throw new ArgumentException("minY should not be greater than maxY.");

            _bounds = new Segment(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BoundingRectangle"/>.
        /// </summary>
        /// <param name="minX">Minimum X coordinate</param>
        /// <param name="minY">Minimum Y coordinate</param>
        /// <param name="maxX">Maximum X coordinate</param>
        /// <param name="maxY">Maximum Y coordinate</param>
        public BoundingRectangle(double minX, double minY, double maxX, double maxY)
        {
            SetBounds(minX, minY, maxX, maxY);
        }
    }

    /// <summary>
    /// Represents an axis-aligned tile.
    /// </summary>
    [Serializable]
    public class Tile
    {
        private BoundingRectangle bbox;
        private uint tileRow, tileCol, zoomLevel = 0;
        private static int height = 256, width = 256; // Every tile has 256x256 resolution
        private  double scaleDenominator, pixelSize;
        private static double tileMatrixMinX, tileMatrixMaxY;
        private Mapping.Map _map;

        /// <summary>
        /// Returns width of a tile
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// Returns height of a tile
        /// </summary>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// Constructs Tile object
        /// </summary>
        /// <param name="map"></param>
        /// <param name="tileRow"></param>
        /// <param name="tileCol"></param>
        public Tile(MapAround.Mapping.Map map, uint tileRow = 0, uint tileCol = 0)
        {
            _map = map;
            bbox = new BoundingRectangle();
            this.tileRow = tileRow;
            this.tileCol = tileCol;
        }

        /// <summary>
        /// Returns row number of a tile
        /// </summary>
        public uint Row
        {
            set { tileRow = value; }
            get { return tileRow; }
        }

        /// <summary>
        /// Returns column number of a tile
        /// </summary>
        public uint Col
        {
            set { tileCol = value; }
            get { return tileCol; }
        }

        /// <summary>
        /// Sets and gets zoom level
        /// </summary>
        public uint ZoomLevel
        {
            set { zoomLevel = value; }
            get { return zoomLevel; }
        }

        /// <summary>
        /// Gets scale denominator for this level
        /// </summary>
        public double ScaleDenominator
        {
            set { scaleDenominator = value; }
            get { return scaleDenominator;  }
        }

        /// <summary>
        /// Gets pixel size for this level
        /// </summary>
        public double PixelSize
        {
            set { pixelSize = value; }
            get { return pixelSize; }
        }

        /// <summary>
        /// Returns tile as a Bounding Rectangle
        /// </summary>
        public BoundingRectangle BBox
        {
            get
            {
                double size = 20037500;
                double pixelSpan = PixelSize,
                       tileSpanX = width * pixelSpan,
                       tileSpanY = height * pixelSpan;
                var box = new BoundingRectangle(-size, -size, size, size);

                tileMatrixMinX = box.MinX;
                tileMatrixMaxY = box.MaxY;
                                
                // Converting upper-left corner (leftX, upperY) of the tile to upper-left corner of the bbox
                double leftX = tileCol * tileSpanX + tileMatrixMinX;
                double upperY = tileMatrixMaxY - tileRow * tileSpanY; //!

                //Converting lower-right corner (rightX, lowerY) of the tile to the lower-right corner of the bbox
                double rightX = (tileCol + 1) * tileSpanX + tileMatrixMinX;
                double lowerY = tileMatrixMaxY - (tileRow + 1) * tileSpanY; //!

                bbox.SetBounds(leftX, lowerY, rightX, upperY);

                return bbox;
            }
        }

        /// <summary>
        /// Checks tile emptiness
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return bbox.IsEmpty();
        }
    }
}