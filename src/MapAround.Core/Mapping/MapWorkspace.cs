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
** File: MapWorkspace.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Classes representing a map workspace
**
=============================================================================*/

using System.Collections.ObjectModel;
using MapAround.ThematicLayer;

namespace MapAround.Mapping
{
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    using MapAround.Geometry;
    using System;

    /// <summary>
    /// Represents a map workspace.
    /// <para>
    /// Instances of this class is used to store map settings like 
    /// a list of layers; data sources, colors, fonts, visible area, 
    /// the scale of the map etc.
    /// </para>
    /// </summary>
    public class MapWorkspace
    {
        private Map _map = null;
        private BoundingRectangle _viewBox = new BoundingRectangle();
        private string _cosmeticRasterFileName = string.Empty;

        private static void addAttribute(XmlDocument doc, XmlElement element, string attributeName, string attributeValue)
        {
            element.Attributes.Append(doc.CreateAttribute(attributeName));
            element.Attributes[attributeName].Value = attributeValue;
        }

        private string floatArrayToString(float[] arr)
        {
            if (arr == null)
                return string.Empty;

            string str = string.Empty;
            foreach (float f in arr)
                str += f.ToString(CultureInfo.InvariantCulture) + ";";

            return str.Substring(0, str.Length - 1);
        }

        private void addTitleStyleElement(TitleStyle TitleStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement titleStyleElement = doc.CreateElement("title_style");
            layerElement.AppendChild(titleStyleElement);
            addAttribute(doc, titleStyleElement, "visible", TitleStyle.TitlesVisible ? "1" : "0");
            addAttribute(doc, titleStyleElement, "visible_scale", TitleStyle.VisibleScale.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, titleStyleElement, "color", ColorTranslator.ToHtml(TitleStyle.Color));
            addAttribute(doc, titleStyleElement, "font_name", TitleStyle.FontFamily);
            addAttribute(doc, titleStyleElement, "font_size", TitleStyle.FontSize.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, titleStyleElement, "font_style", ((int)TitleStyle.FontStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, titleStyleElement, "render_priority", ((int)TitleStyle.RenderPriority).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, titleStyleElement, "use_outline", TitleStyle.UseOutline ? "1" : "0");
            addAttribute(doc, titleStyleElement, "outline_size", TitleStyle.OutlineSize.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, titleStyleElement, "lead_along", TitleStyle.LeadAlong ? "1" : "0");
        }

        private void addPolygonStyleElement(PolygonStyle PolygonStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement polygonStyleElement = doc.CreateElement("polygon_style");
            layerElement.AppendChild(polygonStyleElement);
            addAttribute(doc, polygonStyleElement, "border_width", PolygonStyle.BorderWidth.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonStyleElement, "border_color", ColorTranslator.ToHtml(PolygonStyle.BorderColor));
            addAttribute(doc, polygonStyleElement, "border_visible", PolygonStyle.BorderVisible ? "1" : "0");
            addAttribute(doc, polygonStyleElement, "border_dash_style", ((int)PolygonStyle.BorderDashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonStyleElement, "border_dash_cap", ((int)PolygonStyle.BorderDashCap).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonStyleElement, "hatch_style", ((int)PolygonStyle.HatchStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonStyleElement, "use_hatch", PolygonStyle.UseHatch ? "1" : "0");
            addAttribute(doc, polygonStyleElement, "fill_fore_color", ColorTranslator.ToHtml(PolygonStyle.FillForeColor));
            addAttribute(doc, polygonStyleElement, "fill_back_color", ColorTranslator.ToHtml(PolygonStyle.FillBackColor));
            addAttribute(doc, polygonStyleElement, "fill_pattern", ((int)PolygonStyle.FillPattern).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonStyleElement, "fill_transparent", PolygonStyle.FillBackColor.A.ToString(CultureInfo.InvariantCulture));

        }

        private void addPolylineStyleElement(PolylineStyle PolylineStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement polylineStyleElement = doc.CreateElement("polyline_style");
            layerElement.AppendChild(polylineStyleElement);
            addAttribute(doc, polylineStyleElement, "width", PolylineStyle.Width.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylineStyleElement, "use_annex_line", PolylineStyle.UseAnnexLine ? "1" : "0");

            addAttribute(doc, polylineStyleElement, "color", ColorTranslator.ToHtml(PolylineStyle.Color));
            addAttribute(doc, polylineStyleElement, "dash_style", ((int)PolylineStyle.DashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylineStyleElement, "dash_cap", ((int)PolylineStyle.DashCap).ToString(CultureInfo.InvariantCulture));

            if (PolylineStyle.DashPattern != null)
                addAttribute(doc, polylineStyleElement, "dash_pattern", floatArrayToString(PolylineStyle.DashPattern));

            addAttribute(doc, polylineStyleElement, "is_compound", PolylineStyle.IsCompound ? "1" : "0");
            if (PolylineStyle.Compound != null)
                addAttribute(doc, polylineStyleElement, "compound", floatArrayToString(PolylineStyle.Compound));

            addAttribute(doc, polylineStyleElement, "annex_color", ColorTranslator.ToHtml(PolylineStyle.AnnexColor));
            addAttribute(doc, polylineStyleElement, "annex_dash_style", ((int)PolylineStyle.AnnexDashStyle).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylineStyleElement, "annex_dash_cap", ((int)PolylineStyle.AnnexDashCap).ToString(CultureInfo.InvariantCulture));

            if (PolylineStyle.DashPattern != null)
                addAttribute(doc, polylineStyleElement, "annex_dash_pattern", floatArrayToString(PolylineStyle.AnnexDashPattern));

            addAttribute(doc, polylineStyleElement, "is_annex_compound", PolylineStyle.IsAnnexCompound ? "1" : "0");
            if (PolylineStyle.AnnexCompound != null)
                addAttribute(doc, polylineStyleElement, "annex_compound", floatArrayToString(PolylineStyle.AnnexCompound));

            addAttribute(doc, polylineStyleElement, "use_outline", PolylineStyle.UseOutline ? "1" : "0");
            addAttribute(doc, polylineStyleElement, "outline_width", PolylineStyle.OutlineWidth.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylineStyleElement, "outline_color", ColorTranslator.ToHtml(PolylineStyle.OutlineColor));
            addAttribute(doc, polylineStyleElement, "outline_transparent", PolylineStyle.OutlineColor.A.ToString(CultureInfo.InvariantCulture));
        }

        private void addPointStyleElement(PointStyle PointStyle, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement pointStyleElement = doc.CreateElement("point_style");
            layerElement.AppendChild(pointStyleElement);
            addAttribute(doc, pointStyleElement, "display_kind", ((int)PointStyle.DisplayKind).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, pointStyleElement, "color", ColorTranslator.ToHtml(PointStyle.Color));
            addAttribute(doc, pointStyleElement, "size", PointStyle.Size.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, pointStyleElement, "symbol", PointStyle.Symbol.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, pointStyleElement, "font_name", PointStyle.FontName);
            addAttribute(doc,pointStyleElement, "contentAlignment", PointStyle.ContentAlignment.ToString());

            if (PointStyle.Image != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    PointStyle.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Close();
                    string pngStr = System.Convert.ToBase64String(ms.ToArray());
                    addAttribute(doc, pointStyleElement, "bitmap", pngStr);
                }
            }
        }

        private void addRasterbindingElement(RasterLayer layer, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement bindingElement = doc.CreateElement("binding");
            layerElement.AppendChild(bindingElement);

            addAttribute(doc, bindingElement, "raster_x", ((int)layer.Binding.RasterX).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, bindingElement, "raster_y", ((int)layer.Binding.RasterY).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, bindingElement, "map_x", ((double)layer.Binding.MapPoint.X).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, bindingElement, "map_y", ((double)layer.Binding.MapPoint.Y).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, bindingElement, "pixel_width", ((double)layer.Binding.PixelWidth).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, bindingElement, "pixel_height", ((double)layer.Binding.PixelHeight).ToString(CultureInfo.InvariantCulture));
        }

        private void addRasterStyleElement(RasterLayer layer, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement rasterStyleElement = doc.CreateElement("raster_style");
            layerElement.AppendChild(rasterStyleElement);
            addAttribute(doc, rasterStyleElement, "interpolation_mode", ((int)layer.Style.InterpolationMode).ToString(CultureInfo.InvariantCulture));

            addAttribute(doc, rasterStyleElement, "cm00", ((double)layer.Style.ColorAdjustmentMatrix.Matrix00).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm01", ((double)layer.Style.ColorAdjustmentMatrix.Matrix01).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm02", ((double)layer.Style.ColorAdjustmentMatrix.Matrix02).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm03", ((double)layer.Style.ColorAdjustmentMatrix.Matrix03).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm04", ((double)layer.Style.ColorAdjustmentMatrix.Matrix04).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm10", ((double)layer.Style.ColorAdjustmentMatrix.Matrix10).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm11", ((double)layer.Style.ColorAdjustmentMatrix.Matrix11).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm12", ((double)layer.Style.ColorAdjustmentMatrix.Matrix12).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm13", ((double)layer.Style.ColorAdjustmentMatrix.Matrix13).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm14", ((double)layer.Style.ColorAdjustmentMatrix.Matrix14).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm20", ((double)layer.Style.ColorAdjustmentMatrix.Matrix20).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm21", ((double)layer.Style.ColorAdjustmentMatrix.Matrix21).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm22", ((double)layer.Style.ColorAdjustmentMatrix.Matrix22).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm23", ((double)layer.Style.ColorAdjustmentMatrix.Matrix23).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm24", ((double)layer.Style.ColorAdjustmentMatrix.Matrix24).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm30", ((double)layer.Style.ColorAdjustmentMatrix.Matrix30).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm31", ((double)layer.Style.ColorAdjustmentMatrix.Matrix31).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm32", ((double)layer.Style.ColorAdjustmentMatrix.Matrix32).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm33", ((double)layer.Style.ColorAdjustmentMatrix.Matrix33).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm34", ((double)layer.Style.ColorAdjustmentMatrix.Matrix34).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm40", ((double)layer.Style.ColorAdjustmentMatrix.Matrix40).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm41", ((double)layer.Style.ColorAdjustmentMatrix.Matrix41).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm42", ((double)layer.Style.ColorAdjustmentMatrix.Matrix42).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm43", ((double)layer.Style.ColorAdjustmentMatrix.Matrix43).ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, rasterStyleElement, "cm44", ((double)layer.Style.ColorAdjustmentMatrix.Matrix44).ToString(CultureInfo.InvariantCulture));
        }

        private void addIndiciesElements(FeatureLayer layer, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement pointsIndexElement = doc.CreateElement("points_index");
            layerElement.AppendChild(pointsIndexElement);
            addAttribute(doc, pointsIndexElement, "box_square_threshold", layer.DefaultPointsIndexSettings.BoxSquareThreshold.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, pointsIndexElement, "max_depth", layer.DefaultPointsIndexSettings.MaxDepth.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, pointsIndexElement, "min_shape_count", layer.DefaultPointsIndexSettings.MinFeatureCount.ToString(CultureInfo.InvariantCulture));

            XmlElement polylinesIndexElement = doc.CreateElement("polylines_index");
            layerElement.AppendChild(polylinesIndexElement);
            addAttribute(doc, polylinesIndexElement, "box_square_threshold", layer.DefaultPolylinesIndexSettings.BoxSquareThreshold.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylinesIndexElement, "max_depth", layer.DefaultPolylinesIndexSettings.MaxDepth.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polylinesIndexElement, "min_shape_count", layer.DefaultPolylinesIndexSettings.MinFeatureCount.ToString(CultureInfo.InvariantCulture));

            XmlElement polygonsIndexElement = doc.CreateElement("polygons_index");
            layerElement.AppendChild(polygonsIndexElement);
            addAttribute(doc, polygonsIndexElement, "box_square_threshold", layer.DefaultPolygonsIndexSettings.BoxSquareThreshold.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonsIndexElement, "max_depth", layer.DefaultPolygonsIndexSettings.MaxDepth.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, polygonsIndexElement, "min_shape_count", layer.DefaultPolygonsIndexSettings.MinFeatureCount.ToString(CultureInfo.InvariantCulture));

        }

        private void addLegendElement(FeatureLayer layer, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement legendSettings = doc.CreateElement("legend_settings");
            layerElement.AppendChild(legendSettings);
            addAttribute(doc, legendSettings, "display_point_sample", layer.LegendSettings.DisplayPointSample ? "1" : "0");
            addAttribute(doc, legendSettings, "display_polyline_sample", layer.LegendSettings.DisplayPolylineSample ? "1" : "0");
            addAttribute(doc, legendSettings, "display_polygon_sample", layer.LegendSettings.DisplayPolygonSample ? "1" : "0");
            addAttribute(doc, legendSettings, "point_sample_title", layer.LegendSettings.PointSampleTitle);
            addAttribute(doc, legendSettings, "polyline_sample_title", layer.LegendSettings.PolylineSampleTitle);
            addAttribute(doc, legendSettings, "polygon_sample_title", layer.LegendSettings.PolygonSampleTitle);
        }

        private void addAutoTitleSettingsElement(FeatureLayer layer, XmlDocument doc, XmlElement layerElement)
        {
            if (layer.AutoTitleSettings != null)
            {
                XmlElement autoTitlesSettings = doc.CreateElement("auto_titles");
                layerElement.AppendChild(autoTitlesSettings);
                addAttribute(doc, autoTitlesSettings, "attribute_name", layer.AutoTitleSettings.AttributeName);
                addAttribute(doc, autoTitlesSettings, "attribute_index", layer.AutoTitleSettings.AttributeIndex.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void addDataProviderElement(LayerBase layer, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement dataProvider = doc.CreateElement("data_provider");
            layerElement.AppendChild(dataProvider);
            addAttribute(doc, dataProvider, "registration_name", layer.DataProviderRegName);

            XmlElement parameterList = doc.CreateElement("parameters");
            dataProvider.AppendChild(parameterList);
            foreach (string s in layer.DataProviderParameters.Keys)
            {
                XmlElement parameter = doc.CreateElement("parameter");
                parameterList.AppendChild(parameter);
                addAttribute(doc, parameter, "name", s);
                addAttribute(doc, parameter, "value", layer.DataProviderParameters[s]);
            }
        }

        private void addFeatureLayerExtesions(IEnumerable<FeatureLayerExtension> featureLayerExtensions, XmlDocument doc, XmlElement layerElement)
        {
            XmlElement layer_extensions = doc.CreateElement("layer_extensions");
            layerElement.AppendChild(layer_extensions);
            foreach (var extension in featureLayerExtensions)
            {
                XmlElement extension_ellement = doc.CreateElement("layer_extension");
                layer_extensions.AppendChild(extension_ellement);
                addAttribute(doc, extension_ellement, "registration_name", extension.GetType().AssemblyQualifiedName);
                extension.addToXml(extension_ellement);
            }
        }

        private void addLayerElement(LayerBase layer, XmlDocument doc, XmlElement layersElement)
        {
            XmlElement layerElement = doc.CreateElement("layer");
            layersElement.AppendChild(layerElement);
            addAttribute(doc, layerElement, "title", layer.Title);
            addAttribute(doc, layerElement, "description", layer.Description);
            addAttribute(doc, layerElement, "alias", layer.Alias);
            addAttribute(doc, layerElement, "visible", layer.Visible ? "1" : "0");
            addAttribute(doc, layerElement, "controllable", layer.Controllable ? "1" : "0");
            addAttribute(doc, layerElement, "cacheable", layer.Cacheable ? "1" : "0");
            addAttribute(doc, layerElement, "minimum_visible_scale", layer.MinVisibleScale.ToString(CultureInfo.InvariantCulture));
            addAttribute(doc, layerElement, "maximum_visible_scale", layer.MaxVisibleScale.ToString(CultureInfo.InvariantCulture));

            FeatureLayer fl = layer as FeatureLayer;

            if (fl != null)
            {
                addAttribute(doc, layerElement, "type", "vector");
#pragma warning disable 618
                addAttribute(doc, layerElement, "datasource", fl.DataSource);
                addAttribute(doc, layerElement, "datasource_type", ((int)fl.DataSourceType).ToString(CultureInfo.InvariantCulture));
#pragma warning restore 618
                addAttribute(doc, layerElement, "shapes_selectable", fl.FeaturesSelectable ? "1" : "0");
                addAttribute(doc, layerElement, "dynamic_data_load", fl.AreFeaturesAutoLoadable ? "1" : "0");

                addTitleStyleElement(fl.TitleStyle, doc, layerElement);
                addPolygonStyleElement(fl.PolygonStyle, doc, layerElement);
                addPolylineStyleElement(fl.PolylineStyle, doc, layerElement);
                addPointStyleElement(fl.PointStyle, doc, layerElement);
                addIndiciesElements(fl, doc, layerElement);
                addLegendElement(fl, doc, layerElement);
                addAutoTitleSettingsElement(fl, doc, layerElement);
                addDataProviderElement(fl, doc, layerElement);
                addAppDataElement(fl, doc, layerElement);
                if (fl.FeatureLayerExtensions.Count>0)
                {
                    addFeatureLayerExtesions(fl.FeatureLayerExtensions, doc, layerElement);
                }
            }

            RasterLayer rl = layer as RasterLayer;
            if (rl != null)
            {
                addAttribute(doc, layerElement, "type", "raster");

                addRasterbindingElement(rl, doc, layerElement);
                addRasterStyleElement(rl, doc, layerElement);
                addDataProviderElement(rl, doc, layerElement);
                addAppDataElement(rl, doc, layerElement);
            }
        }

        

        private void addAppDataElement(LayerBase layer, XmlDocument doc, XmlElement layerElement)
        {
            if (!string.IsNullOrEmpty(layer.ApplicationXmlData))
            {
                XmlElement appData = doc.CreateElement("application_data");
                appData.InnerXml = layer.ApplicationXmlData;
                layerElement.AppendChild(appData);
            }
        }

        private string getXml()
        {
            CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.LCID);
            ci.NumberFormat.NumberDecimalSeparator = ".";
            XmlDocument doc = new XmlDocument();
            XmlDeclaration declaration = doc.CreateXmlDeclaration("1.0", Encoding.UTF8.WebName, null);

            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(declaration, root);

            // --------- корень
            XmlElement workspaceRoot = doc.CreateElement("map_workspace");
            doc.AppendChild(workspaceRoot);

            addAttribute(doc, workspaceRoot, "version", "1.0");

            // --------- просмотр
            XmlElement viewBoxElement = doc.CreateElement("view_box");
            workspaceRoot.AppendChild(viewBoxElement);

            addAttribute(doc, viewBoxElement, "min_x", _viewBox.IsEmpty() ? string.Empty : _viewBox.MinX.ToString(ci));
            addAttribute(doc, viewBoxElement, "min_y", _viewBox.IsEmpty() ? string.Empty : _viewBox.MinY.ToString(ci));
            addAttribute(doc, viewBoxElement, "max_x", _viewBox.IsEmpty() ? string.Empty : _viewBox.MaxX.ToString(ci));
            addAttribute(doc, viewBoxElement, "max_y", _viewBox.IsEmpty() ? string.Empty : _viewBox.MaxY.ToString(ci));

            // --------- карта
            XmlElement mapElement = doc.CreateElement("map");
            workspaceRoot.AppendChild(mapElement);

            addAttribute(doc, mapElement, "title", _map.Title);
            addAttribute(doc, mapElement, "description", _map.Description);
            addAttribute(doc, mapElement, "antialias", _map.RenderingSettings.AntiAliasGeometry ? "1" : "0");
            addAttribute(doc, mapElement, "text_antialias", _map.RenderingSettings.AntiAliasText ? "1" : "0");
            addAttribute(doc, mapElement, "coordinate_system_wkt", _map.CoodrinateSystemWKT);

            if (!string.IsNullOrEmpty(_map.ApplicationXmlData))
            {
                XmlElement appData = doc.CreateElement("application_data");
                appData.InnerXml = _map.ApplicationXmlData;
                mapElement.AppendChild(appData);
            }

            //addAttribute(doc, mapElement, "use_transform_from_coordinate_system", "1");

            // --------- растр
            XmlElement rasterElement = doc.CreateElement("raster");
            mapElement.AppendChild(rasterElement);
            addAttribute(doc, rasterElement, "file_name", this.RasterFileName);

            addAttribute(doc, rasterElement, "min_x", _map.CosmeticRaster.Bounds.MinX.ToString(ci));
            addAttribute(doc, rasterElement, "min_y", _map.CosmeticRaster.Bounds.MinY.ToString(ci));
            addAttribute(doc, rasterElement, "max_x", _map.CosmeticRaster.Bounds.MaxX.ToString(ci));
            addAttribute(doc, rasterElement, "max_y", _map.CosmeticRaster.Bounds.MaxY.ToString(ci));
            addAttribute(doc, rasterElement, "visible", _map.CosmeticRaster.Visible ? "1" : "0");
            addAttribute(doc, rasterElement, "interpolation", ((int)_map.CosmeticRaster.InterpolationMode).ToString());


            // --------- слои
            XmlElement layersElement = doc.CreateElement("layers");
            mapElement.AppendChild(layersElement);

            foreach (LayerBase l in _map.Layers)
                addLayerElement(l, doc, layersElement);

            //using (MemoryStream ms = new MemoryStream())
            //{
            //    doc.Save(ms);
            //    UTF8Encoding utf8 = new UTF8Encoding();
            //    string s = utf8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            //    return s;
            //}

            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            return sw.ToString();
        }

        private void processRaster(XmlNode mapNode)
        {
            _map.CosmeticRaster.Image = null;
            _map.CosmeticRaster.Visible = false;
            _cosmeticRasterFileName = string.Empty;

            XmlNode raster = tryGetNodeByName(mapNode.ChildNodes, "raster");
            if (raster != null)
            {
                double x = double.Parse(raster.Attributes["min_x"].Value, CultureInfo.InvariantCulture);
                double y = double.Parse(raster.Attributes["min_y"].Value, CultureInfo.InvariantCulture);
                ICoordinate minP = PlanimetryEnvironment.NewCoordinate(x, y);

                x = double.Parse(raster.Attributes["max_x"].Value, CultureInfo.InvariantCulture);
                y = double.Parse(raster.Attributes["max_y"].Value, CultureInfo.InvariantCulture);
                ICoordinate maxP = PlanimetryEnvironment.NewCoordinate(x, y);
                _map.CosmeticRaster.Bounds = new BoundingRectangle(minP, maxP);

                _cosmeticRasterFileName = raster.Attributes["file_name"].Value;
                _map.CosmeticRaster.Visible = raster.Attributes["visible"].Value == "1";

                if (raster.Attributes["interpolation"] != null)
                    _map.CosmeticRaster.InterpolationMode = (InterpolationMode)int.Parse(raster.Attributes["interpolation"].Value);
            }
        }

        private void processViewBox(XmlNode workspaceNode)
        {
            XmlNode viewBox = tryGetNodeByName(workspaceNode.ChildNodes, "view_box");
            if (viewBox != null)
            {
                if (viewBox.Attributes["min_x"].Value.Length != 0 &&
                    viewBox.Attributes["min_y"].Value.Length != 0 &&
                    viewBox.Attributes["max_x"].Value.Length != 0 &&
                    viewBox.Attributes["max_y"].Value.Length != 0)
                {
                    double minX, minY, maxX, maxY;
                    minX = double.Parse(viewBox.Attributes["min_x"].Value, CultureInfo.InvariantCulture);
                    minY = double.Parse(viewBox.Attributes["min_y"].Value, CultureInfo.InvariantCulture);
                    maxX = double.Parse(viewBox.Attributes["max_x"].Value, CultureInfo.InvariantCulture);
                    maxY = double.Parse(viewBox.Attributes["max_y"].Value, CultureInfo.InvariantCulture);

                    _viewBox = new BoundingRectangle(minX, minY, maxX, maxY);
                }
                else
                    _viewBox = new BoundingRectangle();
            }
        }

        private void setXml(string value)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            XmlNode workspace = tryGetNodeByName(doc.ChildNodes, "map_workspace");
            if (workspace != null)
            {
                _map = new Map();

                processViewBox(workspace);

                XmlNode map = tryGetNodeByName(workspace.ChildNodes, "map");
                if (map != null)
                {
                    _map.Title = map.Attributes["title"].Value;
                    _map.Description = map.Attributes["description"].Value;
                    if (map.Attributes["antialias"] != null)
                        _map.RenderingSettings.AntiAliasGeometry = map.Attributes["antialias"].Value == "1";

                    if (map.Attributes["text_antialias"] != null)
                        _map.RenderingSettings.AntiAliasText = map.Attributes["text_antialias"].Value == "1";

                    if (map.Attributes["coordinate_system_wkt"] != null)
                        _map.CoodrinateSystemWKT = map.Attributes["coordinate_system_wkt"].Value;

                    processRaster(map);

                    XmlNode appData = tryGetNodeByName(map.ChildNodes, "application_data");
                    if (appData != null)
                        _map.ApplicationXmlData = appData.InnerXml;

                    XmlNode layers = tryGetNodeByName(map.ChildNodes, "layers");
                    if (layers != null)
                    {
                        foreach (XmlNode n in layers.ChildNodes)
                            if (n.Name == "layer")
                                processLayer(n);
                    }
                }
            }
            else
                throw new FormatException("Invalid workspace format");
        }

        private float[] stringToFloatArray(string str)
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

        private void processTitleStyle(XmlNode layerNode, TitleStyle TitleStyle)
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

        private void processPolygonStyle(XmlNode layerNode, PolygonStyle PolygonStyle)
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

        private void processPolylineStyle(XmlNode layerNode, PolylineStyle PolylineStyle)
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

        private void processBinding(XmlNode layerNode, RasterLayer l)
        {
            XmlNode binding = tryGetNodeByName(layerNode.ChildNodes, "binding");
            if (binding != null)
            {
                if (l.Binding == null)
                    l.Binding = new RasterLayer.RasterBinding();

                l.Binding.RasterX = int.Parse(binding.Attributes["raster_x"].Value, CultureInfo.InvariantCulture);
                l.Binding.RasterY = int.Parse(binding.Attributes["raster_y"].Value, CultureInfo.InvariantCulture);
                l.Binding.PixelWidth = double.Parse(binding.Attributes["pixel_width"].Value, CultureInfo.InvariantCulture);
                l.Binding.PixelHeight = double.Parse(binding.Attributes["pixel_height"].Value, CultureInfo.InvariantCulture);

                l.Binding.MapPoint = Geometry.PlanimetryEnvironment.NewCoordinate(
                    double.Parse(binding.Attributes["map_x"].Value, CultureInfo.InvariantCulture),
                    double.Parse(binding.Attributes["map_y"].Value, CultureInfo.InvariantCulture));
            }
        }

        private void processRasterStyle(XmlNode layerNode, RasterLayer l)
        {
            XmlNode rasterStyle = tryGetNodeByName(layerNode.ChildNodes, "raster_style");
            if (rasterStyle != null)
            {
                l.Style.InterpolationMode = (InterpolationMode)int.Parse(rasterStyle.Attributes["interpolation_mode"].Value, CultureInfo.InvariantCulture);

                l.Style.ColorAdjustmentMatrix.Matrix00 = float.Parse(rasterStyle.Attributes["cm00"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix01 = float.Parse(rasterStyle.Attributes["cm01"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix02 = float.Parse(rasterStyle.Attributes["cm02"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix03 = float.Parse(rasterStyle.Attributes["cm03"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix04 = float.Parse(rasterStyle.Attributes["cm04"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix10 = float.Parse(rasterStyle.Attributes["cm10"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix11 = float.Parse(rasterStyle.Attributes["cm11"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix12 = float.Parse(rasterStyle.Attributes["cm12"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix13 = float.Parse(rasterStyle.Attributes["cm13"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix14 = float.Parse(rasterStyle.Attributes["cm14"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix20 = float.Parse(rasterStyle.Attributes["cm20"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix21 = float.Parse(rasterStyle.Attributes["cm21"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix22 = float.Parse(rasterStyle.Attributes["cm22"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix23 = float.Parse(rasterStyle.Attributes["cm23"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix24 = float.Parse(rasterStyle.Attributes["cm24"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix30 = float.Parse(rasterStyle.Attributes["cm30"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix31 = float.Parse(rasterStyle.Attributes["cm31"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix32 = float.Parse(rasterStyle.Attributes["cm32"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix33 = float.Parse(rasterStyle.Attributes["cm33"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix34 = float.Parse(rasterStyle.Attributes["cm34"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix40 = float.Parse(rasterStyle.Attributes["cm40"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix41 = float.Parse(rasterStyle.Attributes["cm41"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix42 = float.Parse(rasterStyle.Attributes["cm42"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix43 = float.Parse(rasterStyle.Attributes["cm43"].Value, CultureInfo.InvariantCulture);
                l.Style.ColorAdjustmentMatrix.Matrix44 = float.Parse(rasterStyle.Attributes["cm44"].Value, CultureInfo.InvariantCulture);
            }
        }

        private void processPointStyle(XmlNode layerNode, PointStyle PointStyle)
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
                if ((pointStyle.Attributes["contentAlignment"] != null) && (Enum.TryParse(pointStyle.Attributes["contentAlignment"].Value, true, out contentAlignment)))
                {
                    PointStyle.ContentAlignment = contentAlignment;
                }

                PointStyle.Color = ColorTranslator.FromHtml(pointStyle.Attributes["color"].Value);
                PointStyle.Size = int.Parse(pointStyle.Attributes["size"].Value, CultureInfo.InvariantCulture);
                PointStyle.Symbol = pointStyle.Attributes["symbol"].Value[0];
            }
        }

        private void processIndicies(XmlNode layerNode, FeatureLayer l)
        {
            XmlNode pointsIndex = tryGetNodeByName(layerNode.ChildNodes, "points_index");
            if (pointsIndex != null)
            {
                l.DefaultPointsIndexSettings =
                    new IndexSettings(int.Parse(pointsIndex.Attributes["max_depth"].Value, CultureInfo.InvariantCulture),
                                      double.Parse(pointsIndex.Attributes["box_square_threshold"].Value, CultureInfo.InvariantCulture),
                                      int.Parse(pointsIndex.Attributes["min_shape_count"].Value, CultureInfo.InvariantCulture));
            }

            XmlNode polylinesIndex = tryGetNodeByName(layerNode.ChildNodes, "polylines_index");
            if (polylinesIndex != null)
            {
                l.DefaultPolylinesIndexSettings =
                    new IndexSettings(int.Parse(polylinesIndex.Attributes["max_depth"].Value, CultureInfo.InvariantCulture),
                                      double.Parse(polylinesIndex.Attributes["box_square_threshold"].Value, CultureInfo.InvariantCulture),
                                      int.Parse(polylinesIndex.Attributes["min_shape_count"].Value, CultureInfo.InvariantCulture));
            }

            XmlNode polygonsIndex = tryGetNodeByName(layerNode.ChildNodes, "polygons_index");
            if (polygonsIndex != null)
            {
                l.DefaultPolygonsIndexSettings =
                    new IndexSettings(int.Parse(polygonsIndex.Attributes["max_depth"].Value, CultureInfo.InvariantCulture),
                                      double.Parse(polygonsIndex.Attributes["box_square_threshold"].Value, CultureInfo.InvariantCulture),
                                      int.Parse(polygonsIndex.Attributes["min_shape_count"].Value, CultureInfo.InvariantCulture));
            }
        }

        private void processLegendSettings(XmlNode layerNode, FeatureLayer l)
        {
            XmlNode legendSettings = tryGetNodeByName(layerNode.ChildNodes, "legend_settings");
            if (legendSettings != null)
            {
                LayerLegendSettings settings =
                    new LayerLegendSettings(
                        legendSettings.Attributes["display_point_sample"].Value == "1",
                        legendSettings.Attributes["display_polyline_sample"].Value == "1",
                        legendSettings.Attributes["display_polygon_sample"].Value == "1",
                        legendSettings.Attributes["point_sample_title"].Value,
                        legendSettings.Attributes["polyline_sample_title"].Value,
                        legendSettings.Attributes["polygon_sample_title"].Value);

                l.LegendSettings = settings;
            }
        }

        private void processAutoTitlesSettings(XmlNode layerNode, FeatureLayer l)
        {
            XmlNode autoTitles = tryGetNodeByName(layerNode.ChildNodes, "auto_titles");
            if (autoTitles != null)
            {
                string attributeName = string.Empty;
                int attributeIndex = -1;
                bool nameExists = autoTitles.Attributes["attribute_name"] != null;
                bool indexExists = autoTitles.Attributes["attribute_index"] != null;

                if (nameExists)
                    attributeName = autoTitles.Attributes["attribute_name"].Value;
                if (indexExists)
                    attributeIndex = int.Parse(autoTitles.Attributes["attribute_index"].Value, CultureInfo.InvariantCulture);

                if (nameExists || indexExists)
                {
                    AutoTitleSettings settings = new AutoTitleSettings(attributeName, attributeIndex);
                    l.AutoTitleSettings = settings;
                }
            }
        }

        private void processDataProvider(XmlNode layerNode, LayerBase l)
        {
            XmlNode dataProvider = tryGetNodeByName(layerNode.ChildNodes, "data_provider");
            if (dataProvider != null)
            {
                l.DataProviderRegName = dataProvider.Attributes["registration_name"].Value;
                XmlNode parameters = tryGetNodeByName(dataProvider.ChildNodes, "parameters");
                if (parameters != null)
                {
                    Dictionary<string, string> p = new Dictionary<string, string>();
                    foreach (XmlNode parameter in parameters)
                        p.Add(parameter.Attributes["name"].Value, parameter.Attributes["value"].Value);

                    l.DataProviderParameters = p;
                }
            }
        }

        private void processAppData(XmlNode layerNode, LayerBase l)
        {
            XmlNode appData = tryGetNodeByName(layerNode.ChildNodes, "application_data");
            if (appData != null)
                l.ApplicationXmlData = appData.InnerXml;
        }

      

        private void processLayerExtension(XmlNode layerExtension, FeatureLayer fl)
        {
            Type type = Type.GetType(layerExtension.Attributes["registration_name"].Value);
            FeatureLayerExtension extension = (FeatureLayerExtension)Activator.CreateInstance(type,  fl);
            extension.processFromXml(layerExtension);            
        }

        private void processLayer(XmlNode node)
        {
            XmlAttribute layerType = node.Attributes["type"];
            LayerBase l;
            if (layerType != null && node.Attributes["type"].Value.ToString().Equals("raster"))
            {
                RasterLayer rl = new RasterLayer();

                processBinding(node, rl);
                processRasterStyle(node, rl);
                processDataProvider(node, rl);
                processAppData(node, rl);

                l = rl;
            }
            else
            {
                FeatureLayer fl = new FeatureLayer();
                if (node.Attributes["datasource"] != null)
                {
#pragma warning disable 618
                    fl.DataSource = node.Attributes["datasource"].Value;
                    fl.DataSourceType = (LayerDataSourceType)int.Parse(node.Attributes["datasource_type"].Value, CultureInfo.InvariantCulture);
#pragma warning restore 618
                }

                if (node.Attributes["dynamic_data_load"] != null)
                    fl.AreFeaturesAutoLoadable = node.Attributes["dynamic_data_load"].Value == "1";

                fl.FeaturesSelectable = node.Attributes["shapes_selectable"].Value == "1";


                processTitleStyle(node, fl.TitleStyle);
                processPolygonStyle(node, fl.PolygonStyle);
                processPolylineStyle(node, fl.PolylineStyle);
                processPointStyle(node, fl.PointStyle);
                processIndicies(node, fl);
                processLegendSettings(node, fl);
                processAutoTitlesSettings(node, fl);
                processDataProvider(node, fl);
                processAppData(node, fl);
                l = fl;
                XmlNode layer_extensions = tryGetNodeByName(node.ChildNodes, "layer_extensions");
                if (layer_extensions!= null)
                {
                    foreach (XmlNode layerExtension in layer_extensions.ChildNodes)
                    {


                        if (layerExtension.Name == "layer_extension")
                            processLayerExtension(layerExtension, fl);

                    }

                }
            }


            
          
            
            l.Title = node.Attributes["title"].Value;

            l.Description = node.Attributes["description"].Value;

            if (node.Attributes["alias"] != null)
                l.Alias = node.Attributes["alias"].Value;

            l.Visible = node.Attributes["visible"].Value == "1";
            

            if (node.Attributes["minimum_visible_scale"] != null)
                l.MinVisibleScale = double.Parse(node.Attributes["minimum_visible_scale"].Value, CultureInfo.InvariantCulture);
            if (node.Attributes["maximum_visible_scale"] != null)
                l.MaxVisibleScale = double.Parse(node.Attributes["maximum_visible_scale"].Value, CultureInfo.InvariantCulture);

            if (node.Attributes["controllable"] != null)
                l.Controllable = node.Attributes["controllable"].Value == "1";

            if (node.Attributes["cacheable"] != null)
                l.Cacheable = node.Attributes["cacheable"].Value == "1";

            _map.AddLayer(l);
        }

       

        private XmlNode tryGetNodeByName(XmlNodeList nodes, string name)
        {
            foreach (XmlNode node in nodes)
                if (node.Name == name)
                    return node;

            return null;
        }

        /// <summary>
        /// Gets or sets an Xml-representation of workspace.
        /// </summary>
        public string XmlRepresentation
        {
            get { return getXml(); }
            set { setXml(value); }
        }

        /// <summary>
        /// Gets or sets a MapAround.Mapping.Map 
        /// instance associated with this workspace.
        /// </summary>
        public Map Map
        {
            get { return _map; }
            set { _map = value; }
        }

        /// <summary>
        /// Gets or sets a bounding rectangle 
        /// defining a visible area of the map.
        /// </summary>
        public BoundingRectangle ViewBox
        {
            get { return _viewBox; }
            set { _viewBox = value; }
        }

        /// <summary>
        /// Gets or sets a filename of the raster.
        /// </summary>
        public string RasterFileName
        {
            get { return _cosmeticRasterFileName; }
            set { _cosmeticRasterFileName = value; }
        }

        /// <summary>
        /// Loads a workspace from a file containing 
        /// an xml-representation of workspace.
        /// </summary>
        /// <param name="fileName">File name</param>
        public void Load(string fileName)
        {
            XmlRepresentation = File.ReadAllText(fileName, Encoding.UTF8);
        }

        /// <summary>
        /// Saves xml-representation of this workspace to file.
        /// </summary>
        /// <param name="fileName">File name</param>
        public void Save(string fileName)
        {
            File.WriteAllText(fileName, XmlRepresentation);
        }
    }
}
