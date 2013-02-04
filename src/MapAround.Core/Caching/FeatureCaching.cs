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
** File: FeatureCaching.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Interfaces and classes for feature caching.
**
=============================================================================*/

namespace MapAround.Caching
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    using MapAround.Mapping;
    using MapAround.Indexing;

    /// <summary>
    /// The MapAround.Caching namespace contains interfaces 
    /// and classes for caching map data, such as features and 
    /// rasters.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Provides access to the cache of feature collection
    /// <para>
    /// Instances, that implement this interface can be used
    /// by data providers to access to the specific caches (eg,
    /// System.Web.Caching.Cache) to speed up access and / or
    /// data sharing.
    /// </para>
    /// </summary>
    public interface IFeatureCollectionCacheAccessor
    {
        /// <summary>
        /// Gets or sets cache access key.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Gets a value indicating whether the objects 
        /// corresponding to the <see cref="Key"/> exist in cache.
        /// </summary>
        bool ExistsInCache { get; }
        
        /// <summary>
        /// Saves a feature collection to the cache.
        /// </summary>
        /// <param name="features">Feature collection</param>
        /// <param name="featureType">Type of features</param>
        void SaveFeatures(ICollection<Feature> features, FeatureType featureType);

        /// <summary>
        /// Restores a feature collection from the cache.
        /// </summary>
        /// <param name="featureType">Type of features</param>
        /// <returns>Restored collection of features</returns>
        ICollection<Feature> RestoreFeatures(FeatureType featureType);

        /// <summary>
        /// Saves a spatial index containing features to the cache.
        /// </summary>
        /// <param name="index">Spatial index</param>
        /// <param name="featureType">Type of features in index</param>
        void SaveFeaturesIndex(ISpatialIndex index, FeatureType featureType);

        /// <summary>
        /// Restores a spatial index containing features from cache.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <returns>Restored spatial index</returns>
        ISpatialIndex RestoreFeaturesIndex(FeatureType featureType);

        /// <summary>
        /// Saves a list of attribute names to the cache.
        /// </summary>
        /// <param name="attributeNames">List of attribute names</param>
        void SaveAttributeNames(IList<string> attributeNames);

        /// <summary>
        /// Restores a list of attribute names from the cache
        /// </summary>
        /// <returns>List of attribute names </returns>
        IList<string> RestoreAttributeNames();
    }

    /// <summary>
    /// Base class providing access to feature caching.
    /// </summary>
    public abstract class FeatureCollectionCacheAccessorBase : IFeatureCollectionCacheAccessor
    {
        private static object _syncRoot = new object();
        private string _key = string.Empty;

        /// <summary>
        /// Checks a value for validity when using as the access key.
        /// </summary>
        /// <param name="key">Value need to be checked</param>
        /// <returns>True, if passed value is valid, else false</returns>
        protected abstract bool CheckKeyString(string key);

        /// <summary>
        /// Adds a collection to the cache or replaces it.
        /// </summary>
        /// <param name="features">Feature collection</param>
        /// <param name="featureType">Feature type</param>
        protected abstract void AddOrReplaceCollection(ICollection<Feature> features, FeatureType featureType);

        /// <summary>
        /// Extracts a collection from cache.
        /// </summary>
        /// <param name="featureType">Type of features in the collection</param>
        /// <returns>Extracted collection</returns>
        protected abstract Collection<Feature> ExtractCollection(FeatureType featureType);

        /// <summary>
        /// Adds a feature index to the cache or replaces it.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <param name="index">Spatial index</param>
        protected abstract void AddOrReplaceIndex(ISpatialIndex index, FeatureType featureType);

        /// <summary>
        /// Extracts a feature index from cache.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <returns>Spatial index</returns>
        protected abstract ISpatialIndex ExtractIndex(FeatureType featureType);

        #region IFeatureCollectionCache Members

        /// <summary>
        /// Gets or sets an access key value.
        /// </summary>
        public string Key
        {
            get
            { return _key; }
            set
            {
                if (CheckKeyString(value))
                    _key = value;
                else
                    throw new ArgumentException("Illegal cache key", "value");
            }
        }


        /// <summary>
        /// Gets a value indicating whether the objects 
        /// corresponding to the <see cref="Key"/> exist in cache.
        /// </summary>
        public abstract bool ExistsInCache { get; }

        /// <summary>
        /// Saves a feature collection to the cache.
        /// </summary>
        /// <param name="features">Feature collection</param>
        /// <param name="featureType">Type of features</param>
        public void SaveFeatures(ICollection<Feature> features, FeatureType featureType)
        {
            // copy features to new collection
            Collection<Feature> preparedCollection = new Collection<Feature>();

            lock (_syncRoot)
            {
                foreach (Feature s in features)
                {
                    if (s.FeatureType != featureType)
                        throw new ArgumentException("Illegal feature type", "features");
                    preparedCollection.Add((Feature)s.Clone());
                }

                AddOrReplaceCollection(preparedCollection, FeatureType.Point);
            }
        }

        /// <summary>
        /// Restores a feature collection from the cache.
        /// </summary>
        /// <param name="featureType">Type of features</param>
        /// <returns>Restored collection of features</returns>
        public ICollection<Feature> RestoreFeatures(FeatureType featureType)
        {
            Collection<Feature> result = null; 

            lock (_syncRoot)
                result = ExtractCollection(featureType);

            return result;
        }

        /// <summary>
        /// Saves a spatial index containing features to the cache.
        /// </summary>
        /// <param name="index">Spatial index</param>
        /// <param name="featureType">Type of features in index</param>
        public void SaveFeaturesIndex(ISpatialIndex index, FeatureType featureType)
        {
            List<Feature> features = new List<Feature>();
            lock (_syncRoot)
            {
                index.QueryObjectsInRectangle(index.IndexedSpace, features);

                foreach (Feature s in features)
                    if (s.FeatureType != featureType)
                        throw new ArgumentException("Illegal feature type", "index");

                AddOrReplaceIndex((ISpatialIndex)index.Clone(), featureType);
            }
        }

        /// <summary>
        /// Restores a spatial index containing features from cache.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <returns>Restored spatial index</returns>
        public ISpatialIndex RestoreFeaturesIndex(FeatureType featureType)
        {
            ISpatialIndex result = null;

            lock (_syncRoot)
                result = ExtractIndex(featureType);

            return result;
        }

        /// <summary>
        /// Saves a list of attribute names to the cache.
        /// </summary>
        /// <param name="attributeNames">List of attribute names</param>
        public void SaveAttributeNames(IList<string> attributeNames)
        {
            List<string> temp = new List<string>();
            foreach (string s in attributeNames)
                temp.Add(s);

            lock (_syncRoot)
                AddOrReplaceAttributeNames(temp);
        }

        /// <summary>
        /// Restores a list of attribute names from the cache
        /// </summary>
        /// <returns>List of attribute names </returns>
        public IList<string> RestoreAttributeNames()
        {
            IList<string> result = null;
            lock (_syncRoot)
                result = ExtractAttributeNames();
            return result;
        }
 
        #endregion

        /// <summary>
        /// Saves an attribute list into cache.
        /// </summary>
        /// <param name="attributeNames">Attribute list</param>
        protected abstract void AddOrReplaceAttributeNames(IList<string> attributeNames);

        /// <summary>
        /// Extracts an attribute list from cache.
        /// </summary>
        /// <returns>Attribute list</returns>
        protected abstract IList<string> ExtractAttributeNames();
    }

    /// <summary>
    /// Simple in-memory cache.
    /// <para>
    /// Use this class when you need
    /// 1. to store objects that are the same for many layers
    /// 2. to reduce time needed to access the frequently requested data
    /// </para>
    /// </summary>
    public class SimpleSpatialDataCache
    {
        private Hashtable _hashTable = new Hashtable();

        /// <summary>
        /// Adds an object to the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="obj">Object</param>
        public void Add(string key, object obj)
        {
            _hashTable.Add(key, obj);
        }

        /// <summary>
        /// Gets an object from cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Object</returns>
        public object Get(string key)
        {
            return _hashTable[key];
        }

        /// <summary>
        /// Inserts an object to the cache.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="obj">Object</param>
        public void Insert(string key, object obj)
        {
            _hashTable[key] = obj;
        }

        /// <summary>
        /// Removes an object from cache.
        /// </summary>
        /// <param name="key">Object key</param>
        public void Remove(string key)
        {
            _hashTable.Remove(key);
        }

        /// <summary>
        /// Gets an object from the cache by its key or places new object (replaces existing).
        /// </summary>
        /// <param name="key">Obect key</param>
        /// <returns>Object</returns>
        public object this[string key]
        {
            get { return Get(key);}
            set { Insert(key, value); }
        }
    }

    /// <summary>
    /// Instances of this class provides access to the MapAround.Caching.SimpleSpatialDataCache
    /// and can be used by data providers as a tempopary storage of retrieved data.
    /// Spatial data providers must to clone features after extracting from cache for correct 
    /// work in multithreading environment.
    /// </summary>
    public class SimpleCacheAccessor : FeatureCollectionCacheAccessorBase
    {
        private SimpleSpatialDataCache _cache = null;

        private void checkCache()
        {
            if (_cache == null)
                throw new InvalidOperationException("Undefined cache");
        }

        private string featureTypeSubKey(FeatureType featureType)
        {
            switch (featureType)
            {
                case FeatureType.Point:
                case FeatureType.MultiPoint:
                    return "points";
                case FeatureType.Polyline:
                    return "polylines";
                case FeatureType.Polygon:
                    return "polygons";
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks a value for validity when using as the access key.
        /// </summary>
        /// <param name="key">Value need to be checked</param>
        /// <returns>True, if passed value is valid, else false</returns>
        protected override bool CheckKeyString(string key)
        {
            return true;
        }

        /// <summary>
        /// Adds a collection to the cache or replaces it.
        /// </summary>
        /// <param name="features">Feature collection</param>
        /// <param name="featureType">Feature type</param>
        protected override void AddOrReplaceCollection(ICollection<Feature> features, FeatureType featureType)
        {
            checkCache();
            _cache.Insert(Key + featureTypeSubKey(featureType), features);
        }

        /// <summary>
        /// Extracts a collection from cache.
        /// </summary>
        /// <param name="featureType">Type of features in the collection</param>
        /// <returns>Extracted collection</returns>
        protected override Collection<Feature> ExtractCollection(FeatureType featureType)
        {
            checkCache();
            object extractedObject = _cache[Key + featureTypeSubKey(featureType)];

            Collection<Feature> features = extractedObject as Collection<Feature>;
            return features;
        }

        /// <summary>
        /// Extracts a feature index from cache.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <returns>Spatial index</returns>
        protected override ISpatialIndex ExtractIndex(FeatureType featureType)
        {
            checkCache();
            object extractedObject = _cache[Key + featureTypeSubKey(featureType) + "index"];

            ISpatialIndex index = extractedObject as ISpatialIndex;
            return index;
        }

        /// <summary>
        /// Adds a feature index to the cache or replaces it.
        /// </summary>
        /// <param name="featureType">Feature type</param>
        /// <param name="index">Spatial index</param>
        protected override void AddOrReplaceIndex(ISpatialIndex index, FeatureType featureType)
        {
            checkCache();
            _cache.Insert(Key + featureTypeSubKey(featureType) + "index", index);
        }

        /// <summary>
        /// Saves an attribute list into cache.
        /// </summary>
        /// <param name="attributeNames">Attribute list</param>
        protected override void AddOrReplaceAttributeNames(IList<string> attributeNames)
        {
            checkCache();
            _cache.Insert(Key + "attributeNames", attributeNames);
        }

        /// <summary>
        /// Extracts an attribute list from cache.
        /// </summary>
        /// <returns>Attribute list</returns>
        protected override IList<string> ExtractAttributeNames()
        {
            checkCache();
            object extractedObject = _cache[Key + "attributeNames"];

            IList<string> attributeNames = extractedObject as IList<string>;
            return attributeNames;
        }

        /// <summary>
        /// Gets a value indicating whether the objects 
        /// corresponding to the Key exist in cache.
        /// </summary>
        public override bool ExistsInCache
        {
            get
            {
                if (_cache == null)
                    return false;

                bool result = _cache[Key + featureTypeSubKey(FeatureType.Point) + "index"] != null &&
                              _cache[Key + featureTypeSubKey(FeatureType.Polyline) + "index"] != null &&
                              _cache[Key + featureTypeSubKey(FeatureType.Polygon) + "index"] != null &&
                              _cache[Key + "attributeNames"] != null;

                return result;
            }
        }

        /// <summary>
        /// Initializes a new instance of Maparaound.Caching.SimpleCacheAccessor.
        /// </summary>
        /// <param name="cache">Maparaound.Caching.SimpleSpatialDataCache instance</param>
        public SimpleCacheAccessor(SimpleSpatialDataCache cache)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");

            _cache = cache;
        }
    }
}