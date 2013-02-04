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
** File: SpatialDataProvider.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Interfaces and base classes for providing access to the vector spatial data
**
=============================================================================*/

namespace MapAround.DataProviders
{
    using System;
    using System.Globalization;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Data;
    using System.Data.Common;
    using System.Collections.ObjectModel;

    using MapAround.Geometry;
    using MapAround.Mapping;
    using MapAround.Serialization;
    using MapAround.Indexing;
    using MapAround.Caching;

    /// <summary>
    /// The MapAround.DataProviders namespace contains interfaces and classes 
    /// for providing an access to the spatial data.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Represents an object that provides an access 
    /// to the vector spatial data.
    /// </summary>
    public interface ISpatialDataProvider
    {
        /// <summary>
        /// Adds the features retrieved from data source to the receiver.
        /// </summary>
        /// <param name="fr">An object that receives features</param> 
        /// <rereturns>A number of retrieved features</rereturns>
        int QueryFeatures(IFeatureReceiver fr);

        /// <summary>
        /// Adds the features retrieved from data source to the receiver.
        /// <para>
        /// Notes to implementors: you don't need to perform exact intersection test, 
        /// it suffices to check the intersection of the bounding rectangles.
        /// </para>
        /// </summary>
        /// <param name="fr">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        /// <rereturns>A number of retrieved features</rereturns>
        int QueryFeatures(IFeatureReceiver fr, BoundingRectangle bounds);
    }

    /// <summary>
    /// Instances of this class contains a data passed to the feature operation event.
    /// </summary>
    public class FeatureOperationEventArgs : EventArgs
    {
        private Feature _feature;
        private bool _isAccepted = true;

        /// <summary>
        /// Gets or sets a value indicating whether an operation was accepted or not.
        /// </summary>
        public bool IsAccepted
        {
            get { return _isAccepted; }
            set { _isAccepted = value; }
        }

        /// <summary>
        /// Feature on wich the operation is performing.
        /// </summary>
        public Feature Feature
        {
            get { return _feature; }
        }

        /// <summary>
        /// Initializes a new instance of Maparound.DataProviders.FeatureOperationEventArgs.
        /// </summary>
        /// <param name="feature">Feature on wich the operation is performing</param>
        public FeatureOperationEventArgs(Feature feature)
        {
            _feature = feature;
        }
    }

    /// <summary>
    /// Maparound.DataProviders.SpatialDataProviderBase is the base class 
    /// for the spatial data providers.
    /// </summary>
    public abstract class SpatialDataProviderBase : ISpatialDataProvider
    {
        private static byte[] doubleBytes = new byte[8];
        private static byte[] intBytes = new byte[8];

        private static int readInt(Stream stream)
        {
            for (int i = 0; i < 4; i++)
            {
                int b = stream.ReadByte();
                if (b == -1)
                    throw new EndOfStreamException();
                intBytes[i] = (byte)b;
            }

            return BitConverter.ToInt32(intBytes, 0);
        }

        private static void writeInt(Stream stream, int value)
        {
            intBytes = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
                stream.WriteByte(intBytes[i]);
        }

        /// <summary>
        /// Construct a feature from its binary representation.
        /// </summary>
        /// <param name="bytes">Byte array that contains binary representation of feature geometry</param>
        protected Feature FeatureFromSpatialDataBytes(byte[] bytes)
        {
            Feature feature = null;

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                IGeometry geometry = BinaryGeometrySerializer.DeserializeGeometry(ms);
                feature = new Feature(geometry);
            }

            return feature;
        }

        /// <summary>
        /// Gets a binary representation of the feature geometry.
        /// </summary>
        /// <param name="feature">Feature</param>
        protected byte[] SpatialDataBytesFromFeature(Feature feature)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryGeometrySerializer.SerializeGeometry(ms, feature.Geometry);
                return ms.ToArray();
            }

            throw new NotSupportedException("Feature type \"" + feature.FeatureType.ToString() + "\" is not supported.");
        }

        /// <summary>
        /// Adds the features retrieved from data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        public abstract int QueryFeatures(IFeatureReceiver receiver);

        /// <summary>
        /// Adds the features retrieved from data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        public abstract int QueryFeatures(IFeatureReceiver receiver, BoundingRectangle bounds);

        /// <summary>
        /// Adds the features retrieved from cache to the receiver.
        /// </summary>
        /// <param name="processAttributes">A value indicating whether the attributes will be processed or not</param>
        /// <param name="cacheAccessor">Cache accessor instance</param>
        /// <param name="fr">An object that receives the retrieved features</param>
        /// <param name="bounds">Rectangle that defines a query region</param>
        /// <returns>Number of retrieved features</returns>
        public static int FillFromCache(IFeatureCollectionCacheAccessor cacheAccessor, IFeatureReceiver fr, BoundingRectangle bounds, bool processAttributes)
        {
            ISpatialIndex pointsIndex = cacheAccessor.RestoreFeaturesIndex(MapAround.Mapping.FeatureType.Point);
            ISpatialIndex polylinesIndex = cacheAccessor.RestoreFeaturesIndex(MapAround.Mapping.FeatureType.Polyline);
            ISpatialIndex polygonsIndex = cacheAccessor.RestoreFeaturesIndex(MapAround.Mapping.FeatureType.Polygon);

            BoundingRectangle b;
            if (!bounds.IsEmpty())
                b = bounds.GetBoundingRectangle();
            else
            {
                b = new BoundingRectangle();
                b.Join(pointsIndex.IndexedSpace);
                b.Join(polylinesIndex.IndexedSpace);
                b.Join(polygonsIndex.IndexedSpace);
            }

            List<Feature> points = new List<Feature>();
            pointsIndex.QueryObjectsInRectangle(bounds, points);

            List<Feature> polylines = new List<Feature>();
            polylinesIndex.QueryObjectsInRectangle(bounds, polylines);

            List<Feature> polygons = new List<Feature>();
            polygonsIndex.QueryObjectsInRectangle(bounds, polygons);

            points.ForEach(point => fr.AddFeature((Feature)point.Clone()));
            polylines.ForEach(polyline => fr.AddFeature((Feature)polyline.Clone()));
            polygons.ForEach(polygon => fr.AddFeature((Feature)polygon.Clone()));

            if (processAttributes)
            {
                fr.FeatureAttributeNames.Clear();
                IList<string> attributeNames = cacheAccessor.RestoreAttributeNames();
                foreach (string s in attributeNames)
                    fr.FeatureAttributeNames.Add(s);
            }

            return points.Count + polylines.Count + polygons.Count;
        }
    }
    
    /// <summary>
    /// Instances of Maparound.DataProviders.IdNeededEventArgs contains data
    /// for the IdNeeded event.
    /// </summary>
    public class IdNeededEventArgs : EventArgs
    {
        private string _id;
        private Feature _feature;

        /// <summary>
        /// The feature for which new id needed.
        /// </summary>
        public Feature Feature
        {
            get { return _feature; }
        }

        /// <summary>
        /// Gets or sets Id value.
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Initializes an instance of Maparound.DataProviders.IdNeededEventArgs.
        /// </summary>
        /// <param name="feature">A feature for which the event handler will be called</param>
        public IdNeededEventArgs(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException("feature");

            _feature = feature;
        }
    }

    /// <summary>
    /// Maparound.DataProviders.SqlSpatialDataProvider is the base class
    /// for the spatial data providers from SQL servers which hasn't native spatial 
    /// data types and functions.
    /// </summary>
    public abstract class SqlSpatialDataProvider : SpatialDataProviderBase
    {
        private string _dataTableName = string.Empty;
        private string _uniqKeyField = string.Empty;
        private string _featureTypeField = string.Empty;
        private string _titleField = string.Empty;
        private string _spatialDataField = string.Empty;
        private string _minXField = string.Empty;
        private string _maxXField = string.Empty;
        private string _minYField = string.Empty;
        private string _maxYField = string.Empty;

        /// <summary>
        /// Raises when a new identity value is needed.
        /// </summary>
        public event EventHandler<IdNeededEventArgs> IdValueNeeded = null;

        /// <summary>
        /// Gets or sets the database table name.
        /// </summary>
        public string DataTableName
        {
            get { return _dataTableName; }
            set { _dataTableName = value; }
        }

        /// <summary>
        /// Gets or sets an identity field name.
        /// </summary>
        public string UniqKeyField
        {
            get { return _uniqKeyField; }
            set { _uniqKeyField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a feature type.
        /// </summary>
        public string FeatureTypeField
        {
            get { return _featureTypeField; }
            set { _featureTypeField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a feature title.
        /// </summary>
        public string TitleField
        {
            get { return _titleField; }
            set { _titleField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a binary spatial data.
        /// </summary>
        public string SpatialDataField
        {
            get { return _spatialDataField; }
            set { _spatialDataField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a minimum X 
        /// coordinate of the feature geometry.
        /// </summary>
        public string MinXField
        {
            get { return _minXField; }
            set { _minXField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a minimum Y 
        /// coordinate of the feature geometry.
        /// </summary>
        public string MinYField
        {
            get { return _minYField; }
            set { _minYField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a maximum X 
        /// coordinate of the feature geometry.
        /// </summary>
        public string MaxXField
        {
            get { return _maxXField; }
            set { _maxXField = value; }
        }

        /// <summary>
        /// Gets or sets a name of field that stores a maximum Y 
        /// coordinate of the feature geometry.
        /// </summary>
        public string MaxYField
        {
            get { return _maxYField; }
            set { _maxYField = value; }
        }

        /// <summary>
        /// Raises when a new feature is fetched.
        /// </summary>
        public event EventHandler<FeatureOperationEventArgs> FeatureFetched;

        private string getSelectString()
        {
            return "select * from " + DataTableName;
        }

        private string getBoundsConditionString(BoundingRectangle bounds)
        {
            CultureInfo ci = new CultureInfo(CultureInfo.CurrentCulture.LCID);
            ci.NumberFormat.NumberDecimalSeparator = ".";

            return MinXField + " <= " + bounds.MaxX.ToString(ci) +
                " and " + MaxXField + " >= " + bounds.MinX.ToString(ci) +
                " and " + MinYField + " <= " + bounds.MaxY.ToString(ci) +
                " and " + MaxYField + " >= " + bounds.MinY.ToString(ci);
        }

        private string getSelectString(BoundingRectangle bounds)
        {
            return "select " +
                   _uniqKeyField + ", " + _titleField + ", " + _spatialDataField + 
                   " from " + DataTableName + 
                   " where " + getBoundsConditionString(bounds);
        }

        private bool addFeatureToReceiver(IFeatureReceiver fr, string uniqKey, string title, byte[] spatialData)
        {
            Feature feature = FeatureFromSpatialDataBytes(spatialData);
            feature.UniqKey = uniqKey;
            feature.Title = title;

            bool isAccepted = true;
            if (FeatureFetched != null)
            {
                FeatureOperationEventArgs foea = new FeatureOperationEventArgs(feature);
                FeatureFetched(this, foea);
                isAccepted = foea.IsAccepted;
            }

            if(isAccepted)
                fr.AddFeature(feature);

            return isAccepted;
        }

        private void addParameter(DbCommand command, string paramName, object paramValue)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = paramName;
            param.Value = paramValue;
            command.Parameters.Add(param);
        }

        private void insertFeatureIntoDataTable(Feature feature, DbCommand command)
        {
            string uniqKey = feature.UniqKey;
            if (IdValueNeeded != null)
            {
                IdNeededEventArgs args = new IdNeededEventArgs(feature);
                IdValueNeeded(this, args);
                uniqKey = args.Id;

            }

            string title = feature.Title;
            int featureType = (int)feature.FeatureType;
            byte[] spatialData = SpatialDataBytesFromFeature(feature);
            BoundingRectangle featureBounds = feature.BoundingRectangle;

            command.CommandText = "insert into " + _dataTableName +
                "(" + UniqKeyField + ", " + TitleField + ", " + FeatureTypeField + ", " + SpatialDataField + ", " + 
                      MinXField + ", " + MinYField + ", " + MaxXField + ", " + MaxYField + ") values " +
                "(@" + UniqKeyField + ", @" + TitleField + ", @" + FeatureTypeField + ", @" + SpatialDataField + ", " + 
                "@" + MinXField + ", @" + MinYField + ", @" + MaxXField + ", @" + MaxYField + ")";

            addParameter(command, UniqKeyField, uniqKey);
            addParameter(command, TitleField, title);
            addParameter(command, FeatureTypeField, featureType);
            addParameter(command, SpatialDataField, spatialData);
            addParameter(command, MinXField, featureBounds.MinX);
            addParameter(command, MinYField, featureBounds.MinY);
            addParameter(command, MaxXField, featureBounds.MaxX);
            addParameter(command, MaxYField, featureBounds.MaxY);

            command.ExecuteNonQuery();
            command.Parameters.Clear();
        }

        /// <summary>
        /// Gets a DbCommand corresponding to the sql server
        /// with assighed connection.
        /// </summary>
        protected abstract DbCommand GetCommand();

        /// <summary>
        /// Adds features from the command to the receiver.
        /// </summary>
        /// <param name="fr">Object that receives features</param>
        /// <param name="commandText">Sql command text</param>
        protected int InternalQueryFeatures(IFeatureReceiver fr, string commandText)
        {
            int result = 0;

            using (DbCommand command = GetCommand())
            {
                if (command != null)
                {
                    command.CommandText = commandText;

                    IDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        if(addFeatureToReceiver(fr, reader[_uniqKeyField].ToString(),
                                    reader[_titleField].ToString(),
                                    (byte[])reader[_spatialDataField]))
                            result++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Fills a data table with features.
        /// </summary>
        /// <param name="features">Enumarator of features</param>
        public virtual void FillDataTable(IEnumerable<Feature> features)
        {
            DbCommand command = GetCommand();

            foreach (Feature feature in features)
                insertFeatureIntoDataTable(feature, command);
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <param name="bounds">Rectangular region you want to fill with the objects</param>
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver, BoundingRectangle bounds)
        {
            return InternalQueryFeatures(receiver, getSelectString(bounds));
        }

        /// <summary>
        /// Adds features retrieved from the data source to the receiver.
        /// </summary>
        /// <param name="receiver">An object that receives features</param> 
        /// <rereturns>A number of retrieved features</rereturns>
        public override int QueryFeatures(IFeatureReceiver receiver)
        {
            return InternalQueryFeatures(receiver, getSelectString());
        }
    }
}