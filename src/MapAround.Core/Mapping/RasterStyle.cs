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
** File: RasterStyle.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes that describes raster appearance on the map
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Drawing.Drawing2D;

    /// <summary>
    /// Defines a raster rendering style.
    /// </summary>
    public class RasterStyle : ICloneable
    {
        private ColorMatrix _colorAdjustmentMatrix = new ColorMatrix(
                        new float[][] { 
                                   new float[] {1,  0,  0,  0, 0},
                                   new float[] {0,  1,  0,  0, 0},
                                   new float[] {0,  0,  1,  0, 0},
                                   new float[] {0,  0,  0,  1, 0},
                                   new float[] {0,  0,  0,  0, 1}});

        private InterpolationMode _interpolationMode = InterpolationMode.Bilinear;

        /// <summary>
        /// Gets or sets a matrix that will be applied to adjust 
        /// the raster colors when rendering.
        /// </summary>
        public ColorMatrix ColorAdjustmentMatrix
        {
          get { return _colorAdjustmentMatrix; }
          set { _colorAdjustmentMatrix = value; }
        }

        /// <summary>
        /// Gets or sets an interpolation mode for the raster.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get { return _interpolationMode; }
            set { _interpolationMode = value; }
        }

        #region ICloneable Members

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>
        public object Clone()
        {
            ColorMatrix cm = this.ColorAdjustmentMatrix;
            RasterStyle rs = new RasterStyle()
            {
                InterpolationMode = this.InterpolationMode,
                ColorAdjustmentMatrix = new ColorMatrix(
                            new float[][] { 
                                   new float[] {cm.Matrix00,  cm.Matrix01,  cm.Matrix02,  cm.Matrix03, cm.Matrix04},
                                   new float[] {cm.Matrix10,  cm.Matrix11,  cm.Matrix12,  cm.Matrix13, cm.Matrix14},
                                   new float[] {cm.Matrix20,  cm.Matrix21,  cm.Matrix22,  cm.Matrix23, cm.Matrix24},
                                   new float[] {cm.Matrix30,  cm.Matrix31,  cm.Matrix32,  cm.Matrix33, cm.Matrix34},
                                   new float[] {cm.Matrix40,  cm.Matrix41,  cm.Matrix42,  cm.Matrix43, cm.Matrix44}})
            };
            return rs;
        }

        #endregion
    }
}