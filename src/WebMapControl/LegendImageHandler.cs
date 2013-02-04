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
** File: LegendImageHandler.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Handling legend image requests
**
=============================================================================*/

namespace MapAround.UI.Web
{
    using System.Web;
    using System.Web.SessionState;
    using System.Drawing;
    using System.Drawing.Imaging;

    using MapAround.Mapping;
    using MapAround.UI;

    /// <summary>
    /// Handles the http-requests of legend image.
    /// </summary>
    public class LegendImageHandler : IHttpHandler, IRequiresSessionState
    {
        private ImageLegend getLegend(HttpContext context)
        {
            string legendName = getLegendName(context.Request);
            if (string.IsNullOrEmpty(legendName))
                return null;

            if (context.Session[getLegendName(context.Request)] != null)
                return (ImageLegend)context.Session[legendName];
            return null;
        }


        private string getLegendName(HttpRequest request)
        {
            return request.QueryString["legend"];
        }

        /// <summary>
        /// Handles a request.
        /// Process the request.
        /// </summary>
        /// <param name="context">An http context</param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "image/jpeg";

            string legendName = getLegendName(context.Request);
            if (string.IsNullOrEmpty(legendName))
                return;

            ImageLegend legend = getLegend(context);

            if (legend == null)
                return;

            using(Bitmap bmp = legend.DrawLegend())
                bmp.Save(context.Response.OutputStream, ImageFormat.Jpeg);
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the System.Web.IHttpHandler
        /// instance. Always return true.
        /// </summary>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

    }
}