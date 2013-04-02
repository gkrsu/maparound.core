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
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using MapAround.Caching;
using MapAround.CoordinateSystems;
using MapAround.CoordinateSystems.Transformations;
using MapAround.Geometry;
using MapAround.Mapping;
using MapAround.Serialization;
using MapAround.Web.Wms;
using MapAround.Web.Wmts;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace MapAround.Web
{
   
    /// <summary>
    /// Map server interface
    /// </summary>
    public interface IMapServer
    {
       

        /// <summary>
        /// Get  the type of support the service
        /// </summary>
        string SupportServiceType { get; }
        /// <summary>
        /// Sets an integer value defining the quality
        /// of the response images.
        /// <para>
        /// The smaller value is set the stronger compression 
        /// level is used to pack the result image.
        /// The value must be in the range from 0 to 100.
        /// </para>
        /// </summary>
        int ImageQuality {  set; }

        /// <summary>
        /// Sets a System.Threading.Semaphore instance 
        /// to limit the number of simultaneously drawing images.
        /// <remarks>
        /// Use this property to limit the usage of GDI resources.
        /// </remarks>
        /// </summary>
        Semaphore DrawingRequestSemaphore {  set; }

        /// <summary>
        /// Sets an interval for waiting a release of GDI 
        /// resources to generate the map image. Use with 
        /// DrawingRequestSemaphore.
        /// </summary>
        TimeSpan DrawingResourcesTimeOut {  set; }

        /// <summary>
        /// Sets an object providing access to the cache of tiles.
        /// </summary>
        ITileCacheAccessor TileCacheAccessor {  set; }

        /// <summary>
        /// Sets an object providing access to the cache of JSON objects.
        /// </summary>
        IJSONCacheAccessor JSONCacheAccessor { set; }

        /// <summary>
        /// Sets the MapAround.Mapping.Map instance
        /// which is used to render images and generate 
        /// other responces.
        /// </summary>
        Map Map {  set; }

        /// <summary>
        /// Sets the gutter size in pizels.
        /// Gutters causes a rendered images are slightly large sizes, 
        /// but eliminates undesirable edge effects like clipping symbols.
        /// Gutters do not affect the sizes of the final images because 
        /// they are cut.
        /// </summary>
        int GutterSize {  set; }

        /// <summary>
        /// Sets a value (in pixels) defining
        /// the selection tolerance.
        /// This value should be used to tune up the selection behavior.
        /// </summary>
        int SelectionMargin {  set; }

        /// <summary>
        /// Raises when in response to a GetFeatureInfo request need
        /// to be placed the information about the selected features.
        /// </summary>
        event EventHandler<WmsFeaturesInfoNeededEventArgs> FeaturesInfoNeeded;

        /// <summary>
        /// Rasies before the new map image rendering.
        /// If the requested image exists in cache, the event does not occur.
        /// <remarks>
        /// Use this event to execute long runnig operations such as extraction 
        /// of spatial data from a file or database in the when they are really 
        /// needed.
        /// </remarks>
        /// </summary>
        event EventHandler<RenderNewImageEventArgs> BeforeRenderNewImage;

        /// <summary>
        /// Rasies before the new Layer  rendering.
        /// </summary>
        event EventHandler<PrepareRenderFeatureLayerArgs> PrepareRenderFeatureLayer;

        /// <summary>
        /// Raises when the value of REQUEST does not match any of those listed in 
        /// Web Map Service Implementation Specification V 1.1.1. Handler subscribed 
        /// to this event should generate a response to the request itself, or to 
        /// signal that the request may not be processed.
        /// </summary>
        event EventHandler<WmsUnknownRequestEventArgs> UnknownRequest;

        /// <summary>
        /// Create response
        /// </summary>        
        void  GetResponse(
            NameValueCollection requestParams,
            Stream responseOutputStream, 
            out string responseContentType);
    }

    /// <summary>
    /// Базова реализация сервера WMxS
    /// </summary>
    public abstract class MapServerBase : IMapServer
    {
        /// <summary>
        /// Service description
        /// </summary>        
        internal object _syncRoot = new object();
        internal bool _ignoreCase = true;
        internal int _gutterSize = 0;
        internal int _selectionMargin = 3;
        internal Map _map = null;
        internal ITileCacheAccessor _tileCacheAccessor = null;
        internal IJSONCacheAccessor jsonCacheAccesor = null;
        internal Semaphore _drawingRequestSemaphore = null;
        internal TimeSpan _drawingResourcesTimeOut = new TimeSpan(0, 0, 5);
        internal static ImageCodecInfo _imageCodecInfo = null;
        internal int _imageQuality = 80;

        private BaseServiceDescription _description;
        /// <summary>
        /// Версия сервера.
        /// </summary>
        protected readonly string version = string.Empty;

        /// <summary>        
        ///Тип Сервера.
        /// </summary>
        protected readonly string serverType = string.Empty;

        /// <summary>
        /// Конструктор.
        /// </summary>        
        /// <param name="Version">Версия.</param>
        /// <param name="ServiceType">Тип сервиса.</param>
        /// <param name="description">Описание сервиса</param>
        protected MapServerBase(string Version, string ServiceType, BaseServiceDescription description)
        {            
            version = Version;
            serverType = ServiceType;
            _description = description;
        }

        static MapServerBase()
        {

            _imageCodecInfo = ImageCodecInfo.GetImageEncoders().First(inc => inc.MimeType == "image/png");
            
        }

        private Image GetImage(BoundingRectangle bboxWithGutters,string key)
        {
            if (!ReferenceEquals(_tileCacheAccessor,null))
            {
                var bytes = _tileCacheAccessor.ExtractTileBytes("all", bboxWithGutters, key, "png");
                if (!ReferenceEquals(bytes,null))
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        return Image.FromStream(ms);
                    }
                }
            }
            return null;
        }

        private void SetImage(BoundingRectangle bboxWithGutters, Image image, string key)
        {
            if (!ReferenceEquals(_tileCacheAccessor, null))
            {
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)_imageQuality);
               
                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms,_imageCodecInfo,encoderParams);
                        _tileCacheAccessor.SaveTileBytes("all",bboxWithGutters,key,ms.ToArray(),"png");
                    }
                
            }
           
        }

        /// <summary>
        /// Gets image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="backColor"></param>
        /// <param name="useLayers"></param>
        /// <param name="bboxWithGutters"></param>
        /// <returns></returns>
        protected Image GetImage(int width, int height, Color backColor, LayerBase[] useLayers, BoundingRectangle bboxWithGutters)
        {
            #region Проверка на занятость ресурсов

            // проверяем заняты ли ресурсы
            if (_drawingRequestSemaphore != null)
                if (!_drawingRequestSemaphore.WaitOne(_drawingResourcesTimeOut))                
                    throw new Exception(  "There are currently no resources on the server needed to generate the map image.");
            
            #endregion
            
            try
            {
                bool cacheable = true;
                string allLayerKey = "";

                foreach (var layer in useLayers.OrderBy(x=>x.Alias))
                {
                    cacheable &= layer.Cacheable;
                    allLayerKey += layer.Alias;
                }

                if (cacheable)
                {
                    Image res = GetImage(bboxWithGutters, allLayerKey);
                    
                        if (!ReferenceEquals(res,null))
                        {

                            return res;
                        }
                    
                }
                //Рендер для надписей.
                var wmsFeatureRender = new WmsFeatureRender(_map.RenderingSettings);

                using (Image bmpWithGutters = new Bitmap(width + _gutterSize * 2, height + _gutterSize * 2))
                {
                    // рисуем фон
                    using (Graphics g = Graphics.FromImage(bmpWithGutters))
                        g.Clear(backColor);
                    int i = 0;
                    //Проверка, что вообще есть что отображать.
                    if (!ReferenceEquals(useLayers, null) && (useLayers.Length > 0))
                        do
                        {
                            IFeatureRenderer _oldFeatureRender = null;
                            try
                            {

                                //Начинаем рисовать новый слой.
                                wmsFeatureRender.BeginLayerRender();

                                //Делаем слой видимым. Все остальные слои мы отключили выше.
                                useLayers[i].Visible = true;

                                //Настройка слоя перед рендерингом.
                                if (useLayers[i] is FeatureLayer)
                                    OnPrepareRenderFeatureLayer(new PrepareRenderFeatureLayerArgs((FeatureLayer)useLayers[i]));

                                //Устанавливаем свой рендер, что бы перекрыть отрисовку названий
                                if (useLayers[i] is FeatureLayer)
                                {
                                    _oldFeatureRender = (useLayers[i] as FeatureLayer).FeatureRenderer;
                                    (useLayers[i] as FeatureLayer).FeatureRenderer = wmsFeatureRender;
                                }

                                byte[] titleInfo = null;

                                //Смотрим в кэше.
                                using (Image bmpLayer = GetImageFromChach(useLayers[i], out titleInfo, bboxWithGutters))
                                {
                                    if (!ReferenceEquals(bmpLayer, null))
                                    {
                                        //Добавляем данные о названиях в wmsFeatureRender.
                                        wmsFeatureRender.AddTitleInfo(titleInfo);
                                        //Копируем картинку в результирующий тайл.
                                        CopyImage(bmpLayer, bmpWithGutters);
                                        continue;
                                    }
                                }

                                //Если промах по кэш
                                using (
                                    Image bmpLayer = new Bitmap(width + _gutterSize * 2, height + _gutterSize * 2,
                                                                PixelFormat.Format32bppArgb))
                                {
                                    //Загружаем данные (почему это делается вне WMS сам не понимаю).

                                    OnBeforeRenderNewImage(new RenderNewImageEventArgs(bboxWithGutters,useLayers[i]));
                                    //MapAround загрузит только видимые слои, по этому не заморачиваемся на загрузки слоев по отдельности.   


                                    // рисуем карту
                                    //MapAround отрендерит только видимые слои.
                                    //Также он попытается отрендерить косметический слой, по этой причине мы проверяем выше, что бы он не был задан.
                                    _map.Render(bmpLayer, bboxWithGutters);

                                    //Копируем результат на результирующий растр.
                                    CopyImage(bmpLayer, bmpWithGutters);

                                    // Кладем в кэш растр слоя + информацию о названиях.
                                    SetImageToChach(useLayers[i], bmpLayer, wmsFeatureRender.CurrentTitleInfo, bboxWithGutters);

                                }
                            }
                            finally
                            {

                                i++;
                                //Делаем слой не видимым.
                                useLayers[i - 1].Visible = false;

                                if (useLayers[i - 1] is FeatureLayer)
                                {
                                    //Меняем рендер на старый
                                    (useLayers[i - 1] as FeatureLayer).FeatureRenderer = _oldFeatureRender;
                                }
                            }
                        } while (i < useLayers.Length);

                    //После того как получили все слои запроса рендерим названия.
                    wmsFeatureRender.RenderTitle(bmpWithGutters, bboxWithGutters);

                    //Подготавливаем и отправляем результат.
                    Image bmp = new Bitmap(width, height);                  
                     CopyImageClipped(bmp, bmpWithGutters, width, height);
                     if (cacheable)
                     {
                         SetImage(bboxWithGutters,bmp,allLayerKey);
                     }
                    return bmp;
                }
            }
            finally
            {
                // освобождаем ресурсы, связанные с рисованием карты
                if (_drawingRequestSemaphore != null)
                    _drawingRequestSemaphore.Release();
            }
        }
       
       
            
       
        /// <summary>
        /// Writes information about layers
        /// </summary>

        protected void WriteLayer(JObject json, FeatureLayer layer, BoundingRectangle bboxWithGutters, BoundingRectangle mapViewBox, double scaleFactor, NameValueCollection requestParams)
        {
            

            Newtonsoft.Json.Linq.JObject parent = new Newtonsoft.Json.Linq.JObject();            
            parent.Add("Title", layer.Title);
            parent.Add("Alias", layer.Alias);
            parent.Add("Description", layer.Description);
            parent.Add("Visibility", layer.Visible);
            parent.Add("Controllable", layer.Controllable);
            parent.Add("Cacheable", layer.Cacheable);
            parent.Add("MinVisibleScale", layer.MinVisibleScale);
            parent.Add("MaxVisibleScale", layer.MaxVisibleScale);
            parent.Add("FeatureAttributeNames",JToken.FromObject((layer.FeatureAttributeNames.ToArray())));
            
                        
            json.Add("Layer", parent);

            json.Add("PointStyle",JToken.FromObject(layer.PointStyle));
            json.Add("PolygonStyle", JToken.FromObject(layer.PolygonStyle));
            json.Add("PolylineStyle", JToken.FromObject(layer.PolylineStyle));
            json.Add("TitleStyle", JToken.FromObject(layer.TitleStyle));          
            IList<JToken> _list = new List<JToken>();
             // выполняем проецирование на лету

            foreach (var extension in layer.FeatureLayerExtensions)
            {
                if (extension is ThematicLayer.ThematicLayerExtension)
                {
                    json.Add("ThematicLayerExtension", JToken.FromObject(extension));
                }
            }

            json.Add("FeatureCount", layer.Features.Count());

            if (requestParams.AllKeys.Contains("FEATURES"))
            {
                foreach (var feature in layer.Features)
                {
                    if (!feature.BoundingRectangle.Intersects(mapViewBox)) continue;
                    Newtonsoft.Json.Linq.JObject pointJSON = new Newtonsoft.Json.Linq.JObject();
                    pointJSON.Add("Attributes", JToken.FromObject(feature.Attributes));

                    JObject Geometry = new JObject();
                    switch (feature.FeatureType)
                    {
                        case FeatureType.Polyline:
                            if (Map.OnTheFlyTransform != null)
                            {
                                Polyline p = GeometryTransformer.TransformPolyline(feature.Polyline, Map.OnTheFlyTransform);
                                Feature tempFeature = new Feature(FeatureType.Polyline);
                                tempFeature.Title = feature.Title;
                                tempFeature.Selected = feature.Selected;
                                tempFeature.Polyline = p;
                                JSONMpHelper.DrawPolyline(tempFeature, Geometry, bboxWithGutters, scaleFactor);
                            }
                            else
                                JSONMpHelper.DrawPolyline(feature, Geometry, bboxWithGutters, scaleFactor); break;
                        case FeatureType.Polygon:
                            if (Map.OnTheFlyTransform != null)
                            {
                                Polygon p = GeometryTransformer.TransformPolygon(feature.Polygon, Map.OnTheFlyTransform);
                                Feature tempFeature = new Feature(FeatureType.Polygon);
                                tempFeature.Title = feature.Title;
                                tempFeature.Selected = feature.Selected;
                                tempFeature.Polygon = p;
                                JSONMpHelper.DrawPolygon(tempFeature, Geometry, bboxWithGutters, scaleFactor);
                            }
                            else
                                JSONMpHelper.DrawPolygon(feature, Geometry, bboxWithGutters, scaleFactor); break;
                        case FeatureType.Point:
                        case FeatureType.MultiPoint:

                            // выполняем проецирование на лету
                            if (Map.OnTheFlyTransform != null)
                            {
                                IGeometry p = null;
                                if (feature.FeatureType == FeatureType.Point)
                                    p = GeometryTransformer.TransformPoint(feature.Point, Map.OnTheFlyTransform);
                                else
                                    p = GeometryTransformer.TransformMultiPoint(feature.MultiPoint, Map.OnTheFlyTransform);

                                Feature tempFeature = new Feature(p);
                                tempFeature.Title = feature.Title;
                                tempFeature.Selected = feature.Selected;
                                JSONMpHelper.DrawPoint(tempFeature, Geometry, bboxWithGutters, scaleFactor);
                            }
                            else
                                JSONMpHelper.DrawPoint(feature, Geometry, bboxWithGutters, scaleFactor); break;
                        default:
                            throw new NotSupportedException();
                    }

                    pointJSON.Add("Geometry", Geometry);
                    pointJSON.Add("FeatureType", JToken.FromObject(feature.FeatureType));
                    _list.Add(pointJSON);
                }
                json.Add("features", JToken.FromObject(_list.ToArray()));
            }
        }



        /// <summary>
        /// Returns a vector of objects
        /// </summary>

        protected string GetVector(int layerNumber, int width, int height, LayerBase[] useLayers, BoundingRectangle bboxWithGutters, NameValueCollection requestParams)
        {
            StringWriter stringWriter = new StringWriter();
            Newtonsoft.Json.JsonWriter js = new JsonTextWriter(stringWriter);
            int i = layerNumber;
            JObject objResponse = new JObject();
            //Проверка, что вообще есть что отображать.
            if (!ReferenceEquals(useLayers, null) && (useLayers.Length > 0))
                //do
                {
                    JObject objLayer = new JObject();
                    try
                    {
                        //Делаем слой видимым. Все остальные слои мы отключили выше.
                        useLayers[i].Visible = true;

                        //Настройка слоя перед рендерингом.
                        if (useLayers[i] is FeatureLayer)
                            OnPrepareRenderFeatureLayer(new PrepareRenderFeatureLayerArgs((FeatureLayer)useLayers[i]));

                        //Загружаем данные (почему это делается вне WMS сам не понимаю).

                        OnBeforeRenderNewImage(new RenderNewImageEventArgs(bboxWithGutters, useLayers[i]));
                        //MapAround загрузит только видимые слои, по этому не заморачиваемся на загрузки слоев по отдельности.   
                        BoundingRectangle mapCoordsViewBox = Map.MapViewBoxFromPresentationViewBox(bboxWithGutters);
                    

                        // количество пикселей в единичном отрезке карты (в координатах представления)
                        double scaleFactor = Math.Min(width / bboxWithGutters.Width, height / bboxWithGutters.Height);

                        //Если информация уже загружена, извлекается из кэша, если нет - вызывается WriteLayer и кэшируется
                        if (ReferenceEquals(GetJSONFromCache(useLayers[i], requestParams), null))
                        {
                            if (scaleFactor >= useLayers[i].MinVisibleScale && scaleFactor <= useLayers[i].MaxVisibleScale)
                            {
                                WriteLayer(objLayer, useLayers[i] as FeatureLayer, bboxWithGutters, mapCoordsViewBox, scaleFactor, requestParams);
                                SetJSONToCache(useLayers[i], objLayer.ToString(), requestParams);
                            }
                        }
                        else
                        {
                            objLayer = JObject.Parse(GetJSONFromCache(useLayers[i], requestParams));
                        }
                        
                        objResponse.Add(useLayers[i].Alias,objLayer);
                        objResponse.WriteTo(js);
                    }
                    finally
                    {                                                       
                    }
                } //while (i < useLayers.Length);
                        
            return stringWriter.ToString();          
        }

        /// <summary>
        /// Получение информации о карте в формате json.
        /// </summary>       
        protected void GetVectorInfo(NameValueCollection requestParams, Stream responseOutputStream,
                                                ref string responseContentType)
        {
            responseContentType = string.Empty;

            #region Проверка общих параметров запроса

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

            if (requestParams["FORMAT"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter FORMAT is not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return;
            }

            #endregion

            BoundingRectangle originalBbox = null;
            int width, height;
            CheckRequestParams(requestParams, responseOutputStream, ref responseContentType, out originalBbox, out width, out height);

            //Видимость по умолчанию.
            bool[] _defaultVisibility = new bool[_map.Layers.Count()];

            int j = 0;
            foreach (LayerBase layer in _map.Layers)
            {
                _defaultVisibility[j] = layer.Visible;
                layer.Visible = false; //Отключаем все слои.
                j++;
            }

            lock (_syncRoot)
            {
                LayerBase[] useLayers = null;
                int[] indexLayers = null;
                #region Проверка слоев из запроса

                if (!string.IsNullOrEmpty(requestParams["LAYERS"]))
                {
                    #region Получение слоев из запроса

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
                    for (int i = 0; i < useLayers.Count(); i++)
                    {
                        var array =
                                System.Text.UTF8Encoding.UTF8.GetBytes(GetVector(i, width, height, useLayers,
                                                                                bboxWithGutters, requestParams)); //!!! layerNumber

                        responseContentType = "application/json";
                        responseOutputStream.Write(array, 0, array.Length);
                    }
                }
                catch (Exception except)
                {
                    WmsException(WmsExceptionCode.NotApplicable, except.Message, responseOutputStream, ref responseContentType);
                    return;
                }

                for (j = 0; j < _map.Layers.Count(); j++) //Восстанавливаем все как было.
                    _map.Layers[j].Visible = _defaultVisibility[j];
            }
        }

        /// <summary>
        /// Checks and tries to parse special request params for WMxS server
        /// WMS: BBOX, WIDTH, HEIGHT, SRS
        /// WMTS: TILEMATRIXSET, TILEMATRIX, TILEROW, TILECOL, SRS
        /// </summary>
        /// <param name="requestParams"></param>
        /// <param name="responseOutputStream"></param>
        /// <param name="responseContentType"></param>
        /// <param name="originalBbox"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        protected abstract void CheckRequestParams(NameValueCollection requestParams, Stream responseOutputStream,
                                                ref string responseContentType, out BoundingRectangle originalBbox, out int width, out int height);

        /// <summary>
        /// Get  the type of support the service
        /// </summary>
        public abstract string SupportServiceType { get; }

        /// <summary>
        /// Gets or sets an integer value defining the quality
        /// of the response images.
        /// <para>
        /// The smaller value is set the stronger compression 
        /// level is used to pack the result image.
        /// The value must be in the range from 0 to 100.
        /// </para>
        /// </summary>
        public int ImageQuality
        {
            get { return _imageQuality; }
            set 
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException("value");

                _imageQuality = value; 
            }
        }

        /// <summary>
        /// Gets or sets a System.Threading.Semaphore instance 
        /// to limit the number of simultaneously drawing images.
        /// <remarks>
        /// Use this property to limit the usage of GDI resources.
        /// </remarks>
        /// </summary>
        public Semaphore DrawingRequestSemaphore
        {
            get { return _drawingRequestSemaphore; }
            set { _drawingRequestSemaphore = value; }
        }

        /// <summary>
        /// Gets or sets an interval for waiting a release of GDI 
        /// resources to generate the map image. Use with 
        /// DrawingRequestSemaphore.
        /// </summary>
        public TimeSpan DrawingResourcesTimeOut
        {
            get { return _drawingResourcesTimeOut; }
            set { _drawingResourcesTimeOut = value; }
        }

        /// <summary>
        /// Gets or sets an object providing access to the cache of tiles.
        /// </summary>
        public ITileCacheAccessor TileCacheAccessor
        {
            get { return _tileCacheAccessor; }
            set 
            {
                
#if DEMO
                throw new NotSupportedException("Tile caching does not supported in demo version.");
#else
                _tileCacheAccessor = value; 
#endif
            }
        }

        /// <summary>
        /// Gets or sets an object providing accsess to the cache of JSON objects.
        /// </summary>
        public IJSONCacheAccessor JSONCacheAccessor
        {
            //get { return jsonCacheAccesor; }
            set { jsonCacheAccesor = value; }
        }

        /// <summary>
        /// Gets or sets the MapAround.Mapping.Map instance
        /// which is used to render images and generate 
        /// other responces.
        /// </summary>
        public Map Map
        {
            get { return _map; }
            set { _map = value; }
        }

        /// <summary>
        /// Gets or sets the gutter size in pizels.
        /// Gutters causes a rendered images are slightly large sizes, 
        /// but eliminates undesirable edge effects like clipping symbols.
        /// Gutters do not affect the sizes of the final images because 
        /// they are cut.
        /// </summary>
        public int GutterSize
        {
            get { return _gutterSize; }
            set { _gutterSize = value; }
        }

        /// <summary>
        /// Gets or sets a value (in pixels) defining
        /// the selection tolerance.
        /// This value should be used to tune up the selection behavior.
        /// </summary>
        public int SelectionMargin
        {
            get { return _selectionMargin; }
            set { _selectionMargin = value; }
        }

        /// <summary>
        /// Raises when in response to a GetFeatureInfo request need
        /// to be placed the information about the selected features.
        /// </summary>
        public  event EventHandler<WmsFeaturesInfoNeededEventArgs> FeaturesInfoNeeded;

        /// <summary>
        /// Rasies before the new map image rendering.
        /// If the requested image exists in cache, the event does not occur.
        /// <remarks>
        /// Use this event to execute long runnig operations such as extraction 
        /// of spatial data from a file or database in the when they are really 
        /// needed.
        /// </remarks>
        /// </summary>
        public  event EventHandler<RenderNewImageEventArgs> BeforeRenderNewImage;

        /// <summary>
        /// Rasies before the new Layer  rendering.
        /// </summary>
        public  event EventHandler<PrepareRenderFeatureLayerArgs> PrepareRenderFeatureLayer;

        /// <summary>
        /// Raises when the value of REQUEST does not match any of those listed in 
        /// Web Map Service Implementation Specification V 1.1.1. Handler subscribed 
        /// to this event should generate a response to the request itself, or to 
        /// signal that the request may not be processed.
        /// </summary>
        public  event EventHandler<WmsUnknownRequestEventArgs> UnknownRequest;

        
        /// <summary>
        /// Загрузка данных из кэша
        /// </summary>        
        protected Image GetImageFromChach(LayerBase layer, out byte[] titleInfo, BoundingRectangle bboxWithGutters)
        {
            titleInfo = null;
            if ((_tileCacheAccessor != null) && (layer.Cacheable)) // можно работать с кэшем
            {
                byte[] tileBytes = _tileCacheAccessor.ExtractTileBytes(layer.Alias,bboxWithGutters, getTileString(bboxWithGutters),"png");
                titleInfo = _tileCacheAccessor.ExtractTileBytes(layer.Alias, bboxWithGutters,
                                                                            getTileString(bboxWithGutters), "title");
                if ( (tileBytes != null) && (titleInfo!=null) )
                {
                    using (MemoryStream resultStream = new MemoryStream(tileBytes))
                    {

                        return Image.FromStream(resultStream);

                    }

                }
                return null;
            }
            return null;
        }

        /// <summary>
        ///  Сохранения данных в кэш.
        /// </summary>        
         protected void SetImageToChach(LayerBase layer, Image image, byte[] titleInfo, BoundingRectangle bboxWithGutters)
        {
            if (ReferenceEquals(_tileCacheAccessor, null)) return;
            if (!layer.Cacheable) return;
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)_imageQuality);

            using (MemoryStream resultStream = new MemoryStream())
            {
                image.Save(resultStream, _imageCodecInfo, encoderParams);

                byte[] imageBuffer = resultStream.ToArray();
                _tileCacheAccessor.SaveTileBytes(layer.Alias, bboxWithGutters, getTileString(bboxWithGutters),  imageBuffer,"png");
                _tileCacheAccessor.SaveTileBytes(layer.Alias, bboxWithGutters, getTileString(bboxWithGutters),titleInfo,"title");
            }

        }

        /// <summary>
        /// Gets a JSON object from a cache
        /// </summary>
        protected string GetJSONFromCache(LayerBase layer, NameValueCollection requestParams)
        {
            if (jsonCacheAccesor != null)
            {
                string name = layer.Alias + (requestParams.AllKeys.Contains("FEATURES") ? "withFeatures" : "");
                return jsonCacheAccesor.ExtractJSONBytes(name);
            }
            return null;
        }

        /// <summary>
        /// Sets a JSON object to a cache
        /// </summary>
        protected void SetJSONToCache(LayerBase layer, string jsonObject, NameValueCollection requestParams)
        {
            if (ReferenceEquals(jsonCacheAccesor, null))
                return;
            
            string name = layer.Alias + (requestParams.AllKeys.Contains("FEATURES") ? "withFeatures" : "");
            jsonCacheAccesor.SaveJSONBytes(name, jsonObject);
        }

        /// <summary>
        /// Получение текстового описания области видимости.
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        private string getTileString(BoundingRectangle bbox)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(bbox.MinX.ToString(CultureInfo.InvariantCulture));
            sb.Append(bbox.MinY.ToString(CultureInfo.InvariantCulture));
            sb.Append(bbox.MaxX.ToString(CultureInfo.InvariantCulture));
            sb.Append(bbox.MaxY.ToString(CultureInfo.InvariantCulture));

            return sb.ToString();
        }
        
        /// <summary>
        /// Копирования картинки с обрезанием.
        /// </summary>
        internal void CopyImageClipped(Image bmp, Image bmpWithGutters, int width, int height)
        {
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImageUnscaledAndClipped(bmpWithGutters,
                                              new Rectangle(-_gutterSize,
                                                            -_gutterSize,
                                                            width + _gutterSize,
                                                            height + _gutterSize));

        }

        internal void CopyImage(Image bmpLayer, Image bmpWithGutters)
        {
            using (Graphics g = Graphics.FromImage(bmpWithGutters))
            {
                g.DrawImageUnscaled(bmpLayer, 0, 0);
            }
        }

        internal static ImageCodecInfo getEncoderInfo(string mimeType)
        {
            foreach (ImageCodecInfo encoder in ImageCodecInfo.GetImageEncoders())
                if (encoder.MimeType == mimeType)
                    return encoder;
            return null;
        }

        /// <summary>
        /// Writes an error message into specified stream in compliance to WMS standard.
        /// </summary>
        /// <param name="code">The WMS error code</param>
        /// <param name="Message">The error message</param>
        /// <param name="responseOutputStream">The System.IO.Stream to write the error message</param>
        /// <param name="responseContentType">String for mime-type of response</param>
        public static void WmsException(WmsExceptionCode code, 
                                        string Message, 
                                        Stream responseOutputStream,
                                        ref string responseContentType)
        {
            responseContentType = "text/xml";

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n");
            sb.Append("<ServiceExceptionReport version=\"1.1.1\" xmlns=\"http://www.opengis.net/ogc\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n");
            sb.Append("<ServiceException");
            if (code != WmsExceptionCode.NotApplicable)
                sb.Append(" code=\"" + code + "\"");
            sb.Append(">" + Message + "</ServiceException>\n");
            sb.Append("</ServiceExceptionReport>");

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString().ToCharArray());
            responseOutputStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes an error message into specified stream in compliance to WMTS standard.
        /// </summary>
        /// <param name="code">The WMTS error code</param>
        /// <param name="Message">The error message</param>
        /// <param name="responseOutputStream">The System.IO.Stream to write the error message</param>
        /// <param name="responseContentType">String for mime-type of response</param>
        public static void WmtsException(WmtsExceptionCode code,
                                        string Message,
                                        Stream responseOutputStream,
                                        ref string responseContentType)
        {
            responseContentType = "text/xml";

            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n");
            sb.Append("<ServiceExceptionReport version=\"1.1.1\" xmlns=\"http://www.opengis.net/ogc\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n");
            sb.Append("<ServiceException");
            if (code != WmtsExceptionCode.NotApplicable)
                sb.Append(" code=\"" + code + "\"");
            sb.Append(">" + Message + "</ServiceException>\n");
            sb.Append("</ServiceExceptionReport>");

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString().ToCharArray());
            responseOutputStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Represents WMS error codes.
        /// </summary>
        public enum WmsExceptionCode
        {
            /// <summary>
            /// Request contains a Format not offered by the server.
            /// </summary>
            InvalidFormat,
            /// <summary>
            /// Request contains a SRS not offered by the server for one or more of the
            /// Layers in the request.
            /// </summary>
            InvalidSRS,
            /// <summary>
            /// GetMap request is for a Layer not offered by the server, or GetFeatureInfo
            /// request is for a Layer not shown on the map.
            /// </summary>
            LayerNotDefined,
            /// <summary>
            /// Request is for a Layer in a Style not offered by the server.
            /// </summary>
            StyleNotDefined,
            /// <summary>
            /// GetFeatureInfo request is applied to a Layer which is not declared queryable.
            /// </summary>
            LayerNotQueryable,
            /// <summary>
            /// GetFeatureInfo request contains invalid X or Y value.
            /// </summary>
            InvalidPoint,
            /// <summary>
            /// Value of (optional) UpdateSequence parameter in GetCapabilities request is
            /// equal to current value of service metadata update sequence number.
            /// </summary>
            CurrentUpdateSequence,
            /// <summary>
            /// Value of (optional) UpdateSequence parameter in GetCapabilities request is
            /// greater than current value of service metadata update sequence number.
            /// </summary>
            InvalidUpdateSequence,
            /// <summary>
            /// Request does not include a sample dimension value, and the server did not
            /// declare a default value for that dimension.
            /// </summary>
            MissingDimensionValue,
            /// <summary>
            /// Request contains an invalid sample dimension value.
            /// </summary>
            InvalidDimensionValue,
            /// <summary>
            /// Request is for an optional operation that is not supported by the server.
            /// </summary>
            OperationNotSupported,
            /// <summary>
            /// No error code
            /// </summary>
            NotApplicable
        }

        /// <summary>
        /// Represents WMTS error codes.
        /// </summary>
        public enum WmtsExceptionCode
        {
            /// <summary>
            /// Request contains a Format not offered by the server.
            /// </summary>
            InvalidFormat,
            /// <summary>
            /// Request contains a SRS not offered by the server for one or more of the
            /// Layers in the request.
            /// </summary>
            InvalidSRS,
            /// <summary>
            /// GetMap request is for a Layer not offered by the server, or GetFeatureInfo
            /// request is for a Layer not shown on the map.
            /// </summary>
            LayerNotDefined,
            /// <summary>
            /// Request is for a Layer in a Style not offered by the server.
            /// </summary>
            StyleNotDefined,
            /// <summary>
            /// GetFeatureInfo request is applied to a Layer which is not declared queryable.
            /// </summary>
            LayerNotQueryable,
            /// <summary>
            /// GetFeatureInfo request contains invalid X or Y value.
            /// </summary>
            InvalidPoint,
            /// <summary>
            /// Value of (optional) UpdateSequence parameter in GetCapabilities request is
            /// equal to current value of service metadata update sequence number.
            /// </summary>
            CurrentUpdateSequence,
            /// <summary>
            /// Value of (optional) UpdateSequence parameter in GetCapabilities request is
            /// greater than current value of service metadata update sequence number.
            /// </summary>
            InvalidUpdateSequence,
            /// <summary>
            /// Request does not include a sample dimension value, and the server did not
            /// declare a default value for that dimension.
            /// </summary>
            MissingDimensionValue,
            /// <summary>
            /// Request contains an invalid sample dimension value.
            /// </summary>
            InvalidDimensionValue,
            /// <summary>
            /// Request is for an optional operation that is not supported by the server.
            /// </summary>
            OperationNotSupported,
            /// <summary>
            /// No error code
            /// </summary>
            NotApplicable
        }

        #region EventInvoke
      
        /// <summary>
        /// Вызвать событие FeaturesInfoNeeded
        /// </summary>
        /// <param name="args"></param>
        protected void OnFeaturesInfoNeeded(WmsFeaturesInfoNeededEventArgs args)
         {
             if (FeaturesInfoNeeded != null) FeaturesInfoNeeded(this, args);
         }
     
        /// <summary>
        /// Вызвать событие BeforeRenderNewImage
        /// </summary>
        /// <param name="args"></param>
        protected void OnBeforeRenderNewImage(RenderNewImageEventArgs args)
         {
             if (BeforeRenderNewImage != null) BeforeRenderNewImage(this, args);
         }

        /// <summary>
        /// Вызвать событие PrepareRenderFeatureLayer
        /// </summary>
        /// <param name="args"></param>
        protected void OnPrepareRenderFeatureLayer(PrepareRenderFeatureLayerArgs args)
        {
            if (PrepareRenderFeatureLayer!=null)
            {
                PrepareRenderFeatureLayer(this, args);
            }
        }
     
        /// <summary>
        /// Вызвать событие UnknownRequest
        /// </summary>
        /// <param name="args"></param>
        protected void OnUnknownRequest(WmsUnknownRequestEventArgs args)
        {
            if (UnknownRequest != null)
                UnknownRequest(this, args);
        }
        #endregion
        
        /// <summary>
        /// Проверка настройки окружения.
        /// </summary>
        protected virtual bool TestEnvironment(Stream responseOutputStream, ref string responseContentType)
        {
            if (_map == null)
                throw (new InvalidOperationException("Undefined map"));

            if (_map.Layers.Count == 0)
                throw (new InvalidOperationException("Map does not contain layers"));
            return true;
        }

        /// <summary>
        /// Проверка общих параметров запроса.
        /// </summary>                
        protected virtual bool TestCommonParams(NameValueCollection requestParams,Stream responseOutputStream, ref string responseContentType)
        {
            if (requestParams["REQUEST"] == null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                             "Required parameter REQUEST not specified.",
                             responseOutputStream,
                             ref responseContentType);
                return false;
            }
            if (requestParams["SERVICE"]==null)
            {
                WmsException(WmsExceptionCode.NotApplicable,
                                  "Required parameter SERVICE not specified.",
                                  responseOutputStream,
                                  ref responseContentType);
                return false;   
            }
            else
            {
                if (string.Compare(requestParams["SERVICE"], serverType, _ignoreCase) != 0)
                {
                    WmsException(WmsExceptionCode.NotApplicable,
                                 string.Format("Only SERVICE {0} supported", serverType),
                                 responseOutputStream,
                                 ref responseContentType);
                    return false;
                }
            }
            if (requestParams["VERSION"] != null)
            {
                if (string.Compare(requestParams["VERSION"],version, _ignoreCase) != 0)
                {
                    WmsException(WmsExceptionCode.NotApplicable,
                                 string.Format("Only Version {0} supported",version),
                                 responseOutputStream,
                                 ref responseContentType);
                    return false;
                }
            }
            else
            {
                WmsException(WmsExceptionCode.NotApplicable,
                                "Required parameter VERSION not specified.",
                                responseOutputStream,
                                ref responseContentType);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Запрос GetCapabilities
        /// </summary>
     
        protected abstract void GetCapabilities(NameValueCollection requestParams, Stream responseOutputStream,
                                                ref string responseContentType);

        /// <summary>
        /// Запрос GetFeatureInfo
        /// </summary>       
        protected abstract void GetFeatureInfo(NameValueCollection requestParams, Stream responseOutputStream,
                                               ref string responseContentType);

        /// <summary>
        /// Перенаправляет на обработчик запроса.
        /// </summary>        
        /// <returns>Если <code>false</code> - обработчик не был найден.</returns>
        protected virtual  bool RequestAction(NameValueCollection requestParams,Stream responseOutputStream, ref string responseContentType)
        {
            switch 
            (requestParams["REQUEST"])
            {
                case "GetCapabilities":
                    GetCapabilities(requestParams, responseOutputStream, ref responseContentType);
                    return true;
                case "GetFeatureInfo":
                    GetFeatureInfo(requestParams, responseOutputStream, ref responseContentType);
                    return true;
                case "GetVectorInfo":
                    GetVectorInfo(requestParams, responseOutputStream, ref responseContentType);
                    return true;
                default:
                    return false;
            }

        }

        /// <summary>
        /// Create response
        /// </summary>        
        public virtual void  GetResponse(
                                NameValueCollection requestParams,
                                Stream responseOutputStream, 
                                out string responseContentType)
        {

            responseContentType = string.Empty;
            #region Проверка параметров запроса

            if (!TestEnvironment(responseOutputStream, ref responseContentType)) return;
            if (!TestCommonParams(requestParams, responseOutputStream, ref responseContentType)) return;        



            #endregion
          
            if (!RequestAction(requestParams, responseOutputStream, ref responseContentType))
            {

                WmsUnknownRequestEventArgs args = new WmsUnknownRequestEventArgs(requestParams,
                                                                                 responseOutputStream);
                OnUnknownRequest(args);
                if (args.IsHandled)
                {
                    responseContentType = args.ResponseContentType;
                    return;
                }

                WmsException(WmsExceptionCode.OperationNotSupported,
                             "Invalid request.",
                             responseOutputStream,
                             ref responseContentType);
            }
        }

        /// <summary>
        /// Чтение BBOX;
        /// </summary>        
        protected static BoundingRectangle ParseBbox(string bboxStr)
        {
            string[] strVals = bboxStr.Split(new[] { ',' });
            if (strVals.Length != 4)
                return null;

            double minx = 0, miny = 0, maxx = 0, maxy = 0;

            if (!double.TryParse(strVals[0], NumberStyles.Float, CultureInfo.InvariantCulture, out minx))
                return null;
            if (!double.TryParse(strVals[2], NumberStyles.Float, CultureInfo.InvariantCulture, out maxx))
                return null;
            if (maxx < minx)
                return null;

            if (!double.TryParse(strVals[1], NumberStyles.Float, CultureInfo.InvariantCulture, out miny))
                return null;
            if (!double.TryParse(strVals[3], NumberStyles.Float, CultureInfo.InvariantCulture, out maxy))
                return null;
            if (maxy < miny)
                return null;

            return new BoundingRectangle(minx, miny, maxx, maxy);
        }
        
    }

    /// <summary>
    /// 
    /// </summary>
    public class JSONMpHelper 
    {
        #region Вспомогательные методы рисования полилиний

       

        private static void drawVisiblePolylinePart(JObject g, List<ICoordinate> path, 
                                                    BoundingRectangle viewBox, double scaleFactor)
        {
            double[][] points = new double[path.Count][];

            for (int k = 0; k < path.Count; k++)
            {
                points[k] = new double[2];
                points[k][0] =  ((path[k].X - viewBox.MinX)*scaleFactor);
                points[k][1] =  ((viewBox.MaxY - path[k].Y)*scaleFactor);
            }

            
            g.Add("points",JToken.FromObject(points));
        }

        private static List<ICoordinate> getCrossPoints(ICoordinate vertex1, ICoordinate vertex2,
                                                        BoundingRectangle viewBox)
        {
            List<ICoordinate> result = new List<ICoordinate>();
            Segment currentSegment = new Segment(vertex1.X,
                                                 vertex1.Y,
                                                 vertex2.X,
                                                 vertex2.Y);

            addCrossPointToList(new Segment(viewBox.MinX, viewBox.MinY, viewBox.MinX, viewBox.MaxY),
                                currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MinX, viewBox.MaxY, viewBox.MaxX, viewBox.MaxY),
                                currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MaxX, viewBox.MaxY, viewBox.MaxX, viewBox.MinY),
                                currentSegment, result);

            addCrossPointToList(new Segment(viewBox.MaxX, viewBox.MinY, viewBox.MinX, viewBox.MinY),
                                currentSegment, result);

            return result;
        }

        private static void addCrossPointToList(Segment s1, Segment s2, List<ICoordinate> list)
        {
            Segment stub = new Segment();
            ICoordinate crossPoint = PlanimetryEnvironment.NewCoordinate(0, 0);

            Dimension crossKind =
                PlanimetryAlgorithms.SegmentsIntersection(s1, s2, out crossPoint, out stub);
            if (crossKind == Dimension.Zero)
                list.Add(crossPoint);
        }

        
       

        private static void drawVisiblePolylinePartWithStyleDetection(JObject g, List<ICoordinate> path,
                                                                      BoundingRectangle viewBox,
                                                                      double scaleFactor)
        {
            // Необходимо вычислять смещение шаблона пера перед рисованием полилинии,
            // иначе генерируемые изображения будут не пригодны для сшивания.
            // Смещение шаблона в нулевой вершине полилинии полагается равным нулю.
            // Зная длину от нулевой вершины до отображаемой части полилинии, можно
            // расчитать смещение шаблона в начале отображаемой части.

          
            // основная полилиния
            drawVisiblePolylinePart(g, path,  viewBox, scaleFactor);
           
        }

        private static double getPathLength(List<ICoordinate> path)
        {
            LinePath tempPart = new LinePath();
            tempPart.Vertices = path;
            return tempPart.Length();
        }

        private static void drawPolylineWithIntersectCalculation(JObject g, Feature feature, 
                                                          BoundingRectangle viewBox, double scaleFactor)
        {
            

            List<ICoordinate> currentPath = new List<ICoordinate>();

           
           
                    foreach (LinePath path in feature.Polyline.Paths)
                    {
                        if (path.Vertices.Count < 2)
                            continue;

                        currentPath.Clear();
                      
                        bool isInternalPath = viewBox.ContainsPoint(path.Vertices[0]);
                      
                        IList<ICoordinate> vertices = path.Vertices;

                        for (int j = 0; j < path.Vertices.Count; j++)
                        {
                            if (isInternalPath) // внутренняя часть полилинии
                            {
                                if (viewBox.ContainsPoint(vertices[j])) // остаемся внутри
                                {
                                    currentPath.Add(vertices[j]);
                                    continue;
                                }
                                else // выходим наружу
                                {
                                    // добавляем точку пересечения
                                    List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1], viewBox);
                                    currentPath.Add(crossPoints[0]);

                                    //рисуем
                                    drawVisiblePolylinePartWithStyleDetection(g, currentPath, 
                                                                              viewBox, scaleFactor);


                                    // инициализируем массив точек внешней части полилинии
                                    // и продолжаем выполнение
                                    currentPath.Clear();
                                    currentPath.Add(crossPoints[0]);
                                    currentPath.Add(vertices[j]);
                                    isInternalPath = false;
                                    continue;
                                }
                            }
                            else // внешняя часть полилинии
                            {
                                if (viewBox.ContainsPoint(vertices[j])) // входим внутрь
                                {
                                    // добавляем точку пересечения
                                    List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1], viewBox);
                                    currentPath.Add(crossPoints[0]);


                                    // инициализируем массив точек внутренней части полилинии
                                    // и продолжаем выполнение
                                    currentPath.Clear();
                                    currentPath.Add(crossPoints[0]);
                                    currentPath.Add(vertices[j]);
                                    isInternalPath = true;
                                    continue;
                                }
                                else // пересекаем дважды, либо остаемся снаружи
                                {
                                    // ищем точки пересечения
                                    if (j > 0)
                                    {
                                        List<ICoordinate> crossPoints = getCrossPoints(vertices[j], vertices[j - 1],
                                                                                       viewBox);
                                        if (crossPoints.Count == 0) // остались снаружи
                                        {
                                            currentPath.Add(vertices[j]);
                                            continue;
                                        }
                                        if (crossPoints.Count == 2) // пересекли 2 раза
                                        {
                                            // определяем какую из точек пересечения нужно добавить к текущему пути
                                            double d0 = PlanimetryAlgorithms.Distance(crossPoints[0], vertices[j - 1]);
                                            double d1 = PlanimetryAlgorithms.Distance(crossPoints[1], vertices[j - 1]);
                                            if (d0 < d1)
                                                currentPath.Add(crossPoints[0]);
                                            else
                                                currentPath.Add(crossPoints[1]);


                                            currentPath.Clear();

                                            currentPath.Add(crossPoints[d0 < d1 ? 0 : 1]);
                                            currentPath.Add(crossPoints[d0 < d1 ? 1 : 0]);

                                            // рисуем отрезок
                                            drawVisiblePolylinePartWithStyleDetection(g, currentPath, 
                                                                                      viewBox, scaleFactor
                                                                                      );
                                       

                                            // инициализируем внешнюю часть полилинии
                                            currentPath.Clear();
                                            if (d0 < d1)
                                                currentPath.Add(crossPoints[1]);
                                            else
                                                currentPath.Add(crossPoints[0]);
                                            currentPath.Add(vertices[j]);
                                            isInternalPath = false;
                                            continue;
                                        }
                                    }
                                    else // 1-ю точку нужно просто добавить в список
                                    {
                                        currentPath.Add(vertices[j]);
                                        continue;
                                    }
                                }
                            }
                        }

                        // рисуем часть полилинии, если она внутренняя
                        if (isInternalPath)
                            drawVisiblePolylinePartWithStyleDetection(g, currentPath, 
                                                                     viewBox, scaleFactor);
                    }
            
        }

      

        private static void drawPolylineSimple(JObject g, Feature feature,  BoundingRectangle viewBox,
                                        double scaleFactor)
        {
            foreach (LinePath path in feature.Polyline.Paths)
            {
                if (path.Vertices.Count < 2)
                    continue;

                double[][] points = new double[path.Vertices.Count][];

                for (int j = 0; j < path.Vertices.Count; j++)
                {
                    points[j] = new double[2];
                    points[j][0] =  ((path.Vertices[j].X - viewBox.MinX)*scaleFactor);
                    points[j][1] =  ((viewBox.MaxY - path.Vertices[j].Y)*scaleFactor);
                }

               
                
                    

                    // основная полилиния
                    g.Add("points",JToken.FromObject(points));
                   
                
            }
        }

        #endregion

        /// <summary>
        /// Рисование точки.
        /// </summary>        
        public static void DrawPoint(Feature feature, JObject g,
                                         BoundingRectangle viewBox, double scaleFactor)
        {

            IEnumerable<ICoordinate> targetPoints = null;
            if (feature.FeatureType == FeatureType.Point)
                targetPoints = new ICoordinate[] {feature.Point.Coordinate};
            else
                targetPoints = feature.MultiPoint.Points;

            double[][] _points = new double[targetPoints.Count()][];
            int i = 0;
            foreach (ICoordinate targetPoint in targetPoints)
            {
                _points[i] = new double[2];
                _points[i][0] = (targetPoint.X - viewBox.MinX)*scaleFactor;
                _points[i][1] = (viewBox.MaxY - targetPoint.Y)*scaleFactor;

                i++;

            }
            
            g.Add("points",JToken.FromObject(_points));


        }

        /// <summary>
        /// Рисование линии.
        /// </summary>
        public static void DrawPolyline(Feature feature, JObject g, 
                                            BoundingRectangle viewBox, double scaleFactor)
        {


            double pixelsize = 1/scaleFactor;

           
                if (feature.BoundingRectangle.Width < pixelsize && feature.BoundingRectangle.Height < pixelsize)
                    return;

                Polyline p1 = (Polyline) feature.Polyline.Clone();
                p1.Weed(pixelsize);
                Feature tempFeature = new Feature(FeatureType.Polyline);
                tempFeature.Title = feature.Title;
                tempFeature.Selected = feature.Selected;
                tempFeature.Polyline = p1;
                feature = tempFeature;

                if (feature.Polyline.Paths.Count == 0)
                    return;
            

           
                if (Math.Min(viewBox.Width/(feature.BoundingRectangle.Width),
                             viewBox.Height/(feature.BoundingRectangle.Height)) < 2)
                    drawPolylineWithIntersectCalculation(g, feature,  viewBox, scaleFactor);
                else
                    drawPolylineSimple(g, feature,  viewBox, scaleFactor);

                
           
        }

        /// <summary>
        /// Рисование полигона.
        /// </summary>
        public static void DrawPolygon(Feature feature, JObject g, BoundingRectangle viewBox, double scaleFactor)
        {
            if (feature.Polygon.Contours.Count == 0)
                return;

            //if (feature.Layer != null && feature.Layer.Title == "adp.TAB")
            //{
            //    drawPrismPolygon(feature, g, viewBox, scaleFactor);
            //    // надпись
            //    if (!string.IsNullOrEmpty(feature.Title) && titleVisible)
            //        addTitleBufferElement(g, feature, titleStyle, viewBox, scaleFactor);
            //    return;
            //}


            double pixelsize = 1/scaleFactor;

            if (feature.BoundingRectangle.Width < pixelsize && feature.BoundingRectangle.Height < pixelsize)
                return;

            Polygon tempPolygon = (Polygon)feature.Polygon.Clone();
            tempPolygon.Weed(pixelsize);
            Feature tempFeature = new Feature(FeatureType.Polygon);
            tempFeature.Title = feature.Title;
            tempFeature.Selected = feature.Selected;
            tempFeature.Polygon = tempPolygon;
            feature = tempFeature;

            if (feature.Polygon.Contours.Count == 0)
                return;
        






        IList< double[][]> con = new List<double[][]>();

                foreach (Contour c in feature.Polygon.Contours)
                {
                    // нет смысла пытаться рисовать вырожденные контуры
                    if (c.Vertices.Count <= 2)
                        continue;

                    double[][] points = new double[c.Vertices.Count][];

                    for (int j = 0; j < c.Vertices.Count; j++)
                    {
                        points[j] = new double[2];
                        points[j][0] =  ((c.Vertices[j].X - viewBox.MinX)*scaleFactor);
                        points[j][1] =  ((viewBox.MaxY - c.Vertices[j].Y)*scaleFactor);
                    }
                    
                    if (points.Length > 2)
                    {
                        
                       con.Add(points);
                        
                    }
                }
                g.Add("points", JToken.FromObject(con.ToArray()));

                

        }

       
    }
}