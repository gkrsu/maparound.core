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
** File: Useful.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Description: Some useful coordinate transformations
**
=============================================================================*/


namespace MapAround.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using MapAround.CoordinateSystems;
    using MapAround.CoordinateSystems.Projections;
#if !DEMO
    using MapAround.Geography;
#endif
    using MapAround.Geometry;
    using MapAround.MathUtils;

#if !DEMO

    /// <summary>
    /// Implements a Wagner VI (Kavraysky VII) projection transform.
    /// Used to display the World Map.
    /// <remarks>
    /// Equations of projection are:
    /// x = Cx * Lambda * (Ca + (1 - Sqrt(1 - Cb * Phi ^ 2)))
    /// y = Cy * Phi
    /// The difference between the Kavraysky VII and the Wagner VI is the Cy value.
    /// It should be different by Sqrt (3) / 2 times, other things being equal.
    /// </remarks>
    /// </summary>
    public class Wagner6 : MathTransform
    {
        private IMathTransform _inverse;
        private bool _isInverse = false;

        private double _cx = 0.94745;
        private double _cy = 0.94745;
        private double _ca = 0;
        private double _cb = 0.30396355092701331433;

        /// <summary>
        /// Gets or sets a horizontal scaling value.
        /// </summary>
        public double Cx
        {
            get { return _cx; }
        }

        /// <summary>
        /// Gets or sets a vertical scaling value.
        /// </summary>
        public double Cy
        {
            get { return _cy; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Ca
        {
            get { return _ca; }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Cb
        {
            get { return _cb; }
            set { _cb = value; }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.Wagner6.
        /// </summary>
        public Wagner6()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.Wagner6.
        /// </summary>
        /// <param name="Cx">Horizontal scaling</param>
        /// <param name="Cy">Vertical scaling</param>
        /// <param name="Ca"></param>
        /// <param name="Cb"></param>
        public Wagner6(double Cx, double Cy, double Ca, double Cb)
            : this(false)
        {
            _cx = Cx;
            _cy = Cy;
            _ca = Ca;
            _cb = Cb;
        }

        private Wagner6(bool isInverse)
        {
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
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new Wagner6(!_isInverse);
            return _inverse;
        }

        private double[] apply(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                double p1 = MathUtils.Degrees.ToRadians(p[i]);
                double p2 = MathUtils.Degrees.ToRadians(p[i + 1]);

                result[i + 1] = _cy * p2;
                result[i] = _cx * p1 * (_ca + asqrt(1 - _cb * p2 * p2));
            }

            return result;
        }

        private double[] applyInverted(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                double r2 = p[i + 1] / _cy;
                double r1 = p[i] / (_cx * (_ca + asqrt(1 - _cb * r2 * r2)));

                result[i] = MathUtils.Radians.ToDegrees(r1);
                result[i + 1] = MathUtils.Radians.ToDegrees(r2);
            }

            return result;
        }

        private static double asqrt(double v)
        {
            return ((v <= 0) ? 0 : Math.Sqrt(v));
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
    /// Implements a Robinson projection transform.
    /// Used to display the World Map.
    /// </summary>
    public class Robinson : MathTransform
    {
        private IMathTransform _inverse;
        private bool _isInverse = false;

        private const double _fxc = 0.8487;
        private const double _fyc = 1.3523;
        private const double _c1 = 11.45915590261646417544;
        private const double _rc1 = 0.08726646259971647884;
        private const int _nodes = 18;
        private const double _oneTolerance = 1.000001;
        private const double _rolerance = 1E-8;

        private static double[] _x = new[]
                    {
                        1, -5.67239e-12, -7.15511e-05, 3.11028e-06,
                        0.9986, -0.000482241, -2.4897e-05, -1.33094e-06,
                        0.9954, -0.000831031, -4.4861e-05, -9.86588e-07,
                        0.99, -0.00135363, -5.96598e-05, 3.67749e-06,
                        0.9822, -0.00167442, -4.4975e-06, -5.72394e-06,
                        0.973, -0.00214869, -9.03565e-05, 1.88767e-08,
                        0.96, -0.00305084, -9.00732e-05, 1.64869e-06,
                        0.9427, -0.00382792, -6.53428e-05, -2.61493e-06,
                        0.9216, -0.00467747, -0.000104566, 4.8122e-06,
                        0.8962, -0.00536222, -3.23834e-05, -5.43445e-06,
                        0.8679, -0.00609364, -0.0001139, 3.32521e-06,
                        0.835, -0.00698325, -6.40219e-05, 9.34582e-07,
                        0.7986, -0.00755337, -5.00038e-05, 9.35532e-07,
                        0.7597, -0.00798325, -3.59716e-05, -2.27604e-06,
                        0.7186, -0.00851366, -7.0112e-05, -8.63072e-06,
                        0.6732, -0.00986209, -0.000199572, 1.91978e-05,
                        0.6213, -0.010418, 8.83948e-05, 6.24031e-06,
                        0.5722, -0.00906601, 0.000181999, 6.24033e-06,
                        0.5322, 0, 0, 0
                    };
        private static double[] _y = new[]
                    {
                        0, 0.0124, 3.72529e-10, 1.15484e-09,
                        0.062, 0.0124001, 1.76951e-08, -5.92321e-09,
                        0.124, 0.0123998, -7.09668e-08, 2.25753e-08,
                        0.186, 0.0124008, 2.66917e-07, -8.44523e-08,
                        0.248, 0.0123971, -9.99682e-07, 3.15569e-07,
                        0.31, 0.0124108, 3.73349e-06, -1.1779e-06,
                        0.372, 0.0123598, -1.3935e-05, 4.39588e-06,
                        0.434, 0.0125501, 5.20034e-05, -1.00051e-05,
                        0.4968, 0.0123198, -9.80735e-05, 9.22397e-06,
                        0.5571, 0.0120308, 4.02857e-05, -5.2901e-06,
                        0.6176, 0.0120369, -3.90662e-05, 7.36117e-07,
                        0.6769, 0.0117015, -2.80246e-05, -8.54283e-07,
                        0.7346, 0.0113572, -4.08389e-05, -5.18524e-07,
                        0.7903, 0.0109099, -4.86169e-05, -1.0718e-06,
                        0.8435, 0.0103433, -6.46934e-05, 5.36384e-09,
                        0.8936, 0.00969679, -6.46129e-05, -8.54894e-06,
                        0.9394, 0.00840949, -0.000192847, -4.21023e-06,
                        0.9761, 0.00616525, -0.000256001, -4.21021e-06,
                        1, 0, 0, 0
                    };


        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.Robinson.
        /// </summary>
        public Robinson()
            : this(false)
        {
        }

        private Robinson(bool isInverse)
        {
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
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new Robinson(!_isInverse);
            return _inverse;
        }

        private double[] apply(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                double p1 = MathUtils.Degrees.ToRadians(p[i]);
                double p2 = MathUtils.Degrees.ToRadians(p[i + 1]);

                double dphi;

                int nodeIndex = (int)Math.Floor((dphi = Math.Abs(p2)) * _c1);

                if (nodeIndex >= _nodes)
                    nodeIndex = _nodes - 1;

                dphi = MathUtils.Radians.ToDegrees(dphi - _rc1 * nodeIndex);
                result[i] = V(_x, nodeIndex * 4, dphi) * _fxc * p1;
                result[i + 1] = V(_y, nodeIndex * 4, dphi) * _fyc;

                if (p2 < 0)
                    result[i + 1] = -result[i + 1];
            }

            return result;
        }

        private double[] applyInverted(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                double[] T = new double[4];

                result[i] = p[i] / _fxc;
                result[i + 1] = Math.Abs(p[i + 1] / _fyc);
                if (result[i + 1] >= 1)
                {
                    if (result[i + 1] > _oneTolerance)
                        throw new ApplicationException();

                    result[i + 1] =
                        p[i + 1] < 0 ? -Math.PI * 0.5 : Math.PI * 0.5;
                    result[i] /= _x[_nodes * 4];
                }
                else
                {
                    int k;
                    for (k = (int)Math.Floor(result[i + 1] * _nodes); k < 100000; )
                    {
                        if (_y[k * 4] > result[i + 1])
                            --k;
                        else
                            if (_y[(k + 1) * 4] <= result[i + 1])
                                ++k;
                            else
                                break;
                    }
                    Array.Copy(_y, k * 4, T, 0, 4);
                    double t = 5 * (result[i + 1] - T[0]) / (_y[(k + 1) * 4] - T[0]);
                    T[0] -= result[i + 1];
                    while (true)
                    {
                        double t1 = V(T, 0, t) / DV(T, 0, t);
                        t -= t1;
                        if (Math.Abs(t1) < _rolerance)
                            break;
                    }

                    result[i + 1] = 5 * k + t;
                    if (p[i + 1] < 0)
                        result[i + 1] = -result[i + 1];
                    result[i] /= V(_x, (k * 4), t);
                }

                result[i] = MathUtils.Radians.ToDegrees(result[i]);
            }

            return result;
        }

        private double V(double[] C, int iStart, double z)
        {
            return C[iStart] + z * (C[iStart + 1] + z * (C[iStart + 2] + z * C[iStart + 3]));
        }

        private double DV(double[] C, int iStart, double z)
        {
            return C[iStart + 1] + z * (C[iStart + 2] + C[iStart + 2] + z * 3 * C[iStart + 3]);
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
    /// Implements a gnomonic projection.
    /// <remarks>
    /// Gnomonic projection is used to bring the topological problems 
    /// in the sphere to the problems on the plane. This reduction 
    /// is possible if the envelope of data is less than a hemisphere.
    /// </remarks>
    /// </summary>
    public class Gnomonic : MathTransform
    {
        private IMathTransform _inverse;
        private bool _isInverse = false;

        // vector of the center of the projection
        private readonly Vector3 _center;

        // basis vectors in the projection proskosti
        private readonly Vector3 _xAxis;
        private readonly Vector3 _yAxis;

        private static int getMinEntryIndex(double[] values)
        {
            int i = 0;
            if (Math.Abs(values[1]) < Math.Abs(values[0]))
                i = 1;
            if (Math.Abs(values[2]) < Math.Abs(values[i]))
                i = 2;
            return i;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.Gnomonic.
        /// Center (contact point) is the North Pole.
        /// </summary>
        public Gnomonic()
            : this(0, 90)
        {
        }

        private Gnomonic(bool isInverse)
        {
            _isInverse = isInverse;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystems.Transformations.Gnomonic.
        /// </summary>
        /// <param name="centerLongitude">Longitude of projection center in degrees</param>
        /// <param name="centerLatitude">Latitude of projection center in degrees</param>
        public Gnomonic(double centerLongitude, double centerLatitude)
            : this(false)
        {
            _center = 
                UnitSphere.LatLonToGeocentric(
                    Degrees.ToRadians(centerLatitude), 
                    Degrees.ToRadians(centerLongitude));

            double[] center = { _center.X, _center.Y, _center.Z };
            double[] vector = new double[3];

            int k = getMinEntryIndex(center);
            int j = (k + 2) % 3;
            int i = (j + 2) % 3;

            vector[i] = -center[j];
            vector[j] = center[i];
            vector[k] = 0;

            _xAxis = (new Vector3(vector[0], vector[1], vector[2])).Unitize();
            _yAxis = _center.CrossProduct(_xAxis);
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
        public override IMathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new Gnomonic(!_isInverse);
            return _inverse;
        }

        private double[] apply(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                Vector3 vector = 
                    UnitSphere.LatLonToGeocentric(
                        Degrees.ToRadians(p[i + 1]), 
                        Degrees.ToRadians(p[i]));

                double r = vector * _center;

                if (r < 1e-8)
                    throw new ApplicationException();

                vector = vector / r;

                result[i] = vector * _xAxis;
                result[i + 1] = vector * _yAxis;

            }

            return result;
        }

        private double[] applyInverted(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                if (delta == 3)
                    result[i + 2] = p[i + 2];

                Vector3 vector = _center + _xAxis * p[i] + _yAxis * p[i + 1];
                result[i + 1] = Radians.ToDegrees(UnitSphere.Latitude(vector));
                result[i] = Radians.ToDegrees(UnitSphere.Longitude(vector));
            }

            return result;
        }

        private static double asqrt(double v)
        {
            return ((v <= 0) ? 0 : Math.Sqrt(v));
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

#endif

    /// <summary>
    /// Implements an affine transformations.
    /// An affine transformation between two vector spaces (strictly speaking, two affine spaces) 
    /// consists of a linear transformation followed by a translation: x => A * x + b. 
    /// </summary>
    public class Affine : MathTransform
    {
        private IMathTransform _inverse;
        private bool _isInverse;
        private MathUtils.Matrix _matrix;
        private MathUtils.Matrix _inverseMatrix;

        /// <summary>
        /// Gets an array representing the elements of transformation matrix.
        /// </summary>
        /// <returns>Elements of the direct transformation matrix, even if this 
        /// transformation is inverted by Invert()</returns>
        public double[,] Elements
        {
            get 
            {
                return new double[,] 
                {
                    {_matrix[0, 0], _matrix[0, 1], _matrix[0, 2]},
                    {_matrix[1, 0], _matrix[1, 1], _matrix[1, 2]},
                    {_matrix[2, 0], _matrix[2, 1], _matrix[2, 2]},
                };
            }
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystem.Transformations.Affine.
        /// </summary>
        public Affine(MathUtils.Matrix matrix)
            : this(matrix, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystem.Transformations.Affine.
        /// </summary>
        /// <param name="m11">Value in the first row first column</param>
        /// <param name="m12">Value in the first row of the second column</param>
        /// <param name="m21">Value in the second row first column</param>
        /// <param name="m22">Value in the second row second column</param>
        /// <param name="dx">Value in the third row first column</param>
        /// <param name="dy">Value in the third row second column</param>
        public Affine(double m11, double m12, double m21, double m22, double dx, double dy)
            : this(new Matrix(new double[,]
                {
                    { m11, m12, 0 },
                    { m21, m22, 0 },
                    { dx,  dy,  1 }
                }), false)
        {
 
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystem.Transformations.Affine.
        /// </summary>
        private Affine(MathUtils.Matrix matrix, bool isInverse)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            if (matrix.Size != 3)
                throw new ArgumentException("Affine transform matrix size should be equal three.", "matrix");

            if (matrix[0, 2] != 0 || matrix[1, 2] != 0 || matrix[2, 2] != 1)
                throw new ArgumentException("Matrix does not define an affine transformation.", "matrix");

            _matrix = matrix;
            _isInverse = isInverse;
        }

        /// <summary>
        /// Initializes a new instance of the MapAround.CoordinateSystem.Transformations.Affine.
        /// </summary>
        private Affine(MathUtils.Matrix matrix, MathUtils.Matrix inverseMatrix, bool isInverse)
        {
            if (inverseMatrix == null)
                throw new ArgumentNullException("inverseMatrix");

            _matrix = matrix;
            _inverseMatrix = inverseMatrix;
            _isInverse = isInverse;
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transformation matrix 
        /// is not invertible.
        /// </remarks>
        public override IMathTransform Inverse()
        {
            if (_inverseMatrix == null)
            {
                _inverseMatrix = _matrix.GetInverseMatrix();

                //values ​​may differ slightly from the exact, 
                //in which case check the affinity will not be passed, 
                //so set the value of the third column manually
                _inverseMatrix[0, 2] = 0;
                _inverseMatrix[1, 2] = 0;
                _inverseMatrix[2, 2] = 1;
            }

            _inverse = new Affine(_matrix, _inverseMatrix, !_isInverse);

            return _inverse;
        }

        /// <summary>
        /// Inverts this transform.
        /// </summary>
        /// <remarks>
        /// This method may fail if the transformation matrix 
        /// is not invertible.
        /// </remarks>
        public override void Invert()
        {
            if (_inverseMatrix == null)
                _inverseMatrix = _matrix.GetInverseMatrix();

            _isInverse = !_isInverse;
        }

        private double[] apply(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                result[i] = p[i] * _matrix[0, 0] + p[i + 1] * _matrix[1, 0] + _matrix[2, 0];
                result[i + 1] = p[i] * _matrix[0, 1] + p[i + 1] * _matrix[1, 1] + _matrix[2, 1];

                if (delta == 3)
                    result[i + 2] = p[i + 2];
            }

            return result;
        }

        private double[] applyInverted(double[] p)
        {
            double[] result = new double[p.Length];

            int delta = 2;
            if (p.Length % 3 == 0)
                delta = 3;

            for (int i = 0; i < p.Length / 2; i += delta)
            {
                result[i] = p[i] * _inverseMatrix[0, 0] + p[i + 1] * _inverseMatrix[1, 0] + _inverseMatrix[2, 0];
                result[i + 1] = p[i] * _inverseMatrix[0, 1] + p[i + 1] * _inverseMatrix[1, 1] + _inverseMatrix[2, 1];

                if (delta == 3)
                    result[i + 2] = p[i + 2];
            }

            return result;
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
        /// Combines this affine transfor with the other.
        /// <remarks>
        /// The result of the combined transformation is equivalent to the result 
        /// of successive application of transformations. Preferable to use this 
        /// form of combination, not ConcatenatedTransform, because 
        /// ConcatenatedTransform applies each transformation consistently, and 
        /// CombineWith method calculates the resulting transformation matrix.
        /// </remarks>
        /// </summary>
        /// <param name="other">An affine transform to combine</param>
        public void CombineWith(Affine other)
        {
            _matrix = _matrix.Multiply(other._matrix);
            if (_inverseMatrix != null)
                _inverseMatrix = _matrix.GetInverseMatrix();
        }

        /// <summary>
        /// Create a rotation transform.
        /// </summary>
        /// <param name="angle">An angle (in radians) of rotation</param>
        /// <returns>A rotation transform</returns>
        public static Affine Rotation(double angle)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { Math.Cos(angle), Math.Sin(angle), 0},
                { -Math.Sin(angle),  Math.Cos(angle), 0},
                { 0, 0, 1},
            });
            return new Affine(matrix);
        }

        /// <summary>
        /// Create a transform of rotation at the specified point.
        /// </summary>
        /// <param name="coordinate">Center of rotation</param>
        /// <param name="angle">An angle (in radians) of rotation</param>
        /// <returns>A rotation transform</returns>
        public static Affine RotationAt(ICoordinate coordinate, double angle)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { 1,             0,             0},
                { 0,             1,             0},
                { -coordinate.X, -coordinate.Y, 1},
            });


            matrix = matrix.Multiply(new Matrix(
                new double[,]
            {
                { Math.Cos(angle), Math.Sin(angle), 0},
                { -Math.Sin(angle),  Math.Cos(angle), 0},
                { 0,                0,               1},
            }));

            matrix = matrix.Multiply(new Matrix(
            new double[,]
            {
                { 1,             0,             0},
                { 0,             1,             0},
                { coordinate.X, coordinate.Y, 1},
            }));
            return new Affine(matrix);
        }

        /// <summary>
        /// Create a scaling transform.
        /// </summary>
        /// <param name="factor">Scale factor value</param>
        /// <returns>A scaling transform</returns>
        public static Affine Scaling(double factor)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { factor, 0,      0 },
                { 0,      factor, 0 },
                { 0,      0,      1 },
            });
            return new Affine(matrix);
        }

        /// <summary>
        /// Create a scaling transform.
        /// </summary>
        /// <param name="xFactor">X scale factor</param>
        /// <param name="yFactor">Y scale factor value</param>
        /// <returns>A scaling transform</returns>
        public static Affine Scaling(double xFactor, double yFactor)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { xFactor, 0,       0 },
                { 0,       yFactor, 0 },
                { 0,       0,       1 },
            });
            return new Affine(matrix);
        }

        /// <summary>
        /// Create a translation transform.
        /// </summary>
        /// <param name="x">X translation</param>
        /// <param name="y">Y translation</param>
        /// <returns>A translation transform</returns>
        public static Affine Translation(double x, double y)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { x, y, 1 },
            });
            return new Affine(matrix);
        }

        /// <summary>
        /// Create a shearing transform.
        /// </summary>
        /// <param name="xShare">X sheare</param>
        /// <param name="yShare">Y sheare</param>
        /// <returns>A shearing transform</returns>
        public static Affine Shearing(double xShare, double yShare)
        {
            Matrix matrix = new Matrix(
                new double[,]
            {
                { 1,       xShare, 0 },
                { yShare,  1,      0 },
                { 0,       0,      1 },
            });
            return new Affine(matrix);
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
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> pnts = new List<double[]>(points.Count);
            foreach (double[] p in points)
                pnts.Add(Transform(p));
            return pnts;
        }
    }
}