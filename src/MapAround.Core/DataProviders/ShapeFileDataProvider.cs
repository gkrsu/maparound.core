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
** File: ShapeFileDataProvider.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: ESRI Shape-file data provider
**
=============================================================================*/

namespace MapAround.DataProviders
{
    using System;
    using System.Collections.Generic;

    using MapAround.IO;
    using MapAround.Geometry;
    using MapAround.Mapping;
    using MapAround.Indexing;
    using MapAround.Caching;

    /// <summary>
    /// Instances of MapAround.DataProviders.ShapeFileSpatialDataProvider
    /// provides access to the data stored in ESRI shape-file format.
    /// </summary>
    public class ShapeFileSpatialDataProvider : SpatialDataProviderBase
    {
        private string _fileName = string.Empty;
        private bool _processAttributes = false;
        private System.Text.Encoding _attributesEncoding = System.Text.Encoding.ASCII;
        private IFeatureCollectionCacheAccessor _cacheAccessor = null;

        /// <summary>
        /// Cache accessor object.
        /// <para>
        /// If a cache accessor object was assigned, the first data request 
        /// reads the entire file and puts the data into the cache. 
        /// Subsequent inquiries lead to attempts to find the right data 
        /// in the cache and only if there are none reads a file again.
        /// </para>
        /// <remarks>
        /// Layers use an alias property value as an access key. 
        /// Make sure that the value of this property is not null 
        /// and not an empty string.
        /// </remarks>
        /// </summary>
        public IFeatureCollectionCacheAccessor CacheAccessor
        {
            get { return _cacheAccessor; }
            set { _cacheAccessor = value; }
        }

        /// <summary>
        /// Gets or sets an encoding of attributes.
        /// </summary>
        public System.Text.Encoding AttributesEncoding
        {
            get { return _attributesEncoding; }
            set { _attributesEncoding = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the attributes will be processed.
        /// </summary>
        public bool ProcessAttributes
        {
            get { return _processAttributes; }
            set { _processAttributes = value; }
        }

        /// <summary>
        /// Gets or sets a name of the shape-file.
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        /// <summary>
        /// Raises when a new feature is fetched.
        /// </summary>
        public event EventHandler<FeatureOperationEventArgs> FeatureFetched;

        private void buildAndSaveIndex(MapAround.Mapping.FeatureType featureType, 
                               BoundingRectangle b,
                               IndexSettings settings, 
                               IEnumerable<Feature> features)
        {
            ISpatialIndex index = null;

            if (b.IsEmpty())
                b = new BoundingRectangle(0, 0, 0, 0);

            if (settings.IndexType == "QuadTree")
                index = new QuadTree(b);
            if (index == null)
                index = new KDTree(b);

            index.MaxDepth = settings.MaxDepth;
            index.BoxSquareThreshold = settings.BoxSquareThreshold;
            index.MinObjectCount = settings.MinFeatureCount;

            index.Build(features);

            _cacheAccessor.SaveFeaturesIndex(index, featureType);
        }

        private int internalQueryFeatures(IFeatureReceiver fr, BoundingRectangle bounds, bool checkBounds)
        {
            if (_cacheAccessor != null && !string.IsNullOrEmpty(fr.Alias))
            { 
                _cacheAccessor.Key = fr.Alias;
                if (_cacheAccessor.ExistsInCache)
                    return FillFromCache(_cacheAccessor, fr, bounds, _processAttributes);
                else
                    // If the object is not found in the cache, you must remove all objects from a file, to put them in the cache
                    checkBounds = false;
            }

            ShapeFile shapeFile = new ShapeFile();
            shapeFile.AttributesEncoding = _attributesEncoding;
            shapeFile.Read(_fileName, checkBounds ? bounds : null);

            if (ProcessAttributes)
            {
                fr.FeatureAttributeNames.Clear();
                foreach (string s in shapeFile.AttributeNames)
                    fr.FeatureAttributeNames.Add(s);
            }

            int result = 0;
            string layerHashStr = fr.GetHashCode().ToString();

            List<Feature> points = new List<Feature>();
            List<Feature> multiPoints = new List<Feature>();
            List<Feature> polylines = new List<Feature>();
            List<Feature> polygons = new List<Feature>();

            foreach (ShapeFileRecord record in shapeFile.Records)
            {
                if (!checkBounds ||
                    (record.MaxX >= bounds.MinX && record.MaxY >= bounds.MinY &&
                     record.MinX <= bounds.MaxX && record.MinY <= bounds.MaxY))
                {
                    Feature newFeature = null;
                    IGeometry geometry = geometryFromShapeRecord(record);
                    if (geometry != null)
                    {
                        newFeature = new Feature(geometry);
                        newFeature.UniqKey = layerHashStr + record.RecordNumber.ToString();
                        if (ProcessAttributes && record.Attributes != null)
                            newFeature.Attributes = record.Attributes.ItemArray;

                        if (processFeature(newFeature, fr, points, multiPoints, polylines, polygons))
                            result++;
                    }
                }
            }

            // If the objects are not extracted from the cache may be added to the cache.
            // This should be done only if the retrieval of all objects (checkBounds == false)
            if (_cacheAccessor != null && !string.IsNullOrEmpty(fr.Alias) &&
                checkBounds == false)
            {
                addFeaturesToCache(fr, points, multiPoints, polylines, polygons);
            }

            return result;
        }

        private IGeometry geometryFromShapeRecord(ShapeFileRecord record)
        {
            switch (record.ShapeType)
            {
                // point
                case 1:
                    return new PointD(record.Points[0].X, record.Points[0].Y);
                // polyline
                case 3:
                    Polyline polyline = new Polyline();
                    for (int i = 0; i < record.Parts.Count; i++)
                    {
                        LinePath path = new LinePath();
                        int j;
                        for (j = record.Parts[i]; j < (i == record.Parts.Count - 1 ? record.Points.Count : record.Parts[i + 1]); j++)
                            path.Vertices.Add(PlanimetryEnvironment.NewCoordinate(record.Points[j].X, record.Points[j].Y));

                        polyline.Paths.Add(path);
                    }
                    return polyline;
                // ground
                case 5:
                    Polygon p = new Polygon();
                    for (int i = 0; i < record.Parts.Count; i++)
                    {
                        Contour contour = new Contour();
                        int j;
                        for (j = record.Parts[i]; j < (i == record.Parts.Count - 1 ? record.Points.Count : record.Parts[i + 1]); j++)
                            contour.Vertices.Add(PlanimetryEnvironment.NewCoordinate(record.Points[j].X, record.Points[j].Y));

                        contour.Vertices.RemoveAt(contour.Vertices.Count - 1);
                        p.Contours.Add(contour);
                    }
                    if (p.CoordinateCount > 0)
                        return p;
                    else
                        return null;
                // set of points
                case 8:
                    MultiPoint mp = new MultiPoint();
                    for (int i = 0; i < record.Points.Count; i++)
                        mp.Points.Add(PlanimetryEnvironment.NewCoordinate(record.Points[i].X, record.Points[i].Y));
                    return mp;
            }

            return null;
        }

        private bool processFeature(Feature feature, IFeatureReceiver fr, List<Feature> points,
            List<Feature> multiPoints,
            List<Feature> polylines,
            List<Feature> polygons)
        {
            if (feature == null)
                return false;

            bool isAccepted = true;
            if (FeatureFetched != null)
            {
                FeatureOperationEventArgs foea = new FeatureOperationEventArgs(feature);
                FeatureFetched(this, foea);
                isAccepted = foea.IsAccepted;
            }

            if (!isAccepted)
                return false;

            fr.AddFeature(feature);
            switch (feature.FeatureType)
            {
                case FeatureType.Point: points.Add(feature); break;
                case FeatureType.MultiPoint: multiPoints.Add(feature); break;
                case FeatureType.Polyline: polylines.Add(feature); break;
                case FeatureType.Polygon: polygons.Add(feature); break;
            }

            return true;
        }

        private void addFeaturesToCache(IFeatureReceiver fr, 
            List<Feature> points, 
            List<Feature> multiPoints,
            List<Feature> polylines,
            List<Feature> polygons)
        {
            _cacheAccessor.Key = fr.Alias;
            if (!_cacheAccessor.ExistsInCache)
            {
                BoundingRectangle b = new BoundingRectangle();
                List<Feature> pts = new List<Feature>();
                foreach (Feature feature in points)
                {
                    b.Join(feature.BoundingRectangle);
                    pts.Add(feature);
                }
                foreach (Feature feature in multiPoints)
                {
                    b.Join(feature.BoundingRectangle);
                    pts.Add(feature);
                }

                buildAndSaveIndex(MapAround.Mapping.FeatureType.Point, b, fr.DefaultPointsIndexSettings, pts);

                b = new BoundingRectangle();
                foreach (Feature feature in polylines)
                    b.Join(feature.BoundingRectangle);

                buildAndSaveIndex(MapAround.Mapping.FeatureType.Polyline, b, fr.DefaultPolylinesIndexSettings, polylines);

                b = new BoundingRectangle();
                foreach (Feature feature in polygons)
                    b.Join(feature.BoundingRectangle);

                buildAndSaveIndex(MapAround.Mapping.FeatureType.Polygon, b, fr.DefaultPolygonsIndexSettings, polygons);

                if (_processAttributes)
                    _cacheAccessor.SaveAttributeNames(fr.FeatureAttributeNames);
            }
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver, BoundingRectangle bounds)
        {
            return internalQueryFeatures(receiver, bounds, true);
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver)
        {
            return internalQueryFeatures(receiver, new BoundingRectangle(), false);
        }
    }

    /// <summary>
    /// Shape-file data provider holder.
    /// </summary>
    public class ShapeFileSpatialDataProviderHolder : SpatialDataProviderHolderBase
    {
        private static string[] _parameterNames = { "file_name", "process_attributes", "attributes_encoding" };
        private Dictionary<string, string> _parameters = null;


        /// <summary>
        /// Sets the parameter values.
        /// </summary>
        /// <param name="parameters">Parameter values</param>
        public override void SetParameters(Dictionary<string, string> parameters)
        {
            if (!parameters.ContainsKey("file_name"))
                throw new ArgumentException("Missing parameter \"file_name\".");
            _parameters = parameters;
        }

        /// <summary>
        /// Gets a list containing the names of parameters.
        /// </summary>
        /// <returns>List containing the names of parameters</returns>
        public override string[] GetParameterNames()
        {
            return _parameterNames;
        }

        private ISpatialDataProvider createProviderInstance()
        {
            ShapeFileSpatialDataProvider provider = new ShapeFileSpatialDataProvider();
            if (_parameters == null)
                throw new InvalidOperationException("Parameter values not set.");

            provider.FileName = _parameters["file_name"];
            if (_parameters.ContainsKey("process_attributes"))
                provider.ProcessAttributes = _parameters["process_attributes"] == "1";
            if (_parameters.ContainsKey("attributes_encoding"))
                provider.AttributesEncoding = System.Text.Encoding.GetEncoding(_parameters["attributes_encoding"]);

            return provider;
        }

        /// <summary>
        /// Performs a finalization procedure for the spatial data provider.
        /// This implementation do nothing.
        /// </summary>
        /// <param name="provider">Spatial data provider instance</param>
        public override void ReleaseProviderIfNeeded(ISpatialDataProvider provider)
        {

        }

        /// <summary>
        /// Initializes a new instance of the MapAround.DataProviders.ShapeFileSpatialDataProviderHolder.
        /// </summary>
        public ShapeFileSpatialDataProviderHolder()
            : base("MapAround.DataProviders.ShapeFileSpatialDataProvider")
        {
            GetProviderMethod = createProviderInstance;
        }
    }
}