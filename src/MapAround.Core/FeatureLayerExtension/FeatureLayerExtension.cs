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



﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MapAround.Mapping;

namespace MapAround.ThematicLayer
{
    /// <summary>
    /// FeatureLayer Extension
    /// </summary>
    public abstract class FeatureLayerExtension
    {
        private FeatureLayer _layer;
        /// <summary/>
        protected bool _useExtesion;
        /// <summary/>
        protected static void addAttribute(XmlElement element, string attributeName, string attributeValue)
        {
            element.Attributes.Append(element.OwnerDocument.CreateAttribute(attributeName));
            element.Attributes[attributeName].Value = attributeValue;
        }
        /// <summary/>
        protected  static string floatArrayToString(float[] arr)
        {
            if (arr == null)
                return string.Empty;

            string str = string.Empty;
            foreach (float f in arr)
                str += f.ToString(CultureInfo.InvariantCulture) + ";";

            return str.Substring(0, str.Length - 1);
        }

        /// <summary/>        
        protected XmlNode tryGetNodeByName(XmlNodeList nodes, string name)
        {
            foreach (XmlNode node in nodes)
                if (node.Name == name)
                    return node;

            return null;
        }
        /// <summary/>
        protected float[] stringToFloatArray(string str)
        {
            string[] strValues = str.Split(';');

            if (strValues.Length == 0)
                return null;

            float[] arr = new float[strValues.Length];
            int i = 0;
            foreach (string s in strValues)
            {
                float f;
                if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                {
                    arr[i] = f;
                    i++;
                }
            }

            return arr;
        }
        /// <summary/>
        protected void addTitleStyleElement(TitleStyle TitleStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement titleStyleElement = doc.CreateElement("title_style");
            layerElement.AppendChild(titleStyleElement);
            addAttribute( titleStyleElement, "visible", TitleStyle.TitlesVisible ? "1" : "0");
            addAttribute( titleStyleElement, "visible_scale", TitleStyle.VisibleScale.ToString(CultureInfo.InvariantCulture));
            addAttribute( titleStyleElement, "color", ColorTranslator.ToHtml(TitleStyle.Color));
            addAttribute( titleStyleElement, "font_name", TitleStyle.FontFamily);
            addAttribute( titleStyleElement, "font_size", TitleStyle.FontSize.ToString(CultureInfo.InvariantCulture));
            addAttribute( titleStyleElement, "font_style", ((int)TitleStyle.FontStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute( titleStyleElement, "render_priority", ((int)TitleStyle.RenderPriority).ToString(CultureInfo.InvariantCulture));
            addAttribute( titleStyleElement, "use_outline", TitleStyle.UseOutline ? "1" : "0");
            addAttribute( titleStyleElement, "outline_size", TitleStyle.OutlineSize.ToString(CultureInfo.InvariantCulture));
            addAttribute( titleStyleElement, "lead_along", TitleStyle.LeadAlong ? "1" : "0");
        }
        /// <summary/>
        protected void addPolygonStyleElement(PolygonStyle PolygonStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement polygonStyleElement = doc.CreateElement("polygon_style");
            layerElement.AppendChild(polygonStyleElement);
            addAttribute( polygonStyleElement, "border_width", PolygonStyle.BorderWidth.ToString(CultureInfo.InvariantCulture));
            addAttribute( polygonStyleElement, "border_color", ColorTranslator.ToHtml(PolygonStyle.BorderColor));
            addAttribute( polygonStyleElement, "border_visible", PolygonStyle.BorderVisible ? "1" : "0");
            addAttribute( polygonStyleElement, "border_dash_style", ((int)PolygonStyle.BorderDashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute( polygonStyleElement, "border_dash_cap", ((int)PolygonStyle.BorderDashCap).ToString(CultureInfo.InvariantCulture));
            addAttribute( polygonStyleElement, "hatch_style", ((int)PolygonStyle.HatchStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute( polygonStyleElement, "use_hatch", PolygonStyle.UseHatch ? "1" : "0");
            addAttribute( polygonStyleElement, "fill_fore_color", ColorTranslator.ToHtml(PolygonStyle.FillForeColor));
            addAttribute( polygonStyleElement, "fill_back_color", ColorTranslator.ToHtml(PolygonStyle.FillBackColor));
            addAttribute( polygonStyleElement, "fill_pattern", ((int)PolygonStyle.FillPattern).ToString(CultureInfo.InvariantCulture));
            addAttribute( polygonStyleElement, "fill_transparent", PolygonStyle.FillBackColor.A.ToString(CultureInfo.InvariantCulture));

        }
        /// <summary/>
        protected void addPolylineStyleElement(PolylineStyle PolylineStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement polylineStyleElement = doc.CreateElement("polyline_style");
            layerElement.AppendChild(polylineStyleElement);
            addAttribute( polylineStyleElement, "width", PolylineStyle.Width.ToString(CultureInfo.InvariantCulture));
            addAttribute( polylineStyleElement, "use_annex_line", PolylineStyle.UseAnnexLine ? "1" : "0");

            addAttribute( polylineStyleElement, "color", ColorTranslator.ToHtml(PolylineStyle.Color));
            addAttribute( polylineStyleElement, "dash_style", ((int)PolylineStyle.DashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute( polylineStyleElement, "dash_cap", ((int)PolylineStyle.DashCap).ToString(CultureInfo.InvariantCulture));

            if (PolylineStyle.DashPattern != null)
                addAttribute( polylineStyleElement, "dash_pattern", floatArrayToString(PolylineStyle.DashPattern));

            addAttribute( polylineStyleElement, "is_compound", PolylineStyle.IsCompound ? "1" : "0");
            if (PolylineStyle.Compound != null)
                addAttribute( polylineStyleElement, "compound", floatArrayToString(PolylineStyle.Compound));

            addAttribute( polylineStyleElement, "annex_color", ColorTranslator.ToHtml(PolylineStyle.AnnexColor));
            addAttribute( polylineStyleElement, "annex_dash_style", ((int)PolylineStyle.AnnexDashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute( polylineStyleElement, "annex_dash_cap", ((int)PolylineStyle.AnnexDashCap).ToString(CultureInfo.InvariantCulture));

            if (PolylineStyle.DashPattern != null)
                addAttribute( polylineStyleElement, "annex_dash_pattern", floatArrayToString(PolylineStyle.AnnexDashPattern));

            addAttribute( polylineStyleElement, "is_annex_compound", PolylineStyle.IsAnnexCompound ? "1" : "0");
            if (PolylineStyle.AnnexCompound != null)
                addAttribute( polylineStyleElement, "annex_compound", floatArrayToString(PolylineStyle.AnnexCompound));

            addAttribute( polylineStyleElement, "use_outline", PolylineStyle.UseOutline ? "1" : "0");
            addAttribute( polylineStyleElement, "outline_width", PolylineStyle.OutlineWidth.ToString(CultureInfo.InvariantCulture));
            addAttribute( polylineStyleElement, "outline_color", ColorTranslator.ToHtml(PolylineStyle.OutlineColor));
            addAttribute( polylineStyleElement, "outline_transparent", PolylineStyle.OutlineColor.A.ToString(CultureInfo.InvariantCulture));
        }
        /// <summary/>
        protected void addPointStyleElement(PointStyle PointStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement pointStyleElement = doc.CreateElement("point_style");
            layerElement.AppendChild(pointStyleElement);
            addAttribute( pointStyleElement, "display_kind", ((int)PointStyle.DisplayKind).ToString(CultureInfo.InvariantCulture));
            addAttribute( pointStyleElement, "color", ColorTranslator.ToHtml(PointStyle.Color));
            addAttribute( pointStyleElement, "size", PointStyle.Size.ToString(CultureInfo.InvariantCulture));
            addAttribute( pointStyleElement, "symbol", PointStyle.Symbol.ToString(CultureInfo.InvariantCulture));
            addAttribute( pointStyleElement, "font_name", PointStyle.FontName);
            addAttribute(pointStyleElement, "contentAlignment",PointStyle.ContentAlignment.ToString());
            if (PointStyle.Image != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    PointStyle.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Close();
                    string pngStr = System.Convert.ToBase64String(ms.ToArray());
                    addAttribute( pointStyleElement, "bitmap", pngStr);
                }
            }
        }
        /// <summary/>
        protected void processTitleStyle(XmlNode layerNode, TitleStyle TitleStyle)
        {
            XmlNode titleStyle = tryGetNodeByName(layerNode.ChildNodes, "title_style");
            if (titleStyle != null)
            {
                TitleStyle.TitlesVisible = titleStyle.Attributes["visible"].Value == "1";
                if (titleStyle.Attributes["visible_scale"] != null)
                    TitleStyle.VisibleScale = double.Parse(titleStyle.Attributes["visible_scale"].Value, CultureInfo.InvariantCulture);
                TitleStyle.Color = ColorTranslator.FromHtml(titleStyle.Attributes["color"].Value);
                TitleStyle.FontFamily = titleStyle.Attributes["font_name"].Value;
                TitleStyle.FontSize = float.Parse(titleStyle.Attributes["font_size"].Value, CultureInfo.InvariantCulture);
                TitleStyle.FontStyle = (FontStyle)int.Parse(titleStyle.Attributes["font_style"].Value, CultureInfo.InvariantCulture);
                if (titleStyle.Attributes["render_priority"] != null)
                    TitleStyle.RenderPriority = int.Parse(titleStyle.Attributes["render_priority"].Value, CultureInfo.InvariantCulture);
                if (titleStyle.Attributes["use_outline"] != null)
                    TitleStyle.UseOutline = titleStyle.Attributes["use_outline"].Value == "1";
                if (titleStyle.Attributes["outline_size"] != null)
                    TitleStyle.OutlineSize = int.Parse(titleStyle.Attributes["outline_size"].Value, CultureInfo.InvariantCulture);
                if (titleStyle.Attributes["lead_along"] != null)
                    TitleStyle.LeadAlong = titleStyle.Attributes["lead_along"].Value == "1";
            }
        }
        /// <summary/>
        protected void processPolygonStyle(XmlNode layerNode, PolygonStyle PolygonStyle)
        {
            XmlNode polygonStyle = tryGetNodeByName(layerNode.ChildNodes, "polygon_style");
            if (polygonStyle != null)
            {
                PolygonStyle.BorderWidth = float.Parse(polygonStyle.Attributes["border_width"].Value, CultureInfo.InvariantCulture);
                PolygonStyle.BorderColor = ColorTranslator.FromHtml(polygonStyle.Attributes["border_color"].Value);
                PolygonStyle.BorderDashStyle = (DashStyle)int.Parse(polygonStyle.Attributes["border_dash_style"].Value, CultureInfo.InvariantCulture);
                PolygonStyle.HatchStyle = (HatchStyle)int.Parse(polygonStyle.Attributes["hatch_style"].Value, CultureInfo.InvariantCulture);
                if (polygonStyle.Attributes["border_dash_cap"] != null)
                    PolygonStyle.BorderDashCap = (DashCap)int.Parse(polygonStyle.Attributes["border_dash_cap"].Value, CultureInfo.InvariantCulture);
                PolygonStyle.UseHatch = polygonStyle.Attributes["use_hatch"].Value == "1";
                if (polygonStyle.Attributes["border_visible"] != null)
                    PolygonStyle.BorderVisible = polygonStyle.Attributes["border_visible"].Value == "1";
                PolygonStyle.FillForeColor = ColorTranslator.FromHtml(polygonStyle.Attributes["fill_fore_color"].Value);
                PolygonStyle.FillBackColor = ColorTranslator.FromHtml(polygonStyle.Attributes["fill_back_color"].Value);

                if (polygonStyle.Attributes["fill_transparent"] != null)
                    PolygonStyle.FillBackColor = Color.FromArgb(int.Parse(polygonStyle.Attributes["fill_transparent"].Value, CultureInfo.InvariantCulture), PolygonStyle.FillBackColor);

                if (polygonStyle.Attributes["fill_pattern"] != null)
                    PolygonStyle.FillPattern = (BuiltInFillPatterns)int.Parse(polygonStyle.Attributes["fill_pattern"].Value, CultureInfo.InvariantCulture);
            }
        }
        /// <summary/>
        protected void processPolylineStyle(XmlNode layerNode, PolylineStyle PolylineStyle)
        {
            XmlNode polylineStyle = tryGetNodeByName(layerNode.ChildNodes, "polyline_style");
            if (polylineStyle != null)
            {
                PolylineStyle.Width = int.Parse(polylineStyle.Attributes["width"].Value, CultureInfo.InvariantCulture);

                PolylineStyle.Color = ColorTranslator.FromHtml(polylineStyle.Attributes["color"].Value);
                PolylineStyle.DashStyle = (DashStyle)int.Parse(polylineStyle.Attributes["dash_style"].Value, CultureInfo.InvariantCulture);

                if (polylineStyle.Attributes["dash_cap"] != null)
                    PolylineStyle.DashCap = (DashCap)int.Parse(polylineStyle.Attributes["dash_cap"].Value, CultureInfo.InvariantCulture);

                if (polylineStyle.Attributes["dash_pattern"] != null)
                    PolylineStyle.DashPattern = stringToFloatArray(polylineStyle.Attributes["dash_pattern"].Value);

                if (polylineStyle.Attributes["is_compound"] != null)
                    PolylineStyle.IsCompound = polylineStyle.Attributes["is_compound"].Value == "1";

                if (polylineStyle.Attributes["compound"] != null)
                    PolylineStyle.Compound = stringToFloatArray(polylineStyle.Attributes["compound"].Value);

                if (polylineStyle.Attributes["use_annex_line"] != null)
                {
                    PolylineStyle.UseAnnexLine = polylineStyle.Attributes["use_annex_line"].Value == "1";

                    PolylineStyle.AnnexColor = ColorTranslator.FromHtml(polylineStyle.Attributes["annex_color"].Value);
                    PolylineStyle.AnnexDashStyle = (DashStyle)int.Parse(polylineStyle.Attributes["annex_dash_style"].Value, CultureInfo.InvariantCulture);

                    if (polylineStyle.Attributes["annex_dash_cap"] != null)
                        PolylineStyle.AnnexDashCap = (DashCap)int.Parse(polylineStyle.Attributes["annex_dash_cap"].Value, CultureInfo.InvariantCulture);

                    if (polylineStyle.Attributes["annex_dash_pattern"] != null)
                        PolylineStyle.AnnexDashPattern = stringToFloatArray(polylineStyle.Attributes["annex_dash_pattern"].Value);

                    if (polylineStyle.Attributes["is_annex_compound"] != null)
                        PolylineStyle.IsAnnexCompound = polylineStyle.Attributes["is_annex_compound"].Value == "1";

                    if (polylineStyle.Attributes["annex_compound"] != null)
                        PolylineStyle.AnnexCompound = stringToFloatArray(polylineStyle.Attributes["annex_compound"].Value);
                }

                if (polylineStyle.Attributes["use_outline"] != null)
                    PolylineStyle.UseOutline = polylineStyle.Attributes["use_outline"].Value == "1";
                if (polylineStyle.Attributes["outline_color"] != null)
                    PolylineStyle.OutlineColor = ColorTranslator.FromHtml(polylineStyle.Attributes["outline_color"].Value);
                if (polylineStyle.Attributes["outline_width"] != null)
                    PolylineStyle.OutlineWidth = int.Parse(polylineStyle.Attributes["outline_width"].Value, CultureInfo.InvariantCulture);
                if (polylineStyle.Attributes["outline_transparent"] != null)
                    PolylineStyle.OutlineColor = Color.FromArgb(int.Parse(polylineStyle.Attributes["outline_transparent"].Value, CultureInfo.InvariantCulture), PolylineStyle.OutlineColor);
            }
        }
        /// <summary/>
        protected void processPointStyle(XmlNode layerNode, PointStyle PointStyle)
        {
            XmlNode pointStyle = tryGetNodeByName(layerNode.ChildNodes, "point_style");
            if (pointStyle != null)
            {
                if (pointStyle.Attributes["display_kind"] != null)
                    PointStyle.DisplayKind = (PointDisplayKind)(int.Parse(pointStyle.Attributes["display_kind"].Value, CultureInfo.InvariantCulture));

                if (pointStyle.Attributes["bitmap"] != null)
                {
                    byte[] bmpBytes = System.Convert.FromBase64String(pointStyle.Attributes["bitmap"].Value);
                    using (MemoryStream ms = new MemoryStream(bmpBytes))
                        PointStyle.Image = (Bitmap)Bitmap.FromStream(ms);
                }

                if (pointStyle.Attributes["font_name"] != null)
                    PointStyle.FontName = pointStyle.Attributes["font_name"].Value;
                else
                    PointStyle.FontName = "GeographicSymbols";
                ContentAlignment contentAlignment;
                if ( (pointStyle.Attributes["contentAlignment"]!=null) && (Enum.TryParse(pointStyle.Attributes["contentAlignment"].Value,true,out contentAlignment)))
                {
                    PointStyle.ContentAlignment = contentAlignment;
                }
                PointStyle.Color = ColorTranslator.FromHtml(pointStyle.Attributes["color"].Value);
                PointStyle.Size = int.Parse(pointStyle.Attributes["size"].Value, CultureInfo.InvariantCulture);
                PointStyle.Symbol = pointStyle.Attributes["symbol"].Value[0];
            }
        }

        /// <summary>
        /// Registry extension in FeatureLayer
        /// </summary>
        /// <param name="layer"></param>
        public abstract void RegistryExtension(FeatureLayer layer);

        /// <summary>
        /// UnRegistry extension in FeatureLayer
        /// </summary>
        /// <param name="layer"></param>
        public abstract void UnRegistryExtension(FeatureLayer layer);
       

        /// <summary>
        /// Delete Extesion
        /// </summary>
        public void DeleteExtension()
        {
            _layer.UnRegistreExtension(this);
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="layer">FeatureLayer which will be used for extension</param>
        protected FeatureLayerExtension(FeatureLayer layer)
        {
            _layer = layer;
            _layer.RegistreExtension(this);            
        }

        /// <summary>
        /// Save internal state in XmlNode
        /// </summary>
        /// <param name="node"></param>
        public abstract void processFromXml(XmlNode node);
        /// <summary>
        /// Restore internal state in XmlEllement
        /// </summary>
        /// <param name="element"></param>
        public abstract void addToXml(XmlElement element);

        /// <summary>
        /// Get and set use extension 
        /// </summary>
        public  bool UseExtension
        {
            get { return _useExtesion; }
            set { if (value)
            {

                foreach (var extension in _layer.FeatureLayerExtensions)
                {
                    extension.UseExtension = false;
                }
                _useExtesion = true;

            }
            else
            {
                _useExtesion = false;
            }
            }
        }
    }
}
