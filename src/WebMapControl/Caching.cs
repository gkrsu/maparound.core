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
** File: Caching.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Storing map objects in System.Web.Caching.Cache
**
=============================================================================*/

using System.Text;
using MapAround.Geometry;

namespace MapAround.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Web.Caching;
    using System.IO;
    using System.Globalization;
    using System.Threading;
    using System.Linq;
    
    using MapAround.Mapping;
    using MapAround.Indexing;

    /// <summary>
    /// Provide access to caching features in the System.Web.Caching.Cache.
    /// </summary>
    public class WebCacheAccessor : FeatureCollectionCacheAccessorBase
    {
        private Cache _cache = null;

        private TimeSpan _expirationTimeout = new TimeSpan(1, 0, 0);

        /// <summary>
        /// Gets or sets an expiration timeout of the adding objects.
        /// </summary>
        public TimeSpan ExpirationTimeout
        {
            get { return _expirationTimeout; }
            set { _expirationTimeout = value; }
        }

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
        /// <returns>True, if passed value is valid, otherwise false</returns>
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
            _cache.Insert(Key + featureTypeSubKey(featureType), features, null, System.Web.Caching.Cache.NoAbsoluteExpiration, _expirationTimeout, CacheItemPriority.NotRemovable, null);
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
            _cache.Insert(Key + featureTypeSubKey(featureType) + "index", index, null, System.Web.Caching.Cache.NoAbsoluteExpiration, _expirationTimeout, CacheItemPriority.NotRemovable, null);
        }

        /// <summary>
        /// Saves an attribute list into cache.
        /// </summary>
        /// <param name="attributeNames">Attribute list</param>
        protected override void AddOrReplaceAttributeNames(IList<string> attributeNames)
        {
            checkCache();
            _cache.Insert(Key + "attributeNames", attributeNames, null, System.Web.Caching.Cache.NoAbsoluteExpiration, _expirationTimeout, CacheItemPriority.NotRemovable, null);
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
                if(_cache == null)
                    return false;

                bool result = _cache[Key + featureTypeSubKey(FeatureType.Point) + "index"] != null &&
                              _cache[Key + featureTypeSubKey(FeatureType.Polyline) + "index"] != null &&
                              _cache[Key + featureTypeSubKey(FeatureType.Polygon) + "index"] != null &&
                              _cache[Key + "attributeNames"] != null;

                return  result;
            }
        }

        /// <summary>
        /// Removes all collections corresponding to the current Key from cache.
        /// </summary>
        public void Clear()
        {
            if (_cache[Key + featureTypeSubKey(FeatureType.Point) + "index"] != null)
                _cache.Remove(Key + featureTypeSubKey(FeatureType.Point) + "index");

            if (_cache[Key + featureTypeSubKey(FeatureType.Polyline) + "index"] != null)
                _cache.Remove(Key + featureTypeSubKey(FeatureType.Polyline) + "index");

            if (_cache[Key + featureTypeSubKey(FeatureType.Polygon) + "index"] != null)
                _cache.Remove(Key + featureTypeSubKey(FeatureType.Polygon) + "index");

            if (_cache[Key + "attributeNames"] != null)
                _cache.Remove(Key + "attributeNames");
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.Caching.WebCacheAccessor
        /// </summary>
        /// <param name="cache">A System.Web.Caching.Cache instance</param>
        public WebCacheAccessor(Cache cache)
        {
            _cache = cache;
        }
    }

#if !DEMO

    /// <summary>
    /// Represents an object that provides an access 
    /// to the tile cache storing into files.
    /// </summary>
    public class FileTileCacheAccessor : ITileCacheAccessor
    {
        private string _path;
        private string _prefix = string.Empty;
        private string _mutexName;
        private Mutex _syncMutex = null;

        private void Lock()
        {
            try
            {
                _syncMutex = Mutex.OpenExisting(_mutexName);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                _syncMutex = new Mutex(false, _mutexName);
            }

            _syncMutex.WaitOne();
        }

        private void Unlock()
        {
            if (_syncMutex != null)
                _syncMutex.ReleaseMutex();
        }


        /// <summary>
        /// Gets a file name (excluding directory name) by the cache access key.
        /// </summary>
        /// <param name="cacheKey">A cache access key</param>
        /// <param name="contentType"> </param>
        /// <returns>The file name</returns>
        protected virtual string FileNameFromCacheKey(string cacheKey, string contentType)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider cryptoProvider = 
                new System.Security.Cryptography.MD5CryptoServiceProvider();

            byte[] data = System.Text.Encoding.ASCII.GetBytes(cacheKey);
            data = cryptoProvider.ComputeHash(data);

            string result = string.Empty;
            for (int i = 0; i < data.Length; i++)
                result += data[i].ToString("x2").ToLower();

            result = _prefix + result;

            return result + "." + contentType;
        }

        /// <summary>
        /// Gets layer directory
        /// </summary>
        /// <param name="Layer"></param>
        /// <returns></returns>
        protected virtual string GetLayerDir(string Layer)
        {
            StringBuilder strb = new StringBuilder();
            for(int i=0; i<Layer.Length; i++)
            {
                switch (Layer[i])
                {

                    case '\"':
                        strb.Append("_");
                        break;
                    case ':':
                        strb.Append("_");
                        break;
                    case '*':
                        strb.Append("_");
                        break;
                    case '?':
                        strb.Append("_");
                        break;
                    case '\\':
                        strb.Append("_");
                        break;
                    case '|':
                        strb.Append("_");
                        break;
                    case '/':
                        strb.Append("_");
                        break;
                    default:
                        strb.Append(Layer[i]);
                        break;

                }
            }
            return strb.ToString();
        }

        /// <summary>
        /// Gets area
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        protected virtual string GetAreaDir(BoundingRectangle rectangle)
        {
            return
                System.IO.Path.Combine(
                    rectangle.MinX.ToString("#######.########;-#######.########;#######.########", CultureInfo.InvariantCulture),
                    rectangle.MaxX.ToString("#######.########;-#######.########;#######.########", CultureInfo.InvariantCulture),
                    rectangle.MinY.ToString("#######.########;-#######.########;#######.########", CultureInfo.InvariantCulture),
                    rectangle.MaxY.ToString("#######.########;-#######.########;#######.########", CultureInfo.InvariantCulture));
        }
        /// <summary>
        /// Gets or sets a prefix for the cache key.
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }


        /// <summary>
        /// Gets the physical location of the file
        /// </summary>      
        public string GetCacheFile(string layer, BoundingRectangle area, string key, string contentType)
        {
            return System.IO.Path.Combine(_path, GetLayerDir(layer), GetAreaDir(area), FileNameFromCacheKey(key,contentType));
        }
        #region ITileCacheAccessor Members

        /// <summary>
        /// Extracts a binary representation of tile from cache.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="area"></param>
        /// <param name="key">Access key</param>
        /// <returns>Byte array that contains a binary representation of a tile image</returns>
        public byte[] ExtractTileBytes(string layer,BoundingRectangle area, string key, string contentType)
        {
            Lock();
            try
            {
                string filename = GetCacheFile(layer, area, key,contentType);
                if (File.Exists(filename))
                {
                    File.SetLastAccessTime(filename, DateTime.Now);
                    try
                    {
                        return File.ReadAllBytes(filename);
                    }
                    // the case file access cache outside
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }

                return null;
            }
            finally
            {
                Unlock();
            }
        }
        
        /// <summary>
        /// Saves a binary representation of an image into cache.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="area"></param>
        /// <param name="key">Access key</param>
        /// <param name="tile">Byte array that contains a binary representation of a tile image</param>
        public void SaveTileBytes(string layer, BoundingRectangle area,string key, byte[] tile,string contentType)
        {
            Lock();
            try
            {
                string filename = GetCacheFile(layer, area, key, contentType);
                string path = Path.GetDirectoryName(filename);
                if (File.Exists(filename))
                {
                    File.Delete(filename);
            
                }

                

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                    File.WriteAllBytes(filename, tile);
             
            }
            finally
            {
                Unlock();
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the MapAround.Caching.FileTileCacheAccessor
        /// </summary>
        /// <param name="path">The path to the files of cache</param> 
        public FileTileCacheAccessor(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Directory " + path + " not found.");

            _path = path;

            _mutexName = "Global\\MapAroundTileFileCache" +
                _path.Replace(Path.DirectorySeparatorChar, '_');
        }

        /// <summary>
        /// Invalidate cache given area.
        /// </summary>
        /// <param name="area"></param>
        public void Invalidate(BoundingRectangle area)
        {
            if (!Directory.Exists(_path)) return;
            var layers = Directory.GetDirectories(_path);
            foreach (var layer in layers)
            {
                Invalidate(layer,area);
            }
        }
        /// <summary>
        /// Invalidate cache given area.
        /// </summary>
        /// <param name="layer">Layer</param>
        /// <param name="area">Given area</param>
        public void Invalidate(string layer, BoundingRectangle area)
        {
            var path = Path.Combine(_path, GetLayerDir(layer));
            
            if (!Directory.Exists(path)) return;

            
            var minXValues = CheckIntersect(path, area.MaxX, "MaxX");
            if (!minXValues.Any()) return;
            
            // 1 level - minX
            foreach (var minXValue in minXValues)
            {
                var minXPath = Path.Combine(path, minXValue);

                var maxXValues = CheckIntersect(minXPath, area.MinX, "MinX");
                if (!maxXValues.Any()) continue;
                
                // 2 level - maxX
                foreach (var maxXValue in maxXValues)
                {
                    var maxXPath = Path.Combine(minXPath, maxXValue);

                    var minYValues = CheckIntersect(maxXPath, area.MaxY, "MaxY");
                    if (!minYValues.Any()) continue;
                    
                    // 3 level - minY
                    foreach (var minYValue in minYValues)
                    {
                        var minYPath = Path.Combine(maxXPath, minYValue);

                        var maxYValues = CheckIntersect(minYPath, area.MinY, "MinY");
                        if (!maxYValues.Any()) continue;

                        // 4 level - maxY (delete folders)
                        foreach (var maxYValue in maxYValues)
                        {
                            var maxYPath = Path.Combine(minYPath, maxYValue);
                            Directory.Delete(maxYPath, true);
                        }
                        DeleteEmptyDirectory(minYPath);
                    }
                    DeleteEmptyDirectory(maxXPath);
                }
                DeleteEmptyDirectory(minXPath);
            }
        }

        /// <summary>
        /// Helps to find the intersection.
        /// </summary>
        private IEnumerable<string> CheckIntersect(string path, double layerValue, string param)
        {
            switch (param)
            {
                case "MaxX":  // >= MinX
                case "MaxY":  // >= MinY
                    return from f in Directory.GetDirectories(path)
                           where layerValue >= Double.Parse(Path.GetFileName(f),CultureInfo.InvariantCulture)
                           select f;
                case "MinX":  // =< MaxX
                case "MinY":  // =< MaxY
                    return from f in Directory.GetDirectories(path)
                           where layerValue <= Double.Parse(Path.GetFileName(f),CultureInfo.InvariantCulture)
                           select f;
                default:
                    throw new ArgumentException("param");
            }
        }
        
        /// <summary>
        /// Deletes an empty directory.
        /// </summary>
        private void DeleteEmptyDirectory(string path)
        {
            if (Directory.GetDirectories(path).Length > 0 || Directory.GetFiles(path).Length > 0) return;

            Directory.Delete(path);
        }
    }

    /// <summary>
    /// Represents an object that provides an access 
    /// to the JSON object cache storing into files.
    /// </summary>
    public class FileJSONCacheAccessor : IJSONCacheAccessor
    {
        private string _path;
        private string _prefix = string.Empty;

        /// <summary>
        /// Constructs FileJSONCacheAccessor object.
        /// </summary>
        /// <param name="path"></param>
        public FileJSONCacheAccessor(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Directory " + path + " not found.");
            _path = path;
        }

        /// <summary>
        /// Gets a file name (excluding directory name) by the cache access key.
        /// </summary>
        /// <param name="cacheKey">A cache access key</param>
        /// <returns>The file name</returns>
        protected virtual string FileNameFromCacheKey(string cacheKey)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider cryptoProvider =
                new System.Security.Cryptography.MD5CryptoServiceProvider();

            byte[] data = System.Text.Encoding.ASCII.GetBytes(cacheKey);
            data = cryptoProvider.ComputeHash(data);

            string result = string.Empty;
            for (int i = 0; i < data.Length; i++)
                result += data[i].ToString("x2").ToLower();

            result = _prefix + result;

            return result + ".json";
        }
        
        /// <summary>
        /// Gets or sets a prefix for the cache key.
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        #region IJSONCacheAccessor Members

        /// <summary>
        /// Extracts a string representation of an JSON object from cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ExtractJSONBytes(string key)
        {
            try
            {
                string filename = _path + System.IO.Path.DirectorySeparatorChar + FileNameFromCacheKey(key);
                if (File.Exists(filename))
                {
                    File.SetLastAccessTime(filename, DateTime.Now);
                    try
                    {
                        return File.ReadAllText(filename);
                    }
                    // the case file access cache outside
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                }

                return null;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Saves a string representation of an JSON object into cache.
        /// </summary>
        /// <param name="key">Access key</param>
        /// <param name="jsonObject">Byte array that contains a binary representation of a JSON object</param>
        public void SaveJSONBytes(string key, string jsonObject)
        {
            try
            {
                string filename = _path + System.IO.Path.DirectorySeparatorChar + FileNameFromCacheKey(key);
                string path = Path.GetDirectoryName(filename);

                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                File.WriteAllText(filename, jsonObject);
            }
            finally
            {
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents an object that provides an access 
    /// to the tile cache storing into System.Web.Caching.Cache.
    /// </summary>
    public class WebTileCacheAccessor : ITileCacheAccessor
    {
        private Cache _cache = null;
        private string _prefix = "tileCache";
        private TimeSpan _expirationTimeout = new TimeSpan(0, 5, 0);

        private void checkCache()
        {
            if (_cache == null)
                throw new InvalidOperationException("Undefined cache");
        }

        /// <summary>
        /// Gets or sets an expiration timeout of the adding objects.
        /// </summary>
        public TimeSpan ExpirationTimeout
        {
            get { return _expirationTimeout; }
            set { _expirationTimeout = value; }
        }

        /// <summary>
        /// Gets or sets a prefix for the cache key.
        /// </summary>
        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        #region ITileCacheAccessor Members

        /// <summary>
        /// Extracts a binary representation of tile from cache.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="area">Area description </param>
        /// <param name="key">Access key</param>
        /// <returns>Byte array that contains a binary representation of a tile image</returns>
        public byte[] ExtractTileBytes(string layer, BoundingRectangle area, string key,string contentType)
        {
            checkCache();
            object extractedObject = _cache[_prefix +layer+ key+contentType];
            byte[] tileBytes = extractedObject as byte[];
            return tileBytes;
        }

        /// <summary>
        /// Saves a binary representation of an image into cache.
        /// </summary>
        /// <param name="area">Area description </param>
        /// <param name="layer"></param>
        /// <param name="key">Access key</param>
        /// <param name="tile">Byte array that contains a binary representation of a tile image</param>
        public void SaveTileBytes(string layer, BoundingRectangle area, string key, byte[] tile, string contentType)
        {
            checkCache();
            _cache.Insert(_prefix + layer+key+contentType, tile, null, System.Web.Caching.Cache.NoAbsoluteExpiration, _expirationTimeout, CacheItemPriority.NotRemovable, null);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the MapAround.Caching.WebTileCacheAccessor
        /// </summary>
        /// <param name="cache">An instance of the System.Web.Caching.Cache</param>
        public WebTileCacheAccessor(Cache cache)
        {
            _cache = cache;
        }
    }

#endif

}