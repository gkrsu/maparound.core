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



﻿using MapAround.Geometry;
using MapAround.Web.Wms;
using System.Collections.Generic;

namespace MapAround.Web.Wmts
{
    /// <summary>
    /// Stores metadata for a WMTS service.
    /// </summary>
    public class WmtsServiceDescription : BaseServiceDescription
    {

        /// <summary>
        /// Tile
        /// </summary>
        public Tile Tile;

        /// <summary>
        /// ZoomLevel
        /// </summary>
        public Dictionary<string, int> ZoomLevel;

        /// <summary>
        /// Definition of Well-known scale set GoogleCRS84Quad
        /// </summary>
        private readonly double[,] CRS84Scale = new double[,]  // CRS84[, 0] = ScaleDenominator, CRS84[, 1] = PixelSize(degrees)
        {
            {559082264.0287178, 1.406250000000000},
            {279541132.0143589, 0.703125000000000},
            {139770566.0071794, 0.351562500000000},
            {69885283.00358972, 0.175781250000000},
            {34942641.50179486, 0.087890625000000},
            {17471320.75089743, 0.043945312500000},
            {8735660.375448715, 0.021972656250000},
            {4367830.187724357, 0.010986328125000},
            {2183915.093862179, 0.005493164062500},
            {1091957.546931089, 0.002746582031250},
            {545978.7734655447, 0.001373291015625},
            {272989.3867327723, 0.000686645507812500},
            {136494.6933663862, 0.000343322753906250},
            {68247.34668319309, 0.000171661376953125},
            {34123.67334159654, 0.0000858306884765625},
            {17061.83667079827, 0.0000429153442382812},
            {8530.918335399136, 0.0000214576721191406},
            {4265.459167699568, 0.0000107288360595703},
            {2132.729583849784, 0.00000536441802978516}
        };

        /// <summary>
        /// Definition of Well-known scale set GoogleMapsCompatible
        /// </summary>
        private readonly double[,] EPSGScale = new double[,]  //EPSG[, 0] = ScaleDenominator, EPSG[, 1] = PixelSize(m)
        {
            {559082264.0287178, 156543.0339280410},
            {279541132.0143589, 78271.51696402048},
            {139770566.0071794, 39135.75848201023},
            {69885283.00358972, 19567.87924100512},
            {34942641.50179486, 9783.939620502561},
            {17471320.75089743, 4891.969810251280},
            {8735660.375448715, 2445.984905125640},
            {4367830.187724357, 1222.992452562820},
            {2183915.093862179, 611.4962262814100},
            {1091957.546931089, 305.7481131407048},
            {545978.7734655447, 152.8740565703525},
            {272989.3867327723, 76.43702828517624},
            {136494.6933663862, 38.21851414258813},
            {68247.34668319309, 19.10925707129406},
            {34123.67334159654, 9.554628535647032},
            {17061.83667079827, 4.777314267823516},
            {8530.918335399136, 2.388657133911758},
            {4265.459167699568, 1.194328566955879},
            {2132.729583849784, 0.5971642834779395}
        };

        /// <summary>
        /// Gets Scale Denominator for ith matrix
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double GetScaleDenominator(int i)
        {
            return EPSGScale[i, 0];
        }

        /// <summary>
        /// Gets Pixel Size for ith matrix
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double GetPixelSize(int i)
        {
            return EPSGScale[i, 1];
        }

        /// <summary>
        /// Sets matrix names and levels for them
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, int> EPSG
        {
            get
            {
                Dictionary<string, int> EPSGMatrix = new Dictionary<string, int>();
                EPSGMatrix.Add("EPSG:3857:0", 0);
                EPSGMatrix.Add("EPSG:3857:1", 1);
                EPSGMatrix.Add("EPSG:3857:2", 2);
                EPSGMatrix.Add("EPSG:3857:3", 3);
                EPSGMatrix.Add("EPSG:3857:4", 4);
                EPSGMatrix.Add("EPSG:3857:5", 5);
                EPSGMatrix.Add("EPSG:3857:6", 6);
                EPSGMatrix.Add("EPSG:3857:7", 7);
                EPSGMatrix.Add("EPSG:3857:8", 8);
                EPSGMatrix.Add("EPSG:3857:9", 9);
                EPSGMatrix.Add("EPSG:3857:10", 10);
                EPSGMatrix.Add("EPSG:3857:11", 11);
                EPSGMatrix.Add("EPSG:3857:12", 12);
                EPSGMatrix.Add("EPSG:3857:13", 13);
                EPSGMatrix.Add("EPSG:3857:14", 14);
                EPSGMatrix.Add("EPSG:3857:15", 15);
                EPSGMatrix.Add("EPSG:3857:16", 16);
                EPSGMatrix.Add("EPSG:3857:17", 17);
                EPSGMatrix.Add("EPSG:3857:18", 18);

                return EPSGMatrix;
            }
        }

        ///// <summary>
        ///// Sets matrix names and levels for them
        ///// </summary>
        ///// <returns></returns>
        //private Dictionary<string, int> SetCRS84()
        //{
        //    Dictionary<string, int> CRS84Matrix = new Dictionary<string, int>();
        //    CRS84Matrix.Add("CRS84:0", 0);
        //    CRS84Matrix.Add("CRS84:1", 1);
        //    CRS84Matrix.Add("CRS84:2", 2);
        //    CRS84Matrix.Add("CRS84:3", 3);
        //    CRS84Matrix.Add("CRS84:4", 4);
        //    CRS84Matrix.Add("CRS84:5", 5);
        //    CRS84Matrix.Add("CRS84:6", 6);
        //    CRS84Matrix.Add("CRS84:7", 7);
        //    CRS84Matrix.Add("CRS84:8", 8);
        //    CRS84Matrix.Add("CRS84:9", 9);
        //    CRS84Matrix.Add("CRS84:10", 10);
        //    CRS84Matrix.Add("CRS84:11", 11);
        //    CRS84Matrix.Add("CRS84:12", 12);
        //    CRS84Matrix.Add("CRS84:13", 13);
        //    CRS84Matrix.Add("CRS84:14", 14);
        //    CRS84Matrix.Add("CRS84:15", 15);
        //    CRS84Matrix.Add("CRS84:16", 16);
        //    CRS84Matrix.Add("CRS84:17", 17);
        //    CRS84Matrix.Add("CRS84:18", 18);

        //    return CRS84Matrix;
        //}

        /// <summary>
        /// Initializes a new instance of the MapAround.Web.Wmts.WmtsServiceDescription.
        /// </summary>
        /// <param name="title">Mandatory Human-readable title for pick lists</param>
        /// <param name="onlineResource">Top-level web address of service or service provider.</param>
        public WmtsServiceDescription(string title, string onlineResource):base(title,onlineResource)
        {
           
            Tile = null;
            ZoomLevel = EPSG;
        }
    }
}
