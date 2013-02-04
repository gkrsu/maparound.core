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



namespace MapAround.MathUtils 
{
    using System;
    using System.Collections;
    using System.Data;

    using MapAround.Geometry;

    /// <summary>
    /// The MapAround.MathUtils namespace contains classes implementing mathematical 
    /// algorithms that can be used for spatial data processing.
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Implements a methods which can be used
    /// with angular values represented in radians.
    /// </summary>
    public static class Radians
    {
        /// <summary>
        /// Converts a value of the angle from radians to degrees.
        /// </summary>
        /// <param name="radians">An angle value in radians</param>
        /// <returns>An angle value in degrees</returns>
        public static double ToDegrees(double radians)
        {
            return radians * 57.295779513082320876798154814105;
        }
    }

    /// <summary>
    /// Implements a methods which can be used
    /// with angular values represented in degrees.
    /// </summary>
    public static class Degrees
    {
        /// <summary>
        /// Converts a value of the angle from degrees to radians.
        /// </summary>
        /// <param name="degrees">An angle value in degrees</param>
        /// <returns>An angle value in radians</returns>
        public static double ToRadians(double degrees)
        {
            return degrees / 57.295779513082320876798154814105;
        }
    }

    /// <summary>
    /// Performs singular decomposition of matrix.
    /// </summary>
    internal static class SvdDecomposer
    {
        private static double dSign(double a, double b)
        {
            return (b >= 0 ? Math.Abs(a) : -Math.Abs(a));
        }

        /// <summary>
        /// Solves a linear equations system (A * x = B).
        /// </summary>
        /// <param name="A">The Matrix A</param>
        /// <param name="b">The vector B</param>
        /// <param name="x">The vector x</param>
        public static void Solve(double[,] A, double[] b, double[] x)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            int err;
            double[,] U = new double[m, n];
            double[,] V = new double[n, n];
            double[] W = new double[n];

            CalcDecomposition(A, m, n, W, 1, U, 1, V, out err);
            svdLeastSquares(U, W, V, m, n, b, x);
        }

        private static void svdLeastSquares(double[,] U, double[] w, double[,] V, int m, int n, double[] b, double[] x)
        {
            int jj, j, i;
            double s;
            double[] tmp = new double[n];

            for (j = 0; j < n; j++)
            {
                s = 0.0;
                if (Math.Abs(w[j]) > 1e-7)
                {
                    for (i = 0; i < m; i++)
                        s += U[i, j] * b[i];

                    s /= w[j];
                }
                tmp[j] = s;
            }

            for (j = 0; j < n; j++)
            {
                s = 0.0;
                for (jj = 0; jj < n; jj++)
                    s += V[j, jj] * tmp[jj];
                x[j] = s;
            }
        }

        /// <summary>
        /// Calculates a singular value decomposition of the matrix.
        /// </summary>
        /// <param name="A">Matrix</param>
        /// <param name="m">A row number of the matrix A</param>
        /// <param name="n">A column number of the matrix A</param>
        /// <param name="w">Vector of singular values</param>
        /// <param name="matu"></param>
        /// <param name="U">First matrix in decomposition</param>
        /// <param name="matv"></param>
        /// <param name="V">Third matrix in decomposition</param>
        /// <param name="ierr"></param>
        public static void CalcDecomposition(double[,] A, int m, int n, double[] w, int matu, double[,] U,
            int matv, double[,] V, out int ierr)
        {
            ierr = 0;

            int i, j, k, l, ii, i1, kk, k1, ll, l1, mn, its;
            double c, f, g, h, s, x, y, z, scale, anorm, tmp;
            double[] rv1 = new double[n];

            for (i = 0; i < m; i++)
                for (j = 0; j < n; j++)
                    U[i, j] = A[i, j];

            // 1 этап
            // ѕриведение к двухдиагональному виду отражени€ми ’аусхолдера
            l = 0;
            l1 = 0;
            g = 0;
            scale = 0;
            anorm = 0;

            for (i = 0; i < n; i++)
            {
                l = i + 1;
                rv1[i] = scale * g;
                g = 0;
                s = 0;
                scale = 0;
                if (i < m)
                {
                    for (k = i; k < m; k++)
                        scale += Math.Abs(U[k, i]);

                    if (scale != 0)
                    {

                        for (k = i; k < m; k++)
                        {
                            U[k, i] /= scale;
                            s += U[k, i] * U[k, i];
                        }


                        f = U[i, i];
                        g = -dSign(Math.Sqrt(s), f);
                        h = f * g - s;
                        U[i, i] = f - g;
                        if (i != n - 1)
                        {
                            for (j = l; j < n; j++)
                            {
                                s = 0;

                                for (k = i; k < m; k++)
                                    s += U[k, i] * U[k, j];

                                f = s / h;

                                for (k = i; k < m; k++)
                                    U[k, j] += f * U[k, i];
                            }
                        }

                        for (k = i; k < m; k++)
                            U[k, i] = scale * U[k, i];

                    }
                }

                w[i] = scale * g;
                g = 0;
                s = 0;
                scale = 0;

                if (i < m && i != n - 1)
                {
                    for (k = l; k < n; k++)
                        scale += Math.Abs(U[i, k]);

                    if (scale != 0)
                    {

                        for (k = l; k < n; k++)
                        {
                            U[i, k] /= scale;
                            s += U[i, k] * U[i, k];
                        }


                        f = U[i, l];
                        g = -dSign(Math.Sqrt(s), f);
                        h = f * g - s;
                        U[i, l] = f - g;

                        for (k = l; k < n; k++)
                            rv1[k] = U[i, k] / h;

                        if (i != m - 1)
                            for (j = l; j < m; j++)
                            {
                                s = 0;

                                for (k = l; k < n; k++)
                                    s += U[j, k] * U[i, k];

                                for (k = l; k < n; k++)
                                    U[j, k] += s * rv1[k];
                            }

                        for (k = l; k < n; k++)
                            U[i, k] *= scale;
                    }
                }
                if (anorm < (tmp = Math.Abs(w[i]) + Math.Abs(rv1[i])))
                    anorm = tmp;
            }

            // Ќакопление правосторонних преобразований

            if (matv > 0)
            {

                for (ii = n; ii > 0; ii--)
                {
                    i = ii - 1;

                    if (i != n - 1)
                    {
                        if (g != 0)
                        {
                            for (j = l; j < n; j++)
                                V[j, i] = (U[i, j] / U[i, l]) / g;
                            // двойное деление обходит возможный машинный ноль

                            for (j = l; j < n; j++)
                            {
                                s = 0;

                                for (k = l; k < n; k++)
                                    s += U[i, k] * V[k, j];

                                for (k = l; k < n; k++)
                                    V[k, j] += s * V[k, i];
                            }

                        }

                        for (j = l; j < n; j++)
                        {
                            V[i, j] = 0;
                            V[j, i] = 0;
                        }

                    }
                    V[i, i] = 1;
                    g = rv1[i];
                    l = i;
                }

            }

            // Ќакопление левосторонних преобразований

            if (matu > 0)
            {

                mn = n;
                if (m < n) mn = m; // mn = min(m, n)

                for (l = mn; l > 0; l--)
                {
                    i = l - 1;
                    g = w[i];
                    if (i != n - 1)
                        for (j = l; j < n; j++)
                            U[i, j] = 0;

                    if (g == 0)
                    {
                        for (j = i; j < m; j++)
                            U[j, i] = 0;
                    }
                    else
                    {
                        if (i != mn - 1)
                        {
                            for (j = l; j < n; j++)
                            {
                                s = 0;

                                for (k = l; k < m; k++)
                                    s += U[k, i] * U[k, j];
                                f = (s / U[i, i]) / g;
                                // двойное деление обходит возможный машинный ноль

                                for (k = i; k < m; k++)
                                    U[k, j] += f * U[k, i];
                            }
                        }
                        for (j = i; j < m; j++)
                            U[j, i] /= g;

                    }

                    U[i, i] += 1;
                }

            }

            // 2 этап
            // ѕриведение двухдиагональной матрицы к диагональному виду 

            for (kk = n; kk > 0; kk--)
            {
                k = kk - 1;
                k1 = k - 1;
                its = 0;

              // ѕроверка возможности расщеплени€

                splittingTest:

                for (ll = k + 1; ll > 0; ll--)
                {
                    l = ll - 1;
                    l1 = l - 1;
                    if (Math.Abs(rv1[l]) + anorm == anorm) goto convergenceTest;
                    // rv1[0] всегда равно нулю, поэтому
                    // выхода через конец цикла не будет 
                    if (Math.Abs(w[l1]) + anorm == anorm) break;
                }
                // ≈сли l больше чем 0, то rv1[l] присваиваетс€ нулевое значение

                c = 0;
                s = 1;

                for (i = l; i <= k; i++)
                {
                    f = s * rv1[i];
                    rv1[i] *= c;

                    if (Math.Abs(f) + anorm == anorm) goto convergenceTest;

                    g = w[i];
                    h = Math.Sqrt(f * f + g * g);
                    //assert(h > 0);
                    w[i] = h;
                    c = g / h;
                    s = -f / h;
                    if (matu > 0)
                        for (j = 0; j < m; j++)
                        {
                            y = U[j, l1];
                            z = U[j, i];
                            U[j, l1] = y * c + z * s;
                            U[j, i] = -y * s + z * c;
                        }
                }


                 // ѕроверка сходимости

                convergenceTest:

                z = w[k];

                if (l != k)
                {
                    // сдвиг выбираетс€ из нижнего углового минора пор€дка 2
                    if (its == 30)
                    {
                        ierr = k;
                        break;
                    }
                    its++;
                    x = w[l];
                    y = w[k1];
                    g = rv1[k1];
                    h = rv1[k];
                    f = ((y - z) * (y + z) + (g - h) * (g + h)) / (2.0 * h * y);
                    g = Math.Sqrt(f * f + 1.0);
                    f = ((x - z) * (x + z) + h * (y / (f + dSign(g, f)) - h)) / x;

                    // QR-преобразование
                    c = 1.0;
                    s = 1.0;

                    for (i1 = l; i1 <= k1; i1++)
                    {
                        i = i1 + 1;
                        g = rv1[i];
                        y = w[i];
                        h = s * g;
                        g = c * g;
                        z = Math.Sqrt(f * f + h * h);
                        rv1[i1] = z;
                        c = f / z;
                        s = h / z;
                        f = x * c + g * s;
                        g = -x * s + g * c;
                        h = y * s;
                        y = y * c;
                        if (matv > 0)
                            for (j = 0; j < n; j++)
                            {
                                x = V[j, i1];
                                z = V[j, i];
                                V[j, i1] = x * c + z * s;
                                V[j, i] = -x * s + z * c;
                            }

                        z = Math.Sqrt(f * f + h * h);
                        //assert(z > 0);
                        w[i1] = z;

                        // вращение может быть произвольным, если z равно нулю

                        if (z != 0.0)
                        {
                            c = f / z;
                            s = h / z;
                        }

                        f = c * g + s * y;
                        x = -s * g + c * y;

                        if (matu > 0)

                            for (j = 0; j < m; j++)
                            {
                                y = U[j, i1];
                                z = U[j, i];
                                U[j, i1] = y * c + z * s;
                                U[j, i] = -y * s + z * c;
                            }

                    }

                    rv1[l] = 0;
                    rv1[k] = f;
                    w[k] = x;
                    goto splittingTest;
                }

                // —ходимость

                if (z < 0)
                {
                    // w[k] делаетс€ неотрицательным
                    w[k] = -z;
                    if (matv > 0)
                        for (j = 0; j < n; j++)
                            V[j, k] = -V[j, k];
                }
            }
        }
    }

    /// <summary>
    /// Represents a square matrix.
    /// </summary>
    public class Matrix 
    {
        private double[,] _elements;   // матрица A
        private int _size;           // размерность задачи

        /// <summary>
        /// Initializes a new instance of MapAround.MathUtils.Matrix.
        /// </summary>
        /// <param name="elements">An array of doubles containing matrix elements</param>
        public Matrix(double[,] elements) 
        {
            if (elements == null)
                throw new ArgumentNullException("elements");

            int length = elements.GetLength(0);
            int matrixSize = elements.Length;
            if (matrixSize != length * length)
                throw new ArgumentException("Row number should be equal to col number.");

            this._elements = (double[,])elements.Clone();
            this._size = length;
        }

        /// <summary>
        /// Gets a size of this matrix.
        /// </summary>
        public int Size
        {
            get { return _size; }
        }

        /// <summary>
        /// Gets or sets an element of this matrix.
        /// </summary>
        /// <param name="i">A zero-based index of the element row</param>
        /// <param name="j">A zero-based index of the element column</param>
        /// <returns>Element value</returns>
        public double this[int i, int j]
        {
            get { return _elements[i, j]; }
            set { _elements[i, j] = value; }
        }

        /// <summary>
        /// Multiply this matrix to the other.
        /// </summary>
        /// <param name="other">Factor</param>
        /// <returns>Product</returns>
        public Matrix Multiply(Matrix other)
        {
            double[,] result = new double[_size, _size];

            for (int i = 0; i < _size; i++)
                for (int j = 0; j < _size; j++)
                {
                    double val = 0;
                    for (int r = 0; r < _size; r++)
                        val += this[i, r] * other[r, j];

                    result[i, j] = val;
                }

            return new Matrix(result);
        }

        /// <summary>
        /// Gets a value indicating whether this matrix is invertible.
        /// </summary>
        public bool IsInvertible
        {
            get
            {
                int m = _elements.GetLength(0);
                int n = _elements.GetLength(1);
                int err;
                double[,] U = new double[m, n];
                double[,] V = new double[n, n];
                double[] w = new double[n];

                SvdDecomposer.CalcDecomposition(_elements, m, n, w, 1, U, 1, V, out err);

                for (int i = 0; i < w.Length; i++)
                    if (w[i] == 0)
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Calculates the inverse matrix.
        /// </summary>
        /// <returns>The inverse matrix</returns>
        public Matrix GetInverseMatrix()
        {
            int m = _elements.GetLength(0);
            int n = _elements.GetLength(1);
            int err;
            double[,] U = new double[m, n];
            double[,] V = new double[n, n];
            double[] w = new double[n];

            SvdDecomposer.CalcDecomposition(_elements, m, n, w, 1, U, 1, V, out err);

            for (int i = 0; i < w.Length; i++)
            {
                if(w[i] == 0)
                    throw new InvalidOperationException("Matrix is not invertible");

                w[i] = 1 / w[i];
            }

            // V * w^-1 * Ut
            double[,] result = new double[_size, _size];
            double[,] temp = new double[_size, _size];

            for (int i = 0; i < _size; i++)
                for (int j = 0; j < _size; j++)
                {
                    double val = 0;
                    for (int r = 0; r < _size; r++)
                        val += V[i, r] * (r == j ? w[r] : 0);

                    temp[i, j] = val;
                }

            for (int i = 0; i < _size; i++)
                for (int j = 0; j < _size; j++)
                {
                    double val = 0;
                    for (int r = 0; r < _size; r++)
                        val += temp[i, r] * U[j, r];

                    result[i, j] = val;
                }

            return new Matrix(result);
        }
    }
}
