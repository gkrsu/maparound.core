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
** File: Environment.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descripiotn: Classes that appears as an environment for geometric computations
**
=============================================================================*/

namespace MapAround.Geometry
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;

    /// <summary>
    /// Provides access to methods and properties of object 
    /// that represents coordinates on a 2D plane.
    /// </summary>
    public interface ICoordinate : ICloneable
    {
        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        double X
        {
            get; set; 
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        double Y
        {
            get; set;
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        bool Equals(ICoordinate p);

        /// <summary>
        /// Gets a value indicating whether this coordinate instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        bool ExactEquals(ICoordinate p);

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        void Translate(double x, double y);

        /// <summary>
        /// Gets an array containing coordinate values.
        /// </summary>
        /// <returns>An array containing coordinate values</returns>
        double[] Values();

        /// <summary>
        /// Creates a read only copy of this object.
        /// </summary>
        /// <returns>A read only copy of this object</returns>
        ICoordinate ReadOnlyCopy();

        /// <summary>
        /// Gets a value indicating whether this object is read obly. 
        /// </summary>
        /// <returns>true, if this object is read obly, false otherwise</returns>
        bool IsReadOnly { get; }
    }

    /// <summary>
    /// Represents coordinates on a 2D plane.
    /// Instances of this class are used to determine geometries, 
    /// but are not geometries themselves.
    /// </summary>
    [Serializable]
    public struct Coordinate : ICoordinate
    {
        private double _x;
        private double _y;

        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool Equals(ICoordinate p)
        {
            return PlanimetryAlgorithms.Distance(this, p) < PlanimetryAlgorithms.Tolerance;
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool ExactEquals(ICoordinate p)
        {
            return _x == p.X && _y == p.Y;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The System.Object to compare with the current MapAround.Geometry.Coordinate</param>
        public override bool Equals(object o)
        {
            if (o is ICoordinate)
                return PlanimetryAlgorithms.DistanceTolerant(this, (ICoordinate)o);

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
        /// Returns true if coordinates are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(Coordinate p1, ICoordinate p2)
        {
            return PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Returns true if coordinates are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(Coordinate p1, ICoordinate p2)
        {
            return !PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Gets an array containing coordinate values.
        /// </summary>
        /// <returns>An array containing coordinate values</returns>
        public double[] Values()
        {
            return new double[2] { _x, _y };
        }

        /// <summary>
        /// Creates a read only copy of this object.
        /// </summary>
        /// <returns>A read only copy of this object</returns>
        public ICoordinate ReadOnlyCopy()
        {
            return new ReadOnlyCoordinate(_x, _y);
        }

        /// <summary>
        /// Gets a value indicating whether this object is read obly. 
        /// This implementation always returns false.
        /// </summary>
        /// <returns>false</returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>  
        public object Clone()
        {
            return new Coordinate(_x, _y);
        }

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        public void Translate(double x, double y)
        {
            _x += x;
            _y += y;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>A bounding rectangle of this coordinate</returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            
            return 
                new BoundingRectangle(
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y), 
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Coordinate"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        internal Coordinate(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Coordinate"/>.
        /// </summary>
        /// <param name="coords">An array containing coordinate values</param>
        internal Coordinate(double[] coords)
        {
            if (coords.Length != 2)
                throw new NotSupportedException("Allowed objects with only two coordinates");

            _x = coords[0];
            _y = coords[1];
        }
    }

    /// <summary>
    /// Represents coordinates on a 2D plane with z value.
    /// Instances of this class are used to determine geometries, 
    /// but are not geometries themselves.
    /// </summary>
    [Serializable]
    public struct Coordinate3D : ICoordinate
    {
        private double _x;
        private double _y;
        private double _z;

        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Gets or sets a Z value. 
        /// </summary>
        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// Z values are not compared.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool Equals(ICoordinate p)
        {
            return PlanimetryAlgorithms.Distance(this, p) < PlanimetryAlgorithms.Tolerance;
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// Z values are not compared.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool ExactEquals(ICoordinate p)
        {
            return _x == p.X && _y == p.Y;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The System.Object to compare with the current MapAround.Geometry.Coordinate</param>
        public override bool Equals(object o)
        {
            if (o is ICoordinate)
                return PlanimetryAlgorithms.DistanceTolerant(this, (ICoordinate)o);

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
        /// Returns true if coordinates are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(Coordinate3D p1, ICoordinate p2)
        {
            return PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Returns true if coordinates are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(Coordinate3D p1, ICoordinate p2)
        {
            return !PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Gets an array containing coordinate values.
        /// </summary>
        /// <returns>An array containing coordinate values</returns>
        public double[] Values()
        {
            return new double[3] { _x, _y, _z };
        }

        /// <summary>
        /// Creates a read only copy of this object.
        /// </summary>
        /// <returns>A read only copy of this object</returns>
        public ICoordinate ReadOnlyCopy()
        {
            return new ReadOnlyCoordinate3D(_x, _y, _z);
        }

        /// <summary>
        /// Gets a value indicating whether this object is read obly. 
        /// This implementation always returns false.
        /// </summary>
        /// <returns>false</returns>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>  
        public object Clone()
        {
            return new Coordinate3D(_x, _y, _z);
        }

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        public void Translate(double x, double y)
        {
            _x += x;
            _y += y;
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>A bounding rectangle of this coordinate</returns>
        public BoundingRectangle GetBoundingRectangle()
        {

            return
                new BoundingRectangle(
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y),
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Coordinate3D"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        internal Coordinate3D(double x, double y)
        {
            _x = x;
            _y = y;
            _z = 0;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Coordinate3D"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <param name="z">A Z value</param>
        internal Coordinate3D(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.Coordinate3D"/>.
        /// </summary>
        /// <param name="coords">An array containing coordinate values</param>
        internal Coordinate3D(double[] coords)
        {
            if (coords.Length == 2)
            {
                _x = coords[0];
                _y = coords[1];
                _z = 0;
                return;
            }

            if (coords.Length == 3)
            {
                _x = coords[0];
                _y = coords[1];
                _z = coords[2];
                return;
            }

            throw new NotSupportedException("Allowed objects with two or three coordinates");

        }
    }

    /// <summary>
    /// Represents an immutable coordinates on a 2D plane.
    /// Instances of this class are used to determine geometries, 
    /// but are not geometries themselves.
    /// </summary>
    [Serializable]
    public struct ReadOnlyCoordinate : ICoordinate
    {
        private double _x;
        private double _y;

        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        public double X
        {
            get { return _x; }
            set 
            {
                throw new InvalidOperationException("Object is read only");
            }
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        public double Y
        {
            get { return _y; }
            set 
            {
                throw new InvalidOperationException("Object is read only");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool Equals(ICoordinate p)
        {
            return PlanimetryAlgorithms.Distance(this, p) < PlanimetryAlgorithms.Tolerance;
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool ExactEquals(ICoordinate p)
        {
            if (!(p is ICoordinate))
                return false;

            return _x == p.X && _y == p.Y;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The System.Object to compare with the current MapAround.Geometry.PointD</param>
        public override bool Equals(object o)
        {
            if (o is ICoordinate)
                return PlanimetryAlgorithms.DistanceTolerant(this, (ICoordinate)o);

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
        /// Returns true if coordinates are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(ReadOnlyCoordinate p1, ICoordinate p2)
        {
            return PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Returns true if coordinates are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(ReadOnlyCoordinate p1, ICoordinate p2)
        {
            return !PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Gets an array containing coordinate values.
        /// </summary>
        /// <returns>An array containing coordinate values</returns>
        public double[] Values()
        {
            return new double[2] { _x, _y };
        }

        /// <summary>
        /// Creates a read only copy of this object.
        /// </summary>
        /// <returns>A read only copy of this object</returns>
        public ICoordinate ReadOnlyCopy()
        {
            return new ReadOnlyCoordinate(_x, _y);
        }

        /// <summary>
        /// Gets a value indicating whether this object is read obly. 
        /// This implementation always returns true.
        /// </summary>
        /// <returns>true</returns>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Creates a new Coordinate instance that is a changeable copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>  
        public object Clone()
        {
            return new Coordinate(_x, _y);
        }

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// This implementation always throw an InvalidOperationException.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        /// <exception cref="InvalidOperationException">Throws always</exception>
        public void Translate(double x, double y)
        {
            throw new InvalidOperationException("Object is read only");
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>A bounding rectangle of this coordinate</returns>
        public BoundingRectangle GetBoundingRectangle()
        {

            return
                new BoundingRectangle(
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y),
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.ReadOnlyCoordinate"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        internal ReadOnlyCoordinate(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.ReadOnlyCoordinate"/>.
        /// </summary>
        /// <param name="coords">An array containing coordinate values</param>
        internal ReadOnlyCoordinate(double[] coords)
        {
            if (coords.Length != 2)
                throw new NotSupportedException("Allowed objects with only two coordinates");

            _x = coords[0];
            _y = coords[1];
        }
    }

    /// <summary>
    /// Represents an immutable coordinate on a 2D plane with Z value.
    /// Instances of this class are used to determine geometries, 
    /// but are not geometries themselves.
    /// </summary>
    [Serializable]
    public struct ReadOnlyCoordinate3D : ICoordinate
    {
        private double _x;
        private double _y;
        private double _z;

        /// <summary>
        /// Gets or sets an X coordinate. 
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                throw new InvalidOperationException("Object is read only");
            }
        }

        /// <summary>
        /// Gets or sets a Y coordinate. 
        /// </summary>
        public double Y
        {
            get { return _y; }
            set
            {
                throw new InvalidOperationException("Object is read only");
            }
        }

        /// <summary>
        /// Gets or sets a Z value. 
        /// </summary>
        public double Z
        {
            get { return _z; }
            set
            {
                throw new InvalidOperationException("Object is read only");
            }
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate is equal to another.
        /// Comparisions performs with tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>.
        /// Z values are not compared.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool Equals(ICoordinate p)
        {
            return PlanimetryAlgorithms.Distance(this, p) < PlanimetryAlgorithms.Tolerance;
        }

        /// <summary>
        /// Gets a value indicating whether this coordinate instance is equal to another.
        /// Comparisions performs exactly (used zero tolerance value).
        /// Z values are not compared.
        /// </summary>
        /// <param name="p">The MapAround.Geometry.ICoordinate implementor to compare with the current object</param>
        public bool ExactEquals(ICoordinate p)
        {
            return _x == p.X && _y == p.Y;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="o">The System.Object to compare with the current MapAround.Geometry.PointD</param>
        public override bool Equals(object o)
        {
            if (o is ICoordinate)
                return PlanimetryAlgorithms.DistanceTolerant(this, (ICoordinate)o);

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
        /// Returns true if coordinates are equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator ==(ReadOnlyCoordinate3D p1, ICoordinate p2)
        {
            return PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Returns true if coordinates are not equal (used tolerance value stored in
        /// <see cref="MapAround.Geometry.PlanimetryAlgorithms.Tolerance"/>).
        /// </summary>
        public static bool operator !=(ReadOnlyCoordinate3D p1, ICoordinate p2)
        {
            return !PlanimetryAlgorithms.DistanceTolerant(p1, p2);
        }

        /// <summary>
        /// Gets an array containing coordinate values.
        /// </summary>
        /// <returns>An array containing coordinate values</returns>
        public double[] Values()
        {
            return new double[3] { _x, _y, _z };
        }

        /// <summary>
        /// Creates a read only copy of this object.
        /// </summary>
        /// <returns>A read only copy of this object</returns>
        public ICoordinate ReadOnlyCopy()
        {
            return new ReadOnlyCoordinate(_x, _y);
        }

        /// <summary>
        /// Gets a value indicating whether this object is read obly. 
        /// This implementation always returns true.
        /// </summary>
        /// <returns>true</returns>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Creates a new Coordinate3D instance that is a changeable copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>  
        public object Clone()
        {
            return new Coordinate3D(_x, _y, _z);
        }

        /// <summary>
        /// Adds a values to X and Y coordinates.
        /// This implementation always throw an InvalidOperationException.
        /// </summary>
        /// <param name="x">The value that will be added to X coordinate</param>
        /// <param name="y">The value that will be added to Y coordinate</param>
        /// <exception cref="InvalidOperationException">Throws always</exception>
        public void Translate(double x, double y)
        {
            throw new InvalidOperationException("Object is read only");
        }

        /// <summary>
        /// Calculates a minimal axis-aligned bounding rectangle.
        /// </summary>
        /// <returns>A bounding rectangle of this coordinate</returns>
        public BoundingRectangle GetBoundingRectangle()
        {

            return
                new BoundingRectangle(
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y),
                    PlanimetryEnvironment.CoordinateFactory.Create(_x, _y));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.ReadOnlyCoordinate3D"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        internal ReadOnlyCoordinate3D(double x, double y)
        {
            _x = x;
            _y = y;
            _z = 0;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.ReadOnlyCoordinate3D"/>.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <param name="z">A Z value</param>
        internal ReadOnlyCoordinate3D(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapAround.Geometry.ReadOnlyCoordinate3D"/>.
        /// </summary>
        /// <param name="coords">An array containing coordinate values</param>
        internal ReadOnlyCoordinate3D(double[] coords)
        {
            if (coords.Length == 2)
            {
                _x = coords[0];
                _y = coords[1];
                _z = 0;
                return;
            }

            if (coords.Length == 3)
            {
                _x = coords[0];
                _y = coords[1];
                _z = coords[2];
                return;
            }

            throw new NotSupportedException("Allowed objects with two or three coordinates");
        }
    }

    /// <summary>
    /// Provides access to the properties and methods of object that 
    /// creates a new coordinate instances.
    /// </summary>
    public interface ICoordinateFactory
    {
        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        ICoordinate Create(double x, double y);

        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="values">An array containing coordinate values</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        ICoordinate Create(double[] values);
    }

    /// <summary>
    /// Represents default coordinate factory.
    /// An instance of <see cref="MapAround.Geometry.DefaultCoordinateFactory"/>
    /// creates instances of <see cref="MapAround.Geometry.Coordinate"/>.
    /// </summary>
    public class DefaultCoordinateFactory : ICoordinateFactory
    {
        #region ICoordinateFactory Members

        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public ICoordinate Create(double x, double y)
        {
            return new Coordinate(x, y);
        }

        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="values">An array containing coordinate values</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public ICoordinate Create(double[] values)
        {
            return new Coordinate(values);
        }

        #endregion
    }

    /// <summary>
    /// Represents default coordinate factory.
    /// An instance of <see cref="MapAround.Geometry.Coordinate3DFactory"/>
    /// creates instances of <see cref="MapAround.Geometry.Coordinate3D"/>.
    /// </summary>
    public class Coordinate3DFactory : ICoordinateFactory
    {
        private double _defaultZValue;

        /// <summary>
        /// Gets or sets a default Z value of creating coordinates.
        /// </summary>
        public double DefaultZValue
        {
            get { return _defaultZValue; }
            set { _defaultZValue = value; }
        }

        #region ICoordinateFactory Members

        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public ICoordinate Create(double x, double y)
        {
            return new Coordinate3D(x, y, _defaultZValue);
        }

        /// <summary>
        /// Create a new instance of the object representing coordinate.
        /// </summary>
        /// <param name="values">An array containing coordinate values</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public ICoordinate Create(double[] values)
        {
            if(values.Length == 2)
                return new Coordinate3D(values[0], values[1], _defaultZValue);
            else
                return new Coordinate3D(values);
        }

        #endregion
    }

    /// <summary>
    /// Represents an geometric environment.
    /// </summary>
    public static class PlanimetryEnvironment
    {
        [ThreadStatic]
        private static ICoordinateFactory _coordinateFactory = 
            new DefaultCoordinateFactory();
            //new Coordinate3DFactory();

        /// <summary>
        /// Gets or sets coordinate factory.
        /// Each thread have a separate instance of CoordinateFactory.
        /// </summary>
        public static ICoordinateFactory CoordinateFactory
        {
            get 
            {
                if (_coordinateFactory == null)
                    _coordinateFactory = new DefaultCoordinateFactory();
                return _coordinateFactory; 
            }
            set { _coordinateFactory = value; }
        }

        /// <summary>
        /// Create a new instance of coordinate using a CoordinateFactory.
        /// </summary>
        /// <param name="x">An X coordinate value</param>
        /// <param name="y">A Y coordinate value</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public static ICoordinate NewCoordinate(double x, double y)
        {
            return CoordinateFactory.Create(x, y);
        }

        /// <summary>
        /// Create a new instance of coordinate using a CoordinateFactory.
        /// </summary>
        /// <param name="values">An array containing coordinate values</param>
        /// <returns>A new instance of the object representing coordinate</returns>
        public static ICoordinate NewCoordinate(double[] values)
        {
            return CoordinateFactory.Create(values);
        }
    }
}