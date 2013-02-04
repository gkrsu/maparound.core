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
** File: Interfaces.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Spatial indices interfaces
**
=============================================================================*/

using System;
using System.Collections.Generic;

using MapAround.Geometry;

namespace MapAround.Indexing
{

    /// <summary>
    /// The MapAround.Indexing namespace contains 
    /// interfaces and classes for spatial indexing.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides access to properties and methods of object
    /// supporting spatial indexing.
    /// </summary>
    public interface IIndexable : ICloneable
    {
        /// <summary>
        /// A bounding rectangle of this object.
        /// </summary>
        BoundingRectangle BoundingRectangle
        {
            get;
        }
    }

    /// <summary>
    /// Provides access to properties and methods of object
    /// that implements a spatial indexing funcions.
    /// <para>
    /// This interface should be implemented by objects that are used
    /// for indexing rectangular bounding objects.
    /// </para>
    /// </summary>
    public interface ISpatialIndex : ICloneable
    {
        /// <summary>
        /// Built an index for a specified objects.
        /// </summary>
        /// <param name="objects">Enumerator of objects for indexing</param>
        void Build<T>(IEnumerable<T> objects) where T : IIndexable;

        /// <summary>
        /// Inserts an object into index.
        /// </summary>
        /// <param name="obj">An object to insert</param>
        void Insert<T>(T obj) where T : IIndexable;

        /// <summary>
        /// Removes an object from index.
        /// </summary>
        /// <param name="obj">An object to remove</param>
        /// <returns>true, if the object was removed, false otherwise</returns>
        bool Remove<T>(T obj) where T : IIndexable;

        /// <summary>
        /// Adds objects, which bounding rectangles intersect 
        /// specified rectangle, to the list.
        /// </summary>
        /// <param name="box">A bounding rectangle defining queryable area</param>
        /// <param name="objects">A list for adding objects</param>
        void QueryObjectsInRectangle<T>(BoundingRectangle box, IList<T> objects) where T : IIndexable;

        /// <summary>
        /// Adds objects, which bounding rectangles contains 
        /// specified point, to the list.
        /// </summary>
        /// <param name="point">A point</param>
        /// <param name="objects">A list for adding objects</param>
        void QueryObjectsContainingPoint<T>(ICoordinate point, IList<T> objects) where T : IIndexable;

        /// <summary>
        /// Gets a bounding rectangle defining the indexed space.
        /// </summary>
        BoundingRectangle IndexedSpace
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value defining a maximum index depth.
        /// </summary>
        int MaxDepth
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a threshold 
        /// value of a cell area.
        /// </summary>
        double BoxSquareThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value defining 
        /// minimum object number in a cell.
        /// </summary>
        int MinObjectCount
        {
            get;
            set;
        }
    }
}