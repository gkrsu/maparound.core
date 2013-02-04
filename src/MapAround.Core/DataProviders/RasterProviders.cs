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
** File: SpatialDataProvider.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Interfaces and base classes for providing access to the raster spatial data
**
=============================================================================*/

namespace MapAround.DataProviders
{
    using System.Drawing;
    using System;
    using System.IO;
    using System.Collections.Generic;

    using MapAround.Mapping;
    using MapAround.Geometry;

    /// <summary>
    /// Represents an object that provides an access 
    /// to the raster.
    /// </summary>
    public interface IRasterProvider
    {
        /// <summary>
        /// Retreives a chunk of the raster.
        /// </summary>
        /// <param name="srcX">A minimum X coordinate of the querying area</param>
        /// <param name="srcY">A minimum Y coordinate of the querying area</param>
        /// <param name="srcWidth">A width of the querying area</param>
        /// <param name="srcHeight">A height of the querying area</param>
        /// <param name="maxDestWidth">A maximum width in pixels of the resulting raster</param>
        /// <param name="maxDestHeight">A maximum height in pixels of the resulting raster</param>
        /// <param name="bounds">A bounds of querying area on the map</param>
        /// <param name="receiver">An object receiving raster</param>
        void QueryRaster(int srcX, 
            int srcY, 
            int srcWidth, 
            int srcHeight,
            int maxDestWidth,
            int maxDestHeight,
            BoundingRectangle bounds,
            IRasterReceiver receiver);

        /// <summary>
        /// Gets a width of the raster in pixels.
        /// </summary>
        int Width
        { get; }

        /// <summary>
        /// Gets a height of the raster in pixels.
        /// </summary>
        int Height
        { get; }
    }

    /// <summary>
    /// Provides access to the in-memory stored raster.
    /// </summary>
    public class InMemoryRasterProvider : IRasterProvider
    {
        private Bitmap _bitmap = null;

        /// <summary>
        /// Gets or sets a System.Drawing.Bitmap instance 
        /// which represents an in-memoty raster.
        /// </summary>
        public Bitmap Bitmap
        {
            get { return _bitmap; }
            set { _bitmap = value; }
        }

        #region IRasterProvider Members

        /// <summary>
        /// Gets a width of the raster in pixels.
        /// </summary>
        public int Width
        { 
            get
            {
                if (_bitmap == null)
                    throw new InvalidOperationException("Bitmap is not set.");

                return _bitmap.Width;
            } 
        }

        /// <summary>
        /// Gets a height of the raster in pixels.
        /// </summary>
        public int Height
        { 
            get
            {
                if (_bitmap == null)
                    throw new InvalidOperationException("Bitmap is not set.");

                return _bitmap.Height;
            } 
        }

        /// <summary>
        /// Retreives a chunk of the raster.
        /// </summary>
        /// <param name="srcX">A minimum X coordinate of the querying area</param>
        /// <param name="srcY">A minimum Y coordinate of the querying area</param>
        /// <param name="srcWidth">A width of the querying area</param>
        /// <param name="srcHeight">A height of the querying area</param>
        /// <param name="maxDestWidth">A maximum width in pixels of the resulting raster</param>
        /// <param name="maxDestHeight">A maximum height in pixels of the resulting raster</param>
        /// <param name="bounds">A bounds of querying area on the map</param>
        /// <param name="receiver">An object receiving raster</param>
        public void QueryRaster(int srcX, 
            int srcY, 
            int srcWidth, 
            int srcHeight,
            int maxDestWidth,
            int maxDestHeight, 
            BoundingRectangle bounds,
            IRasterReceiver receiver)
        {
            if (_bitmap == null)
                throw new InvalidOperationException("Bitmap is not set");

            Bitmap result = new Bitmap(Math.Max(maxDestWidth, 1), Math.Max(maxDestHeight, 1), _bitmap.PixelFormat);

            using(Graphics g = Graphics.FromImage(result))
            {
                RectangleF srcRect = new RectangleF(srcX, srcY, srcWidth, srcHeight);
                RectangleF destRect = new RectangleF(0, 0, maxDestWidth, maxDestHeight);
                g.DrawImage(_bitmap, destRect, srcRect, GraphicsUnit.Pixel);
            }

            receiver.AddRasterPreview(result, bounds, Width, Height);
        }

        #endregion
    }

    /// <summary>
    /// InMemory raster provider holder.
    /// </summary>
    public class SmallRasterProviderHolder : RasterProviderHolderBase
    {
        private static string[] _parameterNames = { "file_name" };
        private Dictionary<string, string> _parameters = null;
        private Dictionary<string, InMemoryRasterProvider> _rpoviders = 
            new Dictionary<string,InMemoryRasterProvider>();

        /// <summary>
        /// Sets the parameter values.
        /// </summary>
        /// <param name="parameters">Parameter values</param>
        public override void SetParameters(Dictionary<string, string> parameters)
        {
            string missingField = "Parameter \"{0}\" missimg";
            foreach (string s in _parameterNames)
                if (!parameters.ContainsKey("file_name"))
                    throw new ArgumentException(string.Format(missingField, s));

            _parameters = parameters;
        }

        /// <summary>
        /// Gets a list containing the names of parameters.
        /// </summary>
        /// <returns>List containing the names of parameters</returns>
        public override string[] GetParameterNames()
        {
            return _parameterNames;
        }

        private IRasterProvider createProviderInstance()
        {
            if (_parameters == null)
                throw new InvalidOperationException("Parameter values not set");

            InMemoryRasterProvider provider = null;

            string filename = _parameters["file_name"];
            if(_rpoviders.ContainsKey(filename))
            {
                provider = _rpoviders[filename];
                
                if(provider.Bitmap != null)
                {
                    bool bitmapIsOk = true;    
                    try
                    {
                        int h = provider.Bitmap.Height;
                    }
                    catch(ObjectDisposedException)
                    {
                        bitmapIsOk = false;
                    }
                    if (bitmapIsOk) return provider;
                }
            }

            _rpoviders.Remove(filename);
            provider = new InMemoryRasterProvider();

            if (File.Exists(filename))
            {
                provider.Bitmap = (Bitmap)System.Drawing.Image.FromFile(_parameters["file_name"]);
                _rpoviders.Add(filename, provider);
            }
            else
                throw new FileNotFoundException(_parameters["file_name"]);

            return provider;
        }

        /// <summary>
        /// Performs a finalization procedure for the raster provider.
        /// This implementation do nothing.
        /// </summary>
        /// <param name="provider">Raster provider instance</param>
        public override void ReleaseProviderIfNeeded(IRasterProvider provider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.DataProviders.InMemoryRasterProvider.
        /// </summary>
        public SmallRasterProviderHolder()
            : base("MapAround.DataProviders.InMemoryRasterProvider")
        {
            GetProviderMethod = createProviderInstance;
        }
    }
}