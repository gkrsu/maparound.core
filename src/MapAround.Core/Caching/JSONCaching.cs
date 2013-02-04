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
using System.Linq;
using System.Text;

namespace MapAround.Caching
{
    /// <summary>
    /// Represents an object that provides an access to the JSON cache. 
    /// </summary>
    public interface IJSONCacheAccessor
    {
        /// <summary>
        /// Extracts a string representation of JSON object from cache.
        /// </summary>
        /// <param name="key">Access key</param>
        /// <returns>Byte array that contains a binary representation of a JSON object</returns>
        string ExtractJSONBytes(string key);
        
        /// <summary>
        /// Saves a string representation of an JSON object into cache.
        /// </summary>
        /// <param name="key">Access key</param>
        /// <param name="jsonObject">JSON object</param>
        void SaveJSONBytes(string key, string jsonObject);
    }
}
