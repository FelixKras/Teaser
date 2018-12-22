using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeaserDSV.Utilities
{
    /// <summary>
    /// Implements useful basic linear algebra operations, assuming that matrices are of 3D world.
    /// </summary>
    public class BLAS
    {
        /// <summary>
        /// Adds two vectors of same length and returns result (a + b).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a + b.</returns>
        public static double[] Add(double[] a, double[] b)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = a[i] + b[i];
            }

            return result;
        }

        /// <summary>
        /// Adds two vectors of same length and returns result (a + b).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a + b.</returns>
        public static int[] Add(int[] a, int[] b)
        {
            int[] result = new int[a.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = a[i] + b[i];
            }

            return result;
        }

        /// <summary>
        /// Adds two matrices of same size and returns result (A + B).
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>A + B.</returns>
        public static double[,] Add(double[,] A, double[,] B)
        {
            double[,] result = new double[A.GetLength(0), A.GetLength(1)];

            for (int i = 0; i < A.GetLength(0); i++)
            {
                for (int j = 0; j < A.GetLength(1); j++)
                {
                    result[i, j] = A[i, j] + B[i, j];
                }
            }

            return result;
        }

        /// <summary>
        /// Subtracts two vectors of same length and returns result (a - b).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a - b.</returns>
        public static double[] Subtract(double[] a, double[] b)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = a[i] - b[i];
            }

            return result;
        }

        /// <summary>
        /// Subtracts two vectors of same length and returns result (a - b).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>a - b.</returns>
        public static int[] Subtract(int[] a, int[] b)
        {
            int[] result = new int[a.Length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = a[i] - b[i];
            }

            return result;
        }

        /// <summary>
        /// Returns the L2 norm of a vector ||v||.
        /// </summary>
        /// <param name="v">Vector to compute L2-norm of.</param>
        /// <returns>L2-norm of vector ||v||.</returns>
        public static double Norm2(double[] v)
        {
            double result2 = 0.0;

            foreach (double item in v)
            {
                result2 += item * item;
            }

            return Math.Sqrt(result2);
        }

        /// <summary>
        /// Returns the L2 norm of a vector ||v||.
        /// </summary>
        /// <param name="v">Vector to compute L2-norm of.</param>
        /// <returns>L2-norm of vector ||v||.</returns>
        public static int Norm2(int[] v)
        {
            int result2 = 0;

            foreach (int item in v)
            {
                result2 += item * item;
            }

            return (int)Math.Sqrt(result2);
        }

        /// <summary>
        /// In-place normalization of given vector to become unit-length.
        /// </summary>
        /// <param name="v">Vector to normalize.</param>
        public static void Normalize(double[] v)
        {
            double norm = Norm2(v);

            for (int i = 0; i < v.Length; i++)
            {
                v[i] /= norm;
            }
        }

        /// <summary>
        /// Returns the dot product between two vectors (a, b).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>Dot product (a, b).</returns>
        public static double Dot(double[] a, double[] b)
        {
            double result = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                result += a[i] * b[i];
            }

            return result;
        }

        /// <summary>
        /// GEMM operation, matrix-matrix multiplication (A * B).
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns>A * B.</returns>
        public static double[,] Multiply(double[,] A, double[,] B)
        {
            double[,] result = new double[A.GetLength(0), B.GetLength(1)];

            for (int i = 0; i < A.GetLength(0); i++)
            {
                for (int j = 0; j < B.GetLength(1); j++)
                {
                    for (int k = 0; k < A.GetLength(1); k++)
                    {
                        result[i, j] += A[i, k] * B[k, j];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// GEMV operation, matrix-vector multiplication (A * b).
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <returns>A * b.</returns>
        public static double[] Multiply(double[,] A, double[] b)
        {
            double[] result = new double[b.Length];

            for (int i = 0; i < A.GetLength(0); i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    result[i] += A[i, j] * b[j];
                }
            }

            return result;
        }

        /// <summary>
        /// GEMV operation, vector-matrix multiplication (a * B).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="B"></param>
        /// <returns>a * B.</returns>
        public static double[] Multiply(double[] a, double[,] B)
        {
            double[] result = new double[B.GetLength(0)];

            for (int i = 0; i < result.Length; i++)
            {
                for (int j = 0; j < a.Length; j++)
                {
                    result[i] += a[j] * B[i,j];
                }
            }

            return result;
        }

        /// <summary>
        /// Multiplies each vector element with scalar and returns the result (a .* b).
        /// </summary>
        /// <param name="a">Vector.</param>
        /// <param name="b">Scalar.</param>
        /// <returns>a .* b.</returns>
        public static double[] PointwiseMultiply(double[] a, double b)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] * b;
            }

            return result;
        }

        /// <summary>
        /// Divides each vector element with scalar and returns the result (a ./ b).
        /// </summary>
        /// <param name="a">Vector.</param>
        /// <param name="b">Scalar.</param>
        /// <returns>a ./ b.</returns>
        public static double[] PointwiseDivide(double[] a, double b)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = a[i] / b;
            }

            return result;
        }

        /// <summary>
        /// Computes the absolute value of each vector element and returns the result.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>Absolute value of each vector element.</returns>
        public static double[] PointwiseAbsolute(double[] a)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = Math.Abs(a[i]);
            }

            return result;
        }

        /// <summary>
        /// Rounds each vector element and returns the result.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>Rounded value of each vector element.</returns>
        public static double[] PointwiseRound(double[] a)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = Math.Round(a[i]);
            }

            return result;
        }

        /// <summary>
        /// Compute floor for each vector element and returns the result.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>Floored value of each vector element.</returns>
        public static double[] PointwiseFloor(double[] a)
        {
            double[] result = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = Math.Floor(a[i]);
            }

            return result;
        }
    }
}
