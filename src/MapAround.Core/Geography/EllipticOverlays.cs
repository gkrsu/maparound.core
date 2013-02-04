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
** File: EllipticOverlays.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Class that implements calculations of the elliptic overlays
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
    /// Calculates the overlays of geographies.
    /// <remarks>
    /// Instances of this class are used to calculate the results of Boolean operations 
    /// (union, intersection, difference, symmetric difference) over the point sets. 
    /// Point sets can be defined as a geopolygons, geopolylines or geopoints.
    /// </remarks>
    /// </summary>
    public class EllipticOverlayCalculator
    {
        /// <summary>
        /// Calculates a union of two geographies.
        /// </summary>
        /// <param name="geography1">First geography</param>
        /// <param name="geography2">Second geography</param>
        /// <returns>A union of two geographies</returns>
        public ICollection<IGeography> Union(IGeography geography1, IGeography geography2)
        {
            return CalculateOverlay(geography1, geography2, OverlayType.Union);
        }

        /// <summary>
        /// Calculates an intersection of two geographies.
        /// </summary>
        /// <param name="geography1">First geography</param>
        /// <param name="geography2">Second geography</param>
        /// <returns>An intersection of two geographies</returns>
        public ICollection<IGeography> Intersection(IGeography geography1, IGeography geography2)
        {
            return CalculateOverlay(geography1, geography2, OverlayType.Intersection);
        }

        /// <summary>
        /// Calculates a difference of two geographies.
        /// </summary>
        /// <param name="geography1">First geography</param>
        /// <param name="geography2">Second geography</param>
        /// <returns>A difference of two geographies</returns>
        public ICollection<IGeography> Difference(IGeography geography1, IGeography geography2)
        {
            return CalculateOverlay(geography1, geography2, OverlayType.Difference);
        }

        /// <summary>
        /// Calculates a symmetric difference of two geographies.
        /// </summary>
        /// <param name="geography1">First geography</param>
        /// <param name="geography2">Second geography</param>
        /// <returns>A symmetric difference of two geographies</returns>
        public ICollection<IGeography> SymmetricDifference(IGeography geography1, IGeography geography2)
        {
            return CalculateOverlay(geography1, geography2, OverlayType.SymmetricDifference);
        }

        /// <summary>
        /// Calculates an overlay of two geographies.
        /// </summary>
        /// <param name="geography1">First geography</param>
        /// <param name="geography2">Second geography</param>
        /// <param name="operation">Overlay type</param>
        /// <returns>A resulting overlay of two geographies</returns>
        public ICollection<IGeography> CalculateOverlay(IGeography geography1, IGeography geography2, OverlayType operation)
        {
            if (!(geography1 is GeoPolygon))
                if (!(geography1 is GeoPolyline))
                    if (!(geography1 is GeoPoint))
                        if (!(geography1 is GeoMultiPoint))
                            throw new NotSupportedException(string.Format("Overlay calculations for \"{0}\" is not supported.", geography1.GetType().FullName));

            if (!(geography2 is GeoPolygon))
                if (!(geography2 is GeoPolyline))
                    if (!(geography2 is GeoPoint))
                        if (!(geography2 is GeoMultiPoint))
                            throw new NotSupportedException(string.Format("Overlay calculations for \"{0}\" is not supported.", geography2.GetType().FullName));

            return calculateOverlay(geography1, geography2, operation, false);
        }

        private ICollection<IGeography> calculateOverlay(IGeography geometry1, IGeography geometry2, OverlayType operation, bool p)
        {
            GeographyCollection egc = new GeographyCollection();
            egc.Add(geometry1);
            egc.Add(geometry2);

            GnomonicProjection projection = GeometrySpreader.GetProjection(egc);
            GeometryCollection gc = GeometrySpreader.GetGeometries(egc, projection);

            OverlayCalculator oc = new OverlayCalculator();
            ICollection<IGeometry> planarResult = oc.CalculateOverlay(gc[0], gc[1], operation);

            egc = GeometrySpreader.GetGeographies(planarResult, projection);
            return egc;
        }
    }
}

#endif