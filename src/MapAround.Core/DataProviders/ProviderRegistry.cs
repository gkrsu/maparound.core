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
** File: ProviderRegistry.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Data providers registry
**
=============================================================================*/

namespace MapAround.DataProviders
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;

    /// <summary>
    /// Delegate of a method that gets a spatial data provider.
    /// </summary>
    /// <returns>Data provider instance</returns>
    public delegate ISpatialDataProvider ProviderRetriever();

    /// <summary>
    /// Delegate of a method that gets a raster provider.
    /// </summary>
    /// <returns>Raster provider instance</returns>
    public delegate IRasterProvider RasterProviderRetriever();

    /// <summary>
    /// Represents an object that contains spatial 
    /// data provider registration info
    /// </summary>
    public interface ISpatialDataProviderHolder
    {
        /// <summary>
        /// Sets parameter values.
        /// </summary>
        /// <param name="parameters">Parameter values</param>
        void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Gets a list containing the names of parameters
        /// </summary>
        /// <returns>List containing the names of parameters</returns>
        string[] GetParameterNames();

        /// <summary>
        /// Gets a spatial data provider instance.
        /// </summary>
        ISpatialDataProvider GetProvider();

        /// <summary>
        /// Performs a finalization procedure for the spatial data provider, if needed.
        /// </summary>
        /// <param name="provider">Spatial data provider instance</param>
        void ReleaseProviderIfNeeded(ISpatialDataProvider provider);

        /// <summary>
        /// Gets the name of the spatial data provider.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Represents an object that contains raster 
    /// provider registration info
    /// </summary>
    public interface IRasterProviderHolder
    {
        /// <summary>
        /// Sets parameter values.
        /// </summary>
        /// <param name="parameters">Parameter values</param>
        void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Gets a list containing the names of parameters.
        /// </summary>
        /// <returns>List containing the names of parameters</returns>
        string[] GetParameterNames();

        /// <summary>
        /// Gets a raster provider instance.
        /// </summary>
        IRasterProvider GetProvider();

        /// <summary>
        /// Performs a finalization procedure for the raster provider, if needed.
        /// </summary>
        /// <param name="provider">Raster provider instance</param>
        void ReleaseProviderIfNeeded(IRasterProvider provider);

        /// <summary>
        /// Gets the name of the spatial data provider.
        /// </summary>
        string Name { get; }
    }
    
    /// <summary>
    /// MapAround.DataProviders.SpatialDataProviderHolderBase is the base class
    /// for the data provider holding classes.
    /// Instances of these classes contains the providers registration info,
    /// initialization parameters and defines the resource menegement by 
    /// implementing the ReleaseProviderIfNeeded method.
    /// </summary>
    public abstract class SpatialDataProviderHolderBase : ISpatialDataProviderHolder
    {
        private string _name = string.Empty;
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        /// <summary>
        /// References to the method that returns an ISpatialDataProvider instance.
        /// </summary>
        protected ProviderRetriever GetProviderMethod;

        /// <summary>
        /// Gets or sets a dictionary that contains paramater values.
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// Gets a name of the spatial data provider.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Sets paramater values.
        /// </summary>
        /// <param name="parameters">Dictionary that contains parameter values</param>
        public abstract void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Gets paramater names.
        /// </summary>
        /// <returns>String array that contains parameter names</returns>
        public abstract string[] GetParameterNames();

        /// <summary>
        /// Gets the spatial data provider.
        /// </summary>
        public ISpatialDataProvider GetProvider()
        {
            if (GetProviderMethod != null)
                return GetProviderMethod();

            return null;
        }

        /// <summary>
        /// Performs a finalization procedure for the spatial data provider, if needed.
        /// </summary>
        /// <param name="provider">Spatial data provider instance</param>
        public abstract void ReleaseProviderIfNeeded(ISpatialDataProvider provider);

        /// <summary>
        /// Initializes a new instance of MapAround.DataProviders.SpatialDataProviderInfoBase.
        /// </summary>
        /// <param name="name">Name</param>
        protected SpatialDataProviderHolderBase(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            _name = name;
        }
    }

    /// <summary>
    /// MapAround.DataProviders.RasterProviderHolderBase is the base class
    /// for the data provider holding classes.
    /// Instances of these classes contains the providers registration info,
    /// initialization parameters and defines the resource menegement by 
    /// implementing the ReleaseProviderIfNeeded method.
    /// </summary>
    public abstract class RasterProviderHolderBase : IRasterProviderHolder
    {
        private string _name = string.Empty;
        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        /// <summary>
        /// References to the method that returns an IRasterProvider instance.
        /// </summary>
        protected RasterProviderRetriever GetProviderMethod;

        /// <summary>
        /// Gets or sets a dictionary that contains paramater values.
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// Gets a name of the spatial data provider.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Sets paramater values.
        /// </summary>
        /// <param name="parameters">Dictionary that contains parameter values</param>
        public abstract void SetParameters(Dictionary<string, string> parameters);

        /// <summary>
        /// Gets paramater names.
        /// </summary>
        /// <returns>String array that contains parameter names</returns>
        public abstract string[] GetParameterNames();

        /// <summary>
        /// Gets the raster provider.
        /// </summary>
        public IRasterProvider GetProvider()
        {
            if (GetProviderMethod != null)
                return GetProviderMethod();

            return null;
        }

        /// <summary>
        /// Performs a finalization procedure for the raster provider, if needed.
        /// </summary>
        /// <param name="provider">Spatial data provider instance</param>
        public abstract void ReleaseProviderIfNeeded(IRasterProvider provider);

        /// <summary>
        /// Initializes a new instance of MapAround.DataProviders.RasterProviderHolderBase.
        /// </summary>
        /// <param name="name">Name</param>
        protected RasterProviderHolderBase(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            _name = name;
        }
    }

    /// <summary>
    /// Represents a registry of spatial data providers.
    /// </summary>
    public class SpatialDataProviderManager
    {
        private object _syncRoot = new object();
        private Collection<ISpatialDataProviderHolder> _registeredProviders = new Collection<ISpatialDataProviderHolder>();

        /// <summary>
        /// Gets a collection of registered data providers.
        /// </summary>
        public ReadOnlyCollection<ISpatialDataProviderHolder> RegisteredProviders
        {
            get 
            { 
                return new ReadOnlyCollection<ISpatialDataProviderHolder>(_registeredProviders); 
            }
        }

        /// <summary>
        /// Registers a spatial data provider.
        /// </summary>
        /// <param name="holder">A holder of spatial data provider</param>
        /// <param name="forceUpdate">A value indicating whether an existing registration info
        /// will be updated</param>
        public void RegisterProvider(ISpatialDataProviderHolder holder, bool forceUpdate)
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < _registeredProviders.Count; i++)
                    if (_registeredProviders[i].Name == holder.Name)
                    {
                        if (forceUpdate)
                            _registeredProviders[i] = holder;

                        return;
                    }

                _registeredProviders.Add(holder);
            }
        }

        /// <summary>
        /// Removes registration info.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        public void UnRegisterProvider(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (ISpatialDataProviderHolder info in _registeredProviders)
                    if (info.Name == providerName)
                    {
                        _registeredProviders.Remove(info);
                        break;
                    }
            }
        }

        /// <summary>
        /// Determines whether the data provider is registered.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        /// <returns>true, if the data provider is registered, false otherwise</returns>
        public bool Registered(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (SpatialDataProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                        return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a list containing names of the initialization 
        /// parameters of the spatial data provider.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        /// <returns></returns>
        public string[] GetProviderParameterNames(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (SpatialDataProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                        return info.GetParameterNames();
            }

            return new string[] { };
        }

        /// <summary>
        /// Creates and initializes a new instance of the spatial data provider.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        /// <returns>A new instance of the spatial data provider</returns>
        public ISpatialDataProvider GetProvider(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (SpatialDataProviderHolderBase holder in _registeredProviders)
                    if (holder.Name == providerName)
                        return holder.GetProvider();

                return null;
            }
        }

        /// <summary>
        /// Performs an actions need to be done for finalization of the data provider.
        /// <para>
        /// Holders can provide various scenarious of disposing resources 
        /// for spatial data providers of the same class.
        /// </para>
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        /// <param name="provider">A provider instance for finalization</param>
        public void ReleaseProviderIfNeeded(string providerName, ISpatialDataProvider provider)
        {
            lock (_syncRoot)
            {
                foreach (SpatialDataProviderHolderBase holder in _registeredProviders)
                    if (holder.Name == providerName)
                    {
                        holder.ReleaseProviderIfNeeded(provider);
                        return;
                    }
            }
        }

        /// <summary>
        /// Creates and initializes a new instance of the spatial data provider.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provide</param>
        /// <param name="parameters">A dictionary instance containing initialization parameter values</param>
        /// <returns>A new instance of the spatial data provider</returns>
        public ISpatialDataProvider GetProvider(string providerName, Dictionary<string, string> parameters)
        {
            lock (_syncRoot)
            {
                foreach (SpatialDataProviderHolderBase holder in _registeredProviders)
                    if (holder.Name == providerName)
                    {
                        holder.SetParameters(parameters);
                        return holder.GetProvider();
                    }

                return null;
            }
        }
    }

    /// <summary>
    /// Represents a registry of raster providers.
    /// </summary>
    public class RasterProviderManager
    {
        private object _syncRoot = new object();
        private Collection<IRasterProviderHolder> _registeredProviders = new Collection<IRasterProviderHolder>();

        /// <summary>
        /// Gets a collection of registered data providers.
        /// </summary>
        public ReadOnlyCollection<IRasterProviderHolder> RegisteredProviders
        {
            get
            {
                return new ReadOnlyCollection<IRasterProviderHolder>(_registeredProviders);
            }
        }

        /// <summary>
        /// Registers a raster provider.
        /// </summary>
        /// <param name="holder">A holder of raster provider</param>
        /// <param name="forceUpdate">A value indicating whether an existing registration info
        /// will be updated</param>
        public void RegisterProvider(IRasterProviderHolder holder, bool forceUpdate)
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < _registeredProviders.Count; i++)
                    if (_registeredProviders[i].Name == holder.Name)
                    {
                        if (forceUpdate)
                            _registeredProviders[i] = holder;

                        return;
                    }

                _registeredProviders.Add(holder);
            }
        }

        /// <summary>
        /// Removes registration info.
        /// </summary>
        /// <param name="providerName">A name of the raster provider</param>
        public void UnRegisterProvider(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (IRasterProviderHolder info in _registeredProviders)
                    if (info.Name == providerName)
                    {
                        _registeredProviders.Remove(info);
                        break;
                    }
            }
        }

        /// <summary>
        /// Determines whether the raster provider is registered.
        /// </summary>
        /// <param name="providerName">A name of the raster provider</param>
        /// <returns>true, if the raster provider is registered, false otherwise</returns>
        public bool Registered(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (RasterProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                        return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a list containing names of the initialization 
        /// parameters of the raster provider.
        /// </summary>
        /// <param name="providerName">A name of the raster provider</param>
        /// <returns></returns>
        public string[] GetProviderParameterNames(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (RasterProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                        return info.GetParameterNames();
            }

            return new string[] { };
        }

        /// <summary>
        /// Creates and initializes a new instance of the raster provider.
        /// </summary>
        /// <param name="providerName">A name of the spatial data provider</param>
        /// <returns>A new instance of the raster provider</returns>
        public IRasterProvider GetProvider(string providerName)
        {
            lock (_syncRoot)
            {
                foreach (RasterProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                        return info.GetProvider();

                return null;
            }
        }

        /// <summary>
        /// Performs an actions need to be done for finalization of the raster provider.
        /// <para>
        /// Holders can provide various scenarious of disposing resources 
        /// for raster providers of the same class.
        /// </para>
        /// </summary>
        /// <param name="providerName">A name of the raster provider</param>
        /// <param name="provider">A provider instance for finalization</param>
        public void ReleaseProviderIfNeeded(string providerName, IRasterProvider provider)
        {
            lock (_syncRoot)
            {
                foreach (RasterProviderHolderBase info in _registeredProviders)
                    if (info.Name == providerName)
                    {
                        info.ReleaseProviderIfNeeded(provider);
                        return;
                    }
            }
        }

        /// <summary>
        /// Creates and initializes a new instance of the raster provider.
        /// </summary>
        /// <param name="providerName">A name of the raster provide</param>
        /// <param name="parameters">A dictionary instance containing initialization parameter values</param>
        /// <returns>A new instance of the raster provider</returns>
        public IRasterProvider GetProvider(string providerName, Dictionary<string, string> parameters)
        {
            lock (_syncRoot)
            {
                foreach (RasterProviderHolderBase holder in _registeredProviders)
                    if (holder.Name == providerName)
                    {
                        holder.SetParameters(parameters);
                        return holder.GetProvider();
                    }

                return null;
            }
        }
    }
}