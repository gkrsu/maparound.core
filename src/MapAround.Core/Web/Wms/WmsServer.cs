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
** File: WmsServer.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: WMS-server implementation
**
=============================================================================*/

using MapAround.Web.Wmts;

namespace MapAround.Web.Wms
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Drawing.Imaging;
    using System.Drawing;
    using MapAround.Mapping;
    using MapAround.Geometry;
    using MapAround.Serialization;
    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Transformations;

    /// <summary>
    /// The MapArounde.Web.Wms workspace containing classes 
    /// that implement The OpenGIS® Web Map Service Interface Standard (WMS) 
    /// provides a simple HTTP interface for requesting geo-registered map 
    /// images from one or more distributed geospatial databases. 
    /// <para>
    /// A WMS request defines the geographic layer(s) and area of interest to 
    /// be processed. The response to the request is one or more geo-registered 
    /// map images (returned as JPEG, PNG, etc) that can be displayed in a 
    /// browser application. The interface also supports the ability to specify 
    /// whether the returned images should be transparent so that layers from 
    /// multiple servers can be combined or not.
    /// </para>
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Instances of this class provides data for 
    /// BeforeRenderNewImage event.
    /// </summary>
    public class RenderNewImageEventArgs : EventArgs
    {
        private LayerBase _layer;
        BoundingRectangle _bboxWithGutters;

        /// <summary>
        /// Gets a bounding rectangle defining the 
        /// drawing area with gutters.
        /// </summary>
        public BoundingRectangle BboxWithGutters
        {
            get { return _bboxWithGutters; }
        }


        /// <summary>
        /// Gets  Layer
        ///  </summary>
        public LayerBase Layer
        {
            get { return _layer; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Web.Wms.RenderNewImageEventArgs.
        /// </summary>
        /// <param name="bboxWithGutters">A bounding rectangle defining the drawing area with gutters</param>
        /// <param name="layer">Layer</param>
        internal RenderNewImageEventArgs(BoundingRectangle bboxWithGutters, LayerBase layer)
        {
            _bboxWithGutters = bboxWithGutters;
            _layer = layer;
        }
    }

    /// <summary>
    /// Training event layer to render.
    /// </summary>
    public class PrepareRenderFeatureLayerArgs: EventArgs
    {
        private FeatureLayer _layer;
        
         /// <summary/>         
        internal PrepareRenderFeatureLayerArgs(FeatureLayer layer)
        {
            _layer = layer;
        }
        /// <summary>
        /// Layer <see cref="FeatureLayer"/>.
        /// </summary>
        public FeatureLayer Layer
        {
            get { return _layer; }
        }
    }

    

    /// <summary>
    /// Implements The OpenGIS® Web Map Service Interface.
    /// <remarks>
    /// As the names of layers appearing in http-queries used their aliases 
    /// (MapAround.Mapping.Layer.Alias)
    /// </remarks>
    /// </summary>
    public class WMSServer:MapServerBase
    {
        WmsServiceDescription _description;
        /// <summary>
        /// Get  the type of support the service
        /// </summary>
        public override string SupportServiceType { get { return @"WMS"; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        public WMSServer(WmsServiceDescription description) : base("1.1.1","WMS",description)
        {
            _description = description;
        }

        /// <summary>
        /// Redirects to the request handler.
        /// </summary>        
        /// <returns>If <code>false</code> -handler was not found.</returns>
        protected override bool RequestAction(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            if (string.Compare(requestParams["REQUEST"], "GetMap", _ignoreCase) == 0)
            {
                GetMap(requestParams, responseOutputStream, ref responseContentType);
                return true;
            }

            return base.RequestAction(requestParams, responseOutputStream, ref responseContentType);


        }

        /// <summary>
        /// GetCapabilities request
        /// </summary>
        protected override void GetCapabilities(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            if (string.Compare(requestParams["SERVICE"], "WMS", _ignoreCase) != 0)
                WmsException(WmsExceptionCode.NotApplicable,
                             "Wrong SERVICE value in the GetCapabilities request. Should be \"WMS\"",
                             responseOutputStream,
                             ref responseContentType);

            XmlDocument capabilities = WmsCapabilities.GetCapabilities(_map, _description); 
            responseContentType = "text/xml";
            XmlWriter writer = XmlWriter.Create(responseOutputStream);
            capabilities.WriteTo(writer);
            writer.Close();
        }

        /// <summary>
        /// Request GetFeatureInfo
        /// </summary>       
        protected override void GetFeatureInfo(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            #region Processing Request GetFeatureInfo

            int featureCount = 1;
            if (requestParams["FEATURE_COUNT"] != null)
                int.TryParse(requestParams["FEATURE_COUNT"], out featureCount);

            int x = 0, y = 0;

            if (requestParams["X"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter X undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["X"], out x))
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Parameter X has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }


            if (requestParams["Y"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter Y undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["Y"], out y))
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Parameter Y has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            int width = 0;
            int height = 0;
            if (!int.TryParse(requestParams["WIDTH"], out width))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter WIDTH has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxWidth > 0 && width > _description.MaxWidth)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "Parameter WIDTH is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (!int.TryParse(requestParams["HEIGHT"], out height))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter HEIGHT has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxHeight > 0 && height > _description.MaxHeight)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "Parameter HEIGHT is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            BoundingRectangle bbox = ParseBbox(requestParams["bbox"]);
            if (bbox == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Parameter BBOX has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            string mimeTypeNeeded = "text/html";
            if (requestParams["INFO_FORMAT"] != null)
                mimeTypeNeeded = requestParams["INFO_FORMAT"];

            List<FeatureLayer> queryableLayers = new List<FeatureLayer>();
            if (!string.IsNullOrEmpty(requestParams["LAYERS"]))
            {
                string[] layers = requestParams["LAYERS"].Split(new[] { ',' });
                foreach (string layer in layers)
                {
                    LayerBase l = null;
                    int i;
                    for (i = 0; i < _map.Layers.Count; i++)
                        if (string.Equals(_map.Layers[i].Alias, layer,
                                          StringComparison.InvariantCultureIgnoreCase))
                            l = _map.Layers[i];


                    if (l == null)
                    {
                        WmsException(WmsExceptionCode.LayerNotDefined,
                                     "Layer \"" + layer + "\" not found.",
                                     responseOutputStream,
                                     ref responseContentType);
                        return;
                    }
                    else if (!(l is FeatureLayer) || !((FeatureLayer)l).FeaturesSelectable)
                    {
                        WmsException(WmsExceptionCode.LayerNotQueryable,
                                     "Layer \"" + layer + "\" is not queryable.",
                                     responseOutputStream,
                                     ref responseContentType);
                        return;

                    }
                    else
                        queryableLayers.Add((FeatureLayer)l);
                }

                queryableLayers.Sort(
                    (FeatureLayer l1, FeatureLayer l2) =>
                    _map.Layers.IndexOf(l1) > _map.Layers.IndexOf(l2) ? -1 : 1);

                List<Feature> selectedFeatures = new List<Feature>();

                if (queryableLayers.Count > 0)
                {
                    lock (_syncRoot)
                    {
                        // calculate the error of selection of point and line objects
                        _map.SelectionPointRadius =
                            _selectionMargin * bbox.Width / width;

                        double resultX = bbox.Width / width * x + bbox.MinX;
                        double resultY = bbox.MaxY - bbox.Height / height * y;

                        ICoordinate point = PlanimetryEnvironment.NewCoordinate(resultX, resultY);

                        ICoordinate tempPoint = PlanimetryEnvironment.NewCoordinate(x, y);
                        if (_map.OnTheFlyTransform != null)
                        {
                            ICoordinate delta =
                                PlanimetryEnvironment.NewCoordinate(tempPoint.X + _map.SelectionPointRadius,
                                                                    tempPoint.Y);
                            IMathTransform inverseTransform = _map.OnTheFlyTransform.Inverse();

                            delta =
                                PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(delta.Values()));

                            _map.SelectionPointRadius =
                                PlanimetryAlgorithms.Distance(
                                    PlanimetryEnvironment.NewCoordinate(
                                        inverseTransform.Transform(tempPoint.Values())), delta);


                        }
                        if (queryableLayers[0].Map.OnTheFlyTransform != null)
                        {
                            IMathTransform inverseTransform = queryableLayers[0].Map.OnTheFlyTransform.Inverse();
                            point =
                                PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(point.Values()));
                        }

                        foreach (LayerBase l in queryableLayers)
                        {
                            FeatureLayer fl = l as FeatureLayer;
                            if (fl != null)
                            {
                                Feature feature = null;
                                fl.SelectObject(point, out feature);
                                if (feature != null)
                                    selectedFeatures.Add(feature);

                                if (selectedFeatures.Count == featureCount)
                                    break;
                            }
                        }
                    }
                }


                WmsFeaturesInfoNeededEventArgs args = new WmsFeaturesInfoNeededEventArgs(selectedFeatures,
                                                                                         mimeTypeNeeded,
                                                                                         responseOutputStream,
                                                                                         responseContentType);
                OnFeaturesInfoNeeded(args);
                responseContentType = args.ResponseContentType;

                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("none");

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString().ToCharArray());
            responseOutputStream.Write(bytes, 0, bytes.Length);
            responseContentType = "text/html";

            #endregion   
        }

        private void GetMap(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            #region Verify the request

            if (requestParams["LAYERS"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter LAYERS not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (requestParams["STYLES"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter STYLES not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (requestParams["SRS"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter SRS not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else
            {
                string authCode = "EPSG:-1";
                if (!string.IsNullOrEmpty(_map.CoodrinateSystemWKT))
                {
                    IInfo coordinateSystem =
                        CoordinateSystemWktDeserializer.Parse(_map.CoodrinateSystemWKT);
                    authCode = coordinateSystem.Authority + ":" + coordinateSystem.AuthorityCode.ToString();
                }

                if (requestParams["SRS"] != authCode)
                {
                    WmsException(WmsExceptionCode.InvalidSRS,
                                 "SRS is not supported",
                                 responseOutputStream,
                                 ref responseContentType);
                    return;
                }
            }
            if (requestParams["BBOX"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter BBOX not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (requestParams["WIDTH"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter WIDTH not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (requestParams["HEIGHT"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter HEIGHT not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (requestParams["FORMAT"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter FORMAT not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            #endregion

            #region Render Settings

            Color backColor = Color.White;

            if (_map.CosmeticRaster.Visible)
            {
                //Cosmetic layer all broken, this does not allow its use.
                WmsException(WmsExceptionCode.NotApplicable,
                             "WMS  not support this settings rendering.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            

            if (string.Compare(requestParams["TRANSPARENT"], "TRUE", _ignoreCase) == 0)
                backColor = Color.Transparent;
            else if (requestParams["BGCOLOR"] != null)
            {
                try
                {
                    backColor = ColorTranslator.FromHtml("#" + requestParams["BGCOLOR"]);
                }
                catch
                {
                    WmsException(WmsExceptionCode.NotApplicable,
                                 "Parameter BGCOLOR has wrong value.",
                                 responseOutputStream,
                                 ref responseContentType);
                    return;
                }
            }


            ImageCodecInfo imageEncoder = getEncoderInfo(requestParams["FORMAT"]);
            if (imageEncoder == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Wrong mime-type in FORMAT parameter.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }


            int width = 0;
            int height = 0;

            if (!int.TryParse(requestParams["WIDTH"], out width))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter WIDTH has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxWidth > 0 && width > _description.MaxWidth)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "WIDTH parameter value is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (!int.TryParse(requestParams["HEIGHT"], out height))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter HEIGHT has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxHeight > 0 && height > _description.MaxHeight)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "HEIGHT parameter value is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            BoundingRectangle originalBbox = ParseBbox(requestParams["bbox"]);
            if (originalBbox == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Wrong BBOX parameter.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            #endregion

            //Selected by default.
            bool[] _defaultVisibility = new bool[_map.Layers.Count()];

            int j = 0;
            foreach (LayerBase layer in _map.Layers)
            {
                _defaultVisibility[j] = layer.Visible;
                layer.Visible = false; //Turning off all the layers.
                j++;
            }

            lock (_syncRoot)
            {
                LayerBase[] useLayers = null;
                int[] indexLayers = null;
                #region Checking layers of inquiry

                if (!string.IsNullOrEmpty(requestParams["LAYERS"]))
                {
                    #region Getting layers of inquiry

                    string[] layers = requestParams["LAYERS"].Split(new[] { ',' });
                    if (_description.LayerLimit > 0)
                    {
                        if (layers.Length == 0 && _map.Layers.Count > _description.LayerLimit ||
                            layers.Length > _description.LayerLimit)
                        {
                            WmsException(WmsExceptionCode.OperationNotSupported,
                                         "The number of layers in the query exceeds the limit of layers in the WMS.",
                                         responseOutputStream,
                                         ref responseContentType);
                            return;
                        }
                    }

                    #endregion

                    useLayers = new LayerBase[layers.Length];
                    indexLayers = new int[layers.Length];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        var layer = layers[i];
                        var findLayer = false;
                        for (int k = 0; k < _map.Layers.Count; k++)
                            if (string.Equals(_map.Layers[k].Alias, layer,
                                              StringComparison.InvariantCultureIgnoreCase))
                            {
                                useLayers[i] = _map.Layers[k];
                                indexLayers[i] = k;
                                findLayer = true;
                                break;
                            }


                        if (!findLayer)
                        {
                            WmsException(WmsExceptionCode.LayerNotDefined,
                                         "Layer \"" + layer + "\" not found.",
                                         responseOutputStream,
                                         ref responseContentType);
                            return;
                        }
                    }

                    Array.Sort(indexLayers, useLayers);
                }

                #endregion

                BoundingRectangle bboxWithGutters = (BoundingRectangle)originalBbox.Clone();
                bboxWithGutters.Grow((double)GutterSize * originalBbox.Width / (double)width);

                try
                {
                    using (Image bmp = GetImage(width, height, backColor, useLayers, bboxWithGutters))
                    {
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(
                            System.Drawing.Imaging.Encoder.Quality, (long)_imageQuality);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, imageEncoder, encoderParams);
                            byte[] buffer = ms.ToArray();
                            responseContentType = imageEncoder.MimeType;
                            responseOutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
                catch(Exception except)
                {
                    WmsException(WmsExceptionCode.NotApplicable, except.Message, responseOutputStream, ref responseContentType);
                    return;
                }

                for (j = 0; j < _map.Layers.Count(); j++) // Restore everything as it was.
                    _map.Layers[j].Visible = _defaultVisibility[j];
            }
        }

        /// <summary>
        /// Overrides CheckRequestParams of MapServerBase
        /// </summary>
        /// <param name="requestParams"></param>
        /// <param name="responseOutputStream"></param>
        /// <param name="responseContentType"></param>
        /// <param name="originalBbox"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected override void CheckRequestParams(NameValueCollection requestParams, Stream responseOutputStream,
                                                ref string responseContentType, out BoundingRectangle originalBbox, out int width, out int height)
        {
            originalBbox = null;
            width = 0;
            height = 0;

            #region Checks the special params of WMS request

            if (requestParams["BBOX"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter BBOX is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["WIDTH"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter WIDTH is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["HEIGHT"] == null)
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Required parameter HEIGHT is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["SRS"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter SRS not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else
            {
                string authCode = "EPSG:-1";
                if (!string.IsNullOrEmpty(_map.CoodrinateSystemWKT))
                {
                    IInfo coordinateSystem =
                        CoordinateSystemWktDeserializer.Parse(_map.CoodrinateSystemWKT);
                    authCode = coordinateSystem.Authority + ":" + coordinateSystem.AuthorityCode.ToString();
                }

                if (requestParams["SRS"] != authCode)
                {
                    WmsException(WmsExceptionCode.InvalidSRS,
                                 "SRS is not supported",
                                 responseOutputStream,
                                 ref responseContentType);
                    return;
                }
            }

            #endregion

            #region Tries to parse the special params of WMS request

            if (!int.TryParse(requestParams["WIDTH"], out width))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter WIDTH has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxWidth > 0 && width > _description.MaxWidth)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "WIDTH parameter value is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            if (!int.TryParse(requestParams["HEIGHT"], out height))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter HEIGHT has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (_description.MaxHeight > 0 && height > _description.MaxHeight)
            {
                WmsException(WmsExceptionCode.OperationNotSupported,
                             "HEIGHT parameter value is too large.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            originalBbox = ParseBbox(requestParams["bbox"]);

            if (originalBbox == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Wrong BBOX parameter.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            #endregion
        }
    }
}