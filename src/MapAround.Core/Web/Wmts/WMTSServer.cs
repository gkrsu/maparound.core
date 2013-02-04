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
using MapAround.Web.Wms;

namespace MapAround.Web.Wmts
{
    /// <summary>
    /// Implements The OpenGIS® Web Map Tile Service Interface.
    /// <remarks>
    /// As the names of layers appearing in http-queries used their aliases 
    /// (MapAround.Mapping.Layer.Alias)
    /// </remarks>
    /// </summary>
    public class WMTSServer : MapServerBase
    {
        private WmtsServiceDescription _description;
        /// <summary>
        /// Get  the type of support the service
        /// </summary>
        public override string SupportServiceType { get { return @"WMTS"; } }
        /// <summary>
        /// Designer.
        /// </summary>       
        public WMTSServer(WmtsServiceDescription description)
            : base("1.0.0", "wmts",description)
        {
            _description = description;
        }

        #region Overrides of MapServerBase
        //Ends on the 545 line.

        /// <summary>
        /// GetCapabilities request
        /// </summary>
        protected override void GetCapabilities(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            if (string.Compare(requestParams["SERVICE"], "WMTS", _ignoreCase) != 0)
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Wrong SERVICE value in the GetCapabilities request. Should be \"WMTS\"",
                             responseOutputStream,
                             ref responseContentType);

            XmlDocument capabilities = WmtsCapabilities.GetCapabilities(_map, _description);
            responseContentType = "text/xml";
            XmlWriter writer = XmlWriter.Create(responseOutputStream);
            capabilities.WriteTo(writer);
            writer.Close();
        }

        /// <summary>
        ///GetFeatureInfo request
        /// </summary>       
        protected override void GetFeatureInfo(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            #region Request processing GetFeatureInfo

            int x = 0, y = 0, featureCount = 1;
            int row, column;

            if (requestParams["FEATURE_COUNT"] != null)
                int.TryParse(requestParams["FEATURE_COUNT"], out featureCount);

            if (requestParams["I"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter I undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["I"], out x))
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Parameter I has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            
            if (requestParams["J"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter J undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["J"], out y))
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Parameter J has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILEROW"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEROW undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["TILEROW"], out row))
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Parameter TILEROW has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILECOL"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILECOL undefined.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            else if (!int.TryParse(requestParams["TILECOL"], out column))
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Parameter TILECOL has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            
            string mimeTypeNeeded = "text/html";
            if (requestParams["INFO_FORMAT"] != null)
                mimeTypeNeeded = requestParams["INFO_FORMAT"];

            Tile tile = new Tile(_map, (uint)row, (uint)column);
            //_description.Tile = tile; //
            //tile.PixelSize = _description.GetScaleDenominator(_description.ZoomLevel[tileMatrixName]); //
            //tile.ScaleDenominator = _description.GetPixelSize(_description.ZoomLevel[tileMatrixName]); //
            BoundingRectangle bbox = tile.BBox;
            int width = tile.Width, height = tile.Height;

            List<FeatureLayer> queryableLayers = new List<FeatureLayer>();
            if (!string.IsNullOrEmpty(requestParams["LAYER"]))
            {
                string[] layers = requestParams["LAYER"].Split(new[] { ',' });
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
                        WmtsException(WmtsExceptionCode.LayerNotDefined,
                                     "Layer \"" + layer + "\" not found.",
                                     responseOutputStream,
                                     ref responseContentType);
                        return;
                    }
                    else if (!(l is FeatureLayer) || !((FeatureLayer)l).FeaturesSelectable)
                    {
                        WmtsException(WmtsExceptionCode.LayerNotQueryable,
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

        /// <summary>
        /// Request GetTile
        /// </summary>
        private void GetTile(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            #region Verify the request

            if (requestParams["LAYERS"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter LAYER not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["STYLES"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter STYLE not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILEMATRIXSET"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEMATRIXSET not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            
            if (requestParams["TILEMATRIX"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEMATRIX not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILEROW"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEROW not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILECOL"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILECOL not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["FORMAT"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter FORMAT not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            #endregion

            #region Render Settings

            Color backColor = Color.FromArgb(0, 0, 0, 0);

            if (_map.CosmeticRaster.Visible)
            {
                //Cosmetic layer all broken, this does not allow its use.
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "WMTS  not support this settings rendering.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }
            //Render for inscriptions.
            var wmsFeatureRender = new WmsFeatureRender(_map.RenderingSettings);

            ImageCodecInfo imageEncoder = getEncoderInfo(requestParams["FORMAT"]);
            if (imageEncoder == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Wrong mime-type in FORMAT parameter.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            int tileRow = 0;
            int tileCol = 0;
            
            if (!int.TryParse(requestParams["TILEROW"], out tileRow))
            {
                WmtsException(WmtsExceptionCode.InvalidDimensionValue,
                             "Parameter TILEROW has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (!int.TryParse(requestParams["TILECOL"], out tileCol))
            {
                WmsException(WmsExceptionCode.InvalidDimensionValue,
                             "Parameter TILECOL has wrong value.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            string tileMatrixName = "EPSG:3857:0";
            if (requestParams["TILEMATRIX"] != null)
                tileMatrixName = requestParams["TILEMATRIX"];

         

            Tile tile = new Tile(_map, (uint)tileRow, (uint)tileCol);
            tile.ScaleDenominator = _description.GetScaleDenominator(_description.ZoomLevel[tileMatrixName]);
            tile.PixelSize = _description.GetPixelSize(_description.ZoomLevel[tileMatrixName]);
            tile.ZoomLevel = (uint)_description.ZoomLevel[tileMatrixName];
         
            int width = tile.Width, height = tile.Height;

            BoundingRectangle originalBbox = tile.BBox;

            if (originalBbox == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Wrong Tile parameters.",
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

                if (!string.IsNullOrEmpty(requestParams["LAYER"]))
                {
                    #region Getting layers of inquiry

                    string[] layers = requestParams["LAYER"].Split(new[] { ',' });
                    if (_description.LayerLimit > 0)
                    {
                        if (layers.Length == 0 && _map.Layers.Count > _description.LayerLimit ||
                            layers.Length > _description.LayerLimit)
                        {
                            WmtsException(WmtsExceptionCode.OperationNotSupported,
                                         "The number of layers in the query exceeds the limit of layers in the WMTS.",
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
                            WmtsException(WmtsExceptionCode.LayerNotDefined,
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
                    WmtsException(WmtsExceptionCode.NotApplicable, except.Message, responseOutputStream, ref responseContentType);
                    return;
                }

                for (j = 0; j < _map.Layers.Count(); j++) //Restore it as it was.
                    _map.Layers[j].Visible = _defaultVisibility[j];
            }
        }

        /// <summary>
        /// Redirects to the request handler.
        /// </summary>        
        /// <returns>If <code>false</code> - handler was not found.</returns>
        protected override bool RequestAction(NameValueCollection requestParams, Stream responseOutputStream, ref string responseContentType)
        {
            if (string.Compare(requestParams["REQUEST"], "GetTile", _ignoreCase) == 0)
            {
                GetTile(requestParams, responseOutputStream, ref responseContentType);
                return true;
            }

            return base.RequestAction(requestParams, responseOutputStream, ref responseContentType);
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

            #region Checks the special params of WMTS request

            if (requestParams["TILEMATRIXSET"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEMATRIXSET is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILEMATRIX"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEMATRIX is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILEROW"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILEROW is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            if (requestParams["TILECOL"] == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                             "Required parameter TILECOL is not specified.",
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
            //else
            //{
            //    string authCode = "EPSG:-1";
            //    if (!string.IsNullOrEmpty(_map.CoodrinateSystemWKT))
            //    {
            //        IInfo coordinateSystem =
            //            CoordinateSystemWktDeserializer.Parse(_map.CoodrinateSystemWKT);
            //        authCode = coordinateSystem.Authority + ":" + coordinateSystem.AuthorityCode.ToString();
            //    }

            //    if (requestParams["SRS"] != authCode)
            //    {
            //        WmsException(WmsExceptionCode.InvalidSRS,
            //                     "SRS is not supported",
            //                     responseOutputStream,
            //                     ref responseContentType);
            //        return;
            //    }
            //}

            #endregion

            #region Tries to parse the special request params for WMTS request

            uint row, column;

            if (!uint.TryParse(requestParams["TILEROW"], out row))
            {
                WmtsException(WmtsExceptionCode.InvalidDimensionValue,
                    "Parameter TILEROW has a wrong value.",
                    responseOutputStream,
                    ref responseContentType);
            }

            if (!uint.TryParse(requestParams["TILECOL"], out column))
            {
                WmtsException(WmtsExceptionCode.InvalidDimensionValue,
                    "Parameter TILECOL has a wrong value.",
                    responseOutputStream,
                    ref responseContentType);
            }

            string tileMatrixName = "EPSG:3857:0";
            if (requestParams["TILEMATRIX"] != null)
                tileMatrixName = requestParams["TILEMATRIX"];

            Tile tile = new Tile(_map, row, column);
            width = tile.Width;
            height = tile.Height;
            tile.PixelSize = (_description as WmtsServiceDescription).GetPixelSize((_description as WmtsServiceDescription).ZoomLevel[tileMatrixName]); // Attention!!
            originalBbox = tile.BBox;

            if (originalBbox == null)
            {
                WmtsException(WmtsExceptionCode.NotApplicable,
                                "Wrong BBOX parameter.",
                                responseOutputStream,
                                ref responseContentType);
                return;
            }

            #endregion
        }

        #endregion
    }
}