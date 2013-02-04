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
** File: TileCaching.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Interfaces and classes for tile caching.
**
=============================================================================*/

using MapAround.Geometry;

namespace MapAround.Caching
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    using MapAround.Mapping;
    using MapAround.Indexing;

    /// <summary>
    /// Represents an object that provides an access to the tile cache.
    /// </summary>
    public interface ITileCacheAccessor
    {
        /// <summary>
        /// Extracts a binary representation of tile from cache.
        /// </summary>
        /// <param name="layer">Layer name</param>
        /// <param name="area">Area description</param>
        /// <param name="key">Access key</param>
        /// <param name="contentType">Type data in array</param>
        /// <returns>Byte array that contains a binary representation of a tile image</returns>
        byte[] ExtractTileBytes(string layer, BoundingRectangle area, string key, string contentType);

        /// <summary>
        /// Saves a binary representation of an image into cache.
        /// </summary>
        /// <param name="layer">Layer name</param>
        /// <param name="area">Area description</param>
        /// <param name="key">Access key</param>
        /// <param name="tile">Byte array that contains a binary representation of a tile image</param>
        /// <param name="contentType">Type data in array</param>
        void SaveTileBytes(string layer, BoundingRectangle area, string key, byte[] tile, string contentType);
    }

}