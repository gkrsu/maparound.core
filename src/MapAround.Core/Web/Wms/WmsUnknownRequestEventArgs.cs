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
using System.Collections.Specialized;
using System.IO;

namespace MapAround.Web.Wms
{
    /// <summary>
    /// Provides data for the OnUnknownRequest event.
    /// </summary>
    public class WmsUnknownRequestEventArgs : EventArgs
    {
        private NameValueCollection _requestParams;
        private Stream _responseOutputStream;
        private string responseContentType = string.Empty;
        private bool _isHandled = true;

        /// <summary>
        /// Gets or sets a value indicating whether the request is handled.
        /// </summary>
        public bool IsHandled
        {
            get { return _isHandled; }
            set { _isHandled = value; }
        }

        /// <summary>
        /// Gets or sets a mime-type of the response.
        /// </summary>
        public string ResponseContentType
        {
            get { return responseContentType; }
            set { responseContentType = value; }
        }

        /// <summary>
        /// Gets an output stream to write the response.
        /// </summary>
        public Stream ResponseOutputStream
        {
            get { return _responseOutputStream; }
        }

        /// <summary>
        /// Gets a collection containing the request parameters.
        /// </summary>
        public NameValueCollection RequestParams
        {
            get { return _requestParams; }
        }

        /// <summary>
        /// Initializes a new instance os MapAround.Web.Wms.WmsExtenedRequestEventArgs.
        /// </summary>
        /// <param name="requestParams">a collection containing the request parameters</param>
        /// <param name="responseOutputStream">An output stream to write the response</param>
        public WmsUnknownRequestEventArgs(NameValueCollection requestParams, Stream responseOutputStream)
        {
            _requestParams = requestParams;
            _responseOutputStream = responseOutputStream;
        }
    }
}