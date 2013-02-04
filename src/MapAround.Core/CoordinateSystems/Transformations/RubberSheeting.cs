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
** File: RubberSheeting.cs
** 
** Copyright (c) Complex Solution Group. 
**
** Descriptions: Rubbersheeting transformation.
**
=============================================================================*/

namespace MapAround.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using MapAround.CoordinateSystems;
    using MapAround.Geometry;
    using MapAround.MathUtils;

    /// <summary>
    /// Implements a rubbersheeting transformation.
    /// </summary>
    public class RubberSheetingTransform : MathTransform
    {
        private ICoordinate[] _sourceControlPoints;
        private ICoordinate[] _destinationControlPoints;

        private int[] _affineTransformPointsIndicies;
        private Affine _optimalAffineTransform;
        private ICoordinate[] _controlPointsShifts;

        private Matrix getAffineTransformMatrix(ICoordinate p01, ICoordinate p02, ICoordinate p03,
                                                 ICoordinate p11, ICoordinate p12, ICoordinate p13)
        {
            Matrix a = new Matrix(new double[,] {
                                  { p02.X - p01.X, p02.Y - p01.Y, 0 },
                                  { p03.X - p01.X, p03.Y - p01.Y, 0 },
                                  { p01.X,         p01.Y,         1 }
            });

            Matrix b = new Matrix(new double[,] {
                                  { p12.X - p11.X, p12.Y - p11.Y, 0 },
                                  { p13.X - p11.X, p13.Y - p11.Y, 0 },
                                  { p11.X, p11.Y,                 1 }
            });

            if (!a.IsInvertible)
                return null;

            a = a.GetInverseMatrix();

            a = a.Multiply(b);
            a[0, 2] = 0;
            a[1, 2] = 0;
            a[2, 2] = 1;

            return a;
        }

        private int[] calculateOptimalAffineTransformPoints()
        {
            int[] result = new int[3];
            double minNorm = double.MaxValue;

            for (int i1 = 0; i1 < _sourceControlPoints.Length - 2; i1++)
                for (int i2 = i1 + 1; i2 < _sourceControlPoints.Length - 1; i2++)
                    for (int i3 = i2 + 1; i3 < _sourceControlPoints.Length; i3++)
                    {
                        ICoordinate p01 = _sourceControlPoints[i1];
                        ICoordinate p02 = _sourceControlPoints[i2];
                        ICoordinate p03 = _sourceControlPoints[i3];

                        ICoordinate p11 = _destinationControlPoints[i1];
                        ICoordinate p12 = _destinationControlPoints[i2];
                        ICoordinate p13 = _destinationControlPoints[i3];

                        Matrix m = getAffineTransformMatrix(p01, p02, p03, p11, p12, p13);

                        if (m != null)
                        {
                            ICoordinate[] tempPoints = new ICoordinate[_sourceControlPoints.Length];
                            for (int i = 0; i < _sourceControlPoints.Length; i++)
                                tempPoints[i] = (ICoordinate)_sourceControlPoints[i].Clone();

                            Affine affineTransform = new Affine(m);

                            for (int i = 0; i < tempPoints.Length; i++)
                                tempPoints[i] = 
                                    PlanimetryEnvironment.NewCoordinate(
                                        affineTransform.Transform(tempPoints[i].Values()));

                            double currentNorm = 0;
                            for (int i = 0; i < tempPoints.Length; i++)
                                currentNorm += PlanimetryAlgorithms.Distance(_destinationControlPoints[i], PlanimetryEnvironment.NewCoordinate(tempPoints[i].X, tempPoints[i].Y));

                            if (currentNorm < minNorm)
                            {
                                minNorm = currentNorm;
                                result[0] = i1;
                                result[1] = i2;
                                result[2] = i3;
                            }
                        }
                    }

            return result;
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <remarks>
        /// This method will fail.
        /// </remarks>
        /// <exception cref="NotSupportedException">Throws always</exception>
        public override IMathTransform Inverse()
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Inverts this transform.
        /// </summary>
        /// <remarks>
        /// This method will fail.
        /// </remarks>
        /// <exception cref="NotSupportedException">Throws always</exception>
        public override void Invert()
        {
            throw new NotSupportedException();
        }

        private void calcControlPointsShifts()
        {
            _controlPointsShifts = new ICoordinate[_sourceControlPoints.Length];

            for (int i = 0; i < _sourceControlPoints.Length; i++)
            {
                ICoordinate transformed =
                    PlanimetryEnvironment.NewCoordinate(_optimalAffineTransform.Transform(_sourceControlPoints[i].Values()));

                _controlPointsShifts[i] =
                    PlanimetryEnvironment.NewCoordinate(_destinationControlPoints[i].X - transformed.X,
                                                        _destinationControlPoints[i].Y - transformed.Y);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override double[] Transform(double[] point)
        {
            // check whether we were in a knot interpolation
            for (int k = 0; k < _sourceControlPoints.Length; k++)
            {
                if (_sourceControlPoints[k].X == point[0] &&
                    _sourceControlPoints[k].Y == point[1])
                {
                    // hit
                    return _destinationControlPoints[k].Values();
                }
            }

            ICoordinate source = PlanimetryEnvironment.NewCoordinate(point);
            ICoordinate result =
                PlanimetryEnvironment.NewCoordinate(_optimalAffineTransform.Transform(point));

            double[] distances = new double[_sourceControlPoints.Length];
            double[] w = new double[_sourceControlPoints.Length];

            // calculation of distances to nodes
            double maxDistance = 0;
            for (int t = 0; t < _sourceControlPoints.Length; t++)
            {
                distances[t] = PlanimetryAlgorithms.Distance(_sourceControlPoints[t], source);
                if (maxDistance < distances[t])
                    maxDistance = distances[t];
            }

            // calculation of the weights of the denominators of nodes
            double sum = 0;
            for (int k = 0; k < _sourceControlPoints.Length; k++)
            {
                double temp = (maxDistance - distances[k]) / (maxDistance * distances[k]);
                sum += Math.Pow(temp, 2);
            }

            // calculation of the weights of nodes
            for (int k = 0; k < _sourceControlPoints.Length; k++)
            {
                double temp = (maxDistance - distances[k]) / (maxDistance * distances[k]);
                w[k] = Math.Pow(temp, 2) / sum;
            }

            for (int k = 0; k < _sourceControlPoints.Length; k++)
            {
                result.X += w[k] * _controlPointsShifts[k].X;
                result.Y += w[k] * _controlPointsShifts[k].Y;
            }

            return result.Values();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public override List<double[]> TransformList(List<double[]> points)
        {
            List<double[]> pnts = new List<double[]>(points.Count);
            foreach (double[] p in points)
                pnts.Add(Transform(p));
            return pnts;
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
        /// Initializes a new instance of the 
        /// MapAround.CoordinateSystems.Transformations.RubberSheetingTransform 
        /// </summary>
        /// <param name="sourceControlPoints">An array containing coordinates of the source control points</param>
        /// <param name="destinationControlPoints">An array containing coordinates of the destination control points</param>
        public RubberSheetingTransform(ICoordinate[] sourceControlPoints,
            ICoordinate[] destinationControlPoints)
        {
            if (sourceControlPoints == null)
                throw new ArgumentNullException("sourceControlPoints");

            if (destinationControlPoints == null)
                throw new ArgumentNullException("destinationControlPoints");

            if (sourceControlPoints.Length < 3)
                throw new ArgumentException("Number of source control points should be equal or greater than three.", "sourceControlPoints");

            if (destinationControlPoints.Length < 3)
                throw new ArgumentException("Number of destination control points should be equal or greater than three.", "destinationControlPoints");

            if (destinationControlPoints.Length != sourceControlPoints.Length)
                throw new ArgumentException("Number of destination control points and source control points should be equal");

            _sourceControlPoints = sourceControlPoints;
            _destinationControlPoints = destinationControlPoints;

            _affineTransformPointsIndicies = calculateOptimalAffineTransformPoints();
            _optimalAffineTransform = new Affine(
                getAffineTransformMatrix(
                _sourceControlPoints[_affineTransformPointsIndicies[0]],
                _sourceControlPoints[_affineTransformPointsIndicies[1]],
                _sourceControlPoints[_affineTransformPointsIndicies[2]],
                _destinationControlPoints[_affineTransformPointsIndicies[0]],
                _destinationControlPoints[_affineTransformPointsIndicies[1]],
                _destinationControlPoints[_affineTransformPointsIndicies[2]]
                ));

            calcControlPointsShifts();
        }
    }
}