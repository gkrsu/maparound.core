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
** File: Projections.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Projection classes
**
=============================================================================*/

namespace MapAround.CoordinateSystems.Projections
{
    using System;
    using System.Text;
    using System.Globalization;
    using System.Collections.Generic;

    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Transformations;

    /// <summary>
    /// The MapAround.Projections namespace contains interfaces and classes
    /// that defines projections.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// The IParameterInfo interface provides an interface through which clients of a
    /// Projected Coordinate System or of a Projection can set the parameters of the
    /// projection. It provides a generic interface for discovering the names and default
    /// values of parameters, and for setting and getting parameter values. Subclasses of
    /// this interface may provide projection specific parameter access methods.
    /// </summary>
    public interface IParameterInfo
    {
        /// <summary>
        /// Gets the number of parameters expected.
        /// </summary>
        int NumParameters { get; }

        /// <summary>
        /// Returns the default parameters for this projection.
        /// </summary>
        /// <returns>The default parameters for this projection</returns>
        Parameter[] DefaultParameters();

        /// <summary>
        /// Gets or sets the list of parameters for this projection.
        /// </summary>
        List<Parameter> Parameters { get; set; }

        /// <summary>
        /// Gets the parameter by its name.
        /// </summary>
        /// <param name="name">The name of parameter</param>
        /// <returns>Requested parameter</returns>
        Parameter GetParameterByName(string name);
    }

    /// <summary>
    /// A named parameter value.
    /// </summary>
    public class Parameter
    {
        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.Parameter
        /// </summary>
        /// <remarks>Units are always either meters or degrees.</remarks>
        /// <param name="name">The name of parameter</param>
        /// <param name="value">Value</param>
        public Parameter(string name, double value)
        {
            _name = name;
            _value = value;
        }

        #region IParameter Members

        private string _name;

        /// <summary>
        /// Gets or sets a parameter name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private double _value;

        /// <summary>
        /// Gets or sets a parameter value.
        /// </summary>
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion
    }

    /// <summary>
    /// A named projection parameter value.
    /// </summary>
    /// <remarks>
    /// The linear units of parameters' values match the linear units of the containing 
    /// projected coordinate system. The angular units of parameter values match the 
    /// angular units of the geographic coordinate system that the projected coordinate 
    /// system is based on. (Notice that this is different from <see cref="Parameter"/>,
    /// where the units are always meters and degrees.)
    /// </remarks>
    public class ProjectionParameter
    {
        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.ProjectionParameter.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="value">Parameter value</param>
        public ProjectionParameter(string name, double value)
        {
            _name = name;
            _value = value;
        }

        private string _name;

        /// <summary>
        /// Gets or sets a parameter name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private double _value;

        /// <summary>
        /// Gets or sets a parameter value.
        /// <para>
        /// The linear units of a parameters' values match the linear units of the containing 
        /// projected coordinate system. The angular units of parameter values match the 
        /// angular units of the geographic coordinate system that the projected coordinate 
        /// system is based on.
        /// </para>
        /// </summary>
        public double Value
        {
            get { return _value; }
            set { _value = value; }
        }


        /// <summary>
        /// Gets a well-known text representation of this instance.
        /// </summary>
        public string WKT
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat, "PARAMETER[\"{0}\", {1}]", Name, Value);
            }
        }

        /// <summary>
        /// Gets an XML representation of this instance.
        /// </summary>
        public string XML
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_ProjectionParameter Name=\"{0}\" Value=\"{1}\"/>", Name, Value);
            }
        }
    }

    /// <summary>
    /// Represents a cartographic projection.
    /// Defines the standard information stored with a projection
    /// objects. A projection object implements a coordinate transformation from a geographic
    /// coordinate system to a projected coordinate system, given the ellipsoid for the
    /// geographic coordinate system. It is expected that each coordinate transformation of
    /// interest, e.g., Transverse Mercator, Lambert, will be implemented as a class of
    /// type Projection, supporting the IProjection interface.
    /// </summary>
    public class Projection : SpatialReferenceInfo, IProjection
    {
        internal Projection(string className, List<ProjectionParameter> parameters,
            string name, string authority, long code, string alias,
            string remarks, string abbreviation)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            _parameters = parameters;
            _className = className;
        }

        #region IProjection Members

        /// <summary>
        /// Gets a number of parameters.
        /// </summary>
        public int NumParameters
        {
            get { return _parameters.Count; }
        }

        private List<ProjectionParameter> _parameters;

        /// <summary>
        /// Gets or sets a list containing projection parameters.
        /// </summary>
        internal List<ProjectionParameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// Gets a parameter by its index.
        /// </summary>
        /// <param name="n">An index of the parameter</param>
        public ProjectionParameter GetParameter(int n)
        {
            return _parameters[n];
        }

        /// <summary>
        /// Gets a projection parameter by its name.
        /// </summary>
        /// <remarks>The names of parameters are case sensitive.</remarks>
        /// <param name="name">The parameter name</param>
        public ProjectionParameter GetParameter(string name)
        {
            return _parameters.Find(delegate(ProjectionParameter par)
            { return par.Name.Equals(name, StringComparison.OrdinalIgnoreCase); });
        }

        private string _className;

        /// <summary>
        /// Gets a class of the projection. "Mercator" for example.
        /// </summary>
        public string ClassName
        {
            get { return _className; }
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("PROJECTION[\"{0}\"", Name);
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
                sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "<CS_Projection Classname=\"{0}\">{1}", ClassName, InfoXml);
                foreach (ProjectionParameter param in Parameters)
                    sb.Append(param.XML);
                sb.Append("</CS_Projection>");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj">Object to compare parameters</param>
        /// <returns>True if equal</returns>
        public override bool EqualParams(object obj)
        {
            if (!(obj is Projection))
                return false;
            Projection proj = obj as Projection;
            if (proj.NumParameters != this.NumParameters)
                return false;
            for (int i = 0; i < _parameters.Count; i++)
            {
                ProjectionParameter param = _parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals(proj.GetParameter(i).Name, StringComparison.OrdinalIgnoreCase); });
                if (param == null)
                    return false;
                if (param.Value != proj.GetParameter(i).Value)
                    return false;
            }
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Implements the Albers projection.
    /// </summary>
    /// <remarks>
    /// <para>Implements the Albers projection. The Albers projection is most commonly
    /// used to project the United States of America. It gives the northern
    /// border with Canada a curved appearance.</para>
    ///
    /// <para>The <a href="http://www.geog.mcgill.ca/courses/geo201/mapproj/naaeana.gif">Albers Equal Area</a>
    /// projection has the property that the area bounded
    /// by any pair of parallels and meridians is exactly reproduced between the
    /// image of those parallels and meridians in the projected domain, that is,
    /// the projection preserves the correct area of the earth though distorts
    /// direction, distance and shape somewhat.</para>
    /// </remarks>
    internal class AlbersProjection : MapProjection
    {
        double _falseEasting;
        double _falseNorthing;
        double C;
        double e;				//eccentricity
        double e_sq = 0;
        double ro0;
        double n;
        double _lonCenter;		//central longitude

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.AlbersProjection.
        /// </summary>
        /// <param name="parameters">A list containing the projection parameters</param>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
        /// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
        /// </list>
        /// </remarks>
        public AlbersProjection(List<ProjectionParameter> parameters)
            : this(parameters, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.AlbersProjection.
        /// </summary>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>latitude_of_center</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>longitude_of_center</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>standard_parallel_1</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>standard_parallel_2</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>false_easting</term><description>The easting value assigned to the false origin.</description></item>
        /// <item><term>false_northing</term><description>The northing value assigned to the false origin.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
        public AlbersProjection(List<ProjectionParameter> parameters, bool isInverse)
            : base(parameters, isInverse)
        {
            this.Name = "Albers_Conic_Equal_Area";

            //Retrieve parameters
            ProjectionParameter longitude_of_center = GetParameter("longitude_of_center");
            ProjectionParameter latitude_of_center = GetParameter("latitude_of_center");
            ProjectionParameter standard_parallel_1 = GetParameter("standard_parallel_1");
            ProjectionParameter standard_parallel_2 = GetParameter("standard_parallel_2");
            ProjectionParameter false_easting = GetParameter("false_easting");
            ProjectionParameter false_northing = GetParameter("false_northing");
            //Check for missing parameters
            if (longitude_of_center == null)
            {
                longitude_of_center = GetParameter("central_meridian"); //Allow for altenative name
                if (longitude_of_center == null)
                    throw new ArgumentException("Missing projection parameter 'longitude_of_center'");
            }
            if (latitude_of_center == null)
            {
                latitude_of_center = GetParameter("latitude_of_origin"); //Allow for altenative name
                if (latitude_of_center == null)
                    throw new ArgumentException("Missing projection parameter 'latitude_of_center'");
            }
            if (standard_parallel_1 == null)
                throw new ArgumentException("Missing projection parameter 'standard_parallel_1'");
            if (standard_parallel_2 == null)
                throw new ArgumentException("Missing projection parameter 'standard_parallel_2'");
            if (false_easting == null)
                throw new ArgumentException("Missing projection parameter 'false_easting'");
            if (false_northing == null)
                throw new ArgumentException("Missing projection parameter 'false_northing'");

            _lonCenter = MathUtils.Degrees.ToRadians(longitude_of_center.Value);
            double lat0 = MathUtils.Degrees.ToRadians(latitude_of_center.Value);
            double lat1 = MathUtils.Degrees.ToRadians(standard_parallel_1.Value);
            double lat2 = MathUtils.Degrees.ToRadians(standard_parallel_2.Value);
            this._falseEasting = false_easting.Value * _metersPerUnit;
            this._falseNorthing = false_northing.Value * _metersPerUnit;

            if (Math.Abs(lat1 + lat2) < double.Epsilon)
                throw new ApplicationException("Equal latitudes for standard parallels on opposite sides of Equator.");

            e_sq = 1.0 - Math.Pow(this._semiMinor / this._semiMajor, 2);
            e = Math.Sqrt(e_sq); //Eccentricity

            double alpha1 = alpha(lat1);
            double alpha2 = alpha(lat2);

            double m1 = Math.Cos(lat1) / Math.Sqrt(1 - e_sq * Math.Pow(Math.Sin(lat1), 2));
            double m2 = Math.Cos(lat2) / Math.Sqrt(1 - e_sq * Math.Pow(Math.Sin(lat2), 2));

            n = (Math.Pow(m1, 2) - Math.Pow(m2, 2)) / (alpha2 - alpha1);
            C = Math.Pow(m1, 2) + (n * alpha1);

            ro0 = Ro(alpha(lat0));
            /*
            double sin_p0 = Math.Sin(lat0);
            double cos_p0 = Math.Cos(lat0);
            double q0 = qsfnz(e, sin_p0, cos_p0);

            double sin_p1 = Math.Sin(lat1);
            double cos_p1 = Math.Cos(lat1);
            double m1 = msfnz(e,sin_p1,cos_p1);
            double q1 = qsfnz(e,sin_p1,cos_p1);


            double sin_p2 = Math.Sin(lat2);
            double cos_p2 = Math.Cos(lat2);
            double m2 = msfnz(e,sin_p2,cos_p2);
            double q2 = qsfnz(e,sin_p2,cos_p2);

            if (Math.Abs(lat1 - lat2) > EPSLN)
                ns0 = (m1 * m1 - m2 * m2)/ (q2 - q1);
            else
                ns0 = sin_p1;
            C = m1 * m1 + ns0 * q1;
            rh = this._semiMajor * Math.Sqrt(C - ns0 * q0)/ns0;
            */
        }
        #endregion

        #region Public methods

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lonlat">A coordinate array of the point in decimal degrees</param>
        /// <returns>A coordinate array of the point in projected meters</returns>
        public override double[] DegreesToMeters(double[] lonlat)
        {
            double dLongitude = MathUtils.Degrees.ToRadians(lonlat[0]);
            double dLatitude = MathUtils.Degrees.ToRadians(lonlat[1]);

            double a = alpha(dLatitude);
            double ro = Ro(a);
            double theta = n * (dLongitude - _lonCenter);
            dLongitude = _falseEasting + ro * Math.Sin(theta);
            dLatitude = _falseNorthing + ro0 - (ro * Math.Cos(theta));
            if (lonlat.Length == 2)
                return new double[] { dLongitude / _metersPerUnit, dLatitude / _metersPerUnit };
            else
                return new double[] { dLongitude / _metersPerUnit, dLatitude / _metersPerUnit, lonlat[2] };
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="p">A coordinate array of the point in meters</param>
        /// <returns>A coordinate array of the transformed point in decimal degrees</returns>
        public override double[] MetersToDegrees(double[] p)
        {
            double theta = Math.Atan((p[0] * _metersPerUnit - _falseEasting) / (ro0 - (p[1] * _metersPerUnit - _falseNorthing)));
            double ro = Math.Sqrt(Math.Pow(p[0] * _metersPerUnit - _falseEasting, 2) + Math.Pow(ro0 - (p[1] * _metersPerUnit - _falseNorthing), 2));
            double q = (C - Math.Pow(ro, 2) * Math.Pow(n, 2) / Math.Pow(this._semiMajor, 2)) / n;
            double b = Math.Sin(q / (1 - ((1 - e_sq) / (2 * e)) * Math.Log((1 - e) / (1 + e))));

            double lat = Math.Asin(q * 0.5);
            double preLat = double.MaxValue;
            int iterationCounter = 0;
            while (Math.Abs(lat - preLat) > 0.000001)
            {
                preLat = lat;
                double sin = Math.Sin(lat);
                double e2sin2 = e_sq * Math.Pow(sin, 2);
                lat += (Math.Pow(1 - e2sin2, 2) / (2 * Math.Cos(lat))) * ((q / (1 - e_sq)) - sin / (1 - e2sin2) + 1 / (2 * e) * Math.Log((1 - e * sin) / (1 + e * sin)));
                iterationCounter++;
                if (iterationCounter > 25)
                    throw new ApplicationException("Inverse Albers transform failed to converge.");
            }
            double lon = _lonCenter + (theta / n);
            
            if (p.Length < 3)
                return new double[] 
                {
                    MathUtils.Radians.ToDegrees(lon), 
                    MathUtils.Radians.ToDegrees(lat) 
                };
            else
                return new double[] 
                { 
                    MathUtils.Radians.ToDegrees(lon), 
                    MathUtils.Radians.ToDegrees(lat), p[2] 
                };
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new AlbersProjection(this._parameters, !_isInverse);
            return _inverse;
        }

        #endregion

        #region Math helper functions

        private double alpha(double lat)
        {
            double sin = Math.Sin(lat);
            double sinsq = Math.Pow(sin, 2);
            return (1 - e_sq) * (((sin / (1 - e_sq * sinsq)) - 1 / (2 * e) * Math.Log((1 - e * sin) / (1 + e * sin))));
        }

        private double Ro(double a)
        {
            return this._semiMajor * Math.Sqrt((C - n * a)) / n;
        }

        #endregion
    }

    /// <summary>
    /// Implemetns the Lambert Conformal Conic 2SP Projection.
    /// </summary>
    /// <remarks>
    /// <para>The Lambert Conformal Conic projection is a standard projection for presenting maps
    /// of land areas whose East-West extent is large compared with their North-South extent.
    /// This projection is "conformal" in the sense that lines of latitude and longitude,
    /// which are perpendicular to one another on the earth's surface, are also perpendicular
    /// to one another in the projected domain.</para>
    /// </remarks>
    internal class LambertConformalConic2SP : MapProjection
    {

        double _falseEasting;
        double _falseNorthing;

        private double es = 0;              // square of eccentricity
        private double e = 0;               // eccentricity
        private double _centerLon = 0;      // central longitude
        private double _centerLat = 0;      // central latitude
        private double ns = 0;              // attitude angles between meridians
        private double f0 = 0;              // contraction of the ellipsoid
        private double rh = 0;              // elevation above the ellipsoid

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.LambertConformalConic2SP projection object.
        /// </summary>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
        /// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        public LambertConformalConic2SP(List<ProjectionParameter> parameters)
            : this(parameters, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.LambertConformalConic2SP projection object.
        /// </summary>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Parameter</term><description>Description</description></listheader>
        /// <item><term>latitude_of_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>central_meridian</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
        /// <item><term>standard_parallel_1</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>standard_parallel_2</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
        /// <item><term>false_easting</term><description>The easting value assigned to the false origin.</description></item>
        /// <item><term>false_northing</term><description>The northing value assigned to the false origin.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
        public LambertConformalConic2SP(List<ProjectionParameter> parameters, bool isInverse)
            : base(parameters, isInverse)
        {
            this.Name = "Lambert_Conformal_Conic_2SP";
            this.Authority = "EPSG";
            this.AuthorityCode = 9802;
            ProjectionParameter latitude_of_origin = GetParameter("latitude_of_origin");
            ProjectionParameter central_meridian = GetParameter("central_meridian");
            ProjectionParameter standard_parallel_1 = GetParameter("standard_parallel_1");
            ProjectionParameter standard_parallel_2 = GetParameter("standard_parallel_2");
            ProjectionParameter false_easting = GetParameter("false_easting");
            ProjectionParameter false_northing = GetParameter("false_northing");

            // check for required parameters
            if (latitude_of_origin == null)
                throw new ArgumentException("Missing projection parameter 'latitude_of_origin'");
            if (central_meridian == null)
                throw new ArgumentException("Missing projection parameter 'central_meridian'");
            if (standard_parallel_1 == null)
                throw new ArgumentException("Missing projection parameter 'standard_parallel_1'");
            if (standard_parallel_2 == null)
                throw new ArgumentException("Missing projection parameter 'standard_parallel_2'");
            if (false_easting == null)
                throw new ArgumentException("Missing projection parameter 'false_easting'");
            if (false_northing == null)
                throw new ArgumentException("Missing projection parameter 'false_northing'");

            double c_lat = MathUtils.Degrees.ToRadians(latitude_of_origin.Value);
            double c_lon = MathUtils.Degrees.ToRadians(central_meridian.Value);
            double lat1 = MathUtils.Degrees.ToRadians(standard_parallel_1.Value);
            double lat2 = MathUtils.Degrees.ToRadians(standard_parallel_2.Value);
            this._falseEasting = false_easting.Value * _metersPerUnit;
            this._falseNorthing = false_northing.Value * _metersPerUnit;

            double sin_po;                  // sin value                            
            double cos_po;                  // cos value                            
            double con;                     // temporary variable                   
            double ms1;                     // small m 1                            
            double ms2;                     // small m 2                            
            double ts0;                     // small t 0                            
            double ts1;                     // small t 1                            
            double ts2;                     // small t 2                            

            // standard parallels can not be same
            if (Math.Abs(lat1 + lat2) < Tolerance)
                throw new ArgumentException("Standard parallels are the same", "parameters");

            es = 1.0 - Math.Pow(this._semiMinor / this._semiMajor, 2);
            e = Math.Sqrt(es);


            _centerLon = c_lon;
            _centerLat = c_lat;
            sincos(lat1, out sin_po, out cos_po);
            con = sin_po;
            ms1 = msfnz(e, sin_po, cos_po);
            ts1 = tsfnz(e, lat1, sin_po);
            sincos(lat2, out sin_po, out cos_po);
            ms2 = msfnz(e, sin_po, cos_po);
            ts2 = tsfnz(e, lat2, sin_po);
            sin_po = Math.Sin(_centerLat);
            ts0 = tsfnz(e, _centerLat, sin_po);

            if (Math.Abs(lat1 - lat2) > Tolerance)
                ns = Math.Log(ms1 / ms2) / Math.Log(ts1 / ts2);
            else
                ns = con;
            f0 = ms1 / (ns * Math.Pow(ts1, ns));
            rh = this._semiMajor * f0 * Math.Pow(ts0, ns);
        }
        #endregion

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lonlat">A coordinate array of the point in decimal degrees.</param>
        /// <returns>A coordinate array of the point in projected meters</returns>
        public override double[] DegreesToMeters(double[] lonlat)
        {
            double dLongitude = MathUtils.Degrees.ToRadians(lonlat[0]);
            double dLatitude = MathUtils.Degrees.ToRadians(lonlat[1]);

            double con;                     /* temporary angular variable       */
            double rh1;                     /* elevation above the ellipsoid           */
            double sinPhi;                  /* sine phi                          */
            double theta;                   /* angle                                 */
            double ts;                      

            con = Math.Abs(Math.Abs(dLatitude) - HALF_PI);
            if (con > Tolerance)
            {
                sinPhi = Math.Sin(dLatitude);
                ts = tsfnz(e, dLatitude, sinPhi);
                rh1 = this._semiMajor * f0 * Math.Pow(ts, ns);
            }
            else
            {
                con = dLatitude * ns;
                if (con <= 0)
                    throw new ApplicationException();
                rh1 = 0;
            }
            theta = ns * adjustLon(dLongitude - _centerLon);
            dLongitude = rh1 * Math.Sin(theta) + this._falseEasting;
            dLatitude = rh - rh1 * Math.Cos(theta) + this._falseNorthing;
            if (lonlat.Length == 2)
                return new double[] { dLongitude / _metersPerUnit, dLatitude / _metersPerUnit };
            else
                return new double[] { dLongitude / _metersPerUnit, dLatitude / _metersPerUnit, lonlat[2] };
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="p">A coordinate array of the point in meters</param>
        /// <returns>A coordinate array of the transformed point in decimal degrees</returns>
        public override double[] MetersToDegrees(double[] p)
        {
            double dLongitude = Double.NaN;
            double dLatitude = Double.NaN;

            double rh1;			// elevation above the ellipsoid
            double con;			// sign variable
            double ts;			// small t			
            double theta;	    // angle
            long flag;			// Error Flag

            flag = 0;
            double dX = p[0] * _metersPerUnit - this._falseEasting;
            double dY = rh - p[1] * _metersPerUnit + this._falseNorthing;
            if (ns > 0)
            {
                rh1 = Math.Sqrt(dX * dX + dY * dY);
                con = 1.0;
            }
            else
            {
                rh1 = -Math.Sqrt(dX * dX + dY * dY);
                con = -1.0;
            }
            theta = 0.0;
            if (rh1 != 0)
                theta = Math.Atan2((con * dX), (con * dY));
            if ((rh1 != 0) || (ns > 0.0))
            {
                con = 1.0 / ns;
                ts = Math.Pow((rh1 / (this._semiMajor * f0)), con);
                dLatitude = phi2z(e, ts, out flag);
                if (flag != 0)
                    throw new ApplicationException();
            }
            else dLatitude = -HALF_PI;

            dLongitude = adjustLon(theta / ns + _centerLon);
            if (p.Length == 2)
                return new double[] 
                { 
                    MathUtils.Radians.ToDegrees(dLongitude), 
                    MathUtils.Radians.ToDegrees(dLatitude) 
                };
            else
                return new double[] 
                { 
                    MathUtils.Radians.ToDegrees(dLongitude), 
                    MathUtils.Radians.ToDegrees(dLatitude), p[2] };
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new LambertConformalConic2SP(this._parameters, !_isInverse);
            return _inverse;
        }
    }

    /// <summary>
    /// Implements the Mercator projection.
    /// </summary>
    /// <remarks>
    /// <para>This map projection introduced in 1569 by Gerardus Mercator. It is often described as a cylindrical projection,
    /// but it must be derived mathematically. The meridians are equally spaced, parallel vertical lines, and the
    /// parallels of latitude are parallel, horizontal straight lines, spaced farther and farther apart as their distance
    /// from the Equator increases. This projection is widely used for navigation charts, because any straight line
    /// on a Mercator-projection map is a line of constant true bearing that enables a navigator to plot a straight-line
    /// course. It is less practical for world maps because the scale is distorted; areas farther away from the equator
    /// appear disproportionately large. On a Mercator projection, for example, the landmass of Greenland appears to be
    /// greater than that of the continent of South America; in actual area, Greenland is smaller than the Arabian Peninsula.
    /// </para>
    /// </remarks>
    internal class Mercator : MapProjection
    {
        double _falseEasting;
        double _falseNorthing;
        double lon_center;		// Central longitude
        double lat_origin;		// central latitude 
        double e, e2;			//eccentricities
        double k0;				//small value m

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.Mercator instance with the specified parameters to project points.
        /// </summary>
        /// <param name="parameters">ParameterList with the required parameters.</param>
        /// <remarks>
        /// </remarks>
        public Mercator(List<ProjectionParameter> parameters)
            : this(parameters, false)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.Mercator instance with the specified parameters to project points.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
        /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
        /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
        /// </list>
        /// </remarks>
        public Mercator(List<ProjectionParameter> parameters, bool isInverse)
            : base(parameters, isInverse)
        {
            this.Authority = "EPSG";
            ProjectionParameter central_meridian = GetParameter("central_meridian");
            ProjectionParameter latitude_of_origin = GetParameter("latitude_of_origin");
            ProjectionParameter scale_factor = GetParameter("scale_factor");
            ProjectionParameter false_easting = GetParameter("false_easting");
            ProjectionParameter false_northing = GetParameter("false_northing");

            // check for required parameters
            if (central_meridian == null)
                throw new ArgumentException("Missing projection parameter 'central_meridian'");
            if (latitude_of_origin == null)
                throw new ArgumentException("Missing projection parameter 'latitude_of_origin'");
            if (false_easting == null)
                throw new ArgumentException("Missing projection parameter 'false_easting'");
            if (false_northing == null)
                throw new ArgumentException("Missing projection parameter 'false_northing'");

            lon_center = MathUtils.Degrees.ToRadians(central_meridian.Value);
            lat_origin = MathUtils.Degrees.ToRadians(latitude_of_origin.Value);
            _falseEasting = false_easting.Value * _metersPerUnit;
            _falseNorthing = false_northing.Value * _metersPerUnit;

            double temp = this._semiMinor / this._semiMajor;
            e2 = 1 - temp * temp;
            e = Math.Sqrt(e2);
            if (scale_factor == null) 
            {
                k0 = Math.Cos(lat_origin) / Math.Sqrt(1.0 - e2 * Math.Sin(lat_origin) * Math.Sin(lat_origin));
                this.AuthorityCode = 9805;
                this.Name = "Mercator_2SP";
            }
            else 
            {
                k0 = scale_factor.Value;
                this.Name = "Mercator_1SP";
            }
            this.Authority = "EPSG";
        }

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lonlat">A coordinate array of the point in decimal degrees.</param>
        /// <returns>A coordinate array of the point in projected meters</returns>
        public override double[] DegreesToMeters(double[] lonlat)
        {
            if (double.IsNaN(lonlat[0]) || double.IsNaN(lonlat[1]))
                return new double[] { Double.NaN, Double.NaN, };
            double dLongitude = MathUtils.Degrees.ToRadians(lonlat[0]);
            double dLatitude = MathUtils.Degrees.ToRadians(lonlat[1]);

            if (Math.Abs(Math.Abs(dLatitude) - HALF_PI) <= Tolerance)
            //if (Math.Abs(Math.Abs(dLatitude) - HALF_PI) <= Math.PI / 15)
                throw new ApplicationException("Transformation cannot be computed at the poles.");
            else
            {
                double esinphi = e * Math.Sin(dLatitude);
                double x = _falseEasting + this._semiMajor * k0 * (dLongitude - lon_center);
                double y = _falseNorthing + this._semiMajor * k0 * Math.Log(Math.Tan(PI * 0.25 + dLatitude * 0.5) * Math.Pow((1 - esinphi) / (1 + esinphi), e * 0.5));

                if (lonlat.Length < 3)
                    return new double[] { x / _metersPerUnit, y / _metersPerUnit };
                else
                    return new double[] { x / _metersPerUnit, y / _metersPerUnit, lonlat[2] };
            }
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="p">A coordinate array of the point in meters</param>
        /// <returns>A coordinate array of the transformed point in decimal degrees</returns>
        public override double[] MetersToDegrees(double[] p)
        {
            double dLongitude = Double.NaN;
            double dLatitude = Double.NaN;

            // inverse computations
            double dX = p[0] * _metersPerUnit - this._falseEasting;
            double dY = p[1] * _metersPerUnit - this._falseNorthing;
            double ts = Math.Exp(-dY / (this._semiMajor * k0)); //t

            double chi = HALF_PI - 2 * Math.Atan(ts);
            double e4 = Math.Pow(e, 4);
            double e6 = Math.Pow(e, 6);
            double e8 = Math.Pow(e, 8);

            dLatitude = chi + (e2 * 0.5 + 5 * e4 / 24 + e6 / 12 + 13 * e8 / 360) * Math.Sin(2 * chi)
                + (7 * e4 / 48 + 29 * e6 / 240 + 811 * e8 / 11520) * Math.Sin(4 * chi) +
                +(7 * e6 / 120 + 81 * e8 / 1120) * Math.Sin(6 * chi) +
                +(4279 * e8 / 161280) * Math.Sin(8 * chi);

            dLongitude = dX / (this._semiMajor * k0) + lon_center;
            if (p.Length < 3)
                return new double[] 
                { 
                    MathUtils.Radians.ToDegrees(dLongitude), 
                    MathUtils.Radians.ToDegrees(dLatitude) 
                };
            else
                return new double[] 
                { 
                    MathUtils.Radians.ToDegrees(dLongitude), 
                    MathUtils.Radians.ToDegrees(dLatitude), p[2] 
                };
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new Mercator(this._parameters, !_isInverse);
            return _inverse;
        }
    }

    /// <summary>
    /// Inmplements the Universal (UTM) and Modified (MTM) 
    /// Transverses Mercator projections.
    /// </summary>
    /// <remarks>
    /// <para>This
    /// is a cylindrical projection, in which the cylinder has been rotated 90°.
    /// Instead of being tangent to the equator (or to an other standard latitude),
    /// it is tangent to a central meridian. Deformation are more important as we
    /// are going futher from the central meridian. The Transverse Mercator
    /// projection is appropriate for region wich have a greater extent north-south
    /// than east-west.</para>
    ///
    /// <para>Reference: John P. Snyder (Map Projections - A Working Manual,
    /// U.S. Geological Survey Professional Paper 1395, 1987)</para>
    /// </remarks>
    internal class TransverseMercator : MapProjection
    {

        private double _scaleFactor;	        // scaling factor
        private double _centralMeridian;    	// central longitude
        private double _latOrigin;  	        // central latitude
        private double e0, e1, e2, e3;	        // eccentricities
        private double e, es, esp;		        // eccentricities
        private double _ml0;		            // small value m			
        private double _falseNorthing;	        // Northern bias
        private double _falseEasting;	        // Eastern bias

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.TransverseMercator.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        public TransverseMercator(List<ProjectionParameter> parameters)
            : this(parameters, false)
        {
            
        }
        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.TransverseMercator.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Flag indicating wether is a forward/projection (false) or an inverse projection (true).</param>
        /// <remarks>
        /// <list type="bullet">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>semi_major</term><description>Semi major radius</description></item>
        /// <item><term>semi_minor</term><description>Semi minor radius</description></item>
        /// <item><term>scale_factor</term><description></description></item>
        /// <item><term>central meridian</term><description></description></item>
        /// <item><term>latitude_origin</term><description></description></item>
        /// <item><term>false_easting</term><description></description></item>
        /// <item><term>false_northing</term><description></description></item>
        /// </list>
        /// </remarks>
        public TransverseMercator(List<ProjectionParameter> parameters, bool isInverse)
            : base(parameters, isInverse)
        {
            this.Name = "Transverse_Mercator";
            this.Authority = "EPSG";
            this.AuthorityCode = 9807;
            ProjectionParameter par_scale_factor = GetParameter("scale_factor");
            ProjectionParameter par_central_meridian = GetParameter("central_meridian");
            ProjectionParameter par_latitude_of_origin = GetParameter("latitude_of_origin");
            ProjectionParameter par_false_easting = GetParameter("false_easting");
            ProjectionParameter par_false_northing = GetParameter("false_northing");

            // check for required parameters
            if (par_scale_factor == null)
                throw new ArgumentException("Missing projection parameter 'scale_factor'");
            if (par_central_meridian == null)
                throw new ArgumentException("Missing projection parameter 'central_meridian'");
            if (par_latitude_of_origin == null)
                throw new ArgumentException("Missing projection parameter 'latitude_of_origin'");
            if (par_false_easting == null)
                throw new ArgumentException("Missing projection parameter 'false_easting'");
            if (par_false_northing == null)
                throw new ArgumentException("Missing projection parameter 'false_northing'");

            _scaleFactor = par_scale_factor.Value;
            _centralMeridian = MathUtils.Degrees.ToRadians(par_central_meridian.Value);
            _latOrigin = MathUtils.Degrees.ToRadians(par_latitude_of_origin.Value);
            _falseEasting = par_false_easting.Value * _metersPerUnit;
            _falseNorthing = par_false_northing.Value * _metersPerUnit;

            es = 1.0 - Math.Pow(this._semiMinor / this._semiMajor, 2);
            e = Math.Sqrt(es);
            e0 = e0fn(es);
            e1 = e1fn(es);
            e2 = e2fn(es);
            e3 = e3fn(es);
            _ml0 = this._semiMajor * mlfn(e0, e1, e2, e3, _latOrigin);
            esp = es / (1.0 - es);
        }

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lonlat">A coordinate array of the point in decimal degrees.</param>
        /// <returns>A coordinate array of the point in projected meters</returns>
        public override double[] DegreesToMeters(double[] lonlat)
        {
            double lon = MathUtils.Degrees.ToRadians(lonlat[0]);
            double lat = MathUtils.Degrees.ToRadians(lonlat[1]);

            double deltaLon = 0.0;	  // Delta longitude (Given longitude - center)
            double sinPhi, cosPhi;    // sin and cos value				
            double al, als;		      // temporary values				
            double c, t, tq;	      // temporary values				
            double con, n, ml;	      // cone constant, small m			

            deltaLon = adjustLon(lon - _centralMeridian);
            sincos(lat, out sinPhi, out cosPhi);

            al = cosPhi * deltaLon;
            als = Math.Pow(al, 2);
            c = esp * Math.Pow(cosPhi, 2);
            tq = Math.Tan(lat);
            t = Math.Pow(tq, 2);
            con = 1.0 - es * Math.Pow(sinPhi, 2);
            n = this._semiMajor / Math.Sqrt(con);
            ml = this._semiMajor * mlfn(e0, e1, e2, e3, lat);

            double x =
                _scaleFactor * n * al * (1.0 + als / 6.0 * (1.0 - t + c + als / 20.0 *
                (5.0 - 18.0 * t + Math.Pow(t, 2) + 72.0 * c - 58.0 * esp))) + _falseEasting;
            double y = _scaleFactor * (ml - _ml0 + n * tq * (als * (0.5 + als / 24.0 *
                (5.0 - t + 9.0 * c + 4.0 * Math.Pow(c, 2) + als / 30.0 * (61.0 - 58.0 * t
                + Math.Pow(t, 2) + 600.0 * c - 330.0 * esp))))) + _falseNorthing;

            if (lonlat.Length < 3)
                return new double[] { x / _metersPerUnit, y / _metersPerUnit };
            else
                return new double[] { x / _metersPerUnit, y / _metersPerUnit, lonlat[2] };
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="p">A coordinate array of the point in meters</param>
        /// <returns>A coordinate array of the transformed point in decimal degrees</returns>
        public override double[] MetersToDegrees(double[] p)
        {
            double con, phi;	            	// temporary corners
            double deltaPhi;	                // dep.
            long i;			                    // counter
            double sinPhi, cosPhi, tanPhi;	    // sine, cosine, and tangent of the financial
            double c, cs, t, ts, n, r, d, ds;	// temporary variables
            long maxIterCount = 6;			    //maximum number of iterations


            double x = p[0] * _metersPerUnit - _falseEasting;
            double y = p[1] * _metersPerUnit - _falseNorthing;

            con = (_ml0 + y / _scaleFactor) / this._semiMajor;
            phi = con;
            for (i = 0; ; i++)
            {
                deltaPhi = ((con + e1 * Math.Sin(2.0 * phi) - e2 * Math.Sin(4.0 * phi) + e3 * Math.Sin(6.0 * phi))
                    / e0) - phi;
                phi += deltaPhi;
                if (Math.Abs(deltaPhi) <= Tolerance) break;
                if (i >= maxIterCount)
                    throw new ApplicationException("Latitude failed to converge");
            }
            if (Math.Abs(phi) < HALF_PI)
            {
                sincos(phi, out sinPhi, out cosPhi);
                tanPhi = Math.Tan(phi);
                c = esp * Math.Pow(cosPhi, 2);
                cs = Math.Pow(c, 2);
                t = Math.Pow(tanPhi, 2);
                ts = Math.Pow(t, 2);
                con = 1.0 - es * Math.Pow(sinPhi, 2);
                n = this._semiMajor / Math.Sqrt(con);
                r = n * (1.0 - es) / con;
                d = x / (n * _scaleFactor);
                ds = Math.Pow(d, 2);

                double lat = phi - (n * tanPhi * ds / r) * (0.5 - ds / 24.0 * (5.0 + 3.0 * t +
                    10.0 * c - 4.0 * cs - 9.0 * esp - ds / 30.0 * (61.0 + 90.0 * t +
                    298.0 * c + 45.0 * ts - 252.0 * esp - 3.0 * cs)));
                double lon = adjustLon(_centralMeridian + (d * (1.0 - ds / 6.0 * (1.0 + 2.0 * t +
                    c - ds / 20.0 * (5.0 - 2.0 * c + 28.0 * t - 3.0 * cs + 8.0 * esp +
                    24.0 * ts))) / cosPhi));

                if (p.Length < 3)
                    return new double[] 
                    { 
                        MathUtils.Radians.ToDegrees(lon), 
                        MathUtils.Radians.ToDegrees(lat) 
                    };
                else
                    return new double[] 
                    { 
                        MathUtils.Radians.ToDegrees(lon), 
                        MathUtils.Radians.ToDegrees(lat), p[2] 
                    };
            }
            else
            {
                if (p.Length < 3)
                    return new double[] 
                    { 
                        MathUtils.Radians.ToDegrees(HALF_PI * sign(y)), 
                        MathUtils.Radians.ToDegrees(_centralMeridian) 
                    };
                else
                    return new double[] 
                    { 
                        MathUtils.Radians.ToDegrees(HALF_PI * sign(y)), 
                        MathUtils.Radians.ToDegrees(_centralMeridian), p[2] 
                    };

            }
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new TransverseMercator(this._parameters, !_isInverse);
            return _inverse;
        }
    }

    /// <summary>
    /// The base class for the projection classes.
    /// Contains useful mathematical functions.
    /// </summary>
    internal abstract class MapProjection : MathTransform, IProjection
    {
        protected bool _isInverse = false;
        protected double _es;
        protected double _semiMajor;
        protected double _semiMinor;
        protected double _metersPerUnit;

        protected List<ProjectionParameter> _parameters;
        protected MathTransform _inverse;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.MapProjection.
        /// </summary>
        /// <param name="parameters">List of the expected projection parameters</param>
        /// <param name="isInverse">A value indicating whether the transformation is inverse</param>
        protected MapProjection(List<ProjectionParameter> parameters, bool isInverse)
            : this(parameters)
        {
            _isInverse = isInverse;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Projections.MapProjection.
        /// </summary>
        /// <param name="parameters">List of the expected projection parameters</param>
        protected MapProjection(List<ProjectionParameter> parameters)
        {
            _parameters = parameters;

            ProjectionParameter semimajor = GetParameter("semi_major");
            ProjectionParameter semiminor = GetParameter("semi_minor");
            if (semimajor == null)
                throw new ArgumentException("Missing projection parameter 'semi_major'");
            if (semiminor == null)
                throw new ArgumentException("Missing projection parameter 'semi_minor'");
            this._semiMajor = semimajor.Value;
            this._semiMinor = semiminor.Value;
            ProjectionParameter unit = GetParameter("unit");
            _metersPerUnit = unit.Value;

            this._es = 1.0 - (_semiMinor * _semiMinor) / (_semiMajor * _semiMajor);
        }

        #region IProjection members

        /// <summary>
        /// Gets a projection parameter by its index.
        /// </summary>
        /// <param name="Index">An index of the requested parameter</param>
        /// <returns>An object representing the requested parameter</returns>
        public ProjectionParameter GetParameter(int Index)
        {
            return this._parameters[Index];
        }

        /// <summary>
        /// Gets a projection parameter by its name.
        /// </summary>
        /// <remarks>The names of parameters are case sensitive.</remarks>
        /// <param name="name">The parameter name</param>
        public ProjectionParameter GetParameter(string name)
        {
            return _parameters.Find(delegate(ProjectionParameter par)
            { return par.Name.Equals(name, StringComparison.OrdinalIgnoreCase); });
        }

        /// <summary>
        /// Gets a number of parameters.
        /// </summary>
        public int NumParameters
        {
            get { return this._parameters.Count; }
        }

        /// <summary>
        /// Gets a class of the projection. "Mercator" for example.
        /// </summary>
        public string ClassName
        {
            get { return this.ClassName; }
        }

        private string _Abbreviation;

        /// <summary>
        /// Gets or sets an abbreviation.
        /// </summary>
        public string Abbreviation
        {
            get { return _Abbreviation; }
            set { _Abbreviation = value; }
        }

        private string _Alias;

        /// <summary>
        /// Gets or sets an alias.
        /// </summary>
        public string Alias
        {
            get { return _Alias; }
            set { _Alias = value; }
        }

        private string _Authority;

        /// <summary>
        /// Gets or sets an authority.
        /// </summary>
        public string Authority
        {
            get { return _Authority; }
            set { _Authority = value; }
        }

        private long _Code;

        /// <summary>
        /// Gets or sets an authority code.
        /// </summary>
        public long AuthorityCode
        {
            get { return _Code; }
            set { _Code = value; }
        }

        private string _Name;

        /// <summary>
        /// Gets or sets a name.
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        private string _Remarks;

        /// <summary>
        /// Gets or sets a remarks for this object.
        /// </summary>
        public string Remarks
        {
            get { return _Remarks; }
            set { _Remarks = value; }
        }


        /// <summary>
        /// Gets a well-known text representation of this instance.
        /// </summary>
        public override string WKT
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (_isInverse)
                    sb.Append("INVERSE_MT[");
                sb.AppendFormat("PARAM_MT[\"{0}\"", this.Name);
                for (int i = 0; i < this.NumParameters; i++)
                    sb.AppendFormat(", {0}", this.GetParameter(i).WKT);
                //if (!String.IsNullOrEmpty(Authority) && AuthorityCode > 0)
                //	sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
                if (_isInverse)
                    sb.Append("]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets an XML representation of this instance.
        /// </summary>
        public override string XML
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<CT_MathTransform>");
                if (_isInverse)
                    sb.AppendFormat("<CT_InverseTransform Name=\"{0}\">", ClassName);
                else
                    sb.AppendFormat("<CT_ParameterizedMathTransform Name=\"{0}\">", ClassName);
                for (int i = 0; i < this.NumParameters; i++)
                    sb.AppendFormat(this.GetParameter(i).XML);
                if (_isInverse)
                    sb.Append("</CT_InverseTransform>");
                else
                    sb.Append("</CT_ParameterizedMathTransform>");
                sb.Append("</CT_MathTransform>");
                return sb.ToString();
            }
        }

        #endregion

        #region IMathTransform members

        public abstract double[] MetersToDegrees(double[] p);
        public abstract double[] DegreesToMeters(double[] lonlat);


        /// <summary>
        /// Reverses the transformation
        /// </summary>
        public override void Invert()
        {
            _isInverse = !_isInverse;
        }

        /// <summary>
        /// Gets an inverse transformation.
        /// </summary>
        internal bool IsInverse
        {
            get { return _isInverse; }
        }

        /// <summary>
        /// Transforms a coordinates of point. 
        /// </summary>
        /// <param name="p">An array containing the point coordinates</param>
        /// <returns>An array containing coordinates of the transformed point</returns>
        public override double[] Transform(double[] p)
        {
            if (!_isInverse)
                return this.DegreesToMeters(p);
            else 
                return this.MetersToDegrees(p);
        }

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <remarks>
        /// This method is provided for efficiently transforming many points. The supplied array 
        /// of ordinal values will contain packed ordinal values. For example, if the source 
        /// dimension is 3, then the ordinals will be packed in this order (x0,y0,z0,x1,y1,z1 ...).
        /// The size of the passed array must be an integer multiple of DimSource. The returned 
        /// ordinal values are packed in a similar way. In some DCPs. the ordinals may be 
        /// transformed in-place, and the returned array may be the same as the passed array.
        /// So any client code should not attempt to reuse the passed ordinal values (although
        /// they can certainly reuse the passed array). If there is any problem then the server
        /// implementation will throw an exception. If this happens then the client should not
        /// make any assumptions about the state of the ordinal values.
        /// </remarks>
        /// <param name="ord">An array of ordinal values</param>
        /// <returns>A list of transformed coordinate point ordinal values</returns>
        public override List<double[]> TransformList(List<double[]> ord)
        {
            List<double[]> result = new List<double[]>(ord.Count);
            for (int i = 0; i < ord.Count; i++)
            {
                double[] point = ord[i];
                result.Add(Transform(point));
            }
            return result;
        }

        /// <summary>
        /// Checks whether the values of this instance is equal to the values of another instance.
        /// Only parameters used for coordinate system are used for comparison.
        /// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
        /// </summary>
        /// <param name="obj">Object to compare parameters</param>
        /// <returns>True if equal</returns>
        public bool EqualParams(object obj)
        {
            if (!(obj is MapProjection))
                return false;
            MapProjection proj = obj as MapProjection;
            if (proj.NumParameters != this.NumParameters)
                return false;
            for (int i = 0; i < _parameters.Count; i++)
            {
                ProjectionParameter param = _parameters.Find(delegate(ProjectionParameter par) { return par.Name.Equals(proj.GetParameter(i).Name, StringComparison.OrdinalIgnoreCase); });
                if (param == null)
                    return false;
                if (param.Value != proj.GetParameter(i).Value)
                    return false;
            }
            if (this.IsInverse != proj.IsInverse)
                return false;
            return true;
        }

        #endregion

        #region Auxiliary calculation functions

        /// <summary>
        /// PI
        /// </summary>
        protected const double PI = Math.PI;

        /// <summary>
        /// PI / 2
        /// </summary>
        protected const double HALF_PI = PI * 0.5;

        /// <summary>
        /// PI * 2
        /// </summary>
        protected const double TWO_PI = PI * 2.0;

        /// <summary>
        /// Tolerance
        /// </summary>
        protected const double Tolerance = 1.0e-10;

        /// <summary>
        /// S2R
        /// </summary>
        protected const double S2R = 4.848136811095359e-6;

        /// <summary>
        /// MAX_VAL
        /// </summary>
        protected const double MAX_VAL = 4;

        /// <summary>
        /// prjMAXLONG
        /// </summary>
        protected const double prjMAXLONG = 2147483647;

        /// <summary>
        /// DBLLONG
        /// </summary>
        protected const double DBLLONG = 4.61168601e18;

        /// <summary>
        /// Returns a cube of the value.
        /// </summary>
        /// <param name="x">A value to compute the cube</param>
        protected static double CUBE(double x)
        {
            return Math.Pow(x, 3);
        }

        /// <summary>
        /// Returns a quad of the value.
        /// </summary>
        /// <param name="x">A value to compute the quad</param>
        protected static double QUAD(double x)
        {
            return Math.Pow(x, 4);  
        }

        protected static double GMAX(ref double A, ref double B)
        {
            return Math.Max(A, B); 
        }

        protected static double GMIN(ref double A, ref double B)
        {
            return ((A) < (B) ? (A) : (B)); 
        }

        /// <summary>
        /// IMOD
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        protected static double IMOD(double A, double B)
        {
            return (A) - (((A) / (B)) * (B)); 

        }

        ///<summary>
        ///Returns a sign of value.
        ///</summary>
        protected static double sign(double x)
        {
            if (x < 0.0)
                return (-1);
            else return (1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        protected static double adjustLon(double x)
        {
            long count = 0;
            for (; ; )
            {
                if (Math.Abs(x) <= PI)
                    break;
                else
                    if (((long)Math.Abs(x / Math.PI)) < 2)
                        x = x - (sign(x) * TWO_PI);
                    else
                        if (((long)Math.Abs(x / TWO_PI)) < prjMAXLONG)
                        {
                            x = x - (((long)(x / TWO_PI)) * TWO_PI);
                        }
                        else
                            if (((long)Math.Abs(x / (prjMAXLONG * TWO_PI))) < prjMAXLONG)
                            {
                                x = x - (((long)(x / (prjMAXLONG * TWO_PI))) * (TWO_PI * prjMAXLONG));
                            }
                            else
                                if (((long)Math.Abs(x / (DBLLONG * TWO_PI))) < prjMAXLONG)
                                {
                                    x = x - (((long)(x / (DBLLONG * TWO_PI))) * (TWO_PI * DBLLONG));
                                }
                                else
                                    x = x - (sign(x) * TWO_PI);
                count++;
                if (count > MAX_VAL)
                    break;
            }
            return (x);
        }

        /// <summary>
        /// Function to compute the constant small m which is the radius of
        /// a parallel of latitude, phi, divided by the semimajor axis.
        /// </summary>
        protected static double msfnz(double eccent, double sinPhi, double cosPhi)
        {
            double con;

            con = eccent * sinPhi;
            return ((cosPhi / (Math.Sqrt(1.0 - con * con))));
        }

        /// <summary>
        /// Function to compute constant small q which is the radius of a 
        /// parallel of latitude, phi, divided by the semimajor axis. 
        /// </summary>
        protected static double qsfnz(double eccent, double sinPhi)
        {
            double con;

            if (eccent > 1.0e-7)
            {
                con = eccent * sinPhi;
                return ((1 - eccent * eccent) * (sinPhi / (1 - con * con) - (0.5 / eccent) *
                    Math.Log((1 - con) / (1 + con))));
            }
            else
                return 2 * sinPhi;
        }

        /// <summary>
        /// Function to calculate the sine and cosine in one call.  Some computer
        /// systems have implemented this function, resulting in a faster implementation
        /// than calling each function separately.  It is provided here for those
        /// computer systems which don't implement this function
        /// </summary>
        protected static void sincos(double val, out double sinValue, out double cosineValue)
        {
            sinValue = Math.Sin(val);
            cosineValue = Math.Cos(val);
        }

        /// <summary>
        /// Function to compute the constant small t for use in the forward
        /// computations in the Lambert Conformal Conic and the Polar
        /// Stereographic projections.
        /// </summary>
        protected static double tsfnz(double eccent, double phi, double sinPhi)
        {
            double con;
            double com;
            con = eccent * sinPhi;
            com = 0.5 * eccent;
            con = Math.Pow(((1.0 - con) / (1.0 + con)), com);
            return (Math.Tan(.5 * (HALF_PI - phi)) / con);
        }

        protected static double phi1z(double eccent, double qs, out long flag)
        {
            double eccnts;
            double dphi;
            double con;
            double com;
            double sinpi;
            double cospi;
            double phi;
            flag = 0;
            //double asinz();
            long i;

            phi = asinz(0.5 * qs);
            if (eccent < Tolerance)
                return (phi);
            eccnts = eccent * eccent;
            for (i = 1; i <= 25; i++)
            {
                sincos(phi, out sinpi, out cospi);
                con = eccent * sinpi;
                com = 1 - con * con;
                dphi = 0.5 * com * com / cospi * (qs / (1 - eccnts) - sinpi / com +
                    0.5 / eccent * Math.Log((1 - con) / (1 + con)));
                phi = phi + dphi;
                if (Math.Abs(dphi) <= 1e-7)
                    return (phi);
            }
            throw new ApplicationException("Convergence error");
        }

        ///<summary>
        ///Function to eliminate roundoff errors in asin
        ///</summary>
        protected static double asinz(double con)
        {
            if (Math.Abs(con) > 1.0)
            {
                if (con > 1.0)
                    con = 1.0;
                else
                    con = -1.0;
            }
            return (Math.Asin(con));
        }

        /// <summary>
        /// Function to compute the latitude angle, phi2, for the inverse of the
        /// Lambert Conformal Conic and Polar Stereographic projections.
        /// </summary>
        /// <param name="eccent">Spheroid eccentricity</param>
        /// <param name="ts">Constant value t</param>
        /// <param name="flag">Error flag number</param>
        protected static double phi2z(double eccent, double ts, out long flag)
        {
            double con;
            double dphi;
            double sinpi;
            long i;

            flag = 0;
            double eccnth = .5 * eccent;
            double chi = HALF_PI - 2 * Math.Atan(ts);
            for (i = 0; i <= 15; i++)
            {
                sinpi = Math.Sin(chi);
                con = eccent * sinpi;
                dphi = HALF_PI - 2 * Math.Atan(ts * (Math.Pow(((1.0 - con) / (1.0 + con)), eccnth))) - chi;
                chi += dphi;
                if (Math.Abs(dphi) <= 0.0000000001)
                    return (chi);
            }
            throw new ApplicationException("Convergence error");
        }

        ///<summary>
        ///Functions to compute the constants e0, e1, e2, and e3 which are used
        ///in a series for calculating the distance along a meridian.  The
        ///input x represents the eccentricity squared.
        ///</summary>
        protected static double e0fn(double x)
        {
            return (1.0 - 0.25 * x * (1.0 + x / 16.0 * (3.0 + 1.25 * x)));
        }

        protected static double e1fn(double x)
        {
            return (0.375 * x * (1.0 + 0.25 * x * (1.0 + 0.46875 * x)));
        }

        protected static double e2fn(double x)
        {
            return (0.05859375 * x * x * (1.0 + 0.75 * x));
        }

        protected static double e3fn(double x)
        {
            return (x * x * x * (35.0 / 3072.0));
        }

        /// <summary>
        /// Function to compute the constant e4 from the input of the eccentricity
        /// of the spheroid, x.  This constant is used in the Polar Stereographic
        /// projection.
        /// </summary>
        protected static double e4fn(double x)
        {
            double con;
            double com;
            con = 1.0 + x;
            com = 1.0 - x;
            return (Math.Sqrt((Math.Pow(con, con)) * (Math.Pow(com, com))));
        }

        /// <summary>
        /// Function computes the value of M which is the distance along a meridian
        /// from the Equator to latitude phi.
        /// </summary>
        protected static double mlfn(double e0, double e1, double e2, double e3, double phi)
        {
            return (e0 * phi - e1 * Math.Sin(2.0 * phi) + e2 * Math.Sin(4.0 * phi) - e3 * Math.Sin(6.0 * phi));
        }

        /// <summary>
        /// Function to calculate UTM zone number--NOTE Longitude entered in DEGREES!!!
        /// </summary>
        protected static long calcUtmZone(double lon)
        {
            return ((long)(((lon + 180.0) / 6.0) + 1.0));
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Converts a longitude value in degrees to radians.
        /// </summary>
        /// <param name="x">The value in degrees to convert to radians</param>
        /// <param name="edge">If true, -180 and +180 are valid, otherwise they are considered out of range</param>
        /// <returns>A value in radians</returns>
        static protected double LongitudeToRadians(double x, bool edge)
        {
            if (edge ? (x >= -180 && x <= 180) : (x > -180 && x < 180))
                return MathUtils.Degrees.ToRadians(x);
            throw new ArgumentOutOfRangeException("x", x.ToString(CultureInfo.InvariantCulture) + " not a valid longitude in degrees.");
        }

        /// <summary>
        /// Converts a latitude value in degrees to radians.
        /// </summary>
        /// <param name="y">The value in degrees to to radians</param>
        /// <param name="edge">If true, -90 and +90 are valid, otherwise they are considered out of range</param>
        /// <returns>A value in radians</returns>
        static protected double LatitudeToRadians(double y, bool edge)
        {
            if (edge ? (y >= -90 && y <= 90) : (y > -90 && y < 90))
                return MathUtils.Degrees.ToRadians(y);
            throw new ArgumentOutOfRangeException("y", y.ToString(CultureInfo.InvariantCulture) + " not a valid latitude in degrees.");
        }

        #endregion
    }
}
