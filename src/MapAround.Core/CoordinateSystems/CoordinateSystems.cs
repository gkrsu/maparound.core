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
** File: CoordinateSystems.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes that describes coordinate systems
**
=============================================================================*/

namespace MapAround.CoordinateSystems
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    using MapAround.Serialization;
    using MapAround.CoordinateSystems.Projections;

    /// <summary>
    /// The MapAround.CoordinateSystems namespace contains 
    /// interfaces and classes for coordinate system definition.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// The MapAround.CoordinateSystems.IUnit interface abstracts 
    /// different kinds of units, it has no methods.
    /// </summary>
    public interface IUnit : IInfo { }

    /// <summary>
    /// Defines methods of angular units.
    /// </summary>
    public interface IAngularUnit : IUnit
    {
        /// <summary>
        /// Gets or sets the number of radians per angular unit.
        /// </summary>
        double RadiansPerUnit { get; set; }
    }

    /// <summary>
    /// Defines methods of linear units.
    /// </summary>
    public interface ILinearUnit : IUnit
    {
        /// <summary>
        /// Gets or sets the number of meters per unit.
        /// </summary>
        double MetersPerUnit { get; set; }
    }

    /// <summary>
    /// Represents units of measurement.
    /// </summary>
    public class Unit : SpatialReferenceInfo, IUnit
    {
        private double _conversionFactor;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Unit.
        /// </summary>
        /// <param name="conversionFactor">Conversion factor to base unit</param>
        /// <param name="name">Name of unit</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal Unit(double conversionFactor, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            :
            base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _conversionFactor = conversionFactor;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Unit.
        /// </summary>
        /// <param name="name">Name of unit</param>
        /// <param name="conversionFactor">Conversion factor to base unit</param>
        internal Unit(string name, double conversionFactor)
            : this(conversionFactor, name, String.Empty, -1, String.Empty, String.Empty, String.Empty)
        {
        }

        /// <summary>
        /// Gets or sets the number of units per base-unit.
        /// </summary>
        public double ConversionFactor
        {
            get { return _conversionFactor; }
            set { _conversionFactor = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", Name, _conversionFactor);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <exception cref="NotImplementedException">Throws always</exception>
        public override string XML
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is Unit))
                return false;
            return (obj as Unit).ConversionFactor == this.ConversionFactor;
        }
    }

    /// <summary>
    /// Represents a linear unit of measurement.
    /// </summary>
    public class LinearUnit : SpatialReferenceInfo, ILinearUnit
    {
        private double _metersPerUnit;

        /// <summary>
        /// Initializes a new instance of an instance of the MapAround.CoordinateSystems.LinearUnit.
        /// </summary>
        /// <param name="metersPerUnit">Number of meters per LinearUnit></param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        public LinearUnit(double metersPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            :
            base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _metersPerUnit = metersPerUnit;
        }

        #region Predefined units

        /// <summary>
        /// Gets the International metre. SI standard unit.
        /// </summary>
        public static ILinearUnit Metre
        {
            get { return new LinearUnit(1.0, "metre", "EPSG", 9001, "m", String.Empty, "Also known as International metre. SI standard unit."); }
        }

        /// <summary>
        /// Gets the foot linear unit (1ft = 0.3048m).
        /// </summary>
        public static ILinearUnit Foot
        {
            get { return new LinearUnit(0.3048, "foot", "EPSG", 9002, "ft", String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the US Survey foot linear unit (1ftUS = 0.304800609601219m).
        /// </summary>
        public static ILinearUnit USSurveyFoot
        {
            get { return new LinearUnit(0.304800609601219, "US survey foot", "EPSG", 9003, "American foot", "ftUS", "Used in USA."); }
        }


        /// <summary>
        /// Gets the Nautical Mile linear unit (1NM = 1852m).
        /// </summary>
        public static ILinearUnit NauticalMile
        {
            get { return new LinearUnit(1852, "nautical mile", "EPSG", 9030, "NM", String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets Clarke's foot.
        /// </summary>
        /// <remarks>
        /// Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre.
        /// Used in older Australian, southern African &amp; British West Indian mapping.
        /// </remarks>
        public static ILinearUnit ClarkesFoot
        {
            get { return new LinearUnit(0.3047972654, "Clarke's foot", "EPSG", 9005, "Clarke's foot", String.Empty, "Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre. Used in older Australian, southern African & British West Indian mapping."); }
        }

        #endregion

        #region ILinearUnit Members

        /// <summary>
        /// Gets or sets the number of meters per LinearUnit>.
        /// </summary>
        public double MetersPerUnit
        {
            get { return _metersPerUnit; }
            set { _metersPerUnit = value; }
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", Name, MetersPerUnit);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_LinearUnit MetersPerUnit=\"{0}\">{1}</CS_LinearUnit>", MetersPerUnit, InfoXml);
            }
        }

        #endregion

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is LinearUnit))
                return false;
            return (obj as LinearUnit).MetersPerUnit == this.MetersPerUnit;
        }
    }

    /// <summary>
    /// Represents an angular unit of measurement.
    /// </summary>
    public class AngularUnit : SpatialReferenceInfo, IAngularUnit
    {
        private double _radiansPerUnit;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.AngularUnit.
        /// </summary>
        /// <param name="radiansPerUnit">Radians per unit</param>
        public AngularUnit(double radiansPerUnit)
            : this(
            radiansPerUnit, String.Empty, String.Empty, -1, String.Empty, String.Empty, String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.AngularUnit.
        /// </summary>
        /// <param name="radiansPerUnit">Radians per unit</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal AngularUnit(double radiansPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            :
            base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _radiansPerUnit = radiansPerUnit;
        }

        #region Predifined units

        /// <summary>
        /// Gets the angular degrees that is PI/180 = 0.017453292519943295769236907684886 radians
        /// </summary>
        public static AngularUnit Degrees
        {
            get { return new AngularUnit(0.017453292519943295769236907684886, "degree", "EPSG", 9102, "deg", String.Empty, "=pi/180 radians"); }
        }

        /// <summary>
        /// Gets the radian. SI standard unit.
        /// </summary>
        public static AngularUnit Radian
        {
            get { return new AngularUnit(1, "radian", "EPSG", 9101, "rad", String.Empty, "SI standard unit."); }
        }

        /// <summary>
        /// Gets the grad. PI / 200 = 0.015707963267948966192313216916398 radians
        /// </summary>
        public static AngularUnit Grad
        {
            get { return new AngularUnit(0.015707963267948966192313216916398, "grad", "EPSG", 9105, "gr", String.Empty, "=pi/200 radians."); }
        }

        /// <summary>
        /// Gets the gon. PI / 200 = 0.015707963267948966192313216916398 radians
        /// </summary>
        /// 
        public static AngularUnit Gon
        {
            get { return new AngularUnit(0.015707963267948966192313216916398, "gon", "EPSG", 9106, "g", String.Empty, "=pi/200 radians."); }
        }

        #endregion

        #region IAngularUnit Members
       
        /// <summary>
        /// Gets or sets the number of radians per <see cref="AngularUnit" />.
        /// </summary>
        public double RadiansPerUnit
        {
            get { return _radiansPerUnit; }
            set { _radiansPerUnit = value; }
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", Name, RadiansPerUnit);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_AngularUnit RadiansPerUnit=\"{0}\">{1}</CS_AngularUnit>", RadiansPerUnit, InfoXml);
            }
        }

        #endregion

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is AngularUnit))
                return false;
            return (obj as AngularUnit).RadiansPerUnit == this.RadiansPerUnit;
        }
    }

    /// <summary>
    /// Defines the standard information stored with ellipsoid objects.
    /// </summary>
    public interface IEllipsoid : IInfo
    {
        /// <summary>
        /// Calculates an area of the ellipsoid surface.
        /// </summary>
        /// <returns>an area of the ellipsoid surface</returns>
        double SurfaceArea();

        /// <summary>
        /// Gets or sets the value of the semi-major axis.
        /// </summary>
        double SemiMajorAxis { get; set; }

        /// <summary>
        /// Gets or sets the value of the semi-minor axis.
        /// </summary>
        double SemiMinorAxis { get; set; }

        /// <summary>
        /// Gets or sets the value of the inverse of the flattening constant of the ellipsoid.
        /// </summary>
        double InverseFlattening { get; set; }

        /// <summary>
        /// Gets or sets the value of the axis unit.
        /// </summary>
        ILinearUnit AxisUnit { get; set; }

        /// <summary>
        /// Gets a value indicating whether the Inverse Flattening is definitive 
        /// for this ellipsoid. 
        /// <para>Some ellipsoids use the IVF as the defining value, 
        /// and calculate the polar radius whenever asked. Otherellipsoids use the 
        /// polar radius to calculate the IVF whenever asked. This distinction can 
        /// be important to avoid floating-point rounding errors.
        /// </para>
        /// </summary>
        bool IsInvFDefinitive { get; set; }
    }

    /// <summary>
    /// Represents an ellipsoid.
    /// </summary>
    public class Ellipsoid : SpatialReferenceInfo, IEllipsoid
    {
        private double _semiMajorAxis;
        private double _semiMinorAxis;
        private double _inverseFlattening;
        private ILinearUnit _axisUnit;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Ellipsoid.
        /// </summary>
        /// <param name="semiMajorAxis">Semi major axis</param>
        /// <param name="semiMinorAxis">Semi minor axis</param>
        /// <param name="inverseFlattening">Inverse flattening</param>
        /// <param name="isIvfDefinitive">Inverse Flattening is definitive for this ellipsoid (Semi-minor axis will be overridden)</param>
        /// <param name="axisUnit">Axis unit</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal Ellipsoid(
            double semiMajorAxis,
            double semiMinorAxis,
            double inverseFlattening,
            bool isIvfDefinitive,
            ILinearUnit axisUnit, string name, string authority, long code, string alias,
            string abbreviation, string remarks)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            _semiMajorAxis = semiMajorAxis;
            _inverseFlattening = inverseFlattening;
            _axisUnit = axisUnit;
            _isInvFDefinitive = isIvfDefinitive;
            if (isIvfDefinitive && (inverseFlattening == 0 || double.IsInfinity(inverseFlattening)))
                _semiMinorAxis = semiMajorAxis;
            else if (isIvfDefinitive)
                _semiMinorAxis = (1.0 - (1.0 / _inverseFlattening)) * semiMajorAxis;
            else
                _semiMinorAxis = semiMinorAxis;
        }

        #region Predefined ellipsoids

        /// <summary>
        /// Gets the Krassovsky ellipsoid.
        /// </summary>
        public static Ellipsoid Krasovsky1940
        {
            get
            {
                return new Ellipsoid(6378245, 0, 298.3, true, LinearUnit.Metre, "Krassowsky 1940", "EPSG", 7024, "Krassowsky 1940", "",
                    "Krasovsky ellipsoid 1940");
            }
        }

        /// <summary>
        /// Gets the PZ90 ellipsoid which is used by GLONASS system.
        /// </summary>
        public static Ellipsoid PZ90
        {
            get
            {
                return new Ellipsoid(6378136, 0, 298.257839303, true, LinearUnit.Metre, "PZ-90", "EPSG", 7054, "PZ90", "",
                    "Parameters of the Earth 90. Geodesy and Cartography, 1993.");
            }
        }

        /// <summary>
        /// Gets the WGS84 ellipsoid.
        /// </summary>
        public static Ellipsoid WGS84
        {
            get
            {
                return new Ellipsoid(6378137, 0, 298.257223563, true, LinearUnit.Metre, "WGS 84", "EPSG", 7030, "WGS84", "",
                    "Inverse flattening derived from four defining parameters (semi-major axis; C20 = -484.16685*10e-6; earth's angular velocity w = 7292115e11 rad/sec; gravitational constant GM = 3986005e8 m*m*m/s/s).");
            }
        }

        /// <summary>
        /// Gets the WGS72 ellipsoid.
        /// </summary>
        public static Ellipsoid WGS72
        {
            get
            {
                return new Ellipsoid(6378135.0, 0, 298.26, true, LinearUnit.Metre, "WGS 72", "EPSG", 7043, "WGS 72", String.Empty, String.Empty);
            }
        }

        /// <summary>
        /// Gets the GRS 1980 ellipsoid.
        /// </summary>
        public static Ellipsoid GRS80
        {
            get
            {
                return new Ellipsoid(6378137, 0, 298.257222101, true, LinearUnit.Metre, "GRS 1980", "EPSG", 7019, "International 1979", "",
                    "Adopted by IUGG 1979 Canberra.  Inverse flattening is derived from geocentric gravitational constant GM = 3986005e8 m*m*m/s/s; dynamic form factor J2 = 108263e8 and Earth's angular velocity = 7292115e-11 rad/s.");
            }
        }

        /// <summary>
        /// Gets the International 1924 ellipsoid.
        /// </summary>
        public static Ellipsoid International1924
        {
            get
            {
                return new Ellipsoid(6378388, 0, 297, true, LinearUnit.Metre, "International 1924", "EPSG", 7022, "Hayford 1909", String.Empty,
                    "Described as a=6378388 m. and b=6356909 m. from which 1/f derived to be 296.95926. The figure was adopted as the International ellipsoid in 1924 but with 1/f taken as 297 exactly from which b is derived as 6356911.946m.");
            }
        }

        /// <summary>
        /// Gets an authalic sphere derived from GRS 1980 ellipsoid (code 7019).
        /// </summary>
        /// <remarks>
        ///  An authalic sphere is one with a surface area equal to the surface 
        ///  area of the ellipsoid). 1/f is infinite.
        /// </remarks>
        public static Ellipsoid Sphere
        {
            get
            {
                return new Ellipsoid(6370997.0, 6370997.0, double.PositiveInfinity, false, LinearUnit.Metre, "GRS 1980 Authalic Sphere", "EPSG", 7048, "Sphere", "",
                    "Authalic sphere derived from GRS 1980 ellipsoid (code 7019).  (An authalic sphere is one with a surface area equal to the surface area of the ellipsoid). 1/f is infinite.");
            }
        }

        #endregion

        #region IEllipsoid Members

        /// <summary>
        /// Calculates an area of the ellipsoid surface.
        /// </summary>
        /// <returns>An area of the ellipsoid surface.</returns>
        public double SurfaceArea()
        {
            if (SemiMajorAxis > SemiMinorAxis)
            {
                double a = SemiMajorAxis;
                double b = SemiMinorAxis;
                double ab = Math.Sqrt(a * a - b * b);

                return 2.0 * Math.PI * a * (a + b * b / ab * Math.Log((a + ab) / b));
            }
            else
                if (SemiMajorAxis == SemiMinorAxis)
                {
                    return 4.0 * Math.PI * SemiMajorAxis * SemiMajorAxis;
                }
                else
                {
                    double a = SemiMajorAxis;
                    double b = SemiMinorAxis;
                    double ba = Math.Sqrt(b * b - a * a);

                    return 2.0 * Math.PI * a * (a + b * b / ba * Math.Asin(ba / b));
                }
        }

        /// <summary>
        /// Gets or sets the value of the semi-major axis.
        /// </summary>
        public double SemiMajorAxis
        {
            get { return _semiMajorAxis; }
            set { _semiMajorAxis = value; }
        }

        /// <summary>
        /// Gets or sets the value of the semi-minor axis.
        /// </summary>
        public double SemiMinorAxis
        {
            get { return _semiMinorAxis; }
            set { _semiMinorAxis = value; }
        }

        /// <summary>
        /// Gets or sets the value of the inverse of the flattening constant of the ellipsoid.
        /// </summary>
        public double InverseFlattening
        {
            get { return _inverseFlattening; }
            set { _inverseFlattening = value; }
        }

        /// <summary>
        /// Gets or sets the value of the axis unit.
        /// </summary>
        public ILinearUnit AxisUnit
        {
            get { return _axisUnit; }
            set { _axisUnit = value; }
        }

        private bool _isInvFDefinitive;

        /// <summary>
        /// Gets or sets a value indicating whether the Inverse Flattening is definitive 
        /// for this ellipsoid. 
        /// <para>Some ellipsoids use the IVF as the defining value, 
        /// and calculate the polar radius whenever asked. Otherellipsoids use the 
        /// polar radius to calculate the IVF whenever asked. This distinction can 
        /// be important to avoid floating-point rounding errors.
        /// </para>
        /// </summary>
        public bool IsInvFDefinitive
        {
            get { return _isInvFDefinitive; }
            set { _isInvFDefinitive = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "SPHEROID[\"{0}\", {1}, {2}", Name, SemiMajorAxis, InverseFlattening);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_Ellipsoid SemiMajorAxis=\"{0}\" SemiMinorAxis=\"{1}\" InverseFlattening=\"{2}\" IvfDefinitive=\"{3}\">{4}{5}</CS_Ellipsoid>",
                    SemiMajorAxis, SemiMinorAxis, InverseFlattening, (IsInvFDefinitive ? 1 : 0), InfoXml, AxisUnit.XML); ;
            }
        }

        #endregion

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is Ellipsoid))
                return false;
            Ellipsoid e = obj as Ellipsoid;
            return (e.InverseFlattening == this.InverseFlattening &&
                    e.IsInvFDefinitive == this.IsInvFDefinitive &&
                    e.SemiMajorAxis == this.SemiMajorAxis &&
                    e.SemiMinorAxis == this.SemiMinorAxis &&
                    e.AxisUnit.EqualParams(this.AxisUnit));
        }
    }

    /// <summary>
    /// Provides access to members of the datum object.
    /// </summary>
    /// <remarks>
    /// For the OGC abstract model, it can be defined as a set of real points on the Earth
    /// that have coordinates. A datum can be thought of as a set of parameters
    /// defining completely the origin and orientation of a coordinate system with respect
    /// to the earth. A textual description and/or a set of parameters describing the
    /// relationship of a coordinate system to some predefined physical locations (such
    /// as center of mass) and physical directions (such as axis of spin). The definition
    /// of the datum may also include the temporal behavior (such as the rate of change of
    /// the orientation of the coordinate axes).
    /// </remarks>
    public interface IDatum : IInfo
    {
        /// <summary>
        /// Gets or sets the type of the datum.
        /// </summary>
        DatumType DatumType { get; set; }
    }

    /// <summary>
    /// Defines the standard information stored with prime
    /// meridian objects. Any prime meridian object must 
    /// implement this interface as well as the IInfo 
    /// interface.
    /// </summary>
    public interface IPrimeMeridian : IInfo
    {
        /// <summary>
        /// Gets or sets the longitude of the prime meridian 
        /// (relative to the Greenwich prime meridian).
        /// </summary>
        double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the AngularUnits.
        /// </summary>
        IAngularUnit AngularUnit { get; set; }
    }

    /// <summary>
    /// Represents a meridian used to take longitude 
    /// measurements from.
    /// </summary>
    public class PrimeMeridian : SpatialReferenceInfo, IPrimeMeridian
    {
        private double _longitude;
        private IAngularUnit _angularUnit;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.PrimeMeridian .
        /// </summary>
        /// <param name="longitude">Longitude of prime meridian</param>
        /// <param name="angularUnit">Angular unit</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal PrimeMeridian(double longitude, IAngularUnit angularUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            :
            base(name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _longitude = longitude;
            _angularUnit = angularUnit;
        }

        #region Predefined prime meridans

        /// <summary>
        /// Gets the Greenwich prime meridian.
        /// </summary>
        public static PrimeMeridian Greenwich
        {
            get { return new PrimeMeridian(0.0, CoordinateSystems.AngularUnit.Degrees, "Greenwich", "EPSG", 8901, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Lisbon prime meridian.
        /// </summary>
        public static PrimeMeridian Lisbon
        {
            get { return new PrimeMeridian(-9.0754862, CoordinateSystems.AngularUnit.Degrees, "Lisbon", "EPSG", 8902, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Paris prime meridian.
        /// </summary>
        public static PrimeMeridian Paris
        {
            get { return new PrimeMeridian(2.5969213, CoordinateSystems.AngularUnit.Degrees, "Paris", "EPSG", 8903, String.Empty, String.Empty, "Value adopted by IGN (Paris) in 1936. Equivalent to 2 deg 20min 14.025sec. Preferred by EPSG to earlier value of 2deg 20min 13.95sec (2.596898 grads) used by RGS London."); }
        }

        /// <summary>
        /// Gets the Madrid prime meridian.
        /// </summary>
        public static PrimeMeridian Madrid
        {
            get { return new PrimeMeridian(-3.411658, CoordinateSystems.AngularUnit.Degrees, "Madrid", "EPSG", 8905, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Rome prime meridian.
        /// </summary>
        public static PrimeMeridian Rome
        {
            get { return new PrimeMeridian(12.27084, CoordinateSystems.AngularUnit.Degrees, "Rome", "EPSG", 8906, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Bern prime meridian.
        /// </summary>
        public static PrimeMeridian Bern
        {
            get { return new PrimeMeridian(7.26225, CoordinateSystems.AngularUnit.Degrees, "Bern", "EPSG", 8907, String.Empty, String.Empty, "1895 value. Newer value of 7 deg 26 min 22.335 sec E determined in 1938."); }
        }

        /// <summary>
        /// Gets the Jakarta prime meridian.
        /// </summary>
        public static PrimeMeridian Jakarta
        {
            get { return new PrimeMeridian(106.482779, CoordinateSystems.AngularUnit.Degrees, "Jakarta", "EPSG", 8908, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Brussels prime meridian.
        /// </summary>
        public static PrimeMeridian Brussels
        {
            get { return new PrimeMeridian(4.220471, CoordinateSystems.AngularUnit.Degrees, "Brussels", "EPSG", 8910, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Stockholm prime meridian.
        /// </summary>
        public static PrimeMeridian Stockholm
        {
            get { return new PrimeMeridian(18.03298, CoordinateSystems.AngularUnit.Degrees, "Stockholm", "EPSG", 8911, String.Empty, String.Empty, String.Empty); }
        }

        /// <summary>
        /// Gets the Athens prime meridian.
        /// </summary>
        public static PrimeMeridian Athens
        {
            get { return new PrimeMeridian(23.4258815, CoordinateSystems.AngularUnit.Degrees, "Athens", "EPSG", 8912, String.Empty, String.Empty, "Used in Greece for older mapping based on Hatt projection."); }
        }

        /// <summary>
        /// Gets the Oslo prime meridian.
        /// </summary>
        public static PrimeMeridian Oslo
        {
            get { return new PrimeMeridian(10.43225, CoordinateSystems.AngularUnit.Degrees, "Oslo", "EPSG", 8913, String.Empty, String.Empty, "Formerly known as Kristiania or Christiania."); }
        }

        #endregion

        #region IPrimeMeridian Members

        /// <summary>
        /// Gets or sets the longitude of the prime meridian (relative to the Greenwich prime meridian).
        /// </summary>
        public double Longitude
        {
            get { return _longitude; }
            set { _longitude = value; }
        }

        /// <summary>
        /// Gets or sets the AngularUnits.
        /// </summary>
        public IAngularUnit AngularUnit
        {
            get { return _angularUnit; }
            set { _angularUnit = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "PRIMEM[\"{0}\", {1}", Name, Longitude);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_PrimeMeridian Longitude=\"{0}\" >{1}{2}</CS_PrimeMeridian>", Longitude, InfoXml, AngularUnit.XML);
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is PrimeMeridian))
                return false;
            PrimeMeridian prime = obj as PrimeMeridian;
            return prime.AngularUnit.EqualParams(this.AngularUnit) && prime.Longitude == this.Longitude;
        }

        #endregion
    }

    /// <summary>
    /// Provides access to members of the horizontal datum object.
    /// Used to measure positions on the surface of the Earth.
    /// </summary>
    public interface IHorizontalDatum : IDatum
    {
        /// <summary>
        /// Gets or sets the ellipsoid of the datum.
        /// </summary>
        IEllipsoid Ellipsoid { get; set; }

        /// <summary>
        /// Gets preferred parameters for a Bursa Wolf transformation into WGS84. 
        /// The 7 returned values correspond to (dx,dy,dz) in meters, 
        /// (ex,ey,ez) in arc-seconds, and scaling in parts-per-million.
        /// </summary>
        Wgs84ConversionInfo Wgs84Parameters { get; set; }
    }

    /// <summary>
    /// Provides access to members of the vertival datum object.
    /// Used to measure vertical distances.
    /// </summary>
    public interface IVerticalDatum : IDatum { }

    /// <summary>
    /// Provides access to members of the local datum object.
    /// If two local datum objects have the same datum type and name,
    /// then they can be considered equal. This means that coordinates can be
    /// transformed between two different local coordinate systems, as long as
    /// they are based on the same local datum.
    /// </summary>
    public interface ILocalDatum : IDatum { }

    /// <summary>
    /// Types of datums.
    /// </summary>
    public enum DatumType : int
    {
        /// <summary>
        /// Lowest possible value for horizontal datum types
        /// </summary>
        HD_Min = 1000,

        /// <summary>
        /// Unspecified horizontal datum type. Horizontal datums with this type should never
        /// supply a conversion to WGS84 using Bursa Wolf parameters.
        /// </summary>
        HD_Other = 1000,

        /// <summary>
        /// These datums, such as ED50, NAD27 and NAD83, have been designed to support
        /// horizontal positions on the ellipsoid as opposed to positions in 3-D space. 
        /// These datums were designed mainly to support a horizontal component of 
        /// a position in a domain of limited extent, such as a country, a region 
        /// or a continent.
        /// </summary>
        HD_Classic = 1001,

        /// <summary>
        /// A geocentric datum is a "satellite age" modern geodetic datum mainly of global
        /// extent, such as WGS84 (used in GPS), PZ90 (used in GLONASS) and ITRF. These
        /// datums were designed to support both a horizontal component of position and
        /// a vertical component of position (through ellipsoidal heights). The regional
        /// realizations of ITRF, such as ETRF, are also included in this category.
        /// </summary>
        HD_Geocentric = 1002,

        /// <summary>
        /// Highest possible value for horizontal datum types.
        /// </summary>
        HD_Max = 1999,

        /// <summary>
        /// Lowest possible value for vertical datum types.
        /// </summary>
        VD_Min = 2000,

        /// <summary>
        /// Unspecified vertical datum type.
        /// </summary>
        VD_Other = 2000,

        /// <summary>
        /// A vertical datum for orthometric heights that are measured along the plumb line.
        /// </summary>
        VD_Orthometric = 2001,

        /// <summary>
        /// A vertical datum for ellipsoidal heights that are measured along the normal to
        /// the ellipsoid used in the definition of horizontal datum.
        /// </summary>
        VD_Ellipsoidal = 2002,

        /// <summary>
        /// The vertical datum of altitudes or heights in the atmosphere. These are
        /// approximations of orthometric heights obtained with the help of a barometer or
        /// a barometric altimeter. These values are usually expressed in one of the
        /// following units: meters, feet, millibars (used to measure pressure levels), or
        /// theta value (units used to measure geopotential height).
        /// </summary>
        VD_AltitudeBarometric = 2003,

        /// <summary>
        /// A normal height system.
        /// </summary>
        VD_Normal = 2004,

        /// <summary>
        /// A vertical datum of geoid model derived heights, also called GPS-derived heights.
        /// These heights are approximations of orthometric heights (H), constructed from the
        /// ellipsoidal heights (h) by the use of the given geoid undulation model (N)
        /// through the equation: H=h-N.
        /// </summary>
        VD_GeoidModelDerived = 2005,

        /// <summary>
        /// This attribute is used to support the set of datums generated for hydrographic
        /// engineering projects where depth measurements below sea level are needed. It is
        /// often called a hydrographic or a marine datum. Depths are measured in the
        /// direction perpendicular (approximately) to the actual equipotential surfaces of
        /// the earth's gravity field, using such procedures as echo-sounding.
        /// </summary>
        VD_Depth = 2006,

        /// <summary>
        /// Highest possible value for vertical datum types.
        /// </summary>
        VD_Max = 2999,

        /// <summary>
        /// Lowest possible value for vertical datum types.
        /// </summary>
        LD_Min = 10000,

        /// <summary>
        /// Highest possible value for local datum types.
        /// </summary>
        LD_Max = 32767
    }


    /// <summary>
    /// Represents a datum. Datum is a set of quantities from which other 
    /// quantities are calculated.
    /// </summary>
    /// <remarks>
    /// For the OGC abstract model, it can be defined as a set of real points on the earth
    /// that have coordinates. EG. A datum can be thought of as a set of parameters
    /// defining completely the origin and orientation of a coordinate system with respect
    /// to the earth. A textual description and/or a set of parameters describing the
    /// relationship of a coordinate system to some predefined physical locations (such
    /// as center of mass) and physical directions (such as axis of spin). The definition
    /// of the datum may also include the temporal behavior (such as the rate of change of
    /// the orientation of the coordinate axes).
    /// </remarks>
    public abstract class Datum : SpatialReferenceInfo, IDatum
    {
        private DatumType _datumType;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Datum.
        /// </summary>
        /// <param name="type">Datum type</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal Datum(DatumType type,
            string name, string authority, long code, string alias,
            string remarks, string abbreviation)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            _datumType = type;
        }

        #region IDatum Members

        /// <summary>
        /// Gets or sets the type of the datum.
        /// </summary>
        public DatumType DatumType
        {
            get { return _datumType; }
            set { _datumType = value; }
        }

        #endregion

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is Ellipsoid))
                return false;
            return (obj as Datum).DatumType == this.DatumType;
        }
    }

    /// <summary>
    /// Represents a horizontal datum.
    /// </summary>
    public class HorizontalDatum : Datum, IHorizontalDatum
    {
        private IEllipsoid _ellipsoid;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.HorizontalDatum.
        /// </summary>
        /// <param name="ellipsoid">Ellipsoid</param>
        /// <param name="toWgs84">Parameters for a Bursa Wolf transformation into WGS84</param>
        /// <param name="type">Datum type</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal HorizontalDatum(
            IEllipsoid ellipsoid, Wgs84ConversionInfo toWgs84, DatumType type,
            string name, string authority, long code, string alias, string remarks, string abbreviation)
            : base(type, name, authority, code, alias, remarks, abbreviation)
        {
            _ellipsoid = ellipsoid;
            _wgs84ConversionInfo = toWgs84;
        }

        #region Predefined datums

        /// <summary>
        /// EPSG WGS84
        /// </summary>
        public static HorizontalDatum WGS84
        {
            get
            {
                return new HorizontalDatum(CoordinateSystems.Ellipsoid.WGS84,
                    null, DatumType.HD_Geocentric, "World Geodetic System 1984", "EPSG", 6326, String.Empty,
                    "EPSG's WGS 84 datum has been the then current realisation. No distinction is made between the original WGS 84 frame, WGS 84 (G730), WGS 84 (G873) and WGS 84 (G1150). Since 1997, WGS 84 has been maintained within 10cm of the then current ITRF.", String.Empty);
            }
        }

        /// <summary>
        /// World Geodetic System 1972
        /// </summary>
        /// <remarks>
        /// <para>Was used by GPS NAVSTAR up to the 1987</para>
        /// </remarks>
        public static HorizontalDatum WGS72
        {
            get
            {
                HorizontalDatum datum =
                    new HorizontalDatum(CoordinateSystems.Ellipsoid.WGS72,
                    null, DatumType.HD_Geocentric, "World Geodetic System 1972", "EPSG", 6322, String.Empty,
                    "Used by GPS before 1987. For Transit satellite positioning see also WGS 72BE. Datum code 6323 reserved for southern hemisphere ProjCS's.", String.Empty);
                datum.Wgs84Parameters = new Wgs84ConversionInfo(0, 0, 4.5, 0, 0, 0.554, 0.219);
                return datum;
            }
        }


        /// <summary>
        /// Gets the European Terrestrial Reference System 1989.
        /// </summary>
        /// <remarks>
        /// <para>Area of use:
        /// Europe: Albania; Andorra; Austria; Belgium; Bosnia and Herzegovina; Bulgaria; Croatia;
        /// Cyprus; Czech Republic; Denmark; Estonia; Finland; Faroe Islands; France; Germany; Greece;
        /// Hungary; Ireland; Italy; Latvia; Liechtenstein; Lithuania; Luxembourg; Malta; Netherlands;
        /// Norway; Poland; Portugal; Romania; San Marino; Serbia and Montenegro; Slovakia; Slovenia;
        /// Spain; Svalbard; Sweden; Switzerland; United Kingdom (UK) including Channel Islands and
        /// Isle of Man; Vatican City State.</para>
        /// <para>Origin description: Fixed to the stable part of the Eurasian continental
        /// plate and consistent with ITRS at the epoch 1989.0.</para>
        /// </remarks>
        public static HorizontalDatum ETRF89
        {
            get
            {
                HorizontalDatum datum = new HorizontalDatum(CoordinateSystems.Ellipsoid.GRS80, null, DatumType.HD_Geocentric,
                    "European Terrestrial Reference System 1989", "EPSG", 6258, "ETRF89", "The distinction in usage between ETRF89 and ETRS89 is confused: although in principle conceptually different in practice both are used for the realisation.", String.Empty);
                datum.Wgs84Parameters = new Wgs84ConversionInfo();
                return datum;
            }
        }

        /// <summary>
        /// Gets the European Datum 1950.
        /// </summary>
        /// <remarks>
        /// <para>Area of use:
        /// Europe - west - Denmark; Faroe Islands; France offshore; Israel offshore; Italy including San
        /// Marino and Vatican City State; Ireland offshore; Netherlands offshore; Germany; Greece (offshore);
        /// North Sea; Norway; Spain; Svalbard; Turkey; United Kingdom UKCS offshore. Egypt - Western Desert.
        /// </para>
        /// <para>Origin description: Fundamental point: Potsdam (Helmert Tower).
        /// Latitude: 52 deg 22 min 51.4456 sec N; Longitude: 13 deg  3 min 58.9283 sec E (of Greenwich).</para>
        /// </remarks>
        public static HorizontalDatum ED50
        {
            get
            {
                return new HorizontalDatum(CoordinateSystems.Ellipsoid.International1924, new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0), DatumType.HD_Geocentric,
                "European Datum 1950", "EPSG", 6230, "ED50", String.Empty, String.Empty);
            }
        }

        #endregion

        #region IHorizontalDatum Members

        /// <summary>
        /// Gets or sets the ellipsoid of the datum
        /// </summary>
        public IEllipsoid Ellipsoid
        {
            get { return _ellipsoid; }
            set { _ellipsoid = value; }
        }

        private Wgs84ConversionInfo _wgs84ConversionInfo;

        /// <summary>
        /// Gets preferred parameters for a Bursa Wolf transformation into WGS84
        /// </summary>
        public Wgs84ConversionInfo Wgs84Parameters
        {
            get { return _wgs84ConversionInfo; }
            set { _wgs84ConversionInfo = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("DATUM[\"{0}\", {1}", Name, _ellipsoid.WKT);
                if (_wgs84ConversionInfo != null)
                    sb.AppendFormat(", {0}", _wgs84ConversionInfo.WKT);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_HorizontalDatum DatumType=\"{0}\">{1}{2}{3}</CS_HorizontalDatum>",
                    (int)DatumType, InfoXml, Ellipsoid.XML, (Wgs84Parameters == null ? String.Empty : Wgs84Parameters.XML));
            }
        }

        #endregion

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is HorizontalDatum))
                return false;
            HorizontalDatum datum = obj as HorizontalDatum;
            if (datum.Wgs84Parameters == null && this.Wgs84Parameters != null) return false;
            if (datum.Wgs84Parameters != null && !datum.Wgs84Parameters.Equals(this.Wgs84Parameters))
                return false;
            return (datum != null && this.Ellipsoid != null &&
                datum.Ellipsoid.EqualParams(this.Ellipsoid) || datum == null && this.Ellipsoid == null) && this.DatumType == datum.DatumType;
        }
    }

    /// <summary>
    /// The IInfo interface defines the standard
    /// information stored with spatial reference objects. This
    /// interface is reused for many of the spatial reference
    /// objects in the system.
    /// </summary>
    public interface IInfo
    {
        /// <summary>
        /// Gets or sets the name of the object.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the authority name for this object, e.g., "POSC",
        /// is this is a standard object with an authority specific
        /// identity code. Returns "CUSTOM" if this is a custom object.
        /// </summary>
        string Authority { get; }

        /// <summary>
        /// Gets or sets the authority specific identification code of the object
        /// </summary>
        long AuthorityCode { get; }

        /// <summary>
        /// Gets or sets the alias of the object.
        /// </summary>
        string Alias { get; }

        /// <summary>
        /// Gets or sets the abbreviation of the object.
        /// </summary>
        string Abbreviation { get; }

        /// <summary>
        /// Gets or sets the provider-supplied remarks for the object.
        /// </summary>
        string Remarks { get; }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        string WKT { get; }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        string XML { get; }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        bool EqualParams(object obj);
    }

    /// <summary>
    /// Implements the IInfo interface.
    /// Defines the standard information stored with spatial reference objects.
    /// </summary>
    public abstract class SpatialReferenceInfo : IInfo
    {
        private string _name;
        private string _authority;
        private long _code;
        private string _alias;
        private string _abbreviation;
        private string _remarks;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Info.
        /// </summary>
        /// <remarks>
        /// <para>The metadata items "Abbreviation", "Alias", "Authority", "AuthorityCode", "Name" and 
        /// "Remarks" were specified in the Simple Features interfaces, so they have been kept 
        /// here.</para>
        /// <para>This specification does not dictate what the contents of these items
        /// should be. However, the following guidelines are suggested:</para>
        /// <para>When <see cref="ICoordinateSystemAuthorityFactory" /> is used to create an object, 
        /// the "Authority" and "AuthorityCode" values should be set to the authority name of the factory 
        /// object, and the authority code supplied by the client, respectively. The other values may or 
        /// may not be set. (If the authority is EPSG, the implementer may consider using the corresponding 
        /// metadata values in the EPSG tables.)</para>
        /// <para>When <see cref="CoordinateSystemFactory" /> creates an object, the "Name" should be set 
        /// to the value supplied by the client. All of the other metadata items should be left empty
        /// </para>
        /// </remarks>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal SpatialReferenceInfo(
                        string name,
                        string authority,
                        long code,
                        string alias,
                        string abbreviation,
                        string remarks)
        {
            _name = name;
            _authority = authority;
            _code = code;
            _alias = alias;
            _abbreviation = abbreviation;
            _remarks = remarks;
        }

        #region ISpatialReferenceInfo Members

        /// <summary>
        /// Gets or sets the name of the object.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the authority name for this object, e.g., "EPSG",
        /// is this is a standard object with an authority specific
        /// identity code. Returns "CUSTOM" if this is a custom object.
        /// </summary>
        public string Authority
        {
            get { return _authority; }
            set { _authority = value; }
        }

        /// <summary>
        /// Gets or sets the authority specific identification code of the object
        /// </summary>
        public long AuthorityCode
        {
            get { return _code; }
            set { _code = value; }
        }

        /// <summary>
        /// Gets or sets the alias of the object.
        /// </summary>
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }

        /// <summary>
        /// Gets or sets the abbreviation of the object.
        /// </summary>
        public string Abbreviation
        {
            get { return _abbreviation; }
            set { _abbreviation = value; }
        }

        /// <summary>
        /// Gets or sets the provider-supplied remarks for the object.
        /// </summary>
        public string Remarks
        {
            get { return _remarks; }
            set { _remarks = value; }
        }

        /// <summary>
        /// Returns the well-known of this object.
        /// </summary>
        public override string ToString()
        {
            return WKT;
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public abstract string WKT { get; }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public abstract string XML { get; }

        /// <summary>
        /// Returns an XML string of the info object
        /// </summary>
        internal string InfoXml
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("<CS_Info");
                if (AuthorityCode > 0) sb.AppendFormat(" AuthorityCode=\"{0}\"", AuthorityCode);
                if (!String.IsNullOrEmpty(Abbreviation)) sb.AppendFormat(" Abbreviation=\"{0}\"", Abbreviation);
                if (!String.IsNullOrEmpty(Authority)) sb.AppendFormat(" Authority=\"{0}\"", Authority);
                if (!String.IsNullOrEmpty(Name)) sb.AppendFormat(" Name=\"{0}\"", Name);
                sb.Append("/>");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public abstract bool EqualParams(object obj);

        #endregion
    }

    /// <summary>
    /// Possible oriantetions of axis. Some coordinate systems use non-standard orientations.
    /// For example, the first axis in South African grids usually points West,
    /// instead of East. This information is obviously relevant for algorithms
    /// converting South African grid coordinates into Lat/Long.
    /// </summary>
    public enum AxisOrientationEnum : short
    {
        /// <summary>
        /// Unknown or unspecified axis orientation. This can be used for local or fitted coordinate systems.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Increasing ordinates values go North. This is usually used for Grid Y coordinates and Latitude.
        /// </summary>
        North = 1,

        /// <summary>
        /// Increasing ordinates values go South. This is rarely used.
        /// </summary>
        South = 2,

        /// <summary>
        /// Increasing ordinates values go East. This is rarely used.
        /// </summary>
        East = 3,

        /// <summary>
        /// Increasing ordinates values go West. This is usually used for Grid X coordinates and Longitude.
        /// </summary>
        West = 4,

        /// <summary>
        /// Increasing ordinates values go up. This is used for vertical coordinate systems.
        /// </summary>
        Up = 5,

        /// <summary>
        /// Increasing ordinates values go down. This is used for vertical coordinate systems.
        /// </summary>
        Down = 6
    }

    /// <summary>
    /// Represents the coordinate axis information.
    /// This is used to label axes, and indicate the orientation.
    /// </summary>
    public class AxisInfo
    {
        private string _Name;
        private AxisOrientationEnum _orientation;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.AxisInfo.
        /// </summary>
        /// <param name="name">Name of axis</param>
        /// <param name="orientation">Axis orientation</param>
        public AxisInfo(string name, AxisOrientationEnum orientation)
        {
            _Name = name;
            _orientation = orientation;
        }

        /// <summary>
        /// Gets or sets a human readable name for axis. 
        /// Possible values are X, Y, Long, Lat or any other short string.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// Gets or sets the orientation of this axis.
        /// </summary>
        public AxisOrientationEnum Orientation
        {
            get { return _orientation; }
            set { _orientation = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public string WKT
        {
            get
            {
                return String.Format("AXIS[\"{0}\", {1}]", Name, Orientation.ToString().ToUpper(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_AxisInfo Name=\"{0}\" Orientation=\"{1}\"/>", Name, Orientation.ToString().ToUpper(CultureInfo.InvariantCulture));
            }
        }
    }

    /// <summary>
    /// Represents an object that creates spatial reference objects 
    /// using codes.
    /// </summary>
    /// <remarks>
    /// The codes are maintained by an external authority. A commonly used authority 
    /// is EPSG, which is also used in the GeoTIFF standard.
    /// </remarks>
    public interface ICoordinateSystemAuthorityFactory
    {
        /// <summary>
        /// Gets the authority name for this factory (e.g., "EPSG" or "POSC").
        /// </summary>
        string Authority { get; }

        /// <summary>
        /// Returns a projected coordinate system object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The projected coordinate system object with the given code</returns>
        IProjectedCoordinateSystem CreateProjectedCoordinateSystem(long code);

        /// <summary>
        /// Returns a geographic coordinate system object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The geographic coordinate system object with the given code</returns>
        IGeographicCoordinateSystem CreateGeographicCoordinateSystem(long code);

        /// <summary>
        /// Returns a horizontal datum object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The horizontal datum object with the given code</returns>
        IHorizontalDatum CreateHorizontalDatum(long code);

        /// <summary>
        /// Returns an ellipsoid object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The ellipsoid object with the given code</returns>
        IEllipsoid CreateEllipsoid(long code);

        /// <summary>
        /// Returns a prime meridian object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The prime meridian object with the given code</returns>
        IPrimeMeridian CreatePrimeMeridian(long code);

        /// <summary>
        /// Returns a linear unit object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The linear unit object with the given code</returns>
        ILinearUnit CreateLinearUnit(long code);

        /// <summary>
        /// Returns an <see cref="IAngularUnit">angular unit</see> object corresponding to the given code.
        /// </summary>
        /// <param name="code">The identification code</param>
        /// <returns>The angular unit object for the given code</returns>
        IAngularUnit CreateAngularUnit(long code);

        /// <summary>
        /// Creates a <see cref="IVerticalDatum" /> from a code.
        /// </summary>
        /// <param name="code">Authority code</param>
        /// <returns>Vertical datum for the given code</returns>
        IVerticalDatum CreateVerticalDatum(long code);

        /// <summary>
        /// Create a <see cref="IVerticalCoordinateSystem">vertical coordinate system</see> from a code.
        /// </summary>
        /// <param name="code">Authority code</param>
        /// <returns></returns>
        IVerticalCoordinateSystem CreateVerticalCoordinateSystem(long code);

        /// <summary>
        /// Creates a 3D coordinate system from a code.
        /// </summary>
        /// <param name="code">Authority code</param>
        /// <returns>Compound coordinate system for the given code</returns>
        ICompoundCoordinateSystem CreateCompoundCoordinateSystem(long code);

        /// <summary>
        /// Creates a <see cref="IHorizontalCoordinateSystem">horizontal co-ordinate system</see> from a code.
        /// The horizontal coordinate system could be geographic or projected.
        /// </summary>
        /// <param name="code">Authority code</param>
        /// <returns>Horizontal coordinate system for the given code</returns>
        IHorizontalCoordinateSystem CreateHorizontalCoordinateSystem(long code);

        /// <summary>
        /// Gets a description of the object corresponding to a code.
        /// </summary>
        string DescriptionText { get; }

        /// <summary>
        /// Gets the Geoid code from a WKT name.
        /// </summary>
        /// <remarks>
        /// In the OGC definition of WKT horizontal datums, the geoid is referenced
        /// by a quoted string, which is used as a key value. This method converts
        /// the key value string into a code recognized by this authority.
        /// </remarks>
        /// <param name="wkt"></param>
        /// <returns>A string representing Geoid</returns>
        string GeoidFromWktName(string wkt);

        /// <summary>
        /// Gets the WKT name of a Geoid.
        /// </summary>
        /// <remarks>
        /// In the OGC definition of WKT horizontal datums, the geoid is referenced by
        /// a quoted string, which is used as a key value. This method gets the OGC WKT
        /// key value from a geoid code.
        /// </remarks>
        /// <param name="geoid">A string representing Geoid</param>
        /// <returns>the WKT name of a Geoid</returns>
        string WktGeoidName(string geoid);
    }

    /// <summary>
    /// Builds up complex coordinate systym objects 
    /// from simpler objects or values.
    /// </summary>
    /// <remarks>
    /// <para>ICoordinateSystemFactory allows applications to make coordinate systems that
    /// cannot be created by a <see cref="ICoordinateSystemAuthorityFactory" />. This factory is very
    /// flexible, whereas the authority factory is easier to use.</para>
    /// <para>So <see cref="ICoordinateSystemAuthorityFactory" />can be used to make 'standard' coordinate
    /// systems, and <see cref="CoordinateSystemFactory" /> can be used to make 'special'
    /// coordinate systems.</para>
    /// <para>For example, the EPSG authority has codes for USA state plane coordinate systems
    /// using the NAD83 datum, but these coordinate systems always use meters. EPSG does not
    /// have codes for NAD83 state plane coordinate systems that use feet units. This factory
    /// lets an application create such a hybrid coordinate system.</para>
    /// </remarks>
    public interface ICoordinateSystemFactory
    {
        /// <summary>
        /// Creates a <see cref="ICompoundCoordinateSystem" />.
        /// </summary>
        /// <param name="name">Name of compound coordinate system.</param>
        /// <param name="head">Head coordinate system</param>
        /// <param name="tail">Tail coordinate system</param>
        /// <returns>Compound coordinate system</returns>
        ICompoundCoordinateSystem CreateCompoundCoordinateSystem(string name, ICoordinateSystem head, ICoordinateSystem tail);

        /// <summary>
        /// Creates an <see cref="IEllipsoid" /> from radius values.
        /// </summary>
        /// <seealso cref="M:Topology.CoordinateSystems.ICoordinateSystemFactory.CreateFlattenedSphere(System.String,System.Double,System.Double,Topology.CoordinateSystems.ILinearUnit)" />
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="semiMinorAxis"></param>
        /// <param name="linearUnit"></param>
        /// <returns>Ellipsoid</returns>
        IEllipsoid CreateEllipsoid(string name, double semiMajorAxis, double semiMinorAxis, ILinearUnit linearUnit);

        /// <summary>
        /// Creates a <see cref="IFittedCoordinateSystem" />.
        /// </summary>
        /// <remarks>The units of the axes in the fitted coordinate system will be
        /// inferred from the units of the base coordinate system. If the affine map
        /// performs a rotation, then any mixed axes must have identical units. For
        /// example, a (lat_deg,lon_deg,height_feet) system can be rotated in the
        /// (lat,lon) plane, since both affected axes are in degrees. But you
        /// should not rotate this coordinate system in any other plane.</remarks>
        /// <param name="name">Name of coordinate system</param>
        /// <param name="baseCoordinateSystem">Base coordinate system</param>
        /// <param name="toBaseWkt"></param>
        /// <param name="arAxes"></param>
        /// <returns>Fitted coordinate system</returns>
        IFittedCoordinateSystem CreateFittedCoordinateSystem(string name, ICoordinateSystem baseCoordinateSystem, string toBaseWkt, List<AxisInfo> arAxes);

        /// <summary>
        /// Creates an <see cref="IEllipsoid" /> from an major radius, and inverse flattening.
        /// </summary>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis">Semi major-axis</param>
        /// <param name="inverseFlattening">Inverse flattening</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <returns>Ellipsoid</returns>
        IEllipsoid CreateFlattenedSphere(string name, double semiMajorAxis, double inverseFlattening, ILinearUnit linearUnit);

        /// <summary>
        /// Creates a coordinate system object from an XML string.
        /// </summary>
        /// <param name="xml">XML representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        ICoordinateSystem CreateFromXml(string xml);

        /// <summary>
        /// Creates a spatial reference object given its Well-known text representation.
        /// The output object may be either a <see cref="IGeographicCoordinateSystem" /> or
        /// a <see cref="IProjectedCoordinateSystem" />.
        /// </summary>
        /// <param name="WKT">The Well-known text representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        ICoordinateSystem CreateFromWkt(string WKT);

        /// <summary>
        /// Creates a <see cref="IGeographicCoordinateSystem" />, which could be Lat/Lon or Lon/Lat.
        /// </summary>
        /// <param name="name">Name of geographical coordinate system</param>
        /// <param name="angularUnit">Angular units</param>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <param name="axis0">First axis</param>
        /// <param name="axis1">Second axis</param>
        /// <returns>Geographic coordinate system</returns>
        IGeographicCoordinateSystem CreateGeographicCoordinateSystem(string name, IAngularUnit angularUnit, IHorizontalDatum datum, IPrimeMeridian primeMeridian, AxisInfo axis0, AxisInfo axis1);

        /// <summary>
        /// Creates <see cref="IHorizontalDatum" /> from ellipsoid and Bursa-World parameters.
        /// </summary>
        /// <remarks>
        /// Since this method contains a set of Bursa-Wolf parameters, the created
        /// datum will always have a relationship to WGS84. If you wish to create a
        /// horizontal datum that has no relationship with WGS84, then you can
        /// either specify a <see cref="DatumType">horizontalDatumType</see> 
        /// of <see cref="CoordinateSystems.DatumType.HD_Other" />, 
        /// or create it via WKT.
        /// </remarks>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="datumType">Type of datum</param>
        /// <param name="ellipsoid">Ellipsoid</param>
        /// <param name="toWgs84">Wgs84 conversion parameters</param>
        /// <returns>Horizontal datum</returns>
        IHorizontalDatum CreateHorizontalDatum(string name, DatumType datumType, IEllipsoid ellipsoid, Wgs84ConversionInfo toWgs84);

        /// <summary>
        /// Creates a local coordinate system.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="datum">Local datum</param>
        /// <param name="unit"> Unit of measurement </param>
        /// <param name="axes">The coordinate axes</param>
        /// <returns>Local coordinate system</returns>
        ILocalCoordinateSystem CreateLocalCoordinateSystem(string name, ILocalDatum datum, IUnit unit, List<AxisInfo> axes);

        /// <summary>
        /// Creates a <see cref="T:MapAround.CoordinateSystems.ILocalDatum" />.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Datum type</param>
        /// <returns></returns>
        ILocalDatum CreateLocalDatum(string name, DatumType datumType);

        /// <summary>
        /// Creates a <see cref="IPrimeMeridian" />, relative to Greenwich.
        /// </summary>
        /// <param name="name">Name of prime meridian</param>
        /// <param name="angularUnit">Angular unit</param>
        /// <param name="longitude">Longitude</param>
        /// <returns>Prime meridian</returns>
        IPrimeMeridian CreatePrimeMeridian(string name, IAngularUnit angularUnit, double longitude);

        /// <summary>
        /// Creates a <see cref="IProjectedCoordinateSystem" /> using a projection object.
        /// </summary>
        /// <param name="name">Name of projected coordinate system</param>
        /// <param name="gcs">Geographic coordinate system</param>
        /// <param name="projection">Projection</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="axis0">Primary axis</param>
        /// <param name="axis1">Secondary axis</param>
        /// <returns>Projected coordinate system</returns>
        IProjectedCoordinateSystem CreateProjectedCoordinateSystem(string name, IGeographicCoordinateSystem gcs, IProjection projection, ILinearUnit linearUnit, AxisInfo axis0, AxisInfo axis1);

        /// <summary>
        /// Creates a <see cref="IProjection" />.
        /// </summary>
        /// <param name="name">Name of projection</param>
        /// <param name="wktProjectionClass">Projection class</param>
        /// <param name="Parameters">Projection parameters</param>
        /// <returns>Projection</returns>
        IProjection CreateProjection(string name, string wktProjectionClass, List<ProjectionParameter> Parameters);

        /// <summary>
        /// Creates a vertical coordinate system.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="datum">Vertical Datum</param>
        /// <param name="verticalUnit">The unit of measurement of height (depth, etc.)</param>
        /// <param name="axis">Coordinate axis</param>
        /// <returns>The vertical coordinate</returns>
        IVerticalCoordinateSystem CreateVerticalCoordinateSystem(string name, IVerticalDatum datum, ILinearUnit verticalUnit, AxisInfo axis);

        /// <summary>
        /// Creates a <see cref="IVerticalDatum" /> from an enumerated type value.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Type of datum</param>
        /// <returns>Vertical datum</returns>
        IVerticalDatum CreateVerticalDatum(string name, DatumType datumType);
    }

    /// <summary>
    /// Base interface for all coordinate systems
    /// </summary>
    /// <remarks>
    /// <para>A coordinate system is a mathematical space, where the elements of the space are called
    /// positions. Each position is described by a list of numbers. The length of the list corresponds
    /// to the dimension of the coordinate system. So in a 2D coordinate system each position is
    /// described by a list containing 2 numbers.</para>
    /// <para>
    /// However, in a coordinate system, not all lists of numbers correspond to a position -
    /// some lists may be outside the domain of the coordinate system. For example, in a 2D Lat/Lon
    /// coordinate system, the list (91,91) does not correspond to a position.</para>
    /// <para>
    /// Some coordinate systems also have a mapping from the mathematical space into locations
    /// in the real world. So in a Lat/Lon coordinate system, the mathematical position (lat, long)
    /// corresponds to a location on the surface of the Earth. This mapping from the mathematical
    /// space into real-world locations is called a Datum.</para>
    /// </remarks>
    public interface ICoordinateSystem : IInfo
    {
        /// <summary>
        /// Gets a dimension of the coordinate system.
        /// </summary>
        int Dimension { get; }

        /// <summary>
        /// Gets an axis details for dimension within coordinate system.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Axis info</returns>
        AxisInfo GetAxis(int dimension);

        /// <summary>
        /// Returns units for dimension within coordinate system.
        /// </summary>
        /// <returns>Units for dimension within coordinate system</returns>
        IUnit GetUnits(int dimension);

        /// <summary>
        /// Gets default envelope of coordinate system.
        /// </summary>
        /// <remarks>
        /// Gets default envelope of coordinate system. Coordinate systems
        /// which are bounded should return the minimum bounding box of their
        /// domain. Unbounded coordinate systems should return a box which is
        /// as large as is likely to be used. For example, a (lon,lat)
        /// geographic coordinate system in degrees should return a box from
        /// (-180,-90) to (180,90), and a geocentric coordinate system could
        /// return a box from (-r,-r,-r) to (+r,+r,+r) where r is the
        /// approximate radius of the Earth.
        /// </remarks>
        double[] DefaultEnvelope { get; }
    }

    /// <summary>
    /// Provides access to methods of a 2D coordinate system 
    /// suitable for positions on the Earth's surface.
    /// </summary>
    public interface IHorizontalCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets or sets the HorizontalDatum.
        /// </summary>
        IHorizontalDatum HorizontalDatum { get; set; }
    }

    /// <summary>
    /// Provides access to methods of a local coordinate system, 
    /// with uncertain relationship to the world.
    /// </summary>
    /// <remarks>In general, a local coordinate system cannot be related to other coordinate
    /// systems. However, if two objects supporting this interface have the same dimension,
    /// axes, units and datum then client code is permitted to assume that the two coordinate
    /// systems are identical. This allows several datasets from a common source (e.g. a CAD
    /// system) to be overlaid. In addition, some implementations of the Coordinate
    /// Transformation (CT) package may have a mechanism for correlating local datums. (E.g.
    /// from a database of transformations, which is created and maintained from real-world
    /// measurements.)
    /// </remarks>
    public interface ILocalCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets or sets the local datum
        /// </summary>
        ILocalDatum LocalDatum { get; set; }
    }

    /// <summary>
    /// The IProjectedCoordinateSystem interface defines the standard information stored with
    /// projected coordinate system objects. A projected coordinate system is defined using a
    /// geographic coordinate system object and a projection object that defines the
    /// coordinate transformation from the geographic coordinate system to the projected
    /// coordinate systems. The instances of a single ProjectedCoordinateSystem COM class can
    /// be used to model different projected coordinate systems (e.g., UTM Zone 10, Albers)
    /// by associating the ProjectedCoordinateSystem instances with Projection instances
    /// belonging to different Projection COM classes (Transverse Mercator and Albers,
    /// respectively).
    /// </summary>
    public interface IProjectedCoordinateSystem : IHorizontalCoordinateSystem
    {
        /// <summary>
        /// Gets or sets the geographic coordinate system associated with the projected
        /// coordinate system.
        /// </summary>
        IGeographicCoordinateSystem GeographicCoordinateSystem { get; set; }


        /// <summary>
        /// Gets or sets the linear (projected) units of the projected coordinate system.
        /// </summary>
        ILinearUnit LinearUnit { get; set; }

        /// <summary>
        /// Gets or sets the projection for the projected coordinate system.
        /// </summary>
        IProjection Projection { get; set; }
    }

    /// <summary>
    /// An aggregate of two coordinate systems (CRS). One of these is usually a
    /// CRS based on a two dimensional coordinate system such as a geographic or
    /// a projected coordinate system with a horizontal datum. The other is a
    /// vertical CRS which is a one-dimensional coordinate system with a vertical
    /// datum.
    /// </summary>
    public interface ICompoundCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets first sub-coordinate system.
        /// </summary>
        ICoordinateSystem HeadCS { get; }

        /// <summary>
        /// Gets second sub-coordinate system.
        /// </summary>
        ICoordinateSystem TailCS { get; }
    }

    /// <summary>
    /// A 3D coordinate system, with its origin at the center of the Earth.
    /// </summary>
    public interface IGeocentricCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets or sets the HorizontalDatum. The horizontal datum is used to determine where
        /// the centre of the Earth is considered to be. All coordinate points will be
        /// measured from the centre of the Earth, and not the surface.
        /// </summary>
        IHorizontalDatum HorizontalDatum { get; set; }

        /// <summary>
        /// Gets or sets the units used along all the axes.
        /// </summary>
        ILinearUnit LinearUnit { get; set; }

        /// <summary>
        /// Gets or sets the PrimeMeridian.
        /// </summary>
        IPrimeMeridian PrimeMeridian { get; set; }
    }

    /// <summary>
    /// The IGeographicCoordinateSystem interface is a subclass of IGeodeticSpatialReference and
    /// defines the standard information stored with geographic coordinate system objects.
    /// </summary>
    public interface IGeographicCoordinateSystem : IHorizontalCoordinateSystem
    {
        /// <summary>
        /// Gets or sets the angular units of the geographic coordinate system.
        /// </summary>
        IAngularUnit AngularUnit { get; set; }

        /// <summary>
        /// Gets or sets the prime meridian.
        /// </summary>
        IPrimeMeridian PrimeMeridian { get; set; }

        /// <summary>
        /// Gets the number of available conversions to WGS84 coordinates.
        /// </summary>
        int NumConversionToWGS84 { get; }

        /// <summary>
        /// Gets details on a conversion to WGS84.
        /// </summary>
        Wgs84ConversionInfo GetWgs84ConversionInfo(int index);
    }

    /// <summary>
    /// The IGeodeticSpatialReference interface defines a root interface for all types of geodetic
    /// spatial references, it is a subclass of ICoordinateSystem.
    /// </summary>
    public interface IGeodeticSpatialReference : ICoordinateSystem { }

    /// <summary>
    /// Defines a coordinate system which sits inside another coordinate system. The fitted
    /// coordinate system can be rotated and shifted, or use any other math transform
    /// to inject itself into the base coordinate system.
    /// </summary>
    public interface IFittedCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets underlying coordinate system.
        /// </summary>
        ICoordinateSystem BaseCoordinateSystem { get; }

        /// <summary>
        /// Returns well-known text of a math transform to the base coordinate system.
        /// The dimension of this fitted coordinate system is determined by the source
        /// dimension of the math transform. The transform should be one-to-one within
        /// this coordinate system's domain, and the base coordinate system dimension
        /// must be at least as big as the dimension of this coordinate system.
        /// </summary>
        /// <returns>Well-known text of a math transform to the base coordinate system</returns>
        string ToBase();
    }

    /// <summary>
    /// A one-dimensional coordinate system suitable for vertical measurements.
    /// </summary>
    public interface IVerticalCoordinateSystem : ICoordinateSystem
    {
        /// <summary>
        /// Gets or sets the vertical datum, which indicates the measurement method
        /// </summary>
        IVerticalDatum VerticalDatum { get; set; }

        /// <summary>
        /// Gets or sets the units used along the vertical axis.
        /// </summary>
        ILinearUnit VerticalUnit { get; set; }
    }

    /// <summary>
    /// Provides access to members of the projection object.
    /// A projection object implements a coordinate transformation 
    /// from a geographic coordinate system to a projected coordinate system, 
    /// given the ellipsoid for the geographic coordinate system. 
    /// </summary>
    public interface IProjection : IInfo
    {
        /// <summary>
        /// Gets a number of parameters.
        /// </summary>
        int NumParameters { get; }

        /// <summary>
        /// Gets a class of the projection. "Mercator" for example.
        /// </summary>
        string ClassName { get; }

        /// <summary>
        /// Gets a parameter by its index.
        /// </summary>
        /// <param name="n">An index of the parameter</param>
        ProjectionParameter GetParameter(int n);

        /// <summary>
        /// Gets a projection parameter by its name.
        /// </summary>
        /// <remarks>The names of parameters are case sensitive.</remarks>
        /// <param name="name">The parameter name</param>
        ProjectionParameter GetParameter(string name);
    }

    /// <summary>
    /// Base class for coordinate system classes.
    /// </summary>
    /// <remarks>
    /// <para>A coordinate system is a mathematical space, where the elements of the space
    /// are called positions. Each position is described by a list of numbers. The length
    /// of the list corresponds to the dimension of the coordinate system. So in a 2D
    /// coordinate system each position is described by a list containing 2 numbers.</para>
    /// <para>However, in a coordinate system, not all lists of numbers correspond to a
    /// position - some lists may be outside the domain of the coordinate system. For
    /// example, in a 2D Lat/Lon coordinate system, the list (91,91) does not correspond
    /// to a position.</para>
    /// <para>Some coordinate systems also have a mapping from the mathematical space into
    /// locations in the real world. So in a Lat/Lon coordinate system, the mathematical
    /// position (lat, long) corresponds to a location on the surface of the Earth. This
    /// mapping from the mathematical space into real-world locations is called a Datum.</para>
    /// </remarks>
	public abstract class CoordinateSystem : SpatialReferenceInfo, ICoordinateSystem
	{
        private List<AxisInfo> _axisInfo;
        private double[] _defaultEnvelope;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.CoordinateSystem.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
		internal CoordinateSystem(string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
			: base (name, authority, authorityCode, alias,abbreviation, remarks) { }

		#region ICoordinateSystem Members

        /// <summary>
        /// Gets a dimension of the coordinate system.
        /// </summary>
		public int Dimension
		{
			get { return _axisInfo.Count; }
		}

        /// <summary>
        /// Returns the units for the dimension within coordinate system.
        /// Each dimension in the coordinate system has corresponding units.
        /// </summary>
		public abstract IUnit GetUnits(int dimension);

		internal List<AxisInfo> AxisInfo
		{
			get { return _axisInfo; }
			set { _axisInfo = value; }
		}


        /// <summary>
        /// Returns axis details for dimension within coordinate system.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Axis info</returns>
		public AxisInfo GetAxis(int dimension)
		{
			if (dimension >= _axisInfo.Count || dimension < 0)
				throw new ArgumentException("AxisInfo not available for dimension " + dimension.ToString(CultureInfo.InvariantCulture));
			return _axisInfo[dimension];
		}

        /// <summary>
        /// Gets or sets a default envelope of the coordinate system.
        /// </summary>
        /// <remarks>
        /// Coordinate systems which are bounded should return the minimum bounding box of their domain.
        /// Unbounded coordinate systems should return a box which is as large as is likely to be used.
        /// For example, a (lon,lat) geographic coordinate system in degrees should return a box from
        /// (-180,-90) to (180,90), and a geocentric coordinate system could return a box from (-r,-r,-r)
        /// to (+r,+r,+r) where r is the approximate radius of the Earth.
        /// </remarks>
        public double[] DefaultEnvelope
		{
			get { return _defaultEnvelope; }
			set { _defaultEnvelope = value; }
		}

		#endregion
	}

    /// <summary>
    /// Builds up complex coordinate system objects from simplier objects or values.
    /// </summary>
    /// <remarks>
    /// <para>ICoordinateSystemFactory allows applications to make coordinate systems that
    /// cannot be created by a <see cref="T:Topology.CoordinateSystems.ICoordinateSystemAuthorityFactory" />. This factory is very
    /// flexible, whereas the authority factory is easier to use.</para>
    /// <para>So <see cref="ICoordinateSystemAuthorityFactory" />can be used to make 'standard' coordinate
    /// systems, and <see cref="CoordinateSystemFactory" /> can be used to make 'special'
    /// coordinate systems.</para>
    /// <para>For example, the EPSG authority has codes for USA state plane coordinate systems
    /// using the NAD83 datum, but these coordinate systems always use meters. EPSG does not
    /// have codes for NAD83 state plane coordinate systems that use feet units. This factory
    /// lets an application create such a hybrid coordinate system.</para>
    /// </remarks>
    public class CoordinateSystemFactory : ICoordinateSystemFactory
    {
        #region ICoordinateSystemFactory Members

        /// <summary>
        /// Creates a coordinate system object from an XML string.
        /// </summary>
        /// <param name="xml">XML representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        public ICoordinateSystem CreateFromXml(string xml)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a spatial reference object given its well-known text representation.
        /// The output object may be either a <see cref="IGeographicCoordinateSystem" /> or
        /// a <see cref="IProjectedCoordinateSystem" />.
        /// </summary>
        /// <param name="WKT">The well-known text representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        public ICoordinateSystem CreateFromWkt(string WKT)
        {
            return CoordinateSystemWktDeserializer.Parse(WKT) as ICoordinateSystem;
        }

        /// <summary>
        /// Creates a <see cref="ICompoundCoordinateSystem" />.
        /// </summary>
        /// <param name="name">Name of compound coordinate system.</param>
        /// <param name="head">Head coordinate system</param>
        /// <param name="tail">Tail coordinate system</param>
        /// <returns>Compound coordinate system</returns>
        public ICompoundCoordinateSystem CreateCompoundCoordinateSystem(string name, ICoordinateSystem head, ICoordinateSystem tail)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a <see cref="IFittedCoordinateSystem" />.
        /// </summary>
        /// <remarks>The units of the axes in the fitted coordinate system will be
        /// inferred from the units of the base coordinate system. If the affine map
        /// performs a rotation, then any mixed axes must have identical units. For
        /// example, a (lat_deg,lon_deg,height_feet) system can be rotated in the
        /// (lat,lon) plane, since both affected axes are in degrees. But you
        /// should not rotate this coordinate system in any other plane.</remarks>
        /// <param name="name">Name of coordinate system</param>
        /// <param name="baseCoordinateSystem">Base coordinate system</param>
        /// <param name="toBaseWkt"></param>
        /// <param name="arAxes"></param>
        /// <returns>Fitted coordinate system</returns>
        public IFittedCoordinateSystem CreateFittedCoordinateSystem(string name, ICoordinateSystem baseCoordinateSystem, string toBaseWkt, List<AxisInfo> arAxes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a <see cref="ILocalCoordinateSystem">local coordinate system</see>.
        /// </summary>
        /// <remarks>
        /// The dimension of the local coordinate system is determined by the size of
        /// the axis array. All the axes will have the same units. If you want to make
        /// a coordinate system with mixed units, then you can make a compound
        /// coordinate system from different local coordinate systems.
        /// </remarks>
        /// <param name="name">Name of local coordinate system</param>
        /// <param name="datum">Local datum</param>
        /// <param name="unit">Units</param>
        /// <param name="axes">Axis info</param>
        /// <returns>Local coordinate system</returns>
        public ILocalCoordinateSystem CreateLocalCoordinateSystem(string name, ILocalDatum datum, IUnit unit, List<AxisInfo> axes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an <see cref="Ellipsoid" /> from radius values.
        /// </summary>
        /// <seealso cref="M:Topology.CoordinateSystems.CoordinateSystemFactory.CreateFlattenedSphere(System.String,System.Double,System.Double,Topology.CoordinateSystems.ILinearUnit)" />
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="semiMinorAxis"></param>
        /// <param name="linearUnit"></param>
        /// <returns>Ellipsoid</returns>
        public IEllipsoid CreateEllipsoid(string name, double semiMajorAxis, double semiMinorAxis, ILinearUnit linearUnit)
        {
            return new Ellipsoid(semiMajorAxis, semiMinorAxis, 1.0, false, linearUnit, name, String.Empty, -1, String.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates an <see cref="T:MapAround.CoordinateSystems.IEllipsoid" /> from an major radius, 
        /// and inverse flattening.
        /// </summary>
        /// <seealso cref="M:Topology.CoordinateSystems.ICoordinateSystemFactory.CreateEllipsoid(System.String,System.Double,System.Double,Topology.CoordinateSystems.ILinearUnit)" />
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis">Semi major-axis</param>
        /// <param name="inverseFlattening">Inverse flattening</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <returns>Ellipsoid</returns>
        public IEllipsoid CreateFlattenedSphere(string name, double semiMajorAxis, double inverseFlattening, ILinearUnit linearUnit)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");

            return new Ellipsoid(semiMajorAxis, -1, inverseFlattening, true, linearUnit, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a <see cref="ProjectedCoordinateSystem" /> using a projection object.
        /// </summary>
        /// <param name="name">Name of projected coordinate system</param>
        /// <param name="gcs">Geographic coordinate system</param>
        /// <param name="projection">Projection</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="axis0">Primary axis</param>
        /// <param name="axis1">Secondary axis</param>
        /// <returns>Projected coordinate system</returns>
        public IProjectedCoordinateSystem CreateProjectedCoordinateSystem(string name, IGeographicCoordinateSystem gcs, IProjection projection, ILinearUnit linearUnit, AxisInfo axis0, AxisInfo axis1)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (gcs == null)
                throw new ArgumentNullException("gcs");
            if (projection == null)
                throw new ArgumentNullException("projection");
            if (linearUnit == null)
                throw new ArgumentNullException("linearUnit");
            List<AxisInfo> info = new List<AxisInfo>(2);
            info.Add(axis0);
            info.Add(axis1);
            return new ProjectedCoordinateSystem(null, gcs, linearUnit, projection, info, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a <see cref="Projection" />.
        /// </summary>
        /// <param name="name">Name of projection</param>
        /// <param name="wktProjectionClass">Projection class</param>
        /// <param name="parameters">Projection parameters</param>
        /// <returns>Projection</returns>
        public IProjection CreateProjection(string name, string wktProjectionClass, List<ProjectionParameter> parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Invalid projection parameters");
            return new Projection(wktProjectionClass, parameters, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates <see cref="HorizontalDatum" /> from ellipsoid and Bursa-World parameters.
        /// </summary>
        /// <remarks>
        /// Since this method contains a set of Bursa-Wolf parameters, the created
        /// datum will always have a relationship to WGS84. If you wish to create a
        /// horizontal datum that has no relationship with WGS84, then you can
        /// either specify a <see cref="DatumType">horizontalDatumType</see> 
        /// of <see cref="DatumType.HD_Other" />, or create it via WKT.
        /// </remarks>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="datumType">Type of datum</param>
        /// <param name="ellipsoid">Ellipsoid</param>
        /// <param name="toWgs84">Wgs84 conversion parameters</param>
        /// <returns>Horizontal datum</returns>
        public IHorizontalDatum CreateHorizontalDatum(string name, DatumType datumType, IEllipsoid ellipsoid, Wgs84ConversionInfo toWgs84)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");
            if (ellipsoid == null)
                throw new ArgumentNullException("ellipsoid");

            return new HorizontalDatum(ellipsoid, toWgs84, datumType, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a <see cref="PrimeMeridian" />, relative to Greenwich.
        /// </summary>
        /// <param name="name">Name of prime meridian</param>
        /// <param name="angularUnit">Angular unit</param>
        /// <param name="longitude">Longitude</param>
        /// <returns>Prime meridian</returns>
        public IPrimeMeridian CreatePrimeMeridian(string name, IAngularUnit angularUnit, double longitude)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");
            return new PrimeMeridian(longitude, angularUnit, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a <see cref="GeographicCoordinateSystem" />, which could be Lat/Lon or Lon/Lat.
        /// </summary>
        /// <param name="name">Name of geographical coordinate system</param>
        /// <param name="angularUnit">Angular units</param>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <param name="axis0">First axis</param>
        /// <param name="axis1">Second axis</param>
        /// <returns>Geographic coordinate system</returns>
        public IGeographicCoordinateSystem CreateGeographicCoordinateSystem(string name, IAngularUnit angularUnit, IHorizontalDatum datum, IPrimeMeridian primeMeridian, AxisInfo axis0, AxisInfo axis1)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");
            List<AxisInfo> info = new List<AxisInfo>(2);
            info.Add(axis0);
            info.Add(axis1);
            return new GeographicCoordinateSystem(angularUnit, datum, primeMeridian, info, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a <see cref="ILocalDatum" />.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Datum type</param>
        /// <returns></returns>
        public ILocalDatum CreateLocalDatum(string name, DatumType datumType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a <see cref="IVerticalDatum" /> from an enumerated type value.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Type of datum</param>
        /// <returns>Vertical datum</returns>
        public IVerticalDatum CreateVerticalDatum(string name, DatumType datumType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a <see cref="IVerticalCoordinateSystem" /> from a 
        /// <see cref="IVerticalDatum">datum</see> and 
        /// <see cref="LinearUnit">linear units</see>.
        /// </summary>
        /// <param name="name">Name of vertical coordinate system</param>
        /// <param name="datum">Vertical datum</param>
        /// <param name="verticalUnit">Unit</param>
        /// <param name="axis">Axis info</param>
        /// <returns>Vertical coordinate system</returns>
        public IVerticalCoordinateSystem CreateVerticalCoordinateSystem(string name, IVerticalDatum datum, ILinearUnit verticalUnit, AxisInfo axis)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Creates a <see cref="M:MapAround.CoordinateSystems.CoordinateSystemFactory.CreateGeocentricCoordinateSystem(System.String,Topology.CoordinateSystems.IHorizontalDatum,Topology.CoordinateSystems.ILinearUnit,Topology.CoordinateSystems.IPrimeMeridian)" /> from a <see cref="T:Topology.CoordinateSystems.IHorizontalDatum">datum</see>,
        /// <see cref="ILinearUnit">linear unit</see> and <see cref="IPrimeMeridian" />.
        /// </summary>
        /// <param name="name">Name of geocentric coordinate system</param>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <returns>Geocentric Coordinate System</returns>
        public IGeocentricCoordinateSystem CreateGeocentricCoordinateSystem(string name, IHorizontalDatum datum, ILinearUnit linearUnit, IPrimeMeridian primeMeridian)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Invalid name");
            List<AxisInfo> info = new List<AxisInfo>(3);

            info.Add(new AxisInfo("X", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Y", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Z", AxisOrientationEnum.Other));
            return new GeocentricCoordinateSystem(datum, linearUnit, primeMeridian, info, name, String.Empty, -1, String.Empty, String.Empty, String.Empty);
        }

        #endregion
    }

    /// <summary>
    /// A 3D coordinate system, with its origin at the center of the Earth.
    /// </summary>
    public class GeocentricCoordinateSystem : CoordinateSystem, IGeocentricCoordinateSystem
    {
        private IHorizontalDatum _horizontalDatum;
        private ILinearUnit _linearUnit;
        private IPrimeMeridian _primeMeridian;

        internal GeocentricCoordinateSystem(IHorizontalDatum datum, ILinearUnit linearUnit, IPrimeMeridian primeMeridian, List<AxisInfo> axisinfo,
            string name, string authority, long code, string alias,
            string remarks, string abbreviation)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            _horizontalDatum = datum;
            _linearUnit = linearUnit;
            _primeMeridian = primeMeridian;
            if (axisinfo.Count != 3)
                throw new ArgumentException("Axis info should contain three axes for geocentric coordinate systems");
            base.AxisInfo = axisinfo;
        }

        #region Predefined geographic coordinate systems

        /// <summary>
        /// Creates a geocentric coordinate system based on the WGS84 ellipsoid, 
        /// suitable for GPS measurements.
        /// </summary>
        public static IGeocentricCoordinateSystem WGS84
        {
            get
            {
                return new CoordinateSystemFactory().CreateGeocentricCoordinateSystem("WGS84 Geocentric",
                    CoordinateSystems.HorizontalDatum.WGS84, CoordinateSystems.LinearUnit.Metre,
                    CoordinateSystems.PrimeMeridian.Greenwich);
            }
        }

        #endregion

        #region IGeocentricCoordinateSystem Members

        /// <summary>
        /// Gets or sets the HorizontalDatum. The horizontal datum is used to determine where
        /// the centre of the Earth is considered to be. All coordinate points will be
        /// measured from the centre of the Earth, and not the surface.
        /// </summary>
        public IHorizontalDatum HorizontalDatum
        {
            get { return _horizontalDatum; }
            set { _horizontalDatum = value; }
        }

        /// <summary>
        /// Gets or sets the units used along all the axes.
        /// </summary>
        public ILinearUnit LinearUnit
        {
            get { return _linearUnit; }
            set { _linearUnit = value; }
        }

        /// <summary>
        /// Returns units for dimension within coordinate system. Each dimension in
        /// the coordinate system has corresponding units.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Unit</returns>
        public override IUnit GetUnits(int dimension)
        {
            return _linearUnit;
        }


        /// <summary>
        /// Gets or sets the PrimeMeridian.
        /// </summary>
        public IPrimeMeridian PrimeMeridian
        {
            get { return _primeMeridian; }
            set { _primeMeridian = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("GEOCCS[\"{0}\", {1}, {2}, {3}", Name, HorizontalDatum.WKT, PrimeMeridian.WKT, LinearUnit.WKT);

                //Skip axis info if they contain default values				
                if (AxisInfo.Count != 3 ||
                    AxisInfo[0].Name != "X" || AxisInfo[0].Orientation != AxisOrientationEnum.Other ||
                    AxisInfo[1].Name != "Y" || AxisInfo[1].Orientation != AxisOrientationEnum.East ||
                    AxisInfo[2].Name != "Z" || AxisInfo[2].Orientation != AxisOrientationEnum.North)
                    for (int i = 0; i < AxisInfo.Count; i++)
                        sb.AppendFormat(", {0}", GetAxis(i).WKT);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_GeocentricCoordinateSystem>{1}",
                    this.Dimension, InfoXml);
                foreach (AxisInfo ai in this.AxisInfo)
                    sb.Append(ai.XML);
                sb.AppendFormat("{0}{1}{2}</CS_GeocentricCoordinateSystem></CS_CoordinateSystem>",
                    HorizontalDatum.XML, LinearUnit.XML, PrimeMeridian.XML);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is GeocentricCoordinateSystem))
                return false;
            GeocentricCoordinateSystem gcc = obj as GeocentricCoordinateSystem;
            return gcc.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
                gcc.LinearUnit.EqualParams(this.LinearUnit) &&
                gcc.PrimeMeridian.EqualParams(this.PrimeMeridian);
        }

        #endregion
    }

    /// <summary>
    /// A coordinate system based on latitude and longitude.
    /// </summary>
    /// <remarks>
    /// Some geographic coordinate systems are Lat/Lon, and some are Lon/Lat.
    /// You can find out which this is by examining the axes. You should also
    /// check the angular units, since not all geographic coordinate systems
    /// use degrees.
    /// </remarks>
    public class GeographicCoordinateSystem : HorizontalCoordinateSystem, IGeographicCoordinateSystem
    {
        private IAngularUnit _angularUnit;
        private IPrimeMeridian _primeMeridian;
        private List<Wgs84ConversionInfo> _wgs84ConversionInfo;

        /// <summary>
        /// Creates an instance of the MapAround.CoordinateSystems.GeographicCoordinateSystem.
        /// </summary>
        /// <param name="angularUnit">Angular units</param>
        /// <param name="horizontalDatum">Horizontal datum</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <param name="axisInfo">Axis info</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="authorityCode">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal GeographicCoordinateSystem(IAngularUnit angularUnit, IHorizontalDatum horizontalDatum, IPrimeMeridian primeMeridian, List<AxisInfo> axisInfo, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
            :
            base(horizontalDatum, axisInfo, name, authority, authorityCode, alias, abbreviation, remarks)
        {
            _angularUnit = angularUnit;
            _primeMeridian = primeMeridian;
        }

        #region Predefined geographic coordinate systems

        /// <summary>
        /// Creates a decimal degrees geographic coordinate system based on the WGS84 ellipsoid, 
        /// suitable for GPS measurements
        /// </summary>
        public static GeographicCoordinateSystem WGS84
        {
            get
            {
                List<AxisInfo> axes = new List<AxisInfo>(2);
                axes.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
                axes.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
                return new GeographicCoordinateSystem(CoordinateSystems.AngularUnit.Degrees,
                    CoordinateSystems.HorizontalDatum.WGS84, CoordinateSystems.PrimeMeridian.Greenwich, axes,
                    "WGS 84", "EPSG", 4326, String.Empty, string.Empty, string.Empty);
            }
        }

        #endregion

        #region IGeographicCoordinateSystem Members

        /// <summary>
        /// Gets or sets the angular units.
        /// </summary>
        public IAngularUnit AngularUnit
        {
            get { return _angularUnit; }
            set { _angularUnit = value; }
        }

        /// <summary>
        /// Gets units for dimension within coordinate system. Each dimension in
        /// the coordinate system has corresponding units.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Unit</returns>
        public override IUnit GetUnits(int dimension)
        {
            return _angularUnit;
        }

        /// <summary>
        /// Gets or sets the prime meridian of the geographic coordinate system.
        /// </summary>
        public IPrimeMeridian PrimeMeridian
        {
            get { return _primeMeridian; }
            set { _primeMeridian = value; }
        }

        /// <summary>
        /// Gets the number of available conversions to WGS84 coordinates.
        /// </summary>
        public int NumConversionToWGS84
        {
            get { return _wgs84ConversionInfo.Count; }
        }

        internal List<Wgs84ConversionInfo> WGS84ConversionInfo
        {
            get { return _wgs84ConversionInfo; }
            set { _wgs84ConversionInfo = value; }
        }

        /// <summary>
        /// Gets details of a conversion to WGS84.
        /// </summary>
        public Wgs84ConversionInfo GetWgs84ConversionInfo(int index)
        {
            return _wgs84ConversionInfo[index];
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("GEOGCS[\"{0}\", {1}, {2}, {3}", Name, HorizontalDatum.WKT, PrimeMeridian.WKT, AngularUnit.WKT);
                //Skip axis info if they contain default values
                if (AxisInfo.Count != 2 ||
                    AxisInfo[0].Name != "Lon" || AxisInfo[0].Orientation != AxisOrientationEnum.East ||
                    AxisInfo[1].Name != "Lat" || AxisInfo[1].Orientation != AxisOrientationEnum.North)
                    for (int i = 0; i < AxisInfo.Count; i++)
                        sb.AppendFormat(", {0}", GetAxis(i).WKT);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_GeographicCoordinateSystem>{1}",
                    this.Dimension, InfoXml);
                foreach (AxisInfo ai in this.AxisInfo)
                    sb.Append(ai.XML);
                sb.AppendFormat("{0}{1}{2}</CS_GeographicCoordinateSystem></CS_CoordinateSystem>",
                    HorizontalDatum.XML, AngularUnit.XML, PrimeMeridian.XML);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is GeographicCoordinateSystem))
                return false;
            GeographicCoordinateSystem gcs = obj as GeographicCoordinateSystem;
            if (gcs.Dimension != this.Dimension) return false;
            if (this.WGS84ConversionInfo != null && gcs.WGS84ConversionInfo == null) return false;
            if (this.WGS84ConversionInfo == null && gcs.WGS84ConversionInfo != null) return false;
            if (this.WGS84ConversionInfo != null && gcs.WGS84ConversionInfo != null)
            {
                if (this.WGS84ConversionInfo.Count != gcs.WGS84ConversionInfo.Count) return false;
                for (int i = 0; i < this.WGS84ConversionInfo.Count; i++)
                    if (!gcs.WGS84ConversionInfo[i].Equals(this.WGS84ConversionInfo[i]))
                        return false;
            }
            if (this.AxisInfo.Count != gcs.AxisInfo.Count) return false;
            for (int i = 0; i < gcs.AxisInfo.Count; i++)
                if (gcs.AxisInfo[i].Orientation != this.AxisInfo[i].Orientation)
                    return false;
            return gcs.AngularUnit.EqualParams(this.AngularUnit) &&
                    gcs.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
                    gcs.PrimeMeridian.EqualParams(this.PrimeMeridian);
        }

        #endregion
    }

    /// <summary>
    /// A 2D coordinate system suitable for positions on the Earth's surface.
    /// </summary>

    public abstract class HorizontalCoordinateSystem : CoordinateSystem, IHorizontalCoordinateSystem
    {
        private IHorizontalDatum _horizontalDatum;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.HorizontalCoordinateSystem.
        /// </summary>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="axisInfo">Axis information</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal HorizontalCoordinateSystem(IHorizontalDatum datum, List<AxisInfo> axisInfo,
            string name, string authority, long code, string alias,
            string remarks, string abbreviation)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            _horizontalDatum = datum;
            if (axisInfo.Count != 2)
                throw new ArgumentException("Axis info should contain two axes for horizontal coordinate systems");
            base.AxisInfo = axisInfo;
        }

        #region IHorizontalCoordinateSystem Members

        /// <summary>
        /// Gets or sets the HorizontalDatum.
        /// </summary>
        public IHorizontalDatum HorizontalDatum
        {
            get { return _horizontalDatum; }
            set { _horizontalDatum = value; }
        }

        #endregion
    }

    /// <summary>
    /// Represents a 2D cartographic coordinate system.
    /// </summary>
    public class ProjectedCoordinateSystem : HorizontalCoordinateSystem, IProjectedCoordinateSystem
    {
        private IGeographicCoordinateSystem _geographicCoordinateSystem;
        private ILinearUnit _linearUnit;
        private IProjection _projection;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.ProjectedCoordinateSystem.
        /// </summary>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="geographicCoordinateSystem">Geographic coordinate system</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="projection">Projection</param>
        /// <param name="axisInfo">Axis info</param>
        /// <param name="name">Name</param>
        /// <param name="authority">Authority name</param>
        /// <param name="code">Authority-specific identification code.</param>
        /// <param name="alias">Alias</param>
        /// <param name="abbreviation">Abbreviation</param>
        /// <param name="remarks">Provider-supplied remarks</param>
        internal ProjectedCoordinateSystem(IHorizontalDatum datum, IGeographicCoordinateSystem geographicCoordinateSystem,
            ILinearUnit linearUnit, IProjection projection, List<AxisInfo> axisInfo,
            string name, string authority, long code, string alias,
            string remarks, string abbreviation)
            : base(datum, axisInfo, name, authority, code, alias, abbreviation, remarks)
        {
            _geographicCoordinateSystem = geographicCoordinateSystem;
            _linearUnit = linearUnit;
            _projection = projection;
        }

        #region IProjectedCoordinateSystem Members

        /// <summary>
        /// Gets or sets the GeographicCoordinateSystem.
        /// </summary>
        public IGeographicCoordinateSystem GeographicCoordinateSystem
        {
            get { return _geographicCoordinateSystem; }
            set { _geographicCoordinateSystem = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="P:MapAround.CoordinateSystems.ProjectedCoordinateSystem.LinearUnit">LinearUnits</see>. 
        /// The linear unit must be the same as the <see cref="T:MapAround.CoordinateSystems.CoordinateSystem" /> 
        /// units.
        /// </summary>
        public ILinearUnit LinearUnit
        {
            get { return _linearUnit; }
            set { _linearUnit = value; }
        }

        /// <summary>
        /// Returns units for dimension within coordinate system. Each dimension in
        /// the coordinate system has corresponding units.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Unit</returns>
        public override IUnit GetUnits(int dimension)
        {
            return _linearUnit;
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        public IProjection Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        /// <summary>
        /// Gats a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("PROJCS[\"{0}\", {1}, {2}", Name, GeographicCoordinateSystem.WKT, Projection.WKT);
                for (int i = 0; i < Projection.NumParameters; i++)
                    sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, ", {0}", Projection.GetParameter(i).WKT);
                sb.AppendFormat(", {0}", LinearUnit.WKT);

                if (AxisInfo.Count != 2 ||
                    AxisInfo[0].Name != "X" || AxisInfo[0].Orientation != AxisOrientationEnum.East ||
                    AxisInfo[1].Name != "Y" || AxisInfo[1].Orientation != AxisOrientationEnum.North)
                    for (int i = 0; i < AxisInfo.Count; i++)
                        sb.AppendFormat(", {0}", GetAxis(i).WKT);
                if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
                    "<CS_CoordinateSystem Dimension=\"{0}\"><CS_ProjectedCoordinateSystem>{1}",
                    this.Dimension, InfoXml);
                foreach (AxisInfo ai in this.AxisInfo)
                    sb.Append(ai.XML);

                sb.AppendFormat("{0}{1}{2}</CS_ProjectedCoordinateSystem></CS_CoordinateSystem>",
                    GeographicCoordinateSystem.XML, LinearUnit.XML, Projection.XML);
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is ProjectedCoordinateSystem))
                return false;
            ProjectedCoordinateSystem pcs = obj as ProjectedCoordinateSystem;
            if (pcs.Dimension != this.Dimension)
                return false;
            for (int i = 0; i < pcs.Dimension; i++)
            {
                if (pcs.GetAxis(i).Orientation != this.GetAxis(i).Orientation)
                    return false;
                if (!pcs.GetUnits(i).EqualParams(this.GetUnits(i)))
                    return false;
            }

            return pcs.GeographicCoordinateSystem.EqualParams(this.GeographicCoordinateSystem) &&
                    pcs.HorizontalDatum.EqualParams(this.HorizontalDatum) &&
                    pcs.LinearUnit.EqualParams(this.LinearUnit) &&
                    pcs.Projection.EqualParams(this.Projection);
        }

        #endregion
    }
}
