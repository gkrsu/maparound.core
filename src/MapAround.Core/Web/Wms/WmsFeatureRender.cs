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



using System.Collections.Generic;
using System.Drawing;
using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using MapAround.Geometry;
using MapAround.Mapping;

namespace MapAround.Web.Wms
{
    /// <summary>
    /// Special rendering for FeatureLayer using wms with caching.
    /// </summary>
    internal class WmsFeatureRender:DefaultFeatureRenderer
    {
        //All names have been.
        private List<TitleBufferElement> _allTitleInfo = new List<TitleBufferElement>();
        //Tinctures redoringa card.
        private MapRenderingSettings _renderingSettings;

        /// <summary>
        /// Rendorim new layer.
        ///  <remarks>
        ///  Clears the buffer of the old paint layer.
        /// 
        /// </remarks>
        /// </summary>
        public void BeginLayerRender()
        {
            _titleBuffer.Clear();
            _titleCount = 0;
        }

        /// <summary>
        /// Conctructor.
        /// </summary>
        /// <param name="renderingSettings">Setting the drawing.</param>
        public WmsFeatureRender(MapRenderingSettings renderingSettings)
        {
            _renderingSettings = renderingSettings;
        }

        /// <summary>
        /// Serialized data about the names on the card.
        /// </summary>
        public byte[] CurrentTitleInfo
        {
            get { return Seralize(_titleBuffer); }
        }
        

        /// <summary>
        /// Serialization of text.
        /// </summary>        
        private static byte[] Seralize(IList<TitleBufferElement> sElements )
        {
            var binaryFormatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                binaryFormatter.Serialize(ms,sElements);
                return ms.ToArray();
            }
                        
        }

        /// <summary>
        /// Deserialization text.
        /// </summary>

        private static IList<TitleBufferElement> Deserialize(byte[] bytes)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var ms = new MemoryStream(bytes))
            {
                var result = (IList<TitleBufferElement>)binaryFormatter.Deserialize(ms);
                return result;
            }

        }       

        /// <summary>
        ///Drawing names.
        ///  <remarks>
        /// Algorithm uses the base class.
        ///  </remarks>
        /// </summary>        
        public void RenderTitle(Image image, BoundingRectangle viewBox)
        {
            _titleBuffer.Clear();
            _titleBuffer.AddRange(_allTitleInfo);
            _titleCount =  _titleBuffer.Count;
            double scaleFactor = Math.Min(image.Width / viewBox.Width, image.Height / viewBox.Height);
            using (Graphics g = Graphics.FromImage(image))
            {
                SmoothingMode oldSmoothingMode = g.SmoothingMode;

                if (_renderingSettings.AntiAliasText)
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                else
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                base.FlushTitles(g,viewBox,scaleFactor);
                
                g.SmoothingMode = oldSmoothingMode;
            }
        }
        /// <summary>
        ///Add new data on the text in the general collection.
        /// </summary>
        /// <param name="titleinfo">Serialized data.</param>
        public void AddTitleInfo(byte[] titleinfo)
        {
            _allTitleInfo.AddRange(Deserialize(titleinfo));
        }
        
        /// <summary>
        /// Overriding the <see cref="DefaultFeatureRenderer.FlushTitles(System.Drawing.Graphics,MapAround.Geometry.BoundingRectangle,double)"/>.
        /// </summary>        
        protected override void FlushTitles(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            // In a place that would render the inscription simply collect them all together.
            _allTitleInfo.AddRange(_titleBuffer);            
           return;

            
        }

        

       
    }
}