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
** File: IO.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Coordinate systems serialization
**
=============================================================================*/

namespace MapAround.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using MapAround.IO;
    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Projections;

    /// <summary>
    /// Deserializes coordinate system objects from well-known text.
    /// </summary>
    public class CoordinateSystemWktDeserializer
    {
        /// <summary>
        /// Constructs a coordinate system object from well-known text.
        /// </summary>
        /// <param name="wkt">Well-known text representation of object</param>
        /// <returns>An object constructed from well-known text</returns>
        /// <exception cref="System.ArgumentException">Raises when parsing of string fails.</exception>
        public static IInfo Parse(string wkt)
        {
            IInfo returnObject = null;
            StringReader reader = new StringReader(wkt);
            WktStreamTokenizer tokenizer = new WktStreamTokenizer(reader);
            tokenizer.NextToken();
            string objectName = tokenizer.GetStringValue();
            switch (objectName)
            {
                case "UNIT":
                    returnObject = ReadUnit(tokenizer);
                    break;
                //case "VERT_DATUM":
                //    IVerticalDatum verticalDatum = ReadVerticalDatum(tokenizer);
                //    returnObject = verticalDatum;
                //    break;
                case "SPHEROID":
                    returnObject = ReadEllipsoid(tokenizer);
                    break;
                case "DATUM":
                    returnObject = ReadHorizontalDatum(tokenizer); ;
                    break;
                case "PRIMEM":
                    returnObject = ReadPrimeMeridian(tokenizer);
                    break;
                case "VERT_CS":
                case "GEOGCS":
                case "PROJCS":
                case "COMPD_CS":
                case "GEOCCS":
                case "FITTED_CS":
                case "LOCAL_CS":
                    returnObject = ReadCoordinateSystem(wkt, tokenizer);
                    break;
                default:
                    throw new ArgumentException(String.Format("'{0}' is not recognized.", objectName));

            }
            reader.Close();
            return returnObject;
        }

        /// <summary>
        /// Reads a unit.
        /// </summary>
        private static IUnit ReadUnit(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                ReadAuthority(tokenizer, ref authority, ref authorityCode);
                tokenizer.ReadToken("]");
            }
            return new Unit(unitsPerUnit, unitName, authority, authorityCode, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Reads am authority and authority code.
        /// </summary>
        private static void ReadAuthority(WktStreamTokenizer tokenizer, ref string authority, ref long authorityCode)
        {
            if (tokenizer.GetStringValue() != "AUTHORITY")
                tokenizer.ReadToken("AUTHORITY");
            tokenizer.ReadToken("[");
            authority = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
#if(!Silverlight)
            long.TryParse(tokenizer.ReadDoubleQuotedWord(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture.NumberFormat,
                out authorityCode);
#else
			try { authorityCode = long.Parse(tokenizer.ReadDoubleQuotedWord(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat); }
			catch { }
#endif
            tokenizer.ReadToken("]");
        }

        /// <summary>
        /// Reads a linear unit.
        /// </summary>
        private static ILinearUnit ReadLinearUnit(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                ReadAuthority(tokenizer, ref authority, ref authorityCode);
                tokenizer.ReadToken("]");
            }
            return new LinearUnit(unitsPerUnit, unitName, authority, authorityCode, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Reads an anglular unit.
        /// </summary>
        private static IAngularUnit ReadAngularUnit(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                ReadAuthority(tokenizer, ref authority, ref authorityCode);
                tokenizer.ReadToken("]");
            }
            return new AngularUnit(unitsPerUnit, unitName, authority, authorityCode, String.Empty, String.Empty, String.Empty);
        }

        /// <summary>
        /// Reads an axis info.
        /// </summary>
        private static AxisInfo ReadAxis(WktStreamTokenizer tokenizer)
        {
            if (tokenizer.GetStringValue() != "AXIS")
                tokenizer.ReadToken("AXIS");
            tokenizer.ReadToken("[");
            string axisName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string unitname = tokenizer.GetStringValue();
            tokenizer.ReadToken("]");
            switch (unitname.ToUpper(CultureInfo.InvariantCulture))
            {
                case "DOWN": return new AxisInfo(axisName, AxisOrientationEnum.Down);
                case "EAST": return new AxisInfo(axisName, AxisOrientationEnum.East);
                case "NORTH": return new AxisInfo(axisName, AxisOrientationEnum.North);
                case "OTHER": return new AxisInfo(axisName, AxisOrientationEnum.Other);
                case "SOUTH": return new AxisInfo(axisName, AxisOrientationEnum.South);
                case "UP": return new AxisInfo(axisName, AxisOrientationEnum.Up);
                case "WEST": return new AxisInfo(axisName, AxisOrientationEnum.West);
                default:
                    throw new ArgumentException("Invalid axis name '" + unitname + "' in WKT");
            }
        }


        /// <summary>
        /// Reads a coordinate system.
        /// </summary>
        private static ICoordinateSystem ReadCoordinateSystem(string coordinateSystem, WktStreamTokenizer tokenizer)
        {
            switch (tokenizer.GetStringValue())
            {
                case "GEOGCS":
                    return ReadGeographicCoordinateSystem(tokenizer);
                case "PROJCS":
                    return ReadProjectedCoordinateSystem(tokenizer);
                case "COMPD_CS":
                /*	ICompoundCoordinateSystem compoundCS = ReadCompoundCoordinateSystem(tokenizer);
                    returnCS = compoundCS;
                    break;*/
                case "VERT_CS":
                /*	IVerticalCoordinateSystem verticalCS = ReadVerticalCoordinateSystem(tokenizer);
                    returnCS = verticalCS;
                    break;*/
                case "GEOCCS":
                case "FITTED_CS":
                case "LOCAL_CS":
                    throw new NotSupportedException(String.Format("{0} coordinate system is not supported.", coordinateSystem));
                default:
                    throw new InvalidOperationException(String.Format("{0} coordinate system is not recognized.", coordinateSystem));
            }
        }

        /// <summary>
        /// Reads a WGS84 conversion info.
        /// </summary>
        private static Wgs84ConversionInfo ReadWGS84ConversionInfo(WktStreamTokenizer tokenizer)
        {
            //TOWGS84[0,0,0,0,0,0,0]
            tokenizer.ReadToken("[");
            Wgs84ConversionInfo info = new Wgs84ConversionInfo();
            tokenizer.NextToken();
            info.Dx = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dy = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dz = tokenizer.GetNumericValue();
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                info.Ex = tokenizer.GetNumericValue();

                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                info.Ey = tokenizer.GetNumericValue();

                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                info.Ez = tokenizer.GetNumericValue();

                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                    info.Ppm = tokenizer.GetNumericValue();
                }
            }
            if (tokenizer.GetStringValue() != "]")
                tokenizer.ReadToken("]");
            return info;
        }


        /*
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenizer"></param>
        /// <returns></returns>
        private static ICompoundCoordinateSystem ReadCompoundCoordinateSystem(WktStreamTokenizer tokenizer)
        {
			
            //COMPD_CS[
            //"OSGB36 / British National Grid + ODN",
            //PROJCS[]
            //VERT_CS[]
            //AUTHORITY["EPSG","7405"]
            //]

            tokenizer.ReadToken("[");
            string name=tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string headCSCode =  tokenizer.GetStringValue();
            ICoordinateSystem headCS = ReadCoordinateSystem(headCSCode,tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string tailCSCode =  tokenizer.GetStringValue();
            ICoordinateSystem tailCS = ReadCoordinateSystem(tailCSCode,tokenizer);
            tokenizer.ReadToken(",");
            string authority=String.Empty;
            string authorityCode=String.Empty; 
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            tokenizer.ReadToken("]");
            ICompoundCoordinateSystem compoundCS = new CompoundCoordinateSystem(headCS,tailCS,String.Empty,authority,authorityCode,name,String.Empty,String.Empty); 
            return compoundCS;
			
        }*/

        /// <summary>
        /// Reads an ellipsiod.
        /// </summary>
        private static IEllipsoid ReadEllipsoid(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double majorAxis = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double e = tokenizer.GetNumericValue();

            //tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string authority = String.Empty;
            long authorityCode = -1;
            if (tokenizer.GetStringValue() == ",") //Read authority
            {
                //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                ReadAuthority(tokenizer, ref authority, ref authorityCode);
                tokenizer.ReadToken("]");
            }
            IEllipsoid ellipsoid = new Ellipsoid(majorAxis, 0.0, e, true, LinearUnit.Metre, name, authority, authorityCode, String.Empty, string.Empty, string.Empty);
            return ellipsoid;
        }

        /// <summary>
        /// Reads a projection.
        /// </summary>
        private static IProjection ReadProjection(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("PROJECTION");
            tokenizer.ReadToken("[");//[
            string projectionName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken("]");//]
            tokenizer.ReadToken(",");//,
            tokenizer.ReadToken("PARAMETER");
            List<ProjectionParameter> paramList = new List<ProjectionParameter>();
            while (tokenizer.GetStringValue() == "PARAMETER")
            {
                tokenizer.ReadToken("[");
                string paramName = tokenizer.ReadDoubleQuotedWord();
                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                double paramValue = tokenizer.GetNumericValue();
                tokenizer.ReadToken("]");
                tokenizer.ReadToken(",");
                paramList.Add(new ProjectionParameter(paramName, paramValue));
                tokenizer.NextToken();
            }
            string authority = String.Empty;
            long authorityCode = -1;
            IProjection projection = new Projection(projectionName, paramList, projectionName, authority, authorityCode, String.Empty, String.Empty, string.Empty);
            return projection;
        }

        /// <summary>
        /// Reads a projected coordinate system.
        /// </summary>
        private static IProjectedCoordinateSystem ReadProjectedCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("GEOGCS");
            IGeographicCoordinateSystem geographicCS = ReadGeographicCoordinateSystem(tokenizer);
            tokenizer.ReadToken(",");
            IProjection projection = ReadProjection(tokenizer);
            IUnit unit = ReadLinearUnit(tokenizer);

            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            List<AxisInfo> axes = new List<AxisInfo>(2);
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                while (tokenizer.GetStringValue() == "AXIS")
                {
                    axes.Add(ReadAxis(tokenizer));
                    tokenizer.NextToken();
                    if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                    ReadAuthority(tokenizer, ref authority, ref authorityCode);
                    tokenizer.ReadToken("]");
                }
            }
            //This is default axis values if not specified.
            if (axes.Count == 0)
            {
                axes.Add(new AxisInfo("X", AxisOrientationEnum.East));
                axes.Add(new AxisInfo("Y", AxisOrientationEnum.North));
            }
            IProjectedCoordinateSystem projectedCS = new ProjectedCoordinateSystem(geographicCS.HorizontalDatum, geographicCS, unit as LinearUnit, projection, axes, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            return projectedCS;
        }

        /// <summary>
        /// Reads a geographic coordinate system.
        /// </summary>
        private static IGeographicCoordinateSystem ReadGeographicCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("DATUM");
            IHorizontalDatum horizontalDatum = ReadHorizontalDatum(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("PRIMEM");
            IPrimeMeridian primeMeridian = ReadPrimeMeridian(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("UNIT");
            IAngularUnit angularUnit = ReadAngularUnit(tokenizer);

            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            List<AxisInfo> info = new List<AxisInfo>(2);
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                while (tokenizer.GetStringValue() == "AXIS")
                {
                    info.Add(ReadAxis(tokenizer));
                    tokenizer.NextToken();
                    if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                    ReadAuthority(tokenizer, ref authority, ref authorityCode);
                    tokenizer.ReadToken("]");
                }
            }

            // значения по умолчанию для осей
            if (info.Count == 0)
            {
                info.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
                info.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
            }
            IGeographicCoordinateSystem geographicCS = new GeographicCoordinateSystem(angularUnit, horizontalDatum,
                    primeMeridian, info, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            return geographicCS;
        }

        /// <summary>
        /// Reads a horizontal datum.
        /// </summary>
        private static IHorizontalDatum ReadHorizontalDatum(WktStreamTokenizer tokenizer)
        {
            Wgs84ConversionInfo wgsInfo = null;
            string authority = String.Empty;
            long authorityCode = -1;

            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("SPHEROID");
            IEllipsoid ellipsoid = ReadEllipsoid(tokenizer);
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "TOWGS84")
                {
                    wgsInfo = ReadWGS84ConversionInfo(tokenizer);
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    //tokenizer.ReadAuthority(ref authority, ref authorityCode);
                    ReadAuthority(tokenizer, ref authority, ref authorityCode);
                    tokenizer.ReadToken("]");
                }
            }

            IHorizontalDatum horizontalDatum = new HorizontalDatum(ellipsoid, wgsInfo, DatumType.HD_Geocentric, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);

            return horizontalDatum;
        }

        /// <summary>
        /// Reads a prime meridian.
        /// </summary>
        private static IPrimeMeridian ReadPrimeMeridian(WktStreamTokenizer tokenizer)
        {
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double longitude = tokenizer.GetNumericValue();

            tokenizer.NextToken();
            string authority = String.Empty;
            long authorityCode = -1;
            if (tokenizer.GetStringValue() == ",")
            {
                ReadAuthority(tokenizer, ref authority, ref authorityCode);
                tokenizer.ReadToken("]");
            }

            IPrimeMeridian primeMeridian = new PrimeMeridian(longitude, AngularUnit.Degrees, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);

            return primeMeridian;
        }
    }
}
