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
** File: EllipticGeometries.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Geometry objects on the Earth's surfase
**
=============================================================================*/

#if !DEMO

namespace MapAround.Geography
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using MapAround.Geometry;

    /// <summary>
    /// The MapAround.Geography namespace contains interfaces and classes
    /// that represent geometric objects on the Earth's surface.
    /// <para>
    /// Most of the implemented geometric algorithms is limited by total coverage 
    /// of the input data. It should not exceed the hemisphere. Points that are 
    /// spaced away from the center of mass of all starting points are processed 
    /// less precisely.
    /// </para>
    /// <para>
    /// For real objects on the Earth's surface (up to the continents), this 
    /// restriction is not essential and the lost of accuracy is acceptable.
    /// </para>
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides access to members of the geometry on the Earth's surface.
    /// </summary>
    public interface IGeography : ICloneable
    {
        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        int PointCount { get; }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        GeoPoint[] ExtractPoints();

        /// <summary>
        /// Computes a conver hull of this object.
        /// </summary>
        /// <returns>A list containing convex hull points</returns>
        IList<GeoPoint> GetConvexHull();

        /// <summary>
        /// Computes the envelope of this object.
        /// </summary>
        /// <returns>The envelope of this object</returns>
        Envelope GetEnvelope();

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        IGeometry ToPlanarGeometry(bool convertToDegrees);

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        void Canonicalize();
    }

    /// <summary>
    /// Represents the geometry on the Earth's surface.
    /// </summary>
    public abstract class Geography : IGeography
    {
        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public abstract int PointCount
        {
            get;
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public abstract GeoPoint[] ExtractPoints();

        /// <summary>
        /// Computes a conver hull of this object.
        /// </summary>
        /// <returns>A list containing convex hull points</returns>
        public IList<GeoPoint> GetConvexHull()
        {
            return EllipticAlgorithms.GetConvexHull(ExtractPoints());
        }

        /// <summary>
        /// Computes the envelope of this object.
        /// </summary>
        /// <returns>The envelope of this object</returns>
        public Envelope GetEnvelope()
        {
            return new Envelope(this);
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public abstract IGeometry ToPlanarGeometry(bool convertToDegrees);

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public abstract int Dimension { get; }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public abstract void Canonicalize();

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public abstract object Clone();
    }

    /// <summary>
    /// Represents a collection of geographies.
    /// </summary>
    [Serializable]
    public class GeographyCollection : Collection<IGeography>
    {
        /// <summary>
        /// Gets a value indicating whether a collection is homogenous.
        /// The collection is homogenous when the dimensions of all geographies 
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
        /// Gets a value indicating whether a collection contains geographies 
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
        /// Initializes a new instance of MapAround.Geometry.GeographyCollection.
        /// </summary>
        /// <param name="geography">Enumerator of geographies</param>
        public GeographyCollection(IEnumerable<IGeography> geography)
        {
            foreach (IGeography g in geography)
                this.Add(g);
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Geometry.GeographyCollection.
        /// </summary>
        public GeographyCollection()
        {
        }
    }

    /// <summary>
    /// Represents a point on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoPoint : Geography
    {
        private static readonly double _twoPi = 2.0 * Math.PI;
        private static readonly double _halfPi = 0.5 * Math.PI;

        /// <summary>
        /// Longitude in radians.
        /// </summary>
        public double L;

        /// <summary>
        /// Latitude in radians.
        /// </summary>
        public double Phi;

        /// <summary>
        /// Gets or sets a latitude in degrees.
        /// </summary>
        public double Latitude
        {
            get 
            {
                return MathUtils.Radians.ToDegrees(Phi);
            }
            set 
            {
                Phi = MathUtils.Degrees.ToRadians(value);
            }
        }

        /// <summary>
        /// Gets or sets a longitude in degrees.
        /// </summary>
        public double Longitude
        {
            get
            {
                return MathUtils.Radians.ToDegrees(L);
            }
            set
            {
                L = MathUtils.Degrees.ToRadians(value);
            }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            Phi = (Phi + Math.PI) % _twoPi;
            if (Phi < 0) 
                Phi += _twoPi;
            Phi -= Math.PI;

            if (Phi > _halfPi)
            {
                Phi = Math.PI - Phi;
                L += Math.PI;
            }
            else if (Phi < -_halfPi)
            {
                Phi = -Math.PI - Phi;
                L += Math.PI;
            }

            L = (L + Math.PI) % _twoPi;
            if (L <= 0) 
                L += _twoPi;
            L -= Math.PI;
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 0; }
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[1];
            result[0] = new GeoPoint(L, Phi);
            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            return new GeoPoint(this.L, this.Phi);
        }

        /// <summary>
        /// Computes an intersection of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the intersection</param>
        /// <returns>A collection of geographies that represents the intersection</returns>
        public ICollection<IGeography> Intersection(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the union</param>
        /// <returns>A collection of geographies that represents the union</returns>
        public ICollection<IGeography> Union(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the difference</param>
        /// <returns>A collection of geographies that represents the difference</returns>
        public ICollection<IGeography> Difference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the symmetric difference</param>
        /// <returns>A collection of geographies that represents the symmetric difference</returns>
        public ICollection<IGeography> SymmetricDifference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="angleDistance">The angle distance of the buffer in radians</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geography that represents a buffer</returns>
        public IGeography Buffer(double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            return GeoBufferBuilder.GetBuffer(this, angleDistance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarPoint(convertToDegrees);
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoPoint for
        /// the planar point that is Plate-Caree projection.
        /// </summary>
        /// <param name="point">A planar point</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geopoint</returns>
        public static GeoPoint FromPlanarPoint(ICoordinate point, bool convertFromDegrees)
        {
            GeoPoint result = new GeoPoint(point.X, point.Y);
            if (convertFromDegrees)
            {
                result.L = MathUtils.Degrees.ToRadians(result.L);
                result.Phi = MathUtils.Degrees.ToRadians(result.Phi);
            }
            return result;
        }

        /// <summary>
        /// Gets a planar point that is a result of simple 
        /// Plate-Caree projection of this geopoint.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A planar point</returns>
        public PointD ToPlanarPoint(bool convertToDegrees)
        {
            if(convertToDegrees)
                return new PointD(MathUtils.Radians.ToDegrees(L), MathUtils.Radians.ToDegrees(Phi));

            return new PointD(L, Phi);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoPoint"/>.
        /// </summary>
        /// <param name="l">A longitude value in radians</param>
        /// <param name="phi">A latitude value in radians</param>
        public GeoPoint(double l, double phi)
        {
            L = l;
            Phi = phi;
        }
    }

    /// <summary>
    /// An ordered collection of points on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoMultiPoint : Geography
    {
        private List<GeoPoint> _points = new List<GeoPoint>();

        /// <summary>
        /// Gets or sets a list of geopoints.
        /// </summary>
        public List<GeoPoint> Points
        {
            get { return _points; }
            set { _points = value; }
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                return _points.Count;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 0; }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            foreach (GeoPoint p in _points)
                p.Canonicalize();
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[Points.Count];

            int i = 0;
            foreach (GeoPoint p in Points)
            {
                result[i] = new GeoPoint(p.L, p.Phi);
                i++;
            }

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            GeoMultiPoint multiPoint = new GeoMultiPoint();

            foreach (GeoPoint p in Points)
                multiPoint.Points.Add((GeoPoint)p.Clone());

            return multiPoint;
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarMultiPoint(convertToDegrees);
        }

        /// <summary>
        /// Gets a planar multipoint that is a result of simple 
        /// Plate-Caree projection of this geomultipoint.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A planar multipoint</returns>
        public MultiPoint ToPlanarMultiPoint(bool convertToDegrees)
        {
            MultiPoint result = new MultiPoint();
            foreach (GeoPoint point in Points)
                result.Points.Add(point.ToPlanarPoint(convertToDegrees).Coordinate);

            return result;
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoMultiPoint for
        /// the planar multipoint that is Plate-Caree projection.
        /// </summary>
        /// <param name="multiPoint">A planar multipoint</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geomultipoint</returns>
        public static GeoMultiPoint FromPlanarMultiPoint(MultiPoint multiPoint, bool convertFromDegrees)
        {
            GeoMultiPoint result = new GeoMultiPoint();
            foreach (ICoordinate p in multiPoint.Points)
            {
                GeoPoint point = new GeoPoint(p.X, p.Y);
                if (convertFromDegrees)
                {
                    point.L = MathUtils.Degrees.ToRadians(point.L);
                    point.Phi = MathUtils.Degrees.ToRadians(point.Phi);
                }
                result.Points.Add(point);
            }

            return result;
        }

        /// <summary>
        /// Initializes a new instance of the  <see cref="MapAround.Geography.GeoMultiPoint"/>.
        /// </summary>
        /// <param name="points">Enumerator of points</param>
        public GeoMultiPoint(IEnumerable<GeoPoint> points)
        {
            foreach (GeoPoint point in points)
                Points.Add(point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoMultiPoint"/>.
        /// </summary>
        public GeoMultiPoint()
        {
        }
    }

    /// <summary>
    /// A sequence of connected segments on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoPath : Geography
    {
        private List<GeoPoint> _vertices = new List<GeoPoint>();

        /// <summary>
        /// Gets or sets a list containing 
        /// vertices of this geopath.
        /// </summary>
        public List<GeoPoint> Vertices
        {
            get { return _vertices; }
            set { _vertices = value; }
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                return _vertices.Count;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 1; }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            foreach (GeoPoint p in _vertices)
                p.Canonicalize();
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[Vertices.Count];

            int i = 0;
            foreach (GeoPoint p in Vertices)
            {
                result[i] = new GeoPoint(p.L, p.Phi);
                i++;
            }

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            GeoPath path = new GeoPath();

            foreach (GeoPoint p in Vertices)
                path.Vertices.Add((GeoPoint)p.Clone());

            return path;
        }

        /// <summary>
        /// Removes all segments which length is less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minAngle">The minimum angular length value (in radians)</param>
        public void ReduceSegments(double minAngle)
        {
            if (minAngle < 0)
                throw new ArgumentException("The minimum angle should not be negative", "minAngle");

            if (Vertices.Count == 0)
                return;

            GeoPath path = new GeoPath();
            GeoPoint initialPoint = this.Vertices[0];
            Vector3 v1 = UnitSphere.LatLonToGeocentric(initialPoint.Phi, initialPoint.L);
            Vector3 v2 = null;

            for (int i = 0; i < this.Vertices.Count - 1; i++)
            {
                v2 = UnitSphere.LatLonToGeocentric(this.Vertices[i + 1].Phi, this.Vertices[i + 1].L);

                if (v1.Angle(v2) > minAngle)
                {
                    path.Vertices.Add(initialPoint);
                    initialPoint = this.Vertices[i + 1];
                    v1 = UnitSphere.LatLonToGeocentric(initialPoint.Phi, initialPoint.L);
                }
            }

            v1 = UnitSphere.LatLonToGeocentric(path.Vertices[path.Vertices.Count - 1].Phi, path.Vertices[path.Vertices.Count - 1].L);
            v2 = UnitSphere.LatLonToGeocentric(this.Vertices[this.Vertices.Count - 1].Phi, this.Vertices[this.Vertices.Count - 1].L);
            if(v1.Angle(v2) > minAngle)
                path.Vertices.Add(this.Vertices[this.Vertices.Count - 1]);

            if (path.Vertices.Count == 1)
                path.Vertices.Clear();

            this.Vertices = path.Vertices;
        }

        /// <summary>
        /// Computes a length of the geopath. In the square of the ellipsoid axes unit.
        /// </summary>
        /// <returns>A length of the geopath</returns>
        public double Length()
        {
            if (Vertices.Count <= 1)
                return 0;

            GeodeticCalculator gc = new GeodeticCalculator();
            double result = 0;
            int i;
            for (i = 0; i < Vertices.Count - 1; i++)
            {
                GeodeticCurve curve = 
                gc.CalculateGeodeticCurve(EllipticAlgorithms.Ellipsoid,
                                          new GlobalCoordinates(Vertices[i].Latitude, Vertices[i].Longitude),
                                          new GlobalCoordinates(Vertices[i + 1].Latitude, Vertices[i + 1].Longitude));
                result += curve.EllipsoidalDistance;
            }

            return result;
        }

        /// <summary>
        /// Computes an angular length of the geopath in radians.
        /// </summary>
        /// <returns>An angular length of the geopath in radians</returns>
        public double AngleLength()
        {
            if (Vertices.Count <= 1)
                return 0;

            double result = 0;
            for (int i = 0; i < Vertices.Count - 1; i++)
                result += EllipticAlgorithms.AngularDistance(Vertices[i], Vertices[i + 1]);

            return result;
        }

        /// <summary>
        /// Densifies a geopath points.
        /// After this operation, two successive points of 
        /// geopath should not be spaced more than the 
        /// specified angle.
        /// </summary>
        /// <param name="maxAngle">A densification angle</param>
        public void Densify(double maxAngle)
        {
            List<GeoPoint> result = new List<GeoPoint>();

            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                result.Add(Vertices[i]);
                int j = i + 1;
                List<GeoPoint> addedPoints = UnitSphere.GetArcPoints(Vertices[i], Vertices[j], maxAngle);
                addedPoints.ForEach((GeoPoint p) => result.Add(p));
            }
            result.Add(Vertices[Vertices.Count - 1]);

            _vertices = result;
        }

        /// <summary>
        /// Reverces the order of points.
        /// </summary>
        public void Reverse()
        {
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count / 2; i++, j--)
            {
                GeoPoint temp = Vertices[i];
                Vertices[i] = Vertices[j];
                Vertices[j] = temp;
            }
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarLinePath(convertToDegrees);
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoPath for
        /// the linepath that is Plate-Caree projection.
        /// </summary>
        /// <param name="path">A linepath</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geocontour</returns>
        public static GeoPath FromPlanarPath(LinePath path, bool convertFromDegrees)
        {
            GeoPath result = new GeoPath();
            foreach (ICoordinate p in path.Vertices)
            {
                GeoPoint point = new GeoPoint(p.X, p.Y);
                if (convertFromDegrees)
                {
                    point.L = MathUtils.Degrees.ToRadians(point.L);
                    point.Phi = MathUtils.Degrees.ToRadians(point.Phi);
                }
                result.Vertices.Add(point);
            }

            return result;
        }

        /// <summary>
        /// Gets a linepath that is a result of simple 
        /// Plate-Caree projection of this geopath.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A linepath</returns>
        public LinePath ToPlanarLinePath(bool convertToDegrees)
        {
            LinePath result = new LinePath();
            foreach (GeoPoint point in Vertices)
                result.Vertices.Add(point.ToPlanarPoint(convertToDegrees).Coordinate);

            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoPath"/>.
        /// </summary>
        /// <param name="vertices">Enumerator ot the geopath vertices</param>
        public GeoPath(IEnumerable<GeoPoint> vertices)
        {
            foreach (GeoPoint point in vertices)
                Vertices.Add(point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoPath"/>.
        /// </summary>
        public GeoPath()
        {
        }
    }

    /// <summary>
    /// Represents closed sequence of connected segments
    /// on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoContour : Geography
    {
        private List<GeoPoint> _vertices = new List<GeoPoint>();

        /// <summary>
        /// Contour layout in geopolygon.
        /// </summary>
        public Contour.ContourLayout Layout = Contour.ContourLayout.Unknown;

        /// <summary>
        /// Gets or sets a list containing 
        /// vertices of this geocontour.
        /// </summary>
        public List<GeoPoint> Vertices
        {
            get { return _vertices; }
            set { _vertices = value; }
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                return _vertices.Count;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 1; }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            foreach (GeoPoint p in _vertices)
                p.Canonicalize();
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[Vertices.Count];

            int i = 0;
            foreach (GeoPoint p in Vertices)
            {
                result[i] = new GeoPoint(p.L, p.Phi);
                i++;
            }

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            GeoContour contour = new GeoContour();

            foreach (GeoPoint p in Vertices)
                contour.Vertices.Add((GeoPoint)p.Clone());

            return contour;
        }

        /// <summary>
        /// Removes all segments which length is less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minAngle">The minimum angular length value (in radians)</param>
        public void ReduceSegments(double minAngle)
        {
            if (minAngle < 0)
                throw new ArgumentException("The minimum angle should not be negative", "minAngle");

            GeoContour contour = new GeoContour();

            if (this.Vertices.Count > 1)
            {
                GeoPoint initialPoint = this.Vertices[0];

                Vector3 v1 = UnitSphere.LatLonToGeocentric(initialPoint.Phi, initialPoint.L);
                Vector3 v2 = null;

                for (int i = 0; i < this.Vertices.Count - 1; i++)
                {
                    v2 = UnitSphere.LatLonToGeocentric(this.Vertices[i + 1].Phi, this.Vertices[i + 1].L);

                    if (v1.Angle(v2) > minAngle)
                    {
                        contour.Vertices.Add(initialPoint);
                        initialPoint = this.Vertices[i + 1];
                        v1 = UnitSphere.LatLonToGeocentric(initialPoint.Phi, initialPoint.L);
                    }
                }

                contour.Vertices.Add(this.Vertices[this.Vertices.Count - 1]);

                // need to check the length of the last (closing) of the segment circuit, maybe he should be removed
                while (contour.Vertices.Count > 1)
                {
                    v1 = UnitSphere.LatLonToGeocentric(contour.Vertices[0].Phi, contour.Vertices[0].L);
                    v2 = UnitSphere.LatLonToGeocentric(contour.Vertices[contour.Vertices.Count - 1].Phi, contour.Vertices[contour.Vertices.Count - 1].L);
                    if (v1.Angle(v2) > minAngle)
                        break;

                    contour.Vertices.RemoveAt(contour.Vertices.Count - 1);
                }
            }

            //circuit does not contain segments
            if (contour.Vertices.Count == 1)
                contour.Vertices.Clear();

            this.Vertices = contour.Vertices;
        }

        /// <summary>
        /// Tests if the point lies on the area bounded by 
        /// this geocontour.
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <returns>True, if the point lies on the area bounded by 
        /// this geocontour, false otherwise</returns>
        public bool ContainsPoint(GeoPoint point)
        {
            GeoPolygon polygon = new GeoPolygon();
            polygon.Contours.Add(this);

            return polygon.ContainsPoint(point);
        }

        /// <summary>
        /// Computes a length of the geocontour. In the square of the ellipsoid axes unit.
        /// </summary>
        /// <returns>A length of the geocontour</returns>
        public double Length()
        {
            if (Vertices.Count <= 1)
                return 0;

            GeodeticCalculator gc = new GeodeticCalculator();
            double result = 0;
            int i;
            for (i = 0; i < Vertices.Count; i++)
            {
                int j = i == Vertices.Count ? 0 : i + 1;
                GeodeticCurve curve =
                gc.CalculateGeodeticCurve(EllipticAlgorithms.Ellipsoid,
                                          new GlobalCoordinates(Vertices[i].Latitude, Vertices[i].Longitude),
                                          new GlobalCoordinates(Vertices[j].Latitude, Vertices[j].Longitude));
                result += curve.EllipsoidalDistance;
            }

            return result;
        }

        /// <summary>
        /// Computes an angular length of the geocontour in radians.
        /// </summary>
        /// <returns>An angular length of the geocontour in radians</returns>
        public double AngleLength()
        {
            if (Vertices.Count <= 1)
                return 0;

            double result = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                int j = i == Vertices.Count ? 0 : i + 1;
                result += EllipticAlgorithms.AngularDistance(Vertices[i], Vertices[j]);
            }

            return result;
        }

        /// <summary>
        /// Densifies a geocontour points.
        /// After this operation, two successive points of 
        /// geocontour should not be spaced more than the 
        /// specified angle.
        /// </summary>
        /// <param name="maxAngle">A densification angle</param>
        public void Densify(double maxAngle)
        {
            List<GeoPoint> result = new List<GeoPoint>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                result.Add(Vertices[i]);
                int j = i == Vertices.Count - 1 ? 0 : i + 1;
                List<GeoPoint> addedPoints = UnitSphere.GetArcPoints(Vertices[i], Vertices[j], maxAngle);
                addedPoints.ForEach((GeoPoint p) => result.Add(p));
            }
            _vertices = result;
        }

        /// <summary>
        /// Reverces the order of points.
        /// </summary>
        public void Reverse()
        {
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count / 2; i++, j--)
            {
                GeoPoint temp = Vertices[i];
                Vertices[i] = Vertices[j];
                Vertices[j] = temp;
            }
        }

        /// <summary>
        /// Calculates the area of the 
        /// Lambert Azimuthal Aqual Area Projection 
        /// of this contour.
        /// </summary>
        /// <returns>An area of the projected contour</returns>
        internal double PlaneAreaInLambertProjection(bool northPole)
        {
            List<Vector3> vectors = new List<Vector3>();

            double a = EllipticAlgorithms.Ellipsoid.SemiMajorAxis;
            double b = EllipticAlgorithms.Ellipsoid.SemiMinorAxis;

            // In order to move from the ellipsoid to the sphere converts latitudes:
            // calculate latitude on the sphere with the same (with a reference ellipsoid) surface area.
            // After this area is projected on a plane. 
            // Used projection leaves unchanged the area of ​​figures, only 
            // if it was carried out continuously (geodesic segment moved generally in curves).
            // When replacing a geodesic segments line segments inevitable deviations 
            // from the analytical results that will be smaller, 
            // the smaller (in length) of the geodesic segment contained the original path.

            // eccentricity of the ellipsoid
            double e = Math.Sqrt(1 - b * b / (a * a));

            // equivalent latitude
            double qp = 1 + 0.5 * (1 - e * e) / e * Math.Log((1 + e) / (1 - e));

            GeoContour c = (GeoContour)this.Clone();

            foreach (GeoPoint p in c.Vertices)
            {
                double sinPhi = Math.Sin(p.Phi);
                double q = (1 - e * e) * 
                    (sinPhi / (1 - e * e * sinPhi * sinPhi) - 
                     0.5 / e * Math.Log((1 - e * sinPhi) / (1 + e * sinPhi)));

                p.Phi = Math.Asin(q / qp);

                vectors.Add(UnitSphere.LatLonToGeocentric(p.Phi, p.L));
            }

            double rq = a * Math.Sqrt(0.5 * qp);

            Contour planeContour = new Contour();
            int sign = northPole ? -1 : 1;

            foreach (Vector3 v in vectors)
            {
                double f = Math.Sqrt(2 / (1 + sign * v.Z));
                planeContour.Vertices.Add(PlanimetryEnvironment.NewCoordinate(f * v.X * rq, f * v.Y * rq));
            }
            return planeContour.SimpleArea();
        }

        /// <summary>
        /// Computes an area of the geocontour.
        /// Accuracy of calculations depends on the distance between successive 
        /// points of contour. To get "good" results for geocontours containing 
        /// long segments, it is recommended to densify its points 
        /// - <see cref="MapAround.Geography.GeoContour.Densify"/>).
        /// </summary>
        /// <returns>An area of the geopolygon (in the square of the ellipsoid axes unit)</returns>
        public double Area()
        {
            GeoPoint centroid = EllipticAlgorithms.GetPointsCentroid(this.ExtractPoints());
            centroid.Canonicalize();

            double planeArea = this.PlaneAreaInLambertProjection(centroid.Latitude > 0);
            double aa = EllipticAlgorithms.Ellipsoid.SemiMajorAxis * EllipticAlgorithms.Ellipsoid.SemiMajorAxis;
            return EllipticAlgorithms.Ellipsoid.SurfaceArea() / (4 * Math.PI * aa) * planeArea;
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarContour(convertToDegrees);
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoContour for
        /// the planar contour that is Plate-Caree projection.
        /// </summary>
        /// <param name="contour">A planar contour</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geocontour</returns>
        public static GeoContour FromPlanarContour(Contour contour, bool convertFromDegrees)
        {
            GeoContour result = new GeoContour();
            foreach (ICoordinate p in contour.Vertices)
            {
                GeoPoint point = new GeoPoint(p.X, p.Y);
                if (convertFromDegrees)
                {
                    point.L = MathUtils.Degrees.ToRadians(point.L);
                    point.Phi = MathUtils.Degrees.ToRadians(point.Phi);
                }
                result.Vertices.Add(point);
            }

            return result;
        }

        /// <summary>
        /// Gets a planar contour that is a result of simple 
        /// Plate-Caree projection of this geocontour.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A planar contour</returns>
        public Contour ToPlanarContour(bool convertToDegrees)
        {
            Contour result = new Contour();
            foreach (GeoPoint point in Vertices)
                result.Vertices.Add(point.ToPlanarPoint(convertToDegrees).Coordinate);

            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoContour"/>.
        /// </summary>
        /// <param name="vertices">Enumerator of the geocontour vertices</param>
        public GeoContour(IEnumerable<GeoPoint> vertices)
        {
            foreach (GeoPoint point in vertices)
                Vertices.Add(point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAround.Geography.GeoContour"/>.
        /// </summary>
        public GeoContour()
        {
        }
    }

    /// <summary>
    /// A polyline on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoPolyline : Geography
    {
        private List<GeoPath> _paths = new List<GeoPath>();

        /// <summary>
        /// Gets or sets a list containing geopaths defining this geopolyline.
        /// </summary>
        public List<GeoPath> Paths
        {
            get { return _paths; }
            set { _paths = value; }
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                int result = 0;
                foreach (GeoPath path in Paths)
                    result += path.PointCount;

                return result;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 1; }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            foreach (GeoPath p in _paths)
                p.Canonicalize();
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[PointCount];

            int i = 0;
            foreach(GeoPath path in _paths)
                foreach (GeoPoint p in path.Vertices)
                {
                    result[i] = new GeoPoint(p.L, p.Phi);
                    i++;
                }

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            GeoPolyline polyline = new GeoPolyline();
            foreach (GeoPath path in Paths)
                polyline.Paths.Add((GeoPath)path.Clone());

            return polyline;
        }

        /// <summary>
        /// Computes a length of the geopolyline. In square of the ellipsoid axes unit.
        /// </summary>
        /// <returns>A length of the geopolyline</returns>
        public double Length()
        {
            double result = 0;
            foreach (GeoPath path in this.Paths)
                result += path.Length();

            return result;
        }

        /// <summary>
        /// Computes an angular length of the geopolyline in radians.
        /// </summary>
        /// <returns>An angular length of the geopolyline in radians</returns>
        public double AngleLength()
        {
            double result = 0;
            foreach (GeoPath path in this.Paths)
                result += path.AngleLength();

            return result;
        }

        /// <summary>
        /// Computes an intersection of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the intersection</param>
        /// <returns>A collection of geographies that represents the intersection</returns>
        public ICollection<IGeography> Intersection(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the union</param>
        /// <returns>A collection of geographies that represents the union</returns>
        public ICollection<IGeography> Union(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the difference</param>
        /// <returns>A collection of geographies that represents the difference</returns>
        public ICollection<IGeography> Difference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the symmetric difference</param>
        /// <returns>A collection of geographies that represents the symmetric difference</returns>
        public ICollection<IGeography> SymmetricDifference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="angleDistance">The angle distance of the buffer in radians</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geography that represents a buffer</returns>
        public IGeography Buffer(double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            return GeoBufferBuilder.GetBuffer(this, angleDistance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Removes all segments which length is less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minAngle">The minimum angular length value (in radians)</param>
        public void ReduceSegments(double minAngle)
        {
            foreach (GeoPath p in Paths)
                p.ReduceSegments(minAngle);
        }

        /// <summary>
        /// Densifies a geopolyline points.
        /// After this operation, two successive points of each 
        /// geopath should not be spaced more than the 
        /// specified angle.
        /// </summary>
        /// <param name="maxAngle">A densification angle</param>
        public void Densify(double maxAngle)
        {
            foreach (GeoPath path in Paths)
                path.Densify(maxAngle);
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarPolyline(convertToDegrees);
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoPolyline for
        /// the planar polyline that is Plate-Caree projection.
        /// </summary>
        /// <param name="polyline">A planar polyline</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geopolyline</returns>
        public static GeoPolyline FromPlanarPolyline(Polyline polyline, bool convertFromDegrees)
        {
            GeoPolyline result = new GeoPolyline();
            foreach (LinePath c in polyline.Paths)
                result.Paths.Add(GeoPath.FromPlanarPath(c, convertFromDegrees));

            return result;
        }

        /// <summary>
        /// Gets a planar polyline that is a result of simple 
        /// Plate-Caree projection of this geopolyline.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A planar polyline</returns>
        public Polyline ToPlanarPolyline(bool convertToDegrees)
        {
            Polyline polyline = new Polyline();
            foreach (GeoPath c in Paths)
                polyline.Paths.Add(c.ToPlanarLinePath(convertToDegrees));

            return polyline;
        }
    }

    /// <summary>
    /// Represents a polygon on the Earth's surface.
    /// </summary>
    [Serializable]
    public class GeoPolygon : Geography
    {
        private List<GeoContour> _contours = new List<GeoContour>();

        /// <summary>
        /// Gets or sets a list of geocontours 
        /// defining the border of this polygon.
        /// </summary>
        public List<GeoContour> Contours
        {
            get { return _contours; }
            set { _contours = value; }
        }

        /// <summary>
        /// Gets a number of points defining this object.
        /// </summary>
        public override int PointCount
        {
            get
            {
                int result = 0;
                foreach (GeoContour c in Contours)
                    result += c.PointCount;

                return result;
            }
        }

        /// <summary>
        /// Gets the dimension of this object.
        /// </summary>
        public override int Dimension
        {
            get { return 2; }
        }

        /// <summary>
        /// Canonicalizes all coordinates of this object.
        /// </summary>
        public override void Canonicalize()
        {
            foreach (GeoContour c in _contours)
                c.Canonicalize();
        }

        /// <summary>
        /// Extracts all points defining this object.
        /// </summary>
        /// <returns>An array containing the points defining this object</returns>
        public override GeoPoint[] ExtractPoints()
        {
            GeoPoint[] result = new GeoPoint[PointCount];

            int i = 0;
            foreach (GeoContour contour in _contours)
                foreach (GeoPoint p in contour.Vertices)
                {
                    result[i] = new GeoPoint(p.L, p.Phi);
                    i++;
                }

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public override object Clone()
        {
            GeoPolygon polygon = new GeoPolygon();
            foreach (GeoContour contour in Contours)
                polygon.Contours.Add((GeoContour)contour.Clone());

            return polygon;
        }

        /// <summary>
        /// Computes an intersection of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the intersection</param>
        /// <returns>A collection of geographies that represents the intersection</returns>
        public ICollection<IGeography> Intersection(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Intersection(this, other);
        }

        /// <summary>
        /// Computes a union of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the union</param>
        /// <returns>A collection of geographies that represents the union</returns>
        public ICollection<IGeography> Union(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Union(this, other);
        }

        /// <summary>
        /// Computes a difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the difference</param>
        /// <returns>A collection of geographies that represents the difference</returns>
        public ICollection<IGeography> Difference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.Difference(this, other);
        }

        /// <summary>
        /// Computes a symmetric difference of this geography with another.
        /// </summary>
        /// <param name="other">The instance of MapAround.Geometry.IGeography to compute the symmetric difference</param>
        /// <returns>A collection of geographies that represents the symmetric difference</returns>
        public ICollection<IGeography> SymmetricDifference(IGeography other)
        {
            EllipticOverlayCalculator overlay = new EllipticOverlayCalculator();
            return overlay.SymmetricDifference(this, other);
        }

        /// <summary>
        /// Tests if the point lies on the area bounded by 
        /// the polygon boundary.
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <returns>True, if the point lies on the area bounded by 
        /// the polygon boundary, false otherwise</returns>
        public bool ContainsPoint(GeoPoint point)
        {
            GeographyCollection egc = new GeographyCollection();
            egc.Add(this);
            egc.Add(point);

            GnomonicProjection projection = GeometrySpreader.GetProjection(egc);
            GeometryCollection gc = GeometrySpreader.GetGeometries(egc, projection);

            return ((Polygon)gc[0]).ContainsPoint(((PointD)gc[1]).Coordinate);
        }

        /// <summary>
        /// Gets a planar geometry that is a result of simple 
        /// Plate-Caree projection of this object.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A plane geometry</returns>
        public override IGeometry ToPlanarGeometry(bool convertToDegrees)
        {
            return ToPlanarPolygon(convertToDegrees);
        }

        /// <summary>
        /// Gets a planar polygon that is a result of simple 
        /// Plate-Caree projection of this geopolygon.
        /// </summary>
        /// <param name="convertToDegrees">A value indicating whether an anglular coordinates should be converted to degrees</param>
        /// <returns>A planar polygon</returns>
        public Polygon ToPlanarPolygon(bool convertToDegrees)
        {
            Polygon polygon = new Polygon();
            foreach (GeoContour c in Contours)
                polygon.Contours.Add(c.ToPlanarContour(convertToDegrees));

            return polygon;
        }

        /// <summary>
        /// Computes the contours of the topologically normalized geopolygon.
        /// </summary>
        /// <remarks>
        /// Contours that are not intersected. 
        /// Orientations are set clockwise for holes and 
        /// counter clockwise for shells.
        /// Sections of the boundary repeated an even number of times will be removed.
        /// </remarks>
        /// <returns>A list containing contours of the topologically normalized geopolygon</returns>
        public List<GeoContour> GetSimplifiedContours()
        {
            GeographyCollection colllection = new GeographyCollection();
            colllection.Add(this);
            GnomonicProjection projection = GeometrySpreader.GetProjection(colllection);
            GeometryCollection planeGeometries = GeometrySpreader.GetGeometries(colllection, projection);
            ((Polygon)planeGeometries[0]).Simplify();
            colllection = GeometrySpreader.GetGeographies(planeGeometries, projection);
            return ((GeoPolygon)colllection[0]).Contours;
        }

        /// <summary>
        /// Normalizes the topology of the geopolygon.
        /// Contours are transformed to not intersected. 
        /// Orientations are set clockwise for holes and 
        /// counter clockwise for shells.
        /// </summary>
        public void Simplify()
        {
            this.Contours = GetSimplifiedContours();
        }

        /// <summary>
        /// Builds a buffer for this geometry.
        /// </summary>
        /// <param name="angleDistance">The angle distance of the buffer in radians</param>
        /// <param name="pointsPerCircle">The number of points in a polygon approximating a circle of a point object buffer</param>
        /// <param name="allowParallels">The value indicating whether the parallel computing will be used when possible</param>
        /// <returns>A geography that represents a buffer</returns>
        public IGeography Buffer(double angleDistance, int pointsPerCircle, bool allowParallels)
        {
            return GeoBufferBuilder.GetBuffer(this, angleDistance, pointsPerCircle, allowParallels);
        }

        /// <summary>
        /// Removes all segments which length is less than the 
        /// specified minimum length. Method does not guarantee 
        /// preservation of topology.
        /// </summary>
        /// <param name="minAngle">The minimum angular length value (in radians)</param>
        public void ReduceSegments(double minAngle)
        {
            foreach (GeoContour c in Contours)
                c.ReduceSegments(minAngle);
        }

        /// <summary>
        /// Computes a length of the geopolygon boundary. In square of the ellipsoid axes unit.
        /// </summary>
        /// <returns>A length of the geopolygon boundary</returns>
        public double Perimeter()
        {
            double result = 0;
            foreach (GeoContour c in Contours)
                result += c.Length();

            return result;
        }

        /// <summary>
        /// Computes an angular length of the geopolygon boundary in radians.
        /// </summary>
        /// <returns>An angular length of the geopolygon boundary in radians</returns>
        public double AnglePerimeter()
        {
            double result = 0;
            foreach (GeoContour contour in this.Contours)
                result += contour.AngleLength();

            return result;
        }

        /// <summary>
        /// Densifies a geopolygon points.
        /// After this operation, two successive points of each 
        /// geopolygon contour should not be spaced more than 
        /// the specified angle.
        /// </summary>
        /// <param name="maxAngle">A densification angle</param>
        public void Densify(double maxAngle)
        {
            foreach (GeoContour c in Contours)
                c.Densify(maxAngle);
        }

        /// <summary>
        /// Computes an area of the geopolygon.
        /// Accuracy of calculations depends on the distance between successive 
        /// points of contour. To get "good" results for geopolygons containing 
        /// long segments, it is recommended to densify its points 
        /// - <see cref="MapAround.Geography.GeoPolygon.Densify"/>).
        /// </summary>
        /// <returns>An area of the geopolygon (in the square of the ellipsoid axes unit)</returns>
        public double Area()
        {
            GeoPolygon p = (GeoPolygon)this.Clone();
            p.Simplify();

            GeoPoint centroid = EllipticAlgorithms.GetPointsCentroid(this.ExtractPoints());
            centroid.Canonicalize();
            bool northPole = centroid.Latitude > 0;

            double planeArea = 0;
            foreach (GeoContour c in p.Contours)
            {
                if (c.Layout == Contour.ContourLayout.External)
                    planeArea += c.PlaneAreaInLambertProjection(northPole);
                else
                    planeArea -= c.PlaneAreaInLambertProjection(northPole);
            }
            double aa = EllipticAlgorithms.Ellipsoid.SemiMajorAxis * EllipticAlgorithms.Ellipsoid.SemiMajorAxis;
            return EllipticAlgorithms.Ellipsoid.SurfaceArea() / (4 * Math.PI * aa) * planeArea;
        }

        /// <summary>
        /// Creates an instance of the MapAround.Geography.GeoPolygon for
        /// the planar polygon that is Plate-Caree projection.
        /// </summary>
        /// <param name="polygon">A planar polygon</param>
        /// <param name="convertFromDegrees">A value indicating whether 
        /// the angular coordinates should be converted from degrees to radians</param>
        /// <returns>A geopolygon</returns>
        public static GeoPolygon FromPlanarPolygon(Polygon polygon, bool convertFromDegrees)
        {
            GeoPolygon result = new GeoPolygon();
            foreach (Contour c in polygon.Contours)
            {
                GeoContour contour = GeoContour.FromPlanarContour(c, convertFromDegrees);
                contour.Layout = c.Layout;
                result.Contours.Add(contour);
            }

            return result;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GeoPolygon.
        /// </summary>
        public GeoPolygon()
        { 
        }
    }

    /// <summary>
    /// Represents an envelope of the geographic objects.
    /// <remarks>
    /// Envelope is defined as a center point and an angle, which 
    /// determine region on the ellipsoid surface which contains 
    /// geometric shape. Envelope is not a minimum area envelope, 
    /// which can be defined by the point and angle. Because the 
    /// center is computed by summing the vectors from the center 
    /// of the globe to each vertex in the figure, essentially 
    /// averaging the vertices. The calculation of minimal cap is 
    /// difficult computationally and takes a long time. 
    /// This implementation is a compromise on performance and good 
    /// approximation of the true minimum envelope.
    /// </remarks>
    /// </summary>
    public class Envelope
    {
        private GeoPoint _center = null;
        private double _angle = 0;

        /// <summary>
        /// Gets a center point of enveope.
        /// </summary>
        public GeoPoint Center
        {
            get { return _center; }
        }

        /// <summary>
        /// Gets an angle of enveope in radians.
        /// </summary>
        public double Angle
        {
            get { return _angle; }
        }

        /// <summary>
        /// Builds an envelope (calculates a center point and an angle)
        /// for the specifeid geopoints.
        /// </summary>
        /// <param name="points">Enumerator of geopoints to build envelope</param>
        public void Build(IEnumerable<GeoPoint> points)
        {
            double lat, lon;
            GnomonicProjection.GetCenter(points, out lat, out lon);
            _center = new GeoPoint(lon, lat);

            Vector3 centerVector = UnitSphere.LatLonToGeocentric(_center.Phi, _center.L);
            double maxAngle = 0;
            foreach (GeoPoint p in points)
            {
                Vector3 v = UnitSphere.LatLonToGeocentric(p.Phi, p.L);
                double currentAngle = centerVector.Angle(v);
                if (currentAngle > maxAngle)
                    maxAngle = currentAngle;
            }

            _angle = maxAngle;
        }

        /// <summary>
        /// Joins this envelope with envelope of specified point.
        /// </summary>
        /// <param name="point">A geopoint to join envelope</param>
        public void Join(GeoPoint point)
        {
            if (Center == null)
            {
                _center = (GeoPoint)point.Clone();
                _angle = 0;
            }
            else
            {
                Vector3 centerVector = UnitSphere.LatLonToGeocentric(_center.Phi, _center.L);

                double angleToPoint = 
                    centerVector.Angle(UnitSphere.LatLonToGeocentric(point.Phi, point.L));
                if (angleToPoint > _angle)
                    _angle = angleToPoint;
            }
        }

        /// <summary>
        /// Tests whether this envelope intersects with the other.
        /// </summary>
        /// <param name="other">An envelope instance to test</param>
        /// <returns>true, if the regions overlap, otherwise false</returns>
        public bool Intersect(Envelope other)
        {
            if (this._center == null || other._center == null)
                return false;

            Vector3 v1 = UnitSphere.LatLonToGeocentric(_center.Phi, _center.L);
            Vector3 v2 = UnitSphere.LatLonToGeocentric(other._center.Phi, other._center.L);
            return v1.Angle(v2) > this.Angle + other.Angle;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.Envelope.
        /// </summary>
        public Envelope()
        { 
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.Envelope.
        /// </summary>
        /// <param name="geography">A geography to build envelope</param>
        public Envelope(IGeography geography)
        {
            Build(geography.ExtractPoints());
        }
    }
}

#endif