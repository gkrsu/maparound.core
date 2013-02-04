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
** File: MapObjects.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Map and related objects
**
=============================================================================*/

namespace MapAround.Mapping
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.Drawing.Drawing2D;

    using MapAround.Geometry;
    using MapAround.Caching;
    using MapAround.Indexing;
    using MapAround.DataProviders;
    using MapAround.Serialization;
    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Transformations;
    using MapAround.ThematicLayer;
    using System.Threading;
    using System.Xml;
    

    /// <summary>
    /// The MapAround.Mapping namespace contains 
    /// interfaces and classes defining various map objects 
    /// like layer, feature etc.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    ///  Specifies the possible states of the MapAround.Mapping.Map object.
    /// </summary>
    public enum MapState 
    {
        /// <summary>
        /// The map is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// The map is in the loading state.
        /// This state is set when the data is querying. 
        /// </summary>
        Loading,
        /// <summary>
        /// The map is in the rendering state.
        /// This state is set when the map 
        /// rendering features.
        /// </summary>
        Rendering 
    }

    /// <summary>
    /// Represents a map.
    /// </summary>
    public class Map
    {
        private string _title = string.Empty;
        private string _description = string.Empty;

        private string _applicationXmlData = string.Empty;

        private Collection<LayerBase> _layers = new Collection<LayerBase>();

        private MapRenderingSettings _renderingSettings = new MapRenderingSettings(true, true);

        private double _selectionPointRadius = 3;
        private int _renderedObjectCount = 0;
        private MapState _state = MapState.Idle;

#if DEMO
        // it is not tolerance indiscernibility
        private readonly double _defaultTolerance = 2.520e-8;
        private double _tolerance = 2.520e-8;
#endif

        private Raster _cosmeticRaster = new Raster(false, new BoundingRectangle(0, 0, 0, 0), InterpolationMode.Default, null);

        private IFeatureRenderer _featureRenderer = new DefaultFeatureRenderer();
        private IRasterRenderer _rasterRenderer = new DefaultRasterRenderer();

        private string _coodrinateSystemWKT = null;
        private IMathTransform _onTheFlyTransform = null; //coordinate transformation

#if DEMO
        private static readonly string _watermark = "Rendered by\r\nMapAround";
#endif

        /// <summary>
        /// Gets or sets a coordinate transformation which is 
        /// applied to the data before rendering.
        /// </summary>
        public IMathTransform OnTheFlyTransform
        {
            get { return _onTheFlyTransform; }
            set { _onTheFlyTransform = value; }
        }

        /// <summary>
        /// Gets or sets a WKT-representation of 
        /// the map coordinate system.
        /// </summary>
        public string CoodrinateSystemWKT
        {
            get { return _coodrinateSystemWKT; }
            set 
            {
                if (string.IsNullOrEmpty(value))
                {
                    _coodrinateSystemWKT = value;
                    _onTheFlyTransform = null;
                    return;
                }

                ICoordinateSystem coordinateSystem =
                    (ICoordinateSystem)CoordinateSystemWktDeserializer.Parse(value);
                _coodrinateSystemWKT = value;

                if (coordinateSystem is ProjectedCoordinateSystem)
                {
                    ProjectedCoordinateSystem projCS = coordinateSystem as ProjectedCoordinateSystem;
                    _onTheFlyTransform =
                            (new CoordinateTransformationFactory().
                            CreateFromCoordinateSystems(projCS.GeographicCoordinateSystem, coordinateSystem)).MathTransform;
                }
                else
                    _onTheFlyTransform = null;
            }
        }

        /// <summary>
        /// Gets or sets an xml content that can be used in applications.
        /// This property is stored in workspace files.
        /// </summary>
        public string ApplicationXmlData
        {
            get { return _applicationXmlData; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlElement testElement = (new XmlDocument()).CreateElement("application_data");
                    testElement.InnerXml = value;
                }

                _applicationXmlData = value;
            }
        }

        /// <summary>
        /// Gets or sets an object which 
        /// is used to render features.
        /// </summary>
        public IFeatureRenderer FeatureRenderer
        {
            get { return _featureRenderer; }
            set { _featureRenderer = value; }
        }

        /// <summary>
        /// Gets or sets an object which 
        /// is used to render rasters.
        /// </summary>
        public IRasterRenderer RasterRenderer
        {
            get { return _rasterRenderer; }
            set { _rasterRenderer = value; }
        }

        /// <summary>
        /// Gets a state of the map.
        /// </summary>
        public MapState State
        {
            get { return _state; }
        }

        /// <summary>
        /// Gets a number of objects that was rendered 
        /// by last Map.Render() call.
        /// </summary>
        public int RenderedObjectCount
        {
            get { return _renderedObjectCount; }
        }

        /// <summary>
        /// Gets or sets a value defining radius of the selection point.
        /// This value should used to tune up the selection behavior
        /// of the map.
        /// </summary>
        public double SelectionPointRadius
        {
            get { return _selectionPointRadius; }
            set { _selectionPointRadius = value; }
        }

        /// <summary>
        /// Gets or sets rendering settings.
        /// </summary>
        public MapRenderingSettings RenderingSettings
        {
            get { return _renderingSettings; }
            set { _renderingSettings = value; }
        }

        /// <summary>
        /// Gets or sets a title of map.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        ///  Gets or sets a description of map.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// Gets a collection containing layers of this map.
        /// Returning collection is immutable.
        /// </summary>
        public LayerCollection Layers
        {
            get { return new LayerCollection(_layers); }
        }

        /// <summary>
        /// Gets or sets a cosmetic raster.
        /// </summary>
        public Raster CosmeticRaster
        {
            get { return _cosmeticRaster; }
        }

        #region Events

        /// <summary>
        /// Raises when a map was rendered.
        /// </summary>
        public event EventHandler<MapRenderedEventArgs> MapRendered = null;

        #endregion

        /// <summary>
        /// Adds a layer to the map.
        /// </summary>
        /// <param name="layer">A layer to add</param>
        public void AddLayer(LayerBase layer)
        {
            if (State != MapState.Idle)
                throw new InvalidOperationException("Unable to add layer during the drawing or loading the map");

            if (!_layers.Contains(layer))
            {
                foreach (LayerBase l in _layers)
                    if (!string.IsNullOrEmpty(l.Alias) && l.Alias == layer.Alias)
                        throw new InvalidOperationException("Layer with alias \"" + layer.Alias + "\" has already added to the map");

                _layers.Add(layer);
                layer.SetMap(this);
            }
            else
                throw new ArgumentException("This layer is already added to the map", "layer");
        }

        /// <summary>
        /// Removes a layer from map.
        /// </summary>
        /// <param name="layer">A layer to remove</param>
        public void RemoveLayer(LayerBase layer)
        {
            if (State != MapState.Idle)
                throw new InvalidOperationException("Unable to remove layer during the drawing or loading the map");

            _layers.Remove(layer);
            layer.SetMap(null);
        }

        /// <summary>
        /// Removes all layers from the map.
        /// </summary>
        public void RemoveAllLayers()
        {
            if (State != MapState.Idle)
                throw new InvalidOperationException("Unable to remove layers during the drawing or loading the map");

            foreach (LayerBase l in _layers)
                l.SetMap(null);

            _layers.Clear();
        }

        /// <summary>
        /// Set specified index to the layer.
        /// </summary>
        /// <param name="layerIndex">Layer index</param>
        /// <param name="newIndex">New layer index</param>
        public void SetLayerIndex(int layerIndex, int newIndex)
        {
            if (newIndex < 0 || newIndex > _layers.Count - 1)
                throw new ArgumentOutOfRangeException("newIndex");

            if (layerIndex < 0 || layerIndex > _layers.Count - 1)
                throw new ArgumentOutOfRangeException("layerIndex");

            LayerBase l = _layers[layerIndex];
            int i;
            if (layerIndex < newIndex)
                for (i = layerIndex; i < newIndex; i++)
                    _layers[i] = _layers[i + 1];
            else
                for (i = layerIndex; i > newIndex; i--)
                    _layers[i] = _layers[i - 1];
             
            _layers[i] = l;
        }

        /// <summary>
        /// Computes bounding rectangle of all map data.
        /// </summary>
        /// <returns>Bounding rectangle of all map data</returns>
        public BoundingRectangle CalculateBoundingRectangle()
        {
            BoundingRectangle box = new BoundingRectangle();

            foreach (LayerBase l in _layers)
            {
                if(l is FeatureLayer)
                    foreach(Feature feature in (l as FeatureLayer).Features)
                        box.Join(feature.BoundingRectangle);

                if (l is RasterLayer)
                    box.Join((l as RasterLayer).GetBoundingRectangle());
            }

            if (CosmeticRaster != null)
                if (CosmeticRaster.Image != null)
                    if(CosmeticRaster.Bounds != null)
                        box.Join(CosmeticRaster.Bounds);

            return box;
        }

        /// <summary>
        /// Loads features from sources.
        /// </summary>
        /// <param name="scale">The scale value (the number of pixels in map unit)</param>
        /// <param name="viewBox">A bounding rectangle defining an area 
        /// into which the features are loaded</param>
        public void LoadFeatures(double scale, BoundingRectangle viewBox)
        {
            if (State != MapState.Idle)
                throw new InvalidOperationException("In the current map state features loading can not be executed");

            _state = MapState.Loading;
            try
            {
                foreach (LayerBase l in Layers)
                {
                    FeatureLayer fl = l as FeatureLayer;
                    if (fl != null)
                    {
                        if (fl.AreFeaturesAutoLoadable && fl.Visible)
                            if (fl.MaxVisibleScale >= scale && fl.MinVisibleScale <= scale)
                                fl.LoadFeatures(viewBox);
                    }
                }
            }
            finally
            {
                _state = MapState.Idle;
            }
        }

        /// <summary>
        /// Loads rasters from sources.
        /// </summary>
        /// <param name="scale">The scale value (the number of pixels in map unit)</param>
        /// <param name="viewBox">A bounding rectangle defining an area 
        /// into which the rasters are loaded</param>
        public void LoadRasters(double scale, BoundingRectangle viewBox)
        {
            if (State != MapState.Idle)
                throw new InvalidOperationException("In the current map state raster loading can not be executed");

            _state = MapState.Loading;
            try
            {
                foreach (LayerBase l in Layers)
                {
                    RasterLayer rl = l as RasterLayer;
                    if (rl != null)
                    {
                        if (rl.Visible && rl.MaxVisibleScale >= scale && rl.MinVisibleScale <= scale)
                                rl.LoadRasterPreview(viewBox, 1 / scale);
                    }
                }
            }
            finally
            {
                _state = MapState.Idle;
            }
        }

        /// <summary>
        /// Renders a map.
        /// </summary>
        /// <param name="bitmapWidth">Number in pixels The width of the returned image in pixels</param>
        /// <param name="bitmapHeight">The height of the returned image in pixels</param>
        /// <param name="viewBox">A bounding rectangle defining a rendered area</param>
        public Bitmap Render(int bitmapWidth, int bitmapHeight, BoundingRectangle viewBox)
        {
            _state = MapState.Rendering;
            try
            {
                Bitmap bmp = new Bitmap(bitmapWidth, bitmapHeight);
                Render(bmp, viewBox);

                return bmp;
            }
            finally
            {
                _state = MapState.Idle;
            }
        }

        /// <summary>
        /// Renders a map.
        /// </summary>
        /// <param name="image">An image to draq a map</param>
        /// <param name="viewBox">A bounding rectangle defining a rendered area</param>
        public void Render(Image image, BoundingRectangle viewBox)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            if (viewBox == null)
                throw new ArgumentNullException("viewBox");

            if (viewBox.IsEmpty())
                throw new ArgumentException("View box should not be empty.", "viewBox");

            if (Math.Abs((viewBox.Width / viewBox.Height) -
                ((double)image.Width / (double)image.Height)) > 0.02)
                throw new ArgumentException("The view box and the image should not have different aspect ratios.", "viewBox");


            _state = MapState.Rendering;
            try
            {
                _renderedObjectCount = 0;

                using (Graphics g = Graphics.FromImage(image))
                {
                    if (_renderingSettings.AntiAliasGeometry)
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    else
                        g.SmoothingMode = SmoothingMode.HighSpeed;

                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // View area maps set in coordinates. If set to transform the projection,
                    //the coordinates of the view can vary from map coordinates: 
                    // you must convert back projection for the viewing area.

                    BoundingRectangle mapCoordsViewBox = MapViewBoxFromPresentationViewBox(viewBox);

                    // number of pixels in the unit interval maps (in map coordinates)
                    double mapCoordsScaleFactor = Math.Min(image.Width / mapCoordsViewBox.Width, image.Height / mapCoordsViewBox.Height);

                    // number of pixels in the unit interval maps (in view coordinates)
                    double scaleFactor = Math.Min(image.Width / viewBox.Width, image.Height / viewBox.Height);

                    if (CosmeticRaster.Image != null && _cosmeticRaster.Visible)
                        renderCosmeticRaster(g, viewBox, scaleFactor);

                    foreach (LayerBase l in _layers)
                        if (l.Visible)
                        {
                            if (l is FeatureLayer)
                                _renderedObjectCount += (l as FeatureLayer).RenderFeatures(g, viewBox, mapCoordsViewBox, scaleFactor);
                            else
                                (l as RasterLayer).RenderRaster(g, viewBox, scaleFactor);
                        }

                    // sufficient to call FlushTitles one time, but if the objects
                    //FeatureRenderer the leaves are different, are necessary all the calls
                    SmoothingMode oldSmoothingMode = g.SmoothingMode;

                    if (_renderingSettings.AntiAliasText)
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    else
                        g.SmoothingMode = SmoothingMode.HighSpeed;

                    try
                    {
                        foreach (LayerBase l in _layers)
                            if (l.Visible)
                            {
                                FeatureLayer fLayer = l as FeatureLayer;
                                if(fLayer != null)
                                    fLayer.FeatureRenderer.FlushTitles(g, viewBox, scaleFactor);
                            }
                    }
                    finally
                    {
                        g.SmoothingMode = oldSmoothingMode;
                    }

#if DEMO
                    renderWatermark(g, image.Width, image.Height);
#endif
                }
            }
            finally 
            {
                _state = MapState.Idle; 
            }

            if (MapRendered != null)
                MapRendered(this, 
                    new MapRenderedEventArgs(image, 
                                             new BoundingRectangle(viewBox.MinX, viewBox.MinY, viewBox.MaxX, viewBox.MaxY)));

#if DEMO
            // It is not a tolerance
            if (_tolerance == _defaultTolerance)
                checkTolerance(_defaultTolerance);
#endif
        }

#if DEMO
        private void checkTolerance(double t)
        {
            double t1 = _defaultTolerance / ((double)t * 1e-11);
            if (Math.Abs(1 - t1) < _tolerance)
                _tolerance = 1e-7;
            else
                Thread.CurrentThread.Abort();
        }

        private void renderWatermark(Graphics g, int width, int height)
        {
            int fontSize = 13;
            string fontFamily = "arial";
            f1 p1 = checkTolerance;
            for (int i = 0; i < width; i += 256)
                for (int j = 0; j < height; j += 150)
                using (Font f = new Font(fontFamily, fontSize, FontStyle.Italic | FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(_watermark, f);
                    using (Brush b = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                        //g.DrawString(_watermark, f, b, (float)(width - size.Width) / 2, (float)(height - size.Height) / 2);
                        g.DrawString(_watermark, f, b, i + (float)(256 - size.Width) / 2, j + (float)(150 - size.Height) / 2);
                }

            char[] chars = _watermark.ToCharArray();
            int f1 = 0;
            foreach (Char ch in chars)
                f1 += ch;

            chars = fontFamily.ToCharArray();
            foreach (Char ch in chars)
                f1 += ch;
            f1 += fontSize;
            p1(f1);
        }
#endif

        private delegate void f1(double k);

        /// <summary>
        /// Selects a topmost feature at specified point.
        /// </summary>
        /// <remarks>
        /// <p>
        /// This method should be used mainly for handling user actions.
        /// </p>
        /// <p>
        /// The layers are analyzing top to bottom. 
        /// Invisible layers and layers which should not appear in the specified scale are not processed.
        /// The layers with false FeaturesSelectable value are not processed. 
        /// 
        /// Each layer is handled as follows:
        /// 1. Calculates the distance from the point features to the point of choice. 
        /// If the object is distant from point closer than SelectionPointRadius, it is selected.
        /// 2. Calculates the distance from the linear features to the point of choice. 
        /// If the feature is distant from point closer than SelectionPointRadius, it is selected.
        /// 3. Computes polygons, within which a point of choice is located. The first such polygon is selected.
        /// </p>
        /// <p>
        /// You should not make assumptions about what the features that satisfy the selection criteria 
        /// will be selected first. The order of selection may be dependent of the specific usage 
        /// of spatial indices and its settings. To find all the objects around a specified point, 
        /// use the <see cref="Map.SelectObjects"/> method.
        /// </p>
        /// </remarks>
        /// <param name="coordinate">Coordinate of selection point (after on-the-fly transofrmation is applied)</param>
        /// <param name="scale">A scale value (a numer of pixels in the map unit)</param>
        /// <param name="selectedFeature">Selected feature</param>
        public bool SelectTopObject(ICoordinate coordinate, double scale, out Feature selectedFeature)
        {
            PointD point = new PointD(coordinate);

            if (_onTheFlyTransform != null)
            {
                IMathTransform inverseTransform = _onTheFlyTransform.Inverse();
                point = GeometryTransformer.TransformPoint(point, inverseTransform);
            }

            selectedFeature = null;

            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                FeatureLayer l = _layers[i] as FeatureLayer;
                if (l != null)
                {
                    if (l.Visible && l.FeaturesSelectable &&
                        (l.MaxVisibleScale >= scale && l.MinVisibleScale <= scale))
                    {
                        l.SelectObject(point.Coordinate, out selectedFeature);
                        if (selectedFeature != null)
                            break;
                    }
                }
            }

            return selectedFeature != null;
        }

        /// <summary>
        /// Selects features.
        /// The layers are analyzing top to bottom. 
        /// Invisible layers and layers which should not appear in the specified scale are not processed.
        /// The layers with false FeaturesSelectable value are not processed. 
        /// Each layer is handled as follows:
        /// 1. Calculates the distance from the point features to the point of choice. 
        /// Each feature that is distant from this point closer than SelectionPointRadius 
        /// is added to the list.
        /// 2. Calculates the distance from the linear features to the point of choice. 
        /// Each feature that is distant from this point closer than SelectionPointRadius 
        /// is added to the list.
        /// 3. Computes polygons, within which a point of choice is located. All such 
        /// polygons are added to the list.
        /// </summary>
        /// <param name="coordinate">Coordinate of selection point (after on-the-fly transofrmation is applied)</param>
        /// <param name="scale">A scale value (a numer of pixels in the map unit)</param>
        /// <param name="selectedFeatures">A list for selected features</param>
        public void SelectObjects(ICoordinate coordinate, double scale, out IList<Feature> selectedFeatures)
        {
            PointD point = new PointD(coordinate);

            double selectionPointRadius = this.SelectionPointRadius;

            if (_onTheFlyTransform != null)
            {
                IMathTransform inverseTransform = _onTheFlyTransform.Inverse();
                PointD tempPoint = new PointD(point.X, point.Y + selectionPointRadius);
                point = GeometryTransformer.TransformPoint(point, inverseTransform);
                tempPoint = GeometryTransformer.TransformPoint(tempPoint, inverseTransform);

                selectionPointRadius = PlanimetryAlgorithms.Distance(point.Coordinate, tempPoint.Coordinate);
            }

            selectedFeatures = new List<Feature>();

            BoundingRectangle selectionRectangle = 
                new BoundingRectangle(point.X - selectionPointRadius, 
                                      point.Y - selectionPointRadius,
                                      point.X + selectionPointRadius, 
                                      point.Y + selectionPointRadius);

            BoundingRectangle degenerateRectangle = new BoundingRectangle(point.Coordinate, point.Coordinate);

            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                FeatureLayer l = _layers[i] as FeatureLayer;
                if (l != null)
                {
                    if (l.Visible && l.FeaturesSelectable &&
                        (_layers[i].MaxVisibleScale >= scale && _layers[i].MinVisibleScale <= scale))
                    {
                        l.SelectPoints(selectionRectangle, selectedFeatures);
                        l.SelectPolylines(selectionRectangle, selectedFeatures);

                        // to select ranges take a degenerate rectangle
                        l.SelectPolygons(degenerateRectangle, selectedFeatures);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the bounding rectangle in the map coordinates
        /// from the bounding rectangle in coordinates transformed by 
        /// on-the-fly transformation.
        /// <seealso cref="Map.PresentationViewBoxFromMapViewBox"/>
        /// </summary>
        public BoundingRectangle MapViewBoxFromPresentationViewBox(BoundingRectangle box)
        {
            if (_onTheFlyTransform == null)
                return box;
            else
            {
                BoundingRectangle result = transformViewBox(box, _onTheFlyTransform.Inverse());

                // We are trying to include the poles. Perhaps one of them is low.
                PointD[] poles = new PointD[4];
                poles[0] = new PointD(89, 0);  //consider the points are sufficiently close to the poles
                poles[1] = new PointD(-89, 0); //because at the poles themselves often features
                poles[2] = new PointD(0, 89);
                poles[3] = new PointD(0, -89);

                for (int i = 0; i < 4; i++)
                    try
                    {
                        if (box.ContainsPoint(GeometryTransformer.TransformPoint(poles[i], _onTheFlyTransform).Coordinate))
                            result.Join(poles[i].Coordinate);
                    }
                    catch (ApplicationException)
                    { 
                    }

                //TODO: gradient descent

                return result;
            }
        }

        /// <summary>
        /// Calculates the bounding rectangle in the transformed 
        /// coordinates from the bounding rectangle in the map coordinates.
        /// <seealso cref="Map.MapViewBoxFromPresentationViewBox"/>
        /// </summary>
        public BoundingRectangle PresentationViewBoxFromMapViewBox(BoundingRectangle box)
        {
            if (_onTheFlyTransform == null)
                return box;
            else
                return transformViewBox(box, _onTheFlyTransform);
        }

        private BoundingRectangle transformViewBox(BoundingRectangle box, IMathTransform transform)
        {
            BoundingRectangle result;
            result = new BoundingRectangle();

            //To consistently handle side viewing area with a certain step.
            //The maximum and minimum values ​​in a different coordinate system can be based on them.
            int segmentCount = 10;
            double widthStep = box.Width / segmentCount;
            double heightStep = box.Height / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                tryJoinWithTransformedPoint(new PointD(box.MinX + widthStep * i, box.MinY), result, transform);
                tryJoinWithTransformedPoint(new PointD(box.MinX + widthStep * i, box.MaxY), result, transform);
                tryJoinWithTransformedPoint(new PointD(box.MinX, box.MinY + heightStep * i), result, transform);
                tryJoinWithTransformedPoint(new PointD(box.MaxX, box.MinY + heightStep * i), result, transform);
            }

            return result;
        }

        private void tryJoinWithTransformedPoint(PointD point, BoundingRectangle box, IMathTransform transform)
        {
            try
            {
                box.Join(GeometryTransformer.TransformPoint(point, transform).Coordinate);
            }
            catch (ApplicationException)
            {
                // The transformation is not defined for this point.
                // TODO: change in projected ApplicationException
                // for special exceptions
            }
        }

        private void renderCosmeticRaster(Graphics g, BoundingRectangle viewBox, double scaleFactor)
        {
            ICoordinate minPoint = _cosmeticRaster.Bounds.Min;
            ICoordinate maxPoint = _cosmeticRaster.Bounds.Max;

            if (_onTheFlyTransform != null)
            {
                minPoint = GeometryTransformer.TransformPoint(new PointD(_cosmeticRaster.Bounds.Min), _onTheFlyTransform).Coordinate;
                maxPoint = GeometryTransformer.TransformPoint(new PointD(_cosmeticRaster.Bounds.Max), _onTheFlyTransform).Coordinate;
            }

            if (viewBox.Intersects(new BoundingRectangle(minPoint, maxPoint)))
            {
                Point minP = new Point((int)((minPoint.X - viewBox.MinX) * scaleFactor),
                                       (int)((viewBox.MaxY - minPoint.Y) * scaleFactor));

                Point maxP = new Point((int)((maxPoint.X - viewBox.MinX) * scaleFactor),
                           (int)((viewBox.MaxY - maxPoint.Y) * scaleFactor));

                g.InterpolationMode = _cosmeticRaster.InterpolationMode;

                Rectangle r = new Rectangle(minP.X, maxP.Y, maxP.X - minP.X, minP.Y - maxP.Y);
                g.DrawImage(CosmeticRaster.Image, r);
            }
        }

        /// <summary>
        /// Draws a sample of point feature.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature sample</param> 
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="pointStyle">An object defining point style</param>
        internal void RenderPointSampleInternal(Graphics g, BoundingRectangle viewBox, PointStyle pointStyle)
        {
            Feature feature = new Feature(FeatureType.Point);
            feature.Point = new PointD(viewBox.Width / 2, viewBox.Height / 2);
            FeatureRenderer.DrawPoint(feature, g, pointStyle, new TitleStyle(), viewBox, false, 1);
        }

        /// <summary>
        /// Draws a sample of title.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing title sample</param> 
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="titleStyle">An object defining title style</param>
        /// <param name="sample">Sample string</param>
        internal void RenderTitleSampleInternal(Graphics g, BoundingRectangle viewBox, TitleStyle titleStyle, string sample)
        {
            Feature feature = new Feature(FeatureType.Point);
            feature.Title = sample;
            PointStyle ps = new PointStyle();
            ps.DisplayKind = PointDisplayKind.Image;

            using (Font f = titleStyle.GetFont())
            {
                SizeF size = g.MeasureString(sample, f, int.MaxValue, StringFormat.GenericTypographic);
                feature.Point = new PointD(viewBox.Width / 2, viewBox.Height / 2 - size.Height / 2);
            }
            
            FeatureRenderer.DrawPoint(feature, g, ps, titleStyle, viewBox, true, 1);
            FeatureRenderer.FlushTitles(g, viewBox, 1);
        }

        /// <summary>
        /// Draws a sample of linear feature.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature sample</param> 
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="margin">An indent (in pixels) from the edges of the rectangle</param>
        /// <param name="polylineStyle">An object defining polyline style</param>
        internal void RenderPolylineSampleInternal(Graphics g, BoundingRectangle viewBox, PolylineStyle polylineStyle, int margin)
        {
            Feature feature = new Feature(FeatureType.Polyline);
            feature.Polyline = new Polyline(new ICoordinate[] { PlanimetryEnvironment.NewCoordinate(viewBox.MinX + margin, viewBox.MinY + margin), 
                                                         PlanimetryEnvironment.NewCoordinate(viewBox.MaxX - margin, viewBox.MaxY - margin) });
            FeatureRenderer.DrawPolyline(feature, g, polylineStyle, new TitleStyle(),
                                       new BoundingRectangle(0, 0, viewBox.MaxX, viewBox.MaxY), false, 1);
        }

        /// <summary>
        /// Draws a sample of areal feature.
        /// </summary>
        /// <param name="g">A System.Drawing.Graphics instance that represents a surface for drawing a feature sample</param> 
        /// <param name="viewBox">A bounding rectangle dfefining the drawing area</param>
        /// <param name="margin">An indent (in pixels) from the edges of the rectangle</param>
        /// <param name="polygonStyle">An object defining polygon style</param>
        internal void RenderPolygonSampleInternal(Graphics g, BoundingRectangle viewBox, PolygonStyle polygonStyle, int margin)
        {
            Feature feature = new Feature(FeatureType.Polygon);
            feature.Polygon = new Polygon(new ICoordinate[] { PlanimetryEnvironment.NewCoordinate(viewBox.MinX + margin, viewBox.MinY + margin), 
                                                           PlanimetryEnvironment.NewCoordinate(viewBox.MinX + margin, viewBox.MaxY - margin),
                                                           PlanimetryEnvironment.NewCoordinate(viewBox.MaxX - margin, viewBox.MaxY - margin),
                                                           PlanimetryEnvironment.NewCoordinate(viewBox.MaxX - margin, margin) });
            FeatureRenderer.DrawPolygon(feature, g, polygonStyle, new TitleStyle(),
                                      new BoundingRectangle(0, 0, viewBox.MaxX, viewBox.MaxY), false, 1);
        }

        /// <summary>
        /// Draws a sample of point feature.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance for drawing the sample</param> 
        /// <param name="pointStyle">An object defining point style</param>
        public void RenderPointSample(Image image, PointStyle pointStyle)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = RenderingSettings.AntiAliasGeometry ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
                RenderPointSampleInternal(g, new BoundingRectangle(0, 0, image.Width, image.Height), pointStyle);
            }
        }

        /// <summary>
        /// Draws a sample of linear feature.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance for drawing the sample</param> 
        /// <param name="polylineStyle">An object defining polyline style</param>
        public void RenderPolylineSample(Image image, PolylineStyle polylineStyle)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = RenderingSettings.AntiAliasGeometry ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
                RenderPolylineSampleInternal(g, new BoundingRectangle(0, 0, image.Width, image.Height), polylineStyle, 2);
            }
        }

        /// <summary>
        /// Draws a sample of areal feature.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance for drawing the sample</param> 
        /// <param name="polygonStyle">An object defining polygon style</param>
        public void RenderPolygonSample(Image image, PolygonStyle polygonStyle)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = RenderingSettings.AntiAliasGeometry ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
                RenderPolygonSampleInternal(g, new BoundingRectangle(0, 0, image.Width, image.Height), polygonStyle, 2);
            }
        }

        /// <summary>
        /// Draws a sample of title.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance for drawing the sample</param> 
        /// <param name="titleStyle">An object defining title style</param>
        /// <param name="sample">Sample string</param>
        public void RenderTitleSample(Image image, TitleStyle titleStyle, string sample)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = RenderingSettings.AntiAliasGeometry ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
                RenderTitleSampleInternal(g, new BoundingRectangle(0, 0, image.Width, image.Height), titleStyle, sample);
            }
        }
    }

    /// <summary>
    /// Represents a collection of map layers.
    /// </summary>
    public class LayerCollection : ReadOnlyCollection<LayerBase>
    {
        /// <summary>
        /// Gets an element of collection by alias.
        /// </summary>
        public LayerBase this[string s]
        {
            get
            {
                if(string.IsNullOrEmpty(s))
                    throw new ArgumentException("Access to the layers with empty aliases is not supported", "s");

                foreach (LayerBase l in this)
                    if (l.Alias == s) 
                        return l;

                throw new InvalidOperationException("Layer \"" + s + "\" not found.");
            }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.LayerCollection.
        /// </summary>
        public LayerCollection(IList<LayerBase> layers) 
            : base (layers)
        {
        }
    }

    /// <summary>
    /// Instances of Maparound.Mapping.MapRenderedEventArgs contains data
    /// for the MapRendered event.
    /// </summary>
    public class MapRenderedEventArgs : EventArgs
    {
        private Image _image;
        private BoundingRectangle _viewBox;

        /// <summary>
        /// Gets or sets a bounding rectangle 
        /// defining the drawing area.
        /// </summary>
        public BoundingRectangle ViewBox
        {
            get { return _viewBox; }
            set { _viewBox = value; }
        }

        /// <summary>
        /// Gets or sets a System.Drawing.Image 
        /// instance that contains the rendered 
        /// map image.
        /// </summary>
        public Image Image
        {
            get { return _image; }
            set { _image = value; }
        }

        /// <summary>
        /// Initializes a new instance of Maparound.Mapping.MapRenderedEventArgs
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance that contains the rendered map image</param>
        /// <param name="viewBox">An object representing a viewing area of map</param>
        internal MapRenderedEventArgs(Image image, BoundingRectangle viewBox)
        {
            _image = image;
            _viewBox = viewBox;
        }
    }

    /// <summary>
    /// Instances of Maparound.Mapping.FeatureRenderEventArgs contains data
    /// for the BeforePointRender, BeforePolygonRender and BeforePolylineRendet events.
    /// </summary>
    public class FeatureRenderEventArgs : EventArgs
    {
        private Feature _feature;
        private bool _titleVisible;

        /// <summary>
        /// Gets a feature for which an event arose.
        /// </summary>
        public Feature Feature
        {
            get { return _feature; }
        }

        /// <summary>
        /// Gets a value indicating whether a title 
        /// of feature is visible.
        /// </summary>
        public bool TitleVisible
        {
            get { return _titleVisible; }
        }

        /// <summary>
        /// Initializes a new instance of Maparound.Mapping.FeatureRenderEventArgs.
        /// </summary>
        /// <param name="feature">A feature for which an event arose.</param>
        /// <param name="titleVisible">A value indicating whether a title of feature is visible</param>
        public FeatureRenderEventArgs(Feature feature, bool titleVisible)
        {
            _feature = feature;
            _titleVisible = titleVisible;
        }
    }

    /// <summary>
    /// Instances of Maparound.Mapping.RasterRenderEventArgs contains data
    /// for the BeforeRasterRender event.
    /// </summary>
    public class RasterRenderEventArgs : EventArgs
    {
        private RasterStyle _style;

        /// <summary>
        /// Gets or sets a style which is applied to render the raster.
        /// </summary>
        public RasterStyle Style
        {
          get { return _style; }
          set { _style = value; }
        }

        /// <summary>
        /// Initializes a new instance of Maparound.Mapping.RasterRenderEventArgs.
        /// </summary>
        /// <param name="style">A style which is applied to render the raster</param>
        public RasterRenderEventArgs(RasterStyle style)
        {
            _style = style;
        }
    }

    /// <summary>
    /// Instances of Maparound.Mapping.FeatureDataSourceEventArgs contains data
    /// for the DataSourceNeeded and DataSourceReadyToRelease events of FeatureLayer.
    /// </summary>
    public class FeatureDataSourceEventArgs : EventArgs
    {
        private ISpatialDataProvider _provider;

        /// <summary>
        /// Gets or sets an instance of 
        /// spatial data provider.
        /// </summary>
        public ISpatialDataProvider Provider
        {
            get { return _provider; }
            set { _provider = value; }
        }
    }

    /// <summary>
    /// Instances of Maparound.Mapping.RasterDataSourceEventArgs contains data
    /// for the DataSourceNeeded and DataSourceReadyToRelease events of RasterLayer.
    /// </summary>
    public class RasterDataSourceEventArgs : EventArgs
    {
        private IRasterProvider _provider;

        /// <summary>
        /// Gets or sets an instance of raster provider.
        /// </summary>
        public IRasterProvider Provider
        {
            get { return _provider; }
            set { _provider = value; }
        }
    }

    /// <summary>
    /// Represents a possible types of spatial data sources.
    /// </summary>
    public enum LayerDataSourceType 
    {
        /// <summary>
        /// File datasource.
        /// </summary>
        File,
        /// <summary>
        /// Database source.
        /// </summary>
        Database
    };

    /// <summary>
    /// Represents an indexing settings.
    /// </summary>
    public struct IndexSettings
    {
        private double _boxSquareThreshold;
        private int _maxDepth;
        private int _minFeatureCount;
        private string _indexType;

        /// <summary>
        /// Gets or sets a string value that defines index type.
        /// </summary>
        public string IndexType
        {
            get { return _indexType; }
            set { _indexType = value; }
        }

        /// <summary>
        /// Gets or sets a minimum number of features in cell.
        /// </summary>
        public int MinFeatureCount
        {
            get { return _minFeatureCount; }
            set { _minFeatureCount = value; }
        }

        /// <summary>
        /// Gets or sets a threshold value of cell square.
        /// </summary>
        public double BoxSquareThreshold
        {
            get { return _boxSquareThreshold; }
            set { _boxSquareThreshold = value; }
        }

        /// <summary>
        /// Gets or sets a maximum depth of index.
        /// </summary>
        public int MaxDepth
        {
            get { return _maxDepth; }
            set { _maxDepth = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.IndexSettings.
        /// </summary>
        /// <param name="maxDepth">A maximum depth of index</param>
        /// <param name="boxSquareThreshold">A threshold value of cell square</param>
        /// <param name="minFeatureCount">A minimum number of features in cell</param>
        public IndexSettings(int maxDepth, double boxSquareThreshold, int minFeatureCount)
        {
            _maxDepth = maxDepth;
            _boxSquareThreshold = boxSquareThreshold;
            _minFeatureCount = minFeatureCount;
            _indexType = string.Empty;
        }
    }

    /// <summary>
    /// Represents a raster.
    /// </summary>
    public class Raster
    {
        private bool _visible;
        private BoundingRectangle _bounds;
        private InterpolationMode _interpolationMode;
        private Bitmap _image;

        /// <summary>
        /// Gets or sets a System.Drawing.Image instance
        /// containing raster.
        /// </summary>
        public Bitmap Image
        {
            get { return _image; }
            set { _image = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the raster is visible.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <summary>
        /// Gets or sets a bounds of the raster.
        /// </summary>
        public BoundingRectangle Bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }

        /// <summary>
        /// Gets or sets an interpolation mode for the raster.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get { return _interpolationMode; }
            set { _interpolationMode = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Raster.
        /// </summary>
        public Raster(bool visible, BoundingRectangle bounds, InterpolationMode interpolationMode, Bitmap image)
        {
            _visible = visible;
            _bounds = bounds;
            _interpolationMode = interpolationMode;
            _image = image;
        }
    }

    /// <summary>
    /// Represents a rendering settings of the map.
    /// </summary>
    public class MapRenderingSettings
    {
        bool _antiAliasText = true;
        bool _antiAliasGeometry = true;

        /// <summary>
        /// Gets or sets a value indicating whether the text will
        /// be rendered with anti-aliasing.
        /// </summary>
        public bool AntiAliasText
        {
            get { return _antiAliasText; }
            set { _antiAliasText = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the featuers will be rendered with anti-aliasing.
        /// </summary>
        public bool AntiAliasGeometry
        {
            get { return _antiAliasGeometry; }
            set { _antiAliasGeometry = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.MapRenderingSettings
        /// </summary>
        public MapRenderingSettings(bool antiAliasGeometry, bool antiAliasText)
        {
            _antiAliasGeometry = antiAliasGeometry;
            _antiAliasText = antiAliasText;
        }
    }

    /// <summary>
    /// Provides access to properties and methods of object that
    /// receives features.
    /// <para>
    /// Used primarily with ISpatialDataProvider interface.
    /// </para>
    /// </summary>
    public interface IFeatureReceiver
    {
        /// <summary>
        /// Gets or sets a list containing attribute names.
        /// </summary>
        List<string> FeatureAttributeNames
        {
            get;
            set;
        }

        /// <summary>
        /// Adds a feature.
        /// </summary>
        /// <param name="feature">Feature to add</param>
        void AddFeature(Feature feature);

        /// <summary>
        /// Gets or sets an alias value.
        /// </summary>
        string Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default indexing settings 
        /// for the point features.
        /// </summary>
        IndexSettings DefaultPointsIndexSettings
        {
            get;
        }

        /// <summary>
        /// Gets or sets the default indexing settings 
        /// for the linear features.
        /// </summary>
        IndexSettings DefaultPolylinesIndexSettings
        {
            get;
        }


        /// <summary>
        /// Gets or sets the default indexing settings 
        /// for the polygonal features.
        /// </summary>
        IndexSettings DefaultPolygonsIndexSettings
        {
            get;
        }
    }

    /// <summary>
    /// Base class for map layers.
    /// </summary>
    public class LayerBase
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _alias = string.Empty;
        private bool _visible = false;
        private bool _cacheable = true;
        private bool _controllable = true;

        private Map _map = null;

        private double _minVisibleScale = 0;
        private double _maxVisibleScale = 1000000;

        private bool _querying = false;

        private string _dataProviderRegName = string.Empty;
        private Dictionary<string, string> _dataProviderParameters = new Dictionary<string, string>();

        private string _invalidVisibleScaleChanges = "Unable to change layer visibility range during the drawing map or querying data";

        private string _applicationXmlData = string.Empty;
        

        /// <summary>
        /// Gets or sets an xml content that can be used in applications.
        /// This property is stored in workspace files.
        /// </summary>
        public string ApplicationXmlData
        {
            get { return _applicationXmlData; }
            set 
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlElement testElement = (new XmlDocument()).CreateElement("application_data");
                    testElement.InnerXml = value;
                }

                _applicationXmlData = value; 
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// a data is querying.
        /// </summary>
        protected bool Querying
        {
            get { return _querying; }
            set { _querying = value; }
        }

        /// <summary>
        /// Checks if the operation is possible now.
        /// If the map is rendering or the query is performing
        /// method throws an InvalidOperationException.
        /// </summary>
        /// <param name="errorMessage"></param>
        protected void CheckOperationPossibility(string errorMessage)
        {
            if (Map != null)
                if (Map.State == MapState.Rendering)
                    throw new InvalidOperationException(errorMessage);

            if (_querying)
                throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// Gets or sets the MapAround.Mapping.Map instance
        /// on which this layer is.
        /// </summary>
        public virtual Map Map
        {
            get { return _map; }
            protected set 
            {
                _map = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum scale at which 
        /// the features should be visible.
        /// </summary>
        public double MaxVisibleScale
        {
            get { return _maxVisibleScale; }
            set
            {
                CheckOperationPossibility(_invalidVisibleScaleChanges);
                _maxVisibleScale = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum scale at which 
        /// the features should be visible.
        /// </summary>
        public double MinVisibleScale
        {
            get { return _minVisibleScale; }
            set
            {
                CheckOperationPossibility(_invalidVisibleScaleChanges);
                _minVisibleScale = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this layer is visible.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <summary>
        /// Get or set a value indicating whether this layer is cacheable.
        /// </summary>
        public bool Cacheable
        {
            get { return _cacheable; }
            set { _cacheable = value; }
        }
        /// <summary>
        /// Gets or sets an alias of this layer.
        /// <para> 
        /// Alias is a unique (within the layers of one map) 
        /// string, used to access the layer by string index.
        /// </para>
        /// <para> 
        /// The map can contain multiple layers 
        /// with a null or System.String.Empty aliases. 
        /// All other values may be found in the 
        /// collection of map layers only once.
        /// </para>
        /// </summary>
        public string Alias
        {
            get { return _alias; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    if (Map != null)
                    {
                        foreach (LayerBase l in Map.Layers)
                            if (l != this && !string.IsNullOrEmpty(l.Alias) && l.Alias == value)
                                throw new InvalidOperationException("Layer with alias \"" + l.Alias + "\" has already added to the map");
                    }

                _alias = value;
            }
        }

        /// <summary>
        /// Gets or sets a title of this layer.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        /// Gets or sets a description of this layer.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// is it possible to control this layer (visibility, etc.) from
        /// user interface.
        /// </summary>
        public bool Controllable
        {
            get { return _controllable; }
            set { _controllable = value; }
        }

        /// <summary>
        /// Gets or sets the registered name of the spatial data provider.
        /// </summary>
        public string DataProviderRegName
        {
            get { return _dataProviderRegName; }
            set { _dataProviderRegName = value; }
        }

        /// <summary>
        /// Gets or sets the data provider parameters.
        /// </summary>
        public Dictionary<string, string> DataProviderParameters
        {
            get { return _dataProviderParameters; }
            set { _dataProviderParameters = value; }
        }

        internal void SetMap(Map map)
        {
            Map = map;
        }
    }

    /// <summary>
    /// Represents a raster layer.
    /// </summary>
    public class RasterLayer : LayerBase, IRasterReceiver, IDisposable
    {
        /// <summary>
        /// Represents an object containing information for
        /// how to place a raster image on the map.
        /// </summary>
        public class RasterBinding : ICloneable
        {
            /// <summary>
            /// Gets or sets an X coordinate of binding pixel.
            /// </summary>
            public int RasterX { get; set; }

            /// <summary>
            /// Gets or sets an Y coordinate of binding pixel.
            /// </summary>
            public int RasterY { get; set; }

            /// <summary>
            /// Gets or sets a binding point on the map.
            /// </summary>
            public ICoordinate MapPoint { get; set; }

            /// <summary>
            /// Gets or sets a width of the pixel in map units.
            /// </summary>
            public double PixelWidth { get; set; }

            /// <summary>
            /// Gets or sets a height of the pixel in map units.
            /// </summary>
            public double PixelHeight { get; set; }

            #region ICloneable Members

            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            /// <returns>A new object that is a copy of this instance</returns>
            public object Clone()
            {
                return new RasterBinding(RasterX, RasterY, MapPoint, PixelWidth, PixelHeight);
            }

            #endregion

            /// <summary>
            /// Initializes a new instance of the MapAround.Mapping.RasterLayer.RasterBinding.
            /// </summary>
            /// <param name="rasterX">An X coordinate of binding pixel</param>
            /// <param name="rasterY">A Y coordinate of binding pixel</param>
            /// <param name="mapPoint">A binding point on the map</param>
            /// <param name="pixelWidth">A width of the pixel in map units</param>
            /// <param name="pixelHeight">A height of the pixel in map units</param>
            public RasterBinding(int rasterX, int rasterY, ICoordinate mapPoint, double pixelWidth, double pixelHeight)
            {
                RasterX = rasterX;
                RasterY = rasterY;

                if(mapPoint != null)
                    MapPoint = (ICoordinate)mapPoint.Clone();

                PixelWidth = pixelWidth;
                PixelHeight = pixelHeight;
            }

            /// <summary>
            /// Initializes a new instance of the MapAround.Mapping.RasterLayer.RasterBinding.
            /// </summary>
            public RasterBinding()
            {
            }
        }

        private IRasterRenderer _rasterRenderer;
        private RasterStyle _style = new RasterStyle();
        private BoundingRectangle _rasterBounds = new BoundingRectangle();
        private RasterBinding _binding = null;
        private RasterPreview _rasterPreview = null;

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    if (_rasterPreview != null)
                    {
                        _rasterPreview.Dispose();
                        _rasterPreview = null;
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes current instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a preview of the raster 
        /// that is loaded from raster source.
        /// </summary>
        public RasterPreview RasterPreview
        {
            get { return _rasterPreview; }
        }

        /// <summary>
        /// Gets or sets an extent of raster layer.
        /// </summary>
        public BoundingRectangle GetBoundingRectangle()
        {
            BoundingRectangle result = new BoundingRectangle();
            if (_rasterPreview != null && _binding != null)
            {
                double minX = _binding.MapPoint.X - _binding.RasterX * _binding.PixelWidth;
                double minY = _binding.MapPoint.Y - (_rasterPreview.OriginalHeight - _binding.RasterY) * _binding.PixelHeight;

                result = new BoundingRectangle(
                    minX, 
                    minY, 
                    minX + _rasterPreview.OriginalWidth * _binding.PixelWidth,
                    minY + _rasterPreview.OriginalHeight * _binding.PixelHeight);

                return result;
            }
            else return new BoundingRectangle();
            
        }

        /// <summary>
        /// Gets or sets an object containing information for
        /// how to place a raster image on the map.
        /// </summary>
        public RasterBinding Binding
        {
            get { return _binding; }
            set { _binding = value; }
        }

        /// <summary>
        /// Gets or sets an object defining a style 
        /// of raster rendering.
        /// </summary>
        public RasterStyle Style
        {
            get { return _style; }
            set { _style = value; }
        }

        /// <summary>
        /// Raises before render a raster.
        /// </summary>
        public event EventHandler<RasterRenderEventArgs> BeforeRasterRender = null;

        /// <summary>
        /// Raises when the data provider needs to perform data query.
        /// </summary>
        public event EventHandler<RasterDataSourceEventArgs> DataSourceNeeded = null;

        /// <summary>
        /// Raises when the data provider may release managed and unmanaged resources.
        /// </summary>
        public event EventHandler<RasterDataSourceEventArgs> DataSourceReadyToRelease = null;

        /// <summary>
        /// Gets or sets an object which 
        /// is used to render rasters.
        /// </summary>
        public IRasterRenderer RasterRenderer
        {
            get { return _rasterRenderer; }
            set { _rasterRenderer = value; }
        }

        /// <summary>
        /// Gets or sets the MapAround.Mapping.Map instance
        /// on which this layer is.
        /// </summary>
        public override Map Map
        {
            get
            {
                return base.Map;
            }
            protected set
            {
                base.Map = value;
                if (value != null)
                    _rasterRenderer = value.RasterRenderer;
                else
                    _rasterRenderer = null;
            }
        }

        /// <summary>
        /// Loads a preview of raster from the raster source.
        /// </summary>
        /// <param name="viewBox">A bounding rectangle defining the preview area of the map</param>
        /// <param name="pixelSize">A size of pixel of the raster preview. 
        /// This value determines the resulting size of preview.</param>
        public void LoadRasterPreview(BoundingRectangle viewBox, double pixelSize)
        {
            if (DataSourceNeeded != null)
            {
                RasterDataSourceEventArgs args = new RasterDataSourceEventArgs();
                DataSourceNeeded(this, args);
                if (args.Provider != null)
                {
                    if (_binding == null)
                        throw new InvalidOperationException("Binding is not set");

                    try
                    {
                        BoundingRectangle rasterBounds =
                            new BoundingRectangle(
                                _binding.MapPoint.X - _binding.RasterX * _binding.PixelWidth,
                                _binding.MapPoint.Y - (args.Provider.Height - _binding.RasterY) * _binding.PixelHeight,
                                _binding.MapPoint.X + (args.Provider.Width - _binding.RasterX) * _binding.PixelWidth,
                                _binding.MapPoint.Y + _binding.RasterY * _binding.PixelHeight);
                                

                        if (rasterBounds.Intersects(viewBox))
                        {
                            double minx = viewBox.MinX;
                            double maxx = viewBox.MaxX;
                            double miny = viewBox.MinY;
                            double maxy = viewBox.MaxY;
                            
                            if (minx > rasterBounds.MinX)
                                minx -= (minx - rasterBounds.MinX) % _binding.PixelWidth;

                            if (maxx < rasterBounds.MaxX)
                                maxx += (rasterBounds.MaxX - maxx) % _binding.PixelWidth;

                            if (miny > rasterBounds.MinY)
                                miny -= (miny - rasterBounds.MinY) % _binding.PixelHeight;

                            if (maxy < rasterBounds.MaxY)
                                maxy += (rasterBounds.MaxY - maxy) % _binding.PixelHeight;

                            BoundingRectangle box = (BoundingRectangle)viewBox.Clone();

                            BoundingRectangle visibleRasterAreaOnTheMap =
                                new BoundingRectangle(
                                        Math.Max(rasterBounds.MinX, minx),
                                        Math.Max(rasterBounds.MinY, miny),
                                        Math.Min(rasterBounds.MaxX, maxx),
                                        Math.Min(rasterBounds.MaxY, maxy)
                                    );


                            if (visibleRasterAreaOnTheMap.Width > 0 && visibleRasterAreaOnTheMap.Height > 0)
                            {
                                int srcWidth = (int)Math.Round(visibleRasterAreaOnTheMap.Width / _binding.PixelWidth);
                                int srcHeight = (int)Math.Round(visibleRasterAreaOnTheMap.Height / _binding.PixelHeight);

                                int destWidth = Math.Min((int)Math.Round(visibleRasterAreaOnTheMap.Width / pixelSize), srcWidth);
                                int destHeight = Math.Min((int)Math.Round(visibleRasterAreaOnTheMap.Height / pixelSize), srcHeight);

                                args.Provider.QueryRaster(
                                    (int)Math.Round(_binding.RasterX - (_binding.MapPoint.X - visibleRasterAreaOnTheMap.MinX) / _binding.PixelWidth),
                                    (int)Math.Round(_binding.RasterY - (visibleRasterAreaOnTheMap.MaxY - _binding.MapPoint.Y) / _binding.PixelHeight),
                                    srcWidth, srcHeight, destWidth, destHeight, visibleRasterAreaOnTheMap, this);
                            }
                        }
                        else // raster and view box do not intersect
                            this._rasterPreview = null;
                    }
                    finally
                    {
                        if (DataSourceReadyToRelease != null)
                            DataSourceReadyToRelease(this, args);
                    }
                }
            }
        }

        internal void RenderRaster(Graphics g,
                BoundingRectangle viewBox,
                double scaleFactor)
        {
            if (_rasterRenderer == null)
                throw new InvalidOperationException("RasterRenderer is not set.");

            RasterStyle rasterStyle = (RasterStyle)_style.Clone();
            if (BeforeRasterRender != null)
            {
                RasterRenderEventArgs args = new RasterRenderEventArgs(rasterStyle);
                BeforeRasterRender(this, args);
                rasterStyle = args.Style;
            }

            if (_rasterPreview != null)
                _rasterRenderer.RenderRaster(g, _rasterPreview.Image, rasterStyle, viewBox, _rasterPreview.Bounds, scaleFactor);
        }

        #region IRasterReceiver Members

        /// <summary>
        /// Adds a raster to the receiver.
        /// </summary>
        /// <param name="bitmap">A System.Drawing.Bitmap instance representing the reveiving raster</param>
        /// <param name="bounds">A bounding rectangle defining a bounds of the receiving raster on the map</param>
        /// <param name="originalWidth">A width (in pixels) of original raster</param>
        /// <param name="originalHeight">A height (in pixels) of original raster</param>
        public void AddRasterPreview(Bitmap bitmap, BoundingRectangle bounds, int originalWidth, int originalHeight)
        {
            if (_rasterPreview != null)
            {
                _rasterPreview.Dispose();
                _rasterPreview = null;
            }

            if (bitmap != null)
                _rasterPreview = new RasterPreview(bounds, bitmap, originalWidth, originalHeight);
        }

        #endregion
    }

    /// <summary>
    /// Provides access to methods of object that can 
    /// receive rasters from IRasterProvider implementors.
    /// </summary>
    public interface IRasterReceiver
    {
        /// <summary>
        /// Adds a raster preview to the receiver.
        /// </summary>
        /// <param name="bitmap">A System.Drawing.Bitmap instance representing the reveiving raster</param>
        /// <param name="bounds">A bounding rectangle defining a bounds of the receiving preview on the map</param>
        /// <param name="originalWidth">A width (in pixels) of original raster</param>
        /// <param name="originalHeight">A height (in pixels) of original raster</param>
        void AddRasterPreview(Bitmap bitmap, BoundingRectangle bounds, int originalWidth, int originalHeight);
    }

    /// <summary>
    /// Represents a preview of the raster.
    /// </summary>
    public class RasterPreview : IDisposable
    {
        private BoundingRectangle _bounds;
        private Bitmap _image;
        private int _originalWidth = 0;
        private int _originalHeight = 0;



        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    if (_image != null)
                        _image.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes current instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets a System.Drawing.Image instance
        /// containing the preview of raster.
        /// </summary>
        public Bitmap Image
        {
            get { return _image; }
        }

        /// <summary>
        /// Gets or sets a bounds of the raster in map units.
        /// </summary>
        public BoundingRectangle Bounds
        {
            get { return _bounds; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int OriginalWidth
        {
            get { return _originalWidth; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int OriginalHeight
        {
            get { return _originalHeight; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.RasterPreview.
        /// </summary>
        public RasterPreview(BoundingRectangle bounds, Bitmap image, int originalWidth, int originalHeight)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            if (bounds == null)
                throw new ArgumentNullException("bounds");

            if (bounds.IsEmpty())
                throw new ArgumentException("Bounds should not be empty", "bounds");

            _originalWidth = originalWidth;
            _originalHeight = originalHeight;

            _bounds = bounds;
            _image = image;
        }
    }


    /// <summary>
    /// Represents a layer based on vector data.
    /// </summary>
    public class FeatureLayer : LayerBase, IFeatureReceiver, ISpatialDataProvider
    {
        //private string _title = string.Empty;
        private string _dataSource = string.Empty;
        private LayerDataSourceType _dataSourceType;
       
        private List<FeatureLayerExtension> _featureLayerExtensions = new List<FeatureLayerExtension>();  
        private bool _featuresSelectable = true;
        private bool _areFeaturesAutoLoadable = false;

        private List<string> _featureAttributeNames = new List<string>();
        private LayerLegendSettings _legendSettings = new LayerLegendSettings(false, false, false, string.Empty, string.Empty, string.Empty);

        private PolygonStyle _polygonStyle = new PolygonStyle();
        private PolylineStyle _polylineStyle = new PolylineStyle();
        private TitleStyle _titleStyle = new TitleStyle();
        private PointStyle _pointStyle = new PointStyle();
        private AutoTitleSettings _autoTitleSettings;

        bool _renderSubpixelDetails = true;

        private Collection<Feature> _points = new Collection<Feature>();
        private Collection<Feature> _multiPoints = new Collection<Feature>();
        private Collection<Feature> _polylines = new Collection<Feature>();
        private Collection<Feature> _polygons = new Collection<Feature>();

        private ISpatialIndex _pointsIndex;
        private ISpatialIndex _polylinesIndex;
        private ISpatialIndex _polygonsIndex;



        // harmless adjustment index
        private IndexSettings _defaultPolylinesIndexSettings = new IndexSettings(10, 100000, 4);
        private IndexSettings _defaultPointsIndexSettings = new IndexSettings(10, 100000, 4);
        private IndexSettings _defaultPolygonsIndexSettings = new IndexSettings(10, 100000, 4);

        private IFeatureRenderer _featureRenderer;

        private string _invalidFeatureAdditionErrorMessage = "Unable to add a feature to the layer during the drawing map or querying data";
        private string _invalidFeatureDeletionErrorMessage = "Unable to delete a feature from the layer during the drawing map or querying data";
        private string _invalidFeaturesDeletionErrorMessage = "Unable to delete features from the layer during the drawing map or querying data";

        private event EventHandler<EventArgs> _featureCollectionModified;

        #region FeaturesEnumerable

        private class FeaturesEnumerable : IEnumerable<Feature>
        {
            private FeatureLayer _layer = null;

            #region IEnumerable<Feature> Members

            public IEnumerator<Feature> GetEnumerator()
            {
                return (IEnumerator<Feature>)(this as IEnumerable).GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                LayerFeaturesEnumerator enumerator = new LayerFeaturesEnumerator(_layer);
                return enumerator;   
            }

            #endregion

            public FeaturesEnumerable(FeatureLayer layer)
            {
                if (layer == null)
                    throw new ArgumentNullException("layer");
                _layer = layer;
            }
        }

        private class LayerFeaturesEnumerator : IEnumerator<Feature>
        {
            private int _position = -1;
            private bool _isValid = true;
            private FeatureLayer _layer = null;
            private FeatureType _currentFeatureType = FeatureType.Point;

            private Collection<Feature> getCurrentCollection()
            {
                switch (_currentFeatureType)
                {
                    case FeatureType.Point: return _layer._points;
                    case FeatureType.Polyline: return _layer._polylines;
                    case FeatureType.Polygon: return _layer._polygons;
                    case FeatureType.MultiPoint: return _layer._multiPoints;
                }
                return null;
            }

            private void featuresCollectionModified(object sender, EventArgs e)
            {
                _isValid = false;
            }

            private void checkValid()
            {
                if(!_isValid)
                    throw new InvalidOperationException("Collection was modified");
            }

            #region IEnumerator<Feature> Members

            public Feature Current
            {
                get
                {
                    checkValid();
                    try
                    {
                        Collection<Feature> currentCollection = getCurrentCollection();
                        return currentCollection[_position];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                _layer._featureCollectionModified -= featuresCollectionModified;
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get 
                {
                    return ((LayerFeaturesEnumerator)this).Current; 
                }
            }

            public bool MoveNext()
            {
                checkValid();

                Collection<Feature> currentCollection = getCurrentCollection();

                if (_position < currentCollection.Count - 1)
                {
                    _position++;
                    return true;
                }
                else
                {
                    if (object.ReferenceEquals(currentCollection, _layer._polygons))
                        return false;

                    if (object.ReferenceEquals(currentCollection, _layer._points))
                    {
                        _position = -1;
                        _currentFeatureType = FeatureType.MultiPoint;
                        return MoveNext();
                    }
                    if (object.ReferenceEquals(currentCollection, _layer._multiPoints))
                    {
                        _position = -1;
                        _currentFeatureType = FeatureType.Polyline;
                        return MoveNext();
                    }
                    if (object.ReferenceEquals(currentCollection, _layer._polylines))
                    {
                        _position = -1;
                        _currentFeatureType = FeatureType.Polygon;
                        return MoveNext();
                    }
                }
                return false;
            }

            public void Reset()
            {
                checkValid();
                _position = -1;
                _currentFeatureType = FeatureType.Point;
            }

            #endregion

            public LayerFeaturesEnumerator(FeatureLayer layer)
            {
                if (layer == null)
                    throw new ArgumentNullException("layer");

                _layer = layer;
                _layer._featureCollectionModified += featuresCollectionModified;
            }
        }

        #endregion

        private int addFeaturesToReceiver(IFeatureReceiver fr, IEnumerable<Feature> features)
        {
            int result = 0;
            foreach (Feature f in features)
            {
                bool isAccepted = true;
                if (FeatureFetched != null)
                {
                    FeatureOperationEventArgs foea = new FeatureOperationEventArgs(f);
                    FeatureFetched(this, foea);
                    isAccepted = foea.IsAccepted;
                }

                if (isAccepted)
                {
                    fr.AddFeature((Feature)f.Clone());
                    result++;
                }

            }

            return result;
        }

        internal  void RegistreExtension(FeatureLayerExtension extension)
        {
            _featureLayerExtensions.Add(extension);
            extension.RegistryExtension(this);
        }
        internal void UnRegistreExtension(FeatureLayerExtension extension)
        {
            _featureLayerExtensions.Remove(extension);
            extension.UnRegistryExtension(this);
        }

        #region Public properties

        /// <summary>
        /// Gets or sets the MapAround.Mapping.Map instance
        /// on which this layer is.
        /// </summary>
        public override Map Map
        {
            get 
            {
                return base.Map;
            }
            protected set
            {
                base.Map = value;
                if (value != null)
                    _featureRenderer = value.FeatureRenderer;
                else
                    _featureRenderer = null;
            }
        }

        /// <summary>
        /// Enumerates a features of this layer.
        /// <para>
        /// Enumeration performs in the following order:
        /// point features, line features, polygonal features.
        /// </para>
        /// </summary>
        public IEnumerable<Feature> Features
        {
            get { return new FeaturesEnumerable(this); }
        }

        /// <summary>
        /// Gets or sets a string representing 
        /// the data source of this layer.
        /// </summary>
        [Obsolete("Use DataProviderRegName and DataProviderParameters instead.")]
        public string DataSource
        {
            get { return _dataSource; }
            set { _dataSource = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// the subpixel details of features will be 
        /// rendered.
        /// <remarks>
        /// Filtering of small details performs every time the map renders
        /// and may take some time, which depends on the selected scale 
        /// (pixel size). This option is useful only if the time of filtering
        /// and the time of rendering filtered features is less than the time 
        /// of rendering non-filtered features.
        /// </remarks>
        /// </summary>
        public bool RenderSubpixelDetails
        {
            get { return _renderSubpixelDetails; }
            set { _renderSubpixelDetails = value; }
        }


        /// <summary>
        /// Gets or sets a value indicating whether 
        /// a features of this layer will be loaded
        /// automatically before the map rendering.
        /// <para>
        /// This property determines the policy of data queries. When the property 
        /// value is true, the query is executed every time you call Map.LoadFeatures. 
        /// If the property is false, query is not executed, but the application 
        /// can manage the extraction of data by itself.
        /// </para>
        /// <para>
        /// Data providers that supports caching can implement other 
        /// strategies for data retrieval. For example, to perform 
        /// the fetching of all data in the first request and a partial 
        /// recovery from the cache during subsequent queries.
        /// </para>
        /// </summary>
        public bool AreFeaturesAutoLoadable
        {
            get { return _areFeaturesAutoLoadable; }
            set { _areFeaturesAutoLoadable = value; }
        }



        /// <summary>
        /// Gets or sets an object that renders 
        /// features of this layer.
        /// </summary>
        public IFeatureRenderer FeatureRenderer
        {
            get { return _featureRenderer; }
            set { _featureRenderer = value; }
        }

        /// <summary>
        /// Gets or sets a type of data source.
        /// </summary>
        [Obsolete("Use DataProviderRegName and DataProviderParameters instead.")]
        public LayerDataSourceType DataSourceType
        {
            get { return _dataSourceType; }
            set { _dataSourceType = value; }
        } 

        /// <summary>
        /// Gets or sets a style of polygonal features.
        /// </summary>
        public PolygonStyle PolygonStyle
        {
            get { return _polygonStyle; }
            set { _polygonStyle = value; }
        }

        /// <summary>
        /// Gets or sets a style of linear features.
        /// </summary>
        public PolylineStyle PolylineStyle
        {
            get { return _polylineStyle; }
            set { _polylineStyle = value; }
        }

        /// <summary>
        /// Gets or sets a style of titles.
        /// </summary>
        public TitleStyle TitleStyle
        {
            get { return _titleStyle; }
            set { _titleStyle = value; }
        }

        /// <summary>
        /// Gets or sets a style of point features.
        /// </summary>
        public PointStyle PointStyle
        {
            get { return _pointStyle; }
            set { _pointStyle = value; }
        }

        /// <summary>
        /// Gets or sets the object that defines an automatic titles
        /// for this layer.
        /// </summary>
        public AutoTitleSettings AutoTitleSettings
        {
            get { return _autoTitleSettings; }
            set { _autoTitleSettings = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// is it possible to select the feratures 
        /// of this layer.
        /// </summary>
        /// <remarks>
        /// Mainly used by SelectObject() and SelectObjects() methods.
        /// </remarks>
        public bool FeaturesSelectable
        {
            get { return _featuresSelectable; }
            set { _featuresSelectable = value; }
        }

        /// <summary>
        /// Gets a collection of point features.
        /// </summary>
        public ReadOnlyCollection<Feature> Points
        {
            get { return new ReadOnlyCollection<Feature>(_points); }
        }

        /// <summary>
        /// Gets a collection of multipoint features.
        /// </summary>
        public ReadOnlyCollection<Feature> MultiPoints
        {
            get { return new ReadOnlyCollection<Feature>(_multiPoints); }
        }

        /// <summary>
        /// Gets a collection of polyline features.
        /// </summary>
        public ReadOnlyCollection<Feature> Polylines
        {
            get { return new ReadOnlyCollection<Feature>(_polylines); }
        }

        /// <summary>
        /// Gets a collection of polygonal features.
        /// </summary>
        public ReadOnlyCollection<Feature> Polygons
        {
            get { return new ReadOnlyCollection<Feature>(_polygons); }
        }


        /// <summary>
        /// Gets a value indicating whether a 
        /// spatial index of point features exists.
        /// </summary>
        public bool PointsIndexExists
        {
            get { return _pointsIndex != null; }
        }

        /// <summary>
        /// Gets a value indicating whether a 
        /// spatial index of linear features exists.
        /// </summary>
        public bool PolylinesIndexExists
        {
            get { return _polylinesIndex != null; }
        }

        /// <summary>
        /// Gets a value indicating whether a 
        /// spatial index of polygon features exists.
        /// </summary>
        public bool PolygonsIndexExists
        {
            get { return _polygonsIndex != null; }
        }

        /// <summary>
        /// Gets or sets the default index settings of point features.
        /// If the index type is not supported, KDTRee will be used.
        /// </summary>
        public IndexSettings DefaultPointsIndexSettings
        {
            get { return _defaultPointsIndexSettings; }
            set { _defaultPointsIndexSettings = value; }
        }

        /// <summary>
        /// Gets or sets the default index settings of linear features.
        /// If the index type is not supported, KDTRee will be used.
        /// </summary>
        public IndexSettings DefaultPolylinesIndexSettings
        {
            get { return _defaultPolylinesIndexSettings; }
            set { _defaultPolylinesIndexSettings = value; }
        }

        /// <summary>
        /// Gets or sets the default index settings of polygon features.
        /// If the index type is not supported, KDTRee will be used.
        /// </summary>
        public IndexSettings DefaultPolygonsIndexSettings
        {
            get { return _defaultPolygonsIndexSettings; }
            set { _defaultPolygonsIndexSettings = value; }
        }

        /// <summary>
        /// Gets or sets a names of non-spatial attributes.
        /// </summary>
        public List<string> FeatureAttributeNames
        {
            get { return _featureAttributeNames; }
            set { _featureAttributeNames = value; }
        }

        /// <summary>
        /// Gets or sets a legend settings of this layer.
        /// </summary>
        public LayerLegendSettings LegendSettings
        {
            get { return _legendSettings; }
            set { _legendSettings = value; }
        }

        /// <summary>
        /// Get Extesions by Layer
        /// </summary>
        public ReadOnlyCollection<FeatureLayerExtension> FeatureLayerExtensions
        {
            get {return new ReadOnlyCollection<FeatureLayerExtension>(_featureLayerExtensions);}
        }

        #endregion

        #region Events

        /// <summary>
        /// Raises before render a polygon feature.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePolygonRender = null;

        /// <summary>
        /// Raises before render a polyline feature.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePolylineRender = null;

        /// <summary>
        /// Raises before render apoint feature.
        /// </summary>
        public event EventHandler<FeatureRenderEventArgs> BeforePointRender = null;

        /// <summary>
        /// Raises when the data provider needs to perform data query.
        /// </summary>
        public event EventHandler<FeatureDataSourceEventArgs> DataSourceNeeded = null;

        /// <summary>
        /// Raises when the data provider may release managed and unmanaged resources.
        /// </summary>
        public event EventHandler<FeatureDataSourceEventArgs> DataSourceReadyToRelease = null;

        #endregion

        /// <summary>
        /// Computes the bounding rectangle of all 
        /// features of this layer.
        /// </summary>
        /// <returns>Bounding rectangle of all features of this layer</returns>
        public BoundingRectangle GetBoundingRectangle()
        {
            BoundingRectangle bounds = new BoundingRectangle();

            foreach (Feature feature in Points)
                bounds.Join(feature.BoundingRectangle);

            foreach (Feature feature in Polylines)
                bounds.Join(feature.BoundingRectangle);

            foreach (Feature feature in Polygons)
                bounds.Join(feature.BoundingRectangle);

            foreach (Feature feature in MultiPoints)
                bounds.Join(feature.BoundingRectangle);

            return bounds;
        }

        /// <summary>
        /// Removes spatial index of point features.
        /// </summary>
        public void DropPointsIndex()
        {
            _pointsIndex = null;
        }

        /// <summary>
        /// Removes spatial index of linear features.
        /// </summary>
        public void DropPolylinesIndex()
        {
            _polylinesIndex = null;
        }

        /// <summary>
        /// Removes spatial index of polygon features.
        /// </summary>
        public void DropPolygonsIndex()
        {
            _polygonsIndex = null;
        }

        #region Feature managing

        /// <summary>
        /// Adds a point feature to this layer.
        /// </summary>
        /// <param name="point">A point feature to add</param>
        public void AddPoint(Feature point)
        {
            CheckOperationPossibility(_invalidFeatureAdditionErrorMessage);

            if(point.FeatureType != FeatureType.Point)
                throw new ArgumentException("Adding feature is not a point");

            if (_pointsIndex != null)
                if (_pointsIndex.IndexedSpace.ContainsPoint(point.Point.Coordinate))
                {
                    _pointsIndex.Insert(point);
                }
                else
                    _pointsIndex = null;

            _points.Add(point);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            point.SetLayer(this);
        }

        /// <summary>
        /// Removes a point feature from this layer.
        /// </summary>
        /// <param name="index">A zero-based index of feature (in Points collection) to delete</param>
        public void RemovePoint(int index)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _pointsIndex = null;

            _points[index].SetLayer(null);
            _points.RemoveAt(index);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Removes a point feature from this layer.
        /// </summary>
        /// <param name="point">A point feature to delete</param>
        public void RemovePoint(Feature point)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _pointsIndex = null;

            _points.Remove(point);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            point.SetLayer(null);
        }

        /// <summary>
        /// Removes all point features.
        /// </summary>
        public void RemoveAllPoints()
        {
            CheckOperationPossibility(_invalidFeaturesDeletionErrorMessage);

            foreach (Feature feature in _points)
                feature.SetLayer(null);

            _pointsIndex = null;

            _points.Clear();

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Adds a multipoint features to this layer.
        /// </summary>
        /// <param name="multiPoint">A multipoint feature to add</param>
        public void AddMultiPoint(Feature multiPoint)
        {
            CheckOperationPossibility(_invalidFeatureAdditionErrorMessage);

            if (multiPoint.FeatureType != FeatureType.MultiPoint)
                throw new ArgumentException("Adding feature is not a multipoint");

            if (_pointsIndex != null)
                if (_pointsIndex.IndexedSpace.ContainsRectangle(multiPoint.BoundingRectangle))
                {
                    _pointsIndex.Insert(multiPoint);
                }
                else
                    _pointsIndex = null;

            _multiPoints.Add(multiPoint);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            multiPoint.SetLayer(this);
        }

        /// <summary>
        /// Removes a multipoint feature from this layer.
        /// </summary>
        /// <param name="index">A zero-based index of feature (in MultiPoints collection) to delete</param>
        public void RemoveMultiPoint(int index)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _pointsIndex = null;

            _multiPoints[index].SetLayer(null);
            _multiPoints.RemoveAt(index);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Removes a multipoint feature from this layer.
        /// </summary>
        /// <param name="multiPoint">A multipoint feature to delete</param>
        public void RemoveMultiPoint(Feature multiPoint)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _pointsIndex = null;

            _multiPoints.Remove(multiPoint);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            multiPoint.SetLayer(null);
        }

        /// <summary>
        /// Removes all multipoint features.
        /// </summary>
        public void RemoveAllMultiPoints()
        {
            CheckOperationPossibility(_invalidFeaturesDeletionErrorMessage);

            foreach (Feature feature in _multiPoints)
                feature.SetLayer(null);

            _pointsIndex = null;

            _multiPoints.Clear();

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Adds a polyline feature to this layer.
        /// </summary>
        /// <param name="polyline">A polyline feature to add</param>
        public void AddPolyline(Feature polyline)
        {
            CheckOperationPossibility(_invalidFeatureAdditionErrorMessage);

            if (polyline.FeatureType != FeatureType.Polyline)
                throw new ArgumentException("Adding feature is not a polyline");

            if (_polylinesIndex != null)
                if (_polylinesIndex.IndexedSpace.ContainsRectangle(polyline.BoundingRectangle))
                {
                    _polylinesIndex.Insert(polyline);
                }
                else
                    _polylinesIndex = null;

            _polylines.Add(polyline);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            polyline.SetLayer(this);
        }

        /// <summary>
        /// Removes a polyline feature from this layer.
        /// </summary>
        /// <param name="index">A zero-based index of feature (in Polylines collection) to delete</param>
        public void RemovePolyline(int index)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _polylinesIndex = null;

            _polylines[index].SetLayer(null);
            _polylines.RemoveAt(index);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Removes a polyline feature from this layer.
        /// </summary>
        /// <param name="polyline">A polyline feature to delete</param>
        public void RemovePolyline(Feature polyline)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _polylinesIndex = null;

            _polylines.Remove(polyline);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            polyline.SetLayer(null);
        }

        /// <summary>
        /// Removes all polyline features.
        /// </summary>
        public void RemoveAllPolylines()
        {
            CheckOperationPossibility(_invalidFeaturesDeletionErrorMessage);

            foreach (Feature feature in _polylines)
                feature.SetLayer(null);

            _polylinesIndex = null;

            _polylines.Clear();

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Adds a polygon feature to this layer.
        /// </summary>
        /// <param name="polygon">A polygon feature to add</param>
        public void AddPolygon(Feature polygon)
        {
            CheckOperationPossibility(_invalidFeatureAdditionErrorMessage);

            if (polygon.FeatureType != FeatureType.Polygon)
                throw new ArgumentException("Adding feature is not a polygon");

            if (_polygonsIndex != null)
                if (_polygonsIndex.IndexedSpace.ContainsRectangle(polygon.BoundingRectangle))
                {
                    _polygonsIndex.Insert(polygon);
                }
                else
                    _polygonsIndex = null;

            _polygons.Add(polygon);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            polygon.SetLayer(this);
        }

        /// <summary>
        /// Removes a polygon feature from this layer.
        /// </summary>
        /// <param name="index">A zero-based index of feature (in Polygons collection) to delete</param>
        public void RemovePolygon(int index)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _polygonsIndex = null;

            _polygons[index].SetLayer(null);
            _polygons.RemoveAt(index);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Removes a polygon feature from this layer.
        /// </summary>
        /// <param name="polygon">A polygon feature to delete</param>
        public void RemovePolygon(Feature polygon)
        {
            CheckOperationPossibility(_invalidFeatureDeletionErrorMessage);

            _polygonsIndex = null;

            _polygons.Remove(polygon);

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());

            polygon.SetLayer(null);
        }

        /// <summary>
        /// Removes all polygon features.
        /// </summary>
        public void RemoveAllPolygons()
        {
            CheckOperationPossibility(_invalidFeaturesDeletionErrorMessage);

            foreach (Feature feature in _polygons)
                feature.SetLayer(null);

            _polygonsIndex = null;

            _polygons.Clear();

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Removes all features.
        /// </summary>
        public void RemoveAllFeatures()
        {
            CheckOperationPossibility(_invalidFeaturesDeletionErrorMessage);

            _pointsIndex = null;
            _polylinesIndex = null;
            _polygonsIndex = null;

            _polygons.Clear();
            _polylines.Clear();
            _multiPoints.Clear();
            _points.Clear();

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        /// <summary>
        /// Adds a feature to this layer.
        /// </summary>
        /// <param name="feature">Feature to add</param>
        public void AddFeature(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException("feature");

            switch (feature.FeatureType)
            { 
                case FeatureType.Point:
                    AddPoint(feature);
                    break;
                case FeatureType.Polyline:
                    AddPolyline(feature);
                    break;
                case FeatureType.Polygon:
                    AddPolygon(feature);
                    break;
                case FeatureType.MultiPoint:
                    AddMultiPoint(feature);
                    break;
            }

            if (_featureCollectionModified != null)
                _featureCollectionModified(this, new EventArgs());
        }

        #endregion

        #region Indexing

        /// <summary>
        /// Builds an index of point features.
        /// </summary>
        /// <param name="indexSettings">An index settings</param>
        public void BuildPointsIndex(IndexSettings indexSettings)
        {
            BoundingRectangle bounds = new BoundingRectangle();

            foreach (Feature feature in Points)
                bounds.Join(feature.BoundingRectangle);

            foreach (Feature feature in MultiPoints)
                bounds.Join(feature.BoundingRectangle);

            if (!bounds.IsEmpty())
            {
                List<Feature> features = new List<Feature>();

                foreach (Feature feature in Points)
                    features.Add(feature);

                foreach (Feature feature in MultiPoints)
                    features.Add(feature);

                if(indexSettings.IndexType == "QuadTree")
                    _pointsIndex = new QuadTree(bounds);
                if(_pointsIndex == null)
                    _pointsIndex = new KDTree(bounds);

                _pointsIndex.MaxDepth = indexSettings.MaxDepth;
                _pointsIndex.BoxSquareThreshold = indexSettings.BoxSquareThreshold;
                _pointsIndex.MinObjectCount = indexSettings.MinFeatureCount;

                _pointsIndex.Build(features);
            }
        }

        /// <summary>
        /// Builds an index of linear features.
        /// </summary>
        /// <param name="indexSettings">An index settings</param>
        public void BuildPolylinesIndex(IndexSettings indexSettings)
        {
            BoundingRectangle bounds = new BoundingRectangle();

            foreach (Feature feature in Polylines)
                bounds.Join(feature.Polyline.GetBoundingRectangle());

            if (!bounds.IsEmpty())
            {
                if (indexSettings.IndexType == "QuadTree")
                    _pointsIndex = new QuadTree(bounds);
                if (_pointsIndex == null)
                    _pointsIndex = new KDTree(bounds);

                _polylinesIndex = new KDTree(bounds);
                _polylinesIndex.MaxDepth = indexSettings.MaxDepth;
                _polylinesIndex.BoxSquareThreshold = indexSettings.BoxSquareThreshold;
                _polylinesIndex.MinObjectCount = indexSettings.MinFeatureCount;

                _polylinesIndex.Build(Polylines);
            }
        }

        /// <summary>
        /// Builds an index of areal features.
        /// </summary>
        /// <param name="indexSettings">An index settings</param> 
        public void BuildPolygonsIndex(IndexSettings indexSettings)
        {
            BoundingRectangle bounds = new BoundingRectangle();

            foreach (Feature feature in Polygons)
                bounds.Join(feature.Polygon.GetBoundingRectangle());

            if (!bounds.IsEmpty())
            {
                if (indexSettings.IndexType == "QuadTree")
                    _pointsIndex = new QuadTree(bounds);
                if (_pointsIndex == null)
                    _pointsIndex = new KDTree(bounds);

                _polygonsIndex = new KDTree(bounds);
                _polygonsIndex.MaxDepth = indexSettings.MaxDepth;
                _polygonsIndex.BoxSquareThreshold = indexSettings.BoxSquareThreshold;
                _polygonsIndex.MinObjectCount = indexSettings.MinFeatureCount;

                _polygonsIndex.Build(Polygons);
            }
        }

        /// <summary>
        /// Builds an index of point features.
        /// </summary>
        public void BuildPointsIndex()
        {
            BuildPointsIndex(_defaultPointsIndexSettings);
        }

        /// <summary>
        /// Builds an index of linear features.
        /// </summary>
        public void BuildPolylinesIndex()
        {
            BuildPolylinesIndex(_defaultPolylinesIndexSettings);
        }

        /// <summary>
        /// Builds an index of areal features.
        /// </summary>
        public void BuildPolygonsIndex()
        {
            BuildPolygonsIndex(_defaultPolygonsIndexSettings);
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Renders a sample of features of this layer.
        /// </summary>
        /// <param name="image">A System.Drawing.Image instance to render</param> 
        /// <param name="drawPoint">A value indicating whether a point sample will be rendered</param> 
        /// <param name="drawPolyline">A value indicating whether a line sample will be rendered</param> 
        /// <param name="drawPolygon">A value indicating whether a polygon sample will be rendered</param> 
        public void RenderFeaturesSample(Image image, bool drawPoint, bool drawPolyline, bool drawPolygon)
        {
            if (_featureRenderer == null)
                throw new InvalidOperationException("FeatureRenderer is not set");

            int margin = 2;

            int sampleCount = 0;
            if (drawPoint) sampleCount++;
            if (drawPolygon) sampleCount++;
            if (drawPolyline) sampleCount++;

            if (sampleCount == 0)
                return;

            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = Map.RenderingSettings.AntiAliasGeometry ? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;

                if (drawPoint)
                    Map.RenderPointSampleInternal(g, new BoundingRectangle(0, 0, image.Width / sampleCount, image.Height), PointStyle);

                if (drawPolyline)
                {
                    int minX = 0;
                    int maxX = 0;
                    switch (sampleCount)
                    { 
                        case 3:
                            minX = image.Width / 3;
                            maxX = 2 * image.Width / 3;
                            break;
                        case 2:
                            if (drawPoint)
                            {
                                minX = image.Width / 2;
                                maxX = image.Width;
                            }
                            else
                                maxX = image.Width / 2;
                            break;
                        case 1:
                            maxX = image.Width;
                            break;
                    }

                    Map.RenderPolylineSampleInternal(g, new BoundingRectangle(minX, 0, maxX, image.Height), PolylineStyle, margin);
                }

                if(drawPolygon)
                    Map.RenderPolygonSampleInternal(g, new BoundingRectangle((sampleCount - 1) * image.Width / sampleCount, 0, image.Width, image.Height), PolygonStyle, margin);
            }
        }

        private int renderPolygons(Graphics g, 
                                   BoundingRectangle viewBox,
                                   BoundingRectangle mapCoordsViewBox, 
                                   bool titlesVisible, 
                                   double scaleFactor)
        {
            int result = 0;

            IEnumerable<Feature> polygons = _polygons;
            if (_polygonsIndex != null)
            {
                // trying to extract the necessary objects from the index
                List<Feature> pg = new List<Feature>();
                _polygonsIndex.QueryObjectsInRectangle(mapCoordsViewBox, pg);
                polygons = pg;
            }

            foreach (Feature feature in polygons)
            {
                if ( feature.Visible&&(_polygonsIndex != null || feature.BoundingRectangle.Intersects(mapCoordsViewBox)))
                {
                    if (BeforePolygonRender != null)
                    {
                        BeforePolygonRender(this, new FeatureRenderEventArgs(feature, titlesVisible && TitleStyle.TitlesVisible));

                        // perhaps a handler changed the value PlanimetryAlgorithms.Tolerance,
                        //need to return to the set of the cards.
                        //PlanimetryAlgorithms.Tolerance = Map.Tolerance;
                    }

                    PolygonStyle style = feature.PolygonStyle == null ? this.PolygonStyle : feature.PolygonStyle;
                    TitleStyle titleStyle = feature.TitleStyle == null ? this.TitleStyle : feature.TitleStyle;                    

                    titlesVisible = titleStyle.VisibleScale < scaleFactor && titleStyle.TitlesVisible;
                    if (titlesVisible && _autoTitleSettings != null)
                        setAutoTitle(feature);

                    // Projection on the fly
                    if (Map.OnTheFlyTransform != null)
                    {
                        Polygon p = GeometryTransformer.TransformPolygon(feature.Polygon, Map.OnTheFlyTransform);
                        Feature tempFeature = new Feature(FeatureType.Polygon);
                        tempFeature.Title = feature.Title;
                        tempFeature.Selected = feature.Selected;
                        tempFeature.Polygon = p;
                        FeatureRenderer.DrawPolygon(tempFeature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);
                    }
                    else
                        FeatureRenderer.DrawPolygon(feature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);
                                        
                    result++;
                }
            }
            return result;
        }

        private int renderPolylines(Graphics g, 
                                    BoundingRectangle viewBox,
                                    BoundingRectangle mapCoordsViewBox, 
                                    bool titlesVisible, 
                                    double scaleFactor)
        {
            int result = 0;

            IEnumerable<Feature> polylines = _polylines;
            if (_polylinesIndex != null)
            {
                // trying to extract the necessary objects from the index
                List<Feature> pl = new List<Feature>();
                _polylinesIndex.QueryObjectsInRectangle(mapCoordsViewBox, pl);
                polylines = pl;
            }

            foreach (Feature feature in polylines)
            {
                if (feature.Visible&&(_polylinesIndex != null || feature.BoundingRectangle.Intersects(mapCoordsViewBox)))
                {
                    if (BeforePolylineRender != null)
                    {
                        BeforePolylineRender(this, new FeatureRenderEventArgs(feature, titlesVisible && TitleStyle.TitlesVisible));

                        // perhaps a handler changed the value PlanimetryAlgorithms.Tolerance,
                        //need to return to the set of the cards.
                        //PlanimetryAlgorithms.Tolerance = Map.Tolerance;
                    }

                    PolylineStyle style = feature.PolylineStyle == null ? this.PolylineStyle : feature.PolylineStyle;
                    TitleStyle titleStyle = feature.TitleStyle == null ? this.TitleStyle : feature.TitleStyle;

                    titlesVisible = titleStyle.VisibleScale < scaleFactor && titleStyle.TitlesVisible;
                    if (titlesVisible && _autoTitleSettings != null)
                        setAutoTitle(feature);

                    //Projection on the fly
                    if (Map.OnTheFlyTransform != null)
                    {
                        Polyline p = GeometryTransformer.TransformPolyline(feature.Polyline, Map.OnTheFlyTransform);
                        Feature tempFeature = new Feature(FeatureType.Polyline);
                        tempFeature.Title = feature.Title;
                        tempFeature.Selected = feature.Selected;
                        tempFeature.Polyline = p;
                        FeatureRenderer.DrawPolyline(tempFeature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);
                    }
                    else
                        FeatureRenderer.DrawPolyline(feature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);

                    result++;
                }
            }

            return result;
        }

        private int renderPointFeatureCollection(IEnumerable<Feature> points,
                                 Graphics g,
                                 BoundingRectangle viewBox,
                                 BoundingRectangle mapCoordsViewBox,
                                 bool titlesVisible,
                                 double scaleFactor)
        {
            int result = 0;

            foreach (Feature feature in points)
            {
                if (feature.Visible&&(_pointsIndex != null || mapCoordsViewBox.Intersects(feature.BoundingRectangle)))
                {
                    if (BeforePointRender != null)
                    {
                        BeforePointRender(this, new FeatureRenderEventArgs(feature, titlesVisible && TitleStyle.TitlesVisible));

                        // perhaps a handler changed the value PlanimetryAlgorithms.Tolerance,
                        //need to return to the set of the cards.
                        //PlanimetryAlgorithms.Tolerance = Map.Tolerance;
                    }

                    PointStyle style = feature.PointStyle == null ? this.PointStyle : feature.PointStyle;
                    TitleStyle titleStyle = feature.TitleStyle == null ? this.TitleStyle : feature.TitleStyle;

                    titlesVisible = titleStyle.VisibleScale < scaleFactor && titleStyle.TitlesVisible;
                    if (titlesVisible && _autoTitleSettings != null)
                        setAutoTitle(feature);

                    // Projection on the fly
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
                        FeatureRenderer.DrawPoint(tempFeature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);
                    }
                    else
                        FeatureRenderer.DrawPoint(feature, g, style, titleStyle, viewBox, titlesVisible, scaleFactor);

                    result++;
                }
            }
            return result;
        }

        private int renderPoints(Graphics g, 
                                 BoundingRectangle viewBox,
                                 BoundingRectangle mapCoordsViewBox,
                                 bool titlesVisible, 
                                 double scaleFactor)
        {
            int result = 0;

            if (_pointsIndex != null)
            {
                // trying to extract the necessary objects from the index
                List<Feature> ps = new List<Feature>();
                _pointsIndex.QueryObjectsInRectangle(mapCoordsViewBox, ps);
                //points = ps;
                renderPointFeatureCollection(ps, g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
            }
            else
            {
                result += renderPointFeatureCollection(_multiPoints, g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
                result += renderPointFeatureCollection(_points, g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
            }

            return result;
        }

        internal int RenderFeatures(Graphics g, 
                                BoundingRectangle viewBox,
                                BoundingRectangle mapCoordsViewBox,
                                double scaleFactor)
        {
            if (_featureRenderer == null)
                throw new InvalidOperationException("FeatureRenderer is not set");

            int result = 0;

            // visible labels
            bool titlesVisible = TitleStyle.VisibleScale < scaleFactor;

            // The lens layer should not be displayed at a value scale
            if (scaleFactor >= MinVisibleScale && scaleFactor <= MaxVisibleScale)
            {
                FeatureRenderer.ReduceSubpixelDetails = !RenderSubpixelDetails;

                result += renderPolygons(g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
                FeatureRenderer.FlushSelectedFeatures(g, viewBox, scaleFactor);

                result += renderPolylines(g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
                FeatureRenderer.FlushSelectedFeatures(g, viewBox, scaleFactor);

                result += renderPoints(g, viewBox, mapCoordsViewBox, titlesVisible, scaleFactor);
                FeatureRenderer.FlushSelectedFeatures(g, viewBox, scaleFactor);
            }

            return result;
        }

        private void setAutoTitle(Feature feature)
        {
            if (feature.Attributes == null)
                return;

            int index = FeatureAttributeNames.IndexOf(_autoTitleSettings.AttributeName);
            if (index == -1)
                index = _autoTitleSettings.AttributeIndex;

            if (index >= 0 && index < feature.Attributes.Count())
            {
                object o = feature.Attributes[index];
                if (o != null)
                    feature.Title = o.ToString();
            }
        }

        #endregion 

        #region Feature querying

        /// <summary>
        /// Adds point features that are located within specified rectangle to the list.
        /// </summary>
        /// <param name="box">A bounding rectangle</param> 
        /// <param name="features">A list to add features</param> 
        public void CalculatePointsInRectangle(BoundingRectangle box, List<Feature> features)
        {
            if (_pointsIndex != null)
                _pointsIndex.QueryObjectsInRectangle(box, features);
            else
            {
                foreach (Feature point in Points)
                    if (box.ContainsPoint(point.Point.Coordinate))
                        features.Add(point);

                foreach (Feature point in MultiPoints)
                    if (box.Intersects(point.BoundingRectangle))
                        features.Add(point);
            }
        }

        /// <summary>
        /// Adds polyline features which bounding rectangles intersect specified rectangle to the list.
        /// </summary>
        /// <param name="box">A bounding rectangle</param> 
        /// <param name="features">A list to add features</param> 
        public void CalculatePolylinesInRectangle(BoundingRectangle box, List<Feature> features)
        {
            if (_polylinesIndex != null)
                _polylinesIndex.QueryObjectsInRectangle(box, features);
            else
            {
                foreach (Feature polyline in Polylines)
                    if (box.Intersects(polyline.BoundingRectangle))
                        features.Add(polyline);
            }
        }

        /// <summary>
        /// Adds polygon features which bounding rectangles intersect specified rectangle to the list.
        /// </summary>
        /// <param name="box">A bounding rectangle</param> 
        /// <param name="features">A list to add features</param> 
        public void CalculatePolygonsInRectangle(BoundingRectangle box, List<Feature> features)
        {
            if (_polygonsIndex != null)
                _polygonsIndex.QueryObjectsInRectangle(box, features);
            else
            {
                foreach (Feature polygon in Polygons)
                    if (box.Intersects(polygon.BoundingRectangle))
                        features.Add(polygon);
            }
        }

        /// <summary>
        /// Adds point features that are located within specified rectangle to the collection.
        /// </summary>
        /// <param name="rectangle">A bounding rectangle</param>
        /// <param name="features">A collection to add features</param>
        public void SelectPoints(BoundingRectangle rectangle, ICollection<Feature> features)
        {
            if (_pointsIndex != null)
            {
                List<Feature> points = new List<Feature>();
                _pointsIndex.QueryObjectsInRectangle(rectangle, points);
                foreach (Feature s in points)
                    features.Add(s);
            }
            else
            {
                foreach (Feature s in _points)
                    if (rectangle.ContainsPoint(s.Point.Coordinate))
                        features.Add(s);

                foreach (Feature s in _multiPoints)
                    if (rectangle.Intersects(s.BoundingRectangle))
                        features.Add(s);
            }
        }

        /// <summary>
        /// Adds polyline features which intersect specified rectangle to the collection.
        /// </summary>
        /// <param name="rectangle">A bounding rectangle</param> 
        /// <param name="features">A collection to add features</param> 
        public void SelectPolylines(BoundingRectangle rectangle, ICollection<Feature> features)
        {
            IEnumerable<Feature> polylines;
            if (_polylinesIndex != null)
            {
                List<Feature> pl = new List<Feature>();
                _polylinesIndex.QueryObjectsInRectangle(rectangle, pl);
                polylines = pl;
            }
            else
                polylines = _polylines;

            foreach (Feature s in polylines)
                if (rectangle.Intersects(s.Polyline))
                    features.Add(s);
        }

        /// <summary>
        /// Adds polygon features which intersect specified rectangle to the collection.
        /// </summary>
        /// <param name="rectangle">A bounding rectangle</param> 
        /// <param name="features">A collection to add features</param> 
        public void SelectPolygons(BoundingRectangle rectangle, ICollection<Feature> features)
        {
            IEnumerable<Feature> polygons;
            if (_polygonsIndex != null)
            {
                List<Feature> pg = new List<Feature>();
                _polygonsIndex.QueryObjectsInRectangle(rectangle, pg);
                polygons = (IEnumerable<Feature>)pg;
            }
            else
                polygons = _polygons;

            foreach (Feature s in polygons)
                if (rectangle.Intersects(s.Polygon))
                    features.Add(s);
        }

        /// <summary>
        /// Adds features which intersect specified rectangle to the collection.
        /// </summary>
        /// <param name="rectangle">A bounding rectangle</param> 
        /// <param name="features">A collection to add features</param>
        public void SelectObjects(BoundingRectangle rectangle, ICollection<Feature> features)
        {
            // point objects
            SelectPoints(rectangle, features);

            //linear objects
            SelectPolylines(rectangle, features);

            //area objects
            SelectPolygons(rectangle, features);
        }

        private void selectPoint(ICoordinate point, IEnumerable<Feature> pointFeatureCollection, out Feature selectedFeature)
        {
            selectedFeature = null;

            foreach (Feature feature in pointFeatureCollection)
            {
                if (!feature.Visible) continue;

                if (feature.FeatureType == FeatureType.Point)
                {
                    if (Math.Abs(feature.Point.X - point.X) < Map.SelectionPointRadius &&
                        Math.Abs(feature.Point.Y - point.Y) < Map.SelectionPointRadius)
                    {

                        selectedFeature = feature;
                        return;
                    }
                }
                else
                    foreach (ICoordinate p in feature.MultiPoint.Points)
                        if (Math.Abs(p.X - point.X) < Map.SelectionPointRadius &&
                            Math.Abs(p.Y - point.Y) < Map.SelectionPointRadius)
                        {
                            selectedFeature = feature;
                            return;
                        }
            }
        }

        /// <summary>
        /// Selects a feature of this layer.
        /// <p>
        /// Features is handled as follows:
        /// 1. Calculates the distance from the point features to the point of choice. 
        /// If the object is distant from point closer than SelectionPointRadius, it is selected.
        /// 2. Calculates the distance from the linear features to the point of choice. 
        /// If the feature is distant from point closer than SelectionPointRadius, it is selected.
        /// 3. Computes polygons, within which a point of choice is located. The first such polygon is selected.
        /// </p>
        /// <p>
        /// You should not make assumptions about what the features that satisfy the selection criteria 
        /// will be selected first. The order of selection may be dependent of the specific usage 
        /// of spatial indices and its settings. To find all the objects around a specified point, 
        /// use the <see cref="FeatureLayer.SelectObjects"/> method.
        /// </p>
        /// </summary>
        /// <param name="coordinate">A selection point</param> 
        /// <param name="selectedFeature">Selected feature</param> 
        public void SelectObject(ICoordinate coordinate, out Feature selectedFeature)
        {
            selectedFeature = null;

            if (_pointsIndex != null)
            {
                List<Feature> ps = new List<Feature>();
                BoundingRectangle box = new BoundingRectangle(coordinate.X - Map.SelectionPointRadius, coordinate.Y - Map.SelectionPointRadius,
                                                              coordinate.X + Map.SelectionPointRadius, coordinate.Y + Map.SelectionPointRadius);
                _pointsIndex.QueryObjectsInRectangle(box, ps);

                selectPoint(coordinate, ps, out selectedFeature);
                if (selectedFeature != null)
                    return;
            }
            else
            {
                selectPoint(coordinate, _points, out selectedFeature);
                if (selectedFeature != null)
                    return;

                selectPoint(coordinate, _multiPoints, out selectedFeature);
                if (selectedFeature != null)
                    return;
            }

            IEnumerable<Feature> polylines;
            if (_polylinesIndex != null)
            {
                List<Feature> pl = new List<Feature>();
                BoundingRectangle br = new PointD(coordinate).GetBoundingRectangle();
                br.Grow(Map.SelectionPointRadius);
                _polylinesIndex.QueryObjectsInRectangle(br, pl);
                polylines = pl;
            }
            else
                polylines = _polylines;

            // polyline
            foreach (Feature feature in polylines)
            {
                if (!feature.Visible) continue;
                BoundingRectangle bounds = new BoundingRectangle(feature.BoundingRectangle.Min, feature.BoundingRectangle.Max);
                bounds.Grow(Map.SelectionPointRadius);

                if (coordinate.X > bounds.MinX && coordinate.Y > bounds.MinY &&
                    coordinate.X < bounds.MaxX && coordinate.Y < bounds.MaxY)
                {
                    foreach (LinePath path in feature.Polyline.Paths)
                        for (int i = 0; i < path.Vertices.Count - 1; i++)
                        {
                            Segment s = new Segment(path.Vertices[i].X,
                                                    path.Vertices[i].Y,
                                                    path.Vertices[i + 1].X,
                                                    path.Vertices[i + 1].Y);

                            double distance = PlanimetryAlgorithms.DistanceToSegment(coordinate, s);
                            if (distance < Map.SelectionPointRadius)
                            {
                                selectedFeature = feature;
                                return;
                            }
                        }
                }
            }

            IEnumerable<Feature> polygons;
            if (_polygonsIndex != null)
            {
                List<Feature> pg = new List<Feature>();
                _polygonsIndex.QueryObjectsContainingPoint(coordinate, pg);
                polygons = pg;
            }
            else
                polygons = _polygons;

            // polygons
            foreach (Feature feature in polygons.Reverse())
            {
                if (!feature.Visible) continue;
                if (feature.Polygon.ContainsPoint(coordinate))
                {
                    selectedFeature = feature;
                    return;
                }
            }
        }

        #endregion

        #region SelectObject for GetFeatureInfo with pictures

        /// <summary>
        /// Selects a feature of this layer.
        /// </summary>
        /// <param name="coordinate">The coords</param>
        /// <param name="width">A width of a picture</param>
        /// <param name="height">A height of a picture</param>
        /// <param name="contentAlignment">A content alignment of a picture</param>
        /// <param name="selectedFeature">A selected feature</param>
        public void SelectObject(ICoordinate coordinate, double width, double height, ContentAlignment contentAlignment, out Feature selectedFeature)
        {
            selectedFeature = null;
            List<Feature> ps = new List<Feature>();
            BoundingRectangle box;
            ICoordinate min;
            ICoordinate max;
            #region CalcBbox
            
            switch (contentAlignment)
            {
                case ContentAlignment.TopCenter:
                 min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width/2, coordinate.Y);
                 max = PlanimetryEnvironment.NewCoordinate(coordinate.X + width / 2, coordinate.Y + height); break;
                case ContentAlignment.TopRight:
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X, coordinate.Y);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X + width, coordinate.Y + height); break;
                case ContentAlignment.TopLeft: 
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width, coordinate.Y);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X, coordinate.Y + height); break;
                case ContentAlignment.MiddleCenter:
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width / 2, coordinate.Y - height / 2);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X + width / 2, coordinate.Y + height / 2); break;
                case ContentAlignment.MiddleRight:
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X, coordinate.Y - height / 2);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X + width, coordinate.Y + height / 2); break;
                case ContentAlignment.MiddleLeft: 
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width, coordinate.Y - height / 2);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X, coordinate.Y + height / 2); break;
                case ContentAlignment.BottomCenter:
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width / 2, coordinate.Y - height);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X + width / 2, coordinate.Y); break;
                case ContentAlignment.BottomRight:
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X, coordinate.Y - height); 
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X + width, coordinate.Y); break;
                case ContentAlignment.BottomLeft: 
                    min =    PlanimetryEnvironment.NewCoordinate(coordinate.X - width, coordinate.Y - width);
                    max = PlanimetryEnvironment.NewCoordinate( coordinate.X, coordinate.Y); break;
                default: throw new NotSupportedException();
            }

            if (!ReferenceEquals(Map.OnTheFlyTransform, null))
            {
               
                IMathTransform inverseTransform = Map.OnTheFlyTransform.Inverse();


                min = PlanimetryEnvironment.NewCoordinate(inverseTransform.Transform(min.Values()));
                max =PlanimetryEnvironment.NewCoordinate( inverseTransform.Transform(max.Values()));
            }
            #endregion
            box = new BoundingRectangle(min,max);
            if (_pointsIndex != null)
            {
                _pointsIndex.QueryObjectsInRectangle(box, ps);
            }
            else
            {
                foreach (Feature f in _points)
                {
                    if (box.ContainsPoint(f.Point.Coordinate))
                    {
                        selectedFeature = f;
                        return;
                    }
                }

                foreach (Feature f in _multiPoints)
                {
                    foreach (var point in f.MultiPoint.Points)
                        if (box.ContainsPoint(point))
                        {
                            selectedFeature = f;
                            return;
                        }
                }
            }
        }

        #endregion

        #region Loading features from source

        private void internalLoadFeatures(BoundingRectangle viewbox, bool useBounds)
        {
            // Retrieves an object from the data source
            if (DataSourceNeeded != null)
            {
                FeatureDataSourceEventArgs args = new FeatureDataSourceEventArgs();
                DataSourceNeeded(this, args);
                if (args.Provider != null)
                {
                    try
                    {
                        RemoveAllFeatures();

                        if (useBounds && !viewbox.IsEmpty())
                            args.Provider.QueryFeatures(this, viewbox);
                        else
                            args.Provider.QueryFeatures(this);
                    }
                    finally
                    {
                        if (DataSourceReadyToRelease != null)
                            DataSourceReadyToRelease(this, args);
                    }
                }
            }
        }

        /// <summary>
        /// Load features from the data source.
        /// </summary>
        public void LoadFeatures()
        {
            internalLoadFeatures(new BoundingRectangle(), false);
        }

        /// <summary>
        /// Load features from the data source.
        /// </summary>
        /// <param name="viewBox">A bounding rectangle defining an area which is filled with features</param>
        public void LoadFeatures(BoundingRectangle viewBox)
        {
            internalLoadFeatures(viewBox, true);
        }

        #endregion

        #region ISpatialDataProvider Members

        /// <summary>
        /// Raises when the feature is fetched.
        /// </summary>
        public event EventHandler<FeatureOperationEventArgs> FeatureFetched;

        /// <summary>
        /// Adds the features of this layer to the receiver.
        /// <para>
        /// Only features that exist in the Features 
        /// collection were added. Query to layer data source 
        /// does not executes, the event DataSourceNeeded does not arise.
        /// </para>
        /// </summary>
        /// <param name="fr">An object that receives features</param> 
        /// <rereturns>A number of retrieved features</rereturns>
        public int QueryFeatures(IFeatureReceiver fr)
        {
            
            if (fr == this)
                throw new ArgumentException("Layer cannot be a data provider for himself", "fr");

            Querying = true;
            try
            {
                return addFeaturesToReceiver(fr, Features);
            }
            finally
            {
                Querying = false;
            }
        }

        /// <summary>
        /// Adds the features of this layer to the receiver.
        /// <para>
        /// Only features that exist in the Features 
        /// collection were added. Query to layer data source 
        /// does not executes, the event DataSourceNeeded does not arise.
        /// </para>
        /// </summary>
        /// <param name="fr">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        /// <rereturns>A number of retrieved features</rereturns>
        public int QueryFeatures(IFeatureReceiver fr, BoundingRectangle bounds)
        {
            if (fr == this)
                throw new ArgumentException("Layer cannot be a data provider for himself", "fr");

            Querying = true;

            try
            {
                List<Feature> features = new List<Feature>();
                int result = 0;

                SelectPoints(bounds, features);
                result += addFeaturesToReceiver(fr, features);
                features.Clear();

                SelectPolylines(bounds, features);
                result += addFeaturesToReceiver(fr, features);
                features.Clear();

                SelectPolygons(bounds, features);
                result += addFeaturesToReceiver(fr, features);
                features.Clear();

                return result;
            }
            finally 
            {
                Querying = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the statistical info of layer.
    /// </summary>
    public struct LayerStatistics
    {
        private int _pointCount;
        private int _polylineCount;
        private int _polygonCount;
        private double _avgPolylinesBoxesSquare;
        private double _avgPolygonsBoxesSquare;

        /// <summary>
        /// Gets a number of point features. 
        /// </summary>
        public int PointCount
        {
            get { return _pointCount; }
        }

        /// <summary>
        /// Gets a number of linear features. 
        /// </summary>
        public int PolylineCount
        {
            get { return _polylineCount; }
        }

        /// <summary>
        /// Gets a number of polyong features. 
        /// </summary>
        public int PolygonCount
        {
            get { return _polygonCount; }
        }

        /// <summary>
        /// Gets an average square of bounding rectangles of linear features.
        /// </summary>
        public double AvgPolylinesBoxesSquare
        {
            get { return _avgPolylinesBoxesSquare; }
        }

        /// <summary>
        /// Gets an average square of bounding rectangles of polygon features.
        /// </summary>
        public double AvgPolygonsBoxesSquare
        {
            get { return _avgPolygonsBoxesSquare; }
        }

        /// <summary>
        /// Calculates statistical info for specified layer.
        /// </summary>
        /// <param name="l">A layer to calculate statistics</param>
        public void FillFromLayer(FeatureLayer l)
        {
            _pointCount = l.Points.Count + l.MultiPoints.Count;
            _polylineCount = l.Polylines.Count;
            _polygonCount = l.Polygons.Count;

            _avgPolylinesBoxesSquare = 0;
            foreach (Feature feature in l.Polylines)
                if(!feature.BoundingRectangle.IsEmpty())
                    _avgPolylinesBoxesSquare +=
                        (feature.BoundingRectangle.Width) *
                        (feature.BoundingRectangle.Height) / _polylineCount;

            _avgPolygonsBoxesSquare = 0;
            foreach (Feature feature in l.Polygons)
                if (!feature.BoundingRectangle.IsEmpty())
                    _avgPolygonsBoxesSquare +=
                        (feature.BoundingRectangle.Width) *
                        (feature.BoundingRectangle.Height) / _polygonCount;
        }
    }

    /// <summary>
    /// Instances of this class describes 
    /// a legend elements for the layer.
    /// </summary>
    public class LayerLegendSettings : ICloneable
    {
        private string _pointSampleTitle = string.Empty;
        private string _polylineSampleTitle = string.Empty;
        private string _polygonSampleTitle = string.Empty;

        bool _displayPointSample = true;
        bool _displayPolylineSample = true;
        bool _displayPolygonSample = true;

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns>  
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets a title of point feature sample.
        /// </summary>
        public string PointSampleTitle
        {
            get { return _pointSampleTitle; }
            set { _pointSampleTitle = value; }
        }

        /// <summary>
        /// Gets or sets a title of linear feature sample.
        /// </summary>
        public string PolylineSampleTitle
        {
            get { return _polylineSampleTitle; }
            set { _polylineSampleTitle = value; }
        }

        /// <summary>
        /// Gets or sets a title of polygon feature sample.
        /// </summary>
        public string PolygonSampleTitle
        {
            get { return _polygonSampleTitle; }
            set { _polygonSampleTitle = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// a point sample should be displayed on a legend.
        /// </summary>
        public bool DisplayPointSample
        {
            get { return _displayPointSample; }
            set { _displayPointSample = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// a line sample should be displayed on a legend.
        /// </summary>
        public bool DisplayPolylineSample
        {
            get { return _displayPolylineSample; }
            set { _displayPolylineSample = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether 
        /// a polygon sample should be displayed on a legend.
        /// </summary>
        public bool DisplayPolygonSample
        {
            get { return _displayPolygonSample; }
            set { _displayPolygonSample = value; }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.LayerLegendSettings.
        /// </summary>
        public LayerLegendSettings(bool displayPointSample,
                                   bool displayPolylineSample,
                                   bool displayPolygonSample,
                                   string pointSampleTitle,
                                   string polylineSampleTitle,
                                   string polygonSampleTitle)
        {
            _displayPointSample = displayPointSample;
            _displayPolylineSample = displayPolylineSample;
            _displayPolygonSample = displayPolygonSample;
            _pointSampleTitle = pointSampleTitle;
            _polylineSampleTitle = polylineSampleTitle;
            _polygonSampleTitle = polygonSampleTitle;
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.LayerLegendSettings.
        /// </summary>
        public LayerLegendSettings(LayerLegendSettings proto)
        {
            _displayPointSample = proto.DisplayPointSample;
            _displayPolylineSample = proto.DisplayPolylineSample;
            _displayPolygonSample = proto.DisplayPolygonSample;
            _pointSampleTitle = proto.PointSampleTitle;
            _polylineSampleTitle = proto.PolylineSampleTitle;
            _polygonSampleTitle = proto.PolygonSampleTitle;
        }
    }

    /// <summary>
    /// Enumerates types of features.
    /// </summary>
    public enum FeatureType {
        /// <summary>
        /// Point type of feature.
        /// </summary>
        Point = 1,

        /// <summary>
        /// Polyline type of feature.
        /// </summary>
        Polyline = 2,

        /// <summary>
        /// Polygon type of feature.
        /// </summary>
        Polygon = 3,

        /// <summary>
        /// MultiPoint type of feature.
        /// </summary>
        MultiPoint = 4
    }

    /// <summary>
    /// Represents a feature.
    /// </summary>
    [Serializable]
    public class Feature : IIndexable
    {
        private string _uniqKey = string.Empty;

        private FeatureType _featureType = FeatureType.Point;
        private string _title = string.Empty;
        private object _tag = null;
        private bool _selected = false;
        private bool _visible = true;
        private List<Double> _polylinePartLengths = new List<double>();

        [NonSerialized]
        private FeatureLayer _layer = null;

        private PointD _point;
        private MultiPoint _multiPoint;
        private Polyline _polyline;
        private Polygon _polygon;

        private PointStyle _pointStyle = null;
        private PolylineStyle _polylineStyle = null;
        private PolygonStyle _polygonStyle = null;
        private TitleStyle _titleStyle = null;

        private BoundingRectangle _boundingRectangle = new BoundingRectangle();

        private object[] _attributes = null;

        internal List<Double> PolylinePartLengths
        {
            get { return _polylinePartLengths; }
        }

        internal void SetLayer(FeatureLayer layer)
        {
            _layer = layer;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance</returns> 
        public object Clone()
        {
            Feature result = new Feature(this.FeatureType);

            // copy the styles of output, if
            if(this.PointStyle != null)
                result.PointStyle = (PointStyle)this.PointStyle.Clone();

            if(this.PolylineStyle != null)
                result.PolylineStyle = (PolylineStyle)this.PolylineStyle.Clone();

            if(this.PolygonStyle!= null)
                result.PolygonStyle = (PolygonStyle)this.PolygonStyle.Clone();

            if(this.TitleStyle!= null)
                result.TitleStyle = (TitleStyle)this.TitleStyle.Clone();

            // copy geometric data
            switch(result.FeatureType)
            {
                case FeatureType.Point:
                    result.Point = (PointD)this.Point.Clone();
                    break;
                case FeatureType.Polyline:
                    result.Polyline = (Polyline)this.Polyline.Clone();
                    break;
                case FeatureType.Polygon:
                    result.Polygon = (Polygon)this.Polygon.Clone();
                    break;
                case FeatureType.MultiPoint:
                    result.MultiPoint = (MultiPoint)this.MultiPoint.Clone();
                    break;
            }

            // copy attributes
            if (this.Attributes != null)
            {
                result.Attributes = new object[this.Attributes.Length];
                this.Attributes.CopyTo(result.Attributes, 0);

                // If some of the attributes of support ICloneable, clone them
                for (int i = 0; i < result.Attributes.Length; i++)
                {
                    ICloneable o = result.Attributes[i] as ICloneable;
                    if (o != null)
                        result.Attributes[i] = o.Clone();
                }
            }

            //copy the values ​​of the other properties
            result.Selected = this.Selected;
            result.UniqKey = this.UniqKey;
            result.Title = this.Title;

            //These values ​​are not needed
            result._layer = null;
            result.Tag = null;

            //if (this.Tag != null)
            //    if (this.Tag is ICloneable)
            //        result.Tag = ((ICloneable)this.Tag).Clone();

            return (object)result;
        }

        /// <summary>
        /// Gets a layer having this feature.
        /// </summary>
        public FeatureLayer Layer
        {
            get { return _layer; }
        }

        /// <summary>
        /// Gets or sets a style of rendering point.
        /// This value overrides layer settings.
        /// </summary>
        public PointStyle PointStyle
        {
            get { return _pointStyle; }
            set 
            {
                if(this.FeatureType != FeatureType.MultiPoint &&
                   this.FeatureType != FeatureType.Point)
                    checkFeatureType(FeatureType.Point);

                _pointStyle = value; 
            }
        }

        /// <summary>
        /// Gets or sets a style of rendering polyline.
        /// This value overrides layer settings.
        /// </summary>
        public PolylineStyle PolylineStyle
        {
            get { return _polylineStyle; }
            set 
            {
                checkFeatureType(FeatureType.Polyline);
                _polylineStyle = value; 
            }
        }

        /// <summary>
        /// Gets or sets a style of rendering polygon.
        /// This value overrides layer settings.
        /// </summary>
        public PolygonStyle PolygonStyle
        {
            get { return _polygonStyle; }
            set 
            {
                checkFeatureType(FeatureType.Polygon);
                _polygonStyle = value; 
            }
        }

        /// <summary>
        /// Gets or sets a style of rendering title.
        /// This value overrides layer settings.
        /// </summary>
        public TitleStyle TitleStyle
        {
            get { return _titleStyle; }
            set { _titleStyle = value; }
        }

        /// <summary>
        /// Gets or sets attribute values of this feature.
        /// </summary>
        public object[] Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        /// <summary>
        /// Gets or sets an attribute value by name.
        /// </summary>
        public object this[string s]
        {
            get
            {
                return _attributes[attributeIndexByName(s)];
            }
            set
            {
                _attributes[attributeIndexByName(s)] = value;
            }
        }

        /// <summary>
        /// Gets a bounding rectangle of this feature.
        /// </summary>
        public BoundingRectangle BoundingRectangle
        {
            get { return _boundingRectangle; }
        }

        private int attributeIndexByName(string attributeName)
        {
            if (this.Layer == null)
                throw new InvalidOperationException("Unable to access to attributes by name. Feature is not added to the layer.");

            int index = this.Layer.FeatureAttributeNames.IndexOf(attributeName);
            if (index == -1)
                throw new ArgumentException("Attribute \"" + attributeName + "\" not found.", "s");

            return index;
        }

        private void checkFeatureType(FeatureType featureType)
        { 
            if(_featureType != featureType)
                throw new InvalidOperationException("Invalid operation for this feature type");
        }

        private void checkGeometryChangesPossibility()
        {
            if (Layer != null)
                if (Layer.Map != null)
                    if (Layer.Map.State == MapState.Rendering)
                        throw new InvalidOperationException("Unable to change geometry during map drawing");
        }

        /// <summary>
        /// Gets or sets an uniq key.
        /// This value may use in custom purposes.
        /// </summary>
        public string UniqKey
        {
            get { return _uniqKey; }
            set { _uniqKey = value; }
        }

        /// <summary>
        /// Gets or sets a title.
        /// <para>
        /// Titles renders as a text labels on map.
        /// </para>
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        /// Gets or sets an object which may 
        /// used in custom purposes.
        /// </summary>
        public object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets a type of feature.
        /// <para>
        /// Type of feature defined by associated geometry.
        /// Instance of feature can not change its type.
        /// </para>
        /// </summary>
        public FeatureType FeatureType
        {
            get { return _featureType; }
        }

        /// <summary>
        /// Gets or sets a selected state.
        /// <para>
        /// If it is true, a feature will rendered with the selection highlight.
        /// </para>
        /// </summary>
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; }
        }

        /// <summary>
        /// Get or sets visiable state.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <summary>
        /// Gets or sets a point geometry.
        /// </summary>
        public PointD Point
        {
            get { checkFeatureType(FeatureType.Point); return _point; }
            set 
            {
                checkGeometryChangesPossibility();                
                checkFeatureType(FeatureType.Point);

                if (_point != null)
                    _point.UnLock();

                _point = value;
                _boundingRectangle = new BoundingRectangle(value.X, value.Y, value.X, value.Y);

                _point.Lock();
            }
        }

        /// <summary>
        /// Gets or sets a multipoint geometry.
        /// </summary>
        public MultiPoint MultiPoint
        {
            get { checkFeatureType(FeatureType.MultiPoint); return _multiPoint; }
            set
            {
                checkGeometryChangesPossibility();
                checkFeatureType(FeatureType.MultiPoint);

                if (_multiPoint != null)
                    _multiPoint.UnLock();

                _multiPoint = value;
                _boundingRectangle = _multiPoint.GetBoundingRectangle();

                _multiPoint.Lock();
            }
        }

        /// <summary>
        /// Gets or sets a polyline geometry.
        /// </summary>
        public Polyline Polyline
        {
            get { checkFeatureType(FeatureType.Polyline); return _polyline; }
            set 
            {
                checkGeometryChangesPossibility();
                checkFeatureType(FeatureType.Polyline);

                if (_polyline != null)
                    _polyline.UnLock();

                _polyline = value;
                _boundingRectangle = value.GetBoundingRectangle();

                _polyline.Lock();

                PolylinePartLengths.Clear();
                foreach (LinePath path in _polyline.Paths)
                    PolylinePartLengths.Add(path.Length());
            }
        }

        /// <summary>
        /// Gets or sets a polygon geometry.
        /// </summary>
        public Polygon Polygon
        {
            get { checkFeatureType(FeatureType.Polygon); return _polygon; }
            set 
            {
                checkGeometryChangesPossibility();
                checkFeatureType(FeatureType.Polygon);

                if (_polygon != null)
                    _polygon.UnLock();

                _polygon = value;
                _boundingRectangle = value.GetBoundingRectangle();

                _polygon.Lock();
            }
        }

        /// <summary>
        /// Gets or sets a geometry.
        /// </summary>
        public IGeometry Geometry
        {
            get 
            {
                switch (this.FeatureType)
                { 
                    case FeatureType.Point:
                        return Point;
                    case FeatureType.Polyline:
                        return Polyline;
                    case FeatureType.Polygon:
                        return Polygon;
                    case FeatureType.MultiPoint:
                        return MultiPoint;
                }

                throw new InvalidOperationException("Internal error");
            }
            set
            {
                if (value is PointD)
                {
                    Point = (PointD)value;
                    return;
                }
                else if (value is Polyline)
                {
                    Polyline = (Polyline)value;
                    return;
                }
                else if (value is Polygon)
                {
                    Polygon = (Polygon)value;
                    return;
                }
                else if (value is MultiPoint)
                {
                    MultiPoint = (MultiPoint)value;
                    return;
                }

                throw new NotSupportedException("Geometry \"" + value.GetType().FullName + "\" is not supported");
            }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Feature.
        /// </summary>
        /// <param name="featureType">A type of feature</param>
        public Feature(FeatureType featureType) :
            this(featureType, string.Empty)
        {

        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Feature.
        /// </summary>
        /// <param name="featureType">A type of feature</param>
        /// <param name="uniqKey">Uniq key value</param>
        public Feature(FeatureType featureType, string uniqKey)
        {
            _featureType = featureType;
            _uniqKey = uniqKey;

            switch (_featureType)
            { 
                case FeatureType.Point:
                    _point = new PointD();
                    break;
                case FeatureType.Polyline: 
                    _polyline = new Polyline();
                    break;
                case FeatureType.Polygon:
                    _polygon = new Polygon();
                    break;
                case FeatureType.MultiPoint:
                    _multiPoint = new MultiPoint();
                    break;
            }
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Feature.
        /// </summary>
        /// <param name="geometry">Geometry object</param>
        public Feature(IGeometry geometry) :
            this(geometry, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Feature.
        /// </summary>
        /// <param name="geometry">Geometry object</param>
        /// <param name="uniqKey">Uniq key value</param>
        public Feature(IGeometry geometry, string uniqKey)
            : this (geometry, uniqKey, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of MapAround.Mapping.Feature.
        /// </summary>
        /// <param name="geometry">Geometry object</param>
        /// <param name="uniqKey">Uniq key value</param>
        /// <param name="attributes">An array containing attribute values</param>
        public Feature(IGeometry geometry, string uniqKey, object[] attributes)
        {
            _uniqKey = uniqKey;

            if (geometry is PointD)
            {
                _featureType = FeatureType.Point;
                this.Point = (PointD)geometry;
            }
            else if (geometry is Polyline)
            {
                _featureType = FeatureType.Polyline;
                this.Polyline = (Polyline)geometry;
            }
            else if (geometry is Polygon)
            {
                _featureType = FeatureType.Polygon;
                this.Polygon = (Polygon)geometry;
            }
            else if (geometry is MultiPoint)
            {
                _featureType = FeatureType.MultiPoint;
                this.MultiPoint = (MultiPoint)geometry;
            }
            else
                throw new NotSupportedException(string.Format("Feature type \"{0}\" is not supported by MapAround.Mapping.Feature.", geometry.GetType().FullName));

            _attributes = attributes;
        }
    }
}
