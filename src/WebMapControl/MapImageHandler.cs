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
** File: MapImageHandler.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Handling map image requests
**
=============================================================================*/

namespace MapAround.UI.Web
{
    using System.Web;
    using System.Web.SessionState;
    using System.Drawing;
    using System.Drawing.Imaging;

    using MapAround.Mapping;

    /// <summary>
    /// Handles the http-requests of map image.
    /// </summary>
    public class MapImageHandler : IHttpHandler, IRequiresSessionState
    {
        private int getIntParam(string paramName, int defaultValue, HttpRequest request)
        {
            string s = request.QueryString[paramName];
            int result;
            if (int.TryParse(s, out result))
                return result;
            else
                return defaultValue;
        }

        private int getMapWidth(HttpRequest request)
        {
            return getIntParam("width", 800, request);
        }

        private int getMapHeight(HttpRequest request)
        {
            return getIntParam("height", 600, request);
        }

        private MapWorkspace getWorkspace(HttpContext context)
        {
            string workspaceName = getWorkspaceName(context.Request);
            if (string.IsNullOrEmpty(workspaceName))
                return null;

            if (context.Session[getWorkspaceName(context.Request)] != null)
                return (MapWorkspace)context.Session[workspaceName];
            return null;
        }


        private string getWorkspaceName(HttpRequest request)
        {
            return request.QueryString["workspace"];
        }

        /// <summary>
        /// Handles a request.
        /// </summary>
        /// <param name="context">An http context</param>
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "image/jpeg";

            string workspaceName = getWorkspaceName(context.Request);
            if (string.IsNullOrEmpty(workspaceName))
                return;

            int mapWidth = getMapWidth(context.Request);
            int mapHeight = getMapHeight(context.Request);

            MapWorkspace workspace = getWorkspace(context);

            if (workspace == null)
                return;

            double scale = mapWidth / workspace.ViewBox.Width;

            if(workspace.Map != null)
                workspace.Map.LoadFeatures(scale, workspace.ViewBox);

            using (Bitmap bmp = new Bitmap(mapWidth, mapHeight))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                    g.Clear(Color.White);

                if (workspace.Map != null)
                    workspace.Map.Render(bmp, workspace.ViewBox);

                bmp.Save(context.Response.OutputStream, ImageFormat.Jpeg);
            }
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