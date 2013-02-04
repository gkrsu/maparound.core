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



using MapAround.Geometry;

namespace MapAround.Web.Wms
{
    /// <summary>
    /// Base MapServive description class
    /// </summary>
    public abstract  class BaseServiceDescription
    {
        /// <summary>
        ///Optional narrative description providing additional information.
        /// </summary>
        public string Abstract;

        /// <summary>
        /// <para>The optional element "AccessConstraints" may be omitted if it do not apply to the server. If
        /// the element is present, the reserved word "none" (case-insensitive) shall be used if there are no
        /// access constraints, as follows: "none".</para>
        /// <para>When constraints are imposed, no precise syntax has been defined for the text content of these elements, but
        /// client applications may display the content for user information and action.</para>
        /// </summary>
        public string AccessConstraints;

        /// <summary>
        /// Optional WMS contact information
        /// </summary>
        public WmsContactInformation ContactInformation;

        /// <summary>
        /// The optional element "Fees" may be omitted if it do not apply to the server. If
        /// the element is present, the reserved word "none" (case-insensitive) shall be used if there are no
        /// fees, as follows: "none".
        /// </summary>
        public string Fees;

        /// <summary>
        /// Optional list of keywords or keyword phrases describing the server as a whole to help catalog searching
        /// </summary>
        public string[] Keywords;

        /// <summary>
        /// Maximum number of layers allowed (0=no restrictions)
        /// </summary>
        public uint LayerLimit;

        /// <summary>
        /// Maximum height allowed in pixels (0=no restrictions)
        /// </summary>
        public uint MaxHeight;

        /// <summary>
        /// Maximum width allowed in pixels (0=no restrictions)
        /// </summary>
        public uint MaxWidth;

        /// <summary>
        /// Mandatory Top-level web address of service or service provider.
        /// </summary>
        public string OnlineResource;

        /// <summary>
        /// Mandatory Human-readable title for pick lists
        /// </summary>
        public string Title;


        /// <summary>
        /// Initializes a new instance of the MapAround.Web.Wms.WmsServiceDescription.
        /// </summary>
        /// <param name="title">Mandatory Human-readable title for pick lists</param>
        /// <param name="onlineResource">Top-level web address of service or service provider.</param>
        protected  BaseServiceDescription(string title, string onlineResource)
        {
            Title = title;
            OnlineResource = onlineResource;
            Keywords = null;
            Abstract = string.Empty;
            ContactInformation = new WmsContactInformation();
            Fees = string.Empty;
            AccessConstraints = string.Empty;
            LayerLimit = 0;
            MaxWidth = 0;
            MaxHeight = 0;
        }
    }
    /// <summary>
    /// Stores metadata for a WMS service.
    /// </summary>
    public class WmsServiceDescription:BaseServiceDescription
    {
      

        /// <summary>
        /// 
        /// </summary>
        public BoundingRectangle BoundingBox;

        /// <summary>
        /// Initializes a new instance of the MapAround.Web.Wms.WmsServiceDescription.
        /// </summary>
        /// <param name="title">Mandatory Human-readable title for pick lists</param>
        /// <param name="onlineResource">Top-level web address of service or service provider.</param>
        public WmsServiceDescription(string title, string onlineResource):base(title,onlineResource)
        {
           
            BoundingBox = null;
        }
    }
}