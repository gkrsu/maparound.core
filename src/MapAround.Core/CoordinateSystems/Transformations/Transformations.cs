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
** File: Transformations.cs 
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Coordinate transformation interfaces and classes
**
=============================================================================*/

namespace MapAround.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Projections;

    /// <summary>
    /// The MapAround.Transformations namespace contains interfaces 
    /// and classes defining coordinate transformations.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Semantic type of transform used in coordinate transformation.
    /// </summary>
    public enum TransformType : int
    {
        /// <summary>
        /// Unknown or unspecified type of transform.
        /// </summary>
        Other = 0,

        /// <summary>
        /// Transform depends only on defined parameters.
        /// For example, a cartographic projection.
        /// </summary>
        Conversion = 1,

        /// <summary>
        /// Transform depends only on empirically derived parameters.
        /// For example a datum transformation.
        /// </summary>
        Transformation = 2,

        /// <summary>
        /// Transform depends on both defined and empirical parameters.
        /// </summary>
        ConversionAndTransformation = 3
    }

    /// <summary>
    /// Provides access to members of objects 
    /// that create math transforms.
    /// </summary>
    /// <remarks>
    /// <para>IMathTransformFactory is a low level factory that is used to create
    /// IMathTransform objects.  Many high level GIS applications will never
    /// need to use a IMathTransformFactory directly; they can use a
    /// ICoordinateTransformationFactory instead.  However, the
    /// IMathTransformFactory interface is specified here, since it can be
    /// used directly by applications that wish to transform other types of
    /// coordinates (e.g. color coordinates, or image pixel coordinates).</para>
    ///
    /// <para>The following comments assume that the same vendor implements the math
    /// transform factory interfaces and math transform interfaces.</para>
    ///
    /// <para>A math transform is an object that actually does the work of applying
    /// formulae to coordinate values.  The math transform does not know or
    /// care how the coordinates relate to positions in the real world.  This
    /// lack of semantics makes implementing IMathTransformFactory significantly
    /// easier than it would be otherwise.</para>
    ///
    /// <para>For example IMathTransformFactory can create affine math transforms.  The
    /// affine transform applies a matrix to the coordinates without knowing how
    /// what it is doing relates to the real world.  So if the matrix scales Z
    /// values by a factor of 1000, then it could be converting meters into
    /// millimeters, or it could be converting kilometers into meters.</para>
    ///
    /// <para>Because math transforms have low semantic value (but high mathematical
    /// value), programmers who do not have much knowledge of how GIS applications
    /// use coordinate systems, or how those coordinate systems relate to the real
    /// world can implement IMathTransformFactory.</para>
    ///
    /// <para>The low semantic content of math transforms also means that they will be
    /// useful in applications that have nothing to do with GIS coordinates.  For
    /// example, a math transform could be used to map color coordinates between
    /// different color spaces, such as converting (red, green, blue) colors into
    /// (hue, light, saturation) colors.</para>
    ///
    /// <para>Since a math transform does not know what its source and target coordinate
    /// systems mean, it is not necessary or desirable for a math transform object
    /// to keep information on its source and target coordinate systems.</para>
    /// </remarks>
    public interface IMathTransformFactory
    {
        /// <summary>
        /// Creates an affine transform from a matrix.
        /// </summary>
        /// <remarks>
        /// If the transform's input dimension is M, and output dimension is N, then
        /// the matrix will have size [N+1][M+1]. The +1 in the matrix dimensions
        /// allows the matrix to do a shift, as well as a rotation. The [M][j]
        /// element of the matrix will be the j'th ordinate of the moved origin.
        /// The [i][N] element of the matrix will be 0 for i less than M, and 1
        /// for i equals M.
        /// </remarks>
        ///<param name="matrix">The matrix used to define the affine transform.</param>
        ///<returns>The affine transformation</returns>
        IMathTransform CreateAffineTransform(double[,] matrix);

        /// <summary>
        /// Creates a transform by concatenating two existing transforms.
        /// </summary>
        /// <remarks>
        /// A concatenated transform acts in the same way as applying two transforms,
        /// one after the other.
        ///
        /// The dimension of the output space of the first transform must match
        /// the dimension of the input space in the second transform.
        /// If you wish to concatenate more than two transforms, then you can
        /// repeatedly use this method.
        /// </remarks>
        /// <param name="transform1">The first transform to apply to points.</param>
        /// <param name="transform2">The second transform to apply to points.</param>
        /// <returns>The concatenated transformation</returns>
        IMathTransform CreateConcatenatedTransform(IMathTransform transform1, IMathTransform transform2);

        /// <summary>
        /// Creates a math transform from a well-known text string.
        /// </summary>
        /// <param name="wkt">Well-known text representation of the transform</param>
        /// <rereturns>The transformation</rereturns>
        /// <returns>The transformation</returns>
        IMathTransform CreateFromWKT(string wkt);

        /// <summary>
        /// Creates a math transform from XML.
        /// </summary>
        /// <param name="xml">XML representation of the transform</param>
        IMathTransform CreateFromXML(string xml);

        /// <summary>
        ///  Creates a transform from a classification name and parameters.
        /// </summary>
        /// <remarks>
        /// The client must ensure that all the linear parameters are expressed in
        /// meters, and all the angular parameters are expressed in degrees.  Also,
        /// they must supply "semi_major" and "semi_minor" parameters for
        /// cartographic projection transforms.
        /// </remarks>
        /// <param name="classification">The classification name of the transform (e.g. "Transverse_Mercator")</param>
        /// <param name="parameters">The parameter values in standard units</param>
        /// <returns>The transformation</returns>
        IMathTransform CreateParameterizedTransform(string classification, List<Parameter> parameters);

        /// <summary>
        /// Creates a transform which passes through a subset of ordinates to another transform.
        /// </summary>
        /// <remarks>
        /// This allows transforms to operate on a subset of ordinates.  For example,
        /// if you have (Lat,Lon,Height) coordinates, then you may wish to convert the
        /// height values from meters to feet without affecting the (Lat,Lon) values.
        /// If you wanted to affect the (Lat,Lon) values and leave the Height values
        /// alone, then you would have to swap the ordinates around to
        /// (Height,Lat,Lon).  You can do this with an affine map.
        ///
        /// </remarks>
        /// <param name="firstAffectedOrdinate">The lowest index of the affected ordinates</param>
        /// <param name="subTransform">Transform to use for affected ordinates</param>
        /// <returns>The transformation</returns>
        IMathTransform CreatePassThroughTransform(int firstAffectedOrdinate, IMathTransform subTransform);

        /// <summary>
        /// Indicates whether parameter is angular.
        /// </summary>
        /// <remarks>Clients must ensure that all angular parameter values are in degrees</remarks>
        /// <param name="parameterName">Name of parameter to test</param>
        /// <returns>True if the parameter is angular, else otherwise</returns>
        bool IsParameterAngular(string parameterName);

        /// <summary>
        /// Indicates whether parameter is linear.
        /// </summary>
        /// <remarks>
        /// Clients must ensure that all linear parameter values are in meters.
        /// </remarks>
        /// <param name="parameterName">Name of parameter to test</param>
        /// <returns>True if the parameter is linear, else otherwise</returns>
        bool IsParameterLinear(string parameterName);
    }

    /// <summary>
    /// Transforms multi-dimensional coordinate points.
    /// </summary>
    /// <remarks>
    /// If a client application wishes to query the source and target
    /// coordinate systems of a transformation, then it should keep hold
    /// of the ICoordinateTransformation interface, and use the
    /// contained math transform object whenever it wishes to perform a transform.
    /// </remarks>
    public interface IMathTransform
    {
        /// <summary>
        /// Gets the dimension of input points.
        /// </summary>
        /// <returns>The dimension of input points</returns>
        int DimSource { get; }

        /// <summary>
        /// Gets the dimension of output points.
        /// </summary>
        /// <returns>The dimension of output points</returns>
        int DimTarget { get; }

        /// <summary>
        /// Indicates whether this transform does not move any points.
        /// </summary>
        /// <returns>True if the transform does not move any points, else otherwise</returns>
        bool Identity();

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        string WKT { get; }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        string XML { get; }

        /// <summary>
        /// Gets the derivative of this transform at a point.
        /// </summary>
        /// <remarks>
        /// If the transform does not have a well-defined derivative at the point,
        /// then this function should fail in the usual way for the DCP.
        /// The derivative is the matrix of the non-translating portion of the
        /// approximate affine map at the point.  The matrix will have dimensions
        /// corresponding to the source and target coordinate systems.
        /// If the input dimension is M, and the output dimension is N, then
        /// the matrix will have size [M][N].  The elements of the matrix
        /// {elt[n][m] : n=0..(N-1)} form a vector in the output space which is
        /// parallel to the displacement caused by a small change in the m'th
        /// ordinate in the input space.
        /// </remarks>
        /// <param name="point">Point in domain at which to get derivative.</param>
        /// <returns>An array containing the derivative values</returns>
        double[,] Derivative(double[] point);

        /// <summary>
        /// Gets transformed convex hull.
        /// </summary>
        /// <remarks>
        ///  <para>The supplied ordinates are interpreted as a sequence of points, which
        ///  generates a convex hull in the source space.  The returned sequence of
        ///  ordinates represents a convex hull in the output space.  The number of
        ///  output points will often be different from the number of input points.
        ///  Each of the input points should be inside the valid domain (this can be
        ///  checked by testing the points' domain flags individually).  However,
        ///  the convex hull of the input points may go outside the valid domain.
        ///  The returned convex hull should contain the transformed image of the
        ///  intersection of the source convex hull and the source domain. </para>
        ///
        ///  <para>A convex hull is a shape in a coordinate system, where if two positions A
        ///  and B are inside the shape, then all positions in the straight line
        ///  between A and B are also inside the shape.  So in 3D a cube and a sphere
        ///  are both convex hulls.  Other less obvious examples of convex hulls are
        ///  straight lines, and single points.  (A single point is a convex hull,
        ///  because the positions A and B must both be the same - i.e. the point
        ///  itself.  So the straight line between A and B has zero length.)</para>
        ///
        /// <para>Some examples of shapes that are NOT convex hulls are donuts, and horseshoes.</para>
        /// </remarks>
        /// <param name="points">Packed ordinates of points used to generate convex hull</param>
        List<double> GetCodomainConvexHull(List<double> points);

        /// <summary>
        /// Gets flags classifying domain points within a convex hull.
        /// </summary>
        /// <remarks>
        /// The supplied ordinates are interpreted as a sequence of points, which
        /// generates a convex hull in the source space.  Conceptually, each of the
        /// (usually infinite) points inside the convex hull is then tested against
        /// the source domain.  The flags of all these tests are then combined.  In
        /// practice, implementations of different transforms will use different
        /// short-cuts to avoid doing an infinite number of tests.
        /// </remarks>
        /// <param name="points">Packed ordinates of points used to generate convex hull</param>
        DomainFlags GetDomainFlags(List<double> points);

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// However, all cartographic projections should succeed.
        /// </remarks>
        IMathTransform Inverse();

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        double[] Transform(double[] point);

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <remarks>
        /// This method is provided for efficiently transforming many points.
        /// The supplied array of ordinal values will contain packed ordinal
        /// values.  For example, if the source dimension is 3, then the ordinals
        /// will be packed in this order (x0,y0,z0,x1,y1,z1 ...).  The size
        /// of the passed array must be an integer multiple of DimSource.
        /// The returned ordinal values are packed in a similar way.
        /// In some DCPs. the ordinals may be transformed in-place, and the
        /// returned array may be the same as the passed array.
        /// So any client code should not attempt to reuse the passed ordinal
        /// values (although they can certainly reuse the passed array).
        /// If there is any problem then the server implementation will throw an
        /// exception.  If this happens then the client should not make any
        /// assumptions about the state of the ordinal values.
        /// </remarks>
        /// <param name="points">Packed ordinates of points to transform</param>
        List<double[]> TransformList(List<double[]> points);

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        void Invert();
    }

    /// <summary>
    /// Creates coordinate transformations.
    /// </summary>
    public interface ICoordinateTransformationFactory
    {
        /// <summary>
        /// Creates a transformation between two coordinate systems.
        /// </summary>
        /// <remarks>
        /// This method will examine the coordinate systems in order to
        /// construct a transformation between them. This method may fail if no
        /// path between the coordinate systems is found, using the normal failing
        /// behavior of the DCP (e.g. throwing an exception).
        /// </remarks>
        /// <param name="sourceCS">Input coordinate system</param>
        /// <param name="targetCS">Output coordinate system</param>
        ICoordinateTransformation CreateFromCoordinateSystems(ICoordinateSystem sourceCS, ICoordinateSystem targetCS);
    }

    /// <summary>
    /// Describes a coordinate transformation.
    /// </summary>
    /// <remarks>
    /// <para>This interface only describes a coordinate transformation, it does not
    /// actually perform the transform operation on points.  To transform
    /// points you must use a math transform.</para>
    /// <para>The math transform will transform positions in the source coordinate
    /// system into positions in the target coordinate system.
    /// </para>
    /// </remarks>
    public interface ICoordinateTransformation
    {
        /// <summary>
        /// Human readable description of domain in source coordinate system.
        /// </summary>
        /// <returns></returns>
        string AreaOfUse { get; }

        /// <summary>
        /// Gets an authority which defined transformation and parameter values.
        /// </summary>
        /// <remarks>
        /// An Authority is an organization that maintains definitions of Authority
        /// Codes.  For example the European Petroleum Survey Group (EPSG) maintains
        /// a database of coordinate systems, and other spatial referencing objects,
        /// where each object has a code number ID.  For example, the EPSG code for a
        /// WGS84 Lat/Lon coordinate system is '4326'.
        /// </remarks>
        string Authority { get; }

        /// <summary>
        /// Gets a code used by authority to identify transformation.
        /// </summary>
        /// <remarks>
        /// <para>The AuthorityCode is a compact string defined by an Authority to reference
        /// a particular spatial reference object.  For example, the European Survey
        /// Group (EPSG) authority uses 32 bit integers to reference coordinate systems,
        /// so all their code strings will consist of a few digits.  The EPSG code for
        /// WGS84 Lat/Lon is '4326'.</para>
        /// <para> An empty string is used for no code.</para></remarks>
        long AuthorityCode { get; }

        /// <summary>
        /// Gets math transform.
        /// </summary>
        IMathTransform MathTransform { get; }

        /// <summary>
        /// Gets the name of transformation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the provider-supplied remarks.
        /// </summary>
        string Remarks { get; }

        /// <summary>
        /// Gets the source coordinate system.
        /// </summary>
        ICoordinateSystem SourceCS { get; }

        /// <summary>
        /// Gets the target coordinate system.
        /// </summary>
        ICoordinateSystem TargetCS { get; }

        /// <summary>
        /// Gets the semantic type of transform.
        /// </summary>
        /// <remarks>For example, a datum transformation or a coordinate conversion</remarks>
        TransformType TransformType { get; }
    }

    /// <summary>
    /// Flags indicating parts of domain covered by a convex hull.
    /// </summary>
    /// <remarks>
    /// These flags can be combined.  For example, the value 3 corresponds to
    /// a combination of IDF_Inside and MF_DF_Outside, which means that some
    /// parts of the convex hull are inside the domain, and some parts of the
    /// convex hull are outside the domain.
    /// </remarks>
    public enum DomainFlags : int
    {
        /// <summary>
        /// At least one point in a convex hull is inside the transform's domain.
        /// </summary>
        Inside = 1,

        /// <summary>
        /// At least one point in a convex hull is outside the transform's domain.
        /// </summary>
        Outside = 2,

        /// <summary>
        /// At least one point in a convex hull is not transformed continuously.
        /// </summary>
        /// <remarks>
        /// As an example, consider a "Longitude_Rotation" transform which adjusts
        /// longitude coordinates to take account of a change in Prime Meridian.
        /// If the rotation is 5 degrees east, then the point (Lat=175,Lon=0)
        /// is not transformed continuously, since it is on the meridian line
        /// which will be split at +180/-180 degrees.
        /// </remarks>
        Discontinuous = 4
    }


    /// <summary>
    /// Abstract class for creating multi-dimensional coordinate points transformations.
    /// </summary>
    /// <remarks>
    /// If a client application wishes to query the source and target coordinate
    /// systems of a transformation, then it should keep hold of the
    /// <see cref="MapAround.CoordinateSystems.Transformations.ICoordinateTransformation" /> interface, and use the contained
    /// math transform object whenever it wishes to perform a transform.
    /// </remarks>
    public abstract class MathTransform : IMathTransform
    {
        #region IMathTransform Members

        /// <summary>
        /// Gets the dimension of input points.
        /// </summary>
        /// <returns>The dimension of input points</returns>
        public virtual int DimSource
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the dimension of output points.
        /// </summary>
        /// <returns>The dimension of output points</returns>
        public virtual int DimTarget
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Indicates whether this transform does not move any points.
        /// </summary>
        /// <returns>True if the transform does not move any points, else otherwise</returns>
        public virtual bool Identity()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public abstract string WKT { get; }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public abstract string XML { get; }

        /// <summary>
        /// Gets the derivative of this transform at a point.
        /// </summary>
        /// <remarks>
        /// If the transform does not have a well-defined derivative at the point,
        /// then this function should fail in the usual way for the DCP.
        /// The derivative is the matrix of the non-translating portion of the
        /// approximate affine map at the point.  The matrix will have dimensions
        /// corresponding to the source and target coordinate systems.
        /// If the input dimension is M, and the output dimension is N, then
        /// the matrix will have size [M][N].  The elements of the matrix
        /// {elt[n][m] : n=0..(N-1)} form a vector in the output space which is
        /// parallel to the displacement caused by a small change in the m'th
        /// ordinate in the input space.
        /// </remarks>
        /// <param name="point">Point in domain at which to get derivative.</param>
        /// <returns>An array containing the derivative values</returns>
        public virtual double[,] Derivative(double[] point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets transformed convex hull.
        /// </summary>
        /// <remarks>
        ///  <para>The supplied ordinates are interpreted as a sequence of points, which
        ///  generates a convex hull in the source space.  The returned sequence of
        ///  ordinates represents a convex hull in the output space.  The number of
        ///  output points will often be different from the number of input points.
        ///  Each of the input points should be inside the valid domain (this can be
        ///  checked by testing the points' domain flags individually).  However,
        ///  the convex hull of the input points may go outside the valid domain.
        ///  The returned convex hull should contain the transformed image of the
        ///  intersection of the source convex hull and the source domain. </para>
        ///
        ///  <para>A convex hull is a shape in a coordinate system, where if two positions A
        ///  and B are inside the shape, then all positions in the straight line
        ///  between A and B are also inside the shape.  So in 3D a cube and a sphere
        ///  are both convex hulls.  Other less obvious examples of convex hulls are
        ///  straight lines, and single points.  (A single point is a convex hull,
        ///  because the positions A and B must both be the same - i.e. the point
        ///  itself.  So the straight line between A and B has zero length.)</para>
        ///
        /// <para>Some examples of shapes that are NOT convex hulls are donuts, and horseshoes.</para>
        /// </remarks>
        /// <param name="points">Packed ordinates of points used to generate convex hull</param>
        public virtual List<double> GetCodomainConvexHull(List<double> points)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets flags classifying domain points within a convex hull.
        /// </summary>
        /// <remarks>
        /// The supplied ordinates are interpreted as a sequence of points, which
        /// generates a convex hull in the source space.  Conceptually, each of the
        /// (usually infinite) points inside the convex hull is then tested against
        /// the source domain.  The flags of all these tests are then combined.  In
        /// practice, implementations of different transforms will use different
        /// short-cuts to avoid doing an infinite number of tests.
        /// </remarks>
        /// <param name="points">Packed ordinates of points used to generate convex hull.</param>
        public virtual DomainFlags GetDomainFlags(List<double> points)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// However, all cartographic projections should succeed.
        /// </remarks>
        public abstract IMathTransform Inverse();

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        public abstract double[] Transform(double[] point);

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <remarks>
        /// This method is provided for efficiently transforming many points.
        /// The supplied array of ordinal values will contain packed ordinal
        /// values.  For example, if the source dimension is 3, then the ordinals
        /// will be packed in this order (x0,y0,z0,x1,y1,z1 ...).  The size
        /// of the passed array must be an integer multiple of DimSource.
        /// The returned ordinal values are packed in a similar way.
        /// In some DCPs. the ordinals may be transformed in-place, and the
        /// returned array may be the same as the passed array.
        /// So any client code should not attempt to reuse the passed ordinal
        /// values (although they can certainly reuse the passed array).
        /// If there is any problem then the server implementation will throw an
        /// exception.  If this happens then the client should not make any
        /// assumptions about the state of the ordinal values.
        /// </remarks>
        /// <param name="points">Packed ordinates of points to transform</param>
        public abstract List<double[]> TransformList(List<double[]> points);

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        public abstract void Invert();

        /// <summary>
        /// R2D
        /// </summary>
        protected const double R2D = 180 / Math.PI;

        #endregion
    }

    /// <summary>
    /// The IGeographicTransform interface is implemented on geographic transformation
    /// objects and implements datum transformations between geographic coordinate systems.
    /// </summary>
    public interface IGeographicTransform : IInfo
    {
        /// <summary>
        /// Gets or sets source geographic coordinate system for the transformation.
        /// </summary>
        IGeographicCoordinateSystem SourceGCS { get; set; }

        /// <summary>
        /// Gets or sets the target geographic coordinate system for the transformation.
        /// </summary>
        IGeographicCoordinateSystem TargetGCS { get; set; }

        /// <summary>
        /// Returns an accessor interface to the parameters for this geographic transformation.
        /// </summary>
        IParameterInfo ParameterInfo { get; }

        /// <summary>
        /// Transforms an array of points from the source geographic coordinate system
        /// to the target geographic coordinate system.
        /// </summary>
        /// <param name="points">Points in the source geographic coordinate system</param>
        /// <returns>Points in the target geographic coordinate system</returns>
        List<double[]> Forward(List<double[]> points);


        /// <summary>
        /// Transforms an array of points from the target geographic coordinate system
        /// to the source geographic coordinate system.
        /// </summary>
        /// <param name="points">Points in the target geographic coordinate system</param>
        /// <returns>Points in the source geographic coordinate system</returns>
        List<double[]> Inverse(List<double[]> points);
    }

    /// <summary>
    /// Implements datum transformations between geographic coordinate systems.
    /// Convert geographic coordinate systems.
    /// </summary>
    public class GeographicTransform : MathTransform
    {
        internal GeographicTransform(IGeographicCoordinateSystem sourceGCS, IGeographicCoordinateSystem targetGCS)
        {
            _sourceGCS = sourceGCS;
            _targetGCS = targetGCS;
        }

        #region IGeographicTransform Members

        private IGeographicCoordinateSystem _sourceGCS;

        /// <summary>
        /// Gets or sets source geographic coordinate system for the transformation.
        /// </summary>
        public IGeographicCoordinateSystem SourceGCS
        {
            get { return _sourceGCS; }
            set { _sourceGCS = value; }
        }

        private IGeographicCoordinateSystem _targetGCS;

        /// <summary>
        /// Gets or sets the target geographic coordinate system for the transformation.
        /// </summary>
        public IGeographicCoordinateSystem TargetGCS
        {
            get { return _targetGCS; }
            set { _targetGCS = value; }
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// However, all cartographic projections should succeed.
        /// </remarks>
        public override IMathTransform Inverse()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        public override double[] Transform(double[] point)
        {
            double[] pOut = (double[])point.Clone();
            pOut[0] /= SourceGCS.AngularUnit.RadiansPerUnit;
            pOut[0] -= SourceGCS.PrimeMeridian.Longitude / SourceGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
            pOut[0] += TargetGCS.PrimeMeridian.Longitude / TargetGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
            pOut[0] *= SourceGCS.AngularUnit.RadiansPerUnit;
            return pOut;
        }

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <param name="points">Packed ordinates of points to transform</param>
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> trans = new List<double[]>(points.Count);
            foreach (double[] p in points)
                trans.Add(Transform(p));
            return trans;
        }

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        public override void Invert()
        {
            throw new NotImplementedException();
        }

    }

    /// <summary>
    /// Implements a geocentric transformation.
    /// </summary>
    /// <remarks>
    /// <para>Latitude, Longitude and ellipsoidal height in terms of a 3-dimensional geographic system
    /// may by expressed in terms of a geocentric (earth centered) Cartesian coordinate reference system
    /// X, Y, Z with the Z axis corresponding to the earth's rotation axis positive northwards, the X
    /// axis through the intersection of the prime meridian and equator, and the Y axis through
    /// the intersection of the equator with longitude 90 degrees east. The geographic and geocentric
    /// systems are based on the same geodetic datum.</para>
    /// <para>Geocentric coordinate reference systems are conventionally taken to be defined with the X
    /// axis through the intersection of the Greenwich meridian and equator. This requires that the equivalent
    /// geographic coordinate reference systems based on a non-Greenwich prime meridian should first be
    /// transformed to their Greenwich equivalent. Geocentric coordinates X, Y and Z take their units from
    /// the units of the ellipsoid axes (a and b). As it is conventional for X, Y and Z to be in metres,
    /// if the ellipsoid axis dimensions are given in another linear unit they should first be converted
    /// to metres.</para>
    /// </remarks>
    internal class GeocentricTransform : MathTransform
    {
        private const double COS_67P5 = 0.38268343236508977;    /* cosine of 67.5 degrees */
        private const double AD_C = 1.0026000;                  /* Toms region 1 constant */

        /// <summary>
        /// 
        /// </summary>
        protected bool _isInverse = false;

        private double es;              // Eccentricity squared : (a^2 - b^2)/a^2
        private double semiMajor;		// major axis
        private double semiMinor;		// minor axis
        private double ab;				// Semi_major / semi_minor
        private double ba;				// Semi_minor / semi_major
        private double ses;             // Second eccentricity squared : (a^2 - b^2)/b^2    

        /// <summary>
        /// 
        /// </summary>
        protected List<ProjectionParameter> _parameters;

        /// <summary>
        /// 
        /// </summary>
        protected MathTransform _inverse;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.GeocentricTransform.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters)</param>
        public GeocentricTransform(List<ProjectionParameter> parameters, bool isInverse)
            : this(parameters)
        {
            _isInverse = isInverse;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.GeocentricTransform.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection</param>
        internal GeocentricTransform(List<ProjectionParameter> parameters)
        {
            this._parameters = parameters;
            semiMajor = parameters.Find(delegate(ProjectionParameter par)
            {
                // Do not remove the following lines containing "this._parameters = parameters;"
                // There is an issue deploying code with anonymous delegates to 
                // SQLCLR because they're compiled using a writable static field 
                // (which is not allowed in SQLCLR SAFE mode).
                // To workaround this, we will use a harmless reference to the
                // _parameters field inside the anonymous delegate code making 
                // the compiler generates a private nested class with a function
                // that is used as the delegate.
                // For details, see http://www.hedgate.net/articles/2006/01/27/troubles-with-shared-state-and-anonymous-delegates-in-sqlclr
#pragma warning disable 1717
                this._parameters = parameters;
#pragma warning restore 1717

                return par.Name.Equals("semi_major", StringComparison.OrdinalIgnoreCase);
            }).Value;

            semiMinor = parameters.Find(delegate(ProjectionParameter par)
            {
#pragma warning disable 1717
                this._parameters = parameters; // See explanation above.
#pragma warning restore 1717
                return par.Name.Equals("semi_minor", StringComparison.OrdinalIgnoreCase);
            }).Value;

            es = 1.0 - (semiMinor * semiMinor) / (semiMajor * semiMajor); //e^2
            ses = (Math.Pow(semiMajor, 2) - Math.Pow(semiMinor, 2)) / Math.Pow(semiMinor, 2);
            ba = semiMinor / semiMajor;
            ab = semiMajor / semiMinor;
        }


        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// </remarks>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new GeocentricTransform(this._parameters, !_isInverse);
            return _inverse;
        }

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lonlat">The point in decimal degrees.</param>
        /// <returns>Point in projected meters</returns>
        private double[] DegreesToMeters(double[] lonlat)
        {
            double lon = MathUtils.Degrees.ToRadians(lonlat[0]);
            double lat = MathUtils.Degrees.ToRadians(lonlat[1]);
            double h = lonlat.Length < 3 ? 0 : lonlat[2].Equals(Double.NaN) ? 0 : lonlat[2];
            double v = semiMajor / Math.Sqrt(1 - es * Math.Pow(Math.Sin(lat), 2));
            double x = (v + h) * Math.Cos(lat) * Math.Cos(lon);
            double y = (v + h) * Math.Cos(lat) * Math.Sin(lon);
            double z = ((1 - es) * v + h) * Math.Sin(lat);
            return new double[] { x, y, z, };
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="pnt">Point in meters</param>
        /// <returns>Transformed point in decimal degrees</returns>
        private double[] MetersToDegrees(double[] pnt)
        {
            bool At_Pole = false; // indicates whether location is in polar region */
            double Z = pnt.Length < 3 ? 0 : pnt[2].Equals(Double.NaN) ? 0 : pnt[2];

            double lon = 0;
            double lat = 0;
            double Height = 0;
            if (pnt[0] != 0.0)
                lon = Math.Atan2(pnt[1], pnt[0]);
            else
            {
                if (pnt[1] > 0)
                    lon = Math.PI / 2;
                else if (pnt[1] < 0)
                    lon = -Math.PI * 0.5;
                else
                {
                    At_Pole = true;
                    lon = 0.0;
                    if (Z > 0.0)
                    {   /* north pole */
                        lat = Math.PI * 0.5;
                    }
                    else if (Z < 0.0)
                    {   /* south pole */
                        lat = -Math.PI * 0.5;
                    }
                    else
                    {   /* center of earth */
                        return new double[] 
                        { 
                            MathUtils.Radians.ToDegrees(lon), 
                            MathUtils.Radians.ToDegrees(Math.PI * 0.5), -semiMinor, 
                        };
                    }
                }
            }
            double W2 = pnt[0] * pnt[0] + pnt[1] * pnt[1]; // Square of distance from Z axis
            double W = Math.Sqrt(W2); // distance from Z axis
            double T0 = Z * AD_C; // initial estimate of vertical component
            double S0 = Math.Sqrt(T0 * T0 + W2); //initial estimate of horizontal component
            double Sin_B0 = T0 / S0; //sin(B0), B0 is estimate of Bowring aux variable
            double Cos_B0 = W / S0; //cos(B0)
            double Sin3_B0 = Math.Pow(Sin_B0, 3);
            double T1 = Z + semiMinor * ses * Sin3_B0; //corrected estimate of vertical component
            double Sum = W - semiMajor * es * Cos_B0 * Cos_B0 * Cos_B0; //numerator of cos(phi1)
            double S1 = Math.Sqrt(T1 * T1 + Sum * Sum); //corrected estimate of horizontal component
            double Sin_p1 = T1 / S1; //sin(phi1), phi1 is estimated latitude
            double Cos_p1 = Sum / S1; //cos(phi1)
            double Rn = semiMajor / Math.Sqrt(1.0 - es * Sin_p1 * Sin_p1); //Earth radius at location
            if (Cos_p1 >= COS_67P5)
                Height = W / Cos_p1 - Rn;
            else if (Cos_p1 <= -COS_67P5)
                Height = W / -Cos_p1 - Rn;
            else Height = Z / Sin_p1 + Rn * (es - 1.0);
            if (!At_Pole)
                lat = Math.Atan(Sin_p1 / Cos_p1);
            return new double[] 
            { 
                MathUtils.Radians.ToDegrees(lon), 
                MathUtils.Radians.ToDegrees(lat), 
                Height
            };
        }

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        public override double[] Transform(double[] point)
        {
            if (!_isInverse)
                return this.DegreesToMeters(point);
            else return this.MetersToDegrees(point);
        }

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <param name="points">Packed ordinates of points to transform</param>
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> result = new List<double[]>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                double[] point = points[i];
                result.Add(Transform(point));
            }
            return result;
        }

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        public override void Invert()
        {
            _isInverse = !_isInverse;
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get { throw new NotImplementedException(); }
        }
    }

    /// <summary>
    /// Represents a datum transformations.
    /// </summary>
    internal class DatumTransform : MathTransform
    {
        protected IMathTransform _inverse;
        private Wgs84ConversionInfo _toWgs84;
        double[] v;

        private bool _isInverse = false;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.DatumTransform
        /// Instantiates DatumTransform.
        /// </summary>
        /// <param name="towgs84">An object representing transformation to WGS84</param>
        public DatumTransform(Wgs84ConversionInfo towgs84)
            : this(towgs84, false)
        {
        }

        private DatumTransform(Wgs84ConversionInfo towgs84, bool isInverse)
        {
            _toWgs84 = towgs84;
            v = _toWgs84.GetAffineTransform();
            _isInverse = isInverse;
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        public override string WKT
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        public override string XML
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// </remarks>
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new DatumTransform(_toWgs84, !_isInverse);
            return _inverse;
        }

        private double[] apply(double[] p)
        {
            return new double[] {
				v[0] * p[0] - v[3] * p[1] + v[2] * p[2] + v[4],
				v[3] * p[0] + v[0] * p[1] - v[1] * p[2] + v[5],
			   -v[2] * p[0] + v[1] * p[1] + v[0] * p[2] + v[6], };
        }

        private double[] applyInverted(double[] p)
        {
            return new double[] {
				v[0] * p[0] + v[3] * p[1] - v[2] * p[2] - v[4],
			   -v[3] * p[0] + v[0] * p[1] + v[1] * p[2] - v[5],
			    v[2] * p[0] - v[1] * p[1] + v[0] * p[2] - v[6], };
        }

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        public override double[] Transform(double[] point)
        {
            if (!_isInverse)
                return apply(point);
            else return applyInverted(point);
        }

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <param name="points">Packed ordinates of points to transform</param>
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> pnts = new List<double[]>(points.Count);
            foreach (double[] p in points)
                pnts.Add(Transform(p));
            return pnts;
        }

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        public override void Invert()
        {
            _isInverse = !_isInverse;
        }
    }

    /// <summary>
    /// Creates coordinate transformations.
    /// </summary>
    public class CoordinateTransformationFactory : ICoordinateTransformationFactory
    {
        #region ICoordinateTransformationFactory Members

        /// <summary>
        /// Creates a transformation between two coordinate systems.
        /// </summary>
        /// <remarks>
        /// This method will examine the coordinate systems in order to construct
        /// a transformation between them. This method may fail if no path between
        /// the coordinate systems is found, using the normal failing behavior of
        /// the DCP (e.g. throwing an exception).</remarks>
        /// <param name="sourceCS">Source coordinate system</param>
        /// <param name="targetCS">Target coordinate system</param>
        /// <returns>An object representing requested transformation</returns>
        public ICoordinateTransformation CreateFromCoordinateSystems(ICoordinateSystem sourceCS, ICoordinateSystem targetCS)
        {
            if (sourceCS is IProjectedCoordinateSystem && targetCS is IGeographicCoordinateSystem) //Projected -> Geographic
                return Proj2Geog((IProjectedCoordinateSystem)sourceCS, (IGeographicCoordinateSystem)targetCS);
            else if (sourceCS is IGeographicCoordinateSystem && targetCS is IProjectedCoordinateSystem) //Geographic -> Projected
                return Geog2Proj((IGeographicCoordinateSystem)sourceCS, (IProjectedCoordinateSystem)targetCS);

            else if (sourceCS is IGeographicCoordinateSystem && targetCS is IGeocentricCoordinateSystem) //Geocentric -> Geographic
                return Geog2Geoc((IGeographicCoordinateSystem)sourceCS, (IGeocentricCoordinateSystem)targetCS);

            else if (sourceCS is IGeocentricCoordinateSystem && targetCS is IGeographicCoordinateSystem) //Geocentric -> Geographic
                return Geoc2Geog((IGeocentricCoordinateSystem)sourceCS, (IGeographicCoordinateSystem)targetCS);

            else if (sourceCS is IProjectedCoordinateSystem && targetCS is IProjectedCoordinateSystem) //Projected -> Projected
                return Proj2Proj((sourceCS as IProjectedCoordinateSystem), (targetCS as IProjectedCoordinateSystem));

            else if (sourceCS is IGeocentricCoordinateSystem && targetCS is IGeocentricCoordinateSystem) //Geocentric -> Geocentric
                return CreateGeoc2Geoc((IGeocentricCoordinateSystem)sourceCS, (IGeocentricCoordinateSystem)targetCS);

            else if (sourceCS is IGeographicCoordinateSystem && targetCS is IGeographicCoordinateSystem) //Geographic -> Geographic
                return CreateGeog2Geog(sourceCS as IGeographicCoordinateSystem, targetCS as IGeographicCoordinateSystem);

            throw new NotSupportedException("The transformation between these coordinate systems are not supported");
        }

        #endregion

        #region Methods for converting between specific systems

        private static ICoordinateTransformation Geog2Geoc(IGeographicCoordinateSystem source, IGeocentricCoordinateSystem target)
        {
            IMathTransform geocMathTransform = CreateCoordinateOperation(target);
            return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, String.Empty, String.Empty, -1, String.Empty, String.Empty);
        }

        private static ICoordinateTransformation Geoc2Geog(IGeocentricCoordinateSystem source, IGeographicCoordinateSystem target)
        {
            IMathTransform geocMathTransform = CreateCoordinateOperation(source).Inverse();
            return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, String.Empty, String.Empty, -1, String.Empty, String.Empty);
        }

        private static ICoordinateTransformation Proj2Proj(IProjectedCoordinateSystem source, IProjectedCoordinateSystem target)
        {
            ConcatenatedTransform ct = new ConcatenatedTransform();
            CoordinateTransformationFactory ctFac = new CoordinateTransformationFactory();
            //First transform from projection to geographic
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
            //Transform geographic to geographic:
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem, target.GeographicCoordinateSystem));
            //Transform to new projection
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));

            return new CoordinateTransformation(source,
                target, TransformType.Transformation, ct,
                String.Empty, String.Empty, -1, String.Empty, String.Empty);
        }

        private static ICoordinateTransformation Geog2Proj(IGeographicCoordinateSystem source, IProjectedCoordinateSystem target)
        {
            if (source.EqualParams(target.GeographicCoordinateSystem))
            {
                IMathTransform mathTransform = CreateCoordinateOperation(target.Projection, target.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid, target.LinearUnit);
                return new CoordinateTransformation(source, target, TransformType.Transformation, mathTransform,
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
            else
            {
                // Geographic coordinatesystems differ - Create concatenated transform
                ConcatenatedTransform ct = new ConcatenatedTransform();
                CoordinateTransformationFactory ctFac = new CoordinateTransformationFactory();
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, target.GeographicCoordinateSystem));
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));
                return new CoordinateTransformation(source,
                    target, TransformType.Transformation, ct,
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
        }

        private static ICoordinateTransformation Proj2Geog(IProjectedCoordinateSystem source, IGeographicCoordinateSystem target)
        {
            if (source.GeographicCoordinateSystem.EqualParams(target))
            {
                IMathTransform mathTransform = CreateCoordinateOperation(source.Projection, source.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid, source.LinearUnit).Inverse();
                return new CoordinateTransformation(source, target, TransformType.Transformation, mathTransform,
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
            else
            {	// Geographic coordinatesystems differ - Create concatenated transform
                ConcatenatedTransform ct = new ConcatenatedTransform();
                CoordinateTransformationFactory ctFac = new CoordinateTransformationFactory();
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem, target));
                return new CoordinateTransformation(source,
                    target, TransformType.Transformation, ct,
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
        }

        /// <summary>
        /// Creates geographic to geographic transformation.
        /// </summary>
        /// <remarks>Adds a datum shift if nessesary</remarks>
        /// <param name="source">Source coordinate system</param>
        /// <param name="target">TargetCoordinate system</param>
        /// <returns>The requested transformation</returns>
        private ICoordinateTransformation CreateGeog2Geog(IGeographicCoordinateSystem source, IGeographicCoordinateSystem target)
        {
            if (source.HorizontalDatum.EqualParams(target.HorizontalDatum))
            {
                //datum shift is not needed
                return new CoordinateTransformation(source,
                    target, TransformType.Conversion, new GeographicTransform(source, target),
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
            else
            {
                //datum shift
                // transformation into a geocentric system, datum shift and return to the system of geographical
                CoordinateTransformationFactory ctFac = new CoordinateTransformationFactory();
                CoordinateSystemFactory cFac = new CoordinateSystemFactory();
                IGeocentricCoordinateSystem sourceCentric = cFac.CreateGeocentricCoordinateSystem(source.HorizontalDatum.Name + " Geocentric",
                    source.HorizontalDatum, LinearUnit.Metre, source.PrimeMeridian);
                IGeocentricCoordinateSystem targetCentric = cFac.CreateGeocentricCoordinateSystem(target.HorizontalDatum.Name + " Geocentric",
                    target.HorizontalDatum, LinearUnit.Metre, source.PrimeMeridian);
                ConcatenatedTransform ct = new ConcatenatedTransform();
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, sourceCentric));
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(sourceCentric, targetCentric));
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(targetCentric, target));
                return new CoordinateTransformation(source,
                    target, TransformType.Transformation, ct,
                    String.Empty, String.Empty, -1, String.Empty, String.Empty);
            }
        }

        /// <summary>
        /// Creates geocentric to geocentric transformation.
        /// </summary>
        /// <remarks>Adds a datum shift if nessesary</remarks>
        /// <param name="source">Source coordinate system</param>
        /// <param name="target">TargetCoordinate system</param>
        /// <returns>The requested transformation</returns>
        private static ICoordinateTransformation CreateGeoc2Geoc(IGeocentricCoordinateSystem source, IGeocentricCoordinateSystem target)
        {
            ConcatenatedTransform ct = new ConcatenatedTransform();

            //Does source has a datum different from WGS84 and is there a shift specified?
            if (source.HorizontalDatum.Wgs84Parameters != null && !source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
                ct.CoordinateTransformationList.Add(
                    new CoordinateTransformation(
                    ((target.HorizontalDatum.Wgs84Parameters == null || target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? target : GeocentricCoordinateSystem.WGS84),
                    source, TransformType.Transformation,
                        new DatumTransform(source.HorizontalDatum.Wgs84Parameters)
                        , "", "", -1, "", ""));

            //Does target has a datum different from WGS84 and is there a shift specified?
            if (target.HorizontalDatum.Wgs84Parameters != null && !target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
                ct.CoordinateTransformationList.Add(
                    new CoordinateTransformation(
                    ((source.HorizontalDatum.Wgs84Parameters == null || source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? source : GeocentricCoordinateSystem.WGS84),
                    target,
                    TransformType.Transformation,
                        new DatumTransform(target.HorizontalDatum.Wgs84Parameters).Inverse()
                        , "", "", -1, "", ""));

            if (ct.CoordinateTransformationList.Count == 1) //Since we only have one shift, lets just return the datumshift from/to wgs84
                return new CoordinateTransformation(source, target, TransformType.ConversionAndTransformation, ct.CoordinateTransformationList[0].MathTransform, "", "", -1, "", "");
            else
                return new CoordinateTransformation(source, target, TransformType.ConversionAndTransformation, ct, "", "", -1, "", "");
        }

        #endregion

        private static IMathTransform CreateCoordinateOperation(IGeocentricCoordinateSystem geo)
        {
            List<ProjectionParameter> parameterList = new List<ProjectionParameter>(2);
            parameterList.Add(new ProjectionParameter("semi_major", geo.HorizontalDatum.Ellipsoid.SemiMajorAxis));
            parameterList.Add(new ProjectionParameter("semi_minor", geo.HorizontalDatum.Ellipsoid.SemiMinorAxis));
            return new GeocentricTransform(parameterList);
        }

        private static IMathTransform CreateCoordinateOperation(IProjection projection, IEllipsoid ellipsoid, ILinearUnit unit)
        {
            List<ProjectionParameter> parameterList = new List<ProjectionParameter>(projection.NumParameters);
            for (int i = 0; i < projection.NumParameters; i++)
                parameterList.Add(projection.GetParameter(i));

            parameterList.Add(new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis));
            parameterList.Add(new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis));
            parameterList.Add(new ProjectionParameter("unit", unit.MetersPerUnit));
            IMathTransform transform = null;
            switch (projection.ClassName.ToLower(CultureInfo.InvariantCulture).Replace(' ', '_'))
            {
                case "mercator":
                case "mercator_1sp":
                case "mercator_2sp":
                    transform = new Mercator(parameterList);
                    break;
                case "transverse_mercator":
                    transform = new TransverseMercator(parameterList);
                    break;
                case "albers":
                case "albers_conic_equal_area":
                    transform = new AlbersProjection(parameterList);
                    break;
                case "lambert_conformal_conic":
                case "lambert_conformal_conic_2sp":
                case "lambert_conic_conformal_(2sp)":
                    transform = new LambertConformalConic2SP(parameterList);
                    break;
                default:
                    throw new NotSupportedException(String.Format("Projection {0} is not supported.", projection.ClassName));
            }
            return transform;
        }
    }

    /// <summary>
    /// Creates coordinate transformation objects from codes.
    /// </summary>
    /// <remarks>
    /// The codes are maintained by an external authority.
    /// A commonly used authority is EPSG, which is also used in the GeoTIFF
    /// standard.
    /// </remarks>
    public interface CoordinateTransformationAuthorityFactory
    {
    }

    /// <summary>
    /// Describes a coordinate transformation. This class only describes a
    /// coordinate transformation, it does not actually perform the transform
    /// operation on points. To transform points you must use a 
    /// <see cref="P:MapAround.CoordinateSystems.Transformations.CoordinateTransformation.MathTransform" />.
    /// </summary>
    public class CoordinateTransformation : ICoordinateTransformation
    {
        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.CoordinateTransformation
        /// </summary>
        /// <param name="sourceCS">Source coordinate system</param>
        /// <param name="targetCS">Target coordinate system</param>
        /// <param name="transformType">Transformation type</param>
        /// <param name="mathTransform">Math transform</param>
        /// <param name="name">Name of transform</param>
        /// <param name="authority">Authority</param>
        /// <param name="authorityCode">Authority code</param>
        /// <param name="areaOfUse">Area of use</param>
        /// <param name="remarks">Remarks</param>
        internal CoordinateTransformation(ICoordinateSystem sourceCS, ICoordinateSystem targetCS, TransformType transformType, IMathTransform mathTransform,
                                        string name, string authority, long authorityCode, string areaOfUse, string remarks)
            : base()
        {
            _targetCS = targetCS;
            _sourceCS = sourceCS;
            _transformType = transformType;
            _mathTransform = mathTransform;
            _name = name;
            _authority = authority;
            _authorityCode = authorityCode;
            _areaOfUse = areaOfUse;
            _remarks = remarks;
        }



        #region ICoordinateTransformation Members

        private string _areaOfUse;

        /// <summary>
        /// Human readable description of domain.
        /// </summary>
        /// <returns></returns>	
        public string AreaOfUse
        {
            get { return _areaOfUse; }
        }

        private string _authority;

        /// <summary>
        /// Authority which defined transformation and parameter values.
        /// </summary>
        /// <remarks>
        /// An Authority is an organization that maintains definitions of Authority Codes. 
        /// For example the European Petroleum Survey Group (EPSG) maintains a database of 
        /// coordinate systems, and other spatial referencing objects, where each object has 
        /// a code number ID. For example, the EPSG code for a WGS84 Lat/Lon coordinate system 
        /// is '4326'
        /// </remarks>
        public string Authority
        {
            get { return _authority; }
        }

        private long _authorityCode;

        /// <summary>
        /// Code used by authority to identify transformation. An empty string is used for no code.
        /// </summary>
        /// <remarks>The AuthorityCode is a compact string defined by an Authority to reference 
        /// a particular spatial reference object. For example, the European Survey Group (EPSG) 
        /// authority uses 32 bit integers to reference coordinate systems, so all their code 
        /// strings will consist of a few digits. The EPSG code for WGS84 Lat/Lon is '4326'.
        /// </remarks>
        public long AuthorityCode
        {
            get { return _authorityCode; }
        }

        private IMathTransform _mathTransform;

        /// <summary>
        /// Gets math transform.
        /// </summary>
        public IMathTransform MathTransform
        {
            get { return _mathTransform; }
        }

        private string _name;


        /// <summary>
        /// Gets the name of transformation.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        private string _remarks;

        /// <summary>
        /// Gets the provider-supplied remarks.
        /// </summary>
        public string Remarks
        {
            get { return _remarks; }
        }

        private ICoordinateSystem _sourceCS;

        /// <summary>
        /// Gets the source coordinate system.
        /// </summary>
        public ICoordinateSystem SourceCS
        {
            get { return _sourceCS; }
        }

        private ICoordinateSystem _targetCS;

        /// <summary>
        /// Gets the target coordinate system.
        /// </summary>
        public ICoordinateSystem TargetCS
        {
            get { return _targetCS; }
        }

        private TransformType _transformType;

        /// <summary>
        /// Gets the semantic type of the transform.
        /// </summary>
        public TransformType TransformType
        {
            get { return _transformType; }
        }

        #endregion
    }

    /// <summary>
    /// Represents a concatenation of transformations.
    /// </summary>
    /// <remarks>
    /// A concatenated transform acts in the same way as applying transforms,
    /// one after the other.
    ///
    /// The dimension of the output space of the each transform must match
    /// the dimension of the input space in the next transform.
    /// </remarks>
    internal class ConcatenatedTransform : MathTransform
    {
        /// <summary>
        /// The inverse transformation.
        /// </summary>
        protected IMathTransform InverseTransform;

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.ConcatenatedTransform. 
        /// </summary>
        public ConcatenatedTransform() :
            this(new List<ICoordinateTransformation>()) { }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.ConcatenatedTransform. 
        /// </summary>
        /// <param name="transformlist"></param>
        public ConcatenatedTransform(List<ICoordinateTransformation> transformlist)
        {
            _coordinateTransformationList = transformlist;
        }

        private List<ICoordinateTransformation> _coordinateTransformationList;

        /// <summary>
        /// Gets or sets a list of coordinate transformations.
        /// This list define transformation sequence that will be
        /// applied by the current coordinate transformation.
        /// </summary>
        public List<ICoordinateTransformation> CoordinateTransformationList
        {
            get { return _coordinateTransformationList; }
            set
            {
                _coordinateTransformationList = value;
                InverseTransform = null;
            }
        }

        /// <summary>
        /// Transforms a coordinate point.
        /// </summary>
        /// <remarks>The passed parameter point should not be modified.</remarks>
        /// <param name="point">An array containing the point coordinates to transform</param>
        public override double[] Transform(double[] point)
        {
            foreach (ICoordinateTransformation ct in _coordinateTransformationList)
                point = ct.MathTransform.Transform(point);
            return point;
        }

        /// <summary>
        /// Transforms a list of coordinate point ordinal values.
        /// </summary>
        /// <param name="points">Packed ordinates of points to transform</param>
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> pnts = new List<double[]>(points.Count);
            pnts.AddRange(points);
            foreach (ICoordinateTransformation ct in _coordinateTransformationList)
                pnts = ct.MathTransform.TransformList(pnts);
            return pnts;
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transform is not one to one.
        /// </remarks>
        public override IMathTransform Inverse()
        {
            if (InverseTransform == null)
            {
                InverseTransform = new ConcatenatedTransform(_coordinateTransformationList);
                InverseTransform.Invert();
            }
            return InverseTransform;
        }

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        public override void Invert()
        {
            _coordinateTransformationList.Reverse();
            foreach (ICoordinateTransformation ic in _coordinateTransformationList)
                ic.MathTransform.Invert();
        }

        /// <summary>
        /// Gets a well-known text representation of this object.
        /// </summary>
        /// <exception cref="NotImplementedException">Throws always</exception>
        public override string WKT
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <exception cref="NotImplementedException">Throws always</exception>
        public override string XML
        {
            get { throw new NotImplementedException(); }
        }
    }
}