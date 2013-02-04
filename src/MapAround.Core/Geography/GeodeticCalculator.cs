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



/*===========================================================================
** 
** File: GeodeticCalculator.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Classes that provides solving of the direct and inverse geodetic problems
**
=============================================================================*/

#if !DEMO

namespace MapAround.Geography
{
    using System;

    using MapAround.CoordinateSystems;

    /// <summary>
    /// Encapsulates an angle.
    /// <remarks>
    /// Angles are constructed in degrees, but inside are radians for 
    /// convenience of calculation. The comparison is based on the number 
    /// of full circles. Ie 360 degrees is not equal to 0 degrees.
    /// </remarks>
    /// </summary>
    public struct Angle : IComparable<Angle>
    {
        private const double _piOver180 = Math.PI / 180.0;
        private double _degrees;

        /// <summary>
        /// A zero angle.
        /// </summary>
        static public readonly Angle Zero = new Angle(0);

        /// <summary>
        /// A 180 degree angle.
        /// </summary>
        static public readonly Angle Angle180 = new Angle(180);

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.Angle.
        /// </summary>
        /// <param name="degrees">An angle value in degrees</param>
        public Angle(double degrees)
        {
            _degrees = degrees;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.Angle.
        /// </summary>
        /// <param name="degrees">Integer number of degrees</param>
        /// <param name="minutes">Minutes of arc(0  to 60)</param>
        public Angle(int degrees, double minutes)
        {
            _degrees = minutes / 60.0;

            _degrees = (degrees < 0) ? (degrees - _degrees) : (degrees + _degrees);
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.Angle.
        /// </summary>
        /// <param name="degrees">Integer number of degrees</param>
        /// <param name="minutes">Minutes of arc(0  to 60)</param>
        /// <param name="seconds">Seconds of arc(0  to 60)</param>
        public Angle(int degrees, int minutes, double seconds)
        {
            _degrees = (seconds / 3600.0) + (minutes / 60.0);

            _degrees = (degrees < 0) ? (degrees - _degrees) : (degrees + _degrees);
        }

        /// <summary>
        /// Gets or sets an angle value in degrees.
        /// </summary>
        public double Degrees
        {
            get { return _degrees; }
            set { _degrees = value; }
        }

        /// <summary>
        /// Gets or sets an angle value in radians.
        /// </summary>
        public double Radians
        {
            get { return _degrees * _piOver180; }
            set { _degrees = value / _piOver180; }
        }

        /// <summary>
        /// Gets or sets an absolute value of angle in degrees.
        /// </summary>
        public Angle Abs()
        {
            return new Angle(Math.Abs(_degrees));
        }

        /// <summary>
        /// Compares this angle with the other.
        /// </summary>
        /// <param name="other">An angle to compare with this</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects 
        /// being compared. The return value has the following meanings: Value Meaning 
        /// Less than zero This object is less than the other parameter.  Zero This object
        /// is equal to other. Greater than zero This object is greater than other.</returns>
        public int CompareTo(Angle other)
        {
            return _degrees.CompareTo(other._degrees);
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)(_degrees * 1000033);
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// The comparison is based on the number 
        /// of full circles. Ie 360 degrees is not equal to 0 degrees.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current MapAround.Geography.Angle</param>
        public override bool Equals(object obj)
        {
            if (!(obj is Angle)) return false;

            Angle other = (Angle)obj;

            return _degrees == other._degrees;
        }

        #region Operators

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Angle operator +(Angle lhs, Angle rhs)
        {
            return new Angle(lhs._degrees + rhs._degrees);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Angle operator -(Angle lhs, Angle rhs)
        {
            return new Angle(lhs._degrees - rhs._degrees);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >(Angle lhs, Angle rhs)
        {
            return lhs._degrees > rhs._degrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >=(Angle lhs, Angle rhs)
        {
            return lhs._degrees >= rhs._degrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <(Angle lhs, Angle rhs)
        {
            return lhs._degrees < rhs._degrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <=(Angle lhs, Angle rhs)
        {
            return lhs._degrees <= rhs._degrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator ==(Angle lhs, Angle rhs)
        {
            return lhs._degrees == rhs._degrees;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator !=(Angle lhs, Angle rhs)
        {
            return lhs._degrees != rhs._degrees;
        }

        /// <summary>
        /// Implicitly converts the double value to the corner in degrees.
        /// </summary>
        /// <param name="degrees">A doublevalue </param>
        /// <returns>An angle</returns>
        public static implicit operator Angle(double degrees)
        {
            return new Angle(degrees);
        }

        #endregion
    }

    /// <summary>
    /// Represents a geodetic curve on the Earth's surface.
    /// </summary>
    public struct GeodeticCurve
    {
        private readonly double _ellipsoidalDistance;
        private readonly Angle _azimuth;
        private readonly Angle _reverseAzimuth;

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GeodeticCurve.
        /// </summary>
        /// <param name="ellipsoidalDistance">A linear distance between the endpoints in the units of ellipsoid axes</param>
        /// <param name="azimuth">An azimuth in degrees</param>
        /// <param name="reverseAzimuth">A reverse azimuth in degrees</param>
        public GeodeticCurve(double ellipsoidalDistance, Angle azimuth, Angle reverseAzimuth)
        {
            _ellipsoidalDistance = ellipsoidalDistance;
            _azimuth = azimuth;
            _reverseAzimuth = reverseAzimuth;
        }

        /// <summary>
        /// Gets a linear distance between the endpoints.
        /// </summary>
        public double EllipsoidalDistance
        {
            get { return _ellipsoidalDistance; }
        }

        /// <summary>
        /// Gets an azimuth. Azimuth is the number of degrees from north 
        /// to the direction defined by the start and end point.
        /// </summary>
        public Angle Azimuth
        {
            get { return _azimuth; }
        }

        /// <summary>
        /// Gets a reverse azimuth. Reverse azimuth is the number 
        /// of degrees from north to the direction defined by 
        /// the end and start point.
        /// </summary>
        public Angle ReverseAzimuth
        {
            get { return _reverseAzimuth; }
        }
    }

    /// <summary>
    /// Represents a global coordinates. Which defined by the latitude and longitude.
    /// Negative latitude - the southern hemisphere.
    /// Negative longitude - Eastern Hemisphere.
    /// 
    /// The angles in the canonical form belong to the following intervals:
    /// Latitude: -90 to +90 degrees
    /// Longitude: -180 to +180 degrees
    /// </summary>
    public struct GlobalCoordinates : IComparable<GlobalCoordinates>
    {
        /// <summary>
        /// Latitude. Negative values ??- the southern hemisphere.
        /// </summary>
        private Angle _latitude;

        /// <summary>
        /// Longitude. Negative values ??- the Western Hemisphere.
        /// </summary>
        private Angle _longitude;

        /// <summary>
        /// Sets the longitude and latitude to the canonical form.
        /// The angles in the canonical form belong to the following intervals:
        /// Latitude: -90 to +90 degrees
        /// Longitude: -180 to +180 degrees
        /// </summary>
        public void Canonicalize()
        {
            double latitude = _latitude.Degrees;
            double longitude = _longitude.Degrees;

            latitude = (latitude + 180) % 360;
            if (latitude < 0) latitude += 360;
            latitude -= 180;

            if (latitude > 90)
            {
                latitude = 180 - latitude;
                longitude += 180;
            }
            else if (latitude < -90)
            {
                latitude = -180 - latitude;
                longitude += 180;
            }

            longitude = ((longitude + 180) % 360);
            if (longitude <= 0) longitude += 360;
            longitude -= 180;

            _latitude = new Angle(latitude);
            _longitude = new Angle(longitude);
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GlobalCoordinates.
        /// </summary>
        /// <param name="latitude">A latitude value</param>
        /// <param name="longitude">A longitude value</param>
        public GlobalCoordinates(Angle latitude, Angle longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
            Canonicalize();
        }

        /// <summary>
        /// Gets or sets a latitude.
        /// Value will be reduced to the canonical form.
        /// Negative values - the southern hemisphere.
        /// </summary>
        public Angle Latitude
        {
            get { return _latitude; }
            set
            {
                _latitude = value;
                Canonicalize();
            }
        }

        /// <summary>
        /// Gets or sets the longitude. 
        /// Value will be reduced to canonical form. 
        /// Negative value - the Western Hemisphere.
        /// </summary>
        public Angle Longitude
        {
            get { return _longitude; }
            set
            {
                _longitude = value;
                Canonicalize();
            }
        }

        /// <summary>
        /// Compares this coordinates with the other.
        /// West longitude is less than the east. If the longitudes are equal, 
        /// latitudes are compared: the northern latitude is greater than the southern.
        /// </summary>
        /// <param name="other">A coordinates to compare with this</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects 
        /// being compared. The return value has the following meanings: Value Meaning 
        /// Less than zero This object is less than the other parameter.  Zero This object
        /// is equal to other. Greater than zero This object is greater than other.</returns>
        public int CompareTo(GlobalCoordinates other)
        {
            int result;

            if (_longitude < other._longitude) result = -1;
            else if (_longitude > other._longitude) result = +1;
            else if (_latitude < other._latitude) result = -1;
            else if (_latitude > other._latitude) result = +1;
            else result = 0;

            return result;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return ((int)(_longitude.GetHashCode() * (_latitude.GetHashCode() + 1021))) * 1000033;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current MapAround.Geography.GlobalCoordinates</param>
        public override bool Equals(object obj)
        {
            if (!(obj is GlobalCoordinates)) return false;

            GlobalCoordinates other = (GlobalCoordinates)obj;

            return (_longitude == other._longitude) && (_latitude == other._latitude);
        }
    }

    /// <summary>
    /// Represents a global position. Which defined by the latitude, longitude and elevation
    /// above the surface of the ellipsoid.
    /// </summary>
    public struct GlobalPosition : IComparable<GlobalPosition>
    {
        /// <summary>
        /// Global coordinates.
        /// </summary>
        private GlobalCoordinates _coordinates;

        /// <summary>
        /// Elevation, in meters, above the surface of the ellipsoid.
        /// </summary>
        private double _elevation;

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GlobalPosition.
        /// </summary>
        /// <param name="coords">A global coordinates</param>
        /// <param name="elevation">An elevation obove the surface of the ellipsoid (in units of the ellipsoid's axes)</param>
        public GlobalPosition(GlobalCoordinates coords, double elevation)
        {
            _coordinates = coords;
            _elevation = elevation;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GlobalPosition with zero elevation.
        /// </summary>
        /// <param name="coords">A global coordinates</param>
        public GlobalPosition(GlobalCoordinates coords)
            : this(coords, 0.0)
        {
        }

        /// <summary>
        /// Gets or sets a global coordinates.
        /// </summary>
        public GlobalCoordinates Coordinates
        {
            get { return _coordinates; }
            set { _coordinates = value; }
        }

        /// <summary>
        /// Gets or sets an angle of latitude.
        /// </summary>
        public Angle Latitude
        {
            get { return _coordinates.Latitude; }
            set { _coordinates.Latitude = value; }
        }

        /// <summary>
        /// Gets or sets an angle of longitude.
        /// </summary>
        public Angle Longitude
        {
            get { return _coordinates.Longitude; }
            set { _coordinates.Longitude = value; }
        }

        /// <summary>
        /// Gets or sets an elevation obove the surface of the ellipsoid. In units of the ellipsoid's axes.
        /// </summary>
        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; }
        }

        /// <summary>
        /// Compares this position with the other.
        /// West longitude is less than the east. If the longitudes are equal, 
        /// latitudes are compared: the northern latitude is greater than the southern.
        /// If the latitudes are equal, elevation compared: a large object with a high altitude.
        /// </summary>
        /// <param name="other">A position to compare with this</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the objects 
        /// being compared. The return value has the following meanings: Value Meaning 
        /// Less than zero This object is less than the other parameter.  Zero This object
        /// is equal to other. Greater than zero This object is greater than other.</returns>
        public int CompareTo(GlobalPosition other)
        {
            int retval = _coordinates.CompareTo(other._coordinates);

            if (retval == 0)
            {
                if (_elevation < other._elevation) retval = -1;
                else if (_elevation > other._elevation) retval = +1;
            }

            return retval;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = _coordinates.GetHashCode();

            if (_elevation != 0) hash *= (int)_elevation;

            return hash;
        }

        /// <summary>
        /// Derived from <see cref="System.Object"/>.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current MapAround.Geography.GlobalPosition</param>
        public override bool Equals(object obj)
        {
            if (!(obj is GlobalPosition)) return false;

            GlobalPosition other = (GlobalPosition)obj;

            return (_elevation == other._elevation) && (_coordinates.Equals(other._coordinates));
        }
    }

    /// <summary>
    /// Represents a geodetic measurement.
    /// Geodetic measurement is the segment of a geodesic 
    /// line on the ellipsoid and the altitude difference 
    /// between start and end points of the segment.
    /// </summary>
    public struct GeodeticMeasurement
    {
        private readonly GeodeticCurve _curve;
        private readonly double _elevationChange;
        private readonly double _P2P;

        /// <summary>
        /// Initializes a new instance of the MapAround.Geography.GeodeticMeasurement.
        /// </summary>
        /// <param name="averageCurve">A segment of a geodesic line at the average elevation</param>
        /// <param name="elevationChange">A change of the elevation in units of the ellipsoid's axes</param>
        public GeodeticMeasurement(GeodeticCurve averageCurve, double elevationChange)
        {
            double ellDist = averageCurve.EllipsoidalDistance;

            _curve = averageCurve;
            _elevationChange = elevationChange;
            _P2P = Math.Sqrt(ellDist * ellDist + _elevationChange * _elevationChange);
        }

        /// <summary>
        /// Gets a segment of a geodesic line at the average elevation.
        /// </summary>
        public GeodeticCurve AverageCurve
        {
            get { return _curve; }
        }

        /// <summary>
        /// Gets a linear distance between the endpoints of the average geodesic line. 
        /// </summary>
        public double EllipsoidalDistance
        {
            get { return _curve.EllipsoidalDistance; }
        }

        /// <summary>
        /// Gets an azimuth. Azimuth is the number of degrees from north 
        /// to the direction defined by the start and end point.
        /// </summary>
        public Angle Azimuth
        {
            get { return _curve.Azimuth; }
        }

        /// <summary>
        /// Gets a reverse azimuth. Reverse azimuth is the number 
        /// of degrees from north to the direction defined by 
        /// the end and start point.
        /// </summary>
        public Angle ReverseAzimuth
        {
            get { return _curve.ReverseAzimuth; }
        }

        /// <summary>
        /// Gets a change of the elevation in units of the ellipsoid's axes.
        /// </summary>
        public double ElevationChange
        {
            get { return _elevationChange; }
        }

        /// <summary>
        /// Gets a distance between endpoints in the units of the ellipsoid's axes.
        /// </summary>
        public double PointToPointDistance
        {
            get { return _P2P; }
        }
    }

    /// <summary>
    /// Solves the direct and the inverse geodetic problems.
    /// Implements Thaddeus Vincenty algorithms.
    /// See http://www.ngs.noaa.gov/PUBS_LIB/inverse.pdf
    /// </summary>
    public class GeodeticCalculator
    {
        private const double TwoPi = 2.0 * Math.PI;

        /// <summary>
        /// Solves the direct geodetic problem.
        /// Calculates the destination for a given point, direction and distance. 
        /// </summary>
        /// <param name="ellipsoid">An ellipsoid</param>
        /// <param name="start">A start point</param>
        /// <param name="startBearing">A start bearing</param>
        /// <param name="distance">A distance to target point</param>
        /// <param name="endBearing">An end bearing</param>
        /// <returns>A global coordinates of the destination point</returns>
        public GlobalCoordinates CalculateEndingGlobalCoordinates(Ellipsoid ellipsoid, GlobalCoordinates start, Angle startBearing, double distance, out Angle endBearing)
        {
            double a = ellipsoid.SemiMajorAxis;
            double b = ellipsoid.SemiMinorAxis;
            double aSquared = a * a;
            double bSquared = b * b;
            double f = ellipsoid.IsInvFDefinitive ? 1 / ellipsoid.InverseFlattening : 0;
            double phi1 = start.Latitude.Radians;
            double alpha1 = startBearing.Radians;
            double cosAlpha1 = Math.Cos(alpha1);
            double sinAlpha1 = Math.Sin(alpha1);
            double s = distance;
            double tanU1 = (1.0 - f) * Math.Tan(phi1);
            double cosU1 = 1.0 / Math.Sqrt(1.0 + tanU1 * tanU1);
            double sinU1 = tanU1 * cosU1;

            // eq. 1
            double sigma1 = Math.Atan2(tanU1, cosAlpha1);

            // eq. 2
            double sinAlpha = cosU1 * sinAlpha1;

            double sin2Alpha = sinAlpha * sinAlpha;
            double cos2Alpha = 1 - sin2Alpha;
            double uSquared = cos2Alpha * (aSquared - bSquared) / bSquared;

            // eq. 3
            double A = 1 + (uSquared / 16384) * (4096 + uSquared * (-768 + uSquared * (320 - 175 * uSquared)));

            // eq. 4
            double B = (uSquared / 1024) * (256 + uSquared * (-128 + uSquared * (74 - 47 * uSquared)));

            // iterate until there is a negligible change in sigma
            double deltaSigma;
            double sOverbA = s / (b * A);
            double sigma = sOverbA;
            double sinSigma;
            double prevSigma = sOverbA;
            double sigmaM2;
            double cosSigmaM2;
            double cos2SigmaM2;

            while (true)
            {
                // eq. 5
                sigmaM2 = 2.0 * sigma1 + sigma;
                cosSigmaM2 = Math.Cos(sigmaM2);
                cos2SigmaM2 = cosSigmaM2 * cosSigmaM2;
                sinSigma = Math.Sin(sigma);
                double cosSignma = Math.Cos(sigma);

                // eq. 6
                deltaSigma = B * sinSigma * (cosSigmaM2 + (B / 4.0) * (cosSignma * (-1 + 2 * cos2SigmaM2)
                    - (B / 6.0) * cosSigmaM2 * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM2)));

                // eq. 7
                sigma = sOverbA + deltaSigma;

                // break after converging to tolerance
                if (Math.Abs(sigma - prevSigma) < 1e-13) 
                    break;

                prevSigma = sigma;
            }

            sigmaM2 = 2.0 * sigma1 + sigma;
            cosSigmaM2 = Math.Cos(sigmaM2);
            cos2SigmaM2 = cosSigmaM2 * cosSigmaM2;

            double cosSigma = Math.Cos(sigma);
            sinSigma = Math.Sin(sigma);

            // eq. 8
            double phi2 = Math.Atan2(sinU1 * cosSigma + cosU1 * sinSigma * cosAlpha1,
                                     (1.0 - f) * Math.Sqrt(sin2Alpha + Math.Pow(sinU1 * sinSigma - cosU1 * cosSigma * cosAlpha1, 2.0)));

            // eq. 9
            // This fixes the pole crossing defect spotted by Matt Feemster.  When a path
            // passes a pole and essentially crosses a line of latitude twice - once in
            // each direction - the longitude calculation got messed up.  Using Atan2
            // instead of Atan fixes the defect.  The change is in the next 3 lines.
            //double tanLambda = sinSigma * sinAlpha1 / (cosU1 * cosSigma - sinU1*sinSigma*cosAlpha1);
            //double lambda = Math.Atan(tanLambda);
            double lambda = Math.Atan2(sinSigma * sinAlpha1, cosU1 * cosSigma - sinU1 * sinSigma * cosAlpha1);

            // eq. 10
            double C = (f / 16) * cos2Alpha * (4 + f * (4 - 3 * cos2Alpha));

            // eq. 11
            double L = lambda - (1 - C) * f * sinAlpha * (sigma + C * sinSigma * (cosSigmaM2 + C * cosSigma * (-1 + 2 * cos2SigmaM2)));

            // eq. 12
            double alpha2 = Math.Atan2(sinAlpha, -sinU1 * sinSigma + cosU1 * cosSigma * cosAlpha1);

            // build result
            Angle latitude = new Angle();
            Angle longitude = new Angle();

            latitude.Radians = phi2;
            longitude.Radians = start.Longitude.Radians + L;

            endBearing = new Angle();
            endBearing.Radians = alpha2;

            return new GlobalCoordinates(latitude, longitude);
        }

        /// <summary>
        /// Solves the direct geodetic problem.
        /// Calculates the destination for a given point, direction and distance. 
        /// </summary>
        /// <param name="ellipsoid">An ellipsoid</param>
        /// <param name="start">A start point</param>
        /// <param name="startBearing">A start bearing</param>
        /// <param name="distance">A distance to target point</param>
        /// <returns>A global coordinates of the destination point</returns>
        public GlobalCoordinates CalculateEndingGlobalCoordinates(Ellipsoid ellipsoid, GlobalCoordinates start, Angle startBearing, double distance)
        {
            Angle endBearing = new Angle();

            return CalculateEndingGlobalCoordinates(ellipsoid, start, startBearing, distance, out endBearing);
        }

        /// <summary>
        /// Solves the inverse geodetic problem.
        /// Calculates the geodetic curve between two points on the ellipsoid. 
        /// </summary>
        /// <param name="ellipsoid">An ellipsoid</param>
        /// <param name="start">A start point</param>
        /// <param name="end">An end point</param>
        /// <returns>A geodetic curve between the start and the end points</returns>
        public GeodeticCurve CalculateGeodeticCurve(Ellipsoid ellipsoid, GlobalCoordinates start, GlobalCoordinates end)
        {
            // http://www.ngs.noaa.gov/PUBS_LIB/inverse.pdf

            double a = ellipsoid.SemiMajorAxis;
            double b = ellipsoid.SemiMinorAxis;
            double f = ellipsoid.IsInvFDefinitive ? 1 / ellipsoid.InverseFlattening : 0;

            // get values ??in radians
            double phi1 = start.Latitude.Radians;
            double lambda1 = start.Longitude.Radians;
            double phi2 = end.Latitude.Radians;
            double lambda2 = end.Longitude.Radians;

            // computing
            double a2 = a * a;
            double b2 = b * b;
            double a2b2b2 = (a2 - b2) / b2;

            double omega = lambda2 - lambda1;

            double tanphi1 = Math.Tan(phi1);
            double tanU1 = (1.0 - f) * tanphi1;
            double U1 = Math.Atan(tanU1);
            double sinU1 = Math.Sin(U1);
            double cosU1 = Math.Cos(U1);

            double tanphi2 = Math.Tan(phi2);
            double tanU2 = (1.0 - f) * tanphi2;
            double U2 = Math.Atan(tanU2);
            double sinU2 = Math.Sin(U2);
            double cosU2 = Math.Cos(U2);

            double sinU1sinU2 = sinU1 * sinU2;
            double cosU1sinU2 = cosU1 * sinU2;
            double sinU1cosU2 = sinU1 * cosU2;
            double cosU1cosU2 = cosU1 * cosU2;

            // eq. 13
            double lambda = omega;

            // intermediates we'll need to compute 's'
            double A = 0.0;
            double B = 0.0;
            double sigma = 0.0;
            double deltasigma = 0.0;
            double lambda0;
            bool converged = false;

            for (int i = 0; i < 20; i++)
            {
                lambda0 = lambda;

                double sinlambda = Math.Sin(lambda);
                double coslambda = Math.Cos(lambda);

                // eq. 14
                double sin2sigma = (cosU2 * sinlambda * cosU2 * sinlambda) + Math.Pow(cosU1sinU2 - sinU1cosU2 * coslambda, 2.0);
                double sinsigma = Math.Sqrt(sin2sigma);

                // eq. 15
                double cossigma = sinU1sinU2 + (cosU1cosU2 * coslambda);

                // eq. 16
                sigma = Math.Atan2(sinsigma, cossigma);

                // eq. 17    Careful!  sin2sigma might be almost 0!
                double sinalpha = (sin2sigma == 0) ? 0.0 : cosU1cosU2 * sinlambda / sinsigma;
                double alpha = Math.Asin(sinalpha);
                double cosalpha = Math.Cos(alpha);
                double cos2alpha = cosalpha * cosalpha;

                // eq. 18    Careful!  cos2alpha might be almost 0!
                double cos2sigmam = cos2alpha == 0.0 ? 0.0 : cossigma - 2 * sinU1sinU2 / cos2alpha;
                double u2 = cos2alpha * a2b2b2;

                double cos2sigmam2 = cos2sigmam * cos2sigmam;

                // eq. 3
                A = 1.0 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2)));

                // eq. 4
                B = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));

                // eq. 6
                deltasigma = B * sinsigma * (cos2sigmam + B / 4 * (cossigma * (-1 + 2 * cos2sigmam2) - B / 6 * cos2sigmam * (-3 + 4 * sin2sigma) * (-3 + 4 * cos2sigmam2)));

                // eq. 10
                double C = f / 16 * cos2alpha * (4 + f * (4 - 3 * cos2alpha));

                // eq. 11 (modified)
                lambda = omega + (1 - C) * f * sinalpha * (sigma + C * sinsigma * (cos2sigmam + C * cossigma * (-1 + 2 * cos2sigmam2)));

                // see how much improvement we got
                double change = Math.Abs((lambda - lambda0) / lambda);

                if ((i > 1) && (change < 1e-13))
                {
                    converged = true;
                    break;
                }
            }

            // eq. 19
            double s = b * A * (sigma - deltasigma);
            Angle alpha1;
            Angle alpha2;

            // didn't converge?  must be N/S
            if (!converged)
            {
                if (phi1 > phi2)
                {
                    alpha1 = Angle.Angle180;
                    alpha2 = Angle.Zero;
                }
                else if (phi1 < phi2)
                {
                    alpha1 = Angle.Zero;
                    alpha2 = Angle.Angle180;
                }
                else
                {
                    alpha1 = new Angle(Double.NaN);
                    alpha2 = new Angle(Double.NaN);
                }
            }

            // else, it converged, so do the math
            else
            {
                double radians;
                alpha1 = new Angle();
                alpha2 = new Angle();

                // eq. 20
                radians = Math.Atan2(cosU2 * Math.Sin(lambda), (cosU1sinU2 - sinU1cosU2 * Math.Cos(lambda)));
                if (radians < 0.0) radians += TwoPi;
                alpha1.Radians = radians;

                // eq. 21
                radians = Math.Atan2(cosU1 * Math.Sin(lambda), (-sinU1cosU2 + cosU1sinU2 * Math.Cos(lambda))) + Math.PI;
                if (radians < 0.0) radians += TwoPi;
                alpha2.Radians = radians;
            }

            if (alpha1 >= 360.0) alpha1 -= 360.0;
            if (alpha2 >= 360.0) alpha2 -= 360.0;

            return new GeodeticCurve(s, alpha1, alpha2);
        }

        /// <summary>
        /// Calculates the distance between two points on the ellipsoid given elevations.
        /// 
        /// First finded a new ellipsoid which radius is greater (or less) than the radius of the original ellipsoid
        /// by the difference between the elevations. Then calculated the length of a geodesic on this ellipsoid. The 
        /// distance between two points is calculated as the hypotenuse of a triangle, which cathetus are geodesic 
        /// length and height difference.
        /// </summary>
        /// <param name="refEllipsoid">Ellipsoid</param>
        /// <param name="start">A start point</param>
        /// <param name="end">An end point</param>
        /// <returns>An object that represents the result</returns>
        public GeodeticMeasurement CalculateGeodeticMeasurement(Ellipsoid refEllipsoid, GlobalPosition start, GlobalPosition end)
        {
            GlobalCoordinates startCoords = start.Coordinates;
            GlobalCoordinates endCoords = end.Coordinates;

            // calculate the average elevation
            double elev1 = start.Elevation;
            double elev2 = end.Elevation;
            double elev12 = (elev1 + elev2) / 2.0;

            // compute the difference latitudes
            double phi1 = startCoords.Latitude.Radians;
            double phi2 = endCoords.Latitude.Radians;
            double phi12 = (phi1 + phi2) / 2.0;

            // Calculate new ellipsoid
            double refA = refEllipsoid.SemiMajorAxis;
            double f = refEllipsoid.IsInvFDefinitive ? 1 / refEllipsoid.InverseFlattening : 0;
            double a = refA + elev12 * (1.0 + f * Math.Sin(phi12));

            Ellipsoid ellipsoid = new Ellipsoid(a, f == 0 ? a : 0, f == 0 ? 0 : 1 / f, f != 0, LinearUnit.Metre, string.Empty, string.Empty, 0, string.Empty, string.Empty, string.Empty);

            // calculate the geodesic on an average elevation
            GeodeticCurve averageCurve = CalculateGeodeticCurve(ellipsoid, startCoords, endCoords);

            return new GeodeticMeasurement(averageCurve, elev2 - elev1);
        }
    }
}

#endif