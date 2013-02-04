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
** File: MapControl.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: ASP.NET server MapControl
**
=============================================================================*/

namespace MapAround.UI.Web
{
    using System;
    using System.ComponentModel;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Drawing;
    using System.Drawing.Design;
    using System.IO;
    using System.Globalization;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Web.UI.HtmlControls;
    using System.Runtime.Serialization;

    using MapAround.Mapping;
    using MapAround.Geometry;
    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Transformations;

    /// <summary>
    /// The MapAround.UI.Web namespace contains classes wich may
    /// used to develop user interface for the ASP.NET applications.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Modes of map control.
    /// </summary>
    public enum MapControlMode
    {
        /// <summary>
        /// Pan mode.
        /// </summary>
        Pan,
        /// <summary>
        /// Select mode.
        /// </summary>
        Select,
        /// <summary>
        /// Draw polyline mode.
        /// </summary>
        DrawPolyline,
        /// <summary>
        /// Draw polygon mode.
        /// </summary>
        DrawPolygon
    }

    /// <summary>
    /// ASP.NET server MapControl
    /// </summary>
    [DefaultProperty("Text")]
    [ToolboxData("<{0}:MapControl runat=server></{0}:_MapControl>")]
    public class MapControl : WebControl, INamingContainer, ICallbackEventHandler
    {
        #region Properties

        /// <summary>
        /// A value indicating whether to display the toolbar
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("true")]
        [Localizable(true)]
        [Description("A value indicating whether to display the toolbar.")]
        public bool ToolBar
        {
            get
            {
                object temp = ViewState["ToolBar"];
                if (temp == null)
                {
                    ViewState["ToolBar"] = true;
                    return true;
                }
                return (bool)ViewState["ToolBar"];
            }

            set
            {
                ViewState["ToolBar"] = value;
            }
        }

        /// <summary>
        /// A value indicating whether to display the polyline drawing tool.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue("true")]
        [Localizable(true)]
        [Description("A value indicating whether to display the polyline drawing tool.")]
        public bool DrawPolylineTool
        {
            get
            {
                object temp = ViewState["DrawPolylineTool"];
                if (temp == null)
                {
                    ViewState["DrawPolylineTool"] = true;
                    return true;
                }
                return (bool)ViewState["DrawPolylineTool"];
            }

            set
            {
                ViewState["DrawPolylineTool"] = value;
            }
        }

        /// <summary>
        /// A value indicating whether to display the polygon drawing tool
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue("true")]
        [Localizable(true)]
        [Description("A value indicating whether to display the polygon drawing tool.")]
        public bool DrawPolygonTool
        {
            get
            {
                object temp = ViewState["DrawPolygonTool"];
                if (temp == null)
                {
                    ViewState["DrawPolygonTool"] = true;
                    return true;
                }
                return (bool)ViewState["DrawPolygonTool"];
            }

            set
            {
                ViewState["DrawPolygonTool"] = value;
            }
        }

        /// <summary>
        /// A value indicating whether to display the layer control.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("true")]
        [Localizable(true)]
        [Description("A value indicating whether to display the layer control.")]
        public bool LayersControl
        {
            get
            {
                object temp = ViewState["LayersControl"];
                if (temp == null)
                {
                    ViewState["LayersControl"] = true;
                    return true;
                }
                return (bool)ViewState["LayersControl"];
            }

            set
            {
                ViewState["LayersControl"] = value;
            }
        }

        /// <summary>
        /// A value indicating whether to display scalebar
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("false")]
        [Localizable(false)]
        [Description("A value indicating whether to display scalebar.")]
        public bool ScaleBar
        {
            get
            {
                object temp = ViewState["ScaleBar"];
                if (temp == null)
                {
                    ViewState["ScaleBar"] = true;
                    return true;
                }
                return (bool)ViewState["ScaleBar"];
            }

            set
            {
                ViewState["ScaleBar"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a color of the scale label.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "22;22;22")]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets a color of the scale label.")]
        public Color ScaleLabelColor
        {
            get
            {
                object temp = ViewState["ScaleLabelColor"];
                if (temp == null)
                {
                    Context.Session["ScaleLabelColor"] = Color.FromArgb(22, 22, 22);
                    return Color.FromArgb(22, 22, 22);
                }
                return (Color)ViewState["ScaleLabelColor"];
            }

            set
            {
                ViewState["ScaleLabelColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a color of the scale segment.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "Gray")]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets a color of the scale segment.")]
        public Color ScaleSegmentColor
        {
            get
            {
                object temp = ViewState["ScaleSegmentColor"];
                if (temp == null)
                {
                    Context.Session["ScaleSegmentColor"] = Color.FromArgb(0, 0, 0);
                    return Color.FromArgb(0, 0, 0);
                }
                return (Color)ViewState["ScaleSegmentColor"];
            }

            set
            {
                ViewState["ScaleSegmentColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a color of tool windows.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(typeof(Color), "Gray")]
        [Localizable(true)]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets a color of tool windows.")]
        public Color ToolsColor
        {
            get
            {
                object temp = ViewState["ToolsColor"];
                if (temp == null)
                {
                    Context.Session["ToolsColor"] = Color.FromArgb(0, 0, 0);
                    return Color.FromArgb(0, 0, 0);
                }
                return (Color)ViewState["ToolsColor"];
            }

            set
            {
                ViewState["ToolsColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a tooltip for the panning tool button.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Pan")]
        [Localizable(true)]
        [Description("Gets or sets a tooltip for the panning tool button.")]
        public string DragButtonTooltip
        {
            get
            {
                object temp = ViewState["DragButtonTooltip"];
                if (temp == null)
                {
                    ViewState["DragButtonTooltip"] = "Pan";
                    return "Pan";
                }
                return (string)ViewState["DragButtonTooltip"];
            }

            set
            {
                ViewState["DragButtonTooltip"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a tooltip for the select tool button.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Select feature")]
        [Localizable(true)]
        [Description("Gets or sets a tooltip for the select tool button.")]
        public string SelectObjectButtonTooltip
        {
            get
            {
                object temp = ViewState["SelectObjectButtonTooltip"];
                if (temp == null)
                {
                    ViewState["SelectObjectButtonTooltip"] = "Select feature";
                    return "Select feature";
                }
                return (string)ViewState["SelectObjectButtonTooltip"];
            }

            set
            {
                ViewState["SelectObjectButtonTooltip"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a tooltip for the polyline drawing tool button.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Line path drawing tool")]
        [Localizable(true)]
        [Description("Gets or sets a tooltip for the polyline drawing tool button.")]
        public string DrawPolylineButtonTooltip
        {
            get
            {
                object temp = ViewState["DrawPolylineButtonTooltip"];
                if (temp == null)
                {
                    ViewState["DrawPolylineButtonTooltip"] = "Line path drawing tool";
                    return "Line path drawing tool";
                }
                return (string)ViewState["DrawPolylineButtonTooltip"];
            }

            set
            {
                ViewState["DrawPolylineButtonTooltip"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a tooltip for the polygon drawing tool button.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Polygon drawing tool")]
        [Localizable(true)]
        [Description("Gets or sets a tooltip for the polygon drawing tool button.")]
        public string DrawPolygonButtonTooltip
        {
            get
            {
                object temp = ViewState["DrawPolygonButtonTooltip"];
                if (temp == null)
                {
                    ViewState["DrawPolygonButtonTooltip"] = "Polygon drawing tool";
                    return "Polygon drawing tool";
                }
                return (string)ViewState["DrawPolygonButtonTooltip"];
            }

            set
            {
                ViewState["DrawPolygonButtonTooltip"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the color of points of client drawing tools
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue(typeof(Color), "Red")]
        [Localizable(true)]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets the color of points of client drawing tools.")]
        public Color DrawingToolsPointColor
        {
            get
            {
                object temp = ViewState["DrawingToolsPointColor"];
                if (temp == null)
                {
                    Context.Session["DrawingToolsPointColor"] = Color.FromArgb(255, 0, 0);
                    return Color.FromArgb(255, 0, 0);
                }
                return (Color)ViewState["DrawingToolsPointColor"];
            }

            set
            {
                ViewState["DrawingToolsPointColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the color of lines of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue(typeof(Color), "Brown")]
        [Localizable(true)]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets the color of lines of client drawing tools.")]
        public Color DrawingToolsLineColor
        {
            get
            {
                object temp = ViewState["DrawingToolsLineColor"];
                if (temp == null)
                {
                    Context.Session["DrawingToolsLineColor"] = Color.FromArgb(255, 0, 0);
                    return Color.FromArgb(255, 0, 0);
                }
                return (Color)ViewState["DrawingToolsLineColor"];
            }

            set
            {
                ViewState["DrawingToolsLineColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the color of fill of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue(typeof(Color), "Red")]
        [Localizable(true)]
        [TypeConverter(typeof(WebColorConverter))]
        [Editor(typeof(System.Drawing.Design.ColorEditor), typeof(UITypeEditor))]
        [Description("Gets or sets the color of fill of client drawing tools.")]
        public Color DrawingToolsFillColor
        {
            get
            {
                object temp = ViewState["DrawingToolsFillColor"];
                if (temp == null)
                {
                    Context.Session["DrawingToolsFillColor"] = Color.FromArgb(255, 0, 0);
                    return Color.FromArgb(255, 0, 0);
                }
                return (Color)ViewState["DrawingToolsFillColor"];
            }

            set
            {
                ViewState["DrawingToolsFillColor"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the Size (in pixels) of points of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue(5)]
        [Localizable(true)]
        [Description("Gets or sets the Size (in pixels) of points of client drawing tools.")]
        public int DrawingToolsPointSize
        {
            get
            {
                object temp = ViewState["DrawingToolsPointSize"];
                if (temp == null)
                {
                    ViewState["DrawingToolsPointSize"] = 5;
                    return 5;
                }
                return (int)ViewState["DrawingToolsPointSize"];
            }

            set
            {
                ViewState["DrawingToolsPointSize"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the thickness (in pixels) of lines of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue(2)]
        [Localizable(true)]
        [Description("Gets or sets the thickness (in pixels) of lines of client drawing tools.")]
        public int DrawingToolsLineWidth
        {
            get
            {
                object temp = ViewState["DrawingToolsLineWidth"];
                if (temp == null)
                {
                    ViewState["DrawingToolsLineWidth"] = 2;
                    return 2;
                }
                return (int)ViewState["DrawingToolsLineWidth"];
            }

            set
            {
                ViewState["DrawingToolsLineWidth"] = value;
            }
        }

        /// <summary>
        /// Linear unit label of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue("m")]
        [Localizable(true)]
        [Description("Linear unit label of client drawing tools.")]
        public string LineUnitLabel
        {
            get
            {
                object temp = ViewState["LineUnitLabel"];
                if (temp == null)
                {
                    ViewState["LineUnitLabel"] = "m";
                    return "m";
                }
                return (string)ViewState["LineUnitLabel"];
            }

            set
            {
                ViewState["LineUnitLabel"] = value;
            }
        }

        /// <summary>
        /// Area unit label of client drawing tools.
        /// </summary>
        [Category("Client instruments")]
        [DefaultValue("Sq.m.")]
        [Localizable(true)]
        [Description("Area unit label of client drawing tools.")]
        public string AreaUnitLabel
        {
            get
            {
                object temp = ViewState["AreaUnitLabel"];
                if (temp == null)
                {
                    ViewState["AreaUnitLabel"] = "Sq.m.";
                    return "Sq.m.";
                }
                return (string)ViewState["AreaUnitLabel"];
            }

            set
            {
                ViewState["AreaUnitLabel"] = value;
            }
        }

        /// <summary>
        /// Drag button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Drag button image url.")]
        public string DragButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["DragButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["DragButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Info button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Info button image url.")]
        public string InfoButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["InfoButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["InfoButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Line measurer button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Line measurer button image url.")]
        public string LineMeasurerButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["LineMeasurerButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["LineMeasurerButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Area measurer button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Area measurer button image url.")]
        public string AreaMeasurerButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["AreaMeasurerButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["AreaMeasurerButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Drag button image url (selected state).
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Drag button image url (selected state).")]
        public string SelectedDragButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["SelectedDragButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["SelectedDragButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Line measurer button image url (selected state).
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Line measurer button image url (selected state).")]
        public string SelectedLineMeasurerButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["SelectedLineMeasurerButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["SelectedLineMeasurerButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Area measurer button image url (selected state).
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Area measurer button image url (selected state).")]
        public string SelectedAreaMeasurerButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["SelectedAreaMeasurerButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["SelectedAreaMeasurerButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Info button image url (selected state).
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Info button image url (selected state).")]
        public string SelectedInfoButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["SelectedInfoButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["SelectedInfoButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Close info button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Close info button image url.")]
        public string CloseInfoButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["CloseInfoButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["CloseInfoButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Post feature button image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Post feature button image url.")]
        public string PostFeatureButtonImageUrl
        {
            get
            {
                string s = (string)ViewState["PostFeatureButtonImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["PostFeatureButtonImageUrl"] = value;
            }
        }

        /// <summary>
        /// Map loading progress image url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.ImageUrlEditor), typeof(UITypeEditor))]
        [Description("Map loading progress image url.")]
        public string MapLoadingImageUrl
        {
            get
            {
                string s = (string)ViewState["MapLoadingImageUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["MapLoadingImageUrl"] = value;
            }
        }

        /// <summary>
        /// Line measurer cursor url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.UrlEditor), typeof(UITypeEditor))]
        [Description("Line measurer cursor url.")]
        public string LineMeasurerCursorUrl
        {
            get
            {
                string s = (string)ViewState["LineMeasurerCursorUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["LineMeasurerCursorUrl"] = value;
            }
        }

        /// <summary>
        /// Area measurer cursor url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.UrlEditor), typeof(UITypeEditor))]
        [Description("Area measurer cursor url.")]
        public string AreaMeasurerCursorUrl
        {
            get
            {
                string s = (string)ViewState["AreaMeasurerCursorUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["AreaMeasurerCursorUrl"] = value;
            }
        }

        /// <summary>
        /// Caption of layer control.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Layers")]
        [Localizable(false)]
        [Description("Caption of layer control.")]
        public string LayersControlCaption
        {
            get
            {
                string s = (string)ViewState["LayersControlCaption"];
                return ((s == null) ? "Layers" : s);
            }

            set
            {
                ViewState["LayersControlCaption"] = value;
            }
        }

        /// <summary>
        /// Caption of the feature info window.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("Feature info")]
        [Localizable(false)]
        [Description("Caption of the feature info window.")]
        public string ObjectInfoCaption
        {
            get
            {
                string s = (string)ViewState["FeatureInfoCaption"];
                return ((s == null) ? "Feature info" : s);
            }

            set
            {
                ViewState["FeatureInfoCaption"] = value;
            }
        }

        /// <summary>
        /// Drag tool cursor url.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue("")]
        [Localizable(false)]
        [Editor(typeof(System.Web.UI.Design.UrlEditor), typeof(UITypeEditor))]
        [Description("Drag tool cursor url.")]
        public string DragCursorUrl
        {
            get
            {
                string s = (string)ViewState["DragCursorUrl"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["DragCursorUrl"] = value;
            }
        }

        /// <summary>
        /// The value (percent) of which changes the scale when zooming.
        /// </summary>
        [Bindable(true)]
        [Category("Behavior")]
        [DefaultValue("")]
        [Localizable(false)]
        [Description("The value (percent) of which changes the scale when zooming.")]
        public int ZoomStep
        {
            get
            {
                if (ViewState["ZoomStep"] == null)
                {
                    ViewState["ZoomStep"] = 10;
                    return 10;
                }
                return (int)ViewState["ZoomStep"];
            }

            set
            {
                ViewState["ZoomStep"] = value;
            }
        }

        /// <summary>
        /// Name of the http-handler that generates image of the map.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue("")]
        [Localizable(false)]
        [Description("Name of the http-handler that generates image of the map.")]
        public string HttpHandlerName
        {
            get
            {
                string s = (string)ViewState["HttpHandlerName"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["HttpHandlerName"] = value;
            }
        }

        /// <summary>
        /// Access key value for storing the workspase in the session state.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue("")]
        [Localizable(false)]
        [Description("Access key value for storing the workspase in the session state.")]
        public string WorkspaceUniqString
        {
            get
            {
                string s = (string)ViewState["WorkspaceUniqString"];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                ViewState["WorkspaceUniqString"] = value;
            }
        }

        /// <summary>
        /// A value that indicates whether the map will be aligned to the mouse 
        /// cursor point when zooming.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(true)]
        [Localizable(false)]
        [Description("A value that indicates whether the map will be aligned to the mouse cursor point when zooming.")]
        public bool AlignmentWhileZooming
        {
            get
            {
                if (ViewState["AlignmentWhileZooming"] == null)
                    return true;
                return (bool)ViewState["AlignmentWhileZooming"];
            }

            set
            {
                ViewState["AlignmentWhileZooming"] = value;
            }
        }

        /// <summary>
        /// A value that determines whether the feature posting 
        /// is enabled or not.
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        [Localizable(false)]
        [Description("A value that determines whether the feature posting is enabled or not.")]
        public bool AllowFeaturePosting
        {
            get
            {
                if (ViewState["AllowFeaturePosting"] == null)
                    return false;
                return (bool)ViewState["AllowFeaturePosting"];
            }

            set
            {
                ViewState["AllowFeaturePosting"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a collection containing the coordinates of the feature
        /// which is drawn at the ckient side.
        /// </summary>
        [Browsable(false)]
        public Collection<ICoordinate> ClientFeatureCoordinates
        {
            get { return _clientFeatureCoordinates; }
            set { _clientFeatureCoordinates = value; }
        }

        /// <summary>
        /// Gets or sets a workspace.
        /// </summary>
        [Browsable(false)]
        public MapWorkspace Workspace
        {
            get
            {
                return (MapWorkspace)Context.Session[WorkspaceUniqString];
            }

            set
            {
                setWorkspace(value);
            }
        }

        /// <summary>
        /// Gets or sets a string displayed into Feature Info window.
        /// </summary>
        [Browsable(false)]
        public string ObjectInfoHtml
        {
            get
            {
                string s = (string)Context.Session["MapObjectInfoHtml" + ClientID];
                return ((s == null) ? string.Empty : s);
            }

            set
            {
                Context.Session["RawMapObjectInfoHtml" + ClientID] = value;
                Context.Session["MapObjectInfoHtml" + ClientID] = encodeValue(value);
            }
        }

        private string rawObjectInfoHtml
        {
            get
            {
                string s = (string)Context.Session["RawMapObjectInfoHtml" + ClientID];
                return ((s == null) ? string.Empty : s);
            }
        }

        /// <summary>
        /// Gets or sets a name of the javascript variable.
        /// </summary>
        [Browsable(false)]
        public string JsVarName
        {
            get
            {
                if (ViewState["JsVarName"] == null)
                    return "map";
                return (string)ViewState["JsVarName"];
            }
        }


         #endregion

        #region Events

        /// <summary>
        /// Raises when the data provider may release managed and unmanaged resources.
        /// </summary>
        public event EventHandler<FeatureDataSourceEventArgs> LayerDataSourceReadyToRelease;

        /// <summary>
        /// Raises when the data provider needs to perform data query.
        /// </summary>
        public event EventHandler<FeatureDataSourceEventArgs> LayerDataSourceNeeded;

        /// <summary>
        /// Raises before polygon feature rendering.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePolygonRender;

        /// <summary>
        /// Raises before polyline feature rendering.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePolylineRender;

        /// <summary>
        /// Raises before point feature rendering.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePointRender;

        /// <summary>
        /// Raises when a user selects feature on a map.
        /// </summary>
        public event EventHandler<FeatureSelectedEventArgs> FeatureSelected;

        /// <summary>
        /// Raises when a new feature is posted to the server.
        /// </summary>
        public event EventHandler<FeaturePostEventArgs> FeaturePosted;

        #endregion

        private string _layers = string.Empty;
        private Collection<ICoordinate> _clientFeatureCoordinates = new Collection<ICoordinate>();
        private string _mode = string.Empty;

        private System.Web.UI.WebControls.Image _imgMap1;
        private System.Web.UI.WebControls.Image _imgMap2;

        private HtmlGenericControl _lineDrawingCanvas;
        private HtmlGenericControl _contourDrawingCanvas;
        private HtmlGenericControl _measureResult;

        private System.Web.UI.WebControls.Image _imgMapLoading;
        private HtmlGenericControl _layersControl;
        private HtmlGenericControl _btnSwitchLayers;
        private HtmlGenericControl _layerRecordsHolder;
        private HtmlGenericControl _scaleSegment;
        private HtmlGenericControl _scaleLabel;

        private HtmlGenericControl _toolsHolder;
        private HtmlGenericControl _btnDrag;
        private HtmlGenericControl _btnSelect;
        private HtmlGenericControl _btnDrawLine;
        private HtmlGenericControl _btnDrawContour;

        private HtmlGenericControl _objectInfoPopup;
        private HtmlGenericControl _objectInfoHolder;

        private void setJsVariableName(string name)
        {
            ViewState["JsVarName"] = name;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void CreateChildControls()
        {
            if (!Page.IsCallback)
            {
                addChildControls();
                registerScripts();
            }
        }

        private void addChildControls()
        {
            setJsVariableName(ClientID);

            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            Random r = new Random();
            string rndStr = r.Next().ToString();

            Style.Add("overflow", "hidden");
            Style.Add("z-index", "101");
            Style.Add("position", "relative");
            Style.Add("display", "block");
            Style.Add("background", ColorTranslator.ToHtml(BackColor));

            _imgMap1 = new System.Web.UI.WebControls.Image();
            _imgMap2 = new System.Web.UI.WebControls.Image();
            _imgMap1.Attributes["galleryimg"] = "false";
            _imgMap2.Attributes["galleryimg"] = "false";

            string dragCursorStyle = "default";
            if (!string.IsNullOrEmpty(DragCursorUrl))
                dragCursorStyle = "url(" + ResolveClientUrl(DragCursorUrl) + "), default";

            _imgMap1.ID = "img1";
            _imgMap1.Style.Add("position", "absolute");
            _imgMap1.Style.Add("top", "0px");
            _imgMap1.Style.Add("left", "0px");
            _imgMap1.Style.Add("z-index", "50");
            _imgMap1.Style.Add("cursor", dragCursorStyle);
            _imgMap1.Style.Add("visibility", "visible");

            _imgMap1.Attributes.Add("onmousedown", JsVarName + ".move(this, event)");

            _imgMap1.ImageUrl = HttpHandlerName +
                                "?width=" +
                                MapWidth.ToString(CultureInfo.InvariantCulture) +
                                "&height=" +
                                MapHeight.ToString(CultureInfo.InvariantCulture) +
                                "&workspace=" +
                                WorkspaceUniqString +
                                "&rn=" +
                                rndStr;

            _imgMap2.ID = "img2";
            _imgMap2.Style.Add("position", "absolute");
            _imgMap2.Style.Add("top", "0px");
            _imgMap2.Style.Add("left", "0px");
            _imgMap2.Style.Add("z-index", "51");
            _imgMap2.Style.Add("visibility", "hidden");
            _imgMap2.Style.Add("cursor", dragCursorStyle);

            _imgMap2.Attributes.Add("onmousedown", JsVarName + ".move(this, event)");

            _imgMap2.ImageUrl = _imgMap1.ImageUrl;

            // sizing of the image display map downloads
            int loaderHeight = 12;
            int loaderWidth = 12;
            Stream stream = this.OpenFile(ResolveUrl(MapLoadingImageUrl));
            using (System.Drawing.Image image = Bitmap.FromStream(stream))
            {
                loaderHeight = image.Height;
                loaderWidth = image.Width;
            }

            _imgMapLoading = new System.Web.UI.WebControls.Image();
            _imgMapLoading.ImageUrl = MapLoadingImageUrl;
            _imgMapLoading.Style.Add("position", "absolute");
            _imgMapLoading.Style.Add("top", ((MapHeight - loaderHeight) / 2).ToString(CultureInfo.InvariantCulture) + "px");
            _imgMapLoading.Style.Add("left", ((MapWidth - loaderWidth) / 2).ToString(CultureInfo.InvariantCulture) + "px");
            _imgMapLoading.Style.Add("z-index", "52");
            _imgMapLoading.Style.Add("visibility", "visible");

            Controls.Add(_imgMap1);
            Controls.Add(_imgMap2);
            Controls.Add(_imgMapLoading);


            _measureResult = new HtmlGenericControl("div");
            _measureResult.Style.Add("color", "#555555");
            _measureResult.Style.Add("font-family", "Arial, Helvetica, sans-serif");
            _measureResult.Style.Add("font-weight", "bold");
            _measureResult.Style.Add("font-size", "small");
            _measureResult.Style.Add("height", "16px");
            _measureResult.Style.Add("top", "100px");
            _measureResult.Style.Add("left", "100px");
            _measureResult.Style.Add("width", "auto");
            _measureResult.Style.Add("z-index", "62");
            _measureResult.Style.Add("filter", "alpha(opacity=85)");
            _measureResult.Style.Add("opacity", "0.85");
            _measureResult.Style.Add("visibility", "hidden");
            _measureResult.Style.Add("position", "absolute");
            _measureResult.Style.Add("border", "1px solid #777777");
            _measureResult.Style.Add("padding", "3px 3px 7px 3px");
            _measureResult.Style.Add("background", ColorTranslator.ToHtml(ToolsColor));
            _measureResult.InnerHtml = "<span></span>";

            if (AllowFeaturePosting)
            {
                HtmlGenericControl toolTipBtn = new HtmlGenericControl("input");
                toolTipBtn.Attributes.Add("type", "image");
                toolTipBtn.Attributes.Add("src", ResolveClientUrl(PostFeatureButtonImageUrl));
                toolTipBtn.Attributes.Add("onclick", "return " + JsVarName + ".postShape();");

                toolTipBtn.Style.Add("border-width", "0px");
                _measureResult.Controls.Add(toolTipBtn);
            }

            Controls.Add(_measureResult);

            if (LayersControl)
            {
                _layersControl = new System.Web.UI.HtmlControls.HtmlGenericControl("div");
                _layersControl.ID = "divLayers";
                _layersControl.Style.Add("opacity", "0.75");
                _layersControl.Style.Add("background", ColorTranslator.ToHtml(ToolsColor));
                _layersControl.Style.Add("z-index", "70");
                _layersControl.Style.Add("position", "absolute");
                _layersControl.Style.Add("overflow", "hidden");
                _layersControl.Style.Add("padding", "0px");
                _layersControl.Style.Add("filter", "alpha(opacity=75)");
                _layersControl.Style.Add("margin", "0px");
                _layersControl.Style.Add("border", "1px solid #777777");

                _layersControl.Style.Add("top", "5px");
                _layersControl.Style.Add("left", (MapWidth - 253).ToString() + "px");
                _layersControl.Style.Add("height", "25px");
                _layersControl.Style.Add("width", "241px");

                HtmlGenericControl p = new HtmlGenericControl("p");
                p.Style.Add("margin", "0px");
                p.Style.Add("padding", "0x 0px 0px 0px");
                p.Style.Add("text-align", "center");
                p.Style.Add("font-family", "Arial, Helvetica, sans-serif");
                p.Style.Add("font-weight", "bold");
                p.Style.Add("font-size", "small");
                p.Style.Add("width", "213px");
                p.Style.Add("cursor", "pointer");

                _btnSwitchLayers = new HtmlGenericControl("input");
                _btnSwitchLayers.ID = "btnSwitchLayerControl";

                _btnSwitchLayers.Style.Add("margin", "2px 0px 3px 20px");
                _btnSwitchLayers.Style.Add("color", "#555555");
                _btnSwitchLayers.Style.Add("background-color", ColorTranslator.ToHtml(ToolsColor));
                _btnSwitchLayers.Style.Add("border-width", "0px");

                _btnSwitchLayers.Style.Add("border-style", "None");
                _btnSwitchLayers.Style.Add("font-weight", "bold");
                _btnSwitchLayers.Style.Add("height", "auto");
                _btnSwitchLayers.Style.Add("cursor", "pointer");

                _btnSwitchLayers.Attributes.Add("type", "submit");
                _btnSwitchLayers.Attributes.Add("value", LayersControlCaption);

                p.Controls.Add(_btnSwitchLayers);
                HtmlGenericControl layersPlaceHolder = new HtmlGenericControl("div");
                layersPlaceHolder.ID = "layersPlaceHolder";
                layersPlaceHolder.Controls.Add(p);

                HtmlGenericControl linksHolder = new HtmlGenericControl("p");
                linksHolder.Style.Add("margin", "5px 0px 8px 5px");
                linksHolder.Style.Add("font-size", "x-small");
                linksHolder.Style.Add("font-family", "Arial, Helvetica, sans-serif");
                layersPlaceHolder.Controls.Add(linksHolder);

                HtmlGenericControl lnkShowAll = new HtmlGenericControl("a");
                lnkShowAll.Style.Add("text-decoration", "underline");
                lnkShowAll.Style.Add("color", "#0000FF");
                lnkShowAll.Style.Add("cursor", "pointer");

                lnkShowAll.Attributes.Add("onclick", "javascript:" + JsVarName + ".showAllLayers();");
                lnkShowAll.InnerText = "Show all";
                linksHolder.Controls.Add(lnkShowAll);

                HtmlGenericControl space = new HtmlGenericControl("span");
                space.InnerHtml = "&nbsp;&nbsp;";
                linksHolder.Controls.Add(space);

                HtmlGenericControl lnkCloseAll = new HtmlGenericControl("a");
                lnkCloseAll.Style.Add("text-decoration", "underline");
                lnkCloseAll.Style.Add("color", "#0000FF");
                lnkCloseAll.Style.Add("cursor", "pointer");
                lnkCloseAll.InnerText = "Hide all";

                lnkCloseAll.Attributes.Add("onclick", "javascript:" + JsVarName + ".hideAllLayers();");
                linksHolder.Controls.Add(lnkCloseAll);

                _layerRecordsHolder = new HtmlGenericControl("div");
                _layerRecordsHolder.ID = "layerRecordsHolder";
                _layerRecordsHolder.Style.Add("font-size", "x-small");
                _layerRecordsHolder.Style.Add("font-family", "Arial, Helvetica, sans-serif");
                layersPlaceHolder.Controls.Add(_layerRecordsHolder);

                _layersControl.Controls.Add(layersPlaceHolder);
                Controls.Add(_layersControl);


            }

            // scale
            if (ScaleBar)
            {
                int scaleSegmentLength = 0;
                string scaleLabel = "unknown";
                getScalePresentationData(Workspace.ViewBox, 110, ref scaleSegmentLength, ref scaleLabel);

                _scaleSegment = new HtmlGenericControl("div");
                _scaleSegment.ID = "scaleSegment";
                _scaleSegment.Style.Add("font-size", "0");
                _scaleSegment.Style.Add("z-index", "54");
                _scaleSegment.Style.Add("width", scaleSegmentLength.ToString() + "px");
                _scaleSegment.Style.Add("height", "3px");
                _scaleSegment.Style.Add("border-style", "solid");
                _scaleSegment.Style.Add("border-width", "1px");
                _scaleSegment.Style.Add("padding", "0px 0px 0px 0px");
                _scaleSegment.Style.Add("margin", "0px");
                _scaleSegment.Style.Add("opacity", "0.75");
                _scaleSegment.Style.Add("filter", " alpha(opacity=75)");
                _scaleSegment.Style.Add("background", ColorTranslator.ToHtml(ScaleSegmentColor));
                _scaleSegment.Style.Add("position", "absolute");
                _scaleSegment.Style.Add("top", (MapHeight - 20).ToString() + "px");
                _scaleSegment.Style.Add("left", "10px");

                Controls.Add(_scaleSegment);

                _scaleLabel = new HtmlGenericControl("span");
                _scaleLabel.Style.Add("border-style", "none");
                _scaleLabel.Style.Add("padding", "0px 0px 0px 0px");
                _scaleLabel.Style.Add("margin", "0px");
                _scaleLabel.Style.Add("font-family", "Arial, Helvetica, sans-serif");
                _scaleLabel.Style.Add("font-weight", "bold");
                _scaleLabel.Style.Add("font-size", "small");
                _scaleLabel.Style.Add("color", ColorTranslator.ToHtml(ScaleLabelColor));
                _scaleLabel.Style.Add("background", "transparent");
                _scaleLabel.Style.Add("position", "absolute");
                _scaleLabel.Style.Add("top", (MapHeight - 40).ToString() + "px");
                _scaleLabel.Style.Add("left", "10px");
                _scaleLabel.Style.Add("height", "18px");
                _scaleLabel.Style.Add("opacity", "0.75");
                _scaleLabel.Style.Add("filter", "alpha(opacity=75)");
                _scaleLabel.Style.Add("width", "auto");
                _scaleLabel.Style.Add("z-index", "54");
                _scaleLabel.InnerText = scaleLabel;

                Controls.Add(_scaleLabel);
            }

            // Toolbar
            if (ToolBar)
            {
                _toolsHolder = new HtmlGenericControl("span");
                _toolsHolder.Style.Add("border-style", "none");
                _toolsHolder.Style.Add("padding", "0px 0px 0px 0px");
                _toolsHolder.Style.Add("opacity", "0.65");
                _toolsHolder.Style.Add("filter", "alpha(opacity=65)");
                _toolsHolder.Style.Add("position", "absolute");
                _toolsHolder.Style.Add("top", "5px");
                _toolsHolder.Style.Add("left", "7px");
                _toolsHolder.Style.Add("z-index", "100");

                _toolsHolder.Attributes.Add("onmouseenter", JsVarName + ".fadeInToolbar(event)");
                _toolsHolder.Attributes.Add("onmouseleave", JsVarName + ".fadeOutToolbar(event)");
                _toolsHolder.Attributes.Add("onmouseover", JsVarName + ".fadeInToolbar(event)");
                _toolsHolder.Attributes.Add("onmouseout", JsVarName + ".fadeOutToolbar(event)");

                _btnDrag = newButton("btnDrag", 
                                     true, 
                                     DragButtonTooltip, 
                                     ResolveClientUrl(SelectedDragButtonImageUrl),
                                     JsVarName + ".selectDragTool()");
                _toolsHolder.Controls.Add(_btnDrag);

                _btnSelect = newButton("btnSelect", 
                                        true, 
                                        SelectObjectButtonTooltip, 
                                        ResolveClientUrl(InfoButtonImageUrl),
                                        JsVarName + ".selectSelectTool()");
                _toolsHolder.Controls.Add(_btnSelect);

                _btnDrawLine = newButton("btnDrawLine",  
                                         DrawPolylineTool, 
                                         DrawPolylineButtonTooltip, 
                                         ResolveClientUrl(LineMeasurerButtonImageUrl),
                                         JsVarName + ".selectLineMeasurerTool()");
                _toolsHolder.Controls.Add(_btnDrawLine);

                _btnDrawContour = newButton("btnDrawContour", 
                                            DrawPolygonTool, 
                                            DrawPolygonButtonTooltip, 
                                            ResolveClientUrl(AreaMeasurerButtonImageUrl),
                                            JsVarName + ".selectAreaMeasurerTool()");
                _toolsHolder.Controls.Add(_btnDrawContour);

                Controls.Add(_toolsHolder);
            }

            // information about the object
            string objectInfoVisibility = "hidden";
            if (!string.IsNullOrEmpty(ObjectInfoHtml))
                objectInfoVisibility = "visible";

            _objectInfoPopup = new HtmlGenericControl("div");
            _objectInfoPopup.ID = "objectInfo";
            _objectInfoPopup.Style.Add("background-position", "#D1D0E8");
            _objectInfoPopup.Style.Add("border", "1px solid #777777");
            _objectInfoPopup.Style.Add("padding", "3px 3px 7px 3px");
            _objectInfoPopup.Style.Add("background", ColorTranslator.ToHtml(ToolsColor));
            _objectInfoPopup.Style.Add("position", "absolute");
            _objectInfoPopup.Style.Add("visibility", objectInfoVisibility);
            _objectInfoPopup.Style.Add("height", "auto");
            _objectInfoPopup.Style.Add("opacity", "0.85");
            _objectInfoPopup.Style.Add("filter", "alpha(opacity=85)");
            _objectInfoPopup.Style.Add("top", "100px");
            _objectInfoPopup.Style.Add("left", "100px");
            _objectInfoPopup.Style.Add("width", "262px");
            _objectInfoPopup.Style.Add("z-index", "69");

            HtmlGenericControl captionHolder = new HtmlGenericControl("div");
            captionHolder.Style.Add("font-family", "Arial, Helvetica, sans-serif");
            captionHolder.Style.Add("font-weight", "bold");
            captionHolder.Style.Add("font-size", "small");
            captionHolder.Style.Add("color", "#555555");
            captionHolder.Style.Add("margin-bottom", "25px");
            captionHolder.Style.Add("width", "262px");

            _objectInfoPopup.Controls.Add(captionHolder);

            HtmlGenericControl objectInfoCaption = new HtmlGenericControl("div");
            objectInfoCaption.Style.Add("text-align", "center");
            objectInfoCaption.Style.Add("width", "240px");
            objectInfoCaption.Style.Add("float", "left");
            objectInfoCaption.InnerText = ObjectInfoCaption;

            captionHolder.Controls.Add(objectInfoCaption);

            HtmlGenericControl divCloseButton = new HtmlGenericControl("div");
            divCloseButton.Style.Add("font-size", "x-small");
            divCloseButton.Style.Add("font-family", "Arial, Helvetica, sans-serif");
            divCloseButton.Style.Add("float", "right");
            divCloseButton.Style.Add("margin-right", "3px");

            divCloseButton.Attributes.Add("onmouseout", "javascript:" + JsVarName + ".cancelDrag = false;");
            divCloseButton.Attributes.Add("onmousemove", "javascript:" + JsVarName + ".cancelDrag = true;");
            divCloseButton.Attributes.Add("onclick", "return " + JsVarName + ".hideSelection();");

            captionHolder.Controls.Add(divCloseButton);

            HtmlGenericControl closeButton = new HtmlGenericControl("input");
            closeButton.Style.Add("border-width", "0px");
            closeButton.Attributes.Add("type", "image");
            closeButton.Attributes.Add("src", ResolveClientUrl(CloseInfoButtonImageUrl));

            divCloseButton.Controls.Add(closeButton);

            _objectInfoHolder = new HtmlGenericControl("span");
            _objectInfoHolder.Style.Add("font-family", "Arial, Helvetica, sans-serif");
            _objectInfoHolder.Style.Add("Arial, Helvetica, sans-serif", "");
            _objectInfoHolder.Style.Add("font-size", "small");

            _objectInfoPopup.Controls.Add(_objectInfoHolder);

            Controls.Add(_objectInfoPopup);

            _lineDrawingCanvas = new HtmlGenericControl("canvas");
            _lineDrawingCanvas.Style.Add("position", "absolute");
            _lineDrawingCanvas.Style.Add("top", "0px");
            _lineDrawingCanvas.Style.Add("left", "0px");
            _lineDrawingCanvas.Style.Add("display", "none");
            _lineDrawingCanvas.Style.Add("z-index", "59");
            _lineDrawingCanvas.Style.Add("cursor", "url(" + ResolveClientUrl(LineMeasurerCursorUrl) + "), default");

            _lineDrawingCanvas.Attributes.Add("width", MapWidth.ToString());
            _lineDrawingCanvas.Attributes.Add("height", MapHeight.ToString());

            _lineDrawingCanvas.Attributes.Add("onmousedown", JsVarName + ".lineDrawingCanvasMouseDown(this, event)");
            _lineDrawingCanvas.Attributes.Add("onmousemove", JsVarName + "dm.canvasMouseMove(this, event)");
            _lineDrawingCanvas.Attributes.Add("onmouseup", JsVarName + "dm.canvasMouseUp(this, event)");

            Controls.Add(_lineDrawingCanvas);

            _contourDrawingCanvas = new HtmlGenericControl("canvas");
            _contourDrawingCanvas.Style.Add("position", "absolute");
            _contourDrawingCanvas.Style.Add("top", "0px");
            _contourDrawingCanvas.Style.Add("left", "0px");
            _contourDrawingCanvas.Style.Add("display", "none");
            _contourDrawingCanvas.Style.Add("z-index", "61");
            _contourDrawingCanvas.Style.Add("cursor", "url(" + ResolveClientUrl(AreaMeasurerCursorUrl) + "), default");

            _contourDrawingCanvas.Attributes.Add("width", MapWidth.ToString());
            _contourDrawingCanvas.Attributes.Add("height", MapHeight.ToString());

            _contourDrawingCanvas.Attributes.Add("onmousedown", JsVarName + ".contourDrawingCanvasMouseDown(this, event)");
            _contourDrawingCanvas.Attributes.Add("onmousemove", JsVarName + "am.canvasMouseMove(this, event)");
            _contourDrawingCanvas.Attributes.Add("onmouseup", JsVarName + "am.canvasMouseUp(this, event)");

            Controls.Add(_contourDrawingCanvas);
        }

        private HtmlGenericControl newButton(string id, bool display, string title, string imageUrl, string onclick)
        {
            HtmlGenericControl button = new HtmlGenericControl("img");
            button.ID = id;
            button.Style.Add("cursor", "pointer");
            button.Style.Add("display", display ? "inline" : "none");
            button.Style.Add("border-color", "#555555");
            button.Style.Add("border-width", "0px");
            button.Style.Add("border-style", "solid");

            if (Context.Request.Browser.Browser == "IE")
                button.Style.Add("background", ColorTranslator.ToHtml(ToolsColor));

            button.Attributes.Add("title", title);
            button.Attributes.Add("src", imageUrl);
            button.Attributes.Add("align", "middle");
            button.Attributes.Add("onclick", onclick);
            
            return button;
        }

        private void registerScripts()
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            string pixelSize = (Workspace.ViewBox.Width / MapWidth).ToString(CultureInfo.InvariantCulture);
            string fillColor =
                DrawingToolsFillColor.R.ToString(CultureInfo.InvariantCulture) + ", " +
                DrawingToolsFillColor.G.ToString(CultureInfo.InvariantCulture) + ", " +
                DrawingToolsFillColor.B.ToString(CultureInfo.InvariantCulture);

            ClientScriptManager cm = Page.ClientScript;

            // emulation of the Canvas in IE
            if (Context.Request.Browser.Browser == "IE" &&
                (DrawPolygonTool || DrawPolylineTool))
                cm.RegisterClientScriptResource(this.GetType(), "MapControl.Scripts.excanvas.js");

            //cm.RegisterClientScriptResource(this.GetType(), "MapControl.Scripts.maptools.js");

            cm.RegisterClientScriptBlock(this.GetType(),
                "maptools", global::MapControl.Properties.Resources.maptools, true);

            // Registration script callback
            string cbReference = cm.GetCallbackEventReference(this, "arg",
                 "MapImageReceiveCallBackResult" + ClientID, "");
            string callbackScript = "function " + ClientID + "CallServer(arg, context) {" +
                cbReference + "; }" + 
            "function MapImageReceiveCallBackResult" + ClientID + "(arg, context)" +
            "{" + JsVarName + ".receiveCallBackResult(arg, context);}";
            cm.RegisterClientScriptBlock(this.GetType(),
                "MapImageCallServer" + ClientID, callbackScript, true);


            cm.RegisterStartupScript(this.GetType(),
                "MapControlLayerlistQuery" + ClientID,

                "var " + JsVarName + "dm = new distanceMeasurer( {" +
                    "pointColor: \"" + ColorTranslator.ToHtml(DrawingToolsPointColor) + "\", lineColor: \"" + ColorTranslator.ToHtml(DrawingToolsLineColor) + "\", shadowColor: \"rgba(0, 0, 0, 0.5)\", pointRadius: " + DrawingToolsPointSize.ToString(CultureInfo.InvariantCulture) +
                    ", alwaysDisplayLabel: " + (AllowFeaturePosting ? "true" : "false") +
                    ", lineWidth: " + DrawingToolsLineWidth.ToString(CultureInfo.InvariantCulture) + ", shadowOffset: 2, canvasWidth: " + MapWidth.ToString() +
                    ", canvasHeight: " + MapHeight.ToString(CultureInfo.InvariantCulture) +
                    ", pixelSize: " + pixelSize +
                    ", doc: document, canvasId: \"" + _lineDrawingCanvas.ClientID +
                    "\", divId:\"" + _measureResult.ClientID +
                    "\", metric:\"" + LineUnitLabel + "\"});" +

                "var " + JsVarName + "am = new areaMeasurer( {" +
                    "pointColor: \"" + ColorTranslator.ToHtml(DrawingToolsPointColor) + "\", lineColor: \"" + ColorTranslator.ToHtml(DrawingToolsLineColor) + "\", shadowColor: \"rgba(0, 0, 0, 0.5)\", pointRadius: " + DrawingToolsPointSize.ToString() +
                    ", lineWidth: " + DrawingToolsLineWidth.ToString(CultureInfo.InvariantCulture) + ", shadowOffset: 2, canvasWidth: " + MapWidth.ToString(CultureInfo.InvariantCulture) + ", canvasHeight: " + MapHeight.ToString() +
                    ", alwaysDisplayLabel: " + (AllowFeaturePosting ? "true" : "false") +
                    ", fillColor: \"rgba(" + fillColor + ", 0.3)\", pixelSize: " + pixelSize + ", doc: document, canvasId: \"" + _contourDrawingCanvas.ClientID +
                    "\", divId:\"" + _measureResult.ClientID +
                    "\", metric:\"" + AreaUnitLabel + "\"});" +

                "var " + JsVarName + " = new mapClientControl({id: '" + ClientID + "', " + 
                                                "toolName: 'drag', " +
                                                "variableName: '" + JsVarName + "', " +
                                                "contourDrawer: " + JsVarName + "am, " +
                                                "lineDrawer: " + JsVarName + "dm, " + 

                                                "lineDrawingCanvasId: '" + _lineDrawingCanvas.ClientID + "', " +
                                                "contourDrawingCanvasId: '" + _contourDrawingCanvas.ClientID + "', " +
                                                "measureResultId: '" + _measureResult.ClientID + "', " + 

                                                "objectInfoId: '" + _objectInfoPopup.ClientID + "', " +
                                                "infoId: '" + _objectInfoHolder.ClientID + "', " +

                                                "toolBarId: '" + (_toolsHolder != null ? _toolsHolder.ClientID : string.Empty) + "', " +
                                                "dragButtonEnabledUrl:'" + ResolveClientUrl(SelectedDragButtonImageUrl) +  "', " +
                                                "dragButtonDisabledUrl:'" + ResolveClientUrl(DragButtonImageUrl) + "', " +
                                                "selectButtonEnabledUrl:'" + ResolveClientUrl(SelectedInfoButtonImageUrl) + "', " +
                                                "selectButtonDisabledUrl:'" + ResolveClientUrl(InfoButtonImageUrl) + "', " +
                                                "drawLineButtonEnabledUrl:'" + ResolveClientUrl(SelectedLineMeasurerButtonImageUrl) + "', " +
                                                "drawLineButtonDisabledUrl:'" + ResolveClientUrl(LineMeasurerButtonImageUrl) + "', " +
                                                "drawContourButtonEnabledUrl:'" + ResolveClientUrl(SelectedAreaMeasurerButtonImageUrl) + "', " +
                                                "drawContourButtonDisabledUrl:'" + ResolveClientUrl(AreaMeasurerButtonImageUrl) + "', " +

                                                "dragCursorUrl:'" + ResolveClientUrl(DragCursorUrl) + "', " +
                                                "drawLineCursorUrl:'" + ResolveClientUrl(LineMeasurerCursorUrl) + "', " +
                                                "drawContourCursorUrl:'" + ResolveClientUrl(AreaMeasurerCursorUrl) + "', " +

                                                "dragButtonId: '" + (_btnDrag == null ? "" : _btnDrag.ClientID) + "', " +
                                                "selectButtonId: '" + (_btnSelect == null ? "" : _btnSelect.ClientID) + "', " +
                                                "drawLineButtonId: '" + (_btnDrawLine == null ? "" : _btnDrawLine.ClientID) + "', " +
                                                "drawContourButtonId: '" + (_btnDrawContour == null ? "" : _btnDrawContour.ClientID) + "', " + 
                                                "scaleSegmentId: '" + (_scaleSegment == null ? "" : _scaleSegment.ClientID) + "', " +
                                                "scaleLabelId: '" + (_scaleLabel == null ? "" : _scaleLabel.ClientID) + "', " +
                                                "layerRecordsHolderId: '" + (_layersControl == null ? "" : _layerRecordsHolder.ClientID) + "', " +
                                                "layersSwitchButtonId: '" + (_layersControl == null ? "" : _btnSwitchLayers.ClientID) + "', " +
                                                "layersControlId: '" + (_layersControl == null ? "" : _layersControl.ClientID) + "', " +
                                                "imgLoaderId: '" + (_imgMapLoading.ClientID) + "', " + 
                                                "layerControlEnabled: true});" +

                "document.onmousewheel = function(event) { return " + JsVarName + ".mouseWheel(event); };" + 
                "document.getElementById('" + _imgMap1.ClientID + "').focus();" + 
                "if(window.addEventListener) {" +
                    "window.addEventListener(\"DOMMouseScroll\", function(event) { " + JsVarName + ".mouseWheel(event); }, true);}"
                    , true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="output"></param>
        protected override void RenderContents(HtmlTextWriter output)
        {
            if (DesignMode)
                output.Write("MapControl: " + ClientID);

            base.RenderContents(output);
        }

        /// <summary>
        /// Instantiates MapControl.
        /// </summary>
        public MapControl()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            //if (!Page.IsCallback)
            //    registerScripts();
        }

        #region Translators event cards

        private void dataSourceReadyToRelease(object sender, FeatureDataSourceEventArgs e)
        {
            if (LayerDataSourceReadyToRelease != null)
                LayerDataSourceReadyToRelease(sender, e);
        }

        private void dataSourceNeeded(object sender, FeatureDataSourceEventArgs e)
        {
            if (LayerDataSourceNeeded != null)
                LayerDataSourceNeeded(sender, e);
        }

        private void beforePolygonRender(object sender, FeatureRenderEventArgs e)
        {
            if (BeforePolygonRender != null)
                BeforePolygonRender(sender, e);
        }

        private void beforePolylineRender(object sender, FeatureRenderEventArgs e)
        {
            if (BeforePolylineRender != null)
                BeforePolylineRender(sender, e);
        }

        private void beforePointRender(object sender, FeatureRenderEventArgs e)
        {
            if (BeforePointRender != null)
                BeforePointRender(sender, e);
        }

        #endregion

        /// <summary>
        /// Sets the scale presentation data.
        /// </summary>
        /// <param name="scales">An array of scales</param>
        /// <param name="labels">An array of labels</param>
        public void SetScalePresentationData(double[] scales, string[] labels)
        {
            Context.Session["scales" + ClientID] = scales;
            Context.Session["labels" + ClientID] = labels;
        }

        /// <summary>
        /// Switches mode of the map control to the specified value.
        /// </summary>
        /// <param name="mode">A mode to switch</param>
        public void SwitchToMode(MapControlMode mode)
        {
            _mode = string.Empty;
            switch (mode)
            { 
                case MapControlMode.Pan: 
                    _mode = "drag";
                    break;
                case MapControlMode.Select:
                    _mode = "info";
                    break;
                case MapControlMode.DrawPolyline:
                    _mode = "lineMeasurer";
                    break;
                case MapControlMode.DrawPolygon:
                    _mode = "areaMeasurer";
                    break;
            }
        }

        /// <summary>
        /// Transforms screen coordinates to map coordinates.
        /// </summary>
        /// <param name="x">Client X value</param>
        /// <param name="y">Client Y value</param>
        public ICoordinate ScreenToMap(int x, int y)
        {
            return ScreenToMap(new Point(x, y));
        }

        /// <summary>
        /// Transforms screen coordinates to map coordinates.
        /// </summary>
        /// <param name="point">A point on the screen</param>
        /// <returns>A point on the map</returns>
        public ICoordinate ScreenToMap(Point point)
        {
            return ScreenToMap(PlanimetryEnvironment.NewCoordinate(point.X, point.Y));
        }

        /// <summary>
        /// Transforms screen coordinates to map coordinates.
        /// </summary>
        /// <param name="point">A point of the screen</param>
        /// <returns>A coordinates of point of the map</returns>
        public ICoordinate ScreenToMap(ICoordinate point)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;
            MapWorkspace workspace = getWorkspace();

            double resultX = workspace.ViewBox.Width / MapWidth * point.X + workspace.ViewBox.MinX;
            double resultY = workspace.ViewBox.MaxY - workspace.ViewBox.Height / MapHeight * point.Y;

            ICoordinate result = PlanimetryEnvironment.NewCoordinate(resultX, resultY);

            return result;
        }

        /// <summary>
        /// Transforms map coordinates to screen coordinates.
        /// </summary>
        /// <param name="x">X value on the map</param>
        /// <param name="y">X value on the map</param>
        public Point MapToScreen(double x, double y)
        {
            return MapToScreen(PlanimetryEnvironment.NewCoordinate(x, y));
        }

        /// <summary>
        /// Transforms map coordinates to screen coordinates.
        /// </summary>
        /// <param name="point">A point on the map</param>
        /// <returns>A point on the screen</returns>
        public Point MapToScreen(ICoordinate point)
        {
            ICoordinate p = MapToScreenD(point);

            return new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }

        /// <summary>
        /// Translates the map coordinates to screen coordinates.
        /// </summary>
        /// <param name="point">Coordinate point</param>
        public ICoordinate MapToScreenD(ICoordinate point)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;
            MapWorkspace workspace = getWorkspace();

            double scaleFactor = Math.Min(MapWidth / workspace.ViewBox.Width, MapHeight / workspace.ViewBox.Height);
            double resultX = (point.X - workspace.ViewBox.MinX) * scaleFactor;
            double resultY = (workspace.ViewBox.MaxY - point.Y) * scaleFactor;
            ICoordinate result = PlanimetryEnvironment.NewCoordinate(resultX, resultY);

            return PlanimetryEnvironment.NewCoordinate(result.X, result.Y);
        }

        private void getScalePresentationData(BoundingRectangle viewbox, int maxSegmentLength, ref int segmentLength, ref string label)
        {
            int w = (int)Width.Value;
            segmentLength = 0;
            label = "unknown";
            object obj = Context.Session["scales" + ClientID];
            object obj1 = Context.Session["labels" + ClientID];
            if(obj == null || obj1 == null)
                throw new InvalidOperationException("Scale data not set");
            double[] scales = (double[])obj;
            string[] labels = (string[])obj1;

            for (int i = 0; i < scales.Length; i++)
            {
                if (scales[i] * w / viewbox.Width > maxSegmentLength)
                    if (i > 0)
                    {
                        segmentLength = (int)(scales[i - 1] * w / viewbox.Width);
                        label = labels[i - 1];
                        return;
                    }
            }
        }

        #region ICallbackEventHandler Members

        /// <summary>
        /// Gets a callback result.
        /// </summary>
        /// <returns>A string containing result</returns>
        public string GetCallbackResult()
        {
            Context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            int w = (int)Width.Value;
            int h = (int)Height.Value;

            string result = string.Empty;

            if (!string.IsNullOrEmpty(_layers))
            {
                result = "layers" + _layers;
                _layers = string.Empty;
                return result;
            }
            else
            {
                result = "objectInfo" + ObjectInfoHtml.Length.ToString() + ObjectInfoHtml;
                result += " " + (Workspace.ViewBox.Width / w).ToString(CultureInfo.InvariantCulture) + " ";
                result += "scaleData";
                if (ScaleBar)
                {
                    int segmentLength = 0;
                    string label = string.Empty;
                    getScalePresentationData(Workspace.ViewBox, 110, ref segmentLength, ref label);
                    result += (label.Length + 1).ToString() + " " + 
                              label.ToString() + segmentLength.ToString() + " ";
                }
                else
                    result += "0";

                if(!string.IsNullOrEmpty(_mode))
                    result += "mode" + _mode + " ";

                string featureCoords = string.Empty;
                if(_clientFeatureCoordinates.Count > 0)
                {
                    featureCoords += "shapeCoords" + _clientFeatureCoordinates.Count + " ";
                    foreach (ICoordinate p in _clientFeatureCoordinates)
                    {
                        ICoordinate imagePoint = MapToScreenD(p);
                        featureCoords += imagePoint.X.ToString(CultureInfo.InvariantCulture) + " ";
                        featureCoords += imagePoint.Y.ToString(CultureInfo.InvariantCulture) + " ";
                    }
                }

                result += featureCoords;

                Random r = new Random();

                string s = Context.Request.FilePath;
                string urlLeftPart = Context.Request.Url.GetLeftPart(UriPartial.Authority) + s.Substring(0, s.LastIndexOf('/'));

                result +=
                    urlLeftPart + "/" + ResolveClientUrl(HttpHandlerName) +
                    "?width=" + w.ToString() +
                    "&height=" + h.ToString() +
                    "&workspace=" + WorkspaceUniqString +
                    "&rn=" + r.Next().ToString();

                return result;
            }
        }

        /// <summary>
        /// Raises a callback event.
        /// </summary>
        /// <param name="eventArgument">An argument string</param>
        public void RaiseCallbackEvent(string eventArgument)
        {
            _clientFeatureCoordinates.Clear();

            string[] args = eventArgument.Split(new char[] {';'});

            if (args[0].StartsWith("post"))
            {
                Collection<ICoordinate> points = new Collection<ICoordinate>();

                for (int i = 1; i < args.Length; i += 2)
                    if (!string.IsNullOrEmpty(args[i]))
                        points.Add(ScreenToMap(PlanimetryEnvironment.NewCoordinate(double.Parse(args[i], CultureInfo.InvariantCulture), 
                                               double.Parse(args[i + 1], CultureInfo.InvariantCulture))));

                ICoordinate[] pointsArr = new ICoordinate[points.Count];
                for (int i = 0; i < pointsArr.Length; i++)
                    pointsArr[i] = points[i];

                FeatureType type = FeatureType.Point;

                if (args[0].StartsWith("postPolygon"))
                    type = FeatureType.Polygon;

                if (args[0].StartsWith("postPolyline"))
                    type = FeatureType.Polyline;

                if (FeaturePosted != null)
                    FeaturePosted(this, new FeaturePostEventArgs(type, pointsArr));
                return;
            }

            if (args[0].StartsWith("layerVisibilityChange"))
            {
                int n;
                string s = args[0].Substring(21);
                if (int.TryParse(s, out n))
                    doLayerVisibilityChange(n);
                else
                {
                    if (s == "hideAll")
                    {
                        doSetAllLayersVisibility(false);
                    }
                    else if (s == "showAll")
                    {
                        doSetAllLayersVisibility(true);
                    }
                }
                return;
            }

            int x = 0, y = 0;

            if (args.Length > 2)
            {
                if (args[1].EndsWith("px"))
                    args[1] = args[1].Substring(0, args[1].Length - 2);

                if (args[2].EndsWith("px"))
                    args[2] = args[2].Substring(0, args[2].Length - 2);

                x = int.Parse(args[1]);
                y = int.Parse(args[2]);
            }
            
            switch (args[0])
            { 
                case "hideSelection":
                    doHideSelection();
                    break;
                case "layerListQuery":
                    generateLayerList();
                    break;
                case "drag":
                case "lineMeasurer":
                case "areaMeasurer":
                    translateClientCoords(args, 3);
                    doDragMap(x, y);
                    break;
                case "zoomIn":
                    translateClientCoords(args, 3);
                    changeZoom(ZoomStep, x, y);
                    break;
                case "zoomOut":
                    translateClientCoords(args, 3);
                    changeZoom(-ZoomStep, x, y);
                    break;
                case "center":
                    translateClientCoords(args, 3);
                    doCenterMap(x, y);
                    break;
                case "info":
                    doSelectObject(x, y);
                    break;
            }
        }

        private void translateClientCoords(string[] args, int startIndex)
        {
            for(int i = startIndex; i < args.Length; i += 2)
                if (!string.IsNullOrEmpty(args[i]))
                    _clientFeatureCoordinates.Add(ScreenToMap(PlanimetryEnvironment.NewCoordinate(double.Parse(args[i], CultureInfo.InvariantCulture),
                                                                  double.Parse(args[i + 1], CultureInfo.InvariantCulture))));
        }

        #endregion

        private void setWorkspace(MapWorkspace workspace)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            Context.Session[WorkspaceUniqString] = workspace;

            if (workspace.Map != null)
            {
                foreach (LayerBase l in workspace.Map.Layers)
                {
                    FeatureLayer fl = l as FeatureLayer;
                    if (fl != null)
                    {
                        fl.DataSourceNeeded += dataSourceNeeded;
                        fl.DataSourceReadyToRelease += dataSourceReadyToRelease;
                        fl.BeforePointRender += beforePointRender;
                        fl.BeforePolylineRender += beforePolylineRender;
                        fl.BeforePolygonRender += beforePolygonRender;
                        if (!fl.AreFeaturesAutoLoadable)
                            fl.LoadFeatures();
                    }
                }
            }

            // the aspect ratio of the viewport maps may not match the aspect ratio of viewbox, read from the workspace.
            // to correct it.

            double dx = 0, dy = 0;

            if (workspace.ViewBox.IsEmpty())
                throw new InvalidOperationException("View box should be set in the workspace.");

            if (MapWidth / MapHeight > workspace.ViewBox.Width / workspace.ViewBox.Height)
                dy = -(workspace.ViewBox.Height - (double)MapHeight / (double)MapWidth * workspace.ViewBox.Width);
            else
                dx = -(workspace.ViewBox.Width - (double)MapWidth / (double)MapHeight * workspace.ViewBox.Height);

            workspace.ViewBox = new BoundingRectangle(workspace.ViewBox.MinX,
                                                      workspace.ViewBox.MinY,
                                                      workspace.ViewBox.MaxX + dx,
                                                      workspace.ViewBox.MaxY + dy);
        }

        private MapWorkspace getWorkspace()
        {
            MapWorkspace workspace = (MapWorkspace)HttpContext.Current.Session[WorkspaceUniqString];

            if (workspace != null)
                return workspace;

            return null;
        }

        private static string encodeValue(string s)
        {
            // HtmlTextWriter.WriteEncodedUrl is not yet implemented in MONO

            /*StringWriter sw = new StringWriter();
            HtmlTextWriter writer = new HtmlTextWriter(sw);
            writer.WriteEncodedUrl(s);
            return sw.ToString();*/

            string result = string.Empty;
            byte[] bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(s));
            char[] chars = Encoding.UTF8.GetChars(bytes);

            for (int i = 0; i < chars.Count(); i++)
            {
                byte[] charBytes = Encoding.UTF8.GetBytes(chars, i, 1);
                if (charBytes[0] == 208 || charBytes[0] == 209)
                {
                    for (int j = 0; j < charBytes.Count(); j++)
                        result += "%" + Convert.ToString(charBytes[j], 16);
                }
                else
                {
                    if (char.IsLetterOrDigit(chars[i]) ||
                       char.IsNumber(chars[i]) ||
                       char.IsPunctuation(chars[i]) ||
                        chars[i] == '=')
                        result += Encoding.UTF8.GetChars(charBytes)[0];
                    else
                        result += "%" + Convert.ToString(charBytes[0], 16);
                }
            }

            return result;
        }

        private void doSetAllLayersVisibility(bool visible)
        {
            if (Workspace.Map != null)
            {
                foreach (LayerBase l in Workspace.Map.Layers)
                    if (l.Controllable)
                        l.Visible = visible;
            }
        }

        private void doLayerVisibilityChange(int layerIndex)
        {
            Workspace.Map.Layers[layerIndex].Visible = !Workspace.Map.Layers[layerIndex].Visible;
        }

        private void generateLayerList()
        {
            _layers = string.Empty;

            StringBuilder sb = new StringBuilder();
            if (Workspace.Map != null)
            {
                int i = Workspace.Map.Layers.Count - 1;
                for (int j = Workspace.Map.Layers.Count - 1; j >= 0; j--)
                {
                    LayerBase l = Workspace.Map.Layers[j];
                    string iStr = i.ToString();
                    string disabled = l.Controllable ? string.Empty : " disabled=\"disabled\"";
                    sb.Append("<input id=\"layerCheckBox" + ClientID + iStr +
                                "\" type=\"checkbox\" name=\"layerCheckBox" + ClientID + iStr + "\" " +
                                (l.Visible ? "checked=\"checked\"" : string.Empty) +
                                disabled +
                                " onclick=\"" + JsVarName + ".changeLayerVisibility(" + iStr + ");\"/>" +
                              "<label for=\"layerCheckBox" + ClientID + iStr + "\">" +
                                l.Title + "</label>");
                    sb.Append("<br />");
                    i--;
                }
            }

            _layers = sb.ToString();
        }

        private void doHideSelection()
        {
            if (FeatureSelected != null)
                FeatureSelected(this, new FeatureSelectedEventArgs(null));
        }

        private void changeZoom(int deltaPercent, int mouseX, int mouseY)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            MapWorkspace workspace = getWorkspace();

            if (deltaPercent != 0)
            {
                double delta = (double)deltaPercent / 100;

                if (mouseX < 0 || mouseX > MapWidth || !AlignmentWhileZooming)
                    mouseX = MapWidth / 2;
                if (mouseY < 0 || mouseY > MapHeight || !AlignmentWhileZooming)
                    mouseY = MapHeight / 2;

                ICoordinate node = ScreenToMap(new Point(mouseX, mouseY));

                double leftWidth = node.X - workspace.ViewBox.MinX;
                double rightWidth = workspace.ViewBox.MaxX - node.X;
                double bottomHeight = node.Y - workspace.ViewBox.MinY;
                double topHeight = workspace.ViewBox.MaxY - node.Y;

                double factor = delta > 0 ? 1 - delta / (2 + 2 * delta) : 1 - delta / 2;

                BoundingRectangle viewbox =
                    new BoundingRectangle(node.X - leftWidth * factor,
                                          node.Y - bottomHeight * factor,
                                          node.X + rightWidth * factor,
                                          node.Y + topHeight * factor);

                workspace.ViewBox = viewbox;
            }

        }

        private void doCenterMap(int mx, int my)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            MapWorkspace workspace = getWorkspace();

            double x = (workspace.ViewBox.Width) / MapWidth * mx + workspace.ViewBox.MinX;
            double y = workspace.ViewBox.MaxY - (workspace.ViewBox.Height) / MapHeight * my;

            double dx = x - workspace.ViewBox.Width / 2 - workspace.ViewBox.MinX;
            double dy = y - workspace.ViewBox.Height / 2 - workspace.ViewBox.MinY;
            dragMap(dx, dy);
        }

        private void doDragMap(int mdx, int mdy)
        {
            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            MapWorkspace workspace = getWorkspace();

            double dx = -workspace.ViewBox.Width / MapWidth * mdx;
            double dy = workspace.ViewBox.Height / MapHeight * mdy;

            dragMap(dx, dy);
        }

        private void doSelectObject(int mx, int my)
        {
            MapWorkspace workspace = getWorkspace();

            if (workspace.Map == null)
                return;

            Feature feature = null;

            int MapWidth = (int)Width.Value;
            int MapHeight = (int)Height.Value;

            double x = workspace.ViewBox.Width / MapWidth * mx + workspace.ViewBox.MinX;
            double y = workspace.ViewBox.MaxY - workspace.ViewBox.Height / MapHeight * my;

            // calculate the error of selection of point and line objects
            workspace.Map.SelectionPointRadius =
                4 * workspace.ViewBox.Width / MapWidth;

            ICoordinate point = PlanimetryEnvironment.NewCoordinate(x, y);
            if (workspace.Map.OnTheFlyTransform != null)
            {
                ICoordinate delta = PlanimetryEnvironment.NewCoordinate(point.X + workspace.Map.SelectionPointRadius, point.Y);
                IMathTransform inverseTransform = workspace.Map.OnTheFlyTransform.Inverse();

                delta = PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(delta.Values())); 

                workspace.Map.SelectionPointRadius =
                    PlanimetryAlgorithms.Distance(PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(point.Values())), delta);
                
            }

            double scale = MapWidth / workspace.ViewBox.Width;

            workspace.Map.SelectTopObject(point, scale, out feature);
            if (FeatureSelected != null)
                FeatureSelected(this, new FeatureSelectedEventArgs(feature));
        }

        private void dragMap(double dx, double dy)
        {
            MapWorkspace workspace = getWorkspace();

            workspace.ViewBox = new BoundingRectangle(workspace.ViewBox.MinX + dx,
                                                      workspace.ViewBox.MinY + dy,
                                                      workspace.ViewBox.MaxX + dx,
                                                      workspace.ViewBox.MaxY + dy);
        }
    }

    /// <summary>
    /// Provides data for the FeatureSelected event.
    /// </summary>
    public class FeatureSelectedEventArgs : EventArgs
    {
        private Feature _feature;

        /// <summary>
        /// Gets a selected feature.
        /// </summary>
        public Feature Feature
        {
            get { return _feature; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.Web.FeatureSelectedEventArgs. 
        /// </summary>
        /// <param name="feature">The selected feature</param>
        internal FeatureSelectedEventArgs(Feature feature)
        {
            _feature = feature;
        }
    }

    /// <summary>
    /// Provides data for the FeaturePosted event.
    /// </summary>
    public class FeaturePostEventArgs : EventArgs
    {
        private FeatureType _featureType;
        private ICoordinate[] _points;

        /// <summary>
        /// Gets a type of feature.
        /// </summary>
        public FeatureType FeatureType
        {
            get { return _featureType; }
        }

        /// <summary>
        /// Gets an array containing a coordinates of feature.
        /// </summary>
        public ICoordinate[] Points
        {
            get { return _points; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.UI.Web.FeaturePostEventArgs. 
        /// </summary>
        /// <param name="featureType">The type of feature</param>
        /// <param name="points">An array containing feature coordinates</param>
        internal FeaturePostEventArgs(FeatureType featureType, ICoordinate[] points)
        {
            _featureType = featureType;
            _points = points;
        }
    }

}
