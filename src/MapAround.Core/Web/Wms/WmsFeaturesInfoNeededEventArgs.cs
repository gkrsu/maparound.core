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



using System;
using System.Collections.Generic;
using System.IO;
using MapAround.Mapping;

namespace MapAround.Web.Wms
{
    /// <summary>
    /// Provides data for the FeaturesInfoNeeded event.
    /// </summary>
    public class WmsFeaturesInfoNeededEventArgs : EventArgs
    {
        private string _responseContentType;
        private Stream _responseOutputStream;
        private string _mimeTypeNeeded;
        private List<Feature> _features;

        /// <summary>
        /// Gets a reference to the output stream 
        /// into which the responce is writing.
        /// </summary>
        public Stream ResponseOutputStream
        {
            get { return _responseOutputStream; }
        }

        /// <summary>
        /// Gets or sets a mime-type of the response.
        /// </summary>
        public string ResponseContentType
        {
            get { return _responseContentType; }
            set { _responseContentType = value; }
        }

        /// <summary>
        /// Gets the requested mime-type of the response.
        /// </summary>
        public string MimeTypeNeeded
        {
            get { return _mimeTypeNeeded; }
        }

        /// <summary>
        /// Gets a list containing features to provide info.
        /// </summary>
        public List<Feature> Features
        {
            get { return _features; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Web.Wms.WmsFeaturesInfoNeededEventArgs.
        /// </summary>
        /// <param name="features">A list containing features to provide info</param>
        /// <param name="mimeTypeNeeded">Requested mime-type</param>
        /// <param name="responseOutputStream">The output stream into which to write the responce</param>
        /// <param name="responseContentType">Current responce mime-type</param>
        public WmsFeaturesInfoNeededEventArgs(List<Feature> features, string mimeTypeNeeded, Stream responseOutputStream, string responseContentType)
        {
            _features = features;
            _mimeTypeNeeded = mimeTypeNeeded;
            _responseContentType = responseContentType;
            _responseOutputStream = responseOutputStream;
        }
    }
}