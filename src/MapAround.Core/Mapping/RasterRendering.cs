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
** File: Rendering.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes and interfaces for raster rendering
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    using MapAround.Geometry;
    using System.Drawing.Imaging;

    /// <summary>
    /// Provides access to members of object that draws rasters on the map.
    /// MapAround.Mapping.Map.RasterRenderer can be assigned with implementing objects.
    /// </summary>
    public interface IRasterRenderer
    {
        /// <summary>
        /// Draws a raster.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a raster</param>
        /// <param name="bitmap">A System.Drawing.Bitmap instance to draw</param>
        /// <param name="style">An object defining a raster rendering style</param>
        /// <param name="viewBox">A bounding rectangle defining the drawing area</param>
        /// <param name="bitmapBounds">A bounding rectangle defining the bounds of the image</param>
        /// <param name="scaleFactor">A number of pixels per map unit</param>
        void RenderRaster(Graphics g, Bitmap bitmap, RasterStyle style, BoundingRectangle viewBox, BoundingRectangle bitmapBounds, double scaleFactor);
    }

    /// <summary>
    /// Implements the MapAround.Mapping.IRasterRenderer interface.
    /// </summary>
    internal class DefaultRasterRenderer : IRasterRenderer
    {

        #region IRasterRenderer Members

        public void RenderRaster(Graphics g, Bitmap bitmap, RasterStyle style, BoundingRectangle viewBox, BoundingRectangle bitmapBounds, double scaleFactor)
        {
            ICoordinate minPoint = bitmapBounds.Min;
            ICoordinate maxPoint = bitmapBounds.Max;

            if (viewBox.Intersects(bitmapBounds))
            {
                Point minP = new Point((int)((minPoint.X - viewBox.MinX) * scaleFactor),
                                       (int)((viewBox.MaxY - minPoint.Y) * scaleFactor));

                Point maxP = new Point((int)((maxPoint.X - viewBox.MinX) * scaleFactor),
                           (int)((viewBox.MaxY - maxPoint.Y) * scaleFactor));

                g.InterpolationMode = style.InterpolationMode;

                Rectangle r = new Rectangle(minP.X, maxP.Y, maxP.X - minP.X, minP.Y - maxP.Y);

                using (ImageAttributes imageAttributes = new ImageAttributes())
                {
                    imageAttributes.SetColorMatrix(style.ColorAdjustmentMatrix,
                                                   ColorMatrixFlag.Default,
                                                   ColorAdjustType.Bitmap);

                    g.DrawImage(bitmap, r, 0, 0, bitmap.Width, bitmap.Height,
                                GraphicsUnit.Pixel, imageAttributes);
                }
            }
        }

        #endregion
    }
}