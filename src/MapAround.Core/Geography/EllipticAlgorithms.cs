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
** File: EllipticAlgorithms.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Geometry algorithms on the spheroid's surface
**
=============================================================================*/

namespace MapAround.Geography
{
    using System;
    using System.Collections.Generic;

    using MapAround.Geometry;
    using MapAround.CoordinateSystems;



#if !DEMO

    /// <summary>
    /// An object that performs
    /// the conversion between objects
    /// and elliptical plane geometry by gnomonic projection.
    /// </summary>
    internal class GeometrySpreader
    {
        private static PointD projectPoint(GeoPoint point, GnomonicProjection projection)
        {
            PointD p = point.ToPlanarPoint(false);
            double x, y;
            projection.Project(p.Y, p.X, out x, out y);
            p.X = x;
            p.Y = y;
            return p;
        }

        private static Polyline projectPolyline(GeoPolyline polyline, GnomonicProjection projection)
        {
            Polyline p = polyline.ToPlanarPolyline(false);
            foreach (LinePath path in p.Paths)
            {
                for (int i = 0; i < path.Vertices.Count; i++)
                {
                    double x, y;
                    projection.Project(path.Vertices[i].Y, path.Vertices[i].X, out x, out y);
                    path.Vertices[i] = PlanimetryEnvironment.NewCoordinate(x, y);
                }
            }

            return p;
        }

        private static MultiPoint projectMultiPoint(GeoMultiPoint multiPoint, GnomonicProjection projection)
        {
            MultiPoint mp = multiPoint.ToPlanarMultiPoint(false);
            for (int i = 0; i < mp.Points.Count; i++)
            {
                double x, y;
                projection.Project(mp.Points[i].Y, mp.Points[i].X, out x, out y);
                mp.Points[i] = PlanimetryEnvironment.NewCoordinate(x, y);
            }

            return mp;
        }

        private static Polygon projectPolygon(GeoPolygon polygon, GnomonicProjection projection)
        {
            Polygon p = polygon.ToPlanarPolygon(false);
            foreach (Contour contour in p.Contours)
            {
                for (int i = 0; i < contour.Vertices.Count; i++)
                {
                    double x, y;
                    projection.Project(contour.Vertices[i].Y, contour.Vertices[i].X, out x, out y);
                    contour.Vertices[i] = PlanimetryEnvironment.NewCoordinate(x, y);
                }
            }

            return p;
        }

        private static GeoPoint unprojectPoint(PointD point, GnomonicProjection projection)
        {
            double lat, lon;
            projection.Unproject(point.X, point.Y, out lat, out lon);
            return new GeoPoint(lon, lat);
        }

        private static GeoMultiPoint unprojectMultiPoint(MultiPoint multiPoint, GnomonicProjection projection)
        {
            GeoMultiPoint geoMultiPoint = new GeoMultiPoint();
                foreach (ICoordinate p in multiPoint.Points)
                    geoMultiPoint.Points.Add(unprojectPoint(new PointD(p), projection));

            return geoMultiPoint;
        }

        private static GeoPolyline unprojectPolyline(Polyline polyline, GnomonicProjection projection)
        {
            GeoPolyline geoPolyline = new GeoPolyline();
            foreach (LinePath path in polyline.Paths)
            {
                GeoPath geoPath = new GeoPath();
                foreach (ICoordinate p in path.Vertices)
                    geoPath.Vertices.Add(unprojectPoint(new PointD(p), projection));

                geoPolyline.Paths.Add(geoPath);
            }

            return geoPolyline;
        }

        private static GeoPolygon unprojectPolygon(Polygon polygon, GnomonicProjection projection)
        {
            GeoPolygon geoPolygon = new GeoPolygon();
            foreach (Contour contour in polygon.Contours)
            {
                GeoContour geoContour = new GeoContour();
                geoContour.Layout = contour.Layout;
                foreach (ICoordinate p in contour.Vertices)
                    geoContour.Vertices.Add(unprojectPoint(new PointD(p), projection));

                geoPolygon.Contours.Add(geoContour);
            }

            return geoPolygon;
        }

        /// <summary>
        /// Converts a collection of geometric shapes on the surface of the ellipsoid 
        /// to the collection of geometric figures in the plane according to the given gnomonic projection.
        /// </summary>
        /// <param name="collection">Collection of geometric shapes on the surface of the ellipsoid</param>
        /// <param name="projection">Projection</param>
        /// <returns>Collection of geometric figures in the plane</returns>
        public static GeometryCollection GetGeometries(GeographyCollection collection, GnomonicProjection projection)
        { 
            GeometryCollection result = new GeometryCollection();
            IGeometry geometry = null;
            foreach (IGeography geography in collection)
            {
                if (geography is GeoPoint)
                {
                    geometry = projectPoint((GeoPoint)geography, projection);
                }
                else if (geography is GeoPolyline)
                {
                    geometry = projectPolyline((GeoPolyline)geography, projection);
                }
                else if (geography is GeoPolygon)
                {
                    geometry = projectPolygon((GeoPolygon)geography, projection);
                }
                else if (geography is GeoMultiPoint)
                {
                    geometry = projectMultiPoint((GeoMultiPoint)geography, projection);
                }
                else
                    throw new NotImplementedException("Geometry \"" + geography.GetType().FullName + "\" is not supported.");

                result.Add(geometry);
            }

            return result;
        }

        /// <summary>
        /// Converts a collection of geometric shapes on the surface of the ellipsoid 
        /// to the collection of geometric figures in the plane in line with the given projection.
        /// </summary>
        /// <param name="geometries">Enumerator geometric shapes on the surface of the ellipsoid</param>
        /// <param name="projection">Projection</param>
        /// <returns>Collection of geometric figures in the plane</returns>
        public static GeographyCollection GetGeographies(IEnumerable<IGeometry> geometries, GnomonicProjection projection)
        {
            GeographyCollection result = new GeographyCollection();
            IGeography geometry = null;
            foreach (IGeometry g in geometries)
            {
                if (g is PointD)
                {
                    geometry = unprojectPoint((PointD)g, projection);
                }
                else if (g is Polyline)
                {
                    geometry = unprojectPolyline((Polyline)g, projection);
                }
                else if (g is Polygon)
                {
                    geometry = unprojectPolygon((Polygon)g, projection);
                }
                else if (g is MultiPoint)
                {
                    geometry = unprojectMultiPoint((MultiPoint)g, projection);
                }
                else
                    throw new NotImplementedException("Geometry \"" + g.GetType().FullName + "\" is not supported.");

                result.Add(geometry);
            }

            return result;
        }

        /// <summary>
        /// Calculates gnomonic projection for geometric shapes on the surface of the ellipsoid.
        /// </summary>
        /// <param name="collection">Collection of geometric shapes on the surface of the ellipsoid</param>
        /// <returns>Gnomonic projection</returns>
        public static GnomonicProjection GetProjection(GeographyCollection collection)
        {
            List<GeoPoint> points = new List<GeoPoint>();
            foreach (IGeography g in collection)
            {
                GeoPoint[] pts = g.ExtractPoints();
                foreach (GeoPoint p in pts)
                    points.Add(p);
            }
            double centerLat = 0;
            double centerLon = 0;
            GnomonicProjection.GetCenter(points, out centerLat, out centerLon);
            return new GnomonicProjection(centerLon, centerLat);
        }
    }

    /// <summary>
    /// The unit sphere.
    /// </summary>
    internal class UnitSphere
    {
        public static Vector3 LatLonToGeocentric(double latRad, double lonRad)
        {
            if (Math.Abs(latRad) > Math.PI / 2)
                throw new ArgumentOutOfRangeException("Math.Abs(latitudeRad) > Math.PI / 2");

            double r = Math.Cos(latRad);
            return new Vector3(r * Math.Cos(lonRad), r * Math.Sin(lonRad), Math.Sin(latRad));
        }

        public static double Latitude(Vector3 p)
        {
            return Math.Atan2(p.Z, Math.Sqrt(p.X * p.X + p.Y * p.Y));
        }

        public static double Longitude(Vector3 p)
        {
            return Math.Atan2(p.Y, p.X);
        }

        /// <summary>
        /// Calculates the points on the great circle bounded by two points.
        /// </summary>
        /// <param name="startPoint">Starting point of the arc</param>
        /// <param name="endPoint">The end point of the arc</param>
        /// <param name="maxAngle">Maximum permissible angle between the points</param>
        /// <returns></returns>
        public static List<GeoPoint> GetArcPoints(GeoPoint startPoint, GeoPoint endPoint, double maxAngle)
        {
            List<GeoPoint> result = new List<GeoPoint>();
            Vector3 startPointVector = LatLonToGeocentric(startPoint.Phi, startPoint.L);
            Vector3 endPointVector = LatLonToGeocentric(endPoint.Phi, endPoint.L);

            double angle = endPointVector.Angle(startPointVector);
            if (angle > maxAngle)
            {
                Vector3 zAxis = (startPointVector + endPointVector).CrossProduct(startPointVector - endPointVector).Unitize();
                Vector3 yAxis = (startPointVector).CrossProduct(zAxis);

                int pointCount = Convert.ToInt32(Math.Ceiling(angle / maxAngle));

                double exactAngle = angle / pointCount;

                double cosine = Math.Cos(exactAngle);
                double sine = Math.Sin(exactAngle);
                double x = cosine;
                double y = sine;

                for (int i = 0; i < pointCount - 1; i++)
                {
                    Vector3 newPoint = (startPointVector * x + yAxis * y).Unitize();

                    result.Add(new GeoPoint(Longitude(newPoint), 
                                                 Latitude(newPoint)));

                    double r = x * cosine - y * sine;
                    y = x * sine + y * cosine;
                    x = r;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Gnomonic projection. 
    /// This class, unlike MapAround.CoordinateSystems.Transformations.Gnomonic, 
    /// is designed for internal use.
    /// </summary>
    internal class GnomonicProjection
    {
        //vector of the center of the projection
        private readonly Vector3 _center;

        // basis vectors in the projection proskosti
        private readonly Vector3 _xAxis;
        private readonly Vector3 _yAxis;

        private static int getMinEntryIndex(double[] values)
        {
            int i = 0;
            if (Math.Abs(values[1]) < Math.Abs(values[0]))
                i = 1;
            if (Math.Abs(values[2]) < Math.Abs(values[i]))
                i = 2;
            return i;
        }

        /// <summary>
        /// Calculates the coordinates of the point in the projection plane.
        /// </summary>
        /// <param name="latitude">Latitude of the</param>
        /// <param name="longitude">Longitude points</param>
        /// <param name="x">X coordinate in the plane of projection</param>
        /// <param name="y">Y-coordinate in the plane of projection</param>
        public void Project(double latitude, double longitude, out double x, out double y)
        {
            Vector3 vector = UnitSphere.LatLonToGeocentric(latitude, longitude);
            double r = vector * _center;

            if (r < 1e-8)
                throw new ArgumentOutOfRangeException("The point is located too far from the center of projection");

            vector = vector / r;

            x = vector * _xAxis;
            y = vector * _yAxis;
        }


        /// <summary>
        /// Calculates the latitude and longitude points.
        /// </summary>
        /// <param name="x">X coordinate in the plane of projection</param>
        /// <param name="y">Y-coordinate in the plane of projection</param>
        /// <param name="lat">Latitude of the</param>
        /// <param name="lon">Longitude points</param>
        public void Unproject(double x, double y, out double lat, out double lon)
        {
            Vector3 vector = _center + _xAxis * x + _yAxis * y;
            lat = UnitSphere.Latitude(vector);
            lon = UnitSphere.Longitude(vector);
        }

        /// <summary>
        /// Calculates the longitude and latitude of the center 
        /// of projection to the passed values ​​of latitudes and longitudes.
        /// </summary>
        /// <param name="latLonSequence">Array of real numbers that contains the latitude and longitude 
        /// (to be completed in the form of a sequence of pairs: "latitude", "longitude")</param>
        /// <param name="centerLat">The output value of latitude</param>
        /// <param name="centerLon">The output value of the longitude</param>
        public static void GetCenter(double[] latLonSequence, out double centerLat, out double centerLon)
        {
            if (latLonSequence.Length % 2 != 0)
                throw new ArgumentException("The array should contain an even number of elements", "values");

            int n = latLonSequence.Length / 2;
            double x = 0;
            double y = 0;
            double z = 0;
            for (int i = 0; i < latLonSequence.Length; i += 2)
            {
                Vector3 v = UnitSphere.LatLonToGeocentric(latLonSequence[i], latLonSequence[i + 1]);
                x += v.X / n;
                y += v.Y / n;
                z += v.Z / n;
            }

            Vector3 result = new Vector3(x, y, z);
            centerLat = UnitSphere.Latitude(result);
            centerLon = UnitSphere.Longitude(result);
        }

        /// <summary>
        /// Calculates the longitude and latitude of the projection center for an array of points on the ellipsoid.
        /// </summary>
        /// <param name="points">An array of points on the ellipsoid</param>
        /// <param name="centerLat">The output value of latitude</param>
        /// <param name="centerLon">The output value of the longitude</param>
        public static void GetCenter(IEnumerable<GeoPoint> points, out double centerLat, out double centerLon)
        {
            int count = 0;
            if (points is IList<GeoPoint>)
                count = (points as IList<GeoPoint>).Count;
            else
                foreach (GeoPoint p in points)
                    count++;

            double[] latLonSequence = new double[count * 2];
            int i = 0;
            foreach(GeoPoint p in points)
            {
                latLonSequence[i++] = p.Phi;
                latLonSequence[i++] = p.L;
            }

            GetCenter(latLonSequence, out centerLat, out centerLon);
        }

        /// <summary>
        /// Instantiates GnommonicProjection.
        /// </summary>
        /// <param name="centerLongitude">Longitude of projection center</param>
        /// <param name="centerLatitude">Latitude of projection center</param>
        public GnomonicProjection(double centerLongitude, double centerLatitude)
        {
            _center = UnitSphere.LatLonToGeocentric(centerLatitude, centerLongitude);

            double[] center = { _center.X, _center.Y, _center.Z };
            double[] vector = new double[3];

            int k = getMinEntryIndex(center);
            int j = (k + 2) % 3;
            int i = (j + 2) % 3;

            vector[i] = -center[j];
            vector[j] = center[i];
            vector[k] = 0;

            _xAxis = (new Vector3(vector[0], vector[1], vector[2])).Unitize();
            _yAxis = _center.CrossProduct(_xAxis);
        }
    }

    /// <summary>
    /// Vector in three-dimensional Euclidean space.
    /// </summary>
    internal class Vector3
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        /// <summary>
        /// Calculates the sum of vectors.
        /// </summary>
        /// <param name="a">The first argument of the addition operation</param>
        /// <param name="b">The second argument of the addition operation</param>
        /// <returns>Vector sum</returns>
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        /// <summary>
        /// Computes the difference vectors.
        /// </summary>
        /// <param name="a">The first argument of the subtraction operation</param>
        /// <param name="b">The second argument of the subtraction operation</param>
        /// <returns>Vector difference</returns>
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        /// <summary>
        /// Computes the product of the vector and scalar.
        /// </summary>
        /// <param name="vector">Vector</param>
        /// <param name="a">Scalar</param>
        /// <returns>Product of the vector and scalar</returns>
        public static Vector3 operator *(Vector3 vector, double a)
        {
            return new Vector3(vector.X * a, vector.Y * a, vector.Z * a);
        }

        /// <summary>
        /// Computes the product of a vector and a scalar return value.
        /// </summary>
        /// <param name="v">Vector</param>
        /// <param name="a">Scalar</param>
        /// <returns>Product of a vector and a scalar return value</returns>
        public static Vector3 operator /(Vector3 v, double a)
        {
            return v * (1 / a);
        }

        /// <summary>
        /// Returns a unit vector with the direction of the vector.
        /// </summary>
        /// <returns>Unit vector with the direction of the vector</returns>
        public Vector3 Unitize()
        {
            return this / Length();
        }

        /// <summary>
        /// The scalar product of vectors.
        /// </summary>
        /// <param name="a">The first argument of the inner product</param>
        /// <param name="b">The second argument of the inner product</param>
        /// <returns>Scalar product</returns>
        public static double operator *(Vector3 a, Vector3 b)
        {
            return b.X * a.X + b.Y * a.Y + b.Z * a.Z;
        }

        /// <summary>
        /// Calculates the square of the length.
        /// </summary>
        /// <returns>Square of the length</returns>
        public double LengthSquared()
        {
            return this * this;
        }

        /// <summary>
        /// Calculates the length of the vector.
        /// </summary>
        /// <returns>Length of the vector</returns>
        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        /// <summary>
        /// Square of the distance between this vector and passed as an argument.
        /// </summary>
        /// <param name="a">Vector</param>
        /// <returns>Square of the distance</returns>
        public double DistanceSquared(Vector3 a)
        {
            return (this - a) * (this - a);
        }

        /// <summary>
        /// The distance between this vector and passed as an argument.
        /// </summary>
        /// <param name="a">Vector</param>
        /// <returns>Distance</returns>
        public double Distance(Vector3 a)
        {
            return Math.Sqrt(DistanceSquared(a));
        }

        /// <summary>
        /// Calculates the cross product of this vector and the vector is passed as an argument.
        /// </summary>
        /// <param name="a">Vector</param>
        /// <returns>Vector product</returns>
        public Vector3 CrossProduct(Vector3 a)
        {
            return new Vector3(Y * a.Z - Z * a.Y, Z * a.X - X * a.Z, X * a.Y - Y * a.X);
        }

        /// <summary>
        /// Angle in radians between this vector and vector passed as an argument.
        /// </summary>
        /// <param name="a">Vector</param>
        /// <returns>Angle</returns>
        public double Angle(Vector3 a)
        {
            return 2 * Math.Asin(this.Distance(a) / (2 * a.Length()));
        }

        /// <summary>
        /// Angle in degrees between the vector and passed as an argument.
        /// </summary>
        /// <param name="a">Vector</param>
        /// <returns>Angle</returns>
        public double AngleInDegrees(Vector3 a)
        {
            return MathUtils.Radians.ToDegrees(Angle(a));
        }

        /// <summary>
        /// Creates an instance of Vector3.
        /// </summary>
        /// <param name="x">The value of x</param>
        /// <param name="y">The value of y</param>
        /// <param name="z">The value of z</param>
        public Vector3(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    /// <summary>
    /// Implements basic algorithms on the spheroid's surface.
    /// </summary>
    public static class EllipticAlgorithms
    {
        [ThreadStatic]
        private static Ellipsoid _ellipsoid = null;

        [ThreadStatic]
        private static bool _ellipsoidAssigned;

        /// <summary>
        /// A default instance of the MapAround.CoordinateSystems.Ellipsoid 
        /// which is used by elliptic algorithms. Always equals to WGS84.
        /// </summary>
        public static readonly Ellipsoid DefaultEllipsoid = Ellipsoid.WGS84;

        /// <summary>
        /// Gets or sets an ellipsoid which is used by elliptic algorithms.
        /// </summary>
        public static Ellipsoid Ellipsoid
        {
            get
            {
                if (!_ellipsoidAssigned)
                    _ellipsoid = DefaultEllipsoid;
                return _ellipsoid;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _ellipsoidAssigned = true;
                _ellipsoid = value;
            }
        }

        /// <summary>
        /// Computes an angular distance between two points on the unit sphere.
        /// </summary>
        /// <returns>An angular distance in radians</returns>
        public static double AngularDistance(GeoPoint p1, GeoPoint p2)
        {
            Vector3 v1 = UnitSphere.LatLonToGeocentric(p1.Phi, p1.L);
            Vector3 v2 = UnitSphere.LatLonToGeocentric(p2.Phi, p2.L);
            return v1.Angle(v2);
        }

        /// <summary>
        /// Calculates the center of mass of points.
        /// Calculated as the point on the surface of the ellipsoid, which "points to" 
        /// a vector, which is the sum of the vectors coming from the center of the 
        /// ellipsoid to each of the points.
        /// </summary>
        /// <remarks>
        /// Masses of points are set equal.
        /// </remarks>
        public static GeoPoint GetPointsCentroid(IEnumerable<GeoPoint> points)
        {
            double latitude = 0;
            double longitude = 0;
            GnomonicProjection.GetCenter(points, out latitude, out longitude);
            return new GeoPoint(latitude, longitude);
        }

        /// <summary>
        /// Computes a convex hull of the specified points.
        /// </summary>
        /// <param name="points">Enumerator of coordinates for which convex hull should be computed</param>
        /// <returns>A list containing a sequence of the convex hull points</returns>
        public static IList<GeoPoint> GetConvexHull(IEnumerable<GeoPoint> points)
        {
            GeographyCollection geographyCollection = new GeographyCollection();
            foreach (GeoPoint p in points)
                geographyCollection.Add(p);

            GnomonicProjection projection = GeometrySpreader.GetProjection(geographyCollection);
            GeometryCollection geometryCollection = GeometrySpreader.GetGeometries(geographyCollection, projection);
            List<ICoordinate> list = new List<ICoordinate>();
            foreach(IGeometry g in geometryCollection)
                list.Add(((PointD)g).Coordinate);

            IList<ICoordinate> planarResult = PlanimetryAlgorithms.GetConvexHull(list);
            geometryCollection.Clear();
            foreach (ICoordinate p in planarResult)
                geometryCollection.Add(new PointD(p));

            geographyCollection = GeometrySpreader.GetGeographies(geometryCollection, projection);
            List<GeoPoint> result = new List<GeoPoint>();
            foreach (GeoPoint p in geographyCollection)
                result.Add(p);

            return result;
        }
    }
#endif
}
